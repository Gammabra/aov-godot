using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Core.Tests.AI;

[TestFixture]
public class AIEvaluatorTests
{
    private Mock<IUnitSystem>? _mockUnit;
    private Mock<IMapSystem>? _mockMapSystem;
    private BattleState? _battleState;
    private AIEvaluator? _evaluator;

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

        // 1. Safe default for Personality
        _mockUnit.Setup(u => u.Personality).Returns(AIPersonality.Balanced);
        
        // 2. Safe default for Map Terrain
        _mockMapSystem.Setup(m => m.GetCellType(It.IsAny<(int, int, int)>()))
                      .Returns(AovDataStructures.CellType.Grass);

        // 3. Initialize Evaluator
        _evaluator = new AIEvaluator(_mockUnit.Object);
    }

    [Test]
    public void EvaluateDefensiveAction_WhenHpIsCriticallyLow_ReturnsVeryHighScore()
    {
        if (_mockUnit == null || _mockMapSystem == null || _battleState == null || _evaluator == null)
        {
            Assert.Fail("Mocks, BattleState, or Evaluator not initialized properly.");
            return;
        }

        // Arrange
        _mockUnit.Setup(u => u.Hp).Returns(20f);
        _mockUnit.Setup(u => u.MaxHp).Returns(100f); // 20% HP triggers the < 0.3f check (+100 score)
        
        var currentPos = (0, 0, 0);
        var newPos = (-2, 0, -2); // Retreating position
        
        // Act
        float score = _evaluator.EvaluateDefensiveAction(currentPos, newPos, _battleState);

        // Assert
        // Base(50) + LowHP(100) + ThreatDiff(0) + Allies(0) + Terrain/Personality defaults
        Assert.That(score, Is.GreaterThanOrEqualTo(150f)); 
    }
}
