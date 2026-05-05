namespace AshesOfVelsingrad.Audio;

/// <summary>
///     Static metadata describing one playable audio asset.
/// </summary>
/// <remarks>
///     <para>
///         Tracks are registered up-front in <see cref="AudioCatalog" /> and looked up
///         by <see cref="Id" /> at play time, so call sites stay decoupled from
///         <c>res://</c> paths.
///     </para>
///     <para>
///         <see cref="BaseVolumeMultiplier" /> is a per-track linear multiplier baked
///         into the asset's metadata — useful when one track was mastered louder than
///         the rest. It stacks on top of the bus volume from settings, so
///         <c>0.5</c> means "always play this song at half volume relative to its bus".
///     </para>
/// </remarks>
/// <param name="Id">
///     Stable, human-readable identifier (e.g. <c>"music.main_menu"</c>). Must be
///     unique across the registry; reserved as a string constant on
///     <see cref="AudioCatalog" />.
/// </param>
/// <param name="ResourcePath">
///     Godot resource path (<c>res://...</c>) pointing at the audio asset.
/// </param>
/// <param name="Bus">
///     Logical bus this track plays on. Decides which dispatch path
///     <see cref="IAudioService.Play" /> takes (music crossfade, ambient layer,
///     SFX/UI/Voice one-shot).
/// </param>
/// <param name="BaseVolumeMultiplier">
///     Per-track linear multiplier in <c>[0, 1]</c>. <c>1.0</c> = unmodified,
///     <c>0.5</c> = -6 dB attenuation on top of the bus.
/// </param>
/// <param name="Loop">
///     Whether the track should loop when it finishes. Music and Ambient default
///     to <c>true</c>; one-shots should set this to <c>false</c>.
/// </param>
public sealed record AudioTrack(
    string Id,
    string ResourcePath,
    AudioBus Bus,
    float BaseVolumeMultiplier = 1f,
    bool Loop = true);
