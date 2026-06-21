using AshesOfVelsingrad.player;
using AshesOfVelsingrad.ui;
using Godot;
using System.Threading.Tasks;

namespace AshesOfVelsingrad.Managers;

public enum TutorialStep
{
    Start,
    IntroSequence,
    IntroDialog,
    GuardDialog,
}

public partial class TutorialManager : Node
{
    private TutorialStep _step = TutorialStep.Start;

    [Export]
    private NodePath _introDialogPath = null!;

    [Export]
    private NodePath _guardDialogPath = null!;

    [Export]
    private NodePath _playerPath = null!;

    [Export]
    private NodePath _introSequencePath = null!;

    private Node _introDialog = null!;
    private Node _guardDialog = null!;
    private AovPlayer _player = null!;
    private TextSequence _introSequence = null!;
    // TODO: Fill the tuple to have the complete intro sequence
    private readonly (string, int, float)[] _sequences = [
        ("Prologue", 50, 3)
    ];

    public bool CanMove { get; private set; }

    private void DoIntroDialogStep()
    {
        _introDialog.Call("talk");
    }

    private async Task DoGuardDialog()
    {
        await ToSignal(GetTree().CreateTimer(1f),
            SceneTreeTimer.SignalName.Timeout);
        _guardDialog.Call("talk");
    }

    private void DoIntroSequences()
    {
        _ = _introSequence.PlaySequence(_sequences);
    }

    public override async void _Ready()
    {
        _player = GetNode<AovPlayer>(_playerPath);
        _introDialog = GetNode<Node>(_introDialogPath);
        _introDialog.Connect("dialog_ended", Callable.From(GoToNextStep));
        _guardDialog = GetNode<Node>(_guardDialogPath);
        _guardDialog.Connect("dialog_ended", Callable.From(() => CanMove = true));
        _introSequence = GetNode<TextSequence>(_introSequencePath);
        _introSequence.OnSequenceEnded += GoToNextStep;

        await ToSignal(_introSequence, Node.SignalName.Ready);
        GoToNextStep();
    }

    public void GoToNextStep()
    {
        _step++;

        switch (_step)
        {
            case TutorialStep.IntroSequence:
                DoIntroSequences();
                break;
            case TutorialStep.IntroDialog:
                DoIntroDialogStep();
                break;
            case TutorialStep.GuardDialog:
                _ = DoGuardDialog();
                break;
        }
    }
}
