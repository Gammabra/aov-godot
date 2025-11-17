using System.Collections.Generic;
using System.Linq;
using AshesOfVelsingrad.utilities;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Manages the lifecycle and application of <see cref="StatusEffect{TTarget}"/>s
///     for all tracked targets in the game, including units and map cells.
/// </summary>
/// <remarks>
///     This system keeps track of every target that has active status effects,
///     applies new effects, handles stacking, and processes end-of-turn updates.
///     Targets must implement <see cref="IEffectTarget{TTarget}"/> and be either
///     a <see cref="UnitSystem"/> or a <see cref="MapSystem"/>.
/// </remarks>
public sealed class StatusEffectSystem
{
    /// <summary>
    ///     List of all targets currently having effects.
    /// </summary>
    private readonly List<object> _allTargets = [];

    /// <summary>
    ///     Applies a new status effect to the given target, or stacks it if already present.
    /// </summary>
    /// <typeparam name="TTarget">
    ///     The type of the target, must be either <see cref="UnitSystem"/> or <see cref="MapSystem"/>.
    /// </typeparam>
    /// <param name="target">
    ///     The target on which to apply the status effect.
    /// </param>
    /// <param name="newEffect">
    ///     The <see cref="StatusEffect{TTarget}"/> to apply.
    /// </param>
    /// <remarks>
    ///     If the target already has an effect of the same type and it is stackable,
    ///     the effect will be stacked instead of being applied again.
    /// </remarks>
    public void ApplyEffect<TTarget>(IEffectTarget<TTarget> target, StatusEffect<TTarget> newEffect)
    {
        if (typeof(TTarget) != typeof(MapSystem) && typeof(TTarget) != typeof(UnitSystem))
            return;

        // Add the target to the list if not already present
        if (!_allTargets.Contains(target))
            _allTargets.Add(target);

        // Check if the effect already exists
        StatusEffect<TTarget>? existing = target
            .GetActiveEffects()
            .FirstOrDefault(e => e.GetType() == newEffect.GetType());

        if (existing is not null && existing.IsStackable)
            existing.AddStack();
        else if (existing == null)
            target.ApplyEffect(newEffect);
    }

    /// <summary>
    ///     Processes end-of-turn updates for a single target.
    /// </summary>
    /// <typeparam name="TTarget">
    ///     The type of the target to process.
    /// </typeparam>
    /// <param name="target">
    ///     The target whose status effects should be updated.
    /// </param>
    /// <remarks>
    ///     Each status effect has its <see cref="StatusEffect{TTarget}.OnTurnPassed"/> method called,
    ///     and expired effects are removed automatically. If a target has no more active effects,
    ///     it is removed from the tracking list.
    /// </remarks>
    public void ProcessTargetTurnEnd<TTarget>(IEffectTarget<TTarget> target)
    {
        foreach (StatusEffect<TTarget> statusEffect in
            target.GetActiveEffects().ToList()) // Copy to avoid foreach/remove issues
        {
            if (statusEffect.GetType() != typeof(UnitSystem))
                continue;
            if (statusEffect.Duration == Constants.PermanentStatusEffect)
                continue;
            statusEffect.OnTurnPassed(target);
            if (statusEffect.Duration > 0)
                continue;
            target.RemoveEffect(statusEffect);
        }

        if (target.GetActiveEffects().Count == 0)
            _allTargets.Remove(target);
    }

    /// <summary>
    ///     Processes end-of-turn updates for all tracked targets.
    /// </summary>
    /// <remarks>
    ///     Iterates over all tracked targets in <see cref="_allTargets"/> and updates
    ///     their status effects. Expired effects are removed, and targets with no remaining
    ///     effects are removed from the tracking list.
    ///     Only targets of type <see cref="MapSystem"/> are processed by this method.
    /// </remarks>
    public void ProcessTurnEnd()
    {
        foreach (IEffectTarget<object> target in _allTargets.ToList()) // Make a copy in case list is modified
        {
            if (target.GetType() != typeof(MapSystem))
                continue;
            foreach (StatusEffect<object> statusEffect in
                target.GetActiveEffects().ToList()) // Copy to avoid foreach/remove issues
            {
                if (statusEffect.Duration == Constants.PermanentStatusEffect)
                    continue;
                statusEffect.OnTurnPassed(target);
                if (statusEffect.Duration > 0)
                    continue;
                target.RemoveEffect(statusEffect);
            }

            if (target.GetActiveEffects().Count == 0)
                _allTargets.Remove(target);
        }
    }
}
