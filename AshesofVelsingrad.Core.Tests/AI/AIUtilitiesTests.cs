using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;

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

    #endregion
}
