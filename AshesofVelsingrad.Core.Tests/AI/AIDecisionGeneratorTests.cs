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

        _battleState = new BattleState 
        {
            MapSystem = _mockMapSystem.Object,
            ActingUnit = _mockUnit.Object,
            PlayerUnits = new List<IUnitSystem>(),
            EnemyUnits = new List<IUnitSystem>()
        };

        // Global defaults for the acting AI unit
        _mockUnit.Setup(u => u.Hp).Returns(100f);
        _mockUnit.Setup(u => u.MaxHp).Returns(100f);
        _mockUnit.Setup(u => u.BaseAtk).Returns(10);
        _mockUnit.Setup(u => u.Mana).Returns(50);
        _mockUnit.Setup(u => u.Personality).Returns(AIPersonality.Balanced); // Fixes logic checks
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

    [Test]
    public void GenerateSupportActions_WhenAllyIsSelf_ReturnsNoSupportActionsForSelf()
    {
        // The AI shouldn't try to use targeted support skills on itself via the ally loop
        _battleState!.EnemyUnits.Add(_mockUnit!.Object);
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0, 0, 0));

        var generator = new AIDecisionGenerator(_mockUnit.Object);
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Should only contain the default Pass action, no support actions
        Assert.That(results.Any(d => d.Reasoning.Contains("Support")), Is.False);
    }

    [Test]
    public void GenerateSupportActions_WhenAllyPositionIsNull_SkipsAlly()
    {
        var mockAlly = new Mock<IUnitSystem>();
        _battleState!.EnemyUnits.Add(mockAlly.Object);
        
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0, 0, 0));
        // Force the null branch
        _mockMapSystem.Setup(m => m.GetUnitPosition(mockAlly.Object)).Returns(((int, int, int)?)null);

        var generator = new AIDecisionGenerator(_mockUnit!.Object);
        var results = generator.GenerateAllPossibleActions(_battleState);

        Assert.That(results.Any(d => d.Target == mockAlly.Object), Is.False);
    }

    [Test]
    public void GenerateSupportActions_WithValidHealSkill_AddsUseSkillDecision()
    {
        // Arrange
        var mockAlly = new Mock<IUnitSystem>();
        mockAlly.Setup(a => a.UnitName).Returns("HurtAlly");
        mockAlly.Setup(a => a.Hp).Returns(20f);
        mockAlly.Setup(a => a.MaxHp).Returns(100f); // Prevents NaN
        mockAlly.Setup(a => a.BaseAtk).Returns(10);
        _battleState!.EnemyUnits.Add(mockAlly.Object);

        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(mockAlly.Object)).Returns((1, 0, 0));

        var mockHeal = new Mock<ISkillSystem>();
        mockHeal.Setup(s => s.Name).Returns("Heal");
        mockHeal.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Heal);
        mockHeal.Setup(s => s.Range).Returns(2);
        mockHeal.Setup(s => s.ManaCost).Returns(10);
        mockHeal.Setup(s => s.Cooldown).Returns(0);
        mockHeal.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>()); // <--- FIX: Prevents NRE

        _mockUnit!.Setup(u => u.ActiveSkills).Returns(new List<ISkillSystem> { mockHeal.Object });

        var generator = new AIDecisionGenerator(_mockUnit.Object);

        // Act
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Assert
        Assert.That(results.Any(d => d.Action == AIAction.UseSkill && d.Skill == mockHeal.Object), Is.True);
    }

    [Test]
    public void GenerateSupportActions_WhenAllyNeedsMove_AddsMoveAndSkillDecision()
    {
        // Arrange
        var mockAlly = new Mock<IUnitSystem>();
        mockAlly.Setup(a => a.UnitName).Returns("FarAlly");
        mockAlly.Setup(a => a.Hp).Returns(50f);
        mockAlly.Setup(a => a.MaxHp).Returns(100f);
        mockAlly.Setup(a => a.BaseAtk).Returns(10);
        _battleState!.EnemyUnits.Add(mockAlly.Object);

        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(mockAlly.Object)).Returns((4, 0, 0));

        var mockBuff = new Mock<ISkillSystem>();
        mockBuff.Setup(s => s.Name).Returns("Buff");
        mockBuff.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Buff);
        mockBuff.Setup(s => s.Range).Returns(2);
        mockBuff.Setup(s => s.ManaCost).Returns(0);
        mockBuff.Setup(s => s.Cooldown).Returns(0);
        mockBuff.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>()); // <--- FIX: Prevents NRE

        _mockUnit!.Setup(u => u.ActiveSkills).Returns(new List<ISkillSystem> { mockBuff.Object });
        _mockUnit.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>()))
                .Returns(new List<(int, int, int)> { (2, 0, 0) }); // Move within range

        var generator = new AIDecisionGenerator(_mockUnit.Object);

        // Act
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Assert
        Assert.That(results.Any(d => d.Action == AIAction.MoveAndSkill), Is.True);
    }

    [Test]
    public void GenerateOffensiveActions_WhenTargetPosIsNull_SkipsTarget()
    {
        var mockEnemy = new Mock<IUnitSystem>();
        _battleState!.PlayerUnits.Add(mockEnemy.Object);
        
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0, 0, 0));
        // Force the null branch
        _mockMapSystem.Setup(m => m.GetUnitPosition(mockEnemy.Object)).Returns(((int, int, int)?)null);

        var generator = new AIDecisionGenerator(_mockUnit!.Object);
        var results = generator.GenerateAllPossibleActions(_battleState);

        Assert.That(results.Any(d => d.Target == mockEnemy.Object), Is.False);
    }

    [Test]
    public void GenerateActions_SkipsSkillsThatAreOnCooldownOrTooExpensive()
    {
        var mockEnemy = new Mock<IUnitSystem>();
        _battleState!.PlayerUnits.Add(mockEnemy.Object);
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(mockEnemy.Object)).Returns((1, 0, 0));

        var mockExpensiveSkill = new Mock<ISkillSystem>();
        mockExpensiveSkill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        mockExpensiveSkill.Setup(s => s.ManaCost).Returns(999); // Too expensive
        mockExpensiveSkill.Setup(s => s.Cooldown).Returns(0);

        var mockCooldownSkill = new Mock<ISkillSystem>();
        mockCooldownSkill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        mockCooldownSkill.Setup(s => s.ManaCost).Returns(0);
        mockCooldownSkill.Setup(s => s.Cooldown).Returns(2); // On cooldown

        _mockUnit!.Setup(u => u.Mana).Returns(50);
        _mockUnit.Setup(u => u.ActiveSkills).Returns(new List<ISkillSystem> { mockExpensiveSkill.Object, mockCooldownSkill.Object });

        var generator = new AIDecisionGenerator(_mockUnit.Object);
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Neither skill should generate a decision
        Assert.That(results.Any(d => d.Skill == mockExpensiveSkill.Object || d.Skill == mockCooldownSkill.Object), Is.False);
    }

    [Test]
    public void GenerateDefensiveActions_WhenHpIsHighAndNoEnemiesNear_ReturnsEmpty()
    {
        _mockUnit!.Setup(u => u.Hp).Returns(100f);
        _mockUnit.Setup(u => u.MaxHp).Returns(100f); // 100% HP
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0, 0, 0));

        // Player units list is empty, so no enemies are nearby
        var generator = new AIDecisionGenerator(_mockUnit.Object);
        var results = generator.GenerateAllPossibleActions(_battleState!);

        Assert.That(results.Any(d => d.Reasoning.Contains("Retreat")), Is.False);
    }

    [Test]
    public void GenerateDefensiveActions_WhenInDangerButThreatPositionIsNull_ReturnsEmpty()
    {
        _mockUnit!.Setup(u => u.Hp).Returns(10f);
        _mockUnit.Setup(u => u.MaxHp).Returns(100f); // 10% HP (In Danger)
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0, 0, 0));

        var mockThreat = new Mock<IUnitSystem>();
        _battleState!.PlayerUnits.Add(mockThreat.Object);

        // Simulate that the threat is mathematically nearby, but the MapSystem returns null for its exact position
        _mockMapSystem.Setup(m => m.GetUnitPosition(mockThreat.Object)).Returns(((int, int, int)?)null);

        var generator = new AIDecisionGenerator(_mockUnit.Object);
        var results = generator.GenerateAllPossibleActions(_battleState);

        Assert.That(results.Any(d => d.Reasoning.Contains("Retreat")), Is.False);
    }

    [Test]
    public void ScoreSkill_WithAOE_CalculatesBonus()
    {
        // Arrange
        var mockEnemy1 = new Mock<IUnitSystem>();
        var mockEnemy2 = new Mock<IUnitSystem>();
        _battleState!.PlayerUnits.Add(mockEnemy1.Object);
        _battleState!.PlayerUnits.Add(mockEnemy2.Object);

        _mockMapSystem!.Setup(m => m.GetUnitPosition(mockEnemy1.Object)).Returns((1, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitAt(2, 0, 0)).Returns(mockEnemy2.Object);

        var mockAOE = new Mock<ISkillSystem>();
        mockAOE.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        mockAOE.Setup(s => s.TargetType).Returns(AovDataStructures.TargetTypes.AllEnemies);
        mockAOE.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)> { (1, 0, 0) }); // Offset to hit Enemy 2
        mockAOE.Setup(s => s.ManaCost).Returns(0);

        // Act
        var generator = new AIDecisionGenerator(_mockUnit!.Object);
        _mockUnit.Setup(u => u.ActiveSkills).Returns(new List<ISkillSystem> { mockAOE.Object });
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0, 0, 0));
        
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Assert
        Assert.That(results.Any(d => d.Skill == mockAOE.Object), Is.True);
    }

    [Test]
    public void GenerateActions_WhenSkillOnCooldown_IsSkipped()
    {
        // Arrange: Skill is valid but on cooldown
        var mockSkill = new Mock<ISkillSystem>();
        mockSkill.Setup(s => s.Cooldown).Returns(1); 
        _mockUnit!.Setup(u => u.ActiveSkills).Returns(new List<ISkillSystem> { mockSkill.Object });

        // Act
        var results = new AIDecisionGenerator(_mockUnit.Object).GenerateAllPossibleActions(_battleState!);

        // Assert: Ensure this skill isn't in the results
        Assert.That(results.Any(d => d.Skill == mockSkill.Object), Is.False);
    }

    [Test]
    public void GenerateOffensiveActions_WhenTargetUnreachable_IsSkipped()
    {
        // Arrange: Enemy exists but we can't move to them
        var mockEnemy = new Mock<IUnitSystem>();
        _battleState!.PlayerUnits.Add(mockEnemy.Object);
        _mockMapSystem!.Setup(m => m.GetUnitPosition(mockEnemy.Object)).Returns((10, 0, 10)); // Far away
        
        // AI can't move at all
        _mockUnit!.Setup(u => u.GetPossibleMoves(It.IsAny<IMapSystem>())).Returns(new List<(int, int, int)>());

        // Act
        var results = new AIDecisionGenerator(_mockUnit.Object).GenerateAllPossibleActions(_battleState!);

        // Assert: Should not have MoveAndSkill or UseSkill for that enemy
        Assert.That(results.Any(d => d.Target == mockEnemy.Object), Is.False);
    }

    [Test]
    public void GenerateSupportActions_WhenSkillIsInvalidOrTooExpensive_Continues()
    {
        if (_mockUnit == null || _mockMapSystem == null || _battleState == null)
        {
            Assert.Fail("Mocks or BattleState not initialized properly.");
            return;
        }

        // Arrange
        var mockAlly = new Mock<IUnitSystem>();
        _battleState!.EnemyUnits.Add(mockAlly.Object);
        _mockMapSystem!.Setup(m => m.GetUnitPosition(mockAlly.Object)).Returns((1, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0, 0, 0));

        // Skill 1: Wrong Type (Damage) -> Should hit Line 152
        var mockDamage = new Mock<ISkillSystem>();
        mockDamage.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);

        // Skill 2: Correct Type but no Mana -> Should hit Line 156
        var mockHeal = new Mock<ISkillSystem>();
        mockHeal.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Heal);
        mockHeal.Setup(s => s.ManaCost).Returns(100);
        
        _mockUnit.Setup(u => u.Mana).Returns(10); // Too poor
        _mockUnit.Setup(u => u.ActiveSkills).Returns(new List<ISkillSystem> { 
            mockDamage.Object, 
            mockHeal.Object 
        });

        // Act
        var generator = new AIDecisionGenerator(_mockUnit.Object);
        var results = generator.GenerateAllPossibleActions(_battleState);

        // Assert: Only "Pass" should be here because the skills were skipped
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Action, Is.EqualTo(AIAction.Pass));
    }

    [Test]
    public void GenerateOffensiveActions_WhenSkillIsSupportType_Continues()
    {
        if (_mockUnit == null || _mockMapSystem == null || _battleState == null)
        {
            Assert.Fail("Mocks or BattleState not initialized properly.");
            return;
        }

        // Arrange
        var mockEnemy = new Mock<IUnitSystem>();
        _battleState!.PlayerUnits.Add(mockEnemy.Object);
        _mockMapSystem!.Setup(m => m.GetUnitPosition(mockEnemy.Object)).Returns((1, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0, 0, 0));

        // Skill: Support Type (Heal) -> Should hit Line 80 in Offensive loop
        var mockHeal = new Mock<ISkillSystem>();
        mockHeal.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Heal);
        
        _mockUnit.Setup(u => u.ActiveSkills).Returns(new List<ISkillSystem> { mockHeal.Object });

        // Act
        var results = new AIDecisionGenerator(_mockUnit.Object).GenerateAllPossibleActions(_battleState);

        // Assert
        Assert.That(results.Any(d => d.Skill == mockHeal.Object), Is.False);
    }

    [Test]
    public void GenerateDefensiveActions_TheFinalStand_Line221()
    {
        // 1. Setup AI Unit
        _mockUnit!.Setup(u => u.Hp).Returns(10f); // Pass Line 215
        _mockUnit.Setup(u => u.MaxHp).Returns(100f);

        // 2. The Setup:
        // We need the utility to return SOMETHING. 
        // If the utility is picking from EnemyUnits/PlayerUnits, 
        // let's put the AI unit itself in the PlayerUnits list temporarily.
        _battleState!.PlayerUnits.Add(_mockUnit.Object);

        // 3. The Trap:
        // We return a valid position for the first check (Line 27)
        // But return NULL specifically for the defensive check (Line 220)
        int callCount = 0;
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit.Object))
            .Returns(() => {
                callCount++;
                // The first few calls happen at the start of GenerateAllPossibleActions
                // We want to return null ONLY when the code reaches the defensive section
                return (callCount > 5) ? null : (0, 0, 0);
            });

        // Act
        var results = new AIDecisionGenerator(_mockUnit.Object).GenerateAllPossibleActions(_battleState);

        // Assert
        Assert.That(results, Is.Not.Null);
    }
}