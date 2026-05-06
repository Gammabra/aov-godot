using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
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

    private bool _isLock;

    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private StateMachine? _stateMachine;
    // null! — populated in Initialize() via the [Export] NodePath. Used non-null in
    // _PhysicsProcess; Godot's lifecycle guarantees Initialize runs before any frame
    // callback touches these, so the suppression matches the runtime invariant.
    private SpringArm3D _springArm3D = null!;
    private InteractionComponent? _interactionComponent;
    private AnimatedSprite3D _animatedSprite3D = null!;
    private static AovPlayer? _instance;

    private void Initialize()
    {
        _stateMachine = GetNode<StateMachine>(_stateMachinePath);
        _springArm3D = GetNode<SpringArm3D>(_springArm3DPath);
        _interactionComponent = GetNode<InteractionComponent>(_interactionComponentPath);
        _animatedSprite3D = GetNode<AnimatedSprite3D>(_animatedSprite3DPath);
        _instance = this;

        // If we just returned from a battle (player pressed Forfeit or Continue), the
        // BattleLauncher autoload still has a pending position from when the encounter
        // was triggered. Snap to it so the player wakes up exactly where they were
        // standing when they spoke to the NPC. ConsumePendingReturnPosition is one-shot:
        // calling it clears the value so subsequent scene loads don't re-snap.
        Vector3? returnPos = BattleLauncher.Instance?.ConsumePendingReturnPosition();
        if (returnPos is { } pos)
        {
            GlobalPosition = pos;
            GD.Print($"AovPlayer: restored return position {pos} from BattleLauncher.");
        }
    }

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

    /// <inheritdoc />
    /// <remarks>
    ///     Clear the static <see cref="_instance" /> when this node leaves the tree
    ///     (scene unload, scene reload via <c>BattleLauncher</c>'s Forfeit / Continue
    ///     flow). Without this, the new <see cref="AovPlayer" /> on the reloaded
    ///     exploration scene sees a stale <c>_instance</c> still pointing at the just-
    ///     unloaded player and <see cref="Node.QueueFree" />s itself as a "duplicate" —
    ///     the scene then renders with no player and frozen camera.
    /// </remarks>
    public override void _ExitTree()
    {
        if (_instance == this) _instance = null;
        base._ExitTree();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("interact"))
        {
            IInteractable? interactable = _interactionComponent?.ClosestInteractable as IInteractable;

            if (interactable?.CanInteract() == true)
            {
                interactable.Interact(this);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isLock)
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
}
