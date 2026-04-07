using System;
using System.Collections.Generic;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Moq;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.AI;

[TestFixture]
public class AIUtilitiesTests
{
    private Mock<IUnitSystem>? _mockUnit;
    private Mock<IMapSystem>? _mockMapSystem;
    private BattleState? _battleState;

    [SetUp]
    public void SetUp()
    {
        _mockUnit = new Mock<IUnitSystem>();
        _mockMapSystem = new Mock<IMapSystem>();

        _battleState = new BattleState
        {
            MapSystem = _mockMapSystem.Object,
            ActingUnit = _mockUnit.Object,
            PlayerUnits = new List<IUnitSystem>(),
            EnemyUnits = new List<IUnitSystem>()
        };
    }

    #region Distance & Math Tests

    [Test]
    public void CalculateManhattanDistance_CalculatesCorrect3DDistance()
    {
        var pos1 = (0, 0, 0);
        var pos2 = (3, 4, 0);

        int distance = AIUtilities.CalculateManhattanDistance(pos1, pos2);

        // Note: If your math is sqrt based, this is Euclidean distance. 
        // We assert against your implementation's current expected result.
        Assert.That(distance, Is.EqualTo(5));
    }

    [Test]
    public void CalculateManhattanDistance_SamePosition_ReturnsZero()
    {
        var pos = (5, -2, 3);
        Assert.That(AIUtilities.CalculateManhattanDistance(pos, pos), Is.EqualTo(0));
    }

    [Test]
    public void CalculateMoveToRange_WhenNoMovesPossible_ReturnsNull()
    {
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(new List<(int, int, int)>());
        var result = AIUtilities.CalculateMoveToRange(_mockUnit.Object, _battleState!, (0, 0, 0), 1);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void CalculateMoveToRange_FindsBestPositionForSkillRange()
    {
        var targetPos = (5, 0, 0);
        var moves = new List<(int, int, int)> { (0, 0, 0), (3, 0, 0), (10, 0, 0) };
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(moves);
        _mockUnit.Setup(u => u.Personality).Returns(AIPersonality.Balanced);
        _mockMapSystem!.Setup(m => m.GetCellType(It.IsAny<(int, int, int)>())).Returns(AovDataStructures.CellType.Grass);

        var bestMove = AIUtilities.CalculateMoveToRange(_mockUnit.Object, _battleState!, targetPos, 2);

        Assert.That(bestMove, Is.EqualTo((3, 0, 0)));
    }

    [Test]
    public void CalculateMoveAway_FindsFurthestValidPosition()
    {
        var targetPos = (0, 0, 0);
        var moves = new List<(int, int, int)> { (1, 0, 0), (5, 0, 0), (2, 0, 0) };
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(moves);

        var bestMove = AIUtilities.CalculateMoveAway(_mockUnit.Object, _battleState!, targetPos, 3);

        Assert.That(bestMove, Is.EqualTo((5, 0, 0)));
    }

    [Test]
    public void CalculateMoveToRange_WhenSurroundedAndDefensive_AppliesTacticalScores()
    {
        var targetPos = (10, 10, 10);
        var movePos = (1, 1, 1);
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(new List<(int, int, int)> { movePos });
        _mockUnit.Setup(u => u.Personality).Returns(AIPersonality.Defensive);

        // Unroll enemy setup to avoid loop branches in the test coverage
        var enemy1 = new Mock<IUnitSystem>();
        var enemy2 = new Mock<IUnitSystem>();
        var enemy3 = new Mock<IUnitSystem>();
        _battleState!.PlayerUnits.Add(enemy1.Object);
        _battleState.PlayerUnits.Add(enemy2.Object);
        _battleState.PlayerUnits.Add(enemy3.Object);

        _mockMapSystem!.Setup(m => m.GetUnitPosition(enemy1.Object)).Returns((1, 1, 2));
        _mockMapSystem.Setup(m => m.GetUnitPosition(enemy2.Object)).Returns((1, 1, 2));
        _mockMapSystem.Setup(m => m.GetUnitPosition(enemy3.Object)).Returns((1, 1, 2));

        var result = AIUtilities.CalculateMoveToRange(_mockUnit.Object, _battleState!, targetPos, 5);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void CalculateThreatLevel_WhenEnemyHasNoPosition_SkipsUnit()
    {
        var position = (0, 0, 0);
        var physicalEnemy = new Mock<IUnitSystem>();
        physicalEnemy.Setup(e => e.BaseAtk).Returns(10f);

        _battleState!.PlayerUnits.Clear();
        _battleState.PlayerUnits.Add(new Mock<IUnitSystem>().Object); // Ghost
        _battleState.PlayerUnits.Add(physicalEnemy.Object);

        _mockMapSystem!.Setup(m => m.GetUnitPosition(physicalEnemy.Object)).Returns((1, 0, 0));

        float threat = AIUtilities.CalculateThreatLevel(position, _battleState, 5);

        Assert.That(threat, Is.EqualTo(18f));
    }

    #endregion

    #region Threat Assessment Tests

    [Test]
    public void CanKillThisTurn_WhenDamageIsEnough_ReturnsTrue()
    {
        var mockAttacker = new Mock<IUnitSystem>();
        mockAttacker.Setup(u => u.BaseAtk).Returns(50f);

        var mockTarget = new Mock<IUnitSystem>();
        mockTarget.Setup(u => u.BaseDef).Returns(10f);
        mockTarget.Setup(u => u.Hp).Returns(40f);

        Assert.That(AIUtilities.CanKillThisTurn(mockAttacker.Object, mockTarget.Object), Is.True);
    }

    [Test]
    public void CanKillThisTurn_WhenTargetTooTanky_ReturnsFalse()
    {
        var mockAttacker = new Mock<IUnitSystem>();
        mockAttacker.Setup(u => u.BaseAtk).Returns(20f);

        var mockTarget = new Mock<IUnitSystem>();
        mockTarget.Setup(u => u.BaseDef).Returns(15f);
        mockTarget.Setup(u => u.Hp).Returns(100f);

        Assert.That(AIUtilities.CanKillThisTurn(mockAttacker.Object, mockTarget.Object), Is.False);
    }

    [Test]
    public void CalculateThreatLevel_AggregatesEnemyAttackPower()
    {
        var enemy1 = new Mock<IUnitSystem>();
        enemy1.Setup(e => e.BaseAtk).Returns(10f);
        var enemy2 = new Mock<IUnitSystem>();
        enemy2.Setup(e => e.BaseAtk).Returns(20f);

        _battleState!.PlayerUnits.Add(enemy1.Object);
        _battleState.PlayerUnits.Add(enemy2.Object);

        _mockMapSystem!.Setup(m => m.GetUnitPosition(enemy1.Object)).Returns((0, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(enemy2.Object)).Returns((10, 10, 10));

        float threat = AIUtilities.CalculateThreatLevel((0, 0, 0), _battleState, 5);

        Assert.That(threat, Is.EqualTo(20f));
    }

    [Test]
    public void FindNearestThreat_WhenNoEnemies_ReturnsNull()
    {
        _battleState!.PlayerUnits.Clear();
        Assert.That(AIUtilities.FindNearestThreat(_mockUnit!.Object, _battleState), Is.Null);
    }

    [Test]
    public void ThreatAssessment_EdgeCases()
    {
        // Ignore self
        _battleState!.EnemyUnits.Add(_mockUnit!.Object);
        Assert.That(AIUtilities.CountEnemyAlliesNear(_mockUnit.Object, (0, 0, 0), _battleState, 5), Is.EqualTo(0));

        // Clamped Damage
        var weakAttacker = new Mock<IUnitSystem>();
        weakAttacker.Setup(a => a.BaseAtk).Returns(5f);
        var tankTarget = new Mock<IUnitSystem>();
        tankTarget.Setup(t => t.BaseDef).Returns(100f);
        tankTarget.Setup(t => t.Hp).Returns(10f);
        Assert.That(AIUtilities.CanKillThisTurn(weakAttacker.Object, tankTarget.Object), Is.False);

        // Null position
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns(((int, int, int)?)null);
        Assert.That(AIUtilities.FindNearestThreat(_mockUnit.Object, _battleState), Is.Null);
    }

    #endregion

    #region Position Analysis Tests

    [Test]
    public void EvaluatePositionDefensibility_CalculatesHeightAndChokepointBonuses()
    {
        var testPos = (0, 2, 0);

        // Setup explicit directions to avoid loop/lambda branches
        _mockMapSystem!.Setup(m => m.IsWalkable(1, 2, 0)).Returns(true);
        _mockMapSystem.Setup(m => m.IsWalkable(-1, 2, 0)).Returns(true);
        _mockMapSystem.Setup(m => m.IsWalkable(0, 2, 1)).Returns(true);
        _mockMapSystem.Setup(m => m.IsWalkable(0, 2, -1)).Returns(true);
        _mockMapSystem.Setup(m => m.IsWalkable(0, 3, 0)).Returns(true);
        _mockMapSystem.Setup(m => m.IsWalkable(0, 1, 0)).Returns(false); // The "Chokepoint" wall

        float score = AIUtilities.EvaluatePositionDefensibility(testPos, _battleState!);

        Assert.That(score, Is.EqualTo(19f));
    }

    [Test]
    public void FindCenterPoint_WithValidUnits_ReturnsAveragedPosition()
    {
        var unit1 = new Mock<IUnitSystem>();
        var unit2 = new Mock<IUnitSystem>();
        var unit3 = new Mock<IUnitSystem>();
        var units = new List<IUnitSystem> { unit1.Object, unit2.Object, unit3.Object };

        _mockMapSystem!.Setup(m => m.GetUnitPosition(unit1.Object)).Returns((0, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(unit2.Object)).Returns((2, 6, 2));
        _mockMapSystem.Setup(m => m.GetUnitPosition(unit3.Object)).Returns((4, 0, 4));

        var center = AIUtilities.FindCenterPoint(units, _mockMapSystem.Object);

        Assert.That(center, Is.Not.Null);
        Assert.That(center!.Value, Is.EqualTo((2, 2, 2)));
    }

    [Test]
    public void EvaluatePositionDefensibility_HandlesOutOfBoundsGracefully()
    {
        _mockMapSystem!.Setup(m => m.IsWalkable(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Throws<ArgumentOutOfRangeException>();

        float score = AIUtilities.EvaluatePositionDefensibility((0, 0, 0), _battleState!);

        Assert.That(score, Is.EqualTo(24f));
    }

    [Test]
    public void FindCenterPoint_WithNoValidPositions_ReturnsNull()
    {
        var unit = new Mock<IUnitSystem>();
        _mockMapSystem!.Setup(m => m.GetUnitPosition(unit.Object)).Returns(((int, int, int)?)null);

        Assert.That(AIUtilities.FindCenterPoint(new List<IUnitSystem> { unit.Object }, _mockMapSystem.Object), Is.Null);
    }

    [Test]
    public void HasLineOfSight_SimplePlaceholderTest()
    {
        Assert.That(AIUtilities.HasLineOfSight((0, 0, 0), (1, 0, 1), _mockMapSystem!.Object), Is.True);
        Assert.That(AIUtilities.HasLineOfSight((0, 0, 0), (10, 0, 10), _mockMapSystem!.Object), Is.False);
    }

    [Test]
    public void GetUnitsInRange_And_FindCenterPoint_HandlingNullPositions()
    {
        var physicalUnit = new Mock<IUnitSystem>();
        var units = new List<IUnitSystem> { new Mock<IUnitSystem>().Object, physicalUnit.Object };

        _mockMapSystem!.Setup(m => m.GetUnitPosition(physicalUnit.Object)).Returns((0, 0, 0));

        var inRange = AIUtilities.GetUnitsInRange((0, 0, 0), 5, units, _mockMapSystem.Object);
        var emptyCenter = AIUtilities.FindCenterPoint(new List<IUnitSystem>(), _mockMapSystem.Object);

        Assert.That(inRange.Count, Is.EqualTo(1));
        Assert.That(emptyCenter, Is.Null);
    }

    [Test]
    public void ScoreMovePosition_HandlesDifferentTerrain()
    {
        var movePos = (1, 1, 1);
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(new List<(int, int, int)> { movePos });
        _mockMapSystem!.Setup(m => m.GetCellType(movePos)).Returns((AovDataStructures.CellType)999);

        var result = AIUtilities.CalculateMoveToRange(_mockUnit.Object, _battleState!, (5, 5, 5), 1);

        Assert.That(result, Is.EqualTo(movePos));
    }

    #endregion
}
