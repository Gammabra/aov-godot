using System;
using Godot;

namespace AshesOfVelsingrad.Systems;

public abstract partial class MapSystem
{
    /// <summary>
    ///     Used to calculate the maximum map
    /// </summary>
    private Vector3I _max;

    /// <summary>
    ///     Used to calculate the minimum map
    /// </summary>
    private Vector3I _min;

    /// <summary>
    ///     The size of a single grid in the UI
    /// </summary>
    protected Vector3 MapCellSize { get; set; }

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
}
