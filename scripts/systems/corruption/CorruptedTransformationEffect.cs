using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.status_effects;

namespace AshesOfVelsingrad.systems.corruption;

/// <summary>
///     Time-limited effect that turns the bearer into an uncontrollable monster.
/// </summary>
/// <remarks>
///     <para>
///         Triggered by reaching corruption level 3 (or by the dedicated
///         <c>Corrupted Conversion</c> dark spell, which uses the same effect with
///         a 3-turn duration). While active:
///         <list type="bullet">
///             <item>The unit's <see cref="UnitSystem.Faction" /> is overridden to <see cref="Faction.Enemy" />.</item>
///             <item>The unit is AI-controlled.</item>
///             <item>The unit attacks the nearest target regardless of original faction.</item>
///         </list>
///     </para>
///     <para>
///         When the effect expires, <see cref="CorruptionSystem" /> restores the
///         <see cref="OriginalFaction" /> on the unit (still corrupt — its
///         <see cref="systems.progression.CharacterProfile.CorruptionLevel" /> stays
///         at the level that caused the transformation).
///     </para>
/// </remarks>
public sealed class CorruptedTransformationEffect : StatusEffect
{
    /// <summary>The faction the unit had before transformation, restored on expiration.</summary>
    public Faction OriginalFaction { get; }

    /// <inheritdoc />
    public override bool IsStackable => false;

    /// <inheritdoc />
    public override bool IsPurifiable => false;

    /// <summary>
    ///     Build a new transformation effect.
    /// </summary>
    /// <param name="originalFaction">Faction to restore when the effect expires.</param>
    /// <param name="duration">Number of turns to remain transformed. Defaults to 3.</param>
    public CorruptedTransformationEffect(Faction originalFaction, int duration = 3)
    {
        Name = "Corrupted Transformation";
        Description = "Berserk: faction overridden, attacks nearest target.";
        Duration = duration;
        OriginalFaction = originalFaction;
    }

    /// <inheritdoc />
    public override void OnApply(IEffectTarget target)
    {
        if (target is UnitSystem unit)
            CorruptionSystem.OnTransformationStart(unit);
    }

    /// <inheritdoc />
    public override void OnRemove(IEffectTarget target)
    {
        if (target is UnitSystem unit)
            CorruptionSystem.OnTransformationEnd(unit, OriginalFaction);
    }
}
