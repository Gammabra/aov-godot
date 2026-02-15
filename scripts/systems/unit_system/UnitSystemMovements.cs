using System;
using System.Collections.Generic;
using Godot;

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
            (Vector3I pos, int dist) currentPos = toExplore.Dequeue();

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

    #endregion

    #region Move on the map

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
        return true;
    }

    #endregion
}
