using AshesOfVelsingrad.Data.Corruption;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.status_effects;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.systems.items.behaviours;

/// <summary>
///     Restores HP equal to <see cref="ItemDefinition.Magnitude" /> on every target.
/// </summary>
/// <remarks>Backs Healing Potion, Greater Healing Potion, etc.</remarks>
public sealed class HealingItemBehaviour : IItemBehaviour
{
    /// <inheritdoc />
    public void Apply(in ItemUseContext context)
    {
        foreach (UnitSystem target in context.Targets)
            target.Heal(context.Definition.Magnitude);
    }
}

/// <summary>
///     Restores mana equal to <see cref="ItemDefinition.Magnitude" /> on every target.
/// </summary>
public sealed class ManaItemBehaviour : IItemBehaviour
{
    /// <inheritdoc />
    public void Apply(in ItemUseContext context)
    {
        foreach (UnitSystem target in context.Targets)
            target.RestoreMana(context.Definition.Magnitude);
    }
}

/// <summary>
///     Removes every purifiable status effect from the target.
/// </summary>
/// <remarks>Backs the Antidote item (single-effect cleanse) and a generic "Healer's Salve" (full cleanse).</remarks>
public sealed class CleanseItemBehaviour : IItemBehaviour
{
    /// <inheritdoc />
    public void Apply(in ItemUseContext context)
    {
        foreach (UnitSystem target in context.Targets)
        {
            foreach (StatusEffect<IUnitSystem> effect in target.GetActiveEffects().ToArray())
            {
                if (!effect.IsPurifiable) continue;
                target.RemoveEffect(effect);
                BattleEventBus.Instance?.Publish(new BattleEvents.StatusEffectRemoved(target, effect.Name));
            }
        }
    }
}

/// <summary>
///     Reduces the target's corruption by one level (down to 0).
/// </summary>
/// <remarks>
///     Backs the user-requested "Cure Item" — a Purifying Elixir. Use sparingly: rare and expensive.
///     Also clears purifiable status effects as a side-bonus.
/// </remarks>
public sealed class PurifyingElixirBehaviour : IItemBehaviour
{
    /// <inheritdoc />
    public void Apply(in ItemUseContext context)
    {
        foreach (UnitSystem target in context.Targets)
        {
            CorruptionSystem.Cleanse(target);

            // Bonus: also strip purifiable statuses.
            foreach (StatusEffect<IUnitSystem> effect in target.GetActiveEffects().ToArray())
            {
                if (!effect.IsPurifiable) continue;
                target.RemoveEffect(effect);
                BattleEventBus.Instance?.Publish(new BattleEvents.StatusEffectRemoved(target, effect.Name));
            }
        }
    }
}

/// <summary>
///     Deals <see cref="ItemDefinition.Magnitude" /> raw damage to every target.
/// </summary>
/// <remarks>Backs offensive items like Smoke Bomb or Alchemist's Fire.</remarks>
public sealed class DamageItemBehaviour : IItemBehaviour
{
    /// <inheritdoc />
    public void Apply(in ItemUseContext context)
    {
        foreach (UnitSystem target in context.Targets)
            target.TakeDamage(context.Definition.Magnitude);
    }
}

/// <summary>
///     Revives the target with a percentage of MaxHp encoded in <see cref="ItemDefinition.Magnitude" />.
/// </summary>
/// <remarks>Backs the Phoenix Down-style item. Magnitude is a fraction (0.5 = 50%).</remarks>
public sealed class ReviveItemBehaviour : IItemBehaviour
{
    /// <inheritdoc />
    public void Apply(in ItemUseContext context)
    {
        float ratio = Godot.Mathf.Clamp(context.Definition.Magnitude, 0.05f, 1.0f);
        foreach (UnitSystem target in context.Targets)
            target.Revive(target.MaxHp * ratio);
    }
}
