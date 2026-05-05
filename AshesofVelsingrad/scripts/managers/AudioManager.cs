using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Core.Audio;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     Centralised audio service for music, ambient layers and one-shot SFX.
/// </summary>
/// <remarks>
///     <para>
///         Implements <see cref="IAudioService" /> from the Core layer. The adapter
///         owns the Godot-specific concerns: <c>AudioServer</c> bus indices, pooled
///         <c>AudioStreamPlayer</c> nodes and <see cref="Tween" />-driven fades.
///     </para>
///     <para>
///         Wired to <see cref="SettingsManager" /> via the <c>SettingsChanged</c> signal:
///         volume sliders update bus volumes the same frame the player drags them. On
///         start-up the manager reads persisted volumes (<c>audio.volume.&lt;bus&gt;</c>)
///         from the settings store and applies them.
///     </para>
///     <para>
///         Optimisations: SFX players are pooled (no per-shot allocation), streams are
///         cached after first load, music playback uses two players that crossfade so
///         transitions don't pop, and ambient layers are kept in a dictionary so they
///         can be toggled independently without rebuilding the audio graph.
///     </para>
/// </remarks>
public partial class AudioManager : BaseManager, IAudioService
{
    #region Constants

    /// <summary>Prefix for every audio key stored through <see cref="SettingsManager" />.</summary>
    public const string SettingsPrefix = "audio.volume.";

    private const int _sfxPoolSize = 16;
    private const float _defaultMusicFadeSeconds = 1.5f;
    private const float _defaultAmbientFadeSeconds = 1.5f;
    private const float _defaultStopFadeSeconds = 1.0f;

    // Bus names match the existing audio UI (SettingsPages.cs) so the manager and the
    // legacy UI both target the same Godot buses. The enum stays language-neutral —
    // only the string mapping reflects the project's existing convention.
    private static readonly IReadOnlyDictionary<AudioBus, string> _busNames =
        new Dictionary<AudioBus, string>
        {
            { AudioBus.Master, "Master" },
            { AudioBus.Music, "Music" },
            { AudioBus.Sfx, "SFX" },
            { AudioBus.Ambient, "Ambient" },
            { AudioBus.Ui, "Ui" },
            { AudioBus.Voice, "Voices" },
        };

    #endregion

    #region Singleton

    /// <summary>Active <see cref="AudioManager" /> autoload, or <c>null</c> before <c>_Ready</c>.</summary>
    public new static AudioManager? Instance { get; private set; }

    #endregion

    #region Private Fields

    private readonly List<AudioStreamPlayer> _sfxPool = new(_sfxPoolSize);
    private int _sfxCursor;

    private AudioStreamPlayer _musicA = null!;
    private AudioStreamPlayer _musicB = null!;
    private bool _musicAIsActive = true;
    private Tween? _musicTween;
    private string? _currentMusicPath;

    private readonly Dictionary<string, AmbientLayer> _ambientLayers = new();

    private readonly Dictionary<string, AudioStream> _streamCache = new();

    private readonly Dictionary<AudioBus, float> _persistedLinearVolumes = new();
    private readonly Dictionary<AudioBus, bool> _muted = new();

    private SettingsManager? _settingsManager;
    private bool _settingsConnected;

    #endregion

    #region Lifecycle

    /// <summary>
    ///     Boots the audio service: ensures buses exist, builds the player pool, applies
    ///     persisted volumes and connects to <see cref="SettingsManager" />.
    /// </summary>
    /// <remarks>
    ///     Idempotent on the same instance — guards the test harness which calls
    ///     <c>Initialize</c> via reflection after Godot's <c>_Ready</c> has already
    ///     run it once. A second call on the same node is a no-op; a call from a
    ///     duplicate node frees the duplicate.
    /// </remarks>
    protected override void Initialize()
    {
        if (Instance == this) return;

        if (Instance != null)
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
        Name = "AudioManager";

        EnsureBuses();
        BuildPlayers();
        ApplyInitialVolumesFromSettings();
        ConnectToSettings();

        GD.Print("AudioManager initialized successfully");
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Mirror the <c>TurnManager</c> / <c>GameManager</c> pattern: clear the derived
    ///     static <see cref="Instance" /> so a scene reload doesn't see a stale singleton
    ///     and self-destruct as a "duplicate".
    /// </remarks>
    public override void _ExitTree()
    {
        DisconnectFromSettings();
        if (Instance == this) Instance = null;
        base._ExitTree();
    }

    #endregion

    #region Setup helpers

    /// <summary>
    ///     Creates any missing audio buses programmatically so the manager works without
    ///     a hand-authored <c>default_bus_layout.tres</c>. Master always exists; every
    ///     other bus is appended and routed back to Master, so the master slider keeps
    ///     scaling every other bus.
    /// </summary>
    private static void EnsureBuses()
    {
        var masterName = _busNames[AudioBus.Master];
        foreach (var (bus, name) in _busNames)
        {
            if (bus == AudioBus.Master) continue;
            if (AudioServer.GetBusIndex(name) != -1) continue;

            var newIndex = AudioServer.BusCount;
            AudioServer.AddBus(newIndex);
            AudioServer.SetBusName(newIndex, name);
            AudioServer.SetBusSend(newIndex, masterName);
        }
    }

    private void BuildPlayers()
    {
        _musicA = CreatePlayer("MusicA", _busNames[AudioBus.Music]);
        _musicB = CreatePlayer("MusicB", _busNames[AudioBus.Music]);
        _musicA.VolumeDb = AudioVolumeMath.SilenceDb;
        _musicB.VolumeDb = AudioVolumeMath.SilenceDb;

        for (var i = 0; i < _sfxPoolSize; i++)
        {
            _sfxPool.Add(CreatePlayer($"Sfx{i}", _busNames[AudioBus.Sfx]));
        }
    }

    private AudioStreamPlayer CreatePlayer(string name, string busName)
    {
        var player = new AudioStreamPlayer
        {
            Name = name,
            Bus = busName,
            ProcessMode = ProcessModeEnum.Always, // keep audio when the game pauses
        };
        AddChild(player);
        return player;
    }

    #endregion

    #region IAudioService — Music

    /// <inheritdoc />
    public void PlayMusic(string trackPath, float fadeSeconds = _defaultMusicFadeSeconds, bool loop = true)
    {
        if (string.IsNullOrEmpty(trackPath))
        {
            GD.PrintErr("AudioManager.PlayMusic called with empty trackPath");
            return;
        }

        if (_currentMusicPath == trackPath
            && (_musicAIsActive ? _musicA : _musicB).Playing)
        {
            return;
        }

        var stream = LoadStream(trackPath);
        if (stream == null) return;

        ApplyLoopFlag(stream, loop);

        var nextPlayer = _musicAIsActive ? _musicB : _musicA;
        var prevPlayer = _musicAIsActive ? _musicA : _musicB;
        _musicAIsActive = !_musicAIsActive;

        nextPlayer.Stream = stream;
        nextPlayer.VolumeDb = AudioVolumeMath.SilenceDb;
        nextPlayer.Play();

        CrossfadeMusic(prevPlayer, nextPlayer, Mathf.Max(0f, fadeSeconds));
        _currentMusicPath = trackPath;
    }

    /// <inheritdoc />
    public void StopMusic(float fadeSeconds = _defaultStopFadeSeconds)
    {
        var active = _musicAIsActive ? _musicA : _musicB;
        var inactive = _musicAIsActive ? _musicB : _musicA;
        FadeOutAndStop(active, Mathf.Max(0f, fadeSeconds));
        FadeOutAndStop(inactive, 0f);
        _currentMusicPath = null;
    }

    private void CrossfadeMusic(AudioStreamPlayer outgoing, AudioStreamPlayer incoming, float fadeSeconds)
    {
        _musicTween?.Kill();

        if (fadeSeconds <= 0f)
        {
            outgoing.Stop();
            incoming.VolumeDb = 0f;
            return;
        }

        _musicTween = CreateTween().SetParallel(true);
        _musicTween.TweenProperty(incoming, "volume_db", 0f, fadeSeconds)
            .From(AudioVolumeMath.SilenceDb);
        _musicTween.TweenProperty(outgoing, "volume_db", AudioVolumeMath.SilenceDb, fadeSeconds);
        _musicTween.Chain().TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(outgoing)) outgoing.Stop();
        }));
    }

    private static void FadeOutAndStop(AudioStreamPlayer player, float fadeSeconds)
    {
        if (!player.Playing) return;

        if (fadeSeconds <= 0f)
        {
            player.Stop();
            return;
        }

        var tween = player.CreateTween();
        tween.TweenProperty(player, "volume_db", AudioVolumeMath.SilenceDb, fadeSeconds);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(player)) player.Stop();
        }));
    }

    #endregion

    #region IAudioService — Ambient

    /// <inheritdoc />
    public void PlayAmbient(string layerId, string trackPath, float fadeSeconds = _defaultAmbientFadeSeconds)
    {
        if (string.IsNullOrEmpty(layerId) || string.IsNullOrEmpty(trackPath))
        {
            GD.PrintErr("AudioManager.PlayAmbient requires non-empty layerId and trackPath");
            return;
        }

        var stream = LoadStream(trackPath);
        if (stream == null) return;
        ApplyLoopFlag(stream, true);

        if (_ambientLayers.TryGetValue(layerId, out var existing))
        {
            if (existing.TrackPath == trackPath && existing.Player.Playing) return;

            // Replace the running layer in place so we keep the same node and just
            // swap the stream — saves an AddChild call and a frame of silence.
            existing.TrackPath = trackPath;
            existing.Player.Stream = stream;
            existing.Player.VolumeDb = AudioVolumeMath.SilenceDb;
            existing.Player.Play();
            FadeIn(existing.Player, Mathf.Max(0f, fadeSeconds));
            return;
        }

        var player = CreatePlayer($"Ambient_{layerId}", _busNames[AudioBus.Ambient]);
        player.Stream = stream;
        player.VolumeDb = AudioVolumeMath.SilenceDb;
        player.Play();
        FadeIn(player, Mathf.Max(0f, fadeSeconds));

        _ambientLayers[layerId] = new AmbientLayer(player, trackPath);
    }

    /// <inheritdoc />
    public void StopAmbient(string layerId, float fadeSeconds = _defaultStopFadeSeconds)
    {
        if (!_ambientLayers.TryGetValue(layerId, out var layer)) return;

        var player = layer.Player;
        var fade = Mathf.Max(0f, fadeSeconds);

        if (fade <= 0f)
        {
            player.Stop();
            player.QueueFree();
        }
        else
        {
            var tween = player.CreateTween();
            tween.TweenProperty(player, "volume_db", AudioVolumeMath.SilenceDb, fade);
            tween.TweenCallback(Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(player)) return;
                player.Stop();
                player.QueueFree();
            }));
        }

        _ambientLayers.Remove(layerId);
    }

    /// <inheritdoc />
    public void StopAllAmbient(float fadeSeconds = _defaultStopFadeSeconds)
    {
        // Snapshot keys first so the dictionary mutation inside StopAmbient is safe.
        var keys = new string[_ambientLayers.Count];
        _ambientLayers.Keys.CopyTo(keys, 0);
        foreach (var id in keys) StopAmbient(id, fadeSeconds);
    }

    private static void FadeIn(AudioStreamPlayer player, float fadeSeconds)
    {
        if (fadeSeconds <= 0f)
        {
            player.VolumeDb = 0f;
            return;
        }

        var tween = player.CreateTween();
        tween.TweenProperty(player, "volume_db", 0f, fadeSeconds)
            .From(AudioVolumeMath.SilenceDb);
    }

    #endregion

    #region IAudioService — SFX / UI

    /// <inheritdoc />
    public void PlaySfx(string trackPath, float volumeLinear = 1f, float pitchScale = 1f)
    {
        PlayOneShot(trackPath, _busNames[AudioBus.Sfx], volumeLinear, pitchScale);
    }

    /// <inheritdoc />
    public void PlayUi(string trackPath, float volumeLinear = 1f)
    {
        PlayOneShot(trackPath, _busNames[AudioBus.Ui], volumeLinear, pitchScale: 1f);
    }

    private void PlayOneShot(string trackPath, string busName, float volumeLinear, float pitchScale)
    {
        if (string.IsNullOrEmpty(trackPath)) return;
        var stream = LoadStream(trackPath);
        if (stream == null) return;

        var player = NextSfxPlayer();
        player.Bus = busName;
        player.Stream = stream;
        player.VolumeDb = AudioVolumeMath.LinearToDb(volumeLinear);
        player.PitchScale = Mathf.Max(0.01f, pitchScale);
        player.Play();
    }

    private AudioStreamPlayer NextSfxPlayer()
    {
        // Round-robin first, then steal the oldest player if everything is busy.
        for (var i = 0; i < _sfxPool.Count; i++)
        {
            var idx = (_sfxCursor + i) % _sfxPool.Count;
            var candidate = _sfxPool[idx];
            if (!candidate.Playing)
            {
                _sfxCursor = (idx + 1) % _sfxPool.Count;
                return candidate;
            }
        }

        var stolen = _sfxPool[_sfxCursor];
        _sfxCursor = (_sfxCursor + 1) % _sfxPool.Count;
        return stolen;
    }

    #endregion

    #region IAudioService — Bus volume

    /// <inheritdoc />
    public float GetBusVolume(AudioBus bus)
    {
        if (_muted.TryGetValue(bus, out var muted) && muted)
        {
            return _persistedLinearVolumes.TryGetValue(bus, out var stored) ? stored : 0f;
        }

        var name = _busNames[bus];
        var index = AudioServer.GetBusIndex(name);
        if (index == -1) return 0f;
        return AudioVolumeMath.DbToLinear(AudioServer.GetBusVolumeDb(index));
    }

    /// <inheritdoc />
    public void SetBusVolume(AudioBus bus, float linear)
    {
        var clamped = AudioVolumeMath.ClampLinear(linear);
        _persistedLinearVolumes[bus] = clamped;

        if (_muted.TryGetValue(bus, out var muted) && muted)
        {
            // Keep the persisted slider value so unmuting can restore it, but leave
            // the live bus silent.
            ApplyBusDb(bus, AudioVolumeMath.SilenceDb);
        }
        else
        {
            ApplyBusDb(bus, AudioVolumeMath.LinearToDb(clamped));
        }

        PersistToSettings(bus, clamped);
    }

    /// <inheritdoc />
    public bool IsBusMuted(AudioBus bus)
    {
        return _muted.TryGetValue(bus, out var muted) && muted;
    }

    /// <inheritdoc />
    public void SetBusMuted(AudioBus bus, bool muted)
    {
        _muted[bus] = muted;
        var name = _busNames[bus];
        var index = AudioServer.GetBusIndex(name);
        if (index == -1) return;

        AudioServer.SetBusMute(index, muted);
        if (!muted && _persistedLinearVolumes.TryGetValue(bus, out var stored))
        {
            ApplyBusDb(bus, AudioVolumeMath.LinearToDb(stored));
        }
    }

    private static void ApplyBusDb(AudioBus bus, float db)
    {
        var index = AudioServer.GetBusIndex(_busNames[bus]);
        if (index == -1) return;
        AudioServer.SetBusVolumeDb(index, db);
    }

    #endregion

    #region Settings binding

    /// <summary>Returns the canonical settings key for a bus (e.g. <c>audio.volume.music</c>).</summary>
    /// <param name="bus">Bus to convert.</param>
    public static string GetSettingsKey(AudioBus bus) => SettingsPrefix + bus.ToString().ToLowerInvariant();

    private void ApplyInitialVolumesFromSettings()
    {
        var settings = SettingsManager.Instance;
        foreach (var bus in _busNames.Keys)
        {
            var defaultLinear = bus == AudioBus.Master ? 1.0f : 0.8f;
            var stored = settings?.GetSetting(GetSettingsKey(bus), defaultLinear) ?? defaultLinear;
            var clamped = AudioVolumeMath.ClampLinear(stored);
            _persistedLinearVolumes[bus] = clamped;
            ApplyBusDb(bus, AudioVolumeMath.LinearToDb(clamped));
        }
    }

    private void ConnectToSettings()
    {
        _settingsManager = SettingsManager.Instance;
        if (_settingsManager == null || _settingsConnected) return;

        _settingsManager.SettingsChanged += OnSettingsChanged;
        _settingsConnected = true;
    }

    private void DisconnectFromSettings()
    {
        if (_settingsManager == null || !_settingsConnected) return;
        _settingsManager.SettingsChanged -= OnSettingsChanged;
        _settingsConnected = false;
        _settingsManager = null;
    }

    private void OnSettingsChanged(string key, Variant value)
    {
        if (string.IsNullOrEmpty(key) || !key.StartsWith(SettingsPrefix, StringComparison.Ordinal))
        {
            return;
        }

        if (!TryResolveBus(key, out var bus)) return;

        var linear = AudioVolumeMath.ClampLinear(value.AsSingle());
        _persistedLinearVolumes[bus] = linear;
        if (!IsBusMuted(bus))
        {
            ApplyBusDb(bus, AudioVolumeMath.LinearToDb(linear));
        }
    }

    private static bool TryResolveBus(string key, out AudioBus bus)
    {
        foreach (var (candidate, _) in _busNames)
        {
            if (string.Equals(GetSettingsKey(candidate), key, StringComparison.Ordinal))
            {
                bus = candidate;
                return true;
            }
        }

        bus = AudioBus.Master;
        return false;
    }

    private void PersistToSettings(AudioBus bus, float linear)
    {
        var settings = _settingsManager ?? SettingsManager.Instance;
        settings?.SetSetting(GetSettingsKey(bus), linear);
    }

    #endregion

    #region Resource cache

    private AudioStream? LoadStream(string path)
    {
        if (_streamCache.TryGetValue(path, out var cached)) return cached;

        var loaded = ResourceLoader.Load<AudioStream>(path);
        if (loaded == null)
        {
            GD.PrintErr($"AudioManager: failed to load stream at '{path}'");
            return null;
        }

        _streamCache[path] = loaded;
        return loaded;
    }

    private static void ApplyLoopFlag(AudioStream stream, bool loop)
    {
        switch (stream)
        {
            case AudioStreamOggVorbis ogg:
                ogg.Loop = loop;
                break;
            case AudioStreamMP3 mp3:
                mp3.Loop = loop;
                break;
            case AudioStreamWav wav:
                wav.LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled;
                break;
        }
    }

    #endregion

    #region Nested types

    /// <summary>Tracks an active ambient layer and the resource path that produced it.</summary>
    private sealed class AmbientLayer
    {
        public AudioStreamPlayer Player { get; }
        public string TrackPath { get; set; }

        public AmbientLayer(AudioStreamPlayer player, string trackPath)
        {
            Player = player;
            TrackPath = trackPath;
        }
    }

    #endregion
}
