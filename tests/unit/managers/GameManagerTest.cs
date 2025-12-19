using System;
using System.Collections.Generic;
using System.Reflection;
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
    private Node? _root;
    private readonly List<Node> _testNodes = new();

    #region Helpers

    private T AddNode<T>(T node)
        where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Root is not initialized.");

        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    private void ResetSingleton()
    {
        typeof(GameManager)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null, null);
    }

    private TestConcreteUnitSystem CreateUnit(string name, float hp = 100)
    {
        TestConcreteUnitSystem unit = new(hp: hp);
        unit.Name = name;
        return unit;
    }

    #endregion

    #region Setup / Teardown

    [BeforeTest]
    public void Setup()
    {
        ResetSingleton();
        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Clear();
        _testNodes.Add(_root);
    }

    [AfterTest]
    public void Cleanup()
    {
        foreach (Node node in _testNodes)
            node.QueueFree();
        _testNodes.Clear();
        ResetSingleton();
    }

    #endregion

    #region Tests

    [TestCase]
    public void Initialize_SetsSingleton()
    {
        GD.Print("[TEST] Start Initialize_SetsSingleton");

        GameManager manager = AddNode(new GameManager());
        manager.Call("Initialize");

        object? instance = typeof(GameManager)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null);

        AssertThat(instance).IsNotNull();
        AssertThat(instance).IsEqual(manager);
    }

    [TestCase]
    public void LoadUnits_AddsUnitsFromContainers()
    {
        GD.Print("[TEST] Start LoadUnits_AddsUnitsFromContainers");

        GameManager manager = AddNode(new GameManager());

        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());

        TestConcreteUnitSystem playerUnit = CreateUnit("Player");
        playerContainer.AddChild(playerUnit);

        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy");
        enemyContainer.AddChild(enemyUnit);

        manager.Set("_playerUnitsContainer", playerContainer);
        manager.Set("_enemyUnitsContainer", enemyContainer);

        manager.Call("LoadUnits");

        AssertThat(playerContainer.GetChildCount()).IsEqual(1);
        AssertThat(playerContainer.GetChildCount()).IsEqual(1);
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_playerUnits").Count).IsEqual(1);
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits").Count).IsEqual(1);
    }

    [TestCase]
    public void CheckWinLoseCondition_SetsVictory_WhenNoEnemiesAlive()
    {
        GD.Print("[TEST] Start CheckWinLoseCondition_SetsVictory_WhenNoEnemiesAlive");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player");
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy", 0);

        enemyUnit.SetIsAlive(false);

        SetPrivateField(manager, "_gameOutcome", GameOutcome.Ongoing);
        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);

        CallPrivateMethod(manager, "LoadUnits");

        CallPrivateMethod(manager, "CheckWinLoseCondition");

        GameOutcome outcome = GetPrivateField<GameOutcome>(manager, "_gameOutcome");
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_playerUnits")[0].IsAlive).IsTrue();
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits")[0].IsAlive).IsFalse();
        AssertThat(outcome).IsEqual(GameOutcome.Victory);
    }

    [TestCase]
    public void CheckWinLoseCondition_SetsDefeat_WhenNoPlayersAlive()
    {
        GD.Print("[TEST] Start CheckWinLoseCondition_SetsDefeat_WhenNoPlayersAlive");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player", 0);
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy");

        playerUnit.SetIsAlive(false);

        SetPrivateField(manager, "_gameOutcome", GameOutcome.Ongoing);
        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);

        CallPrivateMethod(manager, "LoadUnits");

        CallPrivateMethod(manager, "CheckWinLoseCondition");

        GameOutcome outcome = GetPrivateField<GameOutcome>(manager, "_gameOutcome");
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_playerUnits")[0].IsAlive).IsFalse();
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits")[0].IsAlive).IsTrue();
        AssertThat((int)outcome).IsEqual((int)GameOutcome.Defeat);
    }

    [TestCase]
    public void CheckUnitsLife_SetsAllUnitsDead()
    {
        GD.Print("[TEST] Start CheckUnitsLife_SetsAllUnitsDead");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player", 0);
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy", 0);

        SetPrivateField(manager, "_gameOutcome", GameOutcome.Ongoing);
        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);

        CallPrivateMethod(manager, "LoadUnits");

        List<UnitSystem> playerUnits = GetPrivateField<List<UnitSystem>>(manager, "_playerUnits");
        List<UnitSystem> enemyUnits = GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits");

        List<UnitSystem> units = playerUnits;
        units.AddRange(enemyUnits);

        CallPrivateMethod(manager, "CheckUnitsLife", units);

        AssertThat(playerUnit.IsAlive).IsFalse();
        AssertThat(enemyUnit.IsAlive).IsFalse();
    }

    [TestCase]
    public void CheckUnitTurnEnd_TriggersVictory()
    {
        GD.Print("[TEST] Start CheckUnitTurnEnd_TriggersVictory");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player");
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy", 0);

        SetPrivateField(manager, "_gameOutcome", GameOutcome.Victory);
        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);

        CallPrivateMethod(manager, "LoadUnits");

        CallPrivateMethod(manager, "CheckUnitTurnEnd");

        GameOutcome outcome = GetPrivateField<GameOutcome>(manager, "_gameOutcome");
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_playerUnits")[0].IsAlive).IsTrue();
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits")[0].IsAlive).IsFalse();
        AssertThat(outcome).IsEqual(GameOutcome.Victory);
    }

    [TestCase]
    public void HandlePlayerUnitMove_CellNotReachable_ReenablesInput()
    {
        GD.Print("[TEST] Start HandlePlayerUnitMove_CellNotReachable_ReenablesInput");

        GameManager manager = AddNode(new GameManager());

        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());
        manager.Set("_battleInputSystemContainer", inputSystem);

        SetPrivateField(
            manager,
            "_currentUnitPossibleMoves",
            new List<(int, int, int)>()
        );

        CallPrivateMethod(
            manager,
            "HandlePlayerUnitMove",
            new Vector3I(1, 0, 1)
        );

        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsTrue();
    }

    [TestCase]
    public void HandlePlayerUnitMove_ValidMove()
    {
        GD.Print("[TEST] Start HandlePlayerUnitMove_ValidMove_ClearsPossibleMoves");

        GameManager manager = AddNode(new GameManager());
        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());
        TestConcreteMapSystem mapSystem = AddNode(new TestConcreteMapSystem());

        SetPrivateField(manager, "_battleInputSystemContainer", inputSystem);
        SetPrivateField(manager, "_mapSystemContainer", mapSystem);

        SetPrivateField(
            manager,
            "_currentUnitPossibleMoves",
            new List<(int, int, int)> { (1, 0, 1) }
        );

        CallPrivateMethod(
            manager,
            "HandlePlayerUnitMove",
            new Vector3I(1, 0, 1)
        );

        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsTrue();
    }

    [TestCase]
    public void HandlePlayerSelectTarget_CellNotReachable_ReenablesInput()
    {
        GD.Print("[TEST] Start HandlePlayerSelectTarget_CellNotReachable_ReenablesInput");

        GameManager manager = AddNode(new GameManager());
        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());
        TestConcreteMapSystem mapSystem = AddNode(new TestConcreteMapSystem());

        SetPrivateField(manager, "_battleInputSystemContainer", inputSystem);
        SetPrivateField(manager, "_mapSystemContainer", mapSystem);

        SetPrivateField(
            manager,
            "_currentUnitReachableCellsForCurrentSelectedSkill",
            new List<(int, int, int)>()
        );

        CallPrivateMethod(
            manager,
            "HandlePlayerSelectTarget",
            new Vector3I(2, 0, 2)
        );

        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsTrue();
    }

    #endregion

    #region Private Utilities

    private static T GetPrivateField<T>(object obj, string field)
    {
        return (T)obj
            .GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(obj)!;
    }

    private static void SetPrivateField(object obj, string field, object value)
    {
        obj
            .GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(obj, value);
    }

    private static object? CallPrivateMethod(object obj, string methodName, params object?[] parameters)
    {
        MethodInfo method = obj
                .GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException($"Method '{methodName}' not found on type '{obj.GetType().Name}'.");

        return method.Invoke(obj, parameters);
    }

    #endregion
}
