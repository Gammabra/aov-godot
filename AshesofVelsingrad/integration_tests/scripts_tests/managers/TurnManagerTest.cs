using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Helpers.Systems;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Managers;

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
        {
            // Tests may have already freed nodes manually (e.g.
            // ExitTree_ClearsStaticInstance). IsInstanceValid is the canonical Godot way
            // to check whether a C# wrapper is still pointing at a live native node.
            if (GodotObject.IsInstanceValid(node))
                node.QueueFree();
        }

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

    [TestCase]
    public void ExitTree_ClearsStaticInstance()
    {
        // Regression test for the Try Again / scene reload bug. Without this, the static
        // Instance kept pointing at the unloaded TurnManager, causing the next scene's
        // TurnManager to QueueFree itself as a duplicate.
        TurnManager manager = AddNode(new TurnManager());
        manager.Call("Initialize");

        object? instanceBefore = typeof(TurnManager)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null);
        AssertThat(instanceBefore).IsNotNull();

        // Simulate the scene unload by removing the node from the tree. RemoveChild fires
        // _ExitTree synchronously — that's all we need to test the static-clear behaviour.
        // We then Free() the node here (synchronous) and drop it from _testNodes so the
        // AfterTest cleanup doesn't double-free it.
        manager.GetParent().RemoveChild(manager);
        _testNodes.Remove(manager);
        manager.Free();

        object? instanceAfter = typeof(TurnManager)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null);
        AssertThat(instanceAfter).IsNull();
    }

    [TestCase]
    public void InitializeTurnOrder_ThreeArg_AcceptsAlliesBetweenPlayerAndEnemy()
    {
        // Regression test for the three-faction overload added in the HUD/faction port.
        TurnManager manager = AddNode(new TurnManager());

        TestConcreteUnitSystem player = CreateUnit("Player", 100);
        TestConcreteUnitSystem ally = CreateUnit("Ally", 80);
        TestConcreteUnitSystem enemy = CreateUnit("Enemy", 60);

        AddNode(player);
        AddNode(ally);
        AddNode(enemy);

        manager.InitializeTurnOrder(
            new List<IUnitSystem> { player },
            new List<IUnitSystem> { ally },
            new List<IUnitSystem> { enemy });

        // Highest speed acts first, so the active unit at index 0 must be the player.
        IUnitSystem first = manager.GetCurrentUnit();
        AssertThat(first).IsEqual(player);
    }

    [TestCase]
    public void GetUpcomingUnits_FiltersOutDeadUnits()
    {
        // Verifies the wrap-around skip-dead behaviour added by AdvanceToNextLiveUnit and
        // also exercised by GetUpcomingUnits when feeding the HUD turn-order strip.
        TurnManager manager = AddNode(new TurnManager());

        TestConcreteUnitSystem alivePlayer = CreateUnit("AlivePlayer", 200);
        TestConcreteUnitSystem deadPlayer = CreateUnit("DeadPlayer", 150);
        TestConcreteUnitSystem enemy = CreateUnit("Enemy", 100);

        AddNode(alivePlayer);
        AddNode(deadPlayer);
        AddNode(enemy);

        // Kill the second player BEFORE feeding the turn manager.
        deadPlayer.Hp = 0;
        deadPlayer.SetIsAlive(false);

        manager.InitializeTurnOrder(
            new List<IUnitSystem> { alivePlayer, deadPlayer },
            new List<IUnitSystem>(),
            new List<IUnitSystem> { enemy });

        IReadOnlyList<IUnitSystem> upcoming = manager.GetUpcomingUnits();
        AssertThat(upcoming).Contains(alivePlayer);
        AssertThat(upcoming).Contains(enemy);
        // Dead unit should not appear in the next-up queue.
        bool hasDead = false;
        foreach (IUnitSystem u in upcoming)
            if (u == deadPlayer) hasDead = true;
        AssertThat(hasDead).IsFalse();
    }

    #endregion
}
