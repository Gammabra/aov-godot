using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.Systems;

public abstract partial class MapSystem
{
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
            cell.SetUnit();
            SetWalkable(cell.X, cell.Y, cell.Z);
            break;
        }

        // Assign to new position
        int index = GetListIndex(newX, newY, newZ);

        CellsInformation[index].SetUnit(unit);
        CellsInformation[index].OnUnitEntered(unit);
        SetWalkable(newX, newY, newZ);
    }

    /// <summary>
    ///     Gets the unit currently occupying map cell at the given position.
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

    /// <summary>
    ///     Gets the unit currently position on map.
    /// </summary>
    /// <param name="unit">The unit to find.</param>
    /// <returns>
    ///     The (x, y, z) position of the unit at the specified map, or <c>null</c> if the unit is not found.
    /// </returns>
    public virtual (int, int,int)? GetUnitPosition(UnitSystem unit)
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

        CellsInformation[index].SetUnit();
    }
}
