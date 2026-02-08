namespace AshesOfVelsingrad.Utilities;

public static class AovDataStructures
{
    /// <summary>
    ///     Represents the different unit archetypes in the game.
    /// </summary>
    public enum UnitType
    {
        /// <summary>Default player-controlled unit.</summary>
        Player,

        /// <summary>Close-range melee fighter unit.</summary>
        Fighter,

        /// <summary>Heavy melee swordsman unit.</summary>
        Swordsman,

        /// <summary>High-mobility stealth unit.</summary>
        Assassin,

        /// <summary>Ranged physical attacker.</summary>
        Archer,

        /// <summary>Magic-based ranged attacker.</summary>
        Mage
    }

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
    ///     Enumeration of every cell type available in the game.
    /// </summary>
    public enum CellType
    {
        // Add a cell type when needed
        Empty = -1,
        Grass
    }

    public enum EffectType
    {
        Damage,
        Heal,
        Revive,
        ApplyModifier,
        RemoveModifier,
        Control,
        Summon
    }

    public enum StatTypeWithModifier
    {
        Atk,
        Def
    }

    public enum ModifierType
    {
        Flat,
        Percent
    }

    /* /// <summary>
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
        StatusEffect,
        Revive
    }*/

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
}
