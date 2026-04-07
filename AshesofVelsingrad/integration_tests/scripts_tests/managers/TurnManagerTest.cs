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
public class TurnManagerTest
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
        typeof(TurnManager)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null, null);
    }

    private TestConcreteUnitSystem CreateUnit(string name, int speed)
    {
        TestConcreteUnitSystem unit = new(baseSpeed: speed);
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
        TurnManager manager = AddNode(new TurnManager());
        manager.Call("Initialize");

        object? instance = typeof(TurnManager)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null);

        AssertThat(instance).IsNotNull();
        AssertThat(instance).IsEqual(manager);
    }

    [TestCase]
    public void InitializeTurnOrder_SortsBySpeed()
    {
        TurnManager manager = AddNode(new TurnManager());

        TestConcreteUnitSystem player = CreateUnit("Player", 10);
        TestConcreteUnitSystem enemy = CreateUnit("Enemy", 20);

        AddNode(player);
        AddNode(enemy);

        manager.InitializeTurnOrder(new List<IUnitSystem> { player }, new List<IUnitSystem> { enemy });

        IUnitSystem first = manager.GetCurrentUnit();
        AssertThat(first).IsEqual(enemy);
    }

    [TestCase]
    public void EndTurnManagerLoop_SetsFinishedState()
    {
        TurnManager manager = AddNode(new TurnManager());
        manager.Call("Initialize");
        manager.Call("EndTurnManagerLoop");

        object? currentState = typeof(TurnManager)
            .GetField("_currentTurnState", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(manager);

        AssertThat(currentState).IsEqual(AovDataStructures.TurnState.Finished);
    }

    #endregion
}
