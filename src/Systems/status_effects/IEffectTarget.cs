using System.Collections.Generic;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Represents any entity that can receive and manage <see cref="StatusEffect{TTarget}" />s.
/// </summary>
/// <typeparam name="TTarget">
///     The concrete type of the target, such as <see cref="UnitSystem" /> or <see cref="MapSystem" />.
/// </typeparam>
/// <remarks>
///     Implementing this interface allows an object to have status effects applied,
///     removed, queried, and iterated over. It is used by <see cref="StatusEffectSystem" />
///     to manage effects for multiple types of targets in a generic way.
/// </remarks>
public interface IEffectTarget<TTarget>
{
    /// <summary>
    ///     Applies a status effect to this target.
    /// </summary>
    /// <param name="statusEffect">
    ///     The <see cref="StatusEffect{TTarget}" /> instance to apply.
    /// </param>
    void ApplyEffect(StatusEffect<TTarget> statusEffect);

    /// <summary>
    ///     Removes a status effect from this target.
    /// </summary>
    /// <param name="statusEffect">
    ///     The <see cref="StatusEffect{TTarget}" /> instance to remove.
    /// </param>
    void RemoveEffect(StatusEffect<TTarget> statusEffect);

    /// <summary>
    ///     Checks whether this target currently has an active status effect of the specified type.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of <see cref="StatusEffect{TTarget}" /> to check for.
    /// </typeparam>
    /// <returns>
    ///     <c>true</c> if the effect is present on this target; otherwise, <c>false</c>.
    /// </returns>
    bool HasEffect<T>()
        where T : StatusEffect<TTarget>;

    /// <summary>
    ///     Retrieves all status effects currently active on this target.
    /// </summary>
    /// <returns>
    ///     A list of <see cref="StatusEffect{TTarget}" /> representing all active effects.
    /// </returns>
    List<StatusEffect<TTarget>> GetActiveEffects();
}
