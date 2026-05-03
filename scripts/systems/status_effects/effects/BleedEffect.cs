using AshesOfVelsingrad.systems.battle;

namespace AshesOfVelsingrad.systems.status_effects.effects;

/// <summary>
///     Damage-over-time effect that prevents healing while active.
/// </summary>
/// <remarks>
///     Stackable. Purifiable. The target's <see cref="UnitSystem.Heal" /> calls become
///     no-ops for the duration of the bleed (see <c>UnitSystem.HasEffect&lt;BleedEffect&gt;</c>
///     check inside <c>Heal</c>).
/// </remarks>
public sealed class BleedEffect : StatusEffect
{
    /// <summary>Damage applied per stack on each tick.</summary>
    public float DamagePerStack { get; }

    /// <inheritdoc />
    public override bool IsStackable => true;

    /// <inheritdoc />
    public override bool IsPurifiable => true;

    /// <summary>
    ///     Build a new <see cref="BleedEffect" />.
    /// </summary>
    /// <param name="duration">Number of turns the effect lasts. -1 = permanent.</param>
    /// <param name="damagePerStack">Damage applied per stack each tick.</param>
    public BleedEffect(int duration = 3, float damagePerStack = 6f)
    {
        Name = "Bleed";
        Description = "Damage over time. Prevents healing. Stacks.";
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
                $"{unit.UnitName} bleeds for {damage:F0} damage.", LogSeverity.Negative
            ));
        }

        base.OnTurnPassed(target);
    }
}
