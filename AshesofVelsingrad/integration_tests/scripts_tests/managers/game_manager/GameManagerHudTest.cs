using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AshesOfVelsingrad.Helpers.Managers;
using AshesOfVelsingrad.Helpers.Systems;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using AshesOfVelsingrad.UI.Hud;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Managers;

[TestSuite]
[RequireGodotRuntime]
public class GameManagerHudTest
{
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private TestConcreteGameManager? _gameManager;
    private TestConcreteMapSystem? _mapSystem;
    private TestConcreteTurnManager? _turnManager;
    private TestConcreteBattleInputSystem? _battleInputSystem;

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

    private void ResetSingletons()
    {
        SetSingletonInstance<GameManager>(null);
        SetSingletonInstance<IMapSystem>(null);
        TestConcreteMapSystem.Instance = null;
    }

    private void SetSingletonInstance<T>(T? instance) where T : class
    {
        PropertyInfo? instanceProperty = typeof(T).GetProperty(
            "Instance",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic
        );
        instanceProperty?.SetValue(null, instance);
    }

    private T AddNodeToTestRoot<T>(T node) where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Test root node is not initialized.");
        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    private void SetupGameManagerDependencies()
    {
        _mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        _mapSystem.CallInitialize();

        _turnManager = AddNodeToTestRoot(new TestConcreteTurnManager());
        _battleInputSystem = AddNodeToTestRoot(new TestConcreteBattleInputSystem());

        Node playerUnitsContainer = AddNodeToTestRoot(new Node { Name = "PlayerUnits" });
        Node enemyUnitsContainer = AddNodeToTestRoot(new Node { Name = "EnemyUnits" });

        var dummyPlayer = new TestConcreteUnitSystem { Name = "TestPlayerUnit" };
        typeof(TestConcreteUnitSystem).GetProperty("Faction", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(dummyPlayer, Faction.Player);
        var dummyEnemy = new TestConcreteUnitSystem { Name = "TestEnemyUnit" };
        typeof(TestConcreteUnitSystem).GetProperty("Faction", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(dummyEnemy, Faction.Enemy);

        playerUnitsContainer.AddChild(dummyPlayer);
        enemyUnitsContainer.AddChild(dummyEnemy);

        _gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        _gameManager.SetNodePaths(
            playerUnitsContainer.GetPath(),
            enemyUnitsContainer.GetPath(),
            _mapSystem.GetPath(),
            _turnManager.GetPath(),
            _battleInputSystem.GetPath()
        );
    }

    #endregion

    #region EnsureHud & Spawning Tests

    [TestCase]
    public async Task EnsureHud_SpawnsNewHud_WhenNoneExists()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        _gameManager.CallEnsureHud();

        // Wait a frame for CallDeferred("add_child") to execute
        await _gameManager.ToSignal(_gameManager.GetTree(), SceneTree.SignalName.ProcessFrame);

        BattleHud? hud = _gameManager.GetBattleHud();
        AssertThat(hud).IsNotNull();
        AssertThat(hud!.Name).IsEqual((StringName)"BattleHud");
        AssertThat(hud.Visible).IsTrue();
    }

    [TestCase]
    public void EnsureHud_ReusesExistingHud_IfAlreadyValid()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        // Pre-inject a manual HUD instance instance
        var existingHud = new BattleHud { Name = "ExistingHud" };
        _root!.AddChild(existingHud);
        _testNodes.Add(existingHud);

        _gameManager.SetBattleHud(existingHud);

        _gameManager.CallEnsureHud();

        AssertThat(_gameManager.GetBattleHud()).IsEqual(existingHud);
    }

    #endregion

    #region Wiring & Binding Tests

    [TestCase]
    public void WireHudEvents_SafelyBypasses_WhenHudIsNull()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();
        _gameManager.SetBattleHud(null);

        // Should execute without throwing a NullReferenceException
        _gameManager.CallWireHudEvents();
    }

    #endregion

    #region Roster & Refresh Lifecycle Tests

    [TestCase]
    public void RefreshHudForActiveUnit_ShowsActionMenu_ForPlayerFaction()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var hud = new BattleHud();
        var actionMenu = new ActionMenu();
        typeof(BattleHud).GetProperty("ActionMenu", BindingFlags.Public | BindingFlags.Instance)?.SetValue(hud, actionMenu);
        _gameManager.SetBattleHud(hud);

        var playerUnit = new TestConcreteUnitSystem();
        typeof(TestConcreteUnitSystem).GetProperty("Faction", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(playerUnit, Faction.Player);

        _gameManager.CallRefreshHudForActiveUnit(playerUnit);

        AssertThat(actionMenu.Visible).IsTrue();
    }

    [TestCase]
    public void RefreshHudForActiveUnit_HidesActionMenu_ForEnemyFaction()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        var hud = new BattleHud();
        var actionMenu = new ActionMenu();
        typeof(BattleHud).GetProperty("ActionMenu", BindingFlags.Public | BindingFlags.Instance)?.SetValue(hud, actionMenu);
        _gameManager.SetBattleHud(hud);

        var enemyUnit = new TestConcreteUnitSystem();
        typeof(TestConcreteUnitSystem).GetProperty("Faction", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(enemyUnit, Faction.Enemy);

        _gameManager.CallRefreshHudForActiveUnit(enemyUnit);

        AssertThat(actionMenu.Visible).IsFalse();
    }

    #endregion

    #region Screen Overlay Tests

    [TestCase]
    public async Task ShowVictoryScreen_SpawnsAndBindsLoot()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        _gameManager.CallShowVictoryScreen();

        await _gameManager.ToSignal(_gameManager.GetTree(), SceneTree.SignalName.ProcessFrame);

        VictoryScreen? victoryScreen = _gameManager.GetVictoryScreen();
        AssertThat(victoryScreen).IsNotNull();
        AssertThat(victoryScreen!.Name).IsEqual((StringName)"VictoryScreen");
    }

    [TestCase]
    public async Task ShowGameOverScreen_SpawnsLazily()
    {
        SetupGameManagerDependencies();
        _gameManager!.CallInitialize();

        _gameManager.CallShowGameOverScreen();

        await _gameManager.ToSignal(_gameManager.GetTree(), SceneTree.SignalName.ProcessFrame);

        GameOverScreen? gameOverScreen = _gameManager.GetGameOverScreen();
        AssertThat(gameOverScreen).IsNotNull();
        AssertThat(gameOverScreen!.Name).IsEqual((StringName)"GameOverScreen");
    }

    #endregion
}
