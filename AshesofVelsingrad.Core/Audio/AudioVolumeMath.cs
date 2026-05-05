using System;

namespace AshesOfVelsingrad.Audio;

/// <summary>
///     Pure-C# math helpers shared by the audio service for converting between the
///     0..1 linear scale stored in user settings and the decibel scale Godot's
///     <c>AudioServer</c> consumes.
/// </summary>
/// <remarks>
///     <para>
///         Lives in the Core assembly so the conversion is unit-tested without a Godot
///         runtime. The adapter calls into these helpers instead of hand-rolling the math
///         in two places.
///     </para>
///     <para>
///         Anything below <see cref="MinAudibleLinear" /> is treated as full silence and
///         clamped to <see cref="SilenceDb" />, which keeps <c>log10(0)</c> from blowing
///         up and matches the intuitive "slider all the way down = muted" behaviour.
///     </para>
/// </remarks>
public static class AudioVolumeMath
{
    /// <summary>Lower bound of the linear (settings) volume scale.</summary>
    public const float MinLinear = 0f;

    /// <summary>Upper bound of the linear (settings) volume scale.</summary>
    public const float MaxLinear = 1f;

    /// <summary>Linear values at or below this are treated as full silence.</summary>
    public const float MinAudibleLinear = 0.0001f;

    /// <summary>Decibel value used to represent silence on a Godot bus.</summary>
    public const float SilenceDb = -80f;

    /// <summary>
    ///     Clamps an arbitrary float into the supported linear range
    ///     [<see cref="MinLinear" />, <see cref="MaxLinear" />].
    /// </summary>
    /// <param name="linear">A linear volume, typically from a UI slider.</param>
    /// <returns>The input clamped to the legal range.</returns>
    public static float ClampLinear(float linear)
    {
        if (float.IsNaN(linear)) return MinLinear;
        return Math.Clamp(linear, MinLinear, MaxLinear);
    }

    /// <summary>
    ///     Converts a 0..1 linear volume into decibels suitable for
    ///     <c>AudioServer.SetBusVolumeDb</c>.
    /// </summary>
    /// <param name="linear">Linear volume; values outside [0,1] are clamped.</param>
    /// <returns>
    ///     Decibel value, or <see cref="SilenceDb" /> for inputs at or below
    ///     <see cref="MinAudibleLinear" />.
    /// </returns>
    public static float LinearToDb(float linear)
    {
        var clamped = ClampLinear(linear);
        if (clamped <= MinAudibleLinear) return SilenceDb;
        return (float)(20.0 * Math.Log10(clamped));
    }

    /// <summary>
    ///     Converts a decibel value into the 0..1 linear scale used by settings.
    /// </summary>
    /// <param name="db">Decibel value, typically from <c>AudioServer.GetBusVolumeDb</c>.</param>
    /// <returns>
    ///     Linear volume in [0,1]. Values at or below <see cref="SilenceDb" /> become
    ///     <see cref="MinLinear" />.
    /// </returns>
    public static float DbToLinear(float db)
    {
        if (float.IsNaN(db) || db <= SilenceDb) return MinLinear;
        var raw = (float)Math.Pow(10.0, db / 20.0);
        return ClampLinear(raw);
    }
}
