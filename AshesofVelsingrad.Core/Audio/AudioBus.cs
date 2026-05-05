namespace AshesOfVelsingrad.Audio;

/// <summary>
///     Logical mixing channels exposed by the audio service.
/// </summary>
/// <remarks>
///     <para>
///         The values double as indices into <c>AudioServer</c> bus tables on the Godot side.
///         The Core layer never resolves them itself — it only routes intent ("play this on
///         the music bus") so the adapter can map each <see cref="AudioBus" /> to a real bus
///         and a settings key.
///     </para>
///     <para>
///         Adding a new bus is a deliberate design choice: every bus needs a settings entry
///         (<c>audio.volume.&lt;name&gt;</c>) and a Godot bus of the same name. Keep the list
///         small.
///     </para>
/// </remarks>
public enum AudioBus
{
    /// <summary>Master output. Scales every other bus.</summary>
    Master,

    /// <summary>Background music (single track at a time, crossfaded).</summary>
    Music,

    /// <summary>One-shot sound effects (footsteps, hits, spell casts).</summary>
    Sfx,

    /// <summary>Looping ambient layers (wind, crickets, dungeon hum).</summary>
    Ambient,

    /// <summary>UI clicks, menu transitions, notifications.</summary>
    Ui,

    /// <summary>Spoken dialogue / VO lines.</summary>
    Voice,
}
