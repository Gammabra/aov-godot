using System.Collections.Generic;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Base class for any entity that can receive and manage <see cref="StatusEffect{T}" />s.
///     Provides default implementations for applying, removing, and querying effects.
/// </summary>
/// ///
/// <typeparam name="TTarget">The type of target this effect system is applied to (e.g., UnitSystem, CellInformation).</typeparam>
public class EffectTarget<TTarget> : IEffectTarget<TTarget>
{
    /// <summary>
    ///     Internal list storing all active status effects on this target.
    /// </summary>
    private readonly List<StatusEffect<TTarget>> _activeEffects = [];

    /// <inheritdoc />
    public virtual void ApplyEffect(StatusEffect<TTarget> statusEffect)
    {
        _activeEffects.Add(statusEffect);
    }

    /// <inheritdoc />
    public virtual void RemoveEffect(StatusEffect<TTarget> statusEffect)
    {
        _activeEffects.Remove(statusEffect);
    }

    /// <inheritdoc />
    public bool HasEffect<T>()
        where T : StatusEffect<TTarget>
    {
        foreach (StatusEffect<TTarget> effect in _activeEffects)
            if (effect is T)
                return true;
        return false;
    }

    /// <inheritdoc />
    public List<StatusEffect<TTarget>> GetActiveEffects()
    {
        return _activeEffects;
    }
}
