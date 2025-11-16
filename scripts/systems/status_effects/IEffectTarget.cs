using System.Collections.Generic;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Represents an entity that can receive and manage status effects.
/// </summary>
public interface IEffectTarget
{
    /// <summary>
    ///     Applies a status effect to this target.
    /// </summary>
    /// <param name="statusEffect">The status effect to apply.</param>
    void ApplyEffect(StatusEffect statusEffect);

    /// <summary>
    ///     Removes an active status effect from this target.
    /// </summary>
    /// <param name="statusEffect">The status effect to remove.</param>
    void RemoveEffect(StatusEffect statusEffect);

    /// <summary>
    ///     Checks whether this target currently has an active status effect of the given type.
    /// </summary>
    /// <typeparam name="T">The type of the status effect to check for.</typeparam>
    /// <returns><c>true</c> if the effect is present, otherwise <c>false</c>.</returns>
    bool HasEffect<T>()
        where T : StatusEffect;

    /// <summary>
    ///     Retrieves all status effects that are currently active on this target.
    /// </summary>
    /// <returns>A list containing all active status effects.</returns>
    List<StatusEffect> GetActiveEffects();
}
