using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Corruption;

// =============================================================================
// Corruption tier marker effects.
// One concrete subclass per level (1/2/3). They live on the unit as long as the
// unit's CorruptionLevel matches their level — CorruptionSystem.SyncRuntimeMarker
// adds/removes them to keep them in sync. Game logic that wants to ask
// "is this unit corruption-1?" calls unit.HasEffect<CorruptionLevel1Effect>().
// =============================================================================

/// <summary>
///     Base class for the three corruption-tier marker effects. Permanent (duration -1) and
///     non-stackable; <see cref="CorruptionSystem" /> swaps them in/out as the tier changes.
/// </summary>
public abstract class CorruptionLevelEffect : StatusEffect<IUnitSystem>
{
    /// <summary>The tier this effect represents (1, 2, or 3).</summary>
    public abstract int Level { get; }

    protected CorruptionLevelEffect(string name, string description)
        : base(name, description, -1, false)
    {
    }
}

/// <summary>Level 1 corruption: -10% healing received, -1 range to all skills.</summary>
public sealed class CorruptionLevel1Effect : CorruptionLevelEffect
{
    /// <inheritdoc />
    public override int Level => 1;

    /// <summary>Multiplier applied to incoming healing (read by heal skills / GameManager).</summary>
    public const float HealingMultiplier = 0.9f;

    /// <summary>Range penalty applied to every skill the unit casts.</summary>
    public const int RangePenalty = 1;

    public CorruptionLevel1Effect()
        : base("Corruption (lvl 1)", "Healing received -10%. Skill range -1.")
    {
    }
}

/// <summary>Level 2 corruption: -15% speed, chance to attack a random ally on its turn.</summary>
public sealed class CorruptionLevel2Effect : CorruptionLevelEffect
{
    /// <inheritdoc />
    public override int Level => 2;

    /// <summary>Multiplier applied to base speed while the effect is active.</summary>
    public const float SpeedMultiplier = 0.85f;

    /// <summary>Probability the unit attacks a random ally on its turn (in [0, 1]).</summary>
    public const float MisdirectChance = 0.30f;

    public CorruptionLevel2Effect()
        : base("Corruption (lvl 2)", "Speed -15%. May attack an ally by mistake.")
    {
    }
}

/// <summary>Level 3 corruption: imminent transformation into an uncontrollable monster.</summary>
public sealed class CorruptionLevel3Effect : CorruptionLevelEffect
{
    /// <inheritdoc />
    public override int Level => 3;

    public CorruptionLevel3Effect()
        : base("Corruption (lvl 3)", "On the brink: transforms into a monster for 2 turns when triggered.")
    {
    }
}
