using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using AshesOfVelsingrad.Utilities;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Systems.Battle;

/// <summary>
///     Coverage for <see cref="Pathfinder" /> — BFS over a stub <see cref="IMapSystem" />.
/// </summary>
/// <remarks>
///     Pathfinder powers the A*-style follow used by tween-animated unit movement in
///     <c>GameManagerCombat.AnimateUnitMove</c>. Regressions here break every player
///     and AI move, so coverage is critical.
/// </remarks>
[TestFixture]
public class PathfinderTests
{
    /// <summary>Square obstacle-free map of side <paramref name="size" />.</summary>
    private static StubMap OpenSquare(int size) => new(size, size, isWalkable: (_, _, _) => true);

    [Test]
    public void FindPath_StartEqualsDestination_ReturnsEmptyPath()
    {
        StubMap map = OpenSquare(4);
        List<(int X, int Y, int Z)>? path = Pathfinder.FindPath(map, (1, 0, 1), (1, 0, 1));
        Assert.That(path, Is.Not.Null);
        Assert.That(path!, Is.Empty);
    }

    [Test]
    public void FindPath_StraightLine_ReturnsCellsInOrderAndExcludesStart()
    {
        StubMap map = OpenSquare(5);
        List<(int X, int Y, int Z)>? path = Pathfinder.FindPath(map, (0, 0, 0), (3, 0, 0));
        Assert.That(path, Is.Not.Null);
        Assert.That(path!, Is.EquivalentTo(new[]
        {
            (1, 0, 0), (2, 0, 0), (3, 0, 0),
        }));
        Assert.That(path[^1], Is.EqualTo((3, 0, 0)));
    }

    [Test]
    public void FindPath_ShortestPath_HasManhattanLength()
    {
        StubMap map = OpenSquare(5);
        List<(int X, int Y, int Z)>? path = Pathfinder.FindPath(map, (0, 0, 0), (3, 0, 4));
        Assert.That(path, Is.Not.Null);
        Assert.That(path!.Count, Is.EqualTo(Pathfinder.ManhattanDistance((0, 0, 0), (3, 0, 4))));
    }

    [Test]
    public void FindPath_AroundObstacle_DetoursThroughWalkableCells()
    {
        // Block the middle column at x=1 except the bottom row, forcing the path to detour.
        StubMap map = new(3, 3, (x, _, z) => !(x == 1 && z != 0));
        List<(int X, int Y, int Z)>? path = Pathfinder.FindPath(map, (0, 0, 2), (2, 0, 2));
        Assert.That(path, Is.Not.Null);
        // Path must NOT include (1, 0, 1) — that cell is blocked.
        foreach ((int X, int Y, int Z) c in path!)
            Assert.That(c, Is.Not.EqualTo((1, 0, 1)));
        Assert.That(path[^1], Is.EqualTo((2, 0, 2)));
    }

    [Test]
    public void FindPath_NoPathAvailable_ReturnsNull()
    {
        // 3×3 grid where the destination at (2, 2) is surrounded on all four cardinal
        // neighbours by non-walkable cells. We also clip every query outside the [0..2]
        // range to non-walkable, otherwise the BFS would happily detour through
        // (3, 1) → (3, 2) → (2, 2) since the delegate would default to "walkable" out
        // of bounds.
        StubMap map = new(3, 3, (x, _, z) =>
        {
            if (x < 0 || x >= 3 || z < 0 || z >= 3) return false;
            if (x == 2 && z == 2) return true;          // destination itself
            if (x == 0 && z == 0) return true;          // start
            // Block every cardinal neighbour of (2, 2): (1,2), (3,2), (2,1), (2,3).
            if ((x, z) is (1, 2) or (2, 1)) return false;
            // (3, 2) and (2, 3) are off-grid and already blocked by the bounds check above.
            return true;
        });
        List<(int X, int Y, int Z)>? path = Pathfinder.FindPath(map, (0, 0, 0), (2, 0, 2));
        Assert.That(path, Is.Null);
    }

    [Test]
    public void FindPath_DestinationNotWalkable_ReturnsNull()
    {
        StubMap map = new(4, 4, (x, _, z) => !(x == 3 && z == 3));
        List<(int X, int Y, int Z)>? path = Pathfinder.FindPath(map, (0, 0, 0), (3, 0, 3));
        Assert.That(path, Is.Null);
    }

    [Test]
    public void FindPath_HandlesOutOfBoundsViaArgumentOutOfRangeException()
    {
        // The IMapSystem implementation throws when queried out of bounds (matches the
        // production Godot impl). Pathfinder.IsWalkableSafe must catch and treat as not walkable.
        StubMap map = new(2, 2, (x, _, z) =>
        {
            if (x < 0 || x >= 2 || z < 0 || z >= 2)
                throw new ArgumentOutOfRangeException();
            return true;
        });
        // Path within bounds should still work — the algorithm will probe out-of-bounds
        // neighbours during BFS and must not propagate the exception.
        List<(int X, int Y, int Z)>? path = Pathfinder.FindPath(map, (0, 0, 0), (1, 0, 1));
        Assert.That(path, Is.Not.Null);
        Assert.That(path!.Count, Is.EqualTo(2));
    }

    [Test]
    public void ManhattanDistance_OnlyConsidersXAndZ()
    {
        Assert.That(Pathfinder.ManhattanDistance((0, 0, 0), (3, 0, 4)), Is.EqualTo(7));
        // Y differences are intentionally ignored.
        Assert.That(Pathfinder.ManhattanDistance((0, 0, 0), (3, 99, 4)), Is.EqualTo(7));
    }

    [Test]
    public void ManhattanDistance_IsNonNegativeAndSymmetric()
    {
        var a = (1, 0, 5);
        var b = (4, 0, 2);
        Assert.That(Pathfinder.ManhattanDistance(a, b), Is.EqualTo(Pathfinder.ManhattanDistance(b, a)));
        Assert.That(Pathfinder.ManhattanDistance(a, b), Is.GreaterThanOrEqualTo(0));
    }

    /// <summary>
    ///     Minimal <see cref="IMapSystem" /> that lets each test express its walkability rule
    ///     as a delegate. Every other method is a no-op stub the pathfinder doesn't touch.
    /// </summary>
    private sealed class StubMap : IMapSystem
    {
        private readonly Func<int, int, int, bool> _isWalkable;

        public StubMap(int width, int depth, Func<int, int, int, bool> isWalkable)
        {
            Width = width;
            Depth = depth;
            _isWalkable = isWalkable;
        }

        public int Width { get; }
        public int Height => 1;
        public int Depth { get; }

        public bool IsWalkable(int x, int y, int z) => _isWalkable(x, y, z);

        public bool IsEmpty(int x, int y, int z) => true;
        public void SetWalkable(int x, int y, int z) { }
        public void MoveUnit(IUnitSystem unit, int x, int y, int z) { }
        public void RemoveUnit(int x, int y, int z) { }
        public (int, int, int)? GetUnitPosition(IUnitSystem unit) => null;
        public IUnitSystem? GetUnitAt(int x, int y, int z) => null;
        public AovDataStructures.CellType GetCellType((int, int, int) position) => AovDataStructures.CellType.Grass;
        public void SetStatusEffectOnCells(List<(int, int, int)> cells, StatusEffect<CellInformation> effect) { }
        public void InjectDependencies(StatusEffectSystem statusEffectSystem) { }
        public void PlaceUnits(List<IUnitSystem> playerUnits, List<IUnitSystem> enemyUnits) { }
    }
}
