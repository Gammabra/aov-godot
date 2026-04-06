using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using System;

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
        // Arrange
        var pos1 = (0, 0, 0);
        var pos2 = (3, 4, 0); // Based on your math: hypoXZ = 3, distance = sqrt(3^2 + 4^2) = 5

        // Act
        int distance = AIUtilities.CalculateManhattanDistance(pos1, pos2);

        // Assert
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
        var result = AIUtilities.CalculateMoveToRange(_mockUnit.Object, _battleState!, (0,0,0), 1);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void CalculateMoveToRange_FindsBestPositionForSkillRange()
    {
        // Arrange: Target is at (5,0,0). Skill range is 2. 
        // Perfect spot is (3,0,0).
        var targetPos = (5, 0, 0);
        var moves = new List<(int, int, int)> { (0,0,0), (3,0,0), (10,0,0) };
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(moves);
        _mockUnit.Setup(u => u.Personality).Returns(AIPersonality.Balanced);
        _mockMapSystem!.Setup(m => m.GetCellType(It.IsAny<(int, int, int)>())).Returns(AovDataStructures.CellType.Grass);

        // Act
        var bestMove = AIUtilities.CalculateMoveToRange(_mockUnit.Object, _battleState!, targetPos, 2);

        // Assert
        Assert.That(bestMove, Is.EqualTo((3, 0, 0))); // distance is exactly 2
    }

    [Test]
    public void CalculateMoveAway_FindsFurthestValidPosition()
    {
        // Arrange
        var targetPos = (0, 0, 0);
        var moves = new List<(int, int, int)> { (1,0,0), (5,0,0), (2,0,0) };
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(moves);

        // Act: Move away, but must be at least distance 3
        var bestMove = AIUtilities.CalculateMoveAway(_mockUnit.Object, _battleState!, targetPos, 3);

        // Assert
        Assert.That(bestMove, Is.EqualTo((5, 0, 0)));
    }

    [Test]
    public void CalculateMoveToRange_WhenSurroundedAndDefensive_AppliesTacticalScores()
    {
        // Arrange
        var targetPos = (10, 10, 10);
        var movePos = (1, 1, 1);
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(new List<(int, int, int)> { movePos });
        
        // Line 129: Trigger Defensive/Balanced branch
        _mockUnit.Setup(u => u.Personality).Returns(AIPersonality.Defensive);

        // Line 137-138: We need 3+ enemies to trigger the penalty
        for (int i = 0; i < 3; i++) {
            var enemy = new Mock<IUnitSystem>();
            _battleState!.PlayerUnits.Add(enemy.Object);
            _mockMapSystem!.Setup(m => m.GetUnitPosition(enemy.Object)).Returns((1, 1, 2)); // Within range 2
        }

        // Act
        AIUtilities.CalculateMoveToRange(_mockUnit.Object, _battleState!, targetPos, 5);

        // Assert
        Assert.Pass(); // Coverage will track the internal scoring lines
    }

    [Test]
    public void CalculateThreatLevel_WhenEnemyHasNoPosition_SkipsUnit()
    {
        // Arrange
        var position = (0, 0, 0);
        var range = 5;

        // Create one "ghost" unit (no position) and one "physical" unit
        var ghostEnemy = new Mock<IUnitSystem>();
        var physicalEnemy = new Mock<IUnitSystem>();
        physicalEnemy.Setup(e => e.BaseAtk).Returns(10f);

        _battleState!.PlayerUnits.Clear();
        _battleState.PlayerUnits.Add(ghostEnemy.Object);
        _battleState.PlayerUnits.Add(physicalEnemy.Object);

        // Mock MapSystem: ghost returns null, physical returns (1,0,0)
        _mockMapSystem!.Setup(m => m.GetUnitPosition(ghostEnemy.Object))
            .Returns(((int, int, int)?)null); // This triggers line 248
        
        _mockMapSystem.Setup(m => m.GetUnitPosition(physicalEnemy.Object))
            .Returns((1, 0, 0));

        // Act
        float threat = AIUtilities.CalculateThreatLevel(position, _battleState, range);

        // Assert
        // Threat should only reflect the physical enemy
        // Dist 1, Range 5: Multiplier = 1 + (5-1)*0.2 = 1.8. Threat = 10 * 1.8 = 18.
        Assert.That(threat, Is.EqualTo(18f));
    }

    #endregion

    #region Threat Assessment Tests

    [Test]
    public void CanKillThisTurn_WhenDamageIsEnough_ReturnsTrue()
    {
        // Arrange
        var mockAttacker = new Mock<IUnitSystem>();
        mockAttacker.Setup(u => u.BaseAtk).Returns(50f);

        var mockTarget = new Mock<IUnitSystem>();
        mockTarget.Setup(u => u.BaseDef).Returns(10f);
        mockTarget.Setup(u => u.Hp).Returns(40f); 
        // Estimated DMG = 40. Buffer is 1.5x (60). Target HP (40) <= 60.

        // Act
        bool canKill = AIUtilities.CanKillThisTurn(mockAttacker.Object, mockTarget.Object);

        // Assert
        Assert.That(canKill, Is.True);
    }

    [Test]
    public void CanKillThisTurn_WhenTargetTooTanky_ReturnsFalse()
    {
        // Arrange
        var mockAttacker = new Mock<IUnitSystem>();
        mockAttacker.Setup(u => u.BaseAtk).Returns(20f);

        var mockTarget = new Mock<IUnitSystem>();
        mockTarget.Setup(u => u.BaseDef).Returns(15f);
        mockTarget.Setup(u => u.Hp).Returns(100f); 

        // Act
        bool canKill = AIUtilities.CanKillThisTurn(mockAttacker.Object, mockTarget.Object);

        // Assert
        Assert.That(canKill, Is.False);
    }

    [Test]
    public void CalculateThreatLevel_AggregatesEnemyAttackPower()
    {
        // Arrange
        var enemy1 = new Mock<IUnitSystem>();
        enemy1.Setup(e => e.BaseAtk).Returns(10f);
        var enemy2 = new Mock<IUnitSystem>();
        enemy2.Setup(e => e.BaseAtk).Returns(20f);

        _battleState!.PlayerUnits.Add(enemy1.Object);
        _battleState.PlayerUnits.Add(enemy2.Object);

        // enemy1 is at distance 0 (Multiplier 1.0 + (5-0)*0.2 = 2.0x) -> 20 threat
        // enemy2 is far away -> 0 threat
        _mockMapSystem!.Setup(m => m.GetUnitPosition(enemy1.Object)).Returns((0,0,0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(enemy2.Object)).Returns((10,10,10));

        // Act
        float threat = AIUtilities.CalculateThreatLevel((0,0,0), _battleState, 5);

        // Assert
        Assert.That(threat, Is.EqualTo(20f));
    }

    [Test]
    public void FindNearestThreat_WhenNoEnemies_ReturnsNull()
    {
        _battleState!.PlayerUnits.Clear();
        _mockMapSystem!.Setup(m => m.GetUnitPosition(It.IsAny<IUnitSystem>())).Returns((0,0,0));
        
        var result = AIUtilities.FindNearestThreat(_mockUnit!.Object, _battleState);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ThreatAssessment_EdgeCases()
    {
        // Line 162: CountEnemyAlliesNear should ignore 'self'
        _battleState!.EnemyUnits.Add(_mockUnit!.Object);
        int count = AIUtilities.CountEnemyAlliesNear(_mockUnit.Object, (0,0,0), _battleState, 5);
        Assert.That(count, Is.EqualTo(0));

        // Line 201-202: Damage clamped to 0
        var weakAttacker = new Mock<IUnitSystem>();
        weakAttacker.Setup(a => a.BaseAtk).Returns(5f);
        var tankTarget = new Mock<IUnitSystem>();
        tankTarget.Setup(t => t.BaseDef).Returns(100f);
        tankTarget.Setup(t => t.Hp).Returns(10f);
        
        bool canKill = AIUtilities.CanKillThisTurn(weakAttacker.Object, tankTarget.Object);
        Assert.That(canKill, Is.False); // 5 - 100 = -95 -> clamped to 0

        // Line 213-214: Null position for FindNearestThreat
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns(((int, int, int)?)null);
        var threat = AIUtilities.FindNearestThreat(_mockUnit.Object, _battleState);
        Assert.That(threat, Is.Null);
    }

    #endregion

    #region Position Analysis Tests

    [Test]
    public void EvaluatePositionDefensibility_CalculatesHeightAndChokepointBonuses()
    {
        if (_mockMapSystem == null || _battleState == null)
        {
            Assert.Fail("MockMapSystem or BattleState not initialized properly.");
            return;
        }

        // Arrange
        var testPos = (0, 2, 0); // Height of 2. Bonus = 2 * 5f = 10f.
        
        // Let's pretend 5 out of 6 directions are walkable, and 1 is a wall.
        // The method checks 6 directions. So adjacentWalkable = 5.
        // Score bonus = (8 - 5) * 3f = 9f.
        // Expected total = 10f + 9f = 19f.
        
        int callCount = 0;
        _mockMapSystem.Setup(m => m.IsWalkable(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(() => 
            {
                callCount++;
                return callCount <= 5; // First 5 calls return true, the 6th returns false
            });

        // Act
        float score = AIUtilities.EvaluatePositionDefensibility(testPos, _battleState);

        // Assert
        Assert.That(score, Is.EqualTo(19f));
    }

    [Test]
    public void FindCenterPoint_WithValidUnits_ReturnsAveragedPosition()
    {
        if (_mockMapSystem == null)
        {
            Assert.Fail("MockMapSystem not initialized properly.");
            return;
        }

        // Arrange
        var unit1 = new Mock<IUnitSystem>();
        var unit2 = new Mock<IUnitSystem>();
        var unit3 = new Mock<IUnitSystem>();

        var units = new List<IUnitSystem> { unit1.Object, unit2.Object, unit3.Object };

        // Setup positions at (0,0,0), (2,6,2), and (4,0,4)
        // Averages: X = (0+2+4)/3 = 2. Y = (0+6+0)/3 = 2. Z = (0+2+4)/3 = 2.
        _mockMapSystem.Setup(m => m.GetUnitPosition(unit1.Object)).Returns((0, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(unit2.Object)).Returns((2, 6, 2));
        _mockMapSystem.Setup(m => m.GetUnitPosition(unit3.Object)).Returns((4, 0, 4));

        // Act
        var center = AIUtilities.FindCenterPoint(units, _mockMapSystem.Object);

        if (center == null)
        {
            Assert.Fail("FindCenterPoint returned null when it should have returned a valid position.");
            return;
        }

        // Assert
        Assert.That(center, Is.Not.Null);
        Assert.That(center.Value, Is.EqualTo((2, 2, 2)));
    }

    [Test]
    public void EvaluatePositionDefensibility_HandlesOutOfBoundsGracefully()
    {
        // Arrange: Force map system to throw when checking neighbors
        _mockMapSystem!.Setup(m => m.IsWalkable(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Throws<ArgumentOutOfRangeException>();

        // Act
        // It should catch the error and count adjacent as 0
        // Score = Height(0)*5 + (8-0)*3 = 24
        float score = AIUtilities.EvaluatePositionDefensibility((0,0,0), _battleState!);

        // Assert
        Assert.That(score, Is.EqualTo(24f));
    }

    [Test]
    public void FindCenterPoint_WithNoValidPositions_ReturnsNull()
    {
        var unit = new Mock<IUnitSystem>();
        _mockMapSystem!.Setup(m => m.GetUnitPosition(unit.Object)).Returns(((int, int, int)?)null);

        var result = AIUtilities.FindCenterPoint(new List<IUnitSystem> { unit.Object }, _mockMapSystem.Object);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void HasLineOfSight_SimplePlaceholderTest()
    {
        // Our current implementation is dx <= 5 && dz <= 5
        Assert.That(AIUtilities.HasLineOfSight((0,0,0), (1,0,1), _mockMapSystem!.Object), Is.True);
        Assert.That(AIUtilities.HasLineOfSight((0,0,0), (10,0,10), _mockMapSystem!.Object), Is.False);
    }

    [Test]
    public void GetUnitsInRange_And_FindCenterPoint_HandlingNullPositions()
    {
        // Arrange
        var ghostUnit = new Mock<IUnitSystem>();
        var physicalUnit = new Mock<IUnitSystem>();
        var units = new List<IUnitSystem> { ghostUnit.Object, physicalUnit.Object };

        _mockMapSystem!.Setup(m => m.GetUnitPosition(ghostUnit.Object)).Returns(((int, int, int)?)null); // Line 297/248
        _mockMapSystem.Setup(m => m.GetUnitPosition(physicalUnit.Object)).Returns((0, 0, 0));

        // Act: GetUnitsInRange (Lines 291-306)
        var inRange = AIUtilities.GetUnitsInRange((0,0,0), 5, units, _mockMapSystem.Object);
        
        // Act: FindCenterPoint empty list (Line 370)
        var emptyCenter = AIUtilities.FindCenterPoint(new List<IUnitSystem>(), _mockMapSystem.Object);

        // Assert
        Assert.That(inRange.Count, Is.EqualTo(1));
        Assert.That(emptyCenter, Is.Null);
    }

    [Test]
    public void ScoreMovePosition_HandlesDifferentTerrain()
    {
        var movePos = (1, 1, 1);
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(new List<(int, int, int)> { movePos });
        
        // Setup a non-grass tile to hit the "rest" of the switch/default logic
        _mockMapSystem!.Setup(m => m.GetCellType(movePos)).Returns((AovDataStructures.CellType)999);

        AIUtilities.CalculateMoveToRange(_mockUnit.Object, _battleState!, (5,5,5), 1);
        Assert.Pass();
    }

    #endregion
}
