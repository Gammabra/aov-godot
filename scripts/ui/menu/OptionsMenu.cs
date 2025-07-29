using Godot;
using AshesOfVelsingrad.Managers;

namespace AshesOfVelsingrad.UI.Menus;

/// <summary>
/// Options menu that allows players to configure game settings.
/// Follows the Component-Based Architecture and event-driven communication.
/// </summary>
public partial class OptionsMenu : Control
{
    // UI References - to be connected in the Godot editor
    [Export] private Slider? _dialogueSizeSlider;
    [Export] private Label? _dialogueSizeLabel;
    [Export] private Button? _resetButton;
    [Export] private Button? _backButton;
    [Export] private Control? _previewDialogue;
    [Export] private Label? _previewText;

    // Events for menu navigation
    [Signal]
    public delegate void BackRequestedEventHandler();

    public override void _Ready()
    {
        // Attendre que les AutoLoad soient initialisés
        CallDeferred(MethodName.DeferredReady);
    }

    private void DeferredReady()
    {
        ConnectSignals();
        InitializeUI();
    }

    private void ConnectSignals()
    {
        // Connect to settings manager
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.DialogueSizeChanged += OnDialogueSizeSettingChanged;
        }
    }

    private void InitializeUI()
    {
        if (SettingsManager.Instance == null)
        {
            GD.PrintErr("SettingsManager not found!");
            return;
        }

        // Initialize dialogue size slider
        var currentSize = SettingsManager.Instance.GetDialogueSize();
        if (_dialogueSizeSlider != null)
        {
            _dialogueSizeSlider.MinValue = 0.5;
            _dialogueSizeSlider.MaxValue = 2.0;
            _dialogueSizeSlider.Step = 0.1;
            _dialogueSizeSlider.Value = currentSize;
        }

        UpdateDialogueSizeLabel(currentSize);
        UpdatePreview();
    }

    private void OnDialogueSizeChanged(double value)
    {
        var size = (float)value;
        SettingsManager.Instance?.SetDialogueSize(size);
        UpdateDialogueSizeLabel(size);
        UpdatePreview();
    }

    private void OnDialogueSizeSettingChanged(float newSize)
    {
        // Update UI when setting changes from external source
        if (_dialogueSizeSlider != null && Mathf.Abs((float)_dialogueSizeSlider.Value - newSize) > 0.01)
        {
            _dialogueSizeSlider.Value = newSize;
        }
        UpdateDialogueSizeLabel(newSize);
        UpdatePreview();
    }

    private void UpdateDialogueSizeLabel(float size)
    {
        if (_dialogueSizeLabel != null)
        {
            var percentage = Mathf.RoundToInt(size * 100);
            _dialogueSizeLabel.Text = $"Dialog sizes: {percentage}%";
        }
    }

    private void UpdatePreview()
    {
        if (_previewText != null)
        {
            var currentSize = SettingsManager.Instance?.GetDialogueSize() ?? 1.0f;

            var font = _previewText.GetThemeFont("font");
            if (font != null)
            {
                var fontSize = Mathf.RoundToInt(18 * currentSize);
                _previewText.AddThemeFontSizeOverride("font_size", fontSize);
            }
        }
        if (_previewDialogue != null)
        {
            _previewDialogue.Scale = Vector2.One * (SettingsManager.Instance?.GetDialogueSize() ?? 1.0f);
        }
    }

    private void OnResetPressed()
    {
        // Show confirmation dialog before resetting
        ShowResetConfirmation();
    }

    private void ShowResetConfirmation()
    {
        var confirmDialog = new AcceptDialog();
        confirmDialog.DialogText = "Do you really want to reset all parameters ?";
        confirmDialog.Title = "Confirm";

        // Add custom buttons
        confirmDialog.AddButton("Cancel", false, "cancel");
        confirmDialog.GetOkButton().Text = "Reset";

        GetTree().Root.AddChild(confirmDialog);
        confirmDialog.PopupCentered();

        confirmDialog.Confirmed += () => {
            SettingsManager.Instance?.ResetToDefaults();
            confirmDialog.QueueFree();
        };

        confirmDialog.CustomAction += (action) => {
            if ((string)action == "cancel")
            {
                confirmDialog.QueueFree();
            }
        };
    }

    private void OnBackPressed()
    {
        EmitSignal(SignalName.BackRequested);
    }

    // Handle input for keyboard navigation
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            OnBackPressed();
            GetViewport().SetInputAsHandled();
        }
    }

    // Animation methods for smooth transitions
    public void ShowMenu()
    {
        Show();
        Modulate = new Color(1, 1, 1, 0);

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", Colors.White, 0.3f);
    }

    public void HideMenu()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.3f);
        tween.TweenCallback(Callable.From(() => Hide()));
    }
}
