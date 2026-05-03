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
public partial class AovPlayerTest
{
	private List<Node> _testNodes = new();
	private Node? _root;

	#region Helpers

	private T AddToTestRoot<T>(T node)
		where T : Node
	{
		if (_root == null)
			throw new InvalidOperationException("Root not initialized");

		_root.AddChild(node);
		_testNodes.Add(node);
		return node;
	}

	private AovPlayer CreatePlayerWithDependencies()
	{
		AovPlayer player = new();

		AnimatedSprite3D sprite = new();
		SpringArm3D spring = new();
		InteractionComponent interaction = new();

		AddToTestRoot(player);

		player.AddChild(sprite);
		player.AddChild(spring);
		player.AddChild(interaction);
		player.Set("_animatedSprite3DPath", sprite.GetPath());
		player.Set("_springArm3DPath", spring.GetPath());
		player.Set("_interactionComponentPath", interaction.GetPath());

		return player;
	}

	private T? GetPrivateField<T>(object obj, string fieldName)
	{
		FieldInfo? field = obj.GetType()
			.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

		return (T?)field?.GetValue(obj);
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

	[TestCase]
	public void Ready_RemovesDuplicateInstances()
	{
		AovPlayer first = CreatePlayerWithDependencies();
		first._Ready();

		AovPlayer second = CreatePlayerWithDependencies();
		second._Ready();

		AssertThat(second.IsQueuedForDeletion()).IsTrue();
	}

	[TestCase]
	public void Input_Interact_DoesNothing_WhenNoInteractable()
	{
		AovPlayer player = CreatePlayerWithDependencies();
		player._Ready();

		InputEventAction action = new()
		{
			Action = "interact",
			Pressed = true
		};

		player._Input(action);

		AssertThat(player).IsNotNull(); // no crash
	}

	#region Test Doubles

	private partial class TestInteractable : Node3D, IInteractable
	{
		public bool Interacted { get; private set; }

		public bool CanInteract() => true;

		public void Interact(IInteractor interactor)
		{
			Interacted = true;
		}

		public void ShowPrompt()
		{
			throw new NotImplementedException();
		}

		public void HidePrompt()
		{
			throw new NotImplementedException();
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
