using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.UI.Menus;

/// <summary>
/// Options menu that allows players to configure game settings.
/// Follows the Component-Based Architecture and event-driven communication.
/// Implements the Manager Pattern for centralized settings management.
/// </summary>
public partial class OptionsMenu : Control
{
    [Export] private PackedScene? _inputButtonScene;
    [Export] private PackedScene? _notifUnboundScene;
    [Export] private Slider? _dialogueSizeSlider;
    [Export] private Label? _dialogueSizeLabel;
    [Export] private Button? _resetButton;
    [Export] private Button? _backButton;
    [Export] private Control? _previewDialogue;
    [Export] private Label? _previewText;
    [Export] private VBoxContainer? _actionList;

    [Signal]
    public delegate void BackRequestedEventHandler();

    private bool _isRemapping = false;
    private string? _actionToRemap;
    private Button? _remappingButton;
    private List<CanvasLayer> _notifUnboundList = new List<CanvasLayer>();

    // Dictionary mapping internal action names to display names
    private Dictionary<string, string> _inputActions = new Dictionary<string, string>
    {
        { "attack", "Attack" },
        { "move_up", "Move Up" },
        { "move_left", "Move Left" },
        { "move_down", "Move Down" },
        { "move_right", "Move Right" },
    };

    /// <summary>
    /// Called when the node is ready. Initializes the options menu by calling deferred setup.
    /// </summary>
    /// <remarks>
    /// This method is called when both the node and its children have entered the scene tree.
    /// It uses CallDeferred to ensure all UI elements are properly initialized.
    /// </remarks>
    public override void _Ready()
    {
        CallDeferred(MethodName.DeferredReady);
    }

    /// <summary>
    /// Deferred initialization to ensure all UI elements are ready.
    /// </summary>
    /// <remarks>
    /// This method connects signals and initializes UI elements.
    /// It's called after _Ready() to ensure proper initialization order.
    /// </remarks>
    private void DeferredReady()
    {
        ConnectSignals();
        InitializeUI();
        // Wait one more frame to ensure SettingsManager has applied saved bindings
        CallDeferred(MethodName.CreateActionList);
    }

    /// <summary>
    /// Gets a user-friendly name for the given input event.
    /// </summary>
    /// <param name="inputEvent">The input event to get the name for.</param>
    /// <returns>A string representing the input event.</returns>
    private static string GetButtonName(InputEvent inputEvent)
    {
        if (inputEvent is InputEventJoypadMotion joypadMotion)
            return joypadMotion.Axis switch
            {
                JoyAxis.TriggerLeft => "LT",
                JoyAxis.TriggerRight => "RT",
                _ => $"Axis {joypadMotion.Axis}",
            };

        if (inputEvent is not InputEventJoypadButton joypadButton)
            return inputEvent.AsText().TrimSuffix(" (Physical)");

        return (int)joypadButton.ButtonIndex switch
        {
            0 => "A (Controller)",
            1 => "B (Controller)",
            2 => "X (Controller)",
            3 => "Y (Controller)",
            7 => "Left Stick",
            8 => "Right Stick",
            9 => "LB",
            10 => "RB",
            11 => "D-Pad Up",
            12 => "D-Pad Down",
            13 => "D-Pad Left",
            14 => "D-Pad Right",
            _ => $"Button {(int)joypadButton.ButtonIndex}",
        };
    }

    /// <summary>
    /// Creates the list of input action buttons dynamically.
    /// </summary>
    private void CreateActionList()
    {
        if (_actionList == null || _inputButtonScene == null)
            return;

        foreach (var children in _actionList.GetChildren())
            children.QueueFree();

        var actions = InputMap.GetActions().OrderBy(a => a);
        foreach (var action in _inputActions)
        {
            if (!action.ToString().StartsWith("ui_"))
            {
                var button = _inputButtonScene.Instantiate<Button>();
                if (button == null) continue;

                _actionList.AddChild(button);

                var marginContainer = button.GetNode<MarginContainer>("MarginContainer");
                if (marginContainer == null) continue;

                var hbox = marginContainer.GetNode<HBoxContainer>("HBoxContainer");
                if (hbox == null) continue;

                var actionLabel = hbox.GetNode<Label>("LabelAction");
                var inputLabel = hbox.GetNode<Label>("LabelInput");
                if (actionLabel == null || inputLabel == null) continue;

                actionLabel.Text = action.Value;
                var events = InputMap.ActionGetEvents(action.Key);
                if (events.Count > 0)
                {
                    inputLabel.Text = GetButtonName(events[0]);
                }
                else
                {
                    inputLabel.Text = "Unbound";
                    GD.Print($"Action '{action.Key}' is unbound");
                }

                _actionList.AddChild(button);
                button.Pressed += () => OnInputButtonPressed(button, action.Key);
            }
        }
    }

    /// <summary>
    /// Handles input button press to start remapping.
    /// </summary>
    /// <param name="button">The button that was pressed.</param>
    /// <param name="action">The action associated with the button.</param>
    private void OnInputButtonPressed(Button button, string action)
    {
        if (button == null) return;

        _isRemapping = true;
        _actionToRemap = action;
        var marginContainer = button.GetNodeOrNull<MarginContainer>("MarginContainer");
        if (marginContainer == null) return;

        var hbox = marginContainer.GetNodeOrNull<HBoxContainer>("HBoxContainer");
        if (hbox == null) return;

        var inputLabel = hbox.GetNodeOrNull<Label>("LabelInput");
        if (inputLabel != null)
        {
            inputLabel.Text = "Press any key...";
        }
    }
    /// <summary>
    /// Captures input for remapping actions.
    /// </summary>
    /// <param name="inputEvent">The input event to process.</param>
    /// <remarks>
    /// This method listens for input events when remapping is active.
    /// It updates the binding in SettingsManager and refreshes the button display.
    /// It ignores certain inputs like Escape or specific joypad buttons to prevent unwanted bindings.
    /// </remarks>
    public override void _Input(InputEvent inputEvent)
    {
        if (_isRemapping && _actionToRemap != null)
        {
            if (InputMap.ActionGetEvents(_actionToRemap).Any(evt => evt.IsMatch(inputEvent)))
            {
                UpdateActionList(_remappingButton, inputEvent);
                _isRemapping = false;
                _actionToRemap = null;
                _remappingButton = null;
                AcceptEvent();
                return;
            }

            if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed ||
                inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed ||
                inputEvent is InputEventJoypadButton joypadButton && joypadButton.Pressed ||
                inputEvent is InputEventJoypadMotion && ((InputEventJoypadMotion)inputEvent).AxisValue > 0.5f)
            {
                if ((inputEvent is InputEventJoypadMotion joypadMotion &&
                    joypadMotion.Axis != JoyAxis.TriggerLeft &&
                    joypadMotion.Axis != JoyAxis.TriggerRight) ||
                    (inputEvent is InputEventJoypadButton joypadBtn &&
                    (joypadBtn.ButtonIndex == JoyButton.Back ||
                     joypadBtn.ButtonIndex == JoyButton.Guide ||
                     joypadBtn.ButtonIndex == JoyButton.Start)) ||
                     (inputEvent is InputEventKey keyEvt && keyEvt.Keycode == Key.Escape))
                {
                    return;
                }

                // Check if this input is already bound to another action
                foreach (string action in InputMap.GetActions())
                {
                    var events = InputMap.ActionGetEvents(action);
                    if (events.Any(evt => evt.IsMatch(inputEvent)))
                    {
                        // Unbind the input from its existing action
                        SettingsManager.Instance?.SetInputBinding(action, null);
                        _ = CreateNotifUnbound(action);
                        break;
                    }
                }

                // Prevent double-click flag from being saved
                if (inputEvent is InputEventMouseButton innerMouseButton && innerMouseButton.DoubleClick)
                    innerMouseButton.DoubleClick = false;

                // Save the new binding through SettingsManager
                SettingsManager.Instance?.SetInputBinding(_actionToRemap, inputEvent);

                // Update the button display
                UpdateActionList(_remappingButton, inputEvent);

                _isRemapping = false;
                _actionToRemap = null;
                _remappingButton = null;

                AcceptEvent();
            }
        }
    }

    /// <summary>
    /// Creates a notification for unbound actions.
    /// </summary>
    /// <param name="action">The action that was unbound.</param>
    /// <remarks>
    /// This method instantiates a notification UI element to inform the user
    /// that an action has been unbound. The notification is displayed for 5 seconds
    /// before being removed. Multiple notifications are stacked vertically.
    /// </remarks>
    private async Task CreateNotifUnbound(string action)
    {
        var notifInstance = _notifUnboundScene?.Instantiate() as CanvasLayer;
        if (notifInstance == null)
            return;

        GetTree().Root.AddChild(notifInstance);
        notifInstance.GetNode<Label>("Panel/Label").Text = $"The action '{action}' is now unbound.";
        _notifUnboundList.Add(notifInstance);

        PlaceNotifUnbound();
        await ToSignal(GetTree().CreateTimer(5), "timeout");

        _notifUnboundList.Remove(notifInstance);
        notifInstance.QueueFree();
        PlaceNotifUnbound();
    }

    /// <summary>
    /// Places unbound notifications on the screen.
    /// </summary>
    private void PlaceNotifUnbound()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        float offset = 0;

        foreach (var notif in _notifUnboundList)
        {
            var panel = notif.GetNode<Panel>("Panel");
            var notifSize = panel.Size;

            panel.Position = new Vector2(
                (viewportSize.X - notifSize.X) / 2,
                viewportSize.Y - notifSize.Y - 20 - offset
            );

            offset += notifSize.Y + 10;
        }
    }

    /// <summary>
    /// Updates the input label for a specific button.
    /// </summary>
    /// <param name="button">The button to update.</param>
    /// <param name="inputEvent">The new input event to display.</param>
    private void UpdateActionList(Button? button, InputEvent inputEvent)
    {
        if (button == null)
            return;

        button.GetNode<Label>("MarginContainer/HBoxContainer/LabelInput").Text =
            GetButtonName(inputEvent);
    }

    /// <summary>
    /// Connects signals from UI elements and SettingsManager.
    /// </summary>
    /// <remarks>
    /// This method sets up event handlers for UI interactions and settings changes.
    /// It ensures that changes in the UI are reflected in the SettingsManager and vice versa.
    /// </remarks>
    private void ConnectSignals()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.DialogueSizeChanged += OnDialogueSizeSettingChanged;
            SettingsManager.Instance.InputBindingChanged += OnInputBindingChanged;
        }
    }

    /// <summary>
    /// Handles input binding changes from SettingsManager.
    /// </summary>
    /// <param name="action">The action whose binding has changed.</param>
    /// <remarks>
    /// This method refreshes the action list to reflect updated bindings.
    /// It ensures that the UI stays in sync with the underlying settings.
    /// </remarks>
    private void OnInputBindingChanged(string action)
    {
        // Refresh the action list to show updated bindings
        CreateActionList();
    }

    /// <summary>
    /// Initializes the UI elements and sets up initial values from settings.
    /// </summary>
    /// <remarks>
    /// This method sets up the dialogue size slider with proper min/max values and step.
    /// It also updates the preview text and dialogue size label to reflect current settings.
    /// If SettingsManager is not available, it logs an error and returns early.
    /// </remarks>
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

    /// <summary>
    /// Handles the dialogue size slider value change event.
    /// </summary>
    /// <param name="value">The new value of the dialogue size slider (0.5 to 2.0).</param>
    /// <remarks>
    /// This method updates the dialogue size in the SettingsManager and refreshes the UI preview.
    /// The value is cast to float for compatibility with the SettingsManager API.
    /// Updates both the label display and the preview text scaling.
    /// </remarks>
    private void OnDialogueSizeChanged(double value)
    {
        var size = (float)value;
        SettingsManager.Instance?.SetDialogueSize(size);
        UpdateDialogueSizeLabel(size);
        UpdatePreview();
    }

    /// <summary>
    /// Handles external dialogue size setting changes from the SettingsManager.
    /// </summary>
    /// <param name="newSize">The new dialogue size value from the settings manager.</param>
    /// <remarks>
    /// This method updates the UI when the setting changes from an external source.
    /// It prevents infinite loops by checking if the slider value differs significantly from the new size.
    /// Updates both the slider position and the preview elements.
    /// </remarks>
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

    /// <summary>
    /// Updates the dialogue size label with the current size as a percentage.
    /// </summary>
    /// <param name="size">The current dialogue size (0.5 to 2.0).</param>
    /// <remarks>
    /// The size is converted to a percentage (50% to 200%) and displayed in the label.
    /// The value is rounded to the nearest integer for clean display.
    /// </remarks>
    private void UpdateDialogueSizeLabel(float size)
    {
        if (_dialogueSizeLabel != null)
        {
            var percentage = Mathf.RoundToInt(size * 100);
            _dialogueSizeLabel.Text = $"Dialog sizes: {percentage}%";
        }
    }

    /// <summary>
    /// Updates the preview text and dialogue size based on the current settings.
    /// </summary>
    /// <remarks>
    /// This method adjusts the font size of the preview text and scales the preview dialogue control.
    /// It uses the current dialogue size from the SettingsManager to provide real-time preview.
    /// The font size is calculated as 18 pixels multiplied by the current scale factor.
    /// Both text font size and control scaling are updated to show the visual impact.
    /// </remarks>
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

    /// <summary>
    /// Handles the reset button press event by showing a confirmation dialog.
    /// </summary>
    /// <remarks>
    /// This method shows a confirmation dialog before resetting settings to prevent accidental resets.
    /// The actual reset operation is performed only after user confirmation.
    /// </remarks>
    private void OnResetPressed()
    {
        ShowResetConfirmation();
    }

    /// <summary>
    /// Shows a confirmation dialog for resetting all settings to default values.
    /// </summary>
    /// <remarks>
    /// This method creates an AcceptDialog with custom buttons for confirmation.
    /// If the user confirms, it calls SettingsManager.ResetToDefaults().
    /// The dialog is properly cleaned up after use to prevent memory leaks.
    /// Provides both "Reset" and "Cancel" options for user choice.
    /// </remarks>
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

        confirmDialog.Confirmed += () =>
        {
            SettingsManager.Instance?.ResetToDefaults();
            confirmDialog.QueueFree();
        };

        confirmDialog.CustomAction += (action) =>
        {
            if ((string)action == "cancel")
            {
                confirmDialog.QueueFree();
            }
        };
    }

    /// <summary>
    /// Handles the back button press event by emitting a navigation signal.
    /// </summary>
    /// <remarks>
    /// This method emits the BackRequested signal to notify the MenuManager
    /// or other listening components that the user wants to return to the previous menu.
    /// Follows the event-driven architecture pattern for menu navigation.
    /// </remarks>
    private void OnBackPressed()
    {
        EmitSignal(SignalName.BackRequested);
    }

    /// <summary>
    /// Shows the options menu with a fade-in animation effect.
    /// </summary>
    /// <remarks>
    /// This method makes the menu visible and animates it from transparent to fully opaque.
    /// Uses a Godot Tween for smooth visual transition over 0.3 seconds.
    /// The menu starts completely transparent and fades to full visibility.
    /// </remarks>
    public void ShowMenu()
    {
        Show();
        Modulate = new Color(1, 1, 1, 0);

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", Colors.White, 0.3f);
    }

    /// <summary>
    /// Hides the options menu with a fade-out animation effect.
    /// </summary>
    /// <remarks>
    /// This method animates the menu from fully visible to transparent, then hides it.
    /// Uses a Godot Tween for smooth visual transition over 0.3 seconds.
    /// The menu is actually hidden after the fade-out animation completes.
    /// Provides a polished user experience with smooth transitions.
    /// </remarks>
    public void HideMenu()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.3f);
        tween.TweenCallback(Callable.From(() => Hide()));
    }

    /// <summary>
    /// Cleanup when the node exits the tree.
    /// </summary>
    public override void _ExitTree()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.DialogueSizeChanged -= OnDialogueSizeSettingChanged;
            SettingsManager.Instance.InputBindingChanged -= OnInputBindingChanged;
        }
    }
}
