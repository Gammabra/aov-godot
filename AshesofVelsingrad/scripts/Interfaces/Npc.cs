using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.Interfaces;

public abstract partial class Npc : CharacterBody3D
{
    private StateMachine _stateMachine = null!;
    private NavigationAgent3D _navigationAgent = null!;
    private Timer _timer = null!;
    private Vector3 _originPoint;
    private float _roamingRadius = 30;

    protected float StopDistance;
    protected float Speed;

    protected void Initialize(StateMachine stateMachine, NavigationAgent3D navigationAgent, float stopDistance, float speed)
    {
        StopDistance = stopDistance;
        Speed = speed;
        _navigationAgent = navigationAgent;
        _stateMachine = stateMachine;
        _originPoint = GlobalPosition;
    }

    public void ToIdle()
    {
        _stateMachine.TransitionTo("IdleState");
    }

    public void ToRoaming()
    {
        _timer.Start();
        _navigationAgent.GetNextPathPosition();
        _navigationAgent.IsNavigationFinished();
    }

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
                Velocity = Velocity.Lerp(Vector3.Zero, 5f * (float)delta);
        }
        MoveAndSlide();
    }
}
