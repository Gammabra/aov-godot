using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.player.States;

public partial class IdleState : State
{
    [Export]
    private NodePath? _animatedSprite3DPath;

    private AnimatedSprite3D? _animatedSprite3D;

    public override void OnStateReady()
    {
        _animatedSprite3D = GetNode<AnimatedSprite3D>(_animatedSprite3DPath);
    }

    public override void Enter()
    {
        _animatedSprite3D?.Play("Idle");
    }

    public override void Exit()
    {
        _animatedSprite3D?.Stop();
    }
}
