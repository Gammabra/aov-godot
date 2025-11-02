using System.Collections.Generic;

namespace AshesOfVelsingrad.systems;

/// <summary>
///     Represents the elemental type of a skill.
/// </summary>
public enum MagicType
{
    None,
    Fire,
    Water,
    Earth,
    Wind,
    Light,
    Dark
}

/// <summary>
///     Defines the type of effect a skill applies when used.
/// </summary>
public enum EffectType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Dot,
    Control,
    StatusEffect
}

/// <summary>
///     Defines the targeting pattern of a skill.
/// </summary>
public enum TargetTypes
{
    SingleAlly,
    AllAllies,
    SingleEnemy,
    AllEnemies
}

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
    /// <summary>
    ///     The name of the skill.
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    ///     A description of the skill, used for tooltips or UI.
    /// </summary>
    public string Description { get; protected set; }

    /// <summary>
    ///     The amount of mana consumed when using this skill.
    /// </summary>
    public float ManaCost { get; protected set; }

    /// <summary>
    ///     The total cooldown duration (in turns) before the skill can be reused.
    /// </summary>
    public int TotalCooldown { get; protected set; }

    /// <summary>
    ///     The remaining cooldown (in turns) before the skill becomes available again.
    /// </summary>
    public int Cooldown { get; protected set; }

    /// <summary>
    ///     The maximum distance (in grid units) from which the skill can target.
    /// </summary>
    public int Range { get; protected set; }

    /// <summary>
    ///     The cells affected relative to the target position (area of effect).
    /// </summary>
    public List<(int, int, int)> AreaEffect { get; protected set; }

    /// <summary>
    ///     The magical element type of this skill (e.g., Fire, Water, Light).
    /// </summary>
    public MagicType MagicType { get; protected set; }

    /// <summary>
    ///     The type of effect this skill applies (e.g., damage, heal, buff).
    /// </summary>
    public EffectType EffectType { get; protected set; }

    /// <summary>
    ///     The type of target(s) this skill can be used on.
    /// </summary>
    public TargetTypes TargetType { get; protected set; }

    /// <summary>
    ///     Executes the skill logic when cast.
    /// </summary>
    /// <remarks>
    ///     This method must be implemented in derived classes
    ///     to define the actual effect of the skill (damage, healing, etc.).
    /// </remarks>
    public abstract void Use(List<UnitSystem> targets);

    /// <summary>
    ///     Reduces the cooldown of the skill by one turn, if greater than zero.
    /// </summary>
    public virtual void ReduceCooldown()
    {
        if (Cooldown <= 0)
            return;
        Cooldown--;
    }
}
