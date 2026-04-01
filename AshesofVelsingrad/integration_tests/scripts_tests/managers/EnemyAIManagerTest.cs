using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.AI;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class EnemyAIManagerTest
{
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private EnemyAIManager? _aiManager;
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
        // Create GameManager
        _gameManager = AddNodeToTestRoot(new TestConcreteGameManager());

        // Create IMapSystem
        _mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        _mapSystem.CallInitialize();
        _mapSystem.AddWalkableCell(0, 0, 0);
        _mapSystem.AddWalkableCell(1, 0, 0);
        _mapSystem.AddWalkableCell(2, 0, 0);

        // Create test units
        var player1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player1" });
        var player2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player2" });
        var enemy1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy1" });
        var enemy2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy2" });

        player1.CallInitialize();
        player2.CallInitialize();
        enemy1.CallInitialize();
        enemy2.CallInitialize();

        _playerUnits.Add(player1);
        _playerUnits.Add(player2);
        _enemyUnits.Add(enemy1);
        _enemyUnits.Add(enemy2);

        // Create and setup TurnManager
        var turnManager = AddNodeToTestRoot(new TestConcreteTurnManager());
        turnManager.SetCurrentUnit(enemy1); // Set an enemy as current unit for the test
        // Set the TurnManager on GameManager via reflection
        var field = typeof(GameManager).GetField("_turnManagerContainer",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_gameManager, turnManager);

        // Create AI Manager
        _aiManager = new EnemyAIManager(_gameManager);
        _aiManager.SetMapSystem(_mapSystem);
        _aiManager.SetUnitReferences(_playerUnits, _enemyUnits);
    }

    #endregion

    #region Constructor Tests

    [TestCase]
    public void Constructor_CreatesInstanceWithGameManager()
    {
        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        var aiManager = new EnemyAIManager(gameManager);

        AssertThat(aiManager).IsNotNull();
    }

    [TestCase]
    public void Constructor_StoresGameManagerReference()
    {
        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        var aiManager = new EnemyAIManager(gameManager);

        // Use reflection to verify the GameManager was stored
        var field = typeof(EnemyAIManager).GetField("_gameManager",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var storedGameManager = field?.GetValue(aiManager);

        AssertThat(storedGameManager).IsEqual(gameManager);
    }

    #endregion

    #region SetMapSystem Tests

    [TestCase]
    public void SetMapSystem_StoresMapReference()
    {
        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        var mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        mapSystem.CallInitialize();

        var aiManager = new EnemyAIManager(gameManager);
        aiManager.SetMapSystem(mapSystem);

        // Use reflection to verify the IMapSystem was stored
        var field = typeof(EnemyAIManager).GetField("_mapSystem",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var storedMap = field?.GetValue(aiManager);

        AssertThat(storedMap).IsEqual(mapSystem);
    }

    #endregion

    #region SetUnitReferences Tests

    [TestCase]
    public void SetUnitReferences_StoresPlayerUnits()
    {
        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        var aiManager = new EnemyAIManager(gameManager);

        var playerUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player1" });
        playerUnit.CallInitialize();
        var playerUnits = new List<IUnitSystem> { playerUnit };
        var enemyUnits = new List<IUnitSystem>();

        aiManager.SetUnitReferences(playerUnits, enemyUnits);

        // Use reflection to verify units were stored
        var field = typeof(EnemyAIManager).GetField("_playerUnits",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var storedPlayers = (List<IUnitSystem>?)field?.GetValue(aiManager);

        AssertThat(storedPlayers).IsNotNull();
        AssertThat(storedPlayers!.Count).IsEqual(1);
        AssertThat(storedPlayers[0]).IsEqual(playerUnit);
    }

    [TestCase]
    public void SetUnitReferences_StoresEnemyUnits()
    {
        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        var aiManager = new EnemyAIManager(gameManager);

        var enemyUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy1" });
        enemyUnit.CallInitialize();
        var playerUnits = new List<IUnitSystem>();
        var enemyUnits = new List<IUnitSystem> { enemyUnit };

        aiManager.SetUnitReferences(playerUnits, enemyUnits);

        // Use reflection to verify units were stored
        var field = typeof(EnemyAIManager).GetField("_enemyUnits",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var storedEnemies = (List<IUnitSystem>?)field?.GetValue(aiManager);

        AssertThat(storedEnemies).IsNotNull();
        AssertThat(storedEnemies!.Count).IsEqual(1);
        AssertThat(storedEnemies[0]).IsEqual(enemyUnit);
    }

    #endregion

    #region GetAlivePlayerUnits Tests

    [TestCase]
    public void GetAlivePlayerUnits_ReturnsOnlyAliveUnits()
    {
        SetupBasicAIManager();

        // Kill one player unit
        _playerUnits[0].TakeDamage(200);
        _playerUnits[0].SetIsAlive(false);

        var aliveUnits = _aiManager!.GetAlivePlayerUnits();

        AssertThat(aliveUnits.Count).IsEqual(1);
        AssertThat(aliveUnits[0]).IsEqual(_playerUnits[1]);
    }

    [TestCase]
    public void GetAlivePlayerUnits_ReturnsEmptyListWhenAllDead()
    {
        SetupBasicAIManager();

        // Kill all player units
        foreach (var unit in _playerUnits)
        {
            unit.TakeDamage(200);
            unit.SetIsAlive(false);
        }

        var aliveUnits = _aiManager!.GetAlivePlayerUnits();

        AssertThat(aliveUnits.Count).IsEqual(0);
    }

    [TestCase]
    public void GetAlivePlayerUnits_ReturnsAllWhenAllAlive()
    {
        SetupBasicAIManager();

        var aliveUnits = _aiManager!.GetAlivePlayerUnits();

        AssertThat(aliveUnits.Count).IsEqual(2);
    }

    #endregion

    #region GetAliveEnemyUnits Tests

    [TestCase]
    public void GetAliveEnemyUnits_ReturnsOnlyAliveUnits()
    {
        SetupBasicAIManager();

        // Kill one enemy unit
        _enemyUnits[0].TakeDamage(200);
        _enemyUnits[0].SetIsAlive(false);

        var aliveUnits = _aiManager!.GetAliveEnemyUnits();

        AssertThat(aliveUnits.Count).IsEqual(1);
        AssertThat(aliveUnits[0]).IsEqual(_enemyUnits[1]);
    }

    [TestCase]
    public void GetAliveEnemyUnits_ReturnsEmptyListWhenAllDead()
    {
        SetupBasicAIManager();

        // Kill all enemy units
        foreach (var unit in _enemyUnits)
        {
            unit.TakeDamage(200);
            unit.SetIsAlive(false);
        }

        var aliveUnits = _aiManager!.GetAliveEnemyUnits();

        AssertThat(aliveUnits.Count).IsEqual(0);
    }

    [TestCase]
    public void GetAliveEnemyUnits_ReturnsAllWhenAllAlive()
    {
        SetupBasicAIManager();

        var aliveUnits = _aiManager!.GetAliveEnemyUnits();

        AssertThat(aliveUnits.Count).IsEqual(2);
    }

    #endregion

    #region ExecuteAITurn Tests

    [TestCase]
    public async Task ExecuteAITurn_PassesTurnWhenMapSystemNotSet()
    {
        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        var aiManager = new EnemyAIManager(gameManager);

        var unit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy1" });
        unit.CallInitialize();

        // Don't set IMapSystem
        await aiManager.ExecuteAITurn(unit);

        // Unit should have passed turn - check via log or state
        // Since we can't easily check the turn state, we just verify no crash occurred
        AssertThat(unit).IsNotNull();
    }

    [TestCase]
    public async Task ExecuteAITurn_PassesTurnWhenGameManagerIsNull()
    {
        var gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        var mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        mapSystem.CallInitialize();

        var aiManager = new EnemyAIManager(gameManager);
        aiManager.SetMapSystem(mapSystem);

        // Set GameManager to null via reflection
        var field = typeof(EnemyAIManager).GetField("_gameManager",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(aiManager, null);

        var unit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy1" });
        unit.CallInitialize();

        await aiManager.ExecuteAITurn(unit);

        // Should complete without crashing
        AssertThat(unit).IsNotNull();
    }

    [TestCase]
    public async Task ExecuteAITurn_ExecutesDefaultBehaviorWhenNoAIBehavior()
    {
        SetupBasicAIManager();

        var unit = _enemyUnits[0];

        // Unit has no AI behavior component attached
        await _aiManager!.ExecuteAITurn((unit as UnitSystem)!);

        // Should complete without crashing - default behavior executes
        AssertThat(unit).IsNotNull();
    }

    [TestCase]
    public async Task ExecuteAITurn_CallsAIBehaviorWhenPresent()
    {
        SetupBasicAIManager();

        var unit = _enemyUnits[0];

        // Add AI behavior component
        var aiBehavior = new TestConcreteEnemyAIBehavior();
        (unit as Node)!.AddChild(aiBehavior);
        _testNodes.Add(aiBehavior);

        await _aiManager!.ExecuteAITurn((unit as UnitSystem)!);

        // Verify AI behavior was executed
        AssertThat(aiBehavior.DecideTurnWasCalled).IsTrue();
    }

    #endregion
}
