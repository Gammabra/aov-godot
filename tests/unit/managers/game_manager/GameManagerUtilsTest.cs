using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class GameManagerUtilsTest
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

        // Create test units and add them as children BEFORE creating GameManager
        TestConcreteUnitSystem playerUnit1 = new() { Name = "PlayerUnit1", BaseSpeed = 200 };
        TestConcreteUnitSystem playerUnit2 = new() { Name = "PlayerUnit2", BaseSpeed = 190 };
        TestConcreteUnitSystem enemyUnit1 = new() { Name = "EnemyUnit1", BaseSpeed = 180 };
        TestConcreteUnitSystem enemyUnit2 = new() { Name = "EnemyUnit2", BaseSpeed = 170 };

        playerUnit1.CallInitialize();
        playerUnit2.CallInitialize();
        enemyUnit1.CallInitialize();
        enemyUnit2.CallInitialize();

        // Add units to their containers
        playerUnitsContainer.AddChild(playerUnit1);
        playerUnitsContainer.AddChild(playerUnit2);
        enemyUnitsContainer.AddChild(enemyUnit1);
        enemyUnitsContainer.AddChild(enemyUnit2);

        // Track for cleanup
        _testNodes.Add(playerUnit1);
        _testNodes.Add(playerUnit2);
        _testNodes.Add(enemyUnit1);
        _testNodes.Add(enemyUnit2);

        // Create game manager AFTER containers have units as children
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

    #region LoadUnits Tests

    [TestCase]
    public void LoadUnits_LoadsPlayerUnits()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        AssertThat(_gameManager.PlayerUnitsCount).IsEqual(2);
    }

    [TestCase]
    public void LoadUnits_LoadsEnemyUnits()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        AssertThat(_gameManager.EnemyUnitsCount).IsEqual(2);
    }

    [TestCase]
    public void LoadUnits_InjectsDependenciesToUnits()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var playerUnit = _gameManager.GetPlayerUnit(0);

        // Instead of checking the field directly, verify the dependency was injected
        // by checking that status effects can be applied (which requires the injected system)
        var effect = new TestConcreteStatusEffect<UnitSystem>();
        playerUnit.SetStatusEffectOnUnit(effect);

        // If the dependency was injected, the effect should be applied successfully
        AssertThat(playerUnit.HasEffect<TestConcreteStatusEffect<UnitSystem>>()).IsTrue();
    }

    [TestCase]
    public void LoadUnits_HandlesEmptyPlayerContainer()
    {
        // Create empty player container
        Node emptyPlayerContainer = AddNodeToTestRoot(new Node { Name = "EmptyPlayers" });
        Node enemyContainer = AddNodeToTestRoot(new Node { Name = "Enemies" });

        // Create unit WITHOUT adding to test root first
        var enemyUnit = new TestConcreteUnitSystem { Name = "Enemy1" };
        enemyUnit.CallInitialize();

        // Add to container, THEN track for cleanup
        enemyContainer.AddChild(enemyUnit);
        _testNodes.Add(enemyUnit); // Add to cleanup list manually

        _mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        _mapSystem.CallInitialize();
        _turnManager = AddNodeToTestRoot(new TestConcreteTurnManager());
        _battleInputSystem = AddNodeToTestRoot(new TestConcreteBattleInputSystem());

        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        gameManager.SetNodePaths(
            emptyPlayerContainer.GetPath(),
            enemyContainer.GetPath(),
            _mapSystem.GetPath(),
            _turnManager.GetPath(),
            _battleInputSystem.GetPath()
        );

        gameManager.CallInitialize();

        AssertThat(gameManager.PlayerUnitsCount).IsEqual(0);
        AssertThat(gameManager.EnemyUnitsCount).IsEqual(1);
    }

    [TestCase]
    public void LoadUnits_HandlesEmptyEnemyContainer()
    {
        // Create containers with only player units
        Node playerContainer = AddNodeToTestRoot(new Node { Name = "Players" });
        Node emptyEnemyContainer = AddNodeToTestRoot(new Node { Name = "EmptyEnemies" });

        // Create unit WITHOUT adding to test root
        var playerUnit = new TestConcreteUnitSystem { Name = "Player1" };
        playerUnit.CallInitialize();

        // Add to container, then track for cleanup
        playerContainer.AddChild(playerUnit);
        _testNodes.Add(playerUnit);

        _mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        _mapSystem.CallInitialize();
        _turnManager = AddNodeToTestRoot(new TestConcreteTurnManager());
        _battleInputSystem = AddNodeToTestRoot(new TestConcreteBattleInputSystem());

        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        gameManager.SetNodePaths(
            playerContainer.GetPath(),
            emptyEnemyContainer.GetPath(),
            _mapSystem.GetPath(),
            _turnManager.GetPath(),
            _battleInputSystem.GetPath()
        );

        gameManager.CallInitialize();

        AssertThat(gameManager.PlayerUnitsCount).IsEqual(1);
        AssertThat(gameManager.EnemyUnitsCount).IsEqual(0);
    }

    [TestCase]
    public void LoadUnits_IgnoresNonUnitSystemChildren()
    {
        Node playerContainer = AddNodeToTestRoot(new Node { Name = "Players" });
        Node enemyContainer = AddNodeToTestRoot(new Node { Name = "Enemies" });

        // Create unit WITHOUT adding to test root
        var playerUnit = new TestConcreteUnitSystem { Name = "Player1" };
        playerUnit.CallInitialize();

        // Add to container, then track for cleanup
        playerContainer.AddChild(playerUnit);
        _testNodes.Add(playerUnit);

        // Add a non-UnitSystem node
        var nonUnitNode = new Node { Name = "NotAUnit" };
        playerContainer.AddChild(nonUnitNode);
        _testNodes.Add(nonUnitNode); // Track for cleanup

        _mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        _mapSystem.CallInitialize();
        _turnManager = AddNodeToTestRoot(new TestConcreteTurnManager());
        _battleInputSystem = AddNodeToTestRoot(new TestConcreteBattleInputSystem());

        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        gameManager.SetNodePaths(
            playerContainer.GetPath(),
            enemyContainer.GetPath(),
            _mapSystem.GetPath(),
            _turnManager.GetPath(),
            _battleInputSystem.GetPath()
        );

        gameManager.CallInitialize();

        // Should only load the UnitSystem, not the regular Node
        AssertThat(gameManager.PlayerUnitsCount).IsEqual(1);
    }

    #endregion

    #region HandlePlayerUnitMove Tests

    [TestCase]
    public void HandlePlayerUnitMove_MovesUnitToValidCell()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var unit = _gameManager.GetPlayerUnit(0);
        _mapSystem!.CellsInformation[0].SetUnit(unit);
        _turnManager!.SetCurrentUnit(unit);

        // Set up possible moves
        _gameManager.SetCurrentUnitPossibleMoves(new List<Vector3I> { new Vector3I(1, 0, 0) });

        _gameManager.CallHandlePlayerUnitMove(new Vector3I(1, 0, 0));

        AssertThat(_mapSystem.GetUnitAt(1, 0, 0)).IsEqual(unit);
    }

    [TestCase]
    public void HandlePlayerUnitMove_RejectsInvalidCell()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var unit = _gameManager.GetPlayerUnit(0);
        _mapSystem!.CellsInformation[0].SetUnit(unit);
        _turnManager!.SetCurrentUnit(unit);

        // Set up possible moves (not including 2,0,0)
        _gameManager.SetCurrentUnitPossibleMoves(new List<Vector3I> { new Vector3I(1, 0, 0) });

        _gameManager.CallHandlePlayerUnitMove(new Vector3I(2, 0, 0));

        // Unit should not have moved
        AssertThat(_mapSystem.GetUnitAt(2, 0, 0)).IsNull();
        AssertThat(_battleInputSystem!.InputEnabled).IsTrue();
    }

    [TestCase]
    public void HandlePlayerUnitMove_ClearsPossibleMovesAfterMove()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var unit = _gameManager.GetPlayerUnit(0);
        _mapSystem!.CellsInformation[0].SetUnit(unit);
        _turnManager!.SetCurrentUnit(unit);

        _gameManager.SetCurrentUnitPossibleMoves(new List<Vector3I> { new Vector3I(1, 0, 0) });

        _gameManager.CallHandlePlayerUnitMove(new Vector3I(1, 0, 0));

        AssertThat(_gameManager.GetCurrentUnitPossibleMovesCount()).IsEqual(0);
    }

    [TestCase]
    public void HandlePlayerUnitMove_HandlesOutOfRangeException()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var unit = _gameManager.GetPlayerUnit(0);
        _turnManager!.SetCurrentUnit(unit);

        // Try to move to a cell that doesn't exist
        _gameManager.SetCurrentUnitPossibleMoves(new List<Vector3I> { new Vector3I(99, 99, 99) });

        // Should not throw
        _gameManager.CallHandlePlayerUnitMove(new Vector3I(99, 99, 99));

        AssertThat(_battleInputSystem!.InputEnabled).IsTrue();
    }

    #endregion

    #region HandlePlayerSelectTarget Tests

    [TestCase]
    public void HandlePlayerSelectTarget_UsesSkillOnValidTarget()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var sourceUnit = _gameManager.GetPlayerUnit(0);
        var targetUnit = _gameManager.GetEnemyUnit(0);
        var skill = new TestConcreteSkillSystem(target: AovDataStructures.TargetTypes.SingleEnemy);
        sourceUnit.ActiveSkills.Add(skill);

        _mapSystem!.CellsInformation[1].SetUnit(targetUnit);
        _turnManager!.SetCurrentUnit(sourceUnit);

        _gameManager.SetSelectedSkill(skill);
        _gameManager.SetCurrentUnitReachableCells(new List<Vector3I> { new Vector3I(1, 0, 0) });

        _gameManager.CallHandlePlayerSelectTarget(new Vector3I(1, 0, 0));

        AssertThat(skill.WasUsed).IsTrue();
    }

    [TestCase]
    public void HandlePlayerSelectTarget_RejectsNonReachableCell()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var sourceUnit = _gameManager.GetPlayerUnit(0);
        var targetUnit = _gameManager.GetEnemyUnit(0);
        var skill = new TestConcreteSkillSystem(target: AovDataStructures.TargetTypes.SingleEnemy);

        _mapSystem!.CellsInformation[2].SetUnit(targetUnit);
        _turnManager!.SetCurrentUnit(sourceUnit);

        _gameManager.SetSelectedSkill(skill);
        _gameManager.SetCurrentUnitReachableCells(new List<Vector3I> { new Vector3I(1, 0, 0) });

        _gameManager.CallHandlePlayerSelectTarget(new Vector3I(2, 0, 0));

        AssertThat(skill.WasUsed).IsFalse();
        AssertThat(_battleInputSystem!.InputEnabled).IsTrue();
    }

    [TestCase]
    public void HandlePlayerSelectTarget_RejectsEmptyCell()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var sourceUnit = _gameManager.GetPlayerUnit(0);
        var skill = new TestConcreteSkillSystem(range: 2, target: AovDataStructures.TargetTypes.SingleEnemy);

        _turnManager!.SetCurrentUnit(sourceUnit);

        _gameManager.SetSelectedSkill(skill);
        // Include both Y=0 and Y=-1 since the code tries Y-1 when Y=0 fails
        _gameManager.SetCurrentUnitReachableCells(new List<Vector3I> { new Vector3I(2, 0, 0) });

        // Reset InputEnabled before the test
        _battleInputSystem!.SetInputEnabled(false);

        // Cell (1,0,0) has no unit
        _gameManager.CallHandlePlayerSelectTarget(new Vector3I(2, 0, 0));

        AssertThat(skill.WasUsed).IsFalse();
        AssertThat(_battleInputSystem.InputEnabled).IsTrue(); // Should re-enable after error
    }

    [TestCase]
    public void HandlePlayerSelectTarget_RejectsReviveOnAliveUnit()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var sourceUnit = _gameManager.GetPlayerUnit(0);
        var targetUnit = _gameManager.GetPlayerUnit(1);
        var skill = new TestConcreteSkillSystem(
            name: "Test Revive",
            effect: AovDataStructures.EffectType.Revive,
            target: AovDataStructures.TargetTypes.SingleAlly
        );
        sourceUnit.ActiveSkills.Add(skill);
        _mapSystem!.CellsInformation[1].SetUnit(targetUnit);

        _turnManager!.SetCurrentUnit(sourceUnit);

        _gameManager.SetSelectedSkill(skill);

        _gameManager.SetCurrentUnitReachableCells(new List<Vector3I> { new Vector3I(1, 0, 0) });

        // Target is alive, can't revive
        AssertThat(targetUnit.IsAlive).IsTrue(); // Verify target is alive first

        _gameManager.CallHandlePlayerSelectTarget(new Vector3I(1, 0, 0));

        AssertThat(skill.WasUsed).IsFalse(); // Skill should NOT be used
        AssertThat(_battleInputSystem!.InputEnabled).IsTrue(); // Input should be re-enabled
    }

    [TestCase]
    public void HandlePlayerSelectTarget_RejectsNonReviveOnDeadUnit()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var sourceUnit = _gameManager.GetPlayerUnit(0);
        var targetUnit = _gameManager.GetPlayerUnit(1);
        var skill = new TestConcreteSkillSystem(
            target: AovDataStructures.TargetTypes.SingleAlly,
            effect: AovDataStructures.EffectType.Heal // Not a revive
        );
        sourceUnit.ActiveSkills.Add(skill);

        // Kill the target
        targetUnit.TakeDamage(200);
        targetUnit.SetIsAlive(false);

        AssertThat(targetUnit.IsAlive).IsFalse(); // Verify target is dead

        _mapSystem!.CellsInformation[1].SetUnit(targetUnit);
        _turnManager!.SetCurrentUnit(sourceUnit);

        _gameManager.SetSelectedSkill(skill);
        _gameManager.SetCurrentUnitReachableCells(new List<Vector3I> { new Vector3I(1, 0, 0) });

        // Target is dead, can't heal
        _gameManager.CallHandlePlayerSelectTarget(new Vector3I(1, 0, 0));

        AssertThat(skill.WasUsed).IsFalse(); // Skill should NOT be used
        AssertThat(_battleInputSystem!.InputEnabled).IsTrue(); // Input should be re-enabled
    }

    [TestCase]
    public void HandlePlayerSelectTarget_HandlesYAxisAdjustment()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var sourceUnit = _gameManager.GetPlayerUnit(0);
        var targetUnit = _gameManager.GetEnemyUnit(0);
        var skill = new TestConcreteSkillSystem(target: AovDataStructures.TargetTypes.SingleEnemy);
        sourceUnit.ActiveSkills.Add(skill);

        // Add a cell one level down
        _mapSystem!.AddWalkableCell(1, -1, 0);
        int index = _mapSystem.CellsInformation.Count - 1;
        _mapSystem.CellsInformation[index].SetUnit(targetUnit);

        _turnManager!.SetCurrentUnit(sourceUnit);

        _gameManager.SetSelectedSkill(skill);
        // Allow targeting at Y=0, but unit is at Y=-1
        _gameManager.SetCurrentUnitReachableCells(new List<Vector3I> { new Vector3I(1, 0, 0) });

        // Try Y=0 first, should adjust to Y=-1
        _gameManager.CallHandlePlayerSelectTarget(new Vector3I(1, 0, 0));

        AssertThat(skill.WasUsed).IsTrue();
    }

    #endregion

    #region CheckUnitsLife Tests

    [TestCase]
    public void CheckUnitsLife_KeepsAliveUnitsAlive()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var unit = _gameManager.GetPlayerUnit(0);

        // Don't kill the unit
        _gameManager.CallCheckUnitsLife(_gameManager.GetPlayerUnitsList());

        AssertThat(unit.IsAlive).IsTrue();
    }

    #endregion

    #region CheckWinLoseCondition Tests

    [TestCase]
    public void CheckWinLoseCondition_DetectsDefeat_WhenAllPlayersDead()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        // Verify units were loaded
        AssertThat(_gameManager.PlayerUnitsCount).IsGreater(0);
        AssertThat(_gameManager.EnemyUnitsCount).IsGreater(0);

        // Kill all player units
        foreach (var unit in _gameManager.GetPlayerUnitsList())
        {
            unit.TakeDamage(200);
            unit.SetIsAlive(false);
        }

        _gameManager.CallCheckWinLoseCondition();

        AssertThat(_gameManager.GetGameOutcome()).IsEqual(AovDataStructures.GameOutcome.Defeat);
        AssertThat(_turnManager!.TurnLoopEnded).IsTrue();
    }

    [TestCase]
    public void CheckWinLoseCondition_DetectsVictory_WhenAllEnemiesDead()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        // Verify units were loaded
        AssertThat(_gameManager.PlayerUnitsCount).IsGreater(0);
        AssertThat(_gameManager.EnemyUnitsCount).IsGreater(0);

        // Kill all enemy units
        foreach (var unit in _gameManager.GetEnemyUnitsList())
        {
            unit.TakeDamage(200);
            unit.SetIsAlive(false);
        }

        _gameManager.CallCheckWinLoseCondition();

        AssertThat(_gameManager.GetGameOutcome()).IsEqual(AovDataStructures.GameOutcome.Victory);
        AssertThat(_turnManager!.TurnLoopEnded).IsTrue();
    }

    [TestCase]
    public void CheckWinLoseCondition_Ongoing_WhenBothSidesAlive()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        // Verify units were loaded
        AssertThat(_gameManager.PlayerUnitsCount).IsGreater(0);
        AssertThat(_gameManager.EnemyUnitsCount).IsGreater(0);

        // All units alive
        _gameManager.CallCheckWinLoseCondition();

        AssertThat(_gameManager.GetGameOutcome()).IsEqual(AovDataStructures.GameOutcome.Ongoing);
        AssertThat(_turnManager!.TurnLoopEnded).IsFalse();
    }

    [TestCase]
    public void CheckWinLoseCondition_Ongoing_WhenSomeUnitsDead()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        // Kill one player, one enemy
        var player = _gameManager.GetPlayerUnit(0);
        var enemy = _gameManager.GetEnemyUnit(0);

        player.TakeDamage(200);
        player.SetIsAlive(false);
        enemy.TakeDamage(200);
        enemy.SetIsAlive(false);

        _gameManager.CallCheckWinLoseCondition();

        AssertThat(_gameManager.GetGameOutcome()).IsEqual(AovDataStructures.GameOutcome.Ongoing);
    }

    #endregion

    #region CheckUnitTurnEnd Tests

    [TestCase]
    public void CheckUnitTurnEnd_UpdatesAllUnitsLife()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var player = _gameManager.GetPlayerUnit(0);
        var enemy = _gameManager.GetEnemyUnit(0);

        // Damage but don't kill
        player.TakeDamage(50);
        enemy.TakeDamage(50);

        _gameManager.CallCheckUnitTurnEnd();

        // Both should still be alive
        AssertThat(player.IsAlive).IsTrue();
        AssertThat(enemy.IsAlive).IsTrue();
    }

    [TestCase]
    public void CheckUnitTurnEnd_ChecksWinLoseCondition()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        // Kill all enemies
        foreach (var unit in _gameManager.GetEnemyUnitsList())
        {
            unit.TakeDamage(200);
        }

        _gameManager.CallCheckUnitTurnEnd();

        AssertThat(_gameManager.GetGameOutcome()).IsEqual(AovDataStructures.GameOutcome.Victory);
    }

    #endregion
}
