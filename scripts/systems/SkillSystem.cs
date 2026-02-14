using System.Collections.Generic;
using Godot;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Base class for all skills available in the game.
/// </summary>
/// <remarks>
///     This class defines the common properties and behaviors of every skill,
///     such as its cost, cooldown, range, type, and targeting logic.
///     Specific skills should inherit from this class and implement the <see cref="Use" /> method.
/// </remarks>
public abstract class SkillSystem
{
    #region Public Properties

    /// <summary>
    ///     The name of the skill.
    /// </summary>
    public string Name { get; protected init; } = string.Empty;

    /// <summary>
    ///     A description of the skill, used for tooltips or UI.
    /// </summary>
    public string Description { get; protected init; } = string.Empty;

    /// <summary>
    ///     The amount of mana consumed when using this skill.
    /// </summary>
    public float ManaCost { get; protected init; }

    /// <summary>
    ///     The total cooldown duration (in turns) before the skill can be reused.
    /// </summary>
    public int TotalCooldown { get; protected init; }

    /// <summary>
    ///     The remaining cooldown (in turns) before the skill becomes available again.
    /// </summary>
    public int Cooldown { get; protected set; }

    /// <summary>
    ///     The maximum distance (in grid units) from which the skill can target.
    /// </summary>
    public int Range { get; protected init; }

    /// <summary>
    ///     The cells affected relative to the target position (area of effect).
    /// </summary>
    public List<Vector3I> AreaEffect { get; protected init; } = [];

    /// <summary>
    ///     The magical element type of this skill (e.g., Fire, Water, Light).
    /// </summary>
    public AovDataStructures.MagicType MagicType { get; protected init; }

    /// <summary>
    ///     The type of effect this skill applies (e.g., damage, heal, buff).
    /// </summary>
    public AovDataStructures.EffectType EffectType { get; protected init; }

    /// <summary>
    ///     The type of target(s) this skill can be used on.
    /// </summary>
    public AovDataStructures.TargetTypes TargetType { get; protected init; }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Executes the skill logic when cast.
    /// </summary>
    /// <remarks>
    ///     This method must be implemented in derived classes
    ///     to define the actual effect of the skill (damage, healing, etc.).
    /// </remarks>
    public abstract void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map);

    /// <summary>
    ///     Set the cooldown of the skill to the total cooldown
    /// </summary>
    public virtual void SetCooldown()
    {
        if (Cooldown != 0)
            return;
        Cooldown = TotalCooldown;
    }

    /// <summary>
    ///     Reduces the cooldown of the skill by one turn, if greater than zero.
    /// </summary>
    public virtual void ReduceCooldown()
    {
        if (Cooldown <= 0)
            return;
        Cooldown--;
    }

    #endregion
}
