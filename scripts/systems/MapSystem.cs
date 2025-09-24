using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.systems;

public enum GroundType
{
    // Add a Ground type when needed
    Empty,
    Grass
}

public class GridInformation(
    int X,
    int Y,
    int Z,
    GroundType GroundType,
    bool IsWalkable
)
{
    public int X { get; } = X;
    public int Y { get; } = Y;
    public int Z { get; } = Z;
    public GroundType GroundType { get; set; } = GroundType;
    public bool IsWalkable { get; set; } = IsWalkable;
    public Unit.IUnit? Unit { get; set; } = null;
}

/// <summary>
///     Base class for the playable maps in the game.
/// </summary>
/// <remarks>
///     This abstract class should be linked to a <see cref="GridMap" /> class.
/// </remarks>
public abstract class MapSystem : GridMap
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
    public Vector3 GridSize { get; }

    /// <summary>
    ///     List of every information for a grid
    /// </summary>
    public List<GridInformation> GridInformation { get; protected set; }

    /// <summary>
    ///     Instance of a map system.
    /// </summary>
    /// <remarks>
    ///     It will be used to check if there is only one instance.
    /// </remarks>
    public static MapSystem? Instance { get; protected set; }

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
    protected abstract void Initialize();

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
            if (cell.X < _min.X) _min.X = cell.X;
            if (cell.Y < _min.Y) _min.Y = cell.Y;
            if (cell.Z < _min.Z) _min.Z = cell.Z;

            if (cell.X > _max.X) _max.X = cell.X;
            if (cell.Y > _max.Y) _max.Y = cell.Y;
            if (cell.Z > _max.Z) _max.Z = cell.Z;
        }

        if (_max.X == int.MinValue) return Vector3I.Zero;

        return _max - _min + Vector3I.One;
    }

    private int GetListIndex(int x, int y, int z);

    #endregion

    #region Public Methods

    public GroundType GetGroundType(int x, int y, int z);
    public void SetGroundType(int x, int y, int z, GroundType type);
    public bool IsWalkable(int x, int y, int z);
    public void SetWalkable(int x, int y, int z);

    protected abstract void PlaceUnits(List<Unit.IUnit> playerUnits, List<Unit.IUnit> enemyUnits);
    public void MoveUnit(Unit.IUnit unit, int newX, int newY, int newZ);
    public Unit.IUnit? GetUnitAt(int x, int y, int z);
    public void RemoveUnit(int x, int y, int z);

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
        if (Instance != this) return;
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
