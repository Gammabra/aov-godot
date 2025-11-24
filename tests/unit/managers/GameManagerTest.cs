using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using GdUnit4;
using Godot;
using Godot.Collections;
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

    private TestConcreteUnitSystem CreateUnit(string name, int hp = 100)
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

        object playerUnitsObj = manager.Get("_playerUnits");
        object enemyUnitsObj = manager.Get("_enemyUnits");

        List<UnitSystem>? playerUnits = playerUnitsObj as List<UnitSystem>;
        List<UnitSystem>? enemyUnits = enemyUnitsObj as List<UnitSystem>;

        if (playerUnits is null || enemyUnits is null)
            return;
        AssertThat(playerUnits.Count).IsEqual(1);
        AssertThat(enemyUnits.Count).IsEqual(1);
    }

    [TestCase]
    public void CheckWinLoseCondition_SetsVictory_WhenNoEnemiesAlive()
    {
        GameManager manager = AddNode(new GameManager());
        SetPrivateField(manager, "_gameOutcome", GameOutcome.Ongoing);
        TurnManager turnManager = new();
        manager.Set("_turnManagerContainer", turnManager);

        TestConcreteUnitSystem playerUnit = CreateUnit("Player");
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy", 0);
        enemyUnit.SetIsAlive(false);

        List<UnitSystem> playerUnits = [];
        playerUnits.Add(playerUnit);
        List<UnitSystem> enemyUnits = [];
        enemyUnits.Add(enemyUnit);

        SetPrivateField(manager, "_playerUnits", playerUnits);
        SetPrivateField(manager, "_enemyUnits", enemyUnits);
        CallPrivateMethod(manager, "CheckWinLoseCondition");

        Variant outcome = manager.Get("_gameOutcome");
        AssertThat((int)outcome).IsEqual((int)GameOutcome.Victory);
    }

    [TestCase]
    public void CheckWinLoseCondition_SetsDefeat_WhenNoPlayersAlive()
    {
        GameManager manager = AddNode(new GameManager());
        SetPrivateField(manager, "_gameOutcome", GameOutcome.Ongoing);
        TurnManager turnManager = new();
        manager.Set("_turnManagerContainer", turnManager);

        TestConcreteUnitSystem playerUnit = CreateUnit("Player", 0);
        playerUnit.SetIsAlive(false);
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy");

        List<UnitSystem> playerUnits = [];
        playerUnits.Add(playerUnit);
        List<UnitSystem> enemyUnits = [];
        enemyUnits.Add(enemyUnit);

        SetPrivateField(manager, "_playerUnits", playerUnits);
        SetPrivateField(manager, "_enemyUnits", enemyUnits);
        CallPrivateMethod(manager, "CheckWinLoseCondition");

        Variant outcome = manager.Get("_gameOutcome");
        AssertThat((int)outcome).IsEqual((int)GameOutcome.Defeat);
    }

    #endregion

    #region Private Utilities

    private void SetPrivateField(object obj, string field, object value)
    {
        obj
            .GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(obj, value);
    }

    private object? CallPrivateMethod(object obj, string methodName, params object?[] parameters)
    {
        MethodInfo method = obj
                .GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException($"Method '{methodName}' not found on type '{obj.GetType().Name}'.");

        return method.Invoke(obj, parameters);
    }

    #endregion
}
