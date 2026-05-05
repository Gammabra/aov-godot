namespace AshesOfVelsingrad.Systems;

/// <summary>
///     <see cref="UnitSystem" /> partial that tracks per-unit corruption state.
/// </summary>
/// <remarks>
///     <para>
///         Corruption persists across battles (unlike most status effects which expire), so it
///         lives directly on the unit rather than as a transient <c>StatusEffect</c>. The
///         marker effects (<see cref="Data.Corruption.CorruptionLevel1Effect" /> etc.) are
///         transient runtime markers that <see cref="Data.Corruption.CorruptionSystem" /> swaps
///         in/out as <see cref="CorruptionLevel" /> changes.
///     </para>
/// </remarks>
public abstract partial class UnitSystem
{
    /// <summary>
    ///     Number of corruption points required to advance one corruption level.
    /// </summary>
    public const int CorruptionPointsPerLevel = 4;

    /// <summary>
    ///     Maximum corruption level. Per the feature document.
    /// </summary>
    public const int MaxCorruptionLevel = 3;

    /// <summary>Persistent corruption tier (0..3). 0 = clean, 3 = transformed.</summary>
    public int CorruptionLevel { get; internal set; }

    /// <summary>
    ///     Corruption-point counter within the current tier; advances the tier when it reaches
    ///     <see cref="CorruptionPointsPerLevel" />.
    /// </summary>
    public int CorruptionPoints { get; internal set; }
}
