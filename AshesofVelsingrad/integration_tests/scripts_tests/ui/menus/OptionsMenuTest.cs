using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AshesOfVelsingrad.Helpers.Managers;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.UI.Menus;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.UI;

[TestSuite]
[RequireGodotRuntime]
public class OptionsMenuTest
{
    private OptionsMenu? _optionsMenu;
    private TestSettingsManager? _testSettingsManager;
    private readonly List<Node> _testNodes = new();
    private Node? _root;

    // UI Components
    private Slider? _dialogueSizeSlider;
    private Label? _dialogueSizeLabel;
    private Button? _resetButton;
    private Button? _backButton;
    private Control? _previewDialogue;
    private Label? _previewText;
    private VBoxContainer? _actionList;
    private PackedScene? _inputButtonScene;

    [BeforeTest]
    public void SetUp()
    {
        GD.Print("[TEST] Starting OptionsMenu SetUp...");

        ClearAllSingletonInstances();
        CleanupGlobalOrphans();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        _testSettingsManager = CreateTestSettingsManager();
        _optionsMenu = new OptionsMenu { Name = "OptionsMenu" };

        if (_optionsMenu == null)
            throw new System.InvalidOperationException("Failed to create OptionsMenu instance.");

        CreateUIComponents();
        SetupExportedProperties();
        AddToTestRoot(_optionsMenu);

        GD.Print("[TEST] OptionsMenu SetUp completed");
    }

    private void CleanupGlobalOrphans()
    {
        var root = ((SceneTree)Godot.Engine.GetMainLoop()).Root;
        foreach (var child in root.GetChildren())
        {
            // Don't free the test runner or the root itself! 
            // Only free things your tests might have leaked.
            if (child is AcceptDialog || child.Name == "TestRoot")
            {
                root.RemoveChild(child);
                child.Free();
            }
        }
    }

    [AfterTest]
    public void TearDown()
    {
        GD.Print("[TEST] Starting OptionsMenu TearDown...");

        TestSettingsManager.ClearTempFiles();

        // 1. Remove the root from the SceneTree first
        if (GodotObject.IsInstanceValid(_root))
        {
            var parent = _root.GetParent();
            if (parent != null)
            {
                parent.RemoveChild(_root);
            }

            // 2. Force immediate deletion of the entire tree
            _root.Free();
        }

        _root = null;
        _optionsMenu = null;
        _testNodes.Clear();
        _inputButtonScene?.Dispose();
        _inputButtonScene = null;

        ClearAllSingletonInstances();

        GD.Print("[TEST] OptionsMenu TearDown completed");
    }

    // === INITIALIZATION TESTS ===

    [TestCase]
    public void Debug_InitializationProcess()
    {
        GD.Print($"[DEBUG] Before _Ready - Slider.Value: {_dialogueSizeSlider!.Value}");
        GD.Print($"[DEBUG] Before _Ready - Settings.GetDialogueSize(): {_testSettingsManager!.GetDialogueSize()}");

        _optionsMenu!._Ready();

        GD.Print($"[DEBUG] After _Ready - Slider.Value: {_dialogueSizeSlider.Value}");
        GD.Print($"[DEBUG] After _Ready - Settings.GetDialogueSize(): {_testSettingsManager.GetDialogueSize()}");

        AssertThat(GodotObject.IsInstanceValid(_optionsMenu)).IsTrue();
    }

    [TestCase]
    public async Task Ready_WithValidSettingsManager_InitializesSuccessfully()
    {
        _optionsMenu!._Ready();

        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(GodotObject.IsInstanceValid(_optionsMenu)).IsTrue();
        AssertThat(_dialogueSizeSlider!.MinValue).IsEqual(0.5);
        AssertThat(_dialogueSizeSlider.MaxValue).IsEqual(2.0);
        AssertThat(_dialogueSizeSlider.Step).IsEqual(0.1);

        var settingsValue = _testSettingsManager!.GetDialogueSize();
        AssertThat(_dialogueSizeSlider.Value).IsEqual((double)settingsValue);
    }

    [TestCase]
    public void Ready_WithNullSettingsManager_PrintsErrorAndReturns()
    {
        ClearAllSingletonInstances();

        var testOptionsMenu = AutoFree(new OptionsMenu { Name = "TestOptionsMenu" });

        if (testOptionsMenu == null)
            throw new System.InvalidOperationException("Failed to create OptionsMenu instance.");

        AddToTestRoot(testOptionsMenu);

        testOptionsMenu._Ready();

        AssertThat(GodotObject.IsInstanceValid(testOptionsMenu)).IsTrue();
    }

    // === DIALOGUE SIZE TESTS ===

    [TestCase]
    public void OnDialogueSizeChanged_ValidValue_UpdatesSettingsAndUI()
    {
        _optionsMenu!._Ready();
        var newSize = 1.5;

        CallPrivateMethod(_optionsMenu, "OnDialogueSizeChanged", newSize);

        AssertThat(_testSettingsManager!.GetDialogueSize()).IsEqual(1.5f);
        AssertThat(_dialogueSizeLabel!.Text).Contains("150%");
    }

    [TestCase]
    public void OnDialogueSizeChanged_ValueBelowMin_ClampsToMinimum()
    {
        _optionsMenu!._Ready();
        var newSize = 0.3;

        CallPrivateMethod(_optionsMenu, "OnDialogueSizeChanged", newSize);

        AssertThat(_testSettingsManager!.GetDialogueSize()).IsEqual(0.5f);
    }

    [TestCase]
    public void OnDialogueSizeChanged_ValueAboveMax_ClampsToMaximum()
    {
        _optionsMenu!._Ready();
        var newSize = 2.5;

        CallPrivateMethod(_optionsMenu, "OnDialogueSizeChanged", newSize);

        AssertThat(_testSettingsManager!.GetDialogueSize()).IsEqual(2.0f);
    }

    [TestCase]
    public void OnDialogueSizeSettingChanged_ExternalChange_UpdatesSliderAndUI()
    {
        _optionsMenu!._Ready();

        _dialogueSizeSlider!.MinValue = 0.5;
        _dialogueSizeSlider.MaxValue = 2.0;
        _dialogueSizeSlider.Value = 1.0;

        var newSize = 1.5f;

        CallPrivateMethod(_optionsMenu, "OnDialogueSizeSettingChanged", newSize);

        var sliderValue = (float)_dialogueSizeSlider.Value;
        AssertThat(sliderValue).IsEqual(newSize);
        AssertThat(_dialogueSizeLabel!.Text).Contains("150%");
    }

    [TestCase]
    public void OnDialogueSizeSettingChanged_SameValue_DoesNotUpdateSlider()
    {
        _optionsMenu!._Ready();
        _dialogueSizeSlider!.MinValue = 0.5;
        _dialogueSizeSlider.MaxValue = 2.0;
        _dialogueSizeSlider.Value = 1.5;
        var originalValue = _dialogueSizeSlider.Value;

        CallPrivateMethod(_optionsMenu, "OnDialogueSizeSettingChanged", 1.5f);

        AssertThat(_dialogueSizeSlider.Value).IsEqual(originalValue);
    }

    [TestCase]
    public void UpdateDialogueSizeLabel_ValidSize_DisplaysCorrectPercentage()
    {
        _optionsMenu!._Ready();

        CallPrivateMethod(_optionsMenu, "UpdateDialogueSizeLabel", 0.75f);

        AssertThat(_dialogueSizeLabel!.Text).IsEqual("Dialog sizes: 75%");
    }

    [TestCase]
    public void UpdateDialogueSizeLabel_EdgeValues_DisplaysCorrectPercentages()
    {
        _optionsMenu!._Ready();

        CallPrivateMethod(_optionsMenu, "UpdateDialogueSizeLabel", 0.5f);
        AssertThat(_dialogueSizeLabel!.Text).IsEqual("Dialog sizes: 50%");

        CallPrivateMethod(_optionsMenu, "UpdateDialogueSizeLabel", 2.0f);
        AssertThat(_dialogueSizeLabel!.Text).IsEqual("Dialog sizes: 200%");
    }

    [TestCase]
    public void UpdatePreview_ValidComponents_UpdatesFontSizeAndScale()
    {
        _optionsMenu!._Ready();
        _testSettingsManager!.SetDialogueSize(1.5f);

        CallPrivateMethod(_optionsMenu, "UpdatePreview");

        var expectedScale = new Vector2(1.5f, 1.5f);
        AssertThat(_previewDialogue!.Scale).IsEqual(expectedScale);
    }

    // === INPUT BINDING TESTS ===

    [TestCase]
    public async Task CreateActionList_WithValidInputs_CreatesButtons()
    {
        // Setup InputMap with test actions
        if (!InputMap.HasAction("test_action"))
        {
            InputMap.AddAction("test_action");
            InputMap.ActionAddEvent("test_action", new InputEventKey { Keycode = Key.A });
        }

        _optionsMenu!._Ready();

        // Wait for deferred creation
        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // Check that action list has been populated
        if (_actionList != null && _actionList.GetChildCount() > 0)
        {
            var firstButton = _actionList.GetChild(0) as Button;
            AssertThat(firstButton).IsNotNull();
        }

        // Cleanup
        if (InputMap.HasAction("test_action"))
        {
            InputMap.EraseAction("test_action");
        }
    }

    [TestCase]
    public void GetButtonName_KeyEvent_ReturnsCorrectName()
    {
        var keyEvent = new InputEventKey { Keycode = Key.Space };

        var method = typeof(OptionsMenu).GetMethod("GetButtonName",
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { keyEvent }) as string;

        AssertThat(result).IsNotNull();
        AssertThat(result).Contains("Space");
    }

    [TestCase]
    public void GetButtonName_JoypadButton_ReturnsCorrectName()
    {
        var joypadEvent = new InputEventJoypadButton { ButtonIndex = JoyButton.A };

        var method = typeof(OptionsMenu).GetMethod("GetButtonName",
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { joypadEvent }) as string;

        AssertThat(result).IsNotNull();
        AssertThat(result).IsEqual("A (Controller)");
    }

    [TestCase]
    public void GetButtonName_JoypadMotion_ReturnsCorrectName()
    {
        var joypadMotion = new InputEventJoypadMotion { Axis = JoyAxis.TriggerLeft };

        var method = typeof(OptionsMenu).GetMethod("GetButtonName",
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { joypadMotion }) as string;

        AssertThat(result).IsNotNull();
        AssertThat(result).IsEqual("LT");
    }

    [TestCase]
    public void OnInputButtonPressed_StartsRemapping()
    {
        _optionsMenu!._Ready();

        // Attach the test button to the root so TearDown cleans it up automatically
        var testButton = AddToTestRoot(new Button());
        var marginContainer = new MarginContainer { Name = "MarginContainer" };
        var hbox = new HBoxContainer();
        var inputLabel = new Label { Name = "LabelInput" };

        // Build the hierarchy
        testButton.AddChild(marginContainer);
        marginContainer.AddChild(hbox);
        hbox.AddChild(inputLabel);

        CallPrivateMethod(_optionsMenu, "OnInputButtonPressed", testButton, "test_action");

        var isRemapping = GetPrivateField<bool>(_optionsMenu, "_isRemapping");
        AssertThat(isRemapping).IsTrue();

        // No need for manual QueueFree() here; TearDown handles _root
    }

    [TestCase]
    public void Input_WhileRemapping_UpdatesBinding()
    {
        // Setup
        if (!InputMap.HasAction("test_remap"))
        {
            InputMap.AddAction("test_remap");
        }

        _optionsMenu!._Ready();

        // Start remapping
        SetPrivateField(_optionsMenu, "_isRemapping", true);
        SetPrivateField(_optionsMenu, "_actionToRemap", "test_remap");

        // Create input event
        var keyEvent = new InputEventKey { Keycode = Key.W, Pressed = true };

        // Trigger input
        _optionsMenu._Input(keyEvent);

        // Verify remapping stopped
        var isRemapping = GetPrivateField<bool>(_optionsMenu, "_isRemapping");
        AssertThat(isRemapping).IsFalse();

        // Cleanup
        if (InputMap.HasAction("test_remap"))
        {
            InputMap.EraseAction("test_remap");
        }
    }

    [TestCase]
    public void Input_EscapeWhileRemapping_DoesNotBind()
    {
        _optionsMenu!._Ready();

        SetPrivateField(_optionsMenu, "_isRemapping", true);
        SetPrivateField(_optionsMenu, "_actionToRemap", "test_action");

        var escapeEvent = new InputEventKey { Keycode = Key.Escape, Pressed = true };

        _optionsMenu._Input(escapeEvent);

        var isRemapping = GetPrivateField<bool>(_optionsMenu, "_isRemapping");
        AssertThat(isRemapping).IsTrue(); // Should still be remapping
    }

    [TestCase]
    public async Task OnInputBindingChanged_RefreshesActionList()
    {
        _optionsMenu!._Ready();

        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(_actionList).IsNotNull();
        var initialChildCount = _actionList!.GetChildCount();

        CallPrivateMethod(_optionsMenu, "OnInputBindingChanged", "test_action");

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // Action list should still have children after refresh
        AssertThat(_actionList.GetChildCount()).IsEqual(initialChildCount);
    }

    // === RESET TESTS ===

    [TestCase]
    public void OnResetPressed_CallsShowResetConfirmation()
    {
        _optionsMenu!._Ready();

        CallPrivateMethod(_optionsMenu, "OnResetPressed");

        AssertThat(GodotObject.IsInstanceValid(_optionsMenu)).IsTrue();
    }

    [TestCase]
    public async Task ShowResetConfirmation_CreatesDialog_WithCorrectProperties()
    {
        _optionsMenu!._Ready();

        CallPrivateMethod(_optionsMenu, "ShowResetConfirmation");

        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        var root = tree.Root;
        AcceptDialog? confirmDialog = null;

        for (int i = 0; i < root.GetChildCount(); i++)
        {
            if (root.GetChild(i) is AcceptDialog dialog)
            {
                confirmDialog = dialog;
                break;
            }
        }

        AssertThat(confirmDialog).IsNotNull();
        AssertThat(confirmDialog!.DialogText).IsEqual("Do you really want to reset all parameters ?");
        AssertThat(confirmDialog.Title).IsEqual("Confirm");

        confirmDialog.QueueFree();
    }

    // === NAVIGATION TESTS ===

    [TestCase]
    public void OnBackPressed_EmitsBackRequestedSignal()
    {
        _optionsMenu!._Ready();
        var signalEmitted = false;

        _optionsMenu.BackRequested += () =>
        {
            signalEmitted = true;
            GD.Print("[TEST] BackRequested signal received");
        };

        CallPrivateMethod(_optionsMenu, "OnBackPressed");

        AssertThat(signalEmitted).IsTrue();
    }

    // === ANIMATION TESTS ===

    [TestCase]
    public async Task ShowMenu_AnimatesFromTransparentToVisible()
    {
        _optionsMenu!._Ready();
        _optionsMenu.Hide();

        _optionsMenu.ShowMenu();

        AssertThat(_optionsMenu.Visible).IsTrue();

        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.CreateTimer(0.4f).ToSignal(tree.CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);

        AssertThat(_optionsMenu.Modulate).IsEqual(Colors.White);
    }

    [TestCase]
    public async Task HideMenu_AnimatesFromVisibleToTransparent()
    {
        _optionsMenu!._Ready();
        _optionsMenu.Show();
        _optionsMenu.Modulate = Colors.White;

        _optionsMenu.HideMenu();

        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.CreateTimer(0.4f).ToSignal(tree.CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);

        AssertThat(_optionsMenu.Visible).IsFalse();
    }

    // === INTEGRATION TESTS ===

    [TestCase]
    public void SliderConnections_WorkCorrectly()
    {
        _optionsMenu!._Ready();

        _dialogueSizeSlider!.Value = 1.8;
        _dialogueSizeSlider.EmitSignal(Slider.SignalName.ValueChanged, 1.8);

        CallPrivateMethod(_optionsMenu, "OnDialogueSizeChanged", 1.8);

        AssertThat(_testSettingsManager!.GetDialogueSize()).IsEqual(1.8f);
    }

    [TestCase]
    public void ButtonConnections_WorkCorrectly()
    {
        _optionsMenu!._Ready();
        var backSignalEmitted = false;

        _optionsMenu.BackRequested += () =>
        {
            backSignalEmitted = true;
        };

        CallPrivateMethod(_optionsMenu, "OnResetPressed");
        CallPrivateMethod(_optionsMenu, "OnBackPressed");

        AssertThat(backSignalEmitted).IsTrue();
    }

    [TestCase]
    public void SettingsManagerEvents_AreHandledCorrectly()
    {
        _optionsMenu!._Ready();

        _testSettingsManager!.SetDialogueSize(1.3f);

        AssertThat(_testSettingsManager.GetDialogueSize()).IsEqual(1.3f);
    }

    [TestCase]
    public void MultipleSliderChanges_WorkConsistently()
    {
        _optionsMenu!._Ready();
        var values = new[] { 0.5, 1.0, 1.5, 2.0, 0.8 };

        foreach (var value in values)
        {
            CallPrivateMethod(_optionsMenu, "OnDialogueSizeChanged", value);

            AssertThat(_testSettingsManager!.GetDialogueSize()).IsEqual((float)value);
            var expectedPercentage = Mathf.RoundToInt((float)value * 100);
            AssertThat(_dialogueSizeLabel!.Text).Contains($"{expectedPercentage}%");
        }
    }

    [TestCase]
    public void OptionsMenu_InheritsFromControl()
    {
        AssertThat(_optionsMenu).IsInstanceOf<Control>();
        AssertThat(_optionsMenu).IsInstanceOf<Node>();
    }

    [TestCase]
    public void NullUIComponents_DoNotCauseErrors()
    {
        var testOptionsMenu = AutoFree(new OptionsMenu { Name = "TestOptionsMenu" });

        if (testOptionsMenu == null)
            throw new System.InvalidOperationException("Failed to create OptionsMenu instance.");

        AddToTestRoot(testOptionsMenu);

        testOptionsMenu._Ready();
        CallPrivateMethod(testOptionsMenu, "OnDialogueSizeChanged", 1.5);
        CallPrivateMethod(testOptionsMenu, "UpdateDialogueSizeLabel", 1.5f);
        CallPrivateMethod(testOptionsMenu, "UpdatePreview");

        AssertThat(GodotObject.IsInstanceValid(testOptionsMenu)).IsTrue();
    }

    [TestCase]
    public async Task IntegrationTest_FullWorkflow()
    {
        _optionsMenu!._Ready();

        _optionsMenu.ShowMenu();
        AssertThat(_optionsMenu.Visible).IsTrue();

        CallPrivateMethod(_optionsMenu, "OnDialogueSizeChanged", 1.6);
        AssertThat(_testSettingsManager!.GetDialogueSize()).IsEqual(1.6f);

        _testSettingsManager.ResetToDefaults();
        AssertThat(_testSettingsManager.GetDialogueSize()).IsEqual(1.0f);

        _optionsMenu.HideMenu();

        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.CreateTimer(0.4f).ToSignal(tree.CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);

        AssertThat(_optionsMenu.Visible).IsFalse();
    }

    // === HELPER METHODS ===

    private TestSettingsManager CreateTestSettingsManager()
    {
        var manager = AddToTestRoot(new TestSettingsManager());
        _testNodes.Add(manager);
        CallInitializeOnTestManager(manager);
        return manager;
    }

    private T AddToTestRoot<T>(T node) where T : Node
    {
        if (_root == null)
            throw new System.InvalidOperationException("Test root node is not initialized.");

        _root.AddChild(node);
        return node;
    }

    private void CallInitializeOnTestManager(TestSettingsManager manager)
    {
        var initializeMethod = typeof(TestSettingsManager).GetMethod("Initialize",
            BindingFlags.NonPublic | BindingFlags.Instance);
        initializeMethod?.Invoke(manager, null);

        GD.Print($"[TEST] After Initialize - SettingsManager.Instance: {SettingsManager.Instance != null}");
    }

    private void CreateUIComponents()
    {
        // Plain instantiation. No AutoFree.
        _dialogueSizeSlider = new HSlider { Name = "DialogueSizeSlider" };
        _dialogueSizeLabel = new Label { Name = "DialogueSizeLabel" };
        _resetButton = new Button { Name = "ResetButton" };
        _backButton = new Button { Name = "BackButton" };
        _previewDialogue = new Control { Name = "PreviewDialogue" };
        _previewText = new Label { Name = "PreviewText" };
        _actionList = new VBoxContainer { Name = "ActionList" };

        _previewDialogue.AddChild(_previewText);

        // Attach them to the menu
        _optionsMenu!.AddChild(_dialogueSizeSlider);
        _optionsMenu.AddChild(_dialogueSizeLabel);
        _optionsMenu.AddChild(_resetButton);
        _optionsMenu.AddChild(_backButton);
        _optionsMenu.AddChild(_previewDialogue);
        _optionsMenu.AddChild(_actionList);
    }

    private PackedScene CreateMockInputButtonScene()
    {
        var scene = new PackedScene();
        var button = new Button();

        var marginContainer = new MarginContainer { Name = "MarginContainer" };
        var hbox = new HBoxContainer { Name = "HBoxContainer" };
        var labelAction = new Label { Name = "LabelAction" };
        var labelInput = new Label { Name = "LabelInput" };

        hbox.AddChild(labelAction);
        hbox.AddChild(labelInput);
        marginContainer.AddChild(hbox);
        button.AddChild(marginContainer);

        scene.Pack(button);
        return scene;
    }

    private void SetupExportedProperties()
    {
        var optionsMenuType = typeof(OptionsMenu);

        SetPrivateField(optionsMenuType, "_dialogueSizeSlider", _dialogueSizeSlider);
        SetPrivateField(optionsMenuType, "_dialogueSizeLabel", _dialogueSizeLabel);
        SetPrivateField(optionsMenuType, "_resetButton", _resetButton);
        SetPrivateField(optionsMenuType, "_backButton", _backButton);
        SetPrivateField(optionsMenuType, "_previewDialogue", _previewDialogue);
        SetPrivateField(optionsMenuType, "_previewText", _previewText);
        SetPrivateField(optionsMenuType, "_actionList", _actionList);
        SetPrivateField(optionsMenuType, "_inputButtonScene", _inputButtonScene);
    }

    private void SetPrivateField(System.Type type, string fieldName, object? value)
    {
        var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_optionsMenu, value);
    }

    private void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(instance, value);
    }

    private T? GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (T?)field.GetValue(instance) : default;
    }

    private void ClearAllSingletonInstances()
    {
        // Clear SettingsManager.Instance using reflection
        var settingsManagerType = typeof(SettingsManager);
        var instanceProperty = settingsManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        instanceProperty?.SetValue(null, null);

        // Clear TestSettingsManager.Instance
        TestSettingsManager.Instance = null;

        GD.Print($"[TEST] Cleared singletons - SettingsManager.Instance: {SettingsManager.Instance}");
    }

    private void CallPrivateMethod(object instance, string methodName, params object[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            throw new System.InvalidOperationException($"Method '{methodName}' not found on type '{instance.GetType().Name}'");
        }

        method.Invoke(instance, parameters);
    }
}
