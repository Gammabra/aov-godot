using System;
using AshesOfVelsingrad.Data.Corruption;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.status_effects;
using Faction = AshesOfVelsingrad.Systems.Faction;

namespace AshesOfVelsingrad.systems.skills.behaviours;

/// <summary>
///     Common helpers shared by every behaviour.
/// </summary>
internal static class BehaviourHelpers
{
    /// <summary>
    ///     Compute the skill's effective base value, factoring in caster stats.
    /// </summary>
    /// <param name="ctx">Execution context.</param>
    /// <returns>Final scaled value (damage, heal, etc.).</returns>
    public static float ScaledPower(in SkillExecutionContext ctx)
    {
        SkillDefinition def = ctx.Definition;
        float stat = def.Scaling switch
        {
            ScalingStat.Attack => ctx.Caster.BaseAtk,
            ScalingStat.Intelligence => ctx.Caster.Intelligence,
            _ => 0f
        };
        return def.BasePower + stat * def.ScalingFactor;
    }

    /// <summary>
    ///     Roll the status-effect side-application configured on the definition.
    /// </summary>
    /// <param name="ctx">Execution context.</param>
    /// <param name="target">Unit that just got hit.</param>
    public static void TryApplyOnHitEffect(in SkillExecutionContext ctx, UnitSystem target)
    {
        SkillDefinition def = ctx.Definition;
        if (string.IsNullOrEmpty(def.StatusEffectIdOnHit))
            return;

        if (GD.RandRange(0.0, 1.0) > def.StatusEffectChance)
            return;

        StatusEffect? effect = StatusEffectFactory.Create(def.StatusEffectIdOnHit, def.StatusEffectDuration);
        if (effect is null)
            return;

        target.ApplyEffect(effect);
        BattleEventBus.Instance?.Publish(
            new BattleEvents.StatusEffectApplied(target, effect.Name, effect.Duration)
        );
    }

    /// <summary>
    ///     Run the corruption-backlash roll if the skill is a corruption source.
    /// </summary>
    /// <param name="ctx">Execution context.</param>
    public static void RollCorruptionBacklash(in SkillExecutionContext ctx)
    {
        if (!ctx.Definition.IsCorruptionSource)
            return;
        CorruptionSystem.RollBacklash(ctx.Caster, ctx.Definition.BaseCorruptionChance);
    }

    /// <summary>Re-export Godot RNG for cleaner imports inside this file.</summary>
    public static class GD
    {
        /// <summary>Random double in <c>[from, to]</c>.</summary>
        /// <param name="from">Lower bound (inclusive).</param>
        /// <param name="to">Upper bound (inclusive).</param>
        /// <returns>Random double.</returns>
        public static double RandRange(double from, double to) => global::Godot.GD.RandRange(from, to);
    }
}

/// <summary>
///     Generic damage behaviour: deals scaled damage to every target, optionally
///     applies a status effect, optionally rolls corruption backlash.
/// </summary>
public sealed class DamageBehaviour : ISkillBehaviour
{
    /// <inheritdoc />
    public void Execute(in SkillExecutionContext context)
    {
        float power = BehaviourHelpers.ScaledPower(context);
        foreach (UnitSystem target in context.Targets)
        {
            target.TakeDamage(power);
            BehaviourHelpers.TryApplyOnHitEffect(context, target);
        }
        BehaviourHelpers.RollCorruptionBacklash(context);
    }
}

/// <summary>
///     Heal behaviour: restores HP to every target by the scaled power.
/// </summary>
public sealed class HealBehaviour : ISkillBehaviour
{
    /// <inheritdoc />
    public void Execute(in SkillExecutionContext context)
    {
        float power = BehaviourHelpers.ScaledPower(context);
        foreach (UnitSystem target in context.Targets)
            target.Heal(power);
    }
}

/// <summary>
///     Buff / debuff behaviour: applies a status effect to each target without
///     dealing damage. Useful for War Cry, Granite Skin, Hawk's Eye, etc.
/// </summary>
public sealed class StatusOnlyBehaviour : ISkillBehaviour
{
    /// <inheritdoc />
    public void Execute(in SkillExecutionContext context)
    {
        foreach (UnitSystem target in context.Targets)
            BehaviourHelpers.TryApplyOnHitEffect(context, target);
        BehaviourHelpers.RollCorruptionBacklash(context);
    }
}

/// <summary>
///     Damage-over-time behaviour: ticks initial damage then applies a DoT effect.
/// </summary>
public sealed class DamageOverTimeBehaviour : ISkillBehaviour
{
    /// <inheritdoc />
    public void Execute(in SkillExecutionContext context)
    {
        float initial = BehaviourHelpers.ScaledPower(context) * 0.5f;
        foreach (UnitSystem target in context.Targets)
        {
            target.TakeDamage(initial);
            BehaviourHelpers.TryApplyOnHitEffect(context, target);
        }
        BehaviourHelpers.RollCorruptionBacklash(context);
    }
}

/// <summary>
///     Control behaviour: applies a stun / freeze / immobilize effect with no damage,
///     or low damage plus the control effect.
/// </summary>
public sealed class ControlBehaviour : ISkillBehaviour
{
    /// <inheritdoc />
    public void Execute(in SkillExecutionContext context)
    {
        float light = BehaviourHelpers.ScaledPower(context) * 0.3f;
        foreach (UnitSystem target in context.Targets)
        {
            if (light > 0)
                target.TakeDamage(light);
            BehaviourHelpers.TryApplyOnHitEffect(context, target);
        }
    }
}

/// <summary>
///     Resurrection behaviour: revives each target unit with a percentage of MaxHp.
///     The percentage is encoded in <see cref="SkillDefinition.BasePower" /> as a
///     value in <c>[0, 1]</c> (0.5 = 50% HP).
/// </summary>
public sealed class ResurrectBehaviour : ISkillBehaviour
{
    /// <inheritdoc />
    public void Execute(in SkillExecutionContext context)
    {
        float ratio = Math.Clamp(context.Definition.BasePower, 0.05f, 1.0f);
        foreach (UnitSystem target in context.Targets)
            target.Revive(target.MaxHp * ratio);
    }
}

/// <summary>
///     Cleanse behaviour: removes purifiable status effects from each target.
///     Used by Light spells like "Éclat Purificateur".
/// </summary>
public sealed class CleanseBehaviour : ISkillBehaviour
{
    /// <inheritdoc />
    public void Execute(in SkillExecutionContext context)
    {
        foreach (UnitSystem target in context.Targets)
        {
            // Iterate over a copy because RemoveEffect mutates the underlying list.
            foreach (StatusEffect<IUnitSystem> effect in target.GetActiveEffects().ToArray())
            {
                if (effect.IsPurifiable)
                {
                    target.RemoveEffect(effect);
                    BattleEventBus.Instance?.Publish(
                        new BattleEvents.StatusEffectRemoved(target, effect.Name)
                    );
                }
            }
        }
    }
}

/// <summary>
///     Corrupted-conversion behaviour: turns target ally into an enemy for a
///     fixed duration. The hallmark Darkness-school crowd-control spell that
///     also corrupts the caster strongly.
/// </summary>
/// <remarks>
///     Implements the user-requested mechanic: "a corruption spell that can turn
///     a member of your party into an enemy for 3 turns." Duration is
///     <see cref="SkillDefinition.StatusEffectDuration" /> (default 3).
/// </remarks>
public sealed class CorruptedConversionBehaviour : ISkillBehaviour
{
    /// <inheritdoc />
    public void Execute(in SkillExecutionContext context)
    {
        int duration = context.Definition.StatusEffectDuration > 0
            ? context.Definition.StatusEffectDuration
            : 3;

        foreach (UnitSystem target in context.Targets)
        {
            // Only friendlies of the caster can be converted.
            if (!target.Faction.IsFriendlyTo(context.Caster.Faction))
                continue;

            CorruptionSystem.ApplyTemporaryConversion(target, Faction.Enemy, duration);
        }

        BehaviourHelpers.RollCorruptionBacklash(context);
    }
}
