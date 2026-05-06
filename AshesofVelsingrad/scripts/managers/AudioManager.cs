using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Audio;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     Centralised audio service for music, ambient layers and one-shot SFX.
///     Implements <see cref="IAudioService" /> from the Core layer and bridges Godot
///     audio APIs to the rest of the game.
/// </summary>
public partial class AudioManager : BaseManager, IAudioService
{
    #region Constants

    public const string SettingsPrefix = "audio.volume.";

    private const int _sfxPoolSize = 16;
    private const float _defaultMusicFadeSeconds = 1.5f;
    private const float _defaultAmbientFadeSeconds = 1.5f;
    private const float _defaultStopFadeSeconds = 1.0f;

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
    private MusicContext _musicContext = MusicContext.None;

    private readonly Dictionary<string, AmbientLayer> _ambientLayers = new();
    private readonly Dictionary<string, AudioStream> _streamCache = new();
    private readonly Dictionary<AudioBus, float> _persistedLinearVolumes = new();
    private readonly Dictionary<AudioBus, bool> _muted = new();

    private SettingsManager? _settingsManager;
    private bool _settingsConnected;

    private readonly AudioRegistry _registry = new();

    #endregion

    #region Public Properties

    public IAudioRegistry Registry => _registry;

    #endregion

    #region Lifecycle

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
        RegisterCatalog();
        ApplyInitialVolumesFromSettings();
        ConnectToSettings();

        GD.Print($"AudioManager initialized successfully ({_registry.Count} track(s), sfxPool={_sfxPool.Count})");

        // The game always boots into a menu — start the menu theme automatically
        // so any boot scene gets music without needing a per-scene hook. Deferred
        // so it runs after the autoload chain finishes and the scene tree is ready
        // for resource loads.
        Callable.From(PlayBootMusic).CallDeferred();
    }

    private void PlayBootMusic()
    {
        // Defensive default for the project's normal boot scene (menu_beta.tscn),
        // which has no script on its root and so doesn't declare a context itself
        // — its <c>ButtonMenu</c> children pick up the slack but only after this
        // deferred call is queued.
        //
        // The check matters when the developer launches a different scene directly
        // (F6 in the editor, or `--main-scene` from CLI). In that case the loaded
        // scene's <c>_Ready</c> runs *before* this deferred call (autoloads
        // initialize first, but their deferred callbacks fire at end-of-frame
        // *after* the new scene's _Ready), so by the time we get here the scene
        // has already declared its real context — Battle, Exploration, whatever.
        // Without this guard we'd happily overwrite that with MainMenu and the
        // menu theme would bleed into the wrong scene.
        if (_musicContext != MusicContext.None) return;
        SetMusicContext(MusicContext.MainMenu);
    }

    public override void _ExitTree()
    {
        DisconnectFromSettings();
        if (Instance == this) Instance = null;
        base._ExitTree();
    }

    #endregion

    #region Setup helpers

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
            ProcessMode = ProcessModeEnum.Always,
        };
        AddChild(player);
        return player;
    }

    private void RegisterCatalog()
    {
        if (_registry.Count > 0) _registry.Clear();
        AudioCatalog.RegisterDefaults(_registry);
    }

    #endregion

    #region IAudioService — Registry dispatch

    public void Play(string trackId)
    {
        if (!_registry.TryGet(trackId, out var track))
        {
            GD.PrintErr($"AudioManager: no track registered with id '{trackId}'");
            return;
        }

        var multiplier = AudioVolumeMath.ClampLinear(track.BaseVolumeMultiplier);

        switch (track.Bus)
        {
            case AudioBus.Music:
                PlayMusicInternal(track.ResourcePath, _defaultMusicFadeSeconds, track.Loop, multiplier);
                break;

            case AudioBus.Ambient:
                PlayAmbientInternal(track.Id, track.ResourcePath, _defaultAmbientFadeSeconds, multiplier);
                break;

            case AudioBus.Sfx:
            case AudioBus.Ui:
            case AudioBus.Voice:
                PlayOneShot(track.ResourcePath, _busNames[track.Bus], multiplier, pitchScale: 1f);
                break;

            case AudioBus.Master:
            default:
                GD.PrintErr($"AudioManager: track '{trackId}' uses unsupported bus '{track.Bus}'");
                break;
        }
    }

    #endregion

    #region Music context

    /// <summary>
    ///     Declares what kind of scene is now active so the right music plays.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Scenes call this on <c>_Ready</c> (or whenever they enter a new
    ///         gameplay phase) instead of poking individual track ids — the catalog
    ///         is then free to re-map a context to a different track without
    ///         touching every call-site.
    ///     </para>
    ///     <para>
    ///         When the catalog has no track registered for the requested context
    ///         (typical during development before exploration / battle music ships)
    ///         the manager fades the current music out, so the menu theme doesn't
    ///         leak into exploration or combat. Calls are idempotent — passing the
    ///         current context is a no-op.
    ///     </para>
    /// </remarks>
    /// <param name="context">The scene category currently active.</param>
    public void SetMusicContext(MusicContext context)
    {
        if (_musicContext == context)
        {
            GD.Print($"[AudioManager] SetMusicContext({context}) — already active, no-op.");
            return;
        }

        var previous = _musicContext;
        _musicContext = context;

        var trackId = ResolveTrackId(context);
        if (trackId is not null && _registry.Contains(trackId))
        {
            GD.Print($"[AudioManager] SetMusicContext: {previous} → {context} (playing '{trackId}').");
            Play(trackId);
        }
        else
        {
            GD.Print($"[AudioManager] SetMusicContext: {previous} → {context} (no track registered for context — stopping music).");
            StopMusic();
        }
    }

    /// <summary>Returns the currently-declared music context.</summary>
    public MusicContext CurrentMusicContext => _musicContext;

    private static string? ResolveTrackId(MusicContext context) => context switch
    {
        MusicContext.MainMenu => AudioCatalog.MainMenuTheme,
        MusicContext.Exploration => AudioCatalog.ExplorationTheme,
        MusicContext.Battle => AudioCatalog.BattleTheme,
        _ => null,
    };

    #endregion

    #region IAudioService — Music

    public void PlayMusic(string trackPath, float fadeSeconds = _defaultMusicFadeSeconds, bool loop = true)
    {
        PlayMusicInternal(trackPath, fadeSeconds, loop, volumeMultiplier: 1f);
    }

    public void StopMusic(float fadeSeconds = _defaultStopFadeSeconds)
    {
        var active = _musicAIsActive ? _musicA : _musicB;
        var inactive = _musicAIsActive ? _musicB : _musicA;
        FadeOutAndStop(active, Mathf.Max(0f, fadeSeconds));
        FadeOutAndStop(inactive, 0f);
        _currentMusicPath = null;
    }

    private void PlayMusicInternal(string trackPath, float fadeSeconds, bool loop, float volumeMultiplier)
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

        var targetDb = AudioVolumeMath.LinearToDb(AudioVolumeMath.ClampLinear(volumeMultiplier));
        CrossfadeMusic(prevPlayer, nextPlayer, Mathf.Max(0f, fadeSeconds), targetDb);
        _currentMusicPath = trackPath;
    }

    private void CrossfadeMusic(
        AudioStreamPlayer outgoing,
        AudioStreamPlayer incoming,
        float fadeSeconds,
        float targetDb)
    {
        _musicTween?.Kill();

        if (fadeSeconds <= 0f)
        {
            outgoing.Stop();
            incoming.VolumeDb = targetDb;
            return;
        }

        _musicTween = CreateTween().SetParallel(true);
        _musicTween.TweenProperty(incoming, "volume_db", targetDb, fadeSeconds)
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

    public void PlayAmbient(string layerId, string trackPath, float fadeSeconds = _defaultAmbientFadeSeconds)
    {
        PlayAmbientInternal(layerId, trackPath, fadeSeconds, volumeMultiplier: 1f);
    }

    private void PlayAmbientInternal(string layerId, string trackPath, float fadeSeconds, float volumeMultiplier)
    {
        if (string.IsNullOrEmpty(layerId) || string.IsNullOrEmpty(trackPath))
        {
            GD.PrintErr("AudioManager.PlayAmbient requires non-empty layerId and trackPath");
            return;
        }

        var stream = LoadStream(trackPath);
        if (stream == null) return;
        ApplyLoopFlag(stream, true);

        var targetDb = AudioVolumeMath.LinearToDb(AudioVolumeMath.ClampLinear(volumeMultiplier));

        if (_ambientLayers.TryGetValue(layerId, out var existing))
        {
            if (existing.TrackPath == trackPath && existing.Player.Playing) return;

            existing.TrackPath = trackPath;
            existing.Player.Stream = stream;
            existing.Player.VolumeDb = AudioVolumeMath.SilenceDb;
            existing.Player.Play();
            FadeIn(existing.Player, Mathf.Max(0f, fadeSeconds), targetDb);
            return;
        }

        var player = CreatePlayer($"Ambient_{layerId}", _busNames[AudioBus.Ambient]);
        player.Stream = stream;
        player.VolumeDb = AudioVolumeMath.SilenceDb;
        player.Play();
        FadeIn(player, Mathf.Max(0f, fadeSeconds), targetDb);

        _ambientLayers[layerId] = new AmbientLayer(player, trackPath);
    }

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

    public void StopAllAmbient(float fadeSeconds = _defaultStopFadeSeconds)
    {
        var keys = new string[_ambientLayers.Count];
        _ambientLayers.Keys.CopyTo(keys, 0);
        foreach (var id in keys) StopAmbient(id, fadeSeconds);
    }

    private static void FadeIn(AudioStreamPlayer player, float fadeSeconds, float targetDb)
    {
        if (fadeSeconds <= 0f)
        {
            player.VolumeDb = targetDb;
            return;
        }

        var tween = player.CreateTween();
        tween.TweenProperty(player, "volume_db", targetDb, fadeSeconds)
            .From(AudioVolumeMath.SilenceDb);
    }

    #endregion

    #region IAudioService — SFX / UI

    public void PlaySfx(string trackPath, float volumeLinear = 1f, float pitchScale = 1f)
    {
        PlayOneShot(trackPath, _busNames[AudioBus.Sfx], volumeLinear, pitchScale);
    }

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

    public void SetBusVolume(AudioBus bus, float linear)
    {
        var clamped = AudioVolumeMath.ClampLinear(linear);
        _persistedLinearVolumes[bus] = clamped;

        if (_muted.TryGetValue(bus, out var muted) && muted)
        {
            ApplyBusDb(bus, AudioVolumeMath.SilenceDb);
        }
        else
        {
            ApplyBusDb(bus, AudioVolumeMath.LinearToDb(clamped));
        }

        PersistToSettings(bus, clamped);
    }

    public bool IsBusMuted(AudioBus bus)
    {
        return _muted.TryGetValue(bus, out var muted) && muted;
    }

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

        // ResourceLoader.Exists() catches the most common failure mode — a freshly
        // added asset that hasn't been imported yet, so there's no .import sidecar
        // and no cached resource under .godot/imported/. Without this branch the
        // failure shows up as a generic "Load returned null", which is easy to
        // miss in a noisy Output panel.
        if (!ResourceLoader.Exists(path))
        {
            GD.PushError(
                $"AudioManager: stream '{path}' has no Godot import. " +
                "Run the project once in the editor (or `godot --headless --import` " +
                "from the project folder) so the .import sidecar gets generated.");
            return null;
        }

        var loaded = ResourceLoader.Load<AudioStream>(path);
        if (loaded == null)
        {
            GD.PushError($"AudioManager: ResourceLoader.Load returned null for '{path}' — wrong path or unsupported format.");
            return null;
        }

        _streamCache[path] = loaded;
        GD.Print($"[AudioManager] Loaded stream '{path}' (cached).");
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
