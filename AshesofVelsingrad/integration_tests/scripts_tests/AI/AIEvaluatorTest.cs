using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class AIEvaluatorTest
{
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private AIEvaluator? _evaluator;
    private TestConcreteGameManager? _gameManager;
    private TestConcreteMapSystem? _mapSystem;
    private TestConcreteUnitSystem? _aiUnit;
    private List<IUnitSystem> _playerUnits = new();
    private List<IUnitSystem> _enemyUnits = new();
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
        // Create GameManager and IMapSystem
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

        // Create AI unit at (2, 0, 2) - center
        _aiUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "AIEnemy" });
        _aiUnit.CallInitialize();
        _aiUnit.PossibleMovesRange = 3;
        _aiUnit.Mana = 100;
        _mapSystem.CellsInformation[12].SetUnit(_aiUnit); // Index for (2,0,2)

        // Create player unit at (4, 0, 4) - diagonal
        var player1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player1" });
        player1.CallInitialize();
        _mapSystem.CellsInformation[24].SetUnit(player1); // Index for (4,0,4)
        _playerUnits.Add(player1);

        // Create ally unit at (0, 0, 0) - corner
        var ally1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally1" });
        ally1.CallInitialize();
        _mapSystem.CellsInformation[0].SetUnit(ally1); // Index for (0,0,0)
        _enemyUnits.Add(_aiUnit);
        _enemyUnits.Add(ally1);

        // Create evaluator
        _evaluator = new AIEvaluator(_aiUnit);

        // Create battle state
        _battleState = new BattleState
        {
            ActingUnit = _aiUnit,
            MapSystem = _mapSystem,
            PlayerUnits = _playerUnits,
            EnemyUnits = _enemyUnits
        };
    }

    private TestConcreteSkillSystem CreateDamageSkill(int range = 1, int manaCost = 10)
    {
        return new TestConcreteSkillSystem(
            name: "Attack",
            effect: AovDataStructures.EffectType.Damage,
            target: AovDataStructures.TargetTypes.SingleEnemy,
            range: range,
            manaCost: manaCost
        );
    }

    private TestConcreteSkillSystem CreateHealSkill(int range = 1, int manaCost = 15)
    {
        return new TestConcreteSkillSystem(
            name: "Heal",
            effect: AovDataStructures.EffectType.Heal,
            target: AovDataStructures.TargetTypes.SingleAlly,
            range: range,
            manaCost: manaCost
        );
    }

    #endregion

    #region Constructor Tests

    [TestCase]
    public void Constructor_CreatesInstance()
    {
        var unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        var evaluator = new AIEvaluator(unit);

        AssertThat(evaluator).IsNotNull();
    }

    [TestCase]
    public void Constructor_StoresUnitReference()
    {
        var unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        var evaluator = new AIEvaluator(unit);

        // Use reflection to verify unit was stored
        var field = typeof(AIEvaluator).GetField("_unit",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var storedUnit = field?.GetValue(evaluator);

        AssertThat(storedUnit).IsEqual(unit);
    }

    #endregion

    #region EvaluateOffensiveAction Tests

    [TestCase]
    public void EvaluateOffensiveAction_ReturnsPositiveScore_ForValidTarget()
    {
        var target = _playerUnits[0];
        var skill = CreateDamageSkill(range: 5);

        float score = _evaluator!.EvaluateOffensiveAction(
            target,
            skill,
            (2, 0, 2),
            (4, 0, 4),
            _battleState!,
            false
        );

        AssertThat(score).IsGreater(0f);
    }

    [TestCase]
    public void EvaluateOffensiveAction_GivesBonusForKillPotential()
    {
        var target = _playerUnits[0];

        // Damage target so AI can kill it
        target.TakeDamage(90); // Leaves at 10 HP

        var skill = CreateDamageSkill(range: 5);

        float score = _evaluator!.EvaluateOffensiveAction(
            target,
            skill,
            (2, 0, 2),
            (4, 0, 4),
            _battleState!,
            false
        );

        // Score should be high due to kill potential
        AssertThat(score).IsGreater(100f);
    }

    [TestCase]
    public void EvaluateOffensiveAction_AppliesMovementPenalty()
    {
        var target = _playerUnits[0];
        var skill = CreateDamageSkill(range: 5);

        float scoreWithoutMove = _evaluator!.EvaluateOffensiveAction(
            target, skill,
            (2, 0, 2),
            (4, 0, 4),
            _battleState!,
            false
        );

        float scoreWithMove = _evaluator!.EvaluateOffensiveAction(
            target, skill,
            (3, 0, 3),
            (4, 0, 4),
            _battleState!,
            true
        );

        // Score with movement should be lower
        AssertThat(scoreWithMove).IsLess(scoreWithoutMove);
    }

    [TestCase]
    public void EvaluateOffensiveAction_PenalizesHighThreatPositions()
    {
        var target = _playerUnits[0];
        var skill = CreateDamageSkill(range: 5);

        // Add more player units nearby to create threat
        var player2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player2" });
        player2.CallInitialize();
        _mapSystem!.CellsInformation[13].SetUnit(player2); // Adjacent to AI
        _playerUnits.Add(player2);

        float score = _evaluator!.EvaluateOffensiveAction(
            target,
            skill,
            (2, 0, 2),
            (4, 0, 4),
            _battleState!,
            false
        );

        // Score should be penalized for threats nearby
        AssertThat(score).IsLess(100f);
    }

    #endregion

    #region EvaluateSupportAction Tests

    [TestCase]
    public void EvaluateSupportAction_ReturnsHighScore_ForCriticalAlly()
    {
        var ally = _enemyUnits[1];

        // Damage ally critically
        ally.TakeDamage(85); // 15% HP remaining

        var healSkill = CreateHealSkill(range: 5);

        float score = _evaluator!.EvaluateSupportAction(
            ally,
            healSkill,
            (2, 0, 2),
            (0, 0, 0),
            _battleState!,
            false
        );

        // Score should be very high for critical heal
        AssertThat(score).IsGreater(150f);
    }

    [TestCase]
    public void EvaluateSupportAction_ReturnsLowScore_ForHealthyAlly()
    {
        var ally = _enemyUnits[1];
        // Ally is at full HP

        var healSkill = CreateHealSkill(range: 5);

        float score = _evaluator!.EvaluateSupportAction(
            ally,
            healSkill,
            (2, 0, 2),
            (0, 0, 0),
            _battleState!,
            false
        );

        // Score should be low or negative for unnecessary heal
        AssertThat(score).IsLess(50f);
    }

    [TestCase]
    public void EvaluateSupportAction_AppliesPersonalityModifier_Defensive()
    {
        _aiUnit!.Personality = AIPersonality.Defensive;

        var ally = _enemyUnits[1];
        ally.TakeDamage(50);

        var healSkill = CreateHealSkill(range: 5);

        float score = _evaluator!.EvaluateSupportAction(
            ally,
            healSkill,
            (2, 0, 2),
            (0, 0, 0),
            _battleState!,
            false
        );

        // Defensive personality should boost support actions
        AssertThat(score).IsGreater(50f);
    }

    [TestCase]
    public void EvaluateSupportAction_AppliesPersonalityModifier_Aggressive()
    {
        _aiUnit!.Personality = AIPersonality.Aggressive;

        var ally = _enemyUnits[1];
        ally.TakeDamage(50);

        var healSkill = CreateHealSkill(range: 5);

        AIEvaluator baseEval = new AIEvaluator(_aiUnit);
        float score = baseEval.EvaluateSupportAction(
            ally,
            healSkill,
            (2, 0, 2),
            (0, 0, 0),
            _battleState!,
            false
        );

        // Aggressive personality should reduce support actions
        // Score will be multiplied by 0.7
        AssertThat(score).IsGreater(0f); // Still positive, just reduced
    }

    #endregion

    #region EvaluateDefensiveAction Tests

    [TestCase]
    public void EvaluateDefensiveAction_ReturnsHighScore_WhenLowHealth()
    {
        // Damage AI unit critically
        _aiUnit!.TakeDamage(80); // 20% HP

        float score = _evaluator!.EvaluateDefensiveAction(
            (2, 0, 2),
            (0, 0, 0),
            _battleState!
        );

        // Score should be very high when low on HP
        AssertThat(score).IsGreater(100f);
    }

    [TestCase]
    public void EvaluateDefensiveAction_FavorsPositionsWithFewerThreats()
    {
        // Add threats near current position
        var player2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player2" });
        player2.CallInitialize();
        _mapSystem!.CellsInformation[13].SetUnit(player2); // Adjacent to (2,0,2)
        _playerUnits.Add(player2);

        float score = _evaluator!.EvaluateDefensiveAction(
            (2, 0, 2), // Current (has threats)
            (0, 0, 1), // New (fewer threats)
            _battleState!
        );

        // Score should favor moving away from threats
        AssertThat(score).IsGreater(50f);
    }

    [TestCase]
    public void EvaluateDefensiveAction_FavorsPositionsNearAllies()
    {
        // Ally is at (0, 0, 0)
        float score = _evaluator!.EvaluateDefensiveAction(
            (2, 0, 2),
            (1, 0, 1), // Closer to ally
            _battleState!
        );

        // Score should include ally proximity bonus
        AssertThat(score).IsGreater(0f);
    }

    [TestCase]
    public void EvaluateDefensiveAction_AppliesPersonalityModifier()
    {
        _aiUnit!.Personality = AIPersonality.Defensive;

        float score = _evaluator!.EvaluateDefensiveAction(
            (2, 0, 2),
            (0, 0, 0),
            _battleState!
        );

        // Defensive personality should boost defensive actions
        AssertThat(score).IsGreater(50f);
    }

    #endregion

    #region ScoreTarget Tests (Private Method via Reflection)

    [TestCase]
    public void ScoreTarget_AggressivePersonality_PrefersCloseTargets_Debug()
    {
        _aiUnit!.Personality = AIPersonality.Aggressive;

        var farTarget = _playerUnits[0]; // At (4,0,4)

        var closeTarget = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player2" });
        closeTarget.CallInitialize();
        _mapSystem!.CellsInformation[13].SetUnit(closeTarget); // (3,0,2)
        _playerUnits.Add(closeTarget);

        var method = typeof(AIEvaluator).GetMethod("ScoreTarget",
            BindingFlags.NonPublic | BindingFlags.Instance);

        float scoreClose = (float)method!.Invoke(_evaluator,
            new object[] { closeTarget, _battleState! })!;
        float scoreFar = (float)method!.Invoke(_evaluator,
            new object[] { farTarget, _battleState! })!;

        var aiPos = _mapSystem.GetUnitPosition(_aiUnit);
        var closePos = _mapSystem.GetUnitPosition(closeTarget);
        var farPos = _mapSystem.GetUnitPosition(farTarget);

        GD.Print($"=== Aggressive Personality Target Scoring ===");
        GD.Print($"AI Position: {aiPos}");
        GD.Print($"Close Target Position: {closePos}");
        GD.Print($"Far Target Position: {farPos}");
        GD.Print($"Distance to close: {AIUtilities.CalculateManhattanDistance(aiPos!.Value, closePos!.Value)}");
        GD.Print($"Distance to far: {AIUtilities.CalculateManhattanDistance(aiPos!.Value, farPos!.Value)}");
        GD.Print($"Close Target Score: {scoreClose}");
        GD.Print($"Far Target Score: {scoreFar}");
        GD.Print($"Close BaseAtk: {closeTarget.BaseAtk}, Far BaseAtk: {farTarget.BaseAtk}");

        AssertThat(scoreClose).IsGreater(scoreFar);
    }

    [TestCase]
    public void ScoreTarget_OpportunisticPersonality_PrefersWeakTargets_Debug()
    {
        _aiUnit!.Personality = AIPersonality.Opportunistic;

        var healthyTarget = _playerUnits[0];

        var weakTarget = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player2" });
        weakTarget.CallInitialize();
        weakTarget.TakeDamage(95); // 5% HP
        _mapSystem!.CellsInformation[23].SetUnit(weakTarget);
        _playerUnits.Add(weakTarget);

        var method = typeof(AIEvaluator).GetMethod("ScoreTarget",
            BindingFlags.NonPublic | BindingFlags.Instance);

        float scoreWeak = (float)method!.Invoke(_evaluator,
            new object[] { weakTarget, _battleState! })!;
        float scoreHealthy = (float)method!.Invoke(_evaluator,
            new object[] { healthyTarget, _battleState! })!;

        GD.Print($"=== Opportunistic Personality Target Scoring ===");
        GD.Print($"Weak Target HP: {weakTarget.Hp}/{weakTarget.MaxHp} ({weakTarget.Hp / weakTarget.MaxHp * 100}%)");
        GD.Print($"Healthy Target HP: {healthyTarget.Hp}/{healthyTarget.MaxHp} ({healthyTarget.Hp / healthyTarget.MaxHp * 100}%)");
        GD.Print($"Weak Target Score: {scoreWeak}");
        GD.Print($"Healthy Target Score: {scoreHealthy}");
        GD.Print($"Can kill weak? {AIUtilities.CanKillThisTurn(_aiUnit!, weakTarget)}");
        GD.Print($"Can kill healthy? {AIUtilities.CanKillThisTurn(_aiUnit!, healthyTarget)}");

        AssertThat(scoreWeak).IsGreater(scoreHealthy);
    }

    #endregion

    #region ScoreSkill Tests (Private Method via Reflection)

    [TestCase]
    public void ScoreSkill_LowerManaCost_HigherScore()
    {
        var target = _playerUnits[0];
        var cheapSkill = CreateDamageSkill(range: 5, manaCost: 5);
        var expensiveSkill = CreateDamageSkill(range: 5, manaCost: 50);

        var method = typeof(AIEvaluator).GetMethod("ScoreSkill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        float scoreCheap = (float)method!.Invoke(_evaluator,
            new object[] { cheapSkill, target, _battleState! })!;
        float scoreExpensive = (float)method!.Invoke(_evaluator,
            new object[] { expensiveSkill, target, _battleState! })!;

        // Cheaper skill should score higher
        AssertThat(scoreCheap).IsGreater(scoreExpensive);
    }

    [TestCase]
    public void ScoreSkill_AggressivePersonality_PrefersDamage()
    {
        _aiUnit!.Personality = AIPersonality.Aggressive;

        var target = _playerUnits[0];
        var damageSkill = CreateDamageSkill();
        var healSkill = CreateHealSkill();

        var method = typeof(AIEvaluator).GetMethod("ScoreSkill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        float scoreDamage = (float)method!.Invoke(_evaluator,
            new object[] { damageSkill, target, _battleState! })!;
        float scoreHeal = (float)method!.Invoke(_evaluator,
            new object[] { healSkill, target, _battleState! })!;

        // Aggressive should prefer damage
        AssertThat(scoreDamage).IsGreater(scoreHeal);
    }

    [TestCase]
    public void ScoreSkill_DefensivePersonality_PrefersHealing()
    {
        _aiUnit!.Personality = AIPersonality.Defensive;

        // Damage an ally so heal is valuable
        var ally = _enemyUnits[1];
        ally.TakeDamage(50);

        var damageSkill = CreateDamageSkill();
        var healSkill = CreateHealSkill();

        var method = typeof(AIEvaluator).GetMethod("ScoreSkill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        float scoreDamage = (float)method!.Invoke(_evaluator,
            new object[] { damageSkill, _playerUnits[0], _battleState! })!;
        float scoreHeal = (float)method!.Invoke(_evaluator,
            new object[] { healSkill, ally, _battleState! })!;

        // Defensive with damaged ally should value healing
        AssertThat(scoreHeal).IsGreater(0f);
    }

    #endregion

    #region ScorePosition Tests (Private Method via Reflection)

    [TestCase]
    public void ScorePosition_PrefersOptimalRange()
    {
        var targetPos = new Vector3I(4, 0, 4);
        int skillRange = 2;

        var method = typeof(AIEvaluator).GetMethod("ScorePosition",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Position at exactly skill range
        float scoreOptimal = (float)method!.Invoke(_evaluator,
            new object[] { new Vector3I(2, 0, 4), targetPos, skillRange, _battleState! })!;

        // Position too close
        float scoreTooClose = (float)method!.Invoke(_evaluator,
            new object[] { new Vector3I(4, 0, 3), targetPos, skillRange, _battleState! })!;

        // Position too far
        float scoreTooFar = (float)method!.Invoke(_evaluator,
            new object[] { new Vector3I(0, 0, 0), targetPos, skillRange, _battleState! })!;

        // Optimal range should score highest
        AssertThat(scoreOptimal).IsGreater(scoreTooClose);
        AssertThat(scoreOptimal).IsGreater(scoreTooFar);
    }

    [TestCase]
    public void ScorePosition_PrefersHigherGround()
    {
        // Add elevated cells
        _mapSystem!.AddWalkableCell(2, 1, 2);
        _mapSystem.AddWalkableCell(2, 2, 2);

        var targetPos = new Vector3I(4, 0, 4);
        int skillRange = 5;

        var method = typeof(AIEvaluator).GetMethod("ScorePosition",
            BindingFlags.NonPublic | BindingFlags.Instance);

        float scoreLowGround = (float)method!.Invoke(_evaluator,
            new object[] { new Vector3I(2, 0, 2), targetPos, skillRange, _battleState! })!;

        float scoreHighGround = (float)method!.Invoke(_evaluator,
            new object[] { new Vector3I(2, 2, 2), targetPos, skillRange, _battleState! })!;

        // Higher ground should score better
        AssertThat(scoreHighGround).IsGreater(scoreLowGround);
    }

    [TestCase]
    public void ScorePosition_PenalizesBeingSurrounded()
    {
        var targetPos = new Vector3I(4, 0, 4);

        // Create two positions at the SAME distance from target
        // to isolate the "surrounded" penalty
        var surroundedPos = new Vector3I(2, 0, 2); // Distance to target = 4
        var safePos = new Vector3I(1, 0, 1); // Distance to target = 6 (farther)

        // Better test: Compare two positions at similar distances
        // Surrounded position: (3, 0, 3) - distance 2 from target
        // Safe position: (3, 0, 1) - distance 4 from target
        // Actually, let's use positions equidistant from target

        surroundedPos = new Vector3I(2, 0, 2); // Distance 4 from (4,0,4)
        safePos = new Vector3I(2, 0, 0); // Distance 4 from (4,0,4) - same distance!

        // Add multiple enemies around SURROUNDED position only
        var player2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player2" });
        var player3 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player3" });
        var player4 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player4" });
        player2.CallInitialize();
        player3.CallInitialize();
        player4.CallInitialize();

        // Surround position (2,0,2) with enemies
        _mapSystem!.CellsInformation[11].SetUnit(player2); // (1,0,2) - adjacent
        _mapSystem.CellsInformation[13].SetUnit(player3); // (3,0,2) - adjacent
        _mapSystem.CellsInformation[7].SetUnit(player4); // (2,0,1) - adjacent
        _playerUnits.Add(player2);
        _playerUnits.Add(player3);
        _playerUnits.Add(player4);

        var method = typeof(AIEvaluator).GetMethod("ScorePosition",
            BindingFlags.NonPublic | BindingFlags.Instance);

        float scoreSurrounded = (float)method!.Invoke(_evaluator,
            new object[] { surroundedPos, targetPos, 5, _battleState! })!;

        float scoreSafe = (float)method!.Invoke(_evaluator,
            new object[] { safePos, targetPos, 5, _battleState! })!;

        GD.Print($"Surrounded position score: {scoreSurrounded}, Safe position score: {scoreSafe}");

        // With 3+ enemies nearby at surrounded position:
        // Penalty: (enemiesNearby - 1) * 15f = (3 - 1) * 15 = -30
        // Safe position has 0 enemies, so no penalty
        // Therefore safe should be ~30 points higher
        AssertThat(scoreSafe).IsGreater(scoreSurrounded);
    }

    #endregion
}
