using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems.status_effects;
using Godot;

namespace AshesOfVelsingrad.systems;

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
///     Base class for all units in the tactical grid-based system.
///     Handles stats, movement, effects, and Godot integration.
/// </summary>
/// <remarks>
///     This class provides fundamental behavior for all combat units, including:
///     - Base stats (HP, attack, defense, etc.)
///     - Turn logic (HasPlayed)
///     - Movement logic (BFS pathfinding in 3D)
///     - Integration with <see cref="MapSystem" /> and <see cref="StatusEffect" />
/// </remarks>
public abstract partial class UnitSystem : CharacterBody3D, IEffectTarget
{
    #region Private fields

    private readonly EffectTarget _effectTarget = new();
    private TaskCompletionSource? _actionTcs;
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    #endregion

    #region Godot properties

    /// <summary>
    ///     Emitted when the unit's portrait texture changes.
    /// </summary>
    /// <param name="texture">The new portrait texture.</param>
    [Signal]
    public delegate void PortraitChangedEventHandler(Texture2D texture);

    /// <summary>
    ///     Emitted when the unit's health changes.
    /// </summary>
    /// <param name="currentHp">The unit's current HP value.</param>
    /// <param name="maxHp">The unit's maximum HP value.</param>
    [Signal]
    public delegate void HealthChangedEventHandler(float currentHp, float maxHp);

    /// <summary>
    ///     The portrait texture displayed for this unit in the UI.
    /// </summary>
    public Texture2D? PortraitTexture { get; protected set; }

    /// <summary>
    /// The 3D Sprite of the unit displayed in the UI.
    /// </summary>
    public Sprite3D? CharacterSprite { get; protected set; }

    #endregion

    #region Public Properties

    /// <summary>The name of the unit.</summary>
    public string UnitName { get; protected set; } = string.Empty;

    /// <summary>Descriptive text about the unit.</summary>
    public string Description { get; protected set; } = string.Empty;

    /// <summary>The current health points of the unit.</summary>
    public float Hp { get; protected set; }

    /// <summary>The maximum health points of the unit.</summary>
    public float MaxHp { get; protected set; }

    /// <summary>The base physical attack value of the unit.</summary>
    public float BaseAtk { get; protected set; }

    /// <summary>The base physical defense value of the unit.</summary>
    public float BaseDef { get; protected set; }

    /// <summary>The unit's base speed (used for turn order or initiative).</summary>
    public float BaseSpeed { get; protected set; }

    /// <summary>The unit's intelligence stat, affecting magical power or effects.</summary>
    public float Intelligence { get; protected set; }

    /// <summary>The unit's available mana points for casting skills.</summary>
    public float ManaPoint { get; protected set; }

    /// <summary>The type or archetype of the unit.</summary>
    public UnitType Type { get; protected set; }

    /// <summary>Indicates whether the unit is alive.</summary>
    public bool IsAlive { get; protected set; } = true;

    /// <summary>Indicates whether the unit has already acted this turn.</summary>
    public bool HasPlayed { get; protected set; }

    /// <summary>The maximum number of tiles the unit can move per turn.</summary>
    public int PossibleMovesRange { get; protected set; }

    /// <summary>List of active (usable) skills available to this unit.</summary>
    public List<SkillSystem> ActiveSkills { get; protected set; } = [];

    /// <summary>List of passive (always-on) skills applied to this unit.</summary>
    public List<SkillSystem> PassiveSkills { get; protected set; } = [];

    /// <summary>The unit’s curse value (used for status mechanics or debuffs).</summary>
    public float Curse { get; protected set; }

    #endregion

    #region Class Initialization

    /// <summary>
    ///     Called when the node is added to the scene tree.
    ///     Initializes the unit instance.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is ready.
    /// </remarks>
    public override void _Ready()
    {
        Initialize();
    }

    /// <summary>
    ///     Initializes the unit instance
    ///     This method should be overridden in derived classes to set up specific functionality.
    /// </summary>
    /// <remarks>
    ///     This method is called by the _Ready method to initialize the map.
    ///     It should contain the logic necessary to set up the unit's state and functionality.
    ///     Derived classes must implement this method to provide their specific initialization logic.
    /// </remarks>
    protected virtual void Initialize()
    {
        foreach (Node child in GetChildren())
            if (child is Sprite3D sprite)
            {
                CharacterSprite = sprite;
                break;
            }
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Set the result to unlock the system and clean the <see cref="TaskCompletionSource" />.
    /// </summary>
    private void CompleteAction()
    {
        _actionTcs?.TrySetResult();
        _actionTcs = null;
    }

    /// <summary>
    ///     Called by every <see cref="UnitSystem" /> function with an action to report to the system that the unit has played.
    /// </summary>
    protected void ReportSystemUnitHasPlayed()
    {
        GD.Print($"{Name} has played");
        CompleteAction();
    }

    /// <summary>
    ///     Returns all possible floors accessible from a given base tile for vertical movement.
    /// </summary>
    /// <param name="baseFloor">The base position (x, y, z).</param>
    /// <param name="isNegate"><c>true</c> to check downward movement; otherwise upward.</param>
    /// <param name="directions">The 4 cardinal directions.</param>
    /// <param name="map">Reference to the map system.</param>
    /// <returns>A list of valid floor (Y) positions the unit can reach.</returns>
    private static List<int> GetPossibleFloorForUnitMoves(
        Vector3I baseFloor,
        bool isNegate,
        Vector3I[] directions,
        MapSystem map
    )
    {
        List<int> possibleFloor = [];
        int floor = 1;

        if (isNegate)
            floor *= -1;
        try
        {
            while (map.IsWalkable(
                    baseFloor.X,
                    baseFloor.Y + floor,
                    baseFloor.Z
                ))
            {
                if (!map.IsWalkable( // Check Left
                        baseFloor.X + directions[0].X,
                        baseFloor.Y + floor,
                        baseFloor.Z
                    ) &&
                    !map.IsWalkable( // Check Right
                        baseFloor.X + directions[1].X,
                        baseFloor.Y + floor,
                        baseFloor.Z
                    ) &&
                    !map.IsWalkable( // Check Forward
                        baseFloor.X,
                        baseFloor.Y + floor,
                        baseFloor.Z + directions[2].Z
                    ) &&
                    !map.IsWalkable( // Check Backward
                        baseFloor.X,
                        baseFloor.Y + floor,
                        baseFloor.Z + directions[3].Z
                    )
                )
                {
                    floor += isNegate ? -1 : 1;
                    continue;
                }

                possibleFloor.Add(baseFloor.Y + floor);
                floor += isNegate ? -1 : 1;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return possibleFloor;
        }

        return possibleFloor;
    }

    /// <summary>
    ///     Queues new positions to explore based on accessible floor levels.
    /// </summary>
    /// <param name="toExplore">The BFS exploration queue.</param>
    /// <param name="possibleFloors">A list of possible floor levels.</param>
    /// <param name="distance">The current BFS distance.</param>
    /// <param name="basePosition">The base grid position.</param>
    private static void QueueNewFloors(
        ref Queue<(Vector3I pos, int dist)> toExplore,
        List<int> possibleFloors,
        int distance,
        Vector3I basePosition
    )
    {
        foreach (int floor in possibleFloors)
        {
            Vector3I newPos = basePosition;
            newPos.Y = floor;
            toExplore.Enqueue((newPos, distance + 1));
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Handles the physics of the unit in the UI.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        Godot.Vector3 velocity = Velocity;

        if (!IsOnFloor()) velocity.Y -= _gravity * (float)delta;

        Velocity = velocity;
        MoveAndSlide();
    }
    /// <summary>
    ///     Lock the system and wait the player for an action
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that completes when the player finishes their action.
    /// </returns>
    public Task WaitForActionAsync()
    {
        _actionTcs = new TaskCompletionSource();
        return _actionTcs.Task;
    }

    /// <summary>
    ///     Performs an attack on the specified targets.
    /// </summary>
    /// <param name="targets">List of target units to attack.</param>
    /// <param name="map">Reference to the map system for positional logic.</param>
    /// <param name="skill">Tells which active skill to use</param>
    public virtual void Play(List<UnitSystem> targets, MapSystem? map, SkillSystem skill)
    {
        if (!ActiveSkills.Contains(skill))
        {
            GD.PrintErr($"The unit does not have the skill '{skill.Name}'");
            return;
        }

        skill.Use(targets, map);
        ReportSystemUnitHasPlayed();
    }

    /// <summary>
    ///     Applies incoming damage to the unit and updates HP.
    /// </summary>
    /// <param name="damage">The amount of damage received.</param>
    public virtual void TakeDamage(float damage)
    {
        float realDamage = damage - BaseDef;

        if (realDamage < 0)
            realDamage = 0;
        Hp -= realDamage;
    }

    /// <summary>
    ///     Marks the unit as having completed its turn.
    /// </summary>
    public void PassTurn()
    {
        HasPlayed = true;
        ReportSystemUnitHasPlayed();
    }

    /// <summary>
    ///     Moves the unit to the specified grid coordinates in the <see cref="MapSystem" />.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate (height).</param>
    /// <param name="z">The Z grid coordinate.</param>
    /// <param name="map">The map on which the unit exists.</param>
    public virtual void SetGridPosition(int x, int y, int z, MapSystem map)
    {
        try
        {
            map.MoveUnit(this, x, y, z);
        }
        catch (ArgumentOutOfRangeException e)
        {
            GD.Print(e.Message);
        }
    }

    /// <summary>
    ///     Checks if the unit can move to a given position.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate (height).</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="map">The map to check against.</param>
    /// <returns><c>true</c> if the position is walkable; otherwise <c>false</c>.</returns>
    public virtual bool CanMoveTo(int x, int y, int z, MapSystem map)
    {
        return map.IsWalkable(x, y, z);
    }

    /// <summary>
    ///     Moves the unit to the specified coordinates if possible.
    /// </summary>
    /// <param name="x">Target X coordinate.</param>
    /// <param name="y">Target Y coordinate (height).</param>
    /// <param name="z">Target Z coordinate.</param>
    /// <param name="map">The map to interact with.</param>
    /// <returns><c>true</c> if the move was successful; otherwise <c>false</c>.</returns>
    public virtual bool MoveTo(int x, int y, int z, MapSystem map)
    {
        if (!CanMoveTo(x, y, z, map))
            return false;
        SetGridPosition(x, y, z, map);
        ReportSystemUnitHasPlayed();
        return true;
    }

    /// <summary>
    ///     Calculates all possible movement tiles for this unit based on its range and position.
    /// </summary>
    /// <param name="map">The map to evaluate movement on.</param>
    /// <returns>A list of reachable coordinates (x, y, z).</returns>
    /// <remarks>
    ///     This method uses a Breadth-First Search (BFS) algorithm to evaluate all valid moves
    ///     considering walkable tiles and vertical traversal (e.g. stairs, cliffs).
    /// </remarks>
    public virtual List<Vector3I> GetPossibleMoves(MapSystem map)
    {
        List<Vector3I> possibleMoves = [];
        Queue<(Vector3I pos, int dist)> toExplore = new();
        Vector3I? unitPosition = map.GetUnitPosition(this);
        List<Vector3I> visitedCells = [];
        Vector3I[] directions =
        [
            new Vector3I(-1, 0, 0), // Left
            new Vector3I(1, 0, 0), // Right
            new Vector3I(0, 0, 1), // Forward
            new Vector3I(0, 0, -1) // Backward
        ];

        if (unitPosition == null)
            return possibleMoves;

        // Queue the unit position
        visitedCells.Add(unitPosition.Value);
        toExplore.Enqueue((unitPosition.Value, 0));
        while (toExplore.Count > 0)
        {
            (Vector3I, int) currentPos = toExplore.Dequeue();

            if (currentPos.Item2 > PossibleMovesRange)
                continue;

            // Set the current position to possible moves and visited cells
            if (currentPos.Item1 != unitPosition.Value && !visitedCells.Contains(currentPos.Item1))
            {
                possibleMoves.Add(currentPos.Item1);
                visitedCells.Add(currentPos.Item1);
            }

            // Check the possible neighbor on the unit
            List<int> possibleUpFloors = GetPossibleFloorForUnitMoves(currentPos.Item1, false, directions, map);
            foreach (int upFloor in possibleUpFloors)
            {
                foreach (Vector3I dir in directions)
                {
                    Vector3I pos = currentPos.Item1;
                    pos.X += dir.X;
                    pos.Y += upFloor;
                    pos.Z += dir.Z;

                    if (map.IsWalkable(pos.X, pos.Y, pos.Z))
                        toExplore.Enqueue((pos, currentPos.Item2 + 1));
                }
            }

            // Check the possible neighbor under the unit
            foreach (Vector3I dir in directions)
            {
                Vector3I posForDown = new Vector3I(
                    currentPos.Item1.X + dir.X,
                    currentPos.Item1.Y,
                    currentPos.Item1.Z + dir.Z
                );

                List<int> possibleDownFloors = GetPossibleFloorForUnitMoves(posForDown, true, directions, map);
                QueueNewFloors(ref toExplore, possibleDownFloors, currentPos.Item2, posForDown);
            }

            // Check the possible neighbor at the level of the unit
            foreach (Vector3I dir in directions)
            {
                Vector3I pos = currentPos.Item1;
                pos.X += dir.X;
                pos.Z += dir.Z;

                try
                {
                    if (map.IsWalkable(pos.X, pos.Y, pos.Z))
                        toExplore.Enqueue((pos, currentPos.Item2 + 1));
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Just continue the loop without enqueue the position.
                    // If the exception must be handled one day, it must be handled here.
                }
            }
        }

        return possibleMoves;
    }

    /// <inheritdoc />
    public void ApplyEffect(StatusEffect statusEffect)
    {
        _effectTarget.ApplyEffect(statusEffect);
    }

    /// <inheritdoc />
    public void RemoveEffect(StatusEffect statusEffect)
    {
        _effectTarget.RemoveEffect(statusEffect);
    }

    /// <inheritdoc />
    public bool HasEffect<T>()
        where T : StatusEffect
    {
        return _effectTarget.HasEffect<T>();
    }

    /// <inheritdoc />
    public List<StatusEffect> GetActiveEffects()
    {
        return _effectTarget.GetActiveEffects();
    }

    #endregion

    #region Class Destroyer

    /// <summary>
    ///     Called when the node is removed from the scene tree.
    ///     Cleans up the unit instance and sets it to null.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is removed from the scene tree.
    ///     It ensures that the unit instance is properly cleaned up and set to null.
    ///     This is important for preventing memory leaks and ensuring that the manager can be re-initialized later if needed.
    /// </remarks>
    public override void _ExitTree()
    {
        Cleanup();
    }

    /// <summary>
    ///     Cleans up the unit instance.
    ///     This method can be overridden in derived classes to implement specific cleanup logic.
    /// </summary>
    /// <remarks>
    ///     This method is called when the unit is removed from the scene tree.
    ///     It provides a place for derived classes to implement any necessary cleanup logic,
    ///     such as disconnecting signals or releasing resources.
    ///     By default, it does nothing, but derived classes can override it to perform specific cleanup tasks.
    /// </remarks>
    protected virtual void Cleanup()
    {
        // Override in derived classes for cleanup logic
    }

    #endregion
}
