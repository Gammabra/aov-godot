using Godot;
using AshesOfVelsingrad.Managers;

namespace AshesOfVelsingrad.UI.Menus;

public partial class PauseMenu : Control
{
    // Emitted so MainManager (or whoever) can react to resume/exit
    [Signal] public delegate void ResumeRequestedEventHandler();
    [Signal] public delegate void ExitToMainMenuRequestedEventHandler();

    [Export] private Button ?_continueButton;
    [Export] private Button ?_saveButton;
    [Export] private Button ?_loadButton;
    [Export] private Button ?_settingsButton;
    [Export] private Button ?_exitButton;

    public override void _Ready()
    {
        MenuManager.Instance?.RegisterMenu(MenuManager.PAUSE_MENU, this);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Allow Escape to toggle pause even when visible
        if (@event.IsActionPressed("pause") && MenuManager.Instance?.IsMenuActive(MenuManager.PAUSE_MENU) == true)
        {
            EmitSignal(SignalName.ResumeRequested);
            GetViewport().SetInputAsHandled();
        }
    }

    // ── Button handlers ────────────────────────────────────────────

    private void OnContinuePressed() => EmitSignal(SignalName.ResumeRequested);

    private void OnSavePressed()
    {
        // TODO: wire to your save system
        GD.Print("[PauseMenu] Save — not yet implemented.");
    }

    private void OnLoadPressed()
    {
        // TODO: wire to your load system
        GD.Print("[PauseMenu] Load — not yet implemented.");
    }

    private void OnSettingsPressed()
    {
        MenuManager.Instance?.ShowMenu(MenuManager.OPTIONS_MENU);
    }

    private void OnExitPressed() => EmitSignal(SignalName.ExitToMainMenuRequested);
}