using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.player;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.UI.Inventory;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Player;

[TestSuite]
[RequireGodotRuntime]
public class AovPlayerTest
{
    private Node? _root;

    private List<Node> _nodesToFree = new();

    #region Helpers

    private void AddToTestRoot(Node node)
    {
        if (_root == null) throw new InvalidOperationException("Root not initialized");
        _root.AddChild(node);
    }

    private void ClearSingleton<T>() where T : class
    {
        typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            ?.GetSetMethod(true)
            ?.Invoke(null, new object?[] { null }); // null value, not empty array
        typeof(T).GetField("Instance", BindingFlags.Public | BindingFlags.Static)
            ?.SetValue(null, null);
    }

    private AovPlayer CreatePlayerWithDependencies(bool mockInventoryUi = true, bool addToTree = true)
    {
        AovPlayer player = new();
        _nodesToFree.Add(player);

        AnimatedSprite3D sprite = new() { Name = "Sprite" };
        SpringArm3D spring = new() { Name = "Spring" };
        InteractionComponent interaction = new() { Name = "Interact" };
        StateMachine stateMachine = new() { Name = "State", InitialState = null! };

        _nodesToFree.Add(sprite);
        _nodesToFree.Add(spring);
        _nodesToFree.Add(interaction);
        _nodesToFree.Add(stateMachine);

        player.AddChild(sprite);
        player.AddChild(spring);
        player.AddChild(interaction);
        player.AddChild(stateMachine);

        player.Set("_animatedSprite3DPath", new NodePath(sprite.Name));
        player.Set("_springArm3DPath", new NodePath(spring.Name));
        player.Set("_interactionComponentPath", new NodePath(interaction.Name));
        player.Set("_stateMachinePath", new NodePath(stateMachine.Name));

        if (mockInventoryUi)
        {
            MockExplorationInventoryUI dummyUi = new() { Name = "DummyUI" };
            _nodesToFree.Add(dummyUi);

            // 1. Group them securely offline
            Node container = new Node { Name = "PlayerContainer" };
            _nodesToFree.Add(container);

            container.AddChild(dummyUi);
            container.AddChild(player);

            // 2. Safely generate a Godot engine-certified relative path
            player.Set("_explorationInventoryUiPath", player.GetPathTo(dummyUi));

            // 3. Mount to tree (Triggers _Ready on the player)
            if (addToTree)
                AddToTestRoot(container);
        }
        else if (addToTree)
        {
            AddToTestRoot(player);
        }

        return player;
    }

    private T? GetPrivateField<T>(object obj, string fieldName)
    {
        FieldInfo? field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T?)field?.GetValue(obj);
    }

    #endregion

    [BeforeTest]
    public void SetUp()
    {
        _nodesToFree = new System.Collections.Generic.List<Node>();
        ClearSingleton<MainManager>();
        ClearSingleton<AudioManager>();
        ClearSingleton<BattleLauncher>();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
    }

    [AfterTest]
    public void TearDown()
    {
        // Free tracked nodes individually first (reverse order)
        for (int i = _nodesToFree.Count - 1; i >= 0; i--)
        {
            var node = _nodesToFree[i];
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                if (node.GetParent() != null)
                    node.GetParent().RemoveChild(node);
                node.Free();
            }
        }
        _nodesToFree.Clear();

        // Free root
        if (GodotObject.IsInstanceValid(_root))
        {
            _root.GetParent()?.RemoveChild(_root);
            _root.Free();
        }
        _root = null;

        // Clean up escaped nodes on scene root
        // Use a snapshot to avoid modifying collection while iterating
        var sceneRoot = ((SceneTree)Engine.GetMainLoop()).Root;
        var escapedNodes = new System.Collections.Generic.List<Node>();
        foreach (Node child in sceneRoot.GetChildren())
        {
            if (child is CanvasLayer or Control)
                escapedNodes.Add(child);
        }
        foreach (Node node in escapedNodes)
        {
            if (GodotObject.IsInstanceValid(node))
            {
                GD.Print($"[TEST] Freeing escaped node: {node.Name}");
                node.Free();
            }
        }

        ClearSingleton<MainManager>();
        ClearSingleton<AudioManager>();
        ClearSingleton<BattleLauncher>();

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [TestCase]
    public void Ready_RemovesDuplicateInstances()
    {
        AovPlayer first = CreatePlayerWithDependencies(addToTree: true);
        AovPlayer second = CreatePlayerWithDependencies(addToTree: true);

        // We removed the manual _Ready() calls! AddToTestRoot ran them naturally.
        AssertThat(second.IsQueuedForDeletion()).IsTrue();
    }

    [TestCase]
    public void Input_Interact_DoesNothing_WhenNoInteractable()
    {
        AovPlayer player = CreatePlayerWithDependencies(addToTree: true);

        InputEventAction action = new() { Action = "interact", Pressed = true };
        player._Input(action);

        AssertThat(player).IsNotNull();
    }

    [TestCase]
    public void ResolveInventoryUi_BindsViaExplicitPath_WhenAssigned()
    {
        AovPlayer player = CreatePlayerWithDependencies(mockInventoryUi: false, addToTree: false);

        MockExplorationInventoryUI expectedUi = new() { Name = "PrePlacedInventoryUI" };
        _nodesToFree.Add(expectedUi);

        // 1. Offline grouping for CI headless safety
        Node container = new Node { Name = "TestContainer" };
        _nodesToFree.Add(container);

        container.AddChild(expectedUi);
        container.AddChild(player);

        // 2. The Godot engine computes the perfect NodePath BEFORE entering the tree
        player.Set("_explorationInventoryUiPath", player.GetPathTo(expectedUi));

        // 3. Mount to tree (Fires _Ready)
        AddToTestRoot(container);

        var resolvedUi = GetPrivateField<ExplorationInventoryUI>(player, "_explorationInventoryUI");
        AssertThat(resolvedUi).IsNotNull();
        AssertThat(resolvedUi).IsEqual(expectedUi);
    }

    [TestCase]
    public void ResolveInventoryUi_FindsUiViaNodePath_WhenMainManagerAbsent()
    {
        // When no MainManager exists but a UI is pre-placed in scene,
        // AovPlayer should find and use it via NodePath (Step 1)
        AovPlayer player = CreatePlayerWithDependencies(mockInventoryUi: true, addToTree: true);

        var resolvedUi = GetPrivateField<ExplorationInventoryUI>(player, "_explorationInventoryUI");
        AssertThat(resolvedUi).IsNotNull();
        AssertThat(resolvedUi).IsInstanceOf<MockExplorationInventoryUI>();
    }
}
