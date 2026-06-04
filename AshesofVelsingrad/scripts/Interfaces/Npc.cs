using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.Interfaces;

/// <summary>
/// Base abstract class for all NPCs in the game.
/// Provides common movement, navigation, and state-transition behaviors
/// such as idling, roaming, and following a target entity.
/// </summary>
public abstract partial class Npc : CharacterBody3D
{
    private StateMachine _stateMachine = null!;
    private NavigationAgent3D _navigationAgent = null!;
    private Timer _timer = null!;
    private Vector3 _originPoint;
    private float _roamingRadius = 30;

    /// <summary>
    /// Minimum distance at which the NPC stops approaching its target.
    /// </summary>
    protected float StopDistance;

    /// <summary>
    /// Movement speed of the NPC.
    /// </summary>
    protected float Speed;

    /// <summary>
    /// Initializes the NPC with its navigation and movement settings.
    /// </summary>
    /// <param name="stateMachine">
    /// State machine responsible for handling NPC state transitions.
    /// </param>
    /// <param name="navigationAgent">
    /// Navigation agent used for pathfinding and movement.
    /// </param>
    /// <param name="stopDistance">
    /// Distance from the target at which the NPC stops moving.
    /// </param>
    /// <param name="speed">
    /// Movement speed of the NPC.
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
    /// Transitions the NPC to its idle state.
    /// </summary>
    public void ToIdle()
    {
        _stateMachine.TransitionTo("IdleState");
    }

    /// <summary>
    /// Starts the roaming behavior by activating the roaming timer
    /// and refreshing the navigation path.
    /// </summary>
    public void ToRoaming()
    {
        _timer.Start();
        _navigationAgent.GetNextPathPosition();
        _navigationAgent.IsNavigationFinished();
    }

    /// <summary>
    /// Makes the NPC follow a target entity using navigation pathfinding.
    /// The NPC moves toward the target until it reaches the configured
    /// stopping distance, while updating its movement state according
    /// to the current movement direction.
    /// </summary>
    /// <param name="nodeToFollow">
    /// The target entity that the NPC should follow.
    /// </param>
    /// <param name="delta">
    /// Frame delta time used for velocity interpolation.
    /// </param>
    public void ToFollowingMovingEntity(Node3D nodeToFollow, double delta)
    {
        float distance = GlobalPosition.DistanceTo(nodeToFollow.GlobalPosition);

        if (distance > StopDistance)
        {
            _navigationAgent.TargetPosition = nodeToFollow.GlobalPosition;

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
                Velocity = Vector3.Zero;
                ToIdle();
            }
            else
            {
                Velocity = Velocity.Lerp(Vector3.Zero, 5f * (float)delta);
            }
        }

        MoveAndSlide();
    }
}
