using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Interfaces;

/// <summary>
/// Base abstract class for all NPC characters in the game.
/// Provides core behavior including navigation, movement handling,
/// and high-level AI sub-states such as idle, roaming, and target following.
/// </summary>
public abstract partial class NpcSystem : CharacterBody3D
{
    private StateMachine _stateMachine = null!;
    private NavigationAgent3D _navigationAgent = null!;
    private Timer _timer = null!;
    private Vector3 _originPoint;
    private float _roamingRadius = 30;
    private AovDataStructures.NpcSubState _subState = AovDataStructures.NpcSubState.Idle;
    private Node3D? _nodeToFollow;

    /// <summary>
    /// Minimum distance at which the NPC considers it has reached its target
    /// and stops advancing further.
    /// </summary>
    protected float StopDistance;

    /// <summary>
    /// Movement speed applied to the NPC when following a navigation path.
    /// </summary>
    protected float Speed;

    /// <summary>
    /// Signal emitted when the NPC successfully reaches a specific target point
    /// while using the "FollowSpecificPoint" behavior.
    /// </summary>
    [Signal]
    public delegate void OnSpecificPointReachedEventHandler();

    /// <summary>
    /// Handles movement logic when the NPC is following a fixed target position.
    /// The NPC moves toward the target until it is within a small threshold distance,
    /// then transitions to idle and emits a completion signal.
    /// </summary>
    private void OnFollowSpecificPoint()
    {
        if (_nodeToFollow == null)
            return;

        float distance = GlobalPosition.DistanceTo(_nodeToFollow.GlobalPosition);

        if (distance > 0.3f)
        {
            Vector3 nextPosition = _navigationAgent.GetNextPathPosition();
            Vector3 nextDirection = (nextPosition - GlobalPosition).Normalized();

            Velocity = nextDirection * Speed;

            float angleRad = Mathf.Atan2(nextDirection.X, -nextDirection.Z);
            float direction = Mathf.RadToDeg(angleRad);

            if (direction > -45 && direction < 45)
                _stateMachine.TransitionTo("WalkForwardState");
            if (direction > 45 && direction <= 135)
                _stateMachine.TransitionTo("WalkRightState");
            if (direction > -135 && direction <= -45)
                _stateMachine.TransitionTo("WalkLeftState");
            if (direction > 135 || direction < -135)
                _stateMachine.TransitionTo("WalkBackwardState");
        }
        else
        {
            ToIdle(true);
            EmitSignalOnSpecificPointReached();
        }

        MoveAndSlide();
    }

    /// <summary>
    /// Handles movement logic when the NPC is following a moving target.
    /// The navigation path is continuously updated to track the target's position.
    /// </summary>
    /// <param name="delta">Frame time step used for smoothing movement.</param>
    private void OnFollowMovingPoint(double delta)
    {
        if (_nodeToFollow == null)
            return;

        float distance = GlobalPosition.DistanceTo(_nodeToFollow.GlobalPosition);

        if (distance > StopDistance)
        {
            _navigationAgent.TargetPosition = _nodeToFollow.GlobalPosition;

            Vector3 nextPosition = _navigationAgent.GetNextPathPosition();
            Vector3 nextDirection = (nextPosition - GlobalPosition).Normalized();

            float angleRad = Mathf.Atan2(nextDirection.X, -nextDirection.Z);
            float direction = Mathf.RadToDeg(angleRad);

            Velocity = nextDirection * Speed;

            if (direction > -45 && direction < 45)
                _stateMachine.TransitionTo("WalkForwardState");
            if (direction > 45 && direction <= 135)
                _stateMachine.TransitionTo("WalkRightState");
            if (direction > -135 && direction <= -45)
                _stateMachine.TransitionTo("WalkLeftState");
            if (direction > 135 || direction < -135)
                _stateMachine.TransitionTo("WalkBackwardState");
        }
        else
        {
            if (Velocity.Length() < 1)
            {
                ToIdle();
            }
            else
            {
                Velocity = Velocity.Lerp(Vector3.Zero, 5f * (float)delta);
            }
        }

        MoveAndSlide();
    }

    /// <summary>
    /// Executes the current movement behavior depending on the active NPC sub-state.
    /// </summary>
    /// <param name="delta">Frame time step.</param>
    protected void HandleCharacterMovement(double delta)
    {
        switch (_subState)
        {
            case AovDataStructures.NpcSubState.FollowSpecificPoint:
                OnFollowSpecificPoint();
                break;

            case AovDataStructures.NpcSubState.FollowMovingPoint:
                OnFollowMovingPoint(delta);
                break;
        }
    }

    /// <summary>
    /// Initializes the NPC systems including navigation, state machine, and movement parameters.
    /// Must be called before any movement logic is executed.
    /// </summary>
    /// <param name="stateMachine">
    /// State machine responsible for animation and behavior transitions.
    /// </param>
    /// <param name="navigationAgent">
    /// Navigation agent used for pathfinding on the NavMesh.
    /// </param>
    /// <param name="stopDistance">
    /// Distance threshold at which the NPC stops moving toward a target.
    /// </param>
    /// <param name="speed">
    /// Movement speed applied during navigation.
    /// </param>
    protected void Initialize(
        StateMachine stateMachine,
        NavigationAgent3D navigationAgent,
        float stopDistance,
        float speed)
    {
        StopDistance = stopDistance;
        Speed = speed;
        _navigationAgent = navigationAgent;
        _stateMachine = stateMachine;
        _originPoint = GlobalPosition;
    }

    /// <summary>
    /// Forces the NPC into an idle state and stops all movement.
    /// </summary>
    /// <param name="changeSubState">
    /// If true, also resets the internal AI sub-state to Idle.
    /// </param>
    public void ToIdle(bool changeSubState = false)
    {
        Velocity = Vector3.Zero;
        _stateMachine.TransitionTo("IdleState");

        if (changeSubState)
            _subState = AovDataStructures.NpcSubState.Idle;
    }

    /// <summary>
    /// Starts roaming behavior (not fully implemented).
    /// Intended to make the NPC wander around its origin point.
    /// </summary>
    public void ToRoaming()
    {
        _timer.Start();
        _navigationAgent.GetNextPathPosition();
        _navigationAgent.IsNavigationFinished();
    }

    /// <summary>
    /// Sets the NPC to follow a fixed target point in the world.
    /// The NPC will stop once it reaches the target position.
    /// </summary>
    /// <param name="nodeToFollow">
    /// Static target position represented by a Node3D.
    /// </param>
    public void ToFollowingSpecificPoint(Node3D nodeToFollow)
    {
        _nodeToFollow = nodeToFollow;
        _subState = AovDataStructures.NpcSubState.FollowSpecificPoint;
        _navigationAgent.TargetPosition = nodeToFollow.GlobalPosition;
    }

    /// <summary>
    /// Sets the NPC to follow a moving entity using continuous path recalculation.
    /// The target position is updated dynamically during movement.
    /// </summary>
    /// <param name="nodeToFollow">
    /// Dynamic target entity to follow.
    /// </param>
    public void ToFollowingMovingEntity(Node3D nodeToFollow)
    {
        _nodeToFollow = nodeToFollow;
        _subState = AovDataStructures.NpcSubState.FollowMovingPoint;
    }
}
