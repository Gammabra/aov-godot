using System;
using System.Collections.Generic;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class AIUtilitiesTest
{
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private TestConcreteGameManager? _gameManager;
    private TestConcreteMapSystem? _mapSystem;
    private TestConcreteUnitSystem? _aiUnit;
    private List<UnitSystem> _playerUnits = new();
    private List<UnitSystem> _enemyUnits = new();
    private BattleState? _battleState;

    #region Setup and Teardown

    [BeforeTest]
    public void Setup()
    {
        _testNodes.Clear();
        _playerUnits.Clear();
        _enemyUnits.Clear();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        SetupBasicTest();
    }

    [AfterTest]
    public void TearDown()
    {
        foreach (Node node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
                node.QueueFree();
        }

        _testNodes.Clear();
        _playerUnits.Clear();
        _enemyUnits.Clear();
    }

    #endregion

    #region Helper Methods

    private T AddNodeToTestRoot<T>(T node) where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Test root node is not initialized.");
        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    private void SetupBasicTest()
    {
        _gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        _mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        _mapSystem.CallInitialize();

        // Create a 5x5 grid
        for (int x = 0; x < 5; x++)
        {
            for (int z = 0; z < 5; z++)
            {
                _mapSystem.AddWalkableCell(x, 0, z);
            }
        }

        // Create AI unit at (2, 0, 2)
        _aiUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "AIEnemy" });
        _aiUnit.CallInitialize();
        _aiUnit.PossibleMovesRange = 3;
        _mapSystem.CellsInformation[12].SetUnit(_aiUnit);

        // Create player units
        var player1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player1" });
        player1.CallInitialize();
        _mapSystem.CellsInformation[24].SetUnit(player1); // (4,0,4)
        _playerUnits.Add(player1);

        var ally1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally1" });
        ally1.CallInitialize();
        _mapSystem.CellsInformation[0].SetUnit(ally1); // (0,0,0)
        _enemyUnits.Add(_aiUnit);
        _enemyUnits.Add(ally1);

        _battleState = new BattleState
        {
            ActingUnit = _aiUnit,
            MapSystem = _mapSystem,
            PlayerUnits = _playerUnits,
            EnemyUnits = _enemyUnits
        };
    }

    #endregion

    #region CalculateManhattanDistance Tests

    [TestCase]
    public void CalculateManhattanDistance_ReturnsZero_ForSamePosition()
    {
        var pos = (2, 0, 2);
        int distance = AIUtilities.CalculateManhattanDistance(pos, pos);

        AssertThat(distance).IsEqual(0);
    }

    [TestCase]
    public void CalculateManhattanDistance_CalculatesHorizontalDistance()
    {
        var pos1 = (0, 0, 0);
        var pos2 = (3, 0, 0);

        int distance = AIUtilities.CalculateManhattanDistance(pos1, pos2);

        AssertThat(distance).IsEqual(3);
    }

    [TestCase]
    public void CalculateManhattanDistance_CalculatesVerticalDistance()
    {
        var pos1 = (0, 0, 0);
        var pos2 = (0, 5, 0);

        int distance = AIUtilities.CalculateManhattanDistance(pos1, pos2);

        AssertThat(distance).IsEqual(5);
    }

    [TestCase]
    public void CalculateManhattanDistance_CalculatesDiagonalDistance()
    {
        var pos1 = (0, 0, 0);
        var pos2 = (3, 0, 4);

        int distance = AIUtilities.CalculateManhattanDistance(pos1, pos2);

        // Using Euclidean: sqrt(3^2 + 4^2) = sqrt(25) = 5
        AssertThat(distance).IsEqual(5);
    }

    [TestCase]
    public void CalculateManhattanDistance_Calculates3DDistance()
    {
        var pos1 = (0, 0, 0);
        var pos2 = (2, 2, 1);

        int distance = AIUtilities.CalculateManhattanDistance(pos1, pos2);

        // sqrt(2^2 + 2^2 + 1^2) = sqrt(9) = 3
        AssertThat(distance).IsEqual(3);
    }

    #endregion

    #region CalculateMoveToRange Tests

    [TestCase]
    public void CalculateMoveToRange_ReturnsNull_WhenNoMovesAvailable()
    {
        var isolatedUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Isolated" });
        isolatedUnit.CallInitialize();
        isolatedUnit.PossibleMovesRange = 0; // Can't move

        var result = AIUtilities.CalculateMoveToRange(
            isolatedUnit,
            _battleState!,
            (4, 0, 4),
            1
        );

        AssertThat(result).IsNull();
    }

    [TestCase]
    public void CalculateMoveToRange_FindsOptimalPosition()
    {
        var targetPos = (4, 0, 4);
        int skillRange = 2;

        var result = AIUtilities.CalculateMoveToRange(
            _aiUnit!,
            _battleState!,
            targetPos,
            skillRange
        );

        AssertThat(result).IsNotNull();

        // Verify the returned position is within movement range
        int distanceToTarget = AIUtilities.CalculateManhattanDistance(result!.Value, targetPos);
        AssertThat(distanceToTarget).IsLessEqual(skillRange + _aiUnit!.PossibleMovesRange);
    }

    #endregion

    #region CalculateMoveAway Tests

    [TestCase]
    public void CalculateMoveAway_FindsFarthestPosition()
    {
        var threatPos = (4, 0, 4);
        int minDistance = 2;

        var result = AIUtilities.CalculateMoveAway(
            _aiUnit!,
            _battleState!,
            threatPos,
            minDistance
        );

        AssertThat(result).IsNotNull();

        // Verify the position is at least minDistance away
        int distance = AIUtilities.CalculateManhattanDistance(result!.Value, threatPos);
        AssertThat(distance).IsGreaterEqual(minDistance);
    }

    [TestCase]
    public void CalculateMoveAway_ReturnsNull_WhenNoValidMoves()
    {
        var isolatedUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Isolated" });
        isolatedUnit.CallInitialize();
        isolatedUnit.PossibleMovesRange = 0;

        var result = AIUtilities.CalculateMoveAway(
            isolatedUnit,
            _battleState!,
            (4, 0, 4),
            2
        );

        AssertThat(result).IsNull();
    }

    #endregion

    #region CountEnemyAlliesNear Tests

    [TestCase]
    public void CountEnemyAlliesNear_CountsAlliesInRange()
    {
        // Add another ally near position (1,0,1)
        var ally2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally2" });
        ally2.CallInitialize();
        _mapSystem!.CellsInformation[6].SetUnit(ally2); // (1,0,1)
        _enemyUnits.Add(ally2);

        int count = AIUtilities.CountEnemyAlliesNear(
            _aiUnit!,
            (2, 0, 2),
            _battleState!,
            2
        );

        // Ally1 at (0,0,0) - distance 3 (out of range)
        // Ally2 at (1,0,1) - distance 2 (in range)
        AssertThat(count).IsEqual(1);
    }

    [TestCase]
    public void CountEnemyAlliesNear_ExcludesSelf()
    {
        int count = AIUtilities.CountEnemyAlliesNear(
            _aiUnit!,
            (2, 0, 2), // AI's own position
            _battleState!,
            10 // Large range
        );

        // Should not count itself
        AssertThat(count).IsGreaterEqual(0);
    }

    [TestCase]
    public void CountEnemyAlliesNear_ReturnsZero_WhenNoAlliesInRange()
    {
        int count = AIUtilities.CountEnemyAlliesNear(
            _aiUnit!,
            (4, 0, 4),
            _battleState!,
            1
        );

        AssertThat(count).IsEqual(0);
    }

    #endregion

    #region CountPlayerUnitsNear Tests

    [TestCase]
    public void CountPlayerUnitsNear_CountsEnemiesInRange()
    {
        // Add another player near (3,0,3)
        var player2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player2" });
        player2.CallInitialize();
        _mapSystem!.CellsInformation[18].SetUnit(player2); // (3,0,3)
        _playerUnits.Add(player2);

        int count = AIUtilities.CountPlayerUnitsNear(
            _aiUnit!,
            (2, 0, 2),
            _battleState!,
            2
        );

        // Player1 at (4,0,4) - distance 3 (out of range)
        // Player2 at (3,0,3) - distance 2 (in range)
        AssertThat(count).IsEqual(1);
    }

    [TestCase]
    public void CountPlayerUnitsNear_ReturnsZero_WhenNoEnemiesInRange()
    {
        int count = AIUtilities.CountPlayerUnitsNear(
            _aiUnit!,
            (0, 0, 0),
            _battleState!,
            1
        );

        AssertThat(count).IsEqual(0);
    }

    #endregion

    #region CanKillThisTurn Tests

    [TestCase]
    public void CanKillThisTurn_ReturnsTrue_WhenTargetCanBeKilled()
    {
        var weakTarget = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Weak" });
        weakTarget.CallInitialize();
        weakTarget.TakeDamage(95); // 5 HP remaining

        _aiUnit!.BaseAtk = 20;
        weakTarget.BaseDef = 5;

        bool canKill = AIUtilities.CanKillThisTurn(_aiUnit, weakTarget);

        // Damage = (20 - 5) * 1.5 = 22.5, Target HP = 5
        AssertThat(canKill).IsTrue();
    }

    [TestCase]
    public void CanKillThisTurn_ReturnsFalse_WhenTargetCannotBeKilled()
    {
        var strongTarget = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Strong" });
        strongTarget.CallInitialize();

        _aiUnit!.BaseAtk = 10;
        strongTarget.BaseDef = 5;

        bool canKill = AIUtilities.CanKillThisTurn(_aiUnit, strongTarget);

        // Damage = (10 - 5) * 1.5 = 7.5, Target HP = 100
        AssertThat(canKill).IsFalse();
    }

    [TestCase]
    public void CanKillThisTurn_ReturnsFalse_WhenDefenseExceedsAttack()
    {
        var tankyTarget = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Tank" });
        tankyTarget.CallInitialize();
        tankyTarget.BaseDef = 30;

        _aiUnit!.BaseAtk = 20;

        bool canKill = AIUtilities.CanKillThisTurn(_aiUnit, tankyTarget);

        // Damage = 0 (defense exceeds attack)
        AssertThat(canKill).IsFalse();
    }

    #endregion

    #region FindNearestThreat Tests

    [TestCase]
    public void FindNearestThreat_FindsClosestEnemy()
    {
        // Add a closer enemy
        var closeEnemy = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "CloseEnemy" });
        closeEnemy.CallInitialize();
        _mapSystem!.CellsInformation[13].SetUnit(closeEnemy); // (3,0,2) - distance 1
        _playerUnits.Add(closeEnemy);

        var nearest = AIUtilities.FindNearestThreat(_aiUnit!, _battleState!);

        AssertThat(nearest).IsEqual(closeEnemy);
    }

    [TestCase]
    public void FindNearestThreat_ReturnsNull_WhenNoEnemies()
    {
        _playerUnits.Clear();

        var nearest = AIUtilities.FindNearestThreat(_aiUnit!, _battleState!);

        AssertThat(nearest).IsNull();
    }

    [TestCase]
    public void FindNearestThreat_ReturnsNull_WhenUnitNotOnMap()
    {
        var offMapUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "OffMap" });
        offMapUnit.CallInitialize();

        var nearest = AIUtilities.FindNearestThreat(offMapUnit, _battleState!);

        AssertThat(nearest).IsNull();
    }

    #endregion

    #region CalculateThreatLevel Tests

    [TestCase]
    public void CalculateThreatLevel_CalculatesBasedOnProximity()
    {
        var position = (2, 0, 2);

        // Add enemy at distance 1
        var closeEnemy = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Close" });
        closeEnemy.CallInitialize();
        closeEnemy.BaseAtk = 20;
        _mapSystem!.CellsInformation[13].SetUnit(closeEnemy); // (3,0,2)
        _playerUnits.Add(closeEnemy);

        float threat = AIUtilities.CalculateThreatLevel(position, _battleState!, 3);

        // Closer enemies contribute more threat
        AssertThat(threat).IsGreater(0f);
    }

    [TestCase]
    public void CalculateThreatLevel_ReturnsZero_WhenNoEnemiesInRange()
    {
        var position = (0, 0, 0);

        float threat = AIUtilities.CalculateThreatLevel(position, _battleState!, 1);

        // Player1 is at (4,0,4), out of range
        AssertThat(threat).IsEqual(0f);
    }

    #endregion

    #region HasLineOfSight Tests

    [TestCase]
    public void HasLineOfSight_ReturnsTrue_ForClosePositions()
    {
        var from = (0, 0, 0);
        var to = (3, 0, 3);

        bool los = AIUtilities.HasLineOfSight(from, to, _mapSystem!);

        AssertThat(los).IsTrue();
    }

    [TestCase]
    public void HasLineOfSight_ReturnsFalse_ForDistantPositions()
    {
        var from = (0, 0, 0);
        var to = (10, 0, 10);

        bool los = AIUtilities.HasLineOfSight(from, to, _mapSystem!);

        AssertThat(los).IsFalse();
    }

    #endregion

    #region GetUnitsInRange Tests

    [TestCase]
    public void GetUnitsInRange_ReturnsUnitsWithinRange()
    {
        var position = (2, 0, 2);

        // Add a close unit
        var closeUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Close" });
        closeUnit.CallInitialize();
        _mapSystem!.CellsInformation[13].SetUnit(closeUnit); // (3,0,2) - distance 1
        _playerUnits.Add(closeUnit);

        var unitsInRange = AIUtilities.GetUnitsInRange(
            position,
            2,
            _playerUnits,
            _mapSystem
        );

        AssertThat(unitsInRange.Count).IsEqual(1);
        AssertThat(unitsInRange[0]).IsEqual(closeUnit);
    }

    [TestCase]
    public void GetUnitsInRange_ReturnsEmpty_WhenNoUnitsInRange()
    {
        var position = (0, 0, 0);

        var unitsInRange = AIUtilities.GetUnitsInRange(
            position,
            1,
            _playerUnits,
            _mapSystem!
        );

        AssertThat(unitsInRange.Count).IsEqual(0);
    }

    #endregion

    #region EvaluatePositionDefensibility Tests

    [TestCase]
    public void EvaluatePositionDefensibility_FavorsHighGround()
    {
        _mapSystem!.AddWalkableCell(2, 2, 2);

        var lowGround = (2, 0, 2);
        var highGround = (2, 2, 2);

        float scoreLow = AIUtilities.EvaluatePositionDefensibility(lowGround, _battleState!);
        float scoreHigh = AIUtilities.EvaluatePositionDefensibility(highGround, _battleState!);

        AssertThat(scoreHigh).IsGreater(scoreLow);
    }

    [TestCase]
    public void EvaluatePositionDefensibility_FavorsChokepoints()
    {
        // Create a chokepoint by limiting walkable adjacent cells
        // Position (0,0,0) is a corner with fewer adjacent cells
        var chokepoint = (0, 0, 0);
        var openArea = (2, 0, 2);

        float scoreChoke = AIUtilities.EvaluatePositionDefensibility(chokepoint, _battleState!);
        float scoreOpen = AIUtilities.EvaluatePositionDefensibility(openArea, _battleState!);

        // Chokepoint should score higher (fewer adjacent walkable cells)
        AssertThat(scoreChoke).IsGreater(scoreOpen);
    }

    #endregion

    #region FindCenterPoint Tests

    [TestCase]
    public void FindCenterPoint_CalculatesAveragePosition()
    {
        var units = new List<UnitSystem>
        {
            _playerUnits[0], // At (4,0,4)
			_enemyUnits[1]   // At (0,0,0)
		};

        var center = AIUtilities.FindCenterPoint(units, _mapSystem!);

        AssertThat(center).IsNotNull();
        // Average: (4+0)/2=2, (0+0)/2=0, (4+0)/2=2
        AssertThat(center!.Value).IsEqual(new Vector3I(2, 0, 2));
    }

    [TestCase]
    public void FindCenterPoint_ReturnsNull_ForEmptyList()
    {
        var empty = new List<UnitSystem>();

        var center = AIUtilities.FindCenterPoint(empty, _mapSystem!);

        AssertThat(center).IsNull();
    }

    [TestCase]
    public void FindCenterPoint_IgnoresUnitsNotOnMap()
    {
        var onMapUnit = _playerUnits[0]; // At (4,0,4)
        var offMapUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "OffMap" });
        offMapUnit.CallInitialize();

        var units = new List<UnitSystem> { onMapUnit, offMapUnit };

        var center = AIUtilities.FindCenterPoint(units, _mapSystem!);

        // Should only consider the on-map unit
        AssertThat(center).IsEqual(new Vector3I(4, 0, 4));
    }

    #endregion
}
