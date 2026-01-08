using AshesOfVelsingrad.utilities;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Base class for all status effects that can be applied to an <see cref="IEffectTarget{TTarget}"/>.
/// </summary>
/// <typeparam name="TTarget">
///     The type of target this status effect can be applied to,
///     such as <see cref="UnitSystem"/> or <see cref="MapSystem"/>.
/// </typeparam>
/// <remarks>
///     This class provides core properties and methods for a status effect,
///     including stacking behavior, duration management, and hooks for
///     when the effect is applied, removed, or updated each turn.
/// </remarks>
public abstract class StatusEffect<TTarget>
{
    /// <summary>
    ///     The display name of the effect.
    /// </summary>
    public string Name { get; protected set; } = string.Empty;

    /// <summary>
    ///     A description of what this effect does.
    /// </summary>
    public string Description { get; protected set; } = string.Empty;

    /// <summary>
    ///     The number of turns the effect will last.
    ///     A value of <c>-1</c> means the effect is permanent.
    /// </summary>
    public int Duration { get; protected set; }

    /// <summary>
    ///     The number of times this effect has been stacked.
    /// </summary>
    public int StackCount { get; protected set; } = 1;

    /// <summary>
    ///     Whether this effect can be stacked.
    /// </summary>
    public virtual bool IsStackable => false;

    /// <summary>
    ///     Called when this effect is applied to a target.
    ///     Override this to implement custom logic (e.g., visual feedback, stat changes).
    /// </summary>
    /// <param name="target">The target receiving the effect.</param>
    public virtual void OnApply(IEffectTarget<TTarget> target)
    {
    }

    /// <summary>
    ///     Called when this effect is removed from a target.
    ///     Override this to implement cleanup logic (e.g., removing buffs, stopping VFX).
    /// </summary>
    /// <param name="target">The target losing the effect.</param>
    public virtual void OnRemove(IEffectTarget<TTarget> target)
    {
    }

    /// <summary>
    ///     Called at the end of each turn to update the effect’s duration or apply ongoing logic.
    /// </summary>
    /// <param name="target">The target affected by this effect.</param>
    public virtual void OnTurnPassed(IEffectTarget<TTarget> target)
    {
        if (Duration == Constants.PermanentStatusEffect)
            return;
        Duration--;
    }

    /// <summary>
    ///     Increases the stack count of the effect if it is stackable.
    /// </summary>
    public virtual void AddStack()
    {
        if (IsStackable)
            StackCount++;
    }
}
