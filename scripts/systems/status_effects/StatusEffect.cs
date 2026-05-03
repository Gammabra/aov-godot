using AshesOfVelsingrad.utilities;

namespace AshesOfVelsingrad.systems.status_effects;

/// <summary>
///     Base class for all status effects that can be applied to an <see cref="IEffectTarget" />.
/// </summary>
/// <remarks>
///     <para>
///         A status effect is a transient piece of state that lives on a target for a fixed
///         number of turns (or permanently). It can react to its lifecycle through
///         <see cref="OnApply" /> / <see cref="OnRemove" /> / <see cref="OnTurnPassed" />.
///     </para>
///     <para>
///         The two flag properties <see cref="IsStackable" /> and <see cref="IsPurifiable" />
///         expose the metadata required by the design's status table (see
///         <c>Feature Document § 5</c>). The <see cref="StatusEffectSystem" /> uses
///         <see cref="IsStackable" /> to decide whether a duplicate application stacks or
///         is ignored; cleansing skills (e.g. "Éclat Purificateur") check
///         <see cref="IsPurifiable" /> before removing.
///     </para>
/// </remarks>
public abstract class StatusEffect
{
    /// <summary>
    ///     The display name of the effect.
    /// </summary>
    public string Name { get; protected set; } = string.Empty;

    /// <summary>
    ///     A description of what this effect does. Used by HUD tooltips.
    /// </summary>
    public string Description { get; protected set; } = string.Empty;

    /// <summary>
    ///     The number of turns the effect will last.
    ///     A value of <see cref="Constants.PermanentStatusEffect" /> (-1) means the effect is permanent.
    /// </summary>
    public int Duration { get; protected internal set; }

    /// <summary>
    ///     The number of times this effect has been stacked.
    /// </summary>
    public int StackCount { get; protected set; } = 1;

    /// <summary>
    ///     Whether multiple copies of this effect can coexist on the same target.
    /// </summary>
    /// <remarks>
    ///     Per the feature doc: Poison, Burn, Bleed are stackable; Curse, Confusion, Stun are not.
    /// </remarks>
    public virtual bool IsStackable => false;

    /// <summary>
    ///     Whether this effect can be removed by a "purify" skill / item.
    /// </summary>
    /// <remarks>
    ///     Per the feature doc, every status except <c>Curse</c> is purifiable.
    /// </remarks>
    public virtual bool IsPurifiable => true;

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
    ///     Called at the end of each turn to update the effect's duration or apply ongoing logic.
    /// </summary>
    /// <param name="target">The target affected by this effect.</param>
    /// <remarks>
    ///     The default implementation decrements <see cref="Duration" /> for non-permanent
    ///     effects. Override to add per-turn ticks (DoT damage, regen, etc.). Always call
    ///     <c>base.OnTurnPassed(target)</c> first if you want the duration to count down.
    /// </remarks>
    public virtual void OnTurnPassed(IEffectTarget target)
    {
        if (Duration == Constants.PermanentStatusEffect)
            return;
        Duration--;
    }

    /// <summary>
    ///     Increment <see cref="StackCount" /> when applicable.
    /// </summary>
    public virtual void AddStack()
    {
        if (IsStackable) StackCount++;
    }
}
