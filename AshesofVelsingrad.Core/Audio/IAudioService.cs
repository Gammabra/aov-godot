namespace AshesOfVelsingrad.Core.Audio;

/// <summary>
///     Contract for the central audio service.
/// </summary>
/// <remarks>
///     <para>
///         Defined in Core so gameplay code (skills, status effects, exploration
///         triggers) can request audio without taking a hard reference to Godot.
///         The adapter (<c>AudioManager</c>) implements this interface against
///         <c>AudioServer</c> and the Godot scene tree.
///     </para>
///     <para>
///         All track parameters are <c>res://</c> paths (or any path Godot's
///         <c>ResourceLoader</c> understands). Volume parameters use the 0..1
///         linear scale; the adapter converts to decibels via
///         <see cref="AudioVolumeMath" />.
///     </para>
/// </remarks>
public interface IAudioService
{
    /// <summary>Starts (or crossfades into) a music track.</summary>
    /// <param name="trackPath">Resource path of the music stream.</param>
    /// <param name="fadeSeconds">Crossfade duration with the currently playing track.</param>
    /// <param name="loop">Whether the stream should loop when it reaches the end.</param>
    void PlayMusic(string trackPath, float fadeSeconds = 1.5f, bool loop = true);

    /// <summary>Fades out and stops the current music track, if any.</summary>
    /// <param name="fadeSeconds">Fade-out duration. <c>0</c> stops immediately.</param>
    void StopMusic(float fadeSeconds = 1.0f);

    /// <summary>
    ///     Starts (or replaces) an ambient layer. Multiple layers can play at once,
    ///     each addressed by <paramref name="layerId" />.
    /// </summary>
    /// <param name="layerId">Stable identifier for the layer (e.g. <c>"wind"</c>, <c>"crickets"</c>).</param>
    /// <param name="trackPath">Resource path of the ambient stream.</param>
    /// <param name="fadeSeconds">Fade-in duration.</param>
    void PlayAmbient(string layerId, string trackPath, float fadeSeconds = 1.5f);

    /// <summary>Fades out and stops a single ambient layer.</summary>
    /// <param name="layerId">Identifier passed to <see cref="PlayAmbient" />.</param>
    /// <param name="fadeSeconds">Fade-out duration.</param>
    void StopAmbient(string layerId, float fadeSeconds = 1.0f);

    /// <summary>Fades out and stops every active ambient layer.</summary>
    /// <param name="fadeSeconds">Fade-out duration applied to each layer.</param>
    void StopAllAmbient(float fadeSeconds = 1.0f);

    /// <summary>Plays a one-shot sound effect through the SFX pool.</summary>
    /// <param name="trackPath">Resource path of the SFX stream.</param>
    /// <param name="volumeLinear">Per-shot volume scale on the 0..1 linear scale.</param>
    /// <param name="pitchScale">Pitch multiplier (1.0 = original pitch).</param>
    void PlaySfx(string trackPath, float volumeLinear = 1f, float pitchScale = 1f);

    /// <summary>Plays a one-shot UI sound through the UI bus.</summary>
    /// <param name="trackPath">Resource path of the UI stream.</param>
    /// <param name="volumeLinear">Per-shot volume scale on the 0..1 linear scale.</param>
    void PlayUi(string trackPath, float volumeLinear = 1f);

    /// <summary>Returns the current volume for a bus on the 0..1 linear scale.</summary>
    /// <param name="bus">Bus to query.</param>
    /// <returns>Linear volume (0 = silent, 1 = unity gain).</returns>
    float GetBusVolume(AudioBus bus);

    /// <summary>Sets the volume for a bus and persists it through the settings layer.</summary>
    /// <param name="bus">Bus to set.</param>
    /// <param name="linear">New linear volume; values outside [0,1] are clamped.</param>
    void SetBusVolume(AudioBus bus, float linear);

    /// <summary>Returns whether a bus is currently muted.</summary>
    /// <param name="bus">Bus to query.</param>
    bool IsBusMuted(AudioBus bus);

    /// <summary>Mutes or unmutes a bus.</summary>
    /// <param name="bus">Bus to toggle.</param>
    /// <param name="muted"><c>true</c> to mute, <c>false</c> to restore the persisted volume.</param>
    void SetBusMuted(AudioBus bus, bool muted);
}
