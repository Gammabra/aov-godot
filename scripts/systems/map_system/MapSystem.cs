using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Systems;

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

    private StatusEffectSystem? _statusEffectSystem;

    private static readonly Dictionary<AovDataStructures.CellType, bool> _cellTypeWalkable = new()
    {
        { AovDataStructures.CellType.Empty, false },
        { AovDataStructures.CellType.Grass, true }
    };

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
    ///     List of information of every grid
    /// </summary>
    public readonly List<CellInformation> CellsInformation = [];

    /// <summary>
    ///     Instance of a map system.
    /// </summary>
    /// <remarks>
    ///     It will be used to check if there is only one instance.
    /// </remarks>
    protected static MapSystem? Instance { get; set; }

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
            bool doesMeshExist = _cellTypeWalkable.TryGetValue((AovDataStructures.CellType)meshId, out bool isWalkable);

            if (!doesMeshExist)
            {
                GD.Print(
                    $"Could not find ground type at ({cell.X},{cell.Y},{cell.Z}), initializing with {AovDataStructures.CellType.Empty}"
                );
                cellInformation = new CellInformation(cell.X, cell.Y, cell.Z, AovDataStructures.CellType.Empty, false);
            }
            else
            {
                GD.Print($"Ground type {meshId} found at ({cell.X},{cell.Y},{cell.Z})");
                cellInformation = new CellInformation(
                    cell.X,
                    cell.Y,
                    cell.Z,
                    (AovDataStructures.CellType)meshId,
                    isWalkable
                );
            }

            CellsInformation.Add(cellInformation);
        }
    }

    /// <summary>
    ///     Injects an instance of the status effect system into this map.
    /// </summary>
    /// <param name="statusEffectSystem">The status effect system to be used by this map.</param>
    public virtual void InjectDependencies(StatusEffectSystem statusEffectSystem)
    {
        _statusEffectSystem = statusEffectSystem;
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

        return CellsInformation[index].CellType == AovDataStructures.CellType.Empty;
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

        CellsInformation[index].SetWalkable();
    }

    /// <summary>
    ///     Applies a status effect to a list of cells on the map.
    /// </summary>
    /// <param name="cells">
    ///     A list of cell coordinates represented as tuples (x, y, z) where the status effect should be applied.
    /// </param>
    /// <param name="statusEffect">The status effect to apply to the cells.</param>
    public virtual void SetStatusEffectOnCells(List<(int, int, int)> cells, StatusEffect<CellInformation> statusEffect)
    {
        foreach ((int, int, int) cell in cells)
            try
            {
                int index = GetListIndex(cell.Item1, cell.Item2, cell.Item3);

                _statusEffectSystem?.ApplyEffect(CellsInformation[index], statusEffect);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Does nothing if the cell don't exist.
            }
    }

    public virtual AovDataStructures.CellType GetCellType(Vector3I position)
    {
        int index = GetListIndex(position.X, position.Y, position.Z);

        return CellsInformation[index].CellType;
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
