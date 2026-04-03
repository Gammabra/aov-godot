using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Moq;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Core.Tests.AI;

[TestFixture]
public class AIDecisionGeneratorTests
{
    private Mock<IUnitSystem>? _mockUnit;
    private Mock<IMapSystem>? _mockMapSystem;
    private BattleState? _battleState;

    [SetUp]
    public void SetUp()
    {
        _mockUnit = new Mock<IUnitSystem>();
        _mockMapSystem = new Mock<IMapSystem>();

        // Initialize BattleState with the mocked map system       
        _battleState = new BattleState 
        {
            MapSystem = _mockMapSystem.Object,
            ActingUnit = _mockUnit.Object,
            PlayerUnits = new List<IUnitSystem>(),
            EnemyUnits = new List<IUnitSystem>()
        };

        // Basic default setup to prevent AIEvaluator from throwing NullReferenceExceptions
        _mockUnit.Setup(u => u.Hp).Returns(100f);
        _mockUnit.Setup(u => u.MaxHp).Returns(100f);
        _mockUnit.Setup(u => u.Mana).Returns(50);
        _mockUnit.Setup(u => u.PossibleMovesRange).Returns(3);
        _mockUnit.Setup(u => u.ActiveSkills).Returns(new List<ISkillSystem>());
        _mockUnit.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>()))
            .Returns(new List<(int, int, int)> { (0, 0, 0), (-1, 0, -1) });
            
        _mockMapSystem.Setup(m => m.GetCellType(It.IsAny<(int, int, int)>()))
            .Returns(AovDataStructures.CellType.Grass);
    }

    [Test]
    public void GenerateAllPossibleActions_WhenUnitPositionIsNull_ReturnsEmptyList()
    {
        if (_mockUnit == null || _mockMapSystem == null || _battleState == null)
        {
            Assert.Fail("Mocks or BattleState not initialized properly.");
            return;
        }

        // Arrange
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns(((int, int, int)?)null);
        var generator = new AIDecisionGenerator(_mockUnit.Object);

        // Act
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Assert
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void GenerateAllPossibleActions_WithNoContext_AlwaysIncludesPassAction()
    {
        if (_mockUnit == null || _mockMapSystem == null || _battleState == null)
        {
            Assert.Fail("Mocks or BattleState not initialized properly.");
            return;
        }

        // Arrange
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0, 0, 0));
        var generator = new AIDecisionGenerator(_mockUnit.Object);

        // Act
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Assert
        Assert.That(results.Any(d => d.Action == AIAction.Pass), Is.True);
        var passAction = results.First(d => d.Action == AIAction.Pass);
        Assert.That(passAction.Score, Is.EqualTo(0f));
    }

    [Test]
    public void GenerateOffensiveActions_WhenEnemyIsInRange_AddsAttackDecision()
    {
        if (_mockUnit == null || _mockMapSystem == null || _battleState == null)
        {
            Assert.Fail("Mocks or BattleState not initialized properly.");
            return;
        }

        // Arrange
        var myPos = (0, 0, 0);
        var targetPos = (1, 1, 0); // Manhattan distance = 2
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns(myPos);

        // Setup Enemy
        var mockEnemy = new Mock<IUnitSystem>();
        mockEnemy.Setup(e => e.UnitName).Returns("TargetDummy");
        _battleState.PlayerUnits.Add(mockEnemy.Object);
        _mockMapSystem.Setup(m => m.GetUnitPosition(mockEnemy.Object)).Returns(targetPos);

        // Setup Damage Skill (Range 3 > Distance 2)
        var mockSkill = new Mock<ISkillSystem>();
        mockSkill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        mockSkill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());
        mockSkill.Setup(s => s.TargetType).Returns(AovDataStructures.TargetTypes.SingleEnemy);
        mockSkill.Setup(s => s.Range).Returns(3);
        mockSkill.Setup(s => s.ManaCost).Returns(5);
        mockSkill.Setup(s => s.Cooldown).Returns(0);
        
        // Ensure the AI unit has a Personality (otherwise _unit.Personality might be problematic)
        _mockUnit.Setup(u => u.Personality).Returns(AIPersonality.Balanced);
        _mockUnit.Setup(u => u.ActiveSkills).Returns(new List<ISkillSystem> { mockSkill.Object });

        var generator = new AIDecisionGenerator(_mockUnit.Object);

        // Act
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Assert
        Assert.That(results.Any(d => d.Action == AIAction.UseSkill), Is.True);
    }

    [Test]
    public void GenerateDefensiveActions_WhenHpIsLow_AddsRetreatDecision()
    {
        if (_mockUnit == null || _mockMapSystem == null || _battleState == null)
        {
            Assert.Fail("Mocks or BattleState not initialized properly.");
            return;
        }

        // 1. Arrange - Setup HP and Personality
        _mockUnit.Setup(u => u.Hp).Returns(10f); 
        _mockUnit.Setup(u => u.MaxHp).Returns(100f);
        _mockUnit.Setup(u => u.Personality).Returns(AIPersonality.Defensive);
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0, 0, 0));

        // 3. Setup a nearby threat
        var mockEnemy = new Mock<IUnitSystem>();
        _battleState.PlayerUnits.Add(mockEnemy.Object);
        _mockMapSystem.Setup(m => m.GetUnitPosition(mockEnemy.Object)).Returns((1, 0, 0));


        var generator = new AIDecisionGenerator(_mockUnit.Object);

        // Act
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Assert
        Assert.That(results.Any(d => d.Action == AIAction.Move && d.Reasoning.Contains("Retreat")), Is.True);
    }
}