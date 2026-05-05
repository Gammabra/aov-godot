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
    private float _speed = 4;

    private NavigationAgent3D _navigationAgent;
    private AovPlayer? _player;
    private float _stopDistance = 1.5f;

    public override void _Ready()
    {
        _navigationAgent = GetNode<NavigationAgent3D>(_navigationAgentPath);
        _player = GetNode<AovPlayer>(_playerPath);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null)
        {
            GD.Print("Player is null");
            return;
        }

        float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

        if (distance > _stopDistance)
        {
            _navigationAgent.TargetPosition = _player.GlobalPosition;

            Vector3 nextPosition = _navigationAgent.GetNextPathPosition();
            Vector3 nextDirection = (nextPosition - GlobalPosition).Normalized();

            Velocity = nextDirection * _speed;
        }
        else
        {
            Velocity = Vector3.Zero;
        }

        MoveAndSlide();
    }
}
