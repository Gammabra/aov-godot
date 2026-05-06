using System;

namespace AshesOfVelsingrad.Audio;

/// <summary>
///     Single source of truth for every audio track shipped with the game.
/// </summary>
/// <remarks>
///     <para>
///         Lives in Core (no Godot dependency) so adding a new track is a one-file
///         change that's covered by unit tests. The static constants below are the
///         public ids — call sites use them instead of hard-coded strings.
///     </para>
///     <para>
///         To add a new track, declare a <c>public const string</c> identifier and
///         add a matching <c>registry.Register(...)</c> call inside
///         <see cref="RegisterDefaults" />. Keep the id namespaced
///         (<c>music.foo</c>, <c>sfx.bar</c>) so they sort sensibly.
///     </para>
/// </remarks>
public static class AudioCatalog
{
    // Music ---------------------------------------------------------------

    /// <summary>Main menu theme. Plays on a loop while the menu is open.</summary>
    public const string MainMenuTheme = "music.main_menu";

    /// <summary>
    ///     Exploration / world-map theme. Selected by
    ///     <see cref="MusicContext.Exploration" /> in <c>AudioManager.SetMusicContext</c>.
    /// </summary>
    /// <remarks>
    ///     Intentionally <i>not</i> registered in <see cref="RegisterDefaults" /> until
    ///     the actual track ships — until then the manager falls back to
    ///     <c>StopMusic()</c> when entering exploration, which is exactly what we
    ///     want: the menu theme stops cleanly instead of bleeding into the world map.
    /// </remarks>
    public const string ExplorationTheme = "music.exploration";

    /// <summary>
    ///     Battle theme. Selected by <see cref="MusicContext.Battle" /> in
    ///     <c>AudioManager.SetMusicContext</c>.
    /// </summary>
    /// <remarks>
    ///     Same story as <see cref="ExplorationTheme" /> — declared as a stable id
    ///     here so call-sites can already reference it; the registry entry is added
    ///     when the audio ships.
    /// </remarks>
    public const string BattleTheme = "music.battle";

    /// <summary>
    ///     Populates <paramref name="registry" /> with every track shipped with the
    ///     game. Calling twice on the same registry throws — use
    ///     <see cref="IAudioRegistry.Clear" /> first if a hot reload is intended.
    /// </summary>
    /// <param name="registry">Registry to populate.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="registry" /> is null.</exception>
    public static void RegisterDefaults(IAudioRegistry registry)
    {
        if (registry == null) throw new ArgumentNullException(nameof(registry));

        registry.Register(new AudioTrack(
            Id: MainMenuTheme,
            ResourcePath: "res://assets/audio_assets/musics/TA_A.ogg",
            Bus: AudioBus.Music,
            BaseVolumeMultiplier: 1.0f,
            Loop: true));

        // Prison-tier exploration theme. Used until the open world / world-map
        // gets its own track; for now every exploration scene routes through
        // MusicContext.Exploration → this track via AudioManager.SetMusicContext.
        // Source WAV was 66 MB; transcoded to OGG Vorbis (-q:a 5) to stay sane on
        // disk and to match what Godot's loop-aware AudioStreamOggVorbis expects.
        registry.Register(new AudioTrack(
            Id: ExplorationTheme,
            ResourcePath: "res://assets/audio_assets/musics/DW_LVL1B.ogg",
            Bus: AudioBus.Music,
            BaseVolumeMultiplier: 1.0f,
            Loop: true));

        // Prison battle theme. Same story as ExplorationTheme — single track for
        // every Battle context until the catalog grows. Same WAV → OGG transcode
        // applied (47 MB → 3 MB).
        registry.Register(new AudioTrack(
            Id: BattleTheme,
            ResourcePath: "res://assets/audio_assets/musics/Awakening of the Juggernaut_FULL.ogg",
            Bus: AudioBus.Music,
            BaseVolumeMultiplier: 1.0f,
            Loop: true));
    }
}
