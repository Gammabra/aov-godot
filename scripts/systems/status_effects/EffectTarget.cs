using System.Collections.Generic;

namespace AshesOfVelsingrad.systems.status_effects;

/// <summary>
///     Base class for any entity that can receive and manage <see cref="StatusEffect" />s.
///     Provides default implementations for applying, removing, and querying effects.
/// </summary>
public class EffectTarget : IEffectTarget
{
    /// <summary>
    ///     Internal list storing all active status effects on this target.
    /// </summary>
    private readonly List<StatusEffect> _activeEffects = [];

    /// <inheritdoc />
    public virtual void ApplyEffect(StatusEffect statusEffect)
    {
        _activeEffects.Add(statusEffect);
        statusEffect.OnApply(this);
    }

    /// <inheritdoc />
    public virtual void RemoveEffect(StatusEffect statusEffect)
    {
        _activeEffects.Remove(statusEffect);
        statusEffect.OnRemove(this);
    }

    /// <inheritdoc />
    public bool HasEffect<T>()
        where T : StatusEffect
    {
        foreach (StatusEffect effect in _activeEffects)
            if (effect is T)
                return true;
        return false;
    }

    /// <inheritdoc />
    public List<StatusEffect> GetActiveEffects()
    {
        return _activeEffects;
    }
}
