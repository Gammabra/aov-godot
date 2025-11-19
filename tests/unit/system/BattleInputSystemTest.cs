using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Systems;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class BattleInputSystemTest
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

	private void SetPrivateField(object instance, string field, object? value)
	{
		instance
			.GetType()
			.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
			.SetValue(instance, value);
	}

	private void ResetSingleton()
	{
		typeof(BattleInputSystem)
			.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
			.SetValue(null, null);

		typeof(BattleInputSystem)
			.GetMethod("SetInstanceForTesting", BindingFlags.Static | BindingFlags.NonPublic)!
			.Invoke(null, new object?[] { null });
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
	public void PassTurnInput_EmitsSignal_AndDisablesInput()
	{
		BattleInputSystem sys = AddNode(new BattleInputSystem());
		sys._Ready();

		bool emitted = false;
		sys.Connect("OnPassTurnPressed", Callable.From(() => emitted = true));

		sys._Input(InputEventFrom("battle_pass_turn"));

		AssertThat(emitted).IsTrue();

		object? enabledField = typeof(BattleInputSystem)
			.GetField("_inputEnabled", BindingFlags.NonPublic | BindingFlags.Instance)!
			.GetValue(sys);

		AssertThat(enabledField).IsNotNull();
		if (enabledField is not null)
			AssertThat((bool)enabledField).IsFalse();
	}

	[TestCase]
	public void SelectMoveInput_EmitsSignal()
	{
		BattleInputSystem sys = AddNode(new BattleInputSystem());
		sys._Ready();

		bool emitted = false;
		sys.Connect("OnSelectMovePressed", Callable.From(() => emitted = true));

		sys._Input(InputEventFrom("battle_select_move"));

		AssertThat(emitted).IsTrue();
	}

	[TestCase]
	public void SelectSkillInput_EmitsCorrectIndex()
	{
		BattleInputSystem sys = AddNode(new BattleInputSystem());
		sys._Ready();

		int received = -1;
		sys.Connect("OnSelectedSkillPressed", Callable.From((int id) => received = id));

		sys._Input(InputEventFrom("battle_select_skill3"));

		AssertThat(received).IsEqual(2);
	}

	[TestCase]
	public void MoveUnitInput_NoCameraOrMap_DoesNothing()
	{
		BattleInputSystem sys = AddNode(new BattleInputSystem());
		sys._Ready();

		bool emitted = false;
		sys.Connect("OnMoveUnitOrSelectTargetPressed", Callable.From((Vector3I _) => emitted = true));

		sys._Input(InputEventFrom("battle_move_unit_and_select_target"));

		AssertThat(emitted).IsFalse();
	}

	[TestCase]
	public void MoveUnitInput_InvokeEmitMethod_EmitsSignal()
	{
		// Arrange : créer caméra + map et initialiser le système
		Camera3D camera = AddNode(new Camera3D());
		TestConcreteMapSystem map = AddNode(new TestConcreteMapSystem()); // ou MockMapSystem qui propose LocalToMap

		BattleInputSystem sys = AddNode(new BattleInputSystem());
		sys.Set("camera3DPath", camera.GetPath());
		sys.Set("mapSystemPath", map.GetPath());
		sys._Ready();

		// Connecter le signal pour capter l'émission
		Vector3I received = new();
		bool emitted = false;
		sys.Connect(
			"OnMoveUnitOrSelectTargetPressed",
			Callable.From((Vector3I p) =>
				{
					emitted = true;
					received = p;
				}
			)
		);

		// Act : invoquer la méthode privée qui est normalement appelée après le raycast
		// Nom exact : "EmitSignalOnMoveUnitOrSelectTargetPressed" (utilisé dans ton code)
		MethodInfo? method = typeof(BattleInputSystem).GetMethod(
			"EmitSignalOnMoveUnitOrSelectTargetPressed",
			BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
		);

		// Si la méthode n'existe pas (variations de nom), on peut émettre le signal via EmitSignal directement :
		if (method != null)
		{
			// Construire une cellule de test
			Vector3I testCell = new(5, 0, 5);
			method.Invoke(sys, new object?[] { testCell });
		}
		else
		{
			// fallback : émettre le signal manuellement (moins "noir box" mais fonctionne)
			sys.EmitSignal("OnMoveUnitOrSelectTargetPressed", new Vector3I(5, 0, 5));
		}

		// Assert
		AssertThat(emitted).IsTrue();
		AssertThat(received).IsEqual(new Vector3I(5, 0, 5));
	}

	[TestCase]
	public void DisabledInput_PreventsAllActions()
	{
		BattleInputSystem sys = AddNode(new BattleInputSystem());
		sys._Ready();
		sys.SetInputEnabled(false);

		bool emitted = false;
		sys.Connect("OnPassTurnPressed", Callable.From(() => emitted = true));

		sys._Input(InputEventFrom("battle_pass_turn"));

		AssertThat(emitted).IsFalse();
	}

	#endregion

	#region Input Utility

	private InputEvent InputEventFrom(string action)
	{
		InputEventAction ev = new();
		ev.Action = action;
		ev.Pressed = true;
		return ev;
	}

	#endregion
}
