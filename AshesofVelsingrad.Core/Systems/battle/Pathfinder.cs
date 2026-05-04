using System;
using System.Collections.Generic;

namespace AshesOfVelsingrad.Systems.Battle;

/// <summary>
///     Stateless grid path-finder. Runs BFS with parent tracking on an
///     <see cref="IMapSystem" /> to find the shortest walkable path between two cells.
/// </summary>
/// <remarks>
///     <para>
///         Pure C# — does not touch Godot. Drop into the Core test project as-is and exercise
///         it with a fake <see cref="IMapSystem" /> implementation.
///     </para>
///     <para>
///         BFS rather than full A* because the grid is uniform-cost; on uniform-cost grids
///         BFS already returns optimal paths and avoids the heuristic-tuning surface. Swap
///         in A* later if non-uniform terrain is added.
///     </para>
///     <para>
///         The neighbour set is the four cardinal X/Z directions on the same Y. Vertical
///         traversal (stairs / ramps) is intentionally not modelled here — extend by passing
///         a custom neighbour function if your map needs it.
///     </para>
/// </remarks>
public static class Pathfinder
{
    /// <summary>
    ///     Find the shortest walkable path from <paramref name="from" /> to <paramref name="to" />.
    /// </summary>
    /// <param name="map">The map to query for walkability.</param>
    /// <param name="from">Starting cell (inclusive).</param>
    /// <param name="to">Destination cell (inclusive).</param>
    /// <returns>
    ///     The list of cells the unit walks through, ordered from the first step to the
    ///     destination (start is NOT included). Returns an empty list when start == destination.
    ///     Returns <c>null</c> when no path exists.
    /// </returns>
    public static List<(int X, int Y, int Z)>? FindPath(
        IMapSystem map,
        (int X, int Y, int Z) from,
        (int X, int Y, int Z) to)
    {
        if (from == to) return [];
        if (!IsWalkableSafe(map, to.X, to.Y, to.Z)) return null;

        Queue<(int X, int Y, int Z)> queue = new();
        Dictionary<(int X, int Y, int Z), (int X, int Y, int Z)> cameFrom = new();
        queue.Enqueue(from);
        cameFrom[from] = from;

        (int dx, int dz)[] directions =
        [
            (-1, 0),
            (1, 0),
            (0, -1),
            (0, 1),
        ];

        while (queue.Count > 0)
        {
            (int X, int Y, int Z) current = queue.Dequeue();
            if (current == to)
                return Reconstruct(cameFrom, from, to);

            foreach ((int dx, int dz) in directions)
            {
                (int X, int Y, int Z) next = (current.X + dx, current.Y, current.Z + dz);
                if (cameFrom.ContainsKey(next)) continue;
                if (!IsWalkableSafe(map, next.X, next.Y, next.Z)) continue;
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        return null;
    }

    /// <summary>
    ///     Compute the Manhattan distance between two cells on the X/Z plane.
    ///     Used by callers as a cheap range check before invoking <see cref="FindPath" />.
    /// </summary>
    /// <param name="a">First cell.</param>
    /// <param name="b">Second cell.</param>
    /// <returns>The unsigned X+Z step count between the two cells.</returns>
    public static int ManhattanDistance((int X, int Y, int Z) a, (int X, int Y, int Z) b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Z - b.Z);
    }

    /// <summary>
    ///     Wrap <see cref="IMapSystem.IsWalkable" /> in a try/catch so we never throw on
    ///     out-of-range coordinates — out-of-bounds is treated as not-walkable.
    /// </summary>
    /// <param name="map">Map to query.</param>
    /// <param name="x">Cell X.</param>
    /// <param name="y">Cell Y.</param>
    /// <param name="z">Cell Z.</param>
    /// <returns>True if walkable; false on any exception or hard "false" result.</returns>
    private static bool IsWalkableSafe(IMapSystem map, int x, int y, int z)
    {
        try
        {
            return map.IsWalkable(x, y, z);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Walk back through <paramref name="cameFrom" /> from <paramref name="to" /> to
    ///     <paramref name="from" />, building the forward path.
    /// </summary>
    /// <param name="cameFrom">BFS predecessor map.</param>
    /// <param name="from">Start cell.</param>
    /// <param name="to">End cell.</param>
    /// <returns>The forward path, excluding <paramref name="from" />.</returns>
    private static List<(int X, int Y, int Z)> Reconstruct(
        Dictionary<(int X, int Y, int Z), (int X, int Y, int Z)> cameFrom,
        (int X, int Y, int Z) from,
        (int X, int Y, int Z) to)
    {
        List<(int X, int Y, int Z)> path = [];
        (int X, int Y, int Z) cur = to;
        while (cur != from)
        {
            path.Add(cur);
            cur = cameFrom[cur];
        }
        path.Reverse();
        return path;
    }
}
