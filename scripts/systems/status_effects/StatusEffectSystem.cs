using System.Collections.Generic;
using System.Linq;
using AshesOfVelsingrad.utilities;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Manages the application and lifecycle of status effects on all tracked targets.
/// </summary>
public sealed class StatusEffectSystem
{
    /// <summary>
    ///     List of all targets currently having effects.
    /// </summary>
    private readonly List<IEffectTarget> _allTargets = [];

    /// <summary>
    ///     Registers a new effect on a target, or stacks it if already present.
    /// </summary>
    public void ApplyEffect(IEffectTarget target, StatusEffect newEffect)
    {
        // Add the target to the list if not already present
        if (!_allTargets.Contains(target))
            _allTargets.Add(target);

        // Check if the effect already exists
        StatusEffect? existing = target
            .GetActiveEffects()
            .FirstOrDefault(e => e.GetType() == newEffect.GetType());

        if (existing is not null && existing.IsStackable)
            existing.AddStack();
        else if (existing == null)
            target.ApplyEffect(newEffect);
    }

    /// <summary>
    ///     Processes end-of-turn updates for all tracked targets.
    /// </summary>
    public void ProcessTurnEnd()
    {
        foreach (IEffectTarget target in _allTargets.ToList()) // Make a copy in case list is modified
        {
            foreach (StatusEffect statusEffect in
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
