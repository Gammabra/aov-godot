using AshesOfVelsingrad.utilities;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Base class for all status effects that can be applied to an <see cref="IEffectTarget" />.
/// </summary>
/// <remarks>
///     This class provides the basic data and behavior for a status effect.
/// </remarks>
public abstract class StatusEffect
{
    /// <summary>
    ///     The display name of the effect.
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    ///     A description of what this effect does.
    /// </summary>
    public string Description { get; protected set; }

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
    public virtual void OnApply(IEffectTarget target)
    {
    }

    /// <summary>
    ///     Called when this effect is removed from a target.
    ///     Override this to implement cleanup logic (e.g., removing buffs, stopping VFX).
    /// </summary>
    /// <param name="target">The target losing the effect.</param>
    public virtual void OnRemove(IEffectTarget target)
    {
    }

    /// <summary>
    ///     Called at the end of each turn to update the effect’s duration or apply ongoing logic.
    /// </summary>
    /// <param name="target">The target affected by this effect.</param>
    public virtual void OnTurnPassed(IEffectTarget target)
    {
        if (Duration == Constants.PermanentStatusEffect)
            return;
        Duration--;
    }

    public virtual void AddStack()
    {
        if (IsStackable) StackCount++;
    }
}
