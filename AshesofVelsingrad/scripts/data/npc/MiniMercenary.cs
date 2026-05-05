using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.player;
using Godot;

namespace AshesOfVelsingrad.data.npc;

public partial class MiniMercenary : CharacterBody3D
{
    [Export]
    private NodePath? _navigationAgentPath;

    [Export]
    private NodePath? _playerPath;

    [Export]
    private NodePath? _stateMachinePath;

    [Export]
    private float _speed = 4;

    private NavigationAgent3D _navigationAgent;
    private AovPlayer? _player;
    private float _stopDistance = 1.5f;
    private StateMachine _stateMachine;

    public override void _Ready()
    {
        _navigationAgent = GetNode<NavigationAgent3D>(_navigationAgentPath);
        _player = GetNode<AovPlayer>(_playerPath);
        _stateMachine = GetNode<StateMachine>(_stateMachinePath);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null)
        {
            return;
        }

        float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

        if (distance > _stopDistance)
        {
            _navigationAgent.TargetPosition = _player.GlobalPosition;

            Vector3 nextPosition = _navigationAgent.GetNextPathPosition();
            Vector3 nextDirection = (nextPosition - GlobalPosition).Normalized();

            float angleRad = Mathf.Atan2(nextDirection.X, -nextDirection.Z);
            float direction = Mathf.RadToDeg(angleRad);

            Velocity = nextDirection * _speed;

            if (direction > -45 && direction < 45)
                _stateMachine?.TransitionTo("WalkForwardState");
            if (direction > 45 && direction <= 135)
                _stateMachine?.TransitionTo("WalkRightState");
            if (direction > -135 && direction <= -45)
                _stateMachine?.TransitionTo("WalkLeftState");
            if (direction > 135 || direction < -135)
                _stateMachine?.TransitionTo("WalkBackwardState");
        }
        else
        {
            if (Velocity.Length() < 1)
            {
                Velocity = Vector3.Zero;
                _stateMachine?.TransitionTo("IdleState");
            }
            else
                Velocity = Velocity.Lerp(Vector3.Zero, 5f * (float)delta);
        }

        MoveAndSlide();
    }
}
