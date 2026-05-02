using Godot;

namespace AshesOfVelsingrad.player;

public sealed partial class AovPlayer : CharacterBody3D
{
	[Export]
	private NodePath? _spritePath;

	[Export]
	private NodePath? _springArm3DPath;

	[Export]
	private float _speed = 4;

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private Sprite3D? _sprite3D;
	private SpringArm3D? _springArm3D;
	private static AovPlayer? _instance;

	public override void _Ready()
	{
		if (IsInsideTree() && GetParent() == GetTree().Root)
		{
			Initialize();
		}
		// For manual instances, check for duplicates.
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

	private void Initialize()
	{
		_sprite3D = GetNode<Sprite3D>(_spritePath);
		_springArm3D = GetNode<SpringArm3D>(_springArm3DPath);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_springArm3D is null)
			return;

		Vector3 currentVelocity = Velocity;

		Vector3 inputDir = new(
			Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
			0,
			Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up")
		);

		Vector3 moveDirection = inputDir.Rotated(Vector3.Up, _springArm3D.Rotation.Y).Normalized();

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
	}
}
