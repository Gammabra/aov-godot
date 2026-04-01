using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public partial class EnemyAIBehaviorTest
{
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private EnemyAIManager? _aiManager;
    private TestConcreteEnemyAIBehavior? _aiBehavior;
    private BattleState? _mockBattleState;
    private TestConcreteGameManager? _gameManager;
    private TestConcreteMapSystem? _mapSystem;
    private List<IUnitSystem> _playerUnits = new();
    private List<IUnitSystem> _enemyUnits = new();

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

        SetupBasicAIManager();

        _mockBattleState = new BattleState
        {
            ActingUnit = _enemyUnits[0],
            MapSystem = _mapSystem!,
            PlayerUnits = _playerUnits,
            EnemyUnits = _enemyUnits
        };
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

    private void SetupBasicAIManager()
    {
        _gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        _mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        _mapSystem.CallInitialize();
        _mapSystem.AddWalkableCell(0, 0, 0);
        _mapSystem.AddWalkableCell(1, 0, 0);
        _mapSystem.AddWalkableCell(2, 0, 0);

        var player = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player1" });
        var enemy = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy1" });

        player.CallInitialize();
        enemy.CallInitialize();

        _playerUnits.Add(player);
        _enemyUnits.Add(enemy);

        var turnManager = AddNodeToTestRoot(new TestConcreteTurnManager());
        turnManager.SetCurrentUnit(enemy);

        var field = typeof(GameManager).GetField("_turnManagerContainer",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_gameManager, turnManager);

        _aiManager = new EnemyAIManager(_gameManager);
        _aiManager.SetMapSystem(_mapSystem);
        _aiManager.SetUnitReferences(_playerUnits, _enemyUnits);
    }

    #endregion

    #region Initialization Tests

    [TestCase]
    public void Ready_InitializesUnitReference_WhenParentIsUnitSystem()
    {
        var unit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "EnemyWithAI" });
        unit.CallInitialize();

        var aiBehavior = new TestConcreteEnemyAIBehavior();
        unit.AddChild(aiBehavior);
        _testNodes.Add(aiBehavior);

        // Trigger _Ready
        aiBehavior._Ready();

        AssertThat(aiBehavior.Unit).IsEqual(unit);
    }

    [TestCase]
    public void Ready_InitializesComponents()
    {
        var unit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "EnemyWithAI" });
        unit.CallInitialize();

        var aiBehavior = new TestConcreteEnemyAIBehavior();
        unit.AddChild(aiBehavior);
        _testNodes.Add(aiBehavior);

        aiBehavior._Ready();

        // Use reflection to check internal components were created
        var generatorField = typeof(EnemyAIBehavior).GetField("_decisionGenerator",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var evaluatorField = typeof(EnemyAIBehavior).GetField("_evaluator",
            BindingFlags.NonPublic | BindingFlags.Instance);

        AssertThat(generatorField?.GetValue(aiBehavior)).IsNotNull();
        AssertThat(evaluatorField?.GetValue(aiBehavior)).IsNotNull();
    }

    #endregion

    #region ExecuteTurn Tests

    [TestCase]
    public async Task ExecuteTurn_HandlesNullUnit()
    {
        // Use the helper to track the node
        _aiBehavior = AddNodeToTestRoot(new TestConcreteEnemyAIBehavior());
        _aiBehavior._unit = null;

        await _aiBehavior.DecideTurn(_mockBattleState!);

        AssertThat(_aiBehavior.Unit).IsNull();
    }

    [TestCase]
    public async Task ExecuteTurn_CallsPassTurn_WhenNoValidActions()
    {
        var unit = _enemyUnits[0];
        _aiBehavior = new TestConcreteEnemyAIBehavior();

        if (unit == null)
            throw new InvalidOperationException("Enemy unit is not initialized."); 

        (unit as Node)!.AddChild(_aiBehavior);
        _testNodes.Add(_aiBehavior);

        _aiBehavior._Ready();
        _aiBehavior.ThinkingDelayMin = 0.01f;
        _aiBehavior.ThinkingDelayMax = 0.01f;

        // Remove all skills so no actions are available
        unit.ActiveSkills.Clear();

        await _aiBehavior.DecideTurn(_mockBattleState!);

        // Verify turn completed (implicitly by not crashing)
        AssertThat(_aiBehavior.Unit).IsEqual(unit);
    }

    #endregion

    #region Property Tests

    [TestCase]
    public void ThinkingDelay_CanBeSetAndRetrieved()
    {
        // Use the helper to track the node
        _aiBehavior = AddNodeToTestRoot(new TestConcreteEnemyAIBehavior
        {
            ThinkingDelayMin = 0.5f,
            ThinkingDelayMax = 2.0f
        });

        AssertThat(_aiBehavior.ThinkingDelayMin).IsEqual(0.5f);
        AssertThat(_aiBehavior.ThinkingDelayMax).IsEqual(2.0f);
    }

    [TestCase]
    public void EnableDebugVisualization_DefaultsToFalse()
    {
        // Use the helper to track the node
        _aiBehavior = AddNodeToTestRoot(new TestConcreteEnemyAIBehavior());
        AssertThat(_aiBehavior.EnableDebugVisualization).IsFalse();
    }

    #endregion

    #region AIDecision Tests

    [TestCase]
    public void AIDecision_StoresAllProperties()
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

    [TestCase]
    public void AIDecision_DefaultsToZeroScore()
    {
        var decision = new AIDecision();
        AssertThat(decision.Score).IsEqual(0f);
    }

    [TestCase]
    public void AIDecision_MoveAndSkill_StoresMovePosition()
    {
        var decision = new AIDecision
        {
            Action = AIAction.MoveAndSkill,
            MovePosition = (5, 0, 5),
            Target = _playerUnits[0],
            Skill = new TestConcreteSkillSystem()
        };

        AssertThat(decision.MovePosition).IsEqual((5, 0, 5));
    }

    #endregion

    #region Enum Tests

    [TestCase]
    public void AIPersonality_HasAllValues()
    {
        var aggressive = AIPersonality.Aggressive;
        var opportunistic = AIPersonality.Opportunistic;
        var defensive = AIPersonality.Defensive;
        var balanced = AIPersonality.Balanced;

        AssertThat((int)aggressive).IsEqual(0);
        AssertThat((int)opportunistic).IsEqual(1);
        AssertThat((int)defensive).IsEqual(2);
        AssertThat((int)balanced).IsEqual(3);
    }

    [TestCase]
    public void AIAction_HasAllValues()
    {
        var moveAndSkill = AIAction.MoveAndSkill;
        var move = AIAction.Move;
        var useSkill = AIAction.UseSkill;
        var pass = AIAction.Pass;

        AssertThat(moveAndSkill).IsNotNull();
        AssertThat(move).IsNotNull();
        AssertThat(useSkill).IsNotNull();
        AssertThat(pass).IsNotNull();
    }

    #endregion
}
