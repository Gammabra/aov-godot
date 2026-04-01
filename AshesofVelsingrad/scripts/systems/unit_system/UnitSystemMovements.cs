using System;
using System.Collections.Generic;

namespace AshesOfVelsingrad.Systems;

public abstract partial class UnitSystem
{
    /// <summary>The maximum number of tiles the unit can move per turn.</summary>
    public int PossibleMovesRange { get; protected set; }

    #region BFS Algorithms for unit moves

    /// <summary>
    ///     Returns all possible floors accessible from a given base tile for vertical movement.
    /// </summary>
    /// <param name="baseFloor">The base position (x, y, z).</param>
    /// <param name="isNegate"><c>true</c> to check downward movement; otherwise upward.</param>
    /// <param name="directions">The 4 cardinal directions.</param>
    /// <param name="map">Reference to the map system.</param>
    /// <returns>A list of valid floor (Y) positions the unit can reach.</returns>
    private static List<int> GetPossibleFloorForUnitMoves(
        (int, int,int) baseFloor,
        bool isNegate,
        (int, int, int)[] directions,
        IMapSystem map
    )
    {
        List<int> possibleFloor = [];
        int floor = 1;

        if (isNegate)
            floor *= -1;
        try
        {
            while (map.IsWalkable(
                    baseFloor.Item1,
                    baseFloor.Item2 + floor,
                    baseFloor.Item3
                ))
            {
                if (!map.IsWalkable( // Check Left
                        baseFloor.Item1 + directions[0].Item1,
                        baseFloor.Item2 + floor,
                        baseFloor.Item3
                    ) &&
                    !map.IsWalkable( // Check Right
                        baseFloor.Item1 + directions[1].Item1,
                        baseFloor.Item2 + floor,
                        baseFloor.Item3
                    ) &&
                    !map.IsWalkable( // Check Forward
                        baseFloor.Item1,
                        baseFloor.Item2 + floor,
                        baseFloor.Item3 + directions[2].Item3
                    ) &&
                    !map.IsWalkable( // Check Backward
                        baseFloor.Item1,
                        baseFloor.Item2 + floor,
                        baseFloor.Item3 + directions[3].Item3
                    )
                )
                {
                    floor += isNegate ? -1 : 1;
                    continue;
                }

                possibleFloor.Add(baseFloor.Item2 + floor);
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
        ref Queue<((int, int,int) pos, int dist)> toExplore,
        List<int> possibleFloors,
        int distance,
        (int, int,int) basePosition
    )
    {
        foreach (int floor in possibleFloors)
        {
            (int, int,int) newPos = basePosition;
            newPos.Item2 = floor;
            toExplore.Enqueue((newPos, distance + 1));
        }
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
    public virtual List<(int, int,int)> GetPossibleMoves(IMapSystem map)
    {
        List<(int, int,int)> possibleMoves = [];
        Queue<((int, int,int) pos, int dist)> toExplore = new();
        (int, int,int)? unitPosition = map.GetUnitPosition(this);
        List<(int, int,int)> visitedCells = [];
        (int, int, int)[] directions =
        [
            (-1, 0, 0), // Left
            (1, 0, 0), // Right
            (0, 0, 1), // Forward
            (0, 0, -1) // Backward
        ];

        if (unitPosition == null)
            return possibleMoves;

        // Queue the unit position
        visitedCells.Add(unitPosition.Value);
        toExplore.Enqueue((unitPosition.Value, 0));
        while (toExplore.Count > 0)
        {
            ((int, int,int) pos, int dist) currentPos = toExplore.Dequeue();

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
                foreach ((int, int, int) dir in directions)
                {
                    (int, int,int) pos = currentPos.Item1;
                    pos.Item1 += dir.Item1;
                    pos.Item2 += upFloor;
                    pos.Item3 += dir.Item3;

                    if (map.IsWalkable(pos.Item1, pos.Item2, pos.Item3))
                        toExplore.Enqueue((pos, currentPos.Item2 + 1));
                }
            }

            // Check the possible neighbor under the unit
            foreach ( (int, int, int) dir in directions)
            {
                (int, int,int) posForDown = (
                    currentPos.Item1.Item1 + dir.Item1,
                    currentPos.Item1.Item2,
                    currentPos.Item1.Item3 + dir.Item3
                );

                List<int> possibleDownFloors = GetPossibleFloorForUnitMoves(posForDown, true, directions, map);
                QueueNewFloors(ref toExplore, possibleDownFloors, currentPos.Item2, posForDown);
            }

            // Check the possible neighbor at the level of the unit
            foreach ( (int, int, int) dir in directions)
            {
                (int, int,int) pos = currentPos.Item1;
                pos.Item1 += dir.Item1;
                pos.Item3 += dir.Item3;

                try
                {
                    if (map.IsWalkable(pos.Item1, pos.Item2, pos.Item3))
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

    #endregion

    #region Move on the map

    /// <summary>
    ///     Moves the unit to the specified grid coordinates in the <see cref="MapSystem" />.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate (height).</param>
    /// <param name="z">The Z grid coordinate.</param>
    /// <param name="map">The map on which the unit exists.</param>
    public virtual void SetGridPosition(int x, int y, int z, IMapSystem map)
    {
        try
        {
            map.MoveUnit(this, x, y, z);
        }
        catch (ArgumentOutOfRangeException e)
        {
            Console.WriteLine(e.Message);
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
    public virtual bool CanMoveTo(int x, int y, int z, IMapSystem map)
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
    public virtual bool MoveTo(int x, int y, int z, IMapSystem map)
    {
        if (!CanMoveTo(x, y, z, map))
            return false;
        SetGridPosition(x, y, z, map);
        return true;
    }

    #endregion
}
