using AshesOfVelsingrad.player;
using Godot;

namespace AshesOfVelsingrad.Managers;

public enum TutorialStep
{
    Start,
    IntroDialog,
}

public partial class TutorialManager : Node
{
    private TutorialStep _step = TutorialStep.Start;

    [Export]
    private NodePath _introDialogPath = null!;

    [Export]
    private NodePath _playerPath = null!;

    private Node _introDialog = null!;
    private AovPlayer _player = null!;

    public bool CanMove { get; private set; }

    public override void _Ready()
    {
        _player = GetNode<AovPlayer>(_playerPath);
        _introDialog = GetNode<Node>(_introDialogPath);
        _introDialog.Connect("dialog_ended", Callable.From(() => CanMove = true));
        GoToNextStep();
    }

    public void GoToNextStep()
    {
        _step++;

        switch (_step)
        {
            case TutorialStep.IntroDialog:
                DoIntroDialogStep();
                break;
        }
    }

    private void DoIntroDialogStep()
    {
        _introDialog.Call("talk");
    }
}
