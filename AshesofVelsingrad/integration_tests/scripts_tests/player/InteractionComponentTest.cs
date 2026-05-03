using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.player;
using AshesOfVelsingrad.Systems;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Player;

[TestSuite]
[RequireGodotRuntime]
public partial class InteractionComponentTest
{
    private readonly List<Node> _testNodes = [];
    private Node? _root;

    #region Helpers

    private T AddToTestRoot<T>(T node) where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Root not initialized");

        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    private InteractionComponent CreateComponent(out AnimatedSprite3D sprite)
    {
        InteractionComponent component = new();
        sprite = new AnimatedSprite3D();

        AddToTestRoot(component);
        component.AddChild(sprite);

        component.Set("_animatedSprite3DPath", sprite.GetPath());

        component._Ready();
        return component;
    }

    private void InvokePrivate(object obj, string method, params object[] args)
    {
        MethodInfo? m = obj.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);

        m?.Invoke(obj, args);
    }

    #endregion

    [BeforeTest]
    public void SetUp()
    {
        _testNodes.Clear();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);
    }

    // --------------------------------------------------
    // ENTRY / EXIT
    // --------------------------------------------------

    [TestCase]
    public void BodyEntered_AddsInteractable()
    {
        InteractionComponent component = CreateComponent(out _);
        TestInteractable obj = AddToTestRoot(new TestInteractable());

        InvokePrivate(component, "OnBodyEntered", obj);

        component._Process(0.1);

        AssertThat(component.ClosestInteractable).IsEqual(obj);
    }

    [TestCase]
    public void BodyEntered_IgnoresNonInteractable()
    {
        InteractionComponent component = CreateComponent(out _);
        Node3D obj = AddToTestRoot(new Node3D());

        InvokePrivate(component, "OnBodyEntered", obj);

        component._Process(0.1);

        AssertThat(component.ClosestInteractable).IsNull();
    }

    [TestCase]
    public void BodyExited_RemovesInteractable()
    {
        InteractionComponent component = CreateComponent(out _);
        TestInteractable obj = AddToTestRoot(new TestInteractable());

        InvokePrivate(component, "OnBodyEntered", obj);
        InvokePrivate(component, "OnBodyExited", obj);

        component._Process(0.1);

        AssertThat(component.ClosestInteractable).IsNull();
    }

    // --------------------------------------------------
    // CLOSEST SELECTION
    // --------------------------------------------------

    [TestCase]
    public void Process_SelectsClosestInteractable()
    {
        InteractionComponent component = CreateComponent(out AnimatedSprite3D sprite);

        TestInteractable near = AddToTestRoot(new TestInteractable());
        TestInteractable far = AddToTestRoot(new TestInteractable());

        sprite.GlobalPosition = Vector3.Zero;
        near.GlobalPosition = new Vector3(1, 0, 0);
        far.GlobalPosition = new Vector3(10, 0, 0);

        InvokePrivate(component, "OnBodyEntered", near);
        InvokePrivate(component, "OnBodyEntered", far);

        component._Process(0.1);

        AssertThat(component.ClosestInteractable).IsEqual(near);
    }

    // --------------------------------------------------
    // PROMPT MANAGEMENT
    // --------------------------------------------------

    [TestCase]
    public void Process_ShowsPrompt_OnClosest()
    {
        InteractionComponent component = CreateComponent(out AnimatedSprite3D sprite);

        TestInteractable obj = AddToTestRoot(new TestInteractable());

        sprite.GlobalPosition = Vector3.Zero;
        obj.GlobalPosition = new Vector3(1, 0, 0);

        InvokePrivate(component, "OnBodyEntered", obj);

        component._Process(0.1);

        AssertThat(obj.ShowCalled).IsTrue();
        AssertThat(obj.HideCalled).IsFalse();
    }

    [TestCase]
    public void Process_HidesPrompt_OnNonClosest()
    {
        InteractionComponent component = CreateComponent(out AnimatedSprite3D sprite);

        TestInteractable near = AddToTestRoot(new TestInteractable());
        TestInteractable far = AddToTestRoot(new TestInteractable());

        sprite.GlobalPosition = Vector3.Zero;
        near.GlobalPosition = new Vector3(1, 0, 0);
        far.GlobalPosition = new Vector3(10, 0, 0);

        InvokePrivate(component, "OnBodyEntered", near);
        InvokePrivate(component, "OnBodyEntered", far);

        component._Process(0.1);

        AssertThat(near.ShowCalled).IsTrue();
        AssertThat(far.HideCalled).IsTrue();
    }

    // --------------------------------------------------
    // EXIT BEHAVIOR
    // --------------------------------------------------

    [TestCase]
    public void BodyExited_ClearsClosest_WhenEmpty()
    {
        InteractionComponent component = CreateComponent(out _);
        TestInteractable obj = AddToTestRoot(new TestInteractable());

        InvokePrivate(component, "OnBodyEntered", obj);
        InvokePrivate(component, "OnBodyExited", obj);

        component._Process(0.1);

        AssertThat(component.ClosestInteractable).IsNull();
    }

    #region Test Double

    private partial class TestInteractable : Node3D, IInteractable
    {
        public bool ShowCalled { get; private set; }
        public bool HideCalled { get; private set; }

        public bool CanInteract() => true;

        public void Interact(IInteractor interactor) { }

        public void ShowPrompt()
        {
            ShowCalled = true;
        }

        public void HidePrompt()
        {
            HideCalled = true;
        }
    }

    #endregion

    [AfterTest]
    public void TearDown()
    {
        foreach (Node node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
                node.QueueFree();
        }

        _testNodes.Clear();
    }
}
