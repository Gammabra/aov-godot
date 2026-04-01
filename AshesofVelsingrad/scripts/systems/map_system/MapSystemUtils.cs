using System;
using Godot;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Systems;

public abstract partial class MapSystem
{
    /// <summary>
    ///     Used to calculate the maximum map
    /// </summary>
    private (int, int, int) _max;

    /// <summary>
    ///     Used to calculate the minimum map
    /// </summary>
    private (int, int, int) _min;

    #region Private Methods

    /// <summary>
    ///     Compute the size of the map in cells, based on all occupied GridMap cells.
    /// </summary>
    private (int, int, int) GetUsedSize()
    {
        _min.Item1 = int.MaxValue;
        _min.Item2 = int.MaxValue;
        _min.Item3 = int.MaxValue;
        _max.Item1 = int.MinValue;
        _max.Item2 = int.MinValue;
        _max.Item3 = int.MinValue;
        foreach (Vector3I cell in GetUsedCells())
        {
            if (cell.X < _min.Item1)
                _min.Item1 = cell.X;
            if (cell.Y < _min.Item2)
                _min.Item2 = cell.Y;
            if (cell.Z < _min.Item3)
                _min.Item3 = cell.Z;
            if (cell.X > _max.Item1)
                _max.Item1 = cell.X;
            if (cell.Y > _max.Item2)
                _max.Item2 = cell.Y;
            if (cell.Z > _max.Item3)
                _max.Item3 = cell.Z;
        }

        if (_max.Item1 == int.MinValue)
            return (0, 0, 0);
        return (_max.Item1 - _min.Item1 + 1, _max.Item2 - _min.Item2 + 1, _max.Item3 - _min.Item3 + 1);
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
