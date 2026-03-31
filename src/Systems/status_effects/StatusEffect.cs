using System;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Base class for all status effects that can be applied to an <see cref="IEffectTarget{TTarget}" />.
/// </summary>
/// <typeparam name="TTarget">
///     The type of target this status effect can be applied to,
///     such as <see cref="IUnitSystem" /> or <see cref="IMapSystem" />.
/// </typeparam>
/// <remarks>
///     This class provides core properties and methods for a status effect,
///     including stacking behavior, duration management, and hooks for
///     when the effect is applied, removed, or updated each turn.
/// </remarks>
public abstract class StatusEffect<TTarget>(
    string name,
    string description,
    int duration,
    bool isStackable,
    AovDataStructures.ModifierType modifierType = default,
    float amount = 0
) : IStatusEffect
{
    /// <summary>
    ///     The display name of the effect.
    /// </summary>
    public string Name { get; protected set; } = name;

    /// <summary>
    ///     A description of what this effect does.
    /// </summary>
    public string Description { get; protected set; } = description;

    /// <summary>
    ///     The number of turns the effect will last.
    ///     A value of <c>-1</c> means the effect is permanent.
    /// </summary>
    public int Duration { get; protected set; } = duration;

    /// <summary>
    ///     The number of times this effect has been stacked.
    /// </summary>
    public int StackCount { get; private set; } = 1;

    /// <summary>
    /// The modifier type (flat/percent)
    /// </summary>
    public AovDataStructures.ModifierType ModifierType { get; private set; } = modifierType;

    /// <summary>
    ///     The amount to apply
    /// </summary>
    public float Amount { get; private set; } = amount;

    /// <summary>
    ///     Whether this effect can be stacked.
    /// </summary>
    public bool IsStackable { get; } = isStackable;

    /// <summary>
    ///     Whether this effect should be applied twice on initial application.
    ///     Used for control effects like stuns that need immediate double application.
    /// </summary>
    public virtual bool ShouldApplyTwice => false;

    /// <summary>
    ///     Store a effect status that can be spread
    /// </summary>
    public StatusEffect<TTarget>? EffectToSpread { get; protected init; }


    /// <summary>
    ///     Called when this effect is applied to a target.
    ///     Override this to implement custom logic (e.g., visual/audio feedback, stat changes).
    /// </summary>
    /// <param name="target">The target receiving the effect.</param>
    public virtual void OnApply(TTarget target)
    {
        Console.WriteLine($"Applying {Name} on {target}");
    }

    /// <summary>
    ///     Called when this effect is removed from a target.
    ///     Override this to implement cleanup logic (e.g., removing buffs, stopping visual/audio feedback).
    /// </summary>
    /// <param name="target">The target losing the effect.</param>
    public virtual void OnRemove(TTarget target)
    {
        Console.WriteLine($"Remove {Name} on {target}");
    }

    /// <summary>
    ///     Called at the end of each turn to update the effect’s duration or apply ongoing logic.
    /// </summary>
    /// <param name="target">The target affected by this effect.</param>
    public virtual void OnTurnPassed(TTarget target)
    {
        if (Duration == Constants.PermanentStatusEffect)
            return;
        Duration--;
        if (target is IUnitSystem unit)
            Console.WriteLine($"Duration of {Name} on {unit.UnitName} is {Duration}");
        else if (target is CellInformation cell)
            Console.WriteLine($"Duration on cell ({cell.X}, {cell.Y}, {cell.Z}) is {Duration}");
    }

    /// <summary>
    ///     Increases the stack count of the effect if it is stackable.
    /// </summary>
    public virtual void AddStack()
    {
        if (IsStackable)
            StackCount++;
    }

    public virtual void ResetDuration(int duration)
    {
        if (Duration == Constants.PermanentStatusEffect)
            return;
        if (Duration < duration)
            Duration = duration;
    }
}
