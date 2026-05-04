using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.systems.battle;

namespace AshesOfVelsingrad.systems.status_effects.effects;

/// <summary>
///     Damage-over-time effect that ticks at the end of each affected unit's turn.
/// </summary>
/// <remarks>
///     Stackable: each additional poison stack adds <see cref="DamagePerStack" /> to the tick.
///     Purifiable. Source: Assassin's Toxines Mortelles, Archer's Flèche Empoisonnée.
/// </remarks>
public sealed class PoisonEffect : StatusEffect
{
    /// <summary>Damage applied per stack on each tick.</summary>
    public float DamagePerStack { get; }

    /// <inheritdoc />
    public override bool IsStackable => true;

    /// <inheritdoc />
    public override bool IsPurifiable => true;

    /// <summary>
    ///     Build a new <see cref="PoisonEffect" />.
    /// </summary>
    /// <param name="duration">Number of turns the effect lasts. -1 = permanent.</param>
    /// <param name="damagePerStack">Damage applied per stack each tick. Defaults to 4.</param>
    public PoisonEffect(int duration = 3, float damagePerStack = 4f)
    {
        Name = "Poison";
        Description = "Damage over time at the end of each turn. Stacks.";
        Duration = duration;
        DamagePerStack = damagePerStack;
    }

    /// <inheritdoc />
    public override void OnTurnPassed(IEffectTarget target)
    {
        if (target is UnitSystem unit && unit.IsAlive)
        {
            float damage = DamagePerStack * StackCount;
            unit.TakeDamage(damage);
            BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
                $"{unit.UnitName} suffers {damage:F0} poison damage.", LogSeverity.Negative
            ));
        }

        base.OnTurnPassed(target);
    }
}
