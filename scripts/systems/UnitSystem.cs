using System;
using System.Collections.Generic;
using AshesOfVelsingrad.systems.status_effects;
using Godot;

namespace AshesOfVelsingrad.systems;

public enum UnitType
{
    // Player Units (Can also be an enemy unit)
    Player,
    Fighter,
    Swordsman,
    Assassin,
    Archer,
    Mage
}

public abstract partial class UnitSystem : CharacterBody2D, IEffectTarget
{
    #region Private fields

    private readonly EffectTarget _effectTarget = new();

    #endregion

    #region Godot properties

    [Signal]
    public delegate void PortraitChangedEventHandler(Texture2D texture);

    [Signal]
    public delegate void HealthChangedEventHandler(float currentHp, float maxHp);

    public Texture2D? PortraitTexture { get; protected set; }

    #endregion

    #region Public Properties

    public string UnitName { get; protected set; } = string.Empty;
    public string Description { get; protected set; } = string.Empty;
    public float Hp { get; protected set; }
    public float MaxHp { get; protected set; }
    public float BaseAtk { get; protected set; }
    public float BaseDef { get; protected set; }
    public float BaseSpeed { get; protected set; }
    public float Intelligence { get; protected set; }
    public float ManaPoint { get; protected set; }
    public UnitType Type { get; protected set; }
    public bool IsAlive { get; protected set; } = true;
    public bool HasPlayed { get; protected set; }
    public int PossibleMovesRange { get; protected set; }
    public List<SkillSystem> ActiveSkills { get; protected set; } = [];
    public List<SkillSystem> PassiveSkills { get; protected set; } = [];
    public float Curse { get; protected set; }

    #endregion

    #region Class Initialization

    public override void _Ready()
    {
        Initialize();
    }

    protected abstract void Initialize();

    #endregion

    #region Public Methods

    public abstract void Attack(List<UnitSystem> targets, MapSystem? map);
    public abstract void TakeDamage(float damage);

    public void PassTurn()
    {
        HasPlayed = true;
    }

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

    public virtual bool CanMoveTo(int x, int y, int z, MapSystem map)
    {
        return map.IsWalkable(x, y, z);
    }

    public virtual bool MoveTo(int x, int y, int z, MapSystem map)
    {
        if (!CanMoveTo(x, y, z, map))
            return false;
        SetGridPosition(x, y, z, map);
        return true;
    }

    private List<int> GetPossibleFloorForUnitMoves(
        (int, int, int) baseFloor,
        bool isNegate,
        (int, int, int)[] directions,
        MapSystem map
    )
    {
        List<int> possibleFloor = [];
        int floor = 1;

        if (isNegate)
            floor *= -1;
        while (map.IsWalkable(
                baseFloor.Item1,
                baseFloor.Item2 + floor,
                baseFloor.Item3
            ))
        {
            if (!map.IsWalkable( // Check Left
                    baseFloor.Item1 + directions[0].Item1,
                    baseFloor.Item2 + floor * directions[3].Item2,
                    baseFloor.Item3
                ) &&
                !map.IsWalkable( // Check Right
                    baseFloor.Item1 + directions[1].Item1,
                    baseFloor.Item2 + floor * directions[1].Item2,
                    baseFloor.Item3
                ) &&
                !map.IsWalkable( // Check Forward
                    baseFloor.Item1,
                    baseFloor.Item2 + floor * directions[2].Item2,
                    baseFloor.Item3 + directions[2].Item3
                ) &&
                !map.IsWalkable( // Check Backward
                    baseFloor.Item1,
                    baseFloor.Item2 + floor * directions[3].Item2,
                    baseFloor.Item3 + directions[3].Item3
                )
            )
                continue;

            possibleFloor.Add(baseFloor.Item2 + floor);
        }

        return possibleFloor;
    }

    private void QueueNewFloors(
        ref Queue<((int, int, int) pos, int dist)> toExplore,
        List<int> possibleFloors,
        int distance,
        (int, int, int) basePosition
    )
    {
        foreach (int floor in possibleFloors)
        {
            basePosition.Item2 += floor;
            toExplore.Enqueue((basePosition, distance + 1));
        }
    }

    /// <summary>
    ///     Get the possible moves the unit has depending on his actual position and his movement range
    /// </summary>
    /// <param name="map"></param>
    /// <returns>List of every possible moves the unit can do</returns>
    /// <remarks>The algorithm used is the BFS</remarks>
    public virtual List<(int, int, int)> GetPossibleMoves(MapSystem map)
    {
        List<(int, int, int)> possibleMoves = [];
        Queue<((int, int, int) pos, int dist)> toExplore = new();
        (int, int, int)? unitPosition = map.GetUnitPosition(this);
        List<(int, int, int)> visitedCells = [];
        (int, int, int)[] directions =
        [
            (-1, 0, 0), // Left
            (1, 0, 0), // Right
            (0, 0, 1), // Forward
            (0, 0, -1), // Backward
            (0, 1, 0), // Up
            (0, -1, 0) // Down
        ];

        if (unitPosition == null)
            return possibleMoves;

        // Queue the unit position
        toExplore.Enqueue((unitPosition.Value, 0));
        while (toExplore.Count > 0)
        {
            ((int, int, int), int) currentPos = toExplore.Dequeue();
            // Check the possible neighbor on the unit
            List<int> possibleUpFloors = GetPossibleFloorForUnitMoves(currentPos.Item1, false, directions, map);
            QueueNewFloors(ref toExplore, possibleUpFloors, currentPos.Item2, currentPos.Item1);

            // Check the possible neighbor under the unit
            (int, int, int) posForDown = currentPos.Item1;
            posForDown.Item1 -= 1;
            List<int> possibleDownLeftFloors = GetPossibleFloorForUnitMoves(posForDown, true, directions, map);
            QueueNewFloors(ref toExplore, possibleDownLeftFloors, currentPos.Item2, posForDown);
            posForDown.Item1 += 2;
            List<int> possibleDownRightFloors = GetPossibleFloorForUnitMoves(posForDown, true, directions, map);
            posForDown.Item3 += 1;
            List<int> possibleDownFrontFloors = GetPossibleFloorForUnitMoves(posForDown, true, directions, map);
            posForDown.Item3 -= 2;
            List<int> possibleDownBackFloors = GetPossibleFloorForUnitMoves(posForDown, true, directions, map);
            List<int> totalPossibleFloors = possibleUpFloors;

            FillTotalPossibleFloors(ref totalPossibleFloors, ref toExplore, possibleDownLeftFloors, currentPos.Item2, posForDown);
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
