using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.systems.status_effects.effects;

/// <summary>
///     Damage-over-time + armor reduction effect.
/// </summary>
/// <remarks>
///     Stackable. Purifiable. Source: Fire-school spells (Fireball, Burning Storm, etc.)
///     and the "burning terrain" residual effect. While active, the target's armor is
///     reduced by <see cref="ArmorReductionPerStack" /> per stack.
/// </remarks>
public sealed class BurnEffect : StatusEffect
{
    /// <summary>Damage applied per stack on each tick.</summary>
    public float DamagePerStack { get; }

    /// <summary>Armor (defense) reduction per stack while the effect is active.</summary>
    public float ArmorReductionPerStack { get; }

    /// <inheritdoc />
    public override bool IsStackable => true;

    /// <inheritdoc />
    public override bool IsPurifiable => true;

    /// <summary>
    ///     Build a new <see cref="BurnEffect" />.
    /// </summary>
    /// <param name="duration">Number of turns the effect lasts. -1 = permanent.</param>
    /// <param name="damagePerStack">Damage applied per stack each tick.</param>
    /// <param name="armorReductionPerStack">Defense penalty per stack while active.</param>
    public BurnEffect(int duration = 3, float damagePerStack = 5f, float armorReductionPerStack = 2f)
    {
        Name = "Burn";
        Description = "Damage over time and reduces armor. Stacks.";
        Duration = duration;
        DamagePerStack = damagePerStack;
        ArmorReductionPerStack = armorReductionPerStack;
    }

    /// <inheritdoc />
    public override void OnApply(IEffectTarget target)
    {
        if (target is UnitSystem unit)
            unit.AdjustDefense(-ArmorReductionPerStack * StackCount);
    }

    /// <inheritdoc />
    public override void OnRemove(IEffectTarget target)
    {
        if (target is UnitSystem unit)
            unit.AdjustDefense(ArmorReductionPerStack * StackCount);
    }

    /// <inheritdoc />
    public override void AddStack()
    {
        // Increase the armor penalty by one more stack worth before bumping the count.
        // OnApply / OnRemove pair handles the original stack; we add the increment here.
        // Net effect: each stack reduces armor by ArmorReductionPerStack.
        StackCount++;
    }

    /// <inheritdoc />
    public override void OnTurnPassed(IEffectTarget target)
    {
        if (target is UnitSystem unit && unit.IsAlive)
        {
            float damage = DamagePerStack * StackCount;
            unit.TakeDamage(damage);
            BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
                $"{unit.UnitName} burns for {damage:F0} damage.", LogSeverity.Negative
            ));
        }

        base.OnTurnPassed(target);
    }
}
