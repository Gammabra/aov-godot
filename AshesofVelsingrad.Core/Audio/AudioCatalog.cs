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
    }
}
