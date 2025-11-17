using System;
using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.Systems;

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
///     Represents the information for a single grid cell in the map,
///     including its coordinates, ground type, walkability, and the unit occupying it.
/// </summary>
/// <param name="x">The x-coordinate of the grid cell.</param>
/// <param name="y">The y-coordinate of the grid cell.</param>
/// <param name="z">The z-coordinate of the grid cell.</param>
/// <param name="cellType">The <see cref="CellType" /> defining the terrain of the cell.</param>
/// <param name="isWalkable">Indicates whether the cell can be walked on.</param>
public sealed class CellInformation(
    int x,
    int y,
    int z,
    CellType cellType,
    bool isWalkable
) : EffectTarget<CellInformation>
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Z { get; } = z;
    public CellType CellType { get; set; } = cellType;
    public bool IsWalkable { get; set; } = isWalkable;
    public UnitSystem? Unit { get; set; }
}

/// <summary>
///     Base class for all playable maps in the game.
/// </summary>
/// <remarks>
///     This class extends Godot's <see cref="GridMap" /> and manages the grid layout,
///     grid metadata (<see cref="CellInformation" />), and unit placement.
///     It enforces a single active instance through the <see cref="Instance" /> property.
/// </remarks>
public abstract partial class MapSystem : GridMap
{
    #region Private Fields

    /// <summary>
    ///     Used to calculate the minimum map
    /// </summary>
    private Vector3I _min;

    /// <summary>
    ///     Used to calculate the maximum map
    /// </summary>
    private Vector3I _max;

    private StatusEffectSystem? _statusEffectSystem;

    #endregion

    #region Protected Properties

    /// <summary>
    ///     List of information of every grid
    /// </summary>
    public readonly List<CellInformation> CellsInformation = [];

    #endregion

    #region Public Properties

    /// <summary>
    ///     The total X axis of the map (in number of cells)
    /// </summary>
    public int Width => GetUsedSize().X;

    /// <summary>
    ///     The total Y axis of the map (in number of cells)
    /// </summary>
    public int Height => GetUsedSize().Y;

    /// <summary>
    ///     The total Z axis of the map (in number of cells)
    /// </summary>
    public int Depth => GetUsedSize().Z;

    /// <summary>
    ///     The size of a single grid in the UI
    /// </summary>
    public Vector3 MapCellSize { get; protected set; }

    public static readonly Dictionary<CellType, bool> CellTypeWalkable = new()
    {
        { CellType.Empty, false },
        { CellType.Grass, true }
    };

    /// <summary>
    ///     Instance of a map system.
    /// </summary>
    /// <remarks>
    ///     It will be used to check if there is only one instance.
    /// </remarks>
    public static MapSystem? Instance { get; set; }

    #endregion

    #region Class Initialization

    /// <summary>
    ///     Called when the node is added to the scene tree.
    ///     Initializes the map instance and checks for duplicates.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is ready.
    ///     It ensures that only one instance of the map exists in the scene tree.
    ///     If a duplicate instance is found, it removes the duplicate.
    /// </remarks>
    public override void _Ready()
    {
        // For AutoLoad, the initialization does immediately
        if (IsInsideTree() && GetParent() == GetTree().Root)
        {
            Initialize();
        }
        // For manual instances, check for duplicates.
        else if (Instance == null)
        {
            Initialize();
        }
        else
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
        }
    }

    /// <summary>
    ///     Initializes the map instance
    ///     This method should be overridden in derived classes to set up specific functionality.
    /// </summary>
    /// <remarks>
    ///     This method is called by the _Ready method to initialize the map.
	///     It should contain the logic necessary to set up the map's state and functionality.
	///     Derived classes must implement this method to provide their specific initialization logic.
	/// </remarks>
	protected virtual void Initialize()
    {
        MapCellSize = CellSize;
        foreach (Vector3I cell in GetUsedCells())
        {
            CellInformation cellInformation;
            int meshId = GetCellItem(cell);
            bool doesMeshExist = CellTypeWalkable.TryGetValue((CellType)meshId, out bool isWalkable);

            if (!doesMeshExist)
            {
                GD.Print(
                    $"Could not find ground type at ({cell.X},{cell.Y},{cell.Z}), initializing with {CellType.Empty}"
                );
                cellInformation = new CellInformation(cell.X, cell.Y, cell.Z, CellType.Empty, false);
            }
            else
            {
                GD.Print($"Ground type {meshId} found at ({cell.X},{cell.Y},{cell.Z})");
                cellInformation = new CellInformation(cell.X, cell.Y, cell.Z, (CellType)meshId, isWalkable);
            }

            CellsInformation.Add(cellInformation);
        }
    }

    /// <summary>
    /// Injects an instance of the status effect system into this map.
    /// </summary>
    /// <param name="statusEffectSystem">The status effect system to be used by this map.</param>
    public virtual void InjectDependencies(StatusEffectSystem statusEffectSystem)
    {
        _statusEffectSystem = statusEffectSystem;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Compute the size of the map in cells, based on all occupied GridMap cells.
    /// </summary>
    private Vector3I GetUsedSize()
    {
        _min.X = int.MaxValue;
        _min.Y = int.MaxValue;
        _min.Z = int.MaxValue;
        _max.X = int.MinValue;
        _max.Y = int.MinValue;
        _max.Z = int.MinValue;
        foreach (Vector3I cell in GetUsedCells())
        {
            if (cell.X < _min.X)
                _min.X = cell.X;
            if (cell.Y < _min.Y)
                _min.Y = cell.Y;
            if (cell.Z < _min.Z)
                _min.Z = cell.Z;
            if (cell.X > _max.X)
                _max.X = cell.X;
            if (cell.Y > _max.Y)
                _max.Y = cell.Y;
            if (cell.Z > _max.Z)
                _max.Z = cell.Z;
        }

        if (_max.X == int.MinValue)
            return Vector3I.Zero;
        return _max - _min + Vector3I.One;
    }

    /// <summary>
    ///     Converts 3D grid coordinates into the corresponding list index
    ///     used by <see cref="CellsInformation" />.
    /// </summary>
    /// <param name="x">The x-coordinate of the grid.</param>
    /// <param name="y">The y-coordinate of the grid.</param>
    /// <param name="z">The z-coordinate of the grid.</param>
    /// <returns>The index of the grid in the internal list.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if the provided coordinates are outside the bounds of the map.
    /// </exception>
    private int GetListIndex(int x, int y, int z)
    {
        int i = 0;

        foreach (CellInformation cellInformation in CellsInformation)
        {
            if (cellInformation.X == x && cellInformation.Y == y && cellInformation.Z == z)
                return i;
            i++;
        }

        throw new ArgumentOutOfRangeException("Out of range");
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Getter to know if a cell is empty.
    /// </summary>
    /// <param name="x">The x-axis</param>
    /// <param name="y">The y-axis</param>
    /// <param name="z">The z-axis</param>
    /// <returns><c>True</c> if the cell is empty, <c>False</c> otherwise</returns>
    public virtual bool IsEmpty(int x, int y, int z)
    {
        int index = GetListIndex(x, y, z);

        return CellsInformation[index].CellType == CellType.Empty;
    }

    /// <summary>
    ///     Getter to know if a cell is walkable.
    /// </summary>
    /// <param name="x">The x-axis</param>
    /// <param name="y">The y-axis</param>
    /// <param name="z">The z-axis</param>
    /// <returns><c>True</c> if the cell is walkable, <c>False</c> otherwise</returns>
    public virtual bool IsWalkable(int x, int y, int z)
    {
        int index = GetListIndex(x, y, z);

        return CellsInformation[index].IsWalkable;
    }

    /// <summary>
    ///     Set the IsWalkable variable of a <see cref="CellInformation" /> to the reverse value.
    /// </summary>
    /// <param name="x">The x-axis</param>
    /// <param name="y">The y-axis</param>
    /// <param name="z">The z-axis</param>
    public virtual void SetWalkable(int x, int y, int z)
    {
        int index = GetListIndex(x, y, z);

        CellsInformation[index].IsWalkable = !CellsInformation[index].IsWalkable;
    }

    /// <summary>
    ///     Place every unit in the map.
    /// </summary>
    /// <param name="playerUnits">List of every unit of the player</param>
    /// <param name="enemyUnits">List of every enemy on the maps</param>
    /// <remarks>
    ///     It must be called only for the class initialization.
    /// </remarks>
    public abstract void PlaceUnits(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits);

    /// <summary>
    ///     Moves a unit to a new position in the map.
    /// </summary>
    /// <param name="unit">The unit to move.</param>
    /// <param name="newX">The target x-coordinate in the cell.</param>
    /// <param name="newY">The target y-coordinate in the cell.</param>
    /// <param name="newZ">The target z-coordinate in the cell.</param>
    /// <remarks>
    ///     Implementations should handle removing the unit from its current cell
    ///     and assigning it to the new cell. This method does not check whether the
    ///     target cell is walkable, which should be verified beforehand.
    /// </remarks>
    public virtual void MoveUnit(UnitSystem unit, int newX, int newY, int newZ)
    {
        // Remove from old position
        foreach (CellInformation cell in CellsInformation)
        {
            if (cell.Unit != unit)
                continue;
            cell.Unit = null;
            SetWalkable(cell.X, cell.Y, cell.Z);
            break;
        }

        // Assign to new position
        int index = GetListIndex(newX, newY, newZ);

        CellsInformation[index].Unit = unit;
        SetWalkable(newX, newY, newZ);
    }

    /// <summary>
    ///     Gets the unit currently occupying the map cell at the given position.
    /// </summary>
    /// <param name="x">The x-coordinate of the map cell.</param>
    /// <param name="y">The y-coordinate of the map cell.</param>
    /// <param name="z">The z-coordinate of the map cell.</param>
    /// <returns>
    ///     The unit at the specified map cell, or <c>null</c> if the cell is empty.
    /// </returns>
    public virtual UnitSystem? GetUnitAt(int x, int y, int z)
    {
        int index = GetListIndex(x, y, z);

        return CellsInformation[index].Unit;
    }

    public virtual (int, int, int)? GetUnitPosition(UnitSystem unit)
    {
        foreach (CellInformation cell in CellsInformation)
            if (cell.Unit == unit)
                return (cell.X, cell.Y, cell.Z);
        return null;
    }

    /// <summary>
    ///     Removes any unit present at the given map cell.
    /// </summary>
    /// <param name="x">The x-coordinate of the map cell.</param>
    /// <param name="y">The y-coordinate of the map cell.</param>
    /// <param name="z">The z-coordinate of the map cell.</param>
    /// <remarks>
    ///     Implementations should clear the reference to the unit from the cell.
    ///     If the cell is already empty, this method does nothing.
    /// </remarks>
    public virtual void RemoveUnit(int x, int y, int z)
    {
        int index = GetListIndex(x, y, z);

        CellsInformation[index].Unit = null;
    }

    /// <summary>
    /// Applies a status effect to a list of cells on the map.
    /// </summary>
    /// <param name="cells">
    /// A list of cell coordinates represented as tuples (x, y, z) where the status effect should be applied.
    /// </param>
    /// <param name="statusEffect">The status effect to apply to the cells.</param>
    public virtual void SetStatusEffectOnCells(List<(int, int, int)> cells, StatusEffect<CellInformation> statusEffect)
    {
        foreach ((int, int, int) cell in cells)
            try
            {
                int index = GetListIndex(cell.Item1, cell.Item2, cell.Item3);

                if (_statusEffectSystem is not null)
                    _statusEffectSystem.ApplyEffect(CellsInformation[index], statusEffect);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Does nothing if the cell don't exist.
            }
    }

    #endregion

    #region Class Destroyer

    /// <summary>
    ///     Called when the node is removed from the scene tree.
    ///     Cleans up the manager instance and sets it to null.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is removed from the scene tree.
    ///     It ensures that the map instance is properly cleaned up and set to null.
    ///     This is important for preventing memory leaks and ensuring that the manager can be re-initialized later if needed.
    /// </remarks>
    public override void _ExitTree()
    {
        if (Instance != this)
            return;
        Cleanup();
        Instance = null;
    }

    /// <summary>
    ///     Cleans up the map instance.
    ///     This method can be overridden in derived classes to implement specific cleanup logic.
    /// </summary>
    /// <remarks>
    ///     This method is called when the manager is removed from the scene tree.
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
