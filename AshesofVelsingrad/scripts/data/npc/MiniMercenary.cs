using AshesOfVelsingrad.Interfaces;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.player;
using Godot;

namespace AshesOfVelsingrad.data.npc;

public partial class MiniMercenary : Npc
{
    [Export]
    private NodePath? _navigationAgentPath;

    [Export]
    private NodePath? _playerPath;

    [Export]
    private NodePath? _stateMachinePath;

    [Export]
    private float _speed = 4;

    // null! — populated in _Ready via the [Export] NodePath. Godot guarantees _Ready
    // runs before any other engine callback touches these, so the suppression is safe.
    private NavigationAgent3D _navigationAgent = null!;
    private AovPlayer? _player;
    private float _stopDistance = 1.5f;
    private StateMachine _stateMachine = null!;

    public override void _Ready()
    {
        NavigationAgent3D navigationAgent = GetNode<NavigationAgent3D>(_navigationAgentPath);
        _player = GetNode<AovPlayer>(_playerPath);
        StateMachine stateMachine = GetNode<StateMachine>(_stateMachinePath);
        Initialize(stateMachine, navigationAgent, _stopDistance, _speed);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null)
        {
            return;
        }

        ToFollowingMovingEntity(_player, delta);
    }
}
