namespace AshesOfVelsingrad.Utilities;

/// <summary>
///     Provides shared data structures and enumerations used across the game systems.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="AovDataStructures" /> centralizes common enumerations and value types
///         related to gameplay logic, including combat resolution, status effects,
///         targeting rules, unit archetypes, and turn management.
///     </para>
///     <para>
///         This static class is intended to act as a single source of truth for
///         core gameplay definitions, ensuring consistency and reducing coupling
///         between systems.
///     </para>
/// </remarks>
public static class AovDataStructures
{
    /// <summary>
    ///     Enumeration of every cell type available in the game.
    /// </summary>
    public enum CellType
    {
        // Add a cell type when needed
        Empty = -1,
        Grass
    }

    /// <summary>
    ///     Defines the current interaction mode when clicking on the map.
    /// </summary>
    public enum ClickOnMapContext
    {
        /// <summary>The player is selecting a cell to move a unit.</summary>
        MoveUnit,

        /// <summary>The player is selecting a unit or target cell for a skill.</summary>
        SelectUnitTarget
    }

    /// <summary>
    ///     Defines the type of effect applied by a skill or status effect.
    /// </summary>
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

    /// <summary>
    ///     Represents the outcome of a battle.
    /// </summary>
    public enum GameOutcome
    {
        /// <summary>The battle is ongoing; no winner yet.</summary>
        Ongoing,

        /// <summary>The player has won the battle.</summary>
        Victory,

        /// <summary>The player has lost the battle.</summary>
        Defeat
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
    ///     Defines how a numerical modifier affects a value.
    /// </summary>
    public enum ModifierType
    {
        Flat,
        Percent
    }

    /// <summary>
    ///     Represents a stat that can receive modifiers.
    /// </summary>
    public enum StatTypeWithModifier
    {
        Atk,
        Def
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

    /// <summary>
    ///     Defines the possible turn states in the battle system.
    /// </summary>
    public enum TurnState
    {
        /// <summary>The player's turn to act.</summary>
        PlayerTurn,

        /// <summary>The enemy's turn to act.</summary>
        EnemyTurn,

        /// <summary>Idle state while waiting for setup or transitions.</summary>
        Waiting,

        /// <summary>State that inform the game is finished.</summary>
        Finished
    }

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
}
