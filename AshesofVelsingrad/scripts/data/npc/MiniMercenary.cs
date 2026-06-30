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

    [Export]
    private NodePath _noticedPath = null!;

    private NavigationAgent3D _navigationAgent = null!;
    private AovPlayer? _player;
    private StateMachine _stateMachine = null!;
    private Sprite3D _noticed = null!;

    public override void _Ready()
    {
        NavigationAgent3D navigationAgent = GetNode<NavigationAgent3D>(_navigationAgentPath);
        StateMachine stateMachine = GetNode<StateMachine>(_stateMachinePath);
        _player = GetNode<AovPlayer>(_playerPath);
        _noticed = GetNode<Sprite3D>(_noticedPath);
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

    public void ToggleNoticed()
    {
        if (_noticed.Visible)
        {
            _noticed.Visible = false;
            return;
        }
        _noticed.Visible = true;
    }
}
