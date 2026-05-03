using Godot;
using AshesOfVelsingrad.Managers;

namespace AshesOfVelsingrad.player.States;

public partial class WalkForwardState : State
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
        _animatedSprite3D?.Play("WalkForward");
    }

    public override void Exit()
    {
        _animatedSprite3D?.Stop();
    }
}
