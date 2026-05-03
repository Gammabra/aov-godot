using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.status_effects;

namespace AshesOfVelsingrad.systems.corruption;

/// <summary>
///     Long-lived status effect representing the unit's current corruption tier.
/// </summary>
/// <remarks>
///     <para>
///         Per <c>Feature Document § 4</c>, corruption ramps through 3 levels with
///         increasing penalties. We model it as a non-purifiable, permanent status
///         effect on the affected unit so the combat layer can read its current
///         level via <c>HasEffect&lt;CorruptionLevel1Effect&gt;()</c> and apply the
///         right modifiers.
///     </para>
///     <para>
///         The persistent corruption value lives on
///         <see cref="systems.progression.CharacterProfile.CorruptionLevel" />.
///         The status effect is just a runtime reflection of that value used to
///         ferry the right modifiers to combat formulas without spreading the
///         "if level == 1" branches everywhere.
///     </para>
/// </remarks>
public abstract class CorruptionLevelEffect : StatusEffect
{
    /// <inheritdoc />
    public override bool IsStackable => false;

    /// <inheritdoc />
    /// <remarks>
    ///     Cleanse skills cannot remove corruption — only the dedicated
    ///     "Purifying Elixir" item or the Light spell "Purifying Burst"
    ///     can reduce corruption (handled in <see cref="CorruptionSystem" />).
    /// </remarks>
    public override bool IsPurifiable => false;

    /// <summary>The corruption tier this effect represents.</summary>
    public abstract int Level { get; }

    /// <summary>
    ///     Default ctor: corruption never wears off on its own — it's only changed
    ///     by <see cref="CorruptionSystem" /> when points accumulate.
    /// </summary>
    protected CorruptionLevelEffect()
    {
        Duration = -1;
    }
}

/// <summary>
///     Level 1 corruption: -10% healing received, -1 range to all skills.
/// </summary>
public sealed class CorruptionLevel1Effect : CorruptionLevelEffect
{
    /// <inheritdoc />
    public override int Level => 1;

    /// <summary>Multiplier applied to incoming healing.</summary>
    public const float HealingMultiplier = 0.9f;

    /// <summary>Range penalty applied to every skill the unit casts.</summary>
    public const int RangePenalty = 1;

    /// <summary>Build the level-1 effect.</summary>
    public CorruptionLevel1Effect()
    {
        Name = "Corruption (lvl 1)";
        Description = "Healing received -10%. Skill range -1.";
    }
}

/// <summary>
///     Level 2 corruption: -15% speed and chance to attack a random ally on its turn.
/// </summary>
public sealed class CorruptionLevel2Effect : CorruptionLevelEffect
{
    /// <inheritdoc />
    public override int Level => 2;

    /// <summary>Multiplier applied to base speed while the effect is active.</summary>
    public const float SpeedMultiplier = 0.85f;

    /// <summary>
    ///     Probability that the affected unit attacks a random ally on its turn (in [0, 1]).
    /// </summary>
    public const float MisdirectChance = 0.30f;

    /// <summary>Build the level-2 effect.</summary>
    public CorruptionLevel2Effect()
    {
        Name = "Corruption (lvl 2)";
        Description = "Speed -15%. May attack an ally by mistake.";
    }
}

/// <summary>
///     Level 3 corruption: imminent transformation into an uncontrollable monster.
/// </summary>
/// <remarks>
///     The transformation itself is modelled by <see cref="CorruptedTransformationEffect" />
///     (a separate, time-limited effect that takes over the unit's faction). Level 3
///     <i>without</i> the transformation effect represents the brief window before
///     the unit goes berserk.
/// </remarks>
public sealed class CorruptionLevel3Effect : CorruptionLevelEffect
{
    /// <inheritdoc />
    public override int Level => 3;

    /// <summary>Build the level-3 effect.</summary>
    public CorruptionLevel3Effect()
    {
        Name = "Corruption (lvl 3)";
        Description = "On the brink: transforms into a monster for 2 turns when triggered.";
    }
}
