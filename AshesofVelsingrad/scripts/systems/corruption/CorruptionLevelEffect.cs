using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Corruption;

/// <summary>
///     Time-limited effect that turns its bearer into an uncontrollable monster.
/// </summary>
/// <remarks>
///     <para>
///         Triggered by reaching corruption level 3, or by the dedicated
///         <c>Conversion Corrompue</c> dark spell with a 3-turn duration. While active:
///     </para>
///     <list type="bullet">
///         <item><description>The bearer's <see cref="UnitSystem.Faction" /> is overridden to
///             <see cref="Faction.Enemy" />.</description></item>
///         <item><description>The unit is AI-controlled and attacks the nearest target
///             regardless of its original allegiance.</description></item>
///     </list>
///     <para>
///         When the effect expires, <see cref="CorruptionSystem.OnTransformationEnd" /> restores
///         <see cref="OriginalFaction" />. The unit's persistent corruption tier stays at
///         the level that caused the transformation.
///     </para>
/// </remarks>
public sealed class CorruptedTransformationEffect : StatusEffect<IUnitSystem>
{
    /// <summary>The faction the unit had before transformation, restored on expiration.</summary>
    public Faction OriginalFaction { get; }

    /// <summary>Build a new transformation effect.</summary>
    /// <param name="originalFaction">Faction to restore when the effect expires.</param>
    /// <param name="duration">Number of turns the unit stays transformed.</param>
    public CorruptedTransformationEffect(Faction originalFaction, int duration = 3)
        : base("Corrupted Transformation", "Berserk: faction overridden, attacks nearest target.", duration, false)
    {
        OriginalFaction = originalFaction;
    }

    /// <inheritdoc />
    public override void OnApply(IUnitSystem target)
    {
        CorruptionSystem.OnTransformationStart(target);
        base.OnApply(target);
    }

    /// <inheritdoc />
    public override void OnRemove(IUnitSystem target)
    {
        CorruptionSystem.OnTransformationEnd(target, OriginalFaction);
        base.OnRemove(target);
    }
}
