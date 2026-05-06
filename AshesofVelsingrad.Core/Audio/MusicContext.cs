namespace AshesOfVelsingrad.Audio;

/// <summary>
///     High-level scene categories used to pick the right music track.
/// </summary>
/// <remarks>
///     <para>
///         Scenes call <c>AudioManager.Instance?.SetMusicContext(...)</c> on
///         <c>_Ready</c> (or whenever they enter a new gameplay phase) to declare
///         what kind of music should be playing. The manager resolves the context
///         to a <see cref="AudioCatalog" /> id and either crossfades to that track
///         or — if no track is registered for the context yet — fades the current
///         music out.
///     </para>
///     <para>
///         This is what keeps the main-menu theme from leaking into exploration
///         and battle: the menu scene declares <see cref="MainMenu" />, the
///         exploration scene declares <see cref="Exploration" />, and the battle
///         scene declares <see cref="Battle" />. As long as every gameplay scene
///         sets its own context, the audio layer always knows what should be
///         playing — even before the exploration / battle tracks ship.
///     </para>
/// </remarks>
public enum MusicContext
{
    /// <summary>No declared context. Any music currently playing fades out.</summary>
    None,

    /// <summary>Main menu / title screen / settings — plays <see cref="AudioCatalog.MainMenuTheme" />.</summary>
    MainMenu,

    /// <summary>Exploration / world map — plays <see cref="AudioCatalog.ExplorationTheme" /> when registered.</summary>
    Exploration,

    /// <summary>In-battle — plays <see cref="AudioCatalog.BattleTheme" /> when registered.</summary>
    Battle,
}
