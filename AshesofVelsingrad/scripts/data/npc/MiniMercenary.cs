using AshesOfVelsingrad.Interfaces;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.player;
using Godot;

namespace AshesOfVelsingrad.data.npc;

public partial class MiniMercenary : NpcSystem
{
    [Export]
    private NodePath? _navigationAgentPath;

    [Export]
    private NodePath? _playerPath;

    [Export]
    private NodePath? _stateMachinePath;

    [Export]
    private float _speed = 4.4f;

    private NavigationAgent3D _navigationAgent = null!;
    private AovPlayer? _player;
    private StateMachine _stateMachine = null!;

    public override void _Ready()
    {
        NavigationAgent3D navigationAgent = GetNode<NavigationAgent3D>(_navigationAgentPath);
        _player = GetNode<AovPlayer>(_playerPath);
        StateMachine stateMachine = GetNode<StateMachine>(_stateMachinePath);
        Initialize(stateMachine, navigationAgent, _speed);
        StopDistance = 1.5f;
        ToFollowingMovingEntity(_player);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null)
        {
            return;
        }

        HandleCharacterMovement(delta);
    }
}
