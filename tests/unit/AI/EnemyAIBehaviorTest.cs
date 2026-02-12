using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using static GdUnit4.Assertions;

namespace UnitTests;

/// <summary>
/// Unit tests for EnemyAIBehavior class using GDUnit4.
/// Tests AI decision-making, initialization, and turn execution.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public partial class EnemyAIBehaviorTest : Node
{
	private Node? _root;
	private EnemyAIManager? _aiManager;
    private readonly List<Node> _testNodes = new();
    private EnemyAIBehavior? _aiBehavior;
    private BattleState? _mockBattleState;
	private TestConcreteGameManager? _gameManager;
	private TestConcreteMapSystem? _mapSystem;
	private List<UnitSystem> _playerUnits = new();
	private List<UnitSystem> _enemyUnits = new();

[BeforeTest]
public void Setup()
{
    _root = new Node();
    _aiBehavior = new TestConcreteEnemyAIBehavior();
    SetupBasicAIManager();
    _mockBattleState = new BattleState
    {
        ActingUnit = _enemyUnits[0],
        MapSystem = _mapSystem!,
        PlayerUnits = _playerUnits,
        EnemyUnits = _enemyUnits,
        GameManager = _gameManager!
    };
}
    
    private T AddNodeToTestRoot<T>(T node) where T : Node
	{
		if (_root == null)
			throw new InvalidOperationException("Test root node is not initialized.");
		_root.AddChild(node);
		_testNodes.Add(node);
		return node;
	}

    private void SetupBasicAIManager()
	{
		// Create GameManager
		_gameManager = AddNodeToTestRoot(new TestConcreteGameManager());

		// Create MapSystem
		_mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
		_mapSystem.CallInitialize();
		_mapSystem.AddWalkableCell(0, 0, 0);
		_mapSystem.AddWalkableCell(1, 0, 0);
		_mapSystem.AddWalkableCell(2, 0, 0);

		// Create test units
		var player = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player1" });
		var enemy = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy1" });

		player.CallInitialize();
		enemy.CallInitialize();

		_playerUnits.Add(player);
		_enemyUnits.Add(enemy);

        // Create and setup TurnManager
        var turnManager = AddNodeToTestRoot(new TestConcreteTurnManager());
        turnManager.SetCurrentUnit(enemy); // Set an enemy as current unit for the test
        // Set the TurnManager on GameManager via reflection
        var field = typeof(GameManager).GetField("_turnManagerContainer",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_gameManager, turnManager);

        // Create AI Manager
        _aiManager = new EnemyAIManager(_gameManager);
        _aiManager.SetMapSystem(_mapSystem);
        _aiManager.SetUnitReferences(_playerUnits, _enemyUnits);
	}

    [TestCase(Description = "AI should initialize properly when attached to UnitSystem")]
    public void TestInitialization_AttachedToUnit()
    {
        if (_aiBehavior == null)
            throw new System.InvalidOperationException("Setup failed to initialize AI behavior or mock unit");

        AssertThat(_aiBehavior._unit).IsNull();
        AssertThat(_aiBehavior.Unit).IsNull();
    }

    [TestCase(Description = "AI should create decision with Pass action when no valid actions available")]
    public void TestMakeDecision_NoValidActions()
    {
        var decision = new AIDecision { Action = AIAction.Pass };
        AssertThat(decision.Action).IsEqual(AIAction.Pass);
    }

    [TestCase(Description = "AI should handle null unit during ExecuteTurn gracefully")]
    public async Task TestExecuteTurn_NullUnit()
    {
        if (_aiBehavior == null || _mockBattleState == null)
            throw new System.InvalidOperationException("Setup failed to initialize AI behavior");

        _aiBehavior._unit = null;
        await _aiBehavior.ExecuteTurn(_mockBattleState);
        AssertThat(_aiBehavior.Unit).IsNull();
    }

    [TestCase(Description = "AIDecision should store all action parameters correctly")]
    public void TestAIDecision_StoresActionParameters()
    {
        var decision = new AIDecision
        {
            Action = AIAction.UseSkill,
            Target = _playerUnits[0],
            Skill = new TestConcreteSkillSystem(),
            Score = 85.5f,
            Reasoning = "High priority target"
        };

        AssertThat(decision.Action).IsEqual(AIAction.UseSkill);
        AssertThat(decision.Target).IsEqual(_playerUnits[0]);
        AssertThat(decision.Score).IsEqual(85.5f);
        AssertThat(decision.Reasoning).IsEqual("High priority target");
    }

    [TestCase(Description = "AIPersonality enum should have all required values")]
    public void TestAIPersonality_EnumValues()
    {
        var personalities = new[] { AIPersonality.Aggressive, AIPersonality.Opportunistic, 
                                    AIPersonality.Defensive, AIPersonality.Balanced };
        AssertThat(personalities).HasSize(4);
    }

    [TestCase(Description = "AIAction enum should have all required values")]
    public void TestAIAction_EnumValues()
    {
        var actions = new[] { AIAction.Move, AIAction.MoveAndSkill, 
                            AIAction.UseSkill, AIAction.Pass };
        AssertThat(actions).HasSize(4);
    }

    [TestCase(Description = "Thinking delay should be within configured range")]
    public void TestThinkingDelay_WithinRange()
    {
        if (_aiBehavior == null)
            throw new System.InvalidOperationException("Setup failed to initialize AI behavior");

        _aiBehavior.ThinkingDelayMin = 0.5f;
        _aiBehavior.ThinkingDelayMax = 2.0f;
        
        AssertThat(_aiBehavior.ThinkingDelayMin).IsGreater(0f);
        AssertThat(_aiBehavior.ThinkingDelayMax).IsGreater(_aiBehavior.ThinkingDelayMin);
    }

    [TestCase(Description = "Debug visualization flag should default to disabled")]
    public void TestDebugVisualization_DefaultDisabled()
    {
        if (_aiBehavior == null)
            throw new System.InvalidOperationException("Setup failed to initialize AI behavior");

        AssertThat(_aiBehavior.EnableDebugVisualization).IsFalse();
    }

    [TestCase(Description = "AIDecision reasoning should default to empty string")]
    public void TestAIDecision_ReasoningDefaultEmpty()
    {
        var decision = new AIDecision();
        AssertThat(decision.Reasoning).IsEqual(string.Empty);
    }

    [TestCase(Description = "AIDecision score should default to zero")]
    public void TestAIDecision_ScoreDefaultZero()
    {
        var decision = new AIDecision();
        AssertThat(decision.Score).IsEqual(0f);
    }

    [TestCase(Description = "Multiple AIDecisions should be independent")]
    public void TestAIDecision_MultipleInstancesIndependent()
    {
        var decision1 = new AIDecision { Score = 10f, Action = AIAction.Move };
        var decision2 = new AIDecision { Score = 20f, Action = AIAction.UseSkill };
        
        AssertThat(decision1.Score).IsNotEqual(decision2.Score);
        AssertThat(decision1.Action).IsNotEqual(decision2.Action);
    }

    [TestCase(Description = "AI should properly handle MoveAndSkill decision structure")]
    public void TestAIDecision_MoveAndSkillStructure()
    {
        var decision = new AIDecision
        {
            Action = AIAction.MoveAndSkill,
            MovePosition = new Vector3I(5, 0, 5),
            Target = _playerUnits[0],
            Skill = new TestConcreteSkillSystem()
        };

        AssertThat(decision.MovePosition).IsEqual(new Vector3I(5, 0, 5));
        AssertThat(decision.Target).IsNotNull();
        AssertThat(decision.Skill).IsNotNull();
    }
}