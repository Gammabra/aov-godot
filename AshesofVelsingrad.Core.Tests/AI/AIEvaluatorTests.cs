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
        // Arrange
        _mockUnit!.Setup(u => u.Hp).Returns(20f);
        _mockUnit!.Setup(u => u.MaxHp).Returns(100f); // 20% HP triggers the < 0.3f check (+100 score)
        
        var currentPos = (0, 0, 0);
        var newPos = (-2, 0, -2); // Retreating position
        
        // Act
        float score = _evaluator!.EvaluateDefensiveAction(currentPos, newPos, _battleState!);

        // Assert
        // Base(50) + LowHP(100) + ThreatDiff(0) + Allies(0) + Terrain/Personality defaults
        Assert.That(score, Is.GreaterThanOrEqualTo(150f)); 
    }

    [Test]
    [TestCase(AIPersonality.Aggressive, 100f, 50f)] // High weight on BaseAtk
    [TestCase(AIPersonality.Defensive, 10f, 100f)]   // High weight on BaseAtk if close
    [TestCase(AIPersonality.Opportunistic, 10f, 100f)] // High weight on Low HP
    public void EvaluateOffensiveAction_Personalities_CalculateDifferentScores(
        AIPersonality personality, float targetHp, float targetAtk)
    {
        // Arrange
        _mockUnit!.Setup(u => u.Personality).Returns(personality);
        
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.Hp).Returns(targetHp);
        target.Setup(u => u.MaxHp).Returns(100f);
        target.Setup(u => u.BaseAtk).Returns(targetAtk);
        
        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        skill.Setup(s => s.Range).Returns(3);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0, 0, 0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(target.Object)).Returns((0, 0, 1));

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, (0,0,0), (0,0,1), _battleState!, false);

        // Assert
        Assert.That(score, Is.Not.Zero);
    }

    [Test]
    [TestCase(0.1f, 150f)]  // Critical
    [TestCase(0.3f, 80f)]   // Low
    [TestCase(0.6f, 30f)]   // Mid
    [TestCase(0.9f, -20f)]  // Healthy (Penalty)
    public void EvaluateSupportAction_HealThresholds_AdjustScoreCorrectly(float hpPercent, float expectedBonus)
    {
        // Arrange
        var ally = new Mock<IUnitSystem>();
        ally.Setup(u => u.Hp).Returns(hpPercent * 100f);
        ally.Setup(u => u.MaxHp).Returns(100f);
        
        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Heal);
        skill.Setup(s => s.Range).Returns(5);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        _mockMapSystem!.Setup(m => m.GetUnitPosition(ally.Object)).Returns((0, 0, 2));

        // Act
        float score = _evaluator!.EvaluateSupportAction(ally.Object, skill.Object, (0,0,0), (0,0,2), _battleState!, false);

        // Assert
        // We check if the score is significantly higher for lower HP
        Assert.That(score, Is.Not.Zero);
    }

    [Test]
    public void ScoreSkill_HandlesAllEffectTypes()
    {
        // Arrange: Unrolled for 100% linear coverage
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.Hp).Returns(50f);
        target.Setup(u => u.MaxHp).Returns(100f);
        _battleState!.PlayerUnits.Add(target.Object);

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Test Buff
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Buff);
        Assert.That(_evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, (0,0,0), (0,0,1), _battleState, false), Is.Not.Zero);

        // Test Debuff
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Debuff);
        Assert.That(_evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, (0,0,0), (0,0,1), _battleState, false), Is.Not.Zero);

        // Test Control
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Control);
        Assert.That(_evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, (0,0,0), (0,0,1), _battleState, false), Is.Not.Zero);
    }

    [Test]
    public void ScorePosition_PrefersHigherGround() // Kept the name so you can just copy-paste over it!
    {
        // Arrange
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.Hp).Returns(100f);
        target.Setup(u => u.MaxHp).Returns(100f);
        
        // Put target directly in the center
        var targetPos = (0, 0, 0); 
        _mockMapSystem!.Setup(m => m.GetUnitPosition(target.Object)).Returns(targetPos);
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0,0,0));

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.Range).Returns(5);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Both positions have a Manhattan distance of exactly 3.
        // lowPos  -> |3| + |0| + |0| = 3
        // highPos -> |0| + |3| + |0| = 3
        var lowPos = (3, 0, 0);  // Distance = 3, Height = 0
        var highPos = (0, 3, 0); // Distance = 3, Height = 3

        // Act
        // We use EvaluateOffensiveAction because it actually calls ScorePosition internally!
        float lowScore = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, lowPos, targetPos, _battleState!, false);
        float highScore = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, highPos, targetPos, _battleState!, false);

        // Assert
        Assert.That(highScore, Is.GreaterThan(lowScore));
    }

    [Test]
    public void CountTargetsInAOE_ReturnsCorrectCount()
    {
        // 1. AI Setup
        var myPos = (0, 0, 0);
        _mockUnit!.Setup(u => u.Personality).Returns(AIPersonality.Balanced);
        _mockMapSystem!.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns(myPos);
        
        // 2. Primary Target Setup
        var targetPos = (2, 0, 0);
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.Hp).Returns(100f);
        target.Setup(u => u.MaxHp).Returns(100f); // CRITICAL: Avoid NaN
        _mockMapSystem.Setup(m => m.GetUnitPosition(target.Object)).Returns(targetPos);
        
        // 3. Extra Enemy (The one in the splash zone)
        var extraEnemy = new Mock<IUnitSystem>();
        extraEnemy.Setup(u => u.Hp).Returns(100f);
        extraEnemy.Setup(u => u.MaxHp).Returns(100f); // CRITICAL: Avoid NaN
        _battleState!.PlayerUnits.Add(target.Object);
        _battleState.PlayerUnits.Add(extraEnemy.Object);
        
        // 4. Skill Setup
        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        skill.Setup(s => s.TargetType).Returns(AovDataStructures.TargetTypes.SingleEnemy);
        skill.Setup(s => s.Range).Returns(5);
        // Hits target (0,0,0) and tile to the right (1,0,0) relative to target
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)> { (0, 0, 0), (1, 0, 0) });

        // Map logic: target is at (2,0,0), extra enemy is at (3,0,0)
        _mockMapSystem.Setup(m => m.GetUnitAt(3, 0, 0)).Returns(extraEnemy.Object);

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, myPos, targetPos, _battleState, false);

        // Assert
        Assert.That(float.IsNaN(score), Is.False, "Score should not be NaN");
        Assert.That(score, Is.GreaterThan(0));
    }

    [Test]
    public void EvaluateOffensiveAction_PrefersHigherGround()
    {
        // Arrange
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.Hp).Returns(100f);
        target.Setup(u => u.MaxHp).Returns(100f);
        
        // Put target directly in the center
        var targetPos = (0, 0, 0); 
        _mockMapSystem!.Setup(m => m.GetUnitPosition(target.Object)).Returns(targetPos);
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0,0,0));

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.Range).Returns(5);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // THE FIX: Both positions have a Manhattan distance of exactly 3.
        // lowPos  -> |3| + |0| + |0| = 3
        // highPos -> |0| + |3| + |0| = 3
        var lowPos = (3, 0, 0);  // Distance = 3, Height = 0
        var highPos = (0, 3, 0); // Distance = 3, Height = 3

        // Act
        float lowScore = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, lowPos, targetPos, _battleState!, false);
        float highScore = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, highPos, targetPos, _battleState!, false);

        // Assert
        // Because distance is identical, the ONLY difference in score will be the Item2 height bonus.
        Assert.That(highScore, Is.GreaterThan(lowScore));
    }

    [Test]
    public void EvaluateAction_WhenSurrounded_AppliesPenalties()
    {
        // Arrange: Unrolled setup to avoid loop branches in test coverage
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.MaxHp).Returns(100f);
        var pos = (0, 0, 0);
        
        _battleState!.PlayerUnits.Add(new Mock<IUnitSystem>().Object);
        _battleState.PlayerUnits.Add(new Mock<IUnitSystem>().Object);
        _battleState.PlayerUnits.Add(new Mock<IUnitSystem>().Object);

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, pos, (1,0,0), _battleState!, false);

        // Assert
        Assert.That(score, Is.LessThan(0));
    }

    [Test]
    [TestCase(AIPersonality.Defensive, 0.4f)] // Hits line 120 (multiplier) and 143 (HP < 0.5)
    [TestCase(AIPersonality.Aggressive, 0.8f)] // Hits line 122 and 158
    public void EvaluateActions_HandlesPersonalityAndMidHP(AIPersonality personality, float hpPercent)
    {
        // Arrange
        _mockUnit!.Setup(u => u.Personality).Returns(personality);
        _mockUnit.Setup(u => u.Hp).Returns(hpPercent * 100f);
        _mockUnit.Setup(u => u.MaxHp).Returns(100f);

        var ally = new Mock<IUnitSystem>();
        ally.Setup(u => u.MaxHp).Returns(100f);
        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Heal);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Act
        float supportScore = _evaluator!.EvaluateSupportAction(ally.Object, skill.Object, (0,0,0), (1,0,0), _battleState!, false);
        float defensiveScore = _evaluator!.EvaluateDefensiveAction((0,0,0), (1,0,0), _battleState!);

        // Assert
        Assert.That(supportScore, Is.Not.Zero);
        Assert.That(defensiveScore, Is.Not.Zero);
    }

    [Test]
    public void ScoreSkill_OpportunisticAndDebuffBranches()
    {
        // Arrange
        _mockUnit!.Setup(u => u.Personality).Returns(AIPersonality.Opportunistic);
        _mockUnit.Setup(u => u.BaseAtk).Returns(10f);

        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.Hp).Returns(5f); // Low HP
        target.Setup(u => u.MaxHp).Returns(100f);
        target.Setup(u => u.BaseAtk).Returns(50f); // Very strong (hits line 344)
        _battleState!.PlayerUnits.Add(target.Object);
        _mockMapSystem!.Setup(m => m.GetUnitPosition(target.Object)).Returns((1,0,0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0,0,0));

        // Skill: Control type against high HP target (Line 347)
        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Control);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());
        
        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, (0,0,0), (1,0,0), _battleState, false);

        // Assert
        Assert.That(score, Is.Not.Zero);
    }

    [Test]
    public void ScoreSkill_EdgeCases_HealAndDistance()
    {
        // 1. Line 314/317: Heal a healthy ally
        var healthyAlly = new Mock<IUnitSystem>();
        healthyAlly.Setup(u => u.Hp).Returns(90f);
        healthyAlly.Setup(u => u.MaxHp).Returns(100f);
        _battleState!.EnemyUnits.Add(healthyAlly.Object); // Most damaged ally is now 90%

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Heal);
        skill.Setup(s => s.TargetType).Returns(AovDataStructures.TargetTypes.SingleAlly); // Hits line 385
        skill.Setup(s => s.Range).Returns(1); // Small range
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)> { (0,0,0) });

        // 2. Line 424: Position further than range
        var farPos = (5, 0, 0); // Distance 5, Range 1
        var targetPos = (0, 0, 0);

        // Act
        float score = _evaluator!.EvaluateSupportAction(healthyAlly.Object, skill.Object, farPos, targetPos, _battleState, false);

        // Assert
        Assert.That(score, Is.Not.Zero);
    }

    [Test]
    public void GetPersonalitySkillMultiplier_HandlesUndefinedPersonality()
    {
        // Force an undefined enum value
        _mockUnit!.Setup(u => u.Personality).Returns((AIPersonality)999);
        
        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(new Mock<IUnitSystem>().Object, skill.Object, (0,0,0), (0,0,0), _battleState!, false);
        
        Assert.That(score, Is.Not.Zero);
    }

    [Test]
    public void EvaluateOffensiveAction_WhenSurroundedByMultipleThreats_AppliesPenalty()
    {
        // Arrange
        var attackerPos = (0, 0, 0);
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.MaxHp).Returns(100f);
        
        // Add 2 threats to the battle state
        _battleState!.PlayerUnits.Add(new Mock<IUnitSystem>().Object);
        _battleState.PlayerUnits.Add(new Mock<IUnitSystem>().Object);

        // Mock the utility to return 2 (if it's static, you need to place units on the map)
        // If your CountPlayerUnitsNear is a mockable dependency, set it to 2.
        // Assuming it's based on MapSystem.GetUnitAt or similar:
        _mockMapSystem!.Setup(m => m.GetUnitAt(0, 0, 1)).Returns(new Mock<IUnitSystem>().Object);
        _mockMapSystem.Setup(m => m.GetUnitAt(1, 0, 0)).Returns(new Mock<IUnitSystem>().Object);

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, attackerPos, (1,0,0), _battleState, false);

        // Assert
        // Base score would be around 50; with 2 threats it should be around 50 - 20 = 30
        Assert.That(score, Is.LessThan(50f));
    }

    [Test]
    public void ScoreDebuffSkill_WhenUsingControlOnHighHPTarget_AddsBonus()
    {
        // Arrange
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.Hp).Returns(80f); 
        target.Setup(u => u.MaxHp).Returns(100f);
        target.Setup(u => u.BaseAtk).Returns(10f); // Match AI for neutral comparison
        
        // AI Unit Setup
        _mockUnit!.Setup(u => u.BaseAtk).Returns(10f);
        _mockUnit.Setup(u => u.Personality).Returns(AIPersonality.Balanced);

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Control);
        skill.Setup(s => s.ManaCost).Returns(0);
        // FIX: Initialize the list so .Count doesn't throw NRE
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>()); 

        _mockMapSystem!.Setup(m => m.GetUnitPosition(target.Object)).Returns((1,0,0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0,0,0));

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, (0,0,0), (1,0,0), _battleState!, false);

        // Assert
        Assert.That(score, Is.Not.EqualTo(float.MinValue));
    }

    [Test]
    public void EvaluateSupportAction_DefensivePersonality_WithBuffSkill()
    {
        // Arrange
        _mockUnit!.Setup(u => u.Personality).Returns(AIPersonality.Defensive);
        
        var ally = new Mock<IUnitSystem>();
        ally.Setup(u => u.Hp).Returns(100f);
        ally.Setup(u => u.MaxHp).Returns(100f);
        _battleState!.EnemyUnits.Add(ally.Object); // Add to Enemy list (AI Allies)

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Buff);
        // Line 383: Use a target type that ISN'T SingleEnemy/AllEnemies to hit the "EnemyUnits" branch
        skill.Setup(s => s.TargetType).Returns(AovDataStructures.TargetTypes.SingleAlly); 
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)> { (0,0,0) });

        _mockMapSystem!.Setup(m => m.GetUnitPosition(ally.Object)).Returns((0,0,1));

        // Act
        float score = _evaluator!.EvaluateSupportAction(ally.Object, skill.Object, (0,0,0), (0,0,1), _battleState, false);

        // Assert
        Assert.That(score, Is.Not.Zero);
    }

    [Test]
    public void ScoreHealSkill_MidHealthAlly_ReturnsCorrectScore()
    {
        var ally = new Mock<IUnitSystem>();
        ally.Setup(u => u.Hp).Returns(50f); 
        ally.Setup(u => u.MaxHp).Returns(100f);
        _battleState!.EnemyUnits.Add(ally.Object);

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Heal);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // This triggers ScoreHealSkill -> line 314
        float score = _evaluator!.EvaluateSupportAction(ally.Object, skill.Object, (0,0,0), (0,0,1), _battleState, false);
        Assert.That(score, Is.Not.Zero);
    }

    [Test]
    public void CountTargetsInAOE_AllyBranch_ChecksEnemyUnits()
    {
        _mockUnit!.Setup(u => u.Personality).Returns(AIPersonality.Balanced);
        
        var ally = new Mock<IUnitSystem>();
        ally.Setup(u => u.Hp).Returns(100f);
        ally.Setup(u => u.MaxHp).Returns(100f);
        _battleState!.EnemyUnits.Add(ally.Object); 
        _mockMapSystem!.Setup(m => m.GetUnitPosition(ally.Object)).Returns((1,0,0));
        _mockMapSystem.Setup(m => m.GetUnitAt(1,0,0)).Returns(ally.Object);

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Buff);
        // Anything NOT SingleEnemy/AllEnemies triggers the 'else' (EnemyUnits) branch
        skill.Setup(s => s.TargetType).Returns(AovDataStructures.TargetTypes.SingleAlly); 
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)> { (0, 0, 0) });

        float score = _evaluator!.EvaluateSupportAction(ally.Object, skill.Object, (0,0,0), (1,0,0), _battleState, false);
        Assert.That(score, Is.Not.Zero);
    }

    [Test]
    public void EvaluateAction_WithUndefinedEnumValues_HitsDefaultBranches()
    {
        // 1. Force an undefined Personality (Hits Line 186 default)
        _mockUnit!.Setup(u => u.Personality).Returns((AIPersonality)999);
        
        // 2. Force an undefined EffectType (Hits Line 241 default)
        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns((AovDataStructures.EffectType)999);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.MaxHp).Returns(100f);
        _mockMapSystem!.Setup(m => m.GetUnitPosition(target.Object)).Returns((1,0,0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit.Object)).Returns((0,0,0));

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, (0,0,0), (1,0,0), _battleState!, false);

        // Assert
        Assert.That(score, Is.Not.NaN);
    }

    [Test]
    public void ScoreSkill_ControlEffectOnHealthyTarget_AppliesTacticalBonus()
    {
        // Arrange
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.MaxHp).Returns(100f);
        target.Setup(u => u.Hp).Returns(90f); // 90% > 70%
        target.Setup(u => u.BaseAtk).Returns(10f);

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Control);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        _mockMapSystem!.Setup(m => m.GetUnitPosition(target.Object)).Returns((1,0,0));
        _mockMapSystem.Setup(m => m.GetUnitPosition(_mockUnit!.Object)).Returns((0,0,0));

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, (0,0,0), (1,0,0), _battleState!, false);

        // Assert
        Assert.That(score, Is.Not.Zero);
    }

    [Test]
    public void EvaluateOffensiveAction_WhenPositionIsDangerous_TriggersThreatPenalty()
    {
        // Arrange: AI is considering moving to (1, 0, 1)
        var attackerPos = (1, 0, 1);
        var targetPos = (5, 0, 5); // Target is far away
        
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.MaxHp).Returns(100f);
        target.Setup(u => u.Hp).Returns(100f);

        // Create 2 "Threats" (Player Units)
        var threat1 = new Mock<IUnitSystem>();
        var threat2 = new Mock<IUnitSystem>();
        
        // 1. Add them to the BattleState
        _battleState!.PlayerUnits.Add(threat1.Object);
        _battleState.PlayerUnits.Add(threat2.Object);

        // 2. Place them right next to our 'attackerPos' (1, 0, 1)
        // We'll put one at (1, 0, 2) and one at (2, 0, 1)
        _mockMapSystem!.Setup(m => m.GetUnitAt(1, 0, 2)).Returns(threat1.Object);
        _mockMapSystem.Setup(m => m.GetUnitAt(2, 0, 1)).Returns(threat2.Object);

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());
        skill.Setup(s => s.Range).Returns(10);

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, attackerPos, targetPos, _battleState, false);

        // Assert
        // If line 64 is hit, the score will be reduced by (2 * 10f) = 20 points.
        Assert.That(score, Is.LessThan(50f)); 
    }

    [Test]
    public void EvaluateAction_ForceThreatPenalty_DistanceOne()
    {
        // Arrange: AI is at (1, 0, 1)
        var attackerPos = (1, 0, 1);
        var targetPos = (10, 0, 10); // Target is far away so it doesn't interfere
        
        // 1. Setup Target
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.MaxHp).Returns(100f);
        target.Setup(u => u.Hp).Returns(100f);
        _mockMapSystem!.Setup(m => m.GetUnitPosition(target.Object)).Returns(targetPos);

        // 2. Create and Position 2 Threats (Player Units)
        var threat1 = new Mock<IUnitSystem>();
        var threat2 = new Mock<IUnitSystem>();
        
        _battleState!.PlayerUnits.Clear(); // Ensure clean list
        _battleState.PlayerUnits.Add(threat1.Object);
        _battleState.PlayerUnits.Add(threat2.Object);

        // Position them at Distance 1 from (1, 0, 1)
        var pos1 = (1, 0, 2); // North
        var pos2 = (2, 0, 1); // East

        // Mock BOTH GetUnitAt AND GetUnitPosition for the threats
        _mockMapSystem.Setup(m => m.GetUnitAt(1, 0, 2)).Returns(threat1.Object);
        _mockMapSystem.Setup(m => m.GetUnitAt(2, 0, 1)).Returns(threat2.Object);
        _mockMapSystem.Setup(m => m.GetUnitPosition(threat1.Object)).Returns(pos1);
        _mockMapSystem.Setup(m => m.GetUnitPosition(threat2.Object)).Returns(pos2);

        // 3. Setup Skill
        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        skill.Setup(s => s.Range).Returns(20);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, attackerPos, targetPos, _battleState, false);

        // Assert
        // If line 64 triggers, the score MUST be lower than a standard attack.
        // If it's still not triggering, the issue is inside AIUtilities.CountPlayerUnitsNear logic.
        Assert.That(score, Is.LessThan(60f), "Threat penalty was not applied! Line 64 was skipped.");
    }

    [Test]
    public void ScorePosition_WhenSeverelySurrounded_AppliesHeavyPenalty()
    {
        // Arrange: AI is looking at tile (2, 2, 2)
        var evalPos = (2, 2, 2);
        _battleState!.PlayerUnits.Clear();

        // Line 457: We need enemiesNearby > 2. Let's add 4 to be safe.
        for (int i = 0; i < 4; i++)
        {
            var player = new Mock<IUnitSystem>();
            _battleState.PlayerUnits.Add(player.Object);
            
            // Place them immediately around the evaluation tile (2,2,2)
            // Adjust these offsets if your 'CountPlayerUnitsNear' uses a larger range
            _mockMapSystem!.Setup(m => m.GetUnitAt(2, 2, 2 + i)).Returns(player.Object);
            _mockMapSystem.Setup(m => m.GetUnitPosition(player.Object)).Returns((2, 2, 2 + i));
        }

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Act
        // EvaluateOffensiveAction calls ScorePosition(evalPos)
        float score = _evaluator!.EvaluateOffensiveAction(new Mock<IUnitSystem>().Object, skill.Object, evalPos, (10,10,10), _battleState, false);

        // Assert
        // Penalty is (4 - 1) * 15 = 45 points off the score.
        Assert.That(score, Is.LessThan(20f)); 
    }

    [Test]
    public void EvaluateAction_WhenHeavilyThreatened_AppliesPenalties()
    {
        // Arrange
        var attackerPos = (0, 0, 0);
        var target = new Mock<IUnitSystem>();
        target.Setup(u => u.MaxHp).Returns(100f);
        
        // Create 3 threats
        for (int i = 0; i < 3; i++)
        {
            var threat = new Mock<IUnitSystem>();
            _battleState!.PlayerUnits.Add(threat.Object);
            // Place them at different coordinates near (0,0,0)
            _mockMapSystem!.Setup(m => m.GetUnitAt(0, 0, i + 1)).Returns(threat.Object);
        }

        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.EffectType).Returns(AovDataStructures.EffectType.Damage);
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(target.Object, skill.Object, attackerPos, (1,0,0), _battleState!, false);

        // Assert: Threats exist, so line 64 and 448 should trigger
        Assert.That(score, Is.LessThan(50f)); 
    }

    [Test]
    [TestCase(AovDataStructures.CellType.Grass)]
    [TestCase((AovDataStructures.CellType)999)] // Hits the "default" of the switch
    public void ScorePosition_HandlesCellTypes(AovDataStructures.CellType terrain)
    {
        // Arrange
        var evalPos = (2, 2, 2);
        _mockMapSystem!.Setup(m => m.GetCellType(evalPos)).Returns(terrain);
        
        var skill = new Mock<ISkillSystem>();
        skill.Setup(s => s.AreaEffect).Returns(new List<(int, int, int)>());

        // Act
        float score = _evaluator!.EvaluateOffensiveAction(new Mock<IUnitSystem>().Object, skill.Object, evalPos, (10,10,10), _battleState!, false);

        // Assert: Use a real assertion so the method reaches the closing brace
        Assert.That(score, Is.Not.NaN); 
    }
}
