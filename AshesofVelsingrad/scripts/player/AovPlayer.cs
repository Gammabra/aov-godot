using System;
using AshesOfVelsingrad.Audio;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Inventory;
using Godot;

namespace AshesOfVelsingrad.player;

/// <summary>
/// Represents the main player character in the game.
/// Handles movement, gravity, and initialization of required nodes.
/// </summary>
public sealed partial class AovPlayer : CharacterBody3D, IInteractor
{
	[Export]
	private NodePath? _stateMachinePath;

	[Export]
	private NodePath? _springArm3DPath;

	[Export]
	private NodePath? _interactionComponentPath;

	[Export]
	private NodePath? _animatedSprite3DPath;

	[Export]
	private float _speed = 4;

	/// <summary>Direct path to a pre-placed ExplorationInventoryUI node inside your UILayer scene structure.</summary>
	[Export]
	private NodePath? _explorationInventoryUiPath;

	[Export]
	private NodePath _tutorialManagerPath = null!;

	/// <summary>Optional container path to house the inventory UI if spawned procedurally at runtime.</summary>
	[Export]
	private NodePath? _uiContainerPath;

	private bool _isLock;

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private StateMachine? _stateMachine;

	private SpringArm3D _springArm3D = null!;
	private InteractionComponent? _interactionComponent;
	private AnimatedSprite3D _animatedSprite3D = null!;
	private static AovPlayer? _instance;
	private ExplorationInventoryUI? _explorationInventoryUI;
	private TutorialManager _tutorialManager = null!;
	private bool _isTutorial;

	private void Initialize()
	{
		_stateMachine = GetNode<StateMachine>(_stateMachinePath);
		_springArm3D = GetNode<SpringArm3D>(_springArm3DPath);
		_interactionComponent = GetNode<InteractionComponent>(_interactionComponentPath);
		_animatedSprite3D = GetNode<AnimatedSprite3D>(_animatedSprite3DPath);
		try
		{
			_tutorialManager = GetNode<TutorialManager>(_tutorialManagerPath);
			_isTutorial = true;
			_interactionComponent.CanInteract = false;
		}
		catch
		{
			_isTutorial = false;
			_interactionComponent.CanInteract = true;
		}

		_instance = this;

		// If we just returned from a battle (player pressed Forfeit or Continue), the
		// BattleLauncher autoload still has a pending position from when the encounter
		// was triggered. Snap to it so the player wakes up exactly where they were
		// standing when they spoke to the NPC.
		Vector3? returnPos = BattleLauncher.Instance?.ConsumePendingReturnPosition();
		if (returnPos is { } pos)
		{
			GlobalPosition = pos;
			GD.Print($"AovPlayer: restored return position {pos} from BattleLauncher.");
		}

		// The player only spawns inside exploration scenes — battle scenes spawn unit nodes
		// through GameManager instead. So an active AovPlayer is a reliable signal that
		// we're now exploring, and the audio manager can switch off the menu theme accordingly.
		AudioManager.Instance?.SetMusicContext(MusicContext.Exploration);

		ResolveExplorationInventoryUI();
	}

	/// <summary>
	/// Evaluates scene architecture options to link or dynamically inject the Exploration UI layer.
	/// </summary>
	private void ResolveExplorationInventoryUI()
	{
		// Step 0: MainManager owns the persistent ExplorationInventoryUI in UILayer
		if (MainManager.Instance is { } manager)
		{
			_explorationInventoryUI = manager.GetExplorationInventoryUI();
			if (_explorationInventoryUI is not null)
			{
				GD.Print("AovPlayer: ExplorationInventoryUI found via MainManager.");
				_explorationInventoryUI.RefreshUnitPanels();
				return;
			}
		}

		// Step 1: Attempt direct resolution via explicit NodePath mapping
		if (_explorationInventoryUiPath is not null && !_explorationInventoryUiPath.IsEmpty)
		{
			_explorationInventoryUI = GetNodeOrNull<ExplorationInventoryUI>(_explorationInventoryUiPath);
			if (_explorationInventoryUI is not null)
			{
				GD.Print($"AovPlayer: ExplorationInventoryUI found via explicit NodePath alignment.");
				_explorationInventoryUI.RefreshUnitPanels();
				return;
			}
		}

		// Step 2: Contextual recursive search of the active exploration scene layout
		SceneTree tree = GetTree();
		Node? currentSceneRoot = tree.CurrentScene;
		if (currentSceneRoot is not null)
		{
			_explorationInventoryUI = FindInventoryUiIn(currentSceneRoot);
			if (_explorationInventoryUI is not null)
			{
				GD.Print($"AovPlayer: ExplorationInventoryUI resolved contextually from scene hierarchy.");
				_explorationInventoryUI.RefreshUnitPanels();
				return;
			}
		}

		// Step 3: Procedural generation fallback if no pre-built design layer exists
		_explorationInventoryUI = new ExplorationInventoryUI { Name = "ExplorationInventoryUI" };

		Node host = currentSceneRoot ?? tree.Root;
		if (_uiContainerPath is not null && !_uiContainerPath.IsEmpty)
		{
			Node? container = GetNodeOrNull(_uiContainerPath);
			if (container is not null) host = container;
		}

		host.CallDeferred(Node.MethodName.AddChild, _explorationInventoryUI);
		_explorationInventoryUI.RefreshUnitPanels();

		GD.Print($"AovPlayer: ExplorationInventoryUI spawned procedurally under host layout: '{host.Name}'");
	}

	private static ExplorationInventoryUI? FindInventoryUiIn(Node root)
	{
		if (root is ExplorationInventoryUI ui) return ui;
		foreach (Node child in root.GetChildren())
		{
			ExplorationInventoryUI? found = FindInventoryUiIn(child);
			if (found is not null) return found;
		}
		return null;
	}

	public override void _Ready()
	{
		if (IsInsideTree() && GetParent() == GetTree().Root)
		{
			Initialize();
		}
		else if (_instance == null)
		{
			Initialize();
		}
		else
		{
			GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
			QueueFree();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_isLock) return;

		if (_isTutorial && !_tutorialManager.CanMove)
			return;

		if (_isTutorial && !_tutorialManager.IsOnlyToggleInventory)
		{
			if (@event.IsActionPressed("open_inventory"))
				_explorationInventoryUI?.Toggle();
			return;
		}

		if (@event.IsActionPressed("interact"))
		{
			IInteractable? interactable = _interactionComponent?.ClosestInteractable as IInteractable;

			if (interactable?.CanInteract() == true)
			{
				interactable.Interact(this);
			}
		}

		if (_isTutorial && !_tutorialManager.CanToggleInventory)
			return;
		if (@event.IsActionPressed("open_inventory"))
			_explorationInventoryUI?.Toggle();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isLock)
		{
			_stateMachine?.TransitionTo("IdleState");
			return;
		}

		if (_isTutorial && !_tutorialManager.CanMove)
		{
			_stateMachine?.TransitionTo("IdleState");
			return;
		}

		Vector3 currentVelocity = Velocity;

		Vector3 inputDir = new(
			Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
			0,
			Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up")
		);

		Vector3 moveDirection = inputDir.Rotated(Vector3.Up, _springArm3D.Rotation.Y).Normalized();

		Vector3 dir = moveDirection;
		dir.Y = 0;

		if (dir.Length() > 0.1f)
		{
			float newPitchAngle = Mathf.Atan2(dir.X, dir.Z);
			_animatedSprite3D.Rotation = new Vector3(0, newPitchAngle, 0);
		}

		currentVelocity.X = moveDirection.X * _speed;
		currentVelocity.Z = moveDirection.Z * _speed;

		if (!IsOnFloor())
		{
			currentVelocity.Y -= _gravity * (float)delta;
		}
		else if (currentVelocity.Y < 0)
		{
			currentVelocity.Y = -0.1f;
		}

		Velocity = currentVelocity;
		MoveAndSlide();

		Vector3 velocity = currentVelocity;
		velocity.Y = 0;

		if (velocity.Length() < 0.1f)
		{
			_stateMachine?.TransitionTo("IdleState");
			return;
		}

		velocity = velocity.Normalized();

		Vector3 forward = -_springArm3D.GlobalTransform.Basis.Z;
		Vector3 right = _springArm3D.GlobalTransform.Basis.X;
		float forwardDot = forward.Dot(velocity);
		float rightDot = right.Dot(velocity);
		float angle = Mathf.Atan2(rightDot, forwardDot);
		float direction = _animatedSprite3D.GlobalRotation.Y - Mathf.RadToDeg(angle);

		if (direction > -45 && direction < 45)
			_stateMachine?.TransitionTo("WalkForwardState");
		if (direction > 45 && direction <= 135)
			_stateMachine?.TransitionTo("WalkLeftState");
		if (direction > -135 && direction <= -45)
			_stateMachine?.TransitionTo("WalkRightState");
		if (direction > 135 || direction < -135)
			_stateMachine?.TransitionTo("WalkBackwardState");
	}

	public void LockInteractor()
	{
		_isLock = true;
	}

	public void UnlockInteractor()
	{
		_isLock = false;
	}

	public void BindInventoryUnits()
	{
		_explorationInventoryUI?.RefreshUnitPanels();
	}

	public void ToggleInteraction(bool active)
	{
		if (_interactionComponent == null)
			return;
		_interactionComponent.CanInteract = active;
	}

	public override void _ExitTree()
	{
		// Only free if we spawned it ourselves (no MainManager) AND it landed on root
		if (_explorationInventoryUI is not null && IsInstanceValid(_explorationInventoryUI))
		{
			bool ownedByMainManager = MainManager.Instance is not null;
			if (!ownedByMainManager && _explorationInventoryUI.GetParent() == GetTree().Root)
				_explorationInventoryUI.QueueFree();
		}

		if (_instance == this) _instance = null;
		base._ExitTree();
	}
}
