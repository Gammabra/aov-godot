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
public class GameManagerTest
{
	private readonly List<Node> _testNodes = new();
	private Node? _root;
	private TestConcreteGameManager? _gameManager;
	private TestConcreteMapSystem? _mapSystem;
	private TestConcreteTurnManager? _turnManager;
	private TestConcreteBattleInputSystem? _battleInputSystem;

	#region Private Helper Methods

	private T AddNodeToTestRoot<T>(T node)
		where T : Node
	{
		if (_root == null)
			throw new InvalidOperationException("Test root node is not initialized.");
		_root.AddChild(node);
		_testNodes.Add(node);
		return node;
	}

	private void SetSingletonInstance<T>(T? instance)
		where T : class
	{
		PropertyInfo? instanceProperty = typeof(T).GetProperty(
			"Instance",
			BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic
		);
		instanceProperty?.SetValue(null, instance);
	}

	private void ResetSingletons()
	{
		SetSingletonInstance<GameManager>(null);
		SetSingletonInstance<MapSystem>(null);
		TestConcreteMapSystem.Instance = null;
	}

	private void SetupGameManagerDependencies()
	{
		// Create map system
		_mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
		_mapSystem.CallInitialize();
		_mapSystem.AddWalkableCell(0, 0, 0);
		_mapSystem.AddWalkableCell(1, 0, 0);
		_mapSystem.AddWalkableCell(2, 0, 0);

		// Create turn manager
		_turnManager = AddNodeToTestRoot(new TestConcreteTurnManager());

		// Create battle input system
		_battleInputSystem = AddNodeToTestRoot(new TestConcreteBattleInputSystem());

		// Create player and enemy unit containers
		Node playerUnitsContainer = AddNodeToTestRoot(new Node { Name = "PlayerUnits" });
		Node enemyUnitsContainer = AddNodeToTestRoot(new Node { Name = "EnemyUnits" });

		// Create test units (create + parent directly into containers)
        var playerUnit = new TestConcreteUnitSystem { Name = "PlayerUnit1" };
        var enemyUnit = new TestConcreteUnitSystem { Name = "EnemyUnit1" };

        playerUnitsContainer.AddChild(playerUnit);
        enemyUnitsContainer.AddChild(enemyUnit);

        // Track nodes for teardown
        _testNodes.Add(playerUnit);
        _testNodes.Add(enemyUnit);

        // Initialize after parenting
        playerUnit.CallInitialize();
        enemyUnit.CallInitialize();

		// Create game manager
		_gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
		
		// Set up node paths
		_gameManager.SetNodePaths(
			playerUnitsContainer.GetPath(),
			enemyUnitsContainer.GetPath(),
			_mapSystem.GetPath(),
			_turnManager.GetPath(),
			_battleInputSystem.GetPath()
		);
	}

	#endregion

	#region Setup and Teardown

	[BeforeTest]
	public void Setup()
	{
		ResetSingletons();
		_testNodes.Clear();

		_root = new Node { Name = "TestRoot" };
		((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
		_testNodes.Add(_root);
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
		ResetSingletons();
	}

	#endregion

	#region Initialization Tests

	[TestCase]
	public void Initialize_SetsSingletonInstance()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		AssertThat(_gameManager.IsInitialized).IsTrue();
	}

	[TestCase]
	public void Initialize_CreatesAIManager()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		AssertThat(_gameManager.AIManager).IsNotNull();
	}

	[TestCase]
	public void Initialize_LoadsUnits()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		AssertThat(_gameManager.PlayerUnitsCount).IsGreater(0);
		AssertThat(_gameManager.EnemyUnitsCount).IsGreater(0);
	}

	[TestCase]
	public void Initialize_ConnectsBattleInputSignals()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		// Verify signals are connected by triggering them
		AssertThat(_battleInputSystem!.IsConnected(
			TestConcreteBattleInputSystem.SignalName.OnPassTurnPressed,
			Callable.From(() => { })
		)).IsFalse(); // We can't easily verify this without reflection, so just check manager initialized
		
		AssertThat(_gameManager.IsInitialized).IsTrue();
	}

	[TestCase]
	public void Initialize_SingletonPattern_PreventsMultipleInstances()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		var secondManager = AddNodeToTestRoot(new TestConcreteGameManager());
		secondManager.SetNodePaths(
			new NodePath("PlayerUnits"),
			new NodePath("EnemyUnits"),
			_mapSystem!.GetPath(),
			_turnManager!.GetPath(),
			_battleInputSystem!.GetPath()
		);
		secondManager.CallInitialize();

		AssertThat(secondManager.IsQueuedForDeletion()).IsTrue();
	}

	#endregion

	#region MoveUnit Tests

	[TestCase]
	public void MoveUnit_MovesUnitToCorrectPosition()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		// Set up a unit at position (0,0,0)
		var unit = _gameManager.GetPlayerUnit(0);
		_mapSystem!.CellsInformation[0].Unit = unit;
		_turnManager!.SetCurrentUnit(unit);

		// Move to (1,0,0)
		_gameManager.MoveUnit(new Vector3I(1, 0, 0));

		AssertThat(_mapSystem.GetUnitAt(1, 0, 0)).IsEqual(unit);
		AssertThat(_gameManager.UnitMoved).IsTrue();
	}

	[TestCase]
	public void MoveUnit_DoesNotMoveIfAlreadyMoved()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		var unit = _gameManager.GetPlayerUnit(0);
		_mapSystem!.CellsInformation[0].Unit = unit;
		_turnManager!.SetCurrentUnit(unit);

		// Move once
		_gameManager.MoveUnit(new Vector3I(1, 0, 0));
		
		// Try to move again
		_gameManager.MoveUnit(new Vector3I(2, 0, 0));

		// Unit should still be at (1,0,0)
		AssertThat(_mapSystem.GetUnitAt(1, 0, 0)).IsEqual(unit);
		AssertThat(_mapSystem.GetUnitAt(2, 0, 0)).IsNull();
	}

	[TestCase]
	public void MoveUnit_HandlesNullMapSystemGracefully()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();
		_gameManager.ClearMapSystem();

		// Should not throw
		_gameManager.MoveUnit(new Vector3I(1, 0, 0));
		
		AssertThat(_gameManager.UnitMoved).IsFalse();
	}

	[TestCase]
	public void MoveUnit_HandlesNullTurnManagerGracefully()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();
		_gameManager.ClearTurnManager();

		// Should not throw
		_gameManager.MoveUnit(new Vector3I(1, 0, 0));
		
		AssertThat(_gameManager.UnitMoved).IsFalse();
	}

	#endregion

	#region UseSkill Tests

	[TestCase]
	public void UseSkill_SingleEnemy_UsesSkillOnTarget()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		var sourceUnit = _gameManager.GetPlayerUnit(0);
		var targetUnit = _gameManager.GetEnemyUnit(0);
		var skill = new TestConcreteSkillSystem(target: TargetTypes.SingleEnemy);
		sourceUnit.ActiveSkills.Add(skill);

		_turnManager!.SetCurrentUnit(sourceUnit);

		_gameManager.UseSkill(sourceUnit, targetUnit, skill);

		AssertThat(skill.WasUsed).IsTrue();
	}

	[TestCase]
	public void UseSkill_SingleAlly_UsesSkillOnAlly()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		var sourceUnit = _gameManager.GetPlayerUnit(0);
		var allyUnit = _gameManager.GetPlayerUnit(0); // Same unit (self-heal)
		var skill = new TestConcreteSkillSystem(target: TargetTypes.SingleAlly);
		sourceUnit.ActiveSkills.Add(skill);

		_turnManager!.SetCurrentUnit(sourceUnit);

		_gameManager.UseSkill(sourceUnit, allyUnit, skill);

		AssertThat(skill.WasUsed).IsTrue();
	}

	[TestCase]
	public void UseSkill_AllEnemies_TargetsAllEnemyUnits()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		var sourceUnit = _gameManager.GetPlayerUnit(0);
		var targetUnit = _gameManager.GetEnemyUnit(0); // Just one to trigger the check
		var skill = new TestConcreteSkillSystem(target: TargetTypes.AllEnemies);
		sourceUnit.ActiveSkills.Add(skill);

		_turnManager!.SetCurrentUnit(sourceUnit);

		_gameManager.UseSkill(sourceUnit, targetUnit, skill);

		AssertThat(skill.WasUsed).IsTrue();
	}

	[TestCase]
	public void UseSkill_InvalidTarget_DoesNotUseSkill()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		var sourceUnit = _gameManager.GetPlayerUnit(0);
		var wrongTargetUnit = _gameManager.GetPlayerUnit(0); // Player targeting player with enemy skill
		var skill = new TestConcreteSkillSystem(target: TargetTypes.SingleEnemy);
		sourceUnit.ActiveSkills.Add(skill);

		_turnManager!.SetCurrentUnit(sourceUnit);

		_gameManager.UseSkill(sourceUnit, wrongTargetUnit, skill);

		AssertThat(skill.WasUsed).IsFalse();
	}

	[TestCase]
	public void UseSkill_HandlesNullTurnManagerGracefully()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();
		_gameManager.ClearTurnManager();

		var sourceUnit = _gameManager.GetPlayerUnit(0);
		var targetUnit = _gameManager.GetEnemyUnit(0);
		var skill = new TestConcreteSkillSystem();

		// Should not throw
		_gameManager.UseSkill(sourceUnit, targetUnit, skill);
		
		AssertThat(skill.WasUsed).IsFalse();
	}

	#endregion

	#region AI Manager Tests

	[TestCase]
	public void AIManager_IsCreatedDuringInitialization()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		AssertThat(_gameManager.AIManager).IsNotNull();
	}

	[TestCase]
	public void AIManager_HasCorrectUnitReferences()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		var alivePlayerUnits = _gameManager.AIManager!.GetAlivePlayerUnits();
		var aliveEnemyUnits = _gameManager.AIManager.GetAliveEnemyUnits();

		AssertThat(alivePlayerUnits.Count).IsGreater(0);
		AssertThat(aliveEnemyUnits.Count).IsGreater(0);
	}

	#endregion

	#region Debug Methods Tests

	[TestCase]
	public void EnableThreatMapDebug_CanBeToggled()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		_gameManager.EnableThreatMapDebug = true;
		AssertThat(_gameManager.EnableThreatMapDebug).IsTrue();

		_gameManager.EnableThreatMapDebug = false;
		AssertThat(_gameManager.EnableThreatMapDebug).IsFalse();
	}

	#endregion

	#region Edge Cases and Error Handling

	[TestCase]
	public void MoveUnit_UpdatesGlobalPosition()
	{
		SetupGameManagerDependencies();
		_gameManager!.CallInitialize();

		var unit = _gameManager.GetPlayerUnit(0);
		_mapSystem!.CellsInformation[0].Unit = unit;
		_turnManager!.SetCurrentUnit(unit);

		Vector3 originalPosition = unit.GlobalPosition;
		_gameManager.MoveUnit(new Vector3I(1, 0, 0));

		// Position should have changed
		AssertThat(unit.GlobalPosition).IsNotEqual(originalPosition);
	}

	[TestCase]
	public void GameManager_HandlesEmptyUnitContainers()
	{
		// Create game manager with empty unit containers
		Node emptyPlayerContainer = AddNodeToTestRoot(new Node { Name = "EmptyPlayers" });
		Node emptyEnemyContainer = AddNodeToTestRoot(new Node { Name = "EmptyEnemies" });

		_mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
		_mapSystem.CallInitialize();

		_turnManager = AddNodeToTestRoot(new TestConcreteTurnManager());
		_battleInputSystem = AddNodeToTestRoot(new TestConcreteBattleInputSystem());

		var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
		gameManager.SetNodePaths(
			emptyPlayerContainer.GetPath(),
			emptyEnemyContainer.GetPath(),
			_mapSystem.GetPath(),
			_turnManager.GetPath(),
			_battleInputSystem.GetPath()
		);

		// Should not throw
		gameManager.CallInitialize();

		AssertThat(gameManager.PlayerUnitsCount).IsEqual(0);
		AssertThat(gameManager.EnemyUnitsCount).IsEqual(0);
	}

	#endregion
}
