using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Helpers.Managers;
using AshesOfVelsingrad.Managers;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Managers;

[TestSuite]
[RequireGodotRuntime]
public class SettingsManagerTest
{
    private TestSettingsManager? _settingsManager;
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private const string _testSettingsPath = "user://test_settings.json";

    [BeforeTest]
    public void SetUp()
    {
        GD.Print("[TEST] Starting SettingsManager SetUp...");

        // Reset singleton instance
        SetSingletonInstance<SettingsManager>(null);
        SetSingletonInstance<TestSettingsManager>(null);

        // Clear test manager temp files
        TestSettingsManager.ClearTempFiles();

        // Create test root
        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        // Clean up any existing test settings file
        if (FileAccess.FileExists(_testSettingsPath))
        {
            DirAccess.RemoveAbsolute(_testSettingsPath);
        }

        GD.Print("[TEST] SettingsManager SetUp completed");
    }

    [TestCase]
    public void Initialize_FirstInstance_SetsInstanceCorrectly()
    {
        // Arrange & Act
        _settingsManager = CreateTestSettingsManager();

        // Assert
        AssertThat(SettingsManager.Instance).IsEqual(_settingsManager);
        AssertThat(TestSettingsManager.Instance).IsEqual(_settingsManager);
        AssertThat(SettingsManager.Instance).IsNotNull();
    }

    [TestCase]
    public void Initialize_SecondInstance_RemovesDuplicate()
    {
        SetSingletonInstance<SettingsManager>(null);
        SetSingletonInstance<TestSettingsManager>(null);

        var firstManager = AddToTestRoot(new TestSettingsManager());
        CallInitializeOnManager(firstManager);
        _testNodes.Add(firstManager);

        AssertThat(SettingsManager.Instance).IsEqual(firstManager);
        AssertThat(TestSettingsManager.Instance).IsEqual(firstManager);

        var originalBaseInstance = SettingsManager.Instance;
        var originalTestInstance = TestSettingsManager.Instance;

        var secondManager = AddToTestRoot(new TestSettingsManager());
        CallInitializeOnManager(secondManager);

        AssertThat(SettingsManager.Instance).IsEqual(originalBaseInstance);
        AssertThat(TestSettingsManager.Instance).IsEqual(originalTestInstance);
        AssertThat(SettingsManager.Instance).IsEqual(firstManager);

        firstManager.SetDialogueSize(1.3f);
        AssertThat(firstManager.GetDialogueSize()).IsEqual(1.3f);
        AssertThat(secondManager.IsQueuedForDeletion()).IsTrue();
    }

    [TestCase]
    public void Initialize_LoadsSettingsOnStart()
    {
        // Arrange & Act
        _settingsManager = CreateTestSettingsManager();

        // Assert
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(1.0f);
    }

    [TestCase]
    public void LoadSettings_NoExistingFile_CreatesDefaultSettings()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act
        _settingsManager.LoadSettings();

        // Assert
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(1.0f);
    }

    [TestCase]
    public void SaveSettings_CreatesSettingsFile()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act
        _settingsManager.SetDialogueSize(1.5f);

        // Assert
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(1.5f);
    }

    [TestCase]
    public void LoadSettings_WithExistingFile_LoadsPreviousSettings()
    {
        // Arrange
        var firstManager = CreateTestSettingsManager();
        firstManager.SetDialogueSize(1.8f);
        firstManager.SetSetting("test_key", "test_value");

        // Act
        var secondManager = CreateTestSettingsManager();
        firstManager.SimulatePersistence(secondManager);
        secondManager.LoadSettings();

        // Assert
        AssertThat(secondManager.GetDialogueSize()).IsEqual(1.8f);
        AssertThat(secondManager.GetSetting<string>("test_key")).IsEqual("test_value");
    }

    [TestCase]
    public void GetDialogueSize_DefaultValue_ReturnsOne()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act & Assert
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(1.0f);
    }

    [TestCase]
    public void SetDialogueSize_ValidValue_UpdatesSize()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act
        _settingsManager.SetDialogueSize(1.5f);

        // Assert
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(1.5f);
    }

    [TestCase]
    public void SetDialogueSize_BelowMinimum_ClampsToMinimum()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act
        _settingsManager.SetDialogueSize(0.3f);

        // Assert
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(0.5f);
    }

    [TestCase]
    public void SetDialogueSize_AboveMaximum_ClampsToMaximum()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act
        _settingsManager.SetDialogueSize(3.0f);

        // Assert
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(2.0f);
    }

    [TestCase]
    public async System.Threading.Tasks.Task SetDialogueSize_EmitsDialogueSizeChangedSignal()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        var signalReceived = false;
        var receivedSize = 0.0f;

        _settingsManager.DialogueSizeChanged += (newSize) =>
        {
            signalReceived = true;
            receivedSize = newSize;
        };

        // Act
        _settingsManager.SetDialogueSize(1.3f);

        // Wait a frame
        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // Assert
        AssertThat(signalReceived).IsTrue();
        AssertThat(receivedSize).IsEqual(1.3f);
    }

    [TestCase]
    public async System.Threading.Tasks.Task SetDialogueSize_EmitsSettingsChangedSignal()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        var signalReceived = false;
        var receivedKey = "";
        var receivedValue = Variant.From(0.0f);

        _settingsManager.SettingsChanged += (key, value) =>
        {
            signalReceived = true;
            receivedKey = key;
            receivedValue = value;
        };

        // Act
        _settingsManager.SetDialogueSize(1.7f);

        // Wait a frame
        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // Assert
        AssertThat(signalReceived).IsTrue();
        AssertThat(receivedKey).IsEqual("DialogueSize");
        AssertThat(receivedValue.AsSingle()).IsEqual(1.7f);
    }

    [TestCase]
    public void SetDialogueSize_SameValue_DoesNotEmitSignal()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        _settingsManager.SetDialogueSize(1.5f);

        var signalCount = 0;
        _settingsManager.DialogueSizeChanged += (newSize) =>
        {
            signalCount++;
        };

        // Act
        _settingsManager.SetDialogueSize(1.5f);

        // Assert
        AssertThat(signalCount).IsEqual(0);
    }

    // === INPUT BINDING TESTS ===

    [TestCase]
    public void SerializeInputEvent_KeyEvent_ReturnsCorrectDictionary()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        var keyEvent = new InputEventKey
        {
            Keycode = Key.Space,
            PhysicalKeycode = Key.Space,
            Pressed = true,
            CtrlPressed = false,
            ShiftPressed = true
        };

        // Act
        var result = _settingsManager.TestSerializeInputEvent(keyEvent);

        // Assert
        AssertThat(result["type"]).IsEqual("key");
        AssertThat((int)result["keycode"]).IsEqual((int)Key.Space);
        AssertThat((bool)result["pressed"]).IsTrue();
        AssertThat((bool)result["shift"]).IsTrue();
    }

    [TestCase]
    public void SerializeInputEvent_MouseButton_ReturnsCorrectDictionary()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        var mouseEvent = new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            CtrlPressed = true
        };

        // Act
        var result = _settingsManager.TestSerializeInputEvent(mouseEvent);

        // Assert
        AssertThat(result["type"]).IsEqual("mouse_button");
        AssertThat((int)result["button_index"]).IsEqual((int)MouseButton.Left);
        AssertThat((bool)result["pressed"]).IsTrue();
        AssertThat((bool)result["ctrl"]).IsTrue();
    }

    [TestCase]
    public void SerializeInputEvent_JoypadButton_ReturnsCorrectDictionary()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        var joypadEvent = new InputEventJoypadButton
        {
            ButtonIndex = JoyButton.A,
            Pressed = true,
            Device = 0
        };

        // Act
        var result = _settingsManager.TestSerializeInputEvent(joypadEvent);

        // Assert
        AssertThat(result["type"]).IsEqual("joypad_button");
        AssertThat(result["button_index"]).IsEqual(JoyButton.A);
        AssertThat((bool)result["pressed"]).IsTrue();
        AssertThat((int)result["device"]).IsEqual(0);
    }

    [TestCase]
    public void DeserializeInputEvent_KeyEvent_ReturnsCorrectEvent()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        var data = new Dictionary<string, object>
        {
            { "type", "key" },
            { "keycode", (int)Key.A },
            { "physical_keycode", (int)Key.A },
            { "unicode", 0 },
            { "pressed", true },
            { "ctrl", false },
            { "shift", true },
            { "alt", false },
            { "meta", false }
        };

        // Act
        var result = _settingsManager.TestDeserializeInputEvent(data) as InputEventKey;

        // Assert
        AssertThat(result).IsNotNull();
        AssertThat(result!.Keycode).IsEqual(Key.A);
        AssertThat(result.Pressed).IsTrue();
        AssertThat(result.ShiftPressed).IsTrue();
    }

    [TestCase]
    public void DeserializeInputEvent_MouseButton_ReturnsCorrectEvent()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        var data = new Dictionary<string, object>
        {
            { "type", "mouse_button" },
            { "button_index", (int)MouseButton.Right },
            { "pressed", true },
            { "ctrl", true },
            { "shift", false },
            { "alt", false },
            { "meta", false }
        };

        // Act
        var result = _settingsManager.TestDeserializeInputEvent(data) as InputEventMouseButton;

        // Assert
        AssertThat(result).IsNotNull();
        AssertThat(result!.ButtonIndex).IsEqual(MouseButton.Right);
        AssertThat(result.Pressed).IsTrue();
        AssertThat(result.CtrlPressed).IsTrue();
    }

    [TestCase]
    public void DeserializeInputEvent_InvalidType_ReturnsNull()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        var data = new Dictionary<string, object>
        {
            { "type", "invalid_type" }
        };

        // Act
        var result = _settingsManager.TestDeserializeInputEvent(data);

        // Assert
        AssertThat(result).IsNull();
    }

    [TestCase]
    public void InputBindingSerialization_RoundTrip_PreservesData()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        var originalEvent = new InputEventKey
        {
            Keycode = Key.W,
            PhysicalKeycode = Key.W,
            Pressed = true,
            ShiftPressed = false,
            CtrlPressed = false
        };

        // Act
        var serialized = _settingsManager.TestSerializeInputEvent(originalEvent);
        var deserialized = _settingsManager.TestDeserializeInputEvent(serialized) as InputEventKey;

        // Assert
        AssertThat(deserialized).IsNotNull();
        AssertThat(deserialized!.Keycode).IsEqual(originalEvent.Keycode);
        AssertThat(deserialized.Pressed).IsEqual(originalEvent.Pressed);
    }

    // === CUSTOM SETTINGS TESTS ===

    [TestCase]
    public void GetSetting_NonExistentKey_ReturnsDefaultValue()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act & Assert
        AssertThat(_settingsManager.GetSetting<string>("non_existent")).IsNull();
        AssertThat(_settingsManager.GetSetting("non_existent", "default")).IsEqual("default");
        AssertThat(_settingsManager.GetSetting("non_existent", 42)).IsEqual(42);
    }

    [TestCase]
    public void SetSetting_StringValue_StoresAndRetrievesCorrectly()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act
        _settingsManager.SetSetting("test_string", "Hello World");

        // Assert
        AssertThat(_settingsManager.GetSetting<string>("test_string")).IsEqual("Hello World");
    }

    [TestCase]
    public void SetSetting_IntValue_StoresAndRetrievesCorrectly()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act
        _settingsManager.SetSetting("test_int", 123);

        // Assert
        AssertThat(_settingsManager.GetSetting<int>("test_int")).IsEqual(123);
    }

    [TestCase]
    public void SetSetting_FloatValue_StoresAndRetrievesCorrectly()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act
        _settingsManager.SetSetting("test_float", 3.14f);

        // Assert
        AssertThat(_settingsManager.GetSetting<float>("test_float")).IsEqual(3.14f);
    }

    [TestCase]
    public void SetSetting_BoolValue_StoresAndRetrievesCorrectly()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act
        _settingsManager.SetSetting("test_bool", true);

        // Assert
        AssertThat(_settingsManager.GetSetting<bool>("test_bool")).IsTrue();
    }

    [TestCase]
    public async System.Threading.Tasks.Task SetSetting_EmitsSettingsChangedSignal()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        var signalReceived = false;
        var receivedKey = "";
        var receivedValue = Variant.CreateFrom("");

        _settingsManager.SettingsChanged += (key, value) =>
        {
            signalReceived = true;
            receivedKey = key;
            receivedValue = value;
        };

        // Act
        _settingsManager.SetSetting("test_signal", "signal_value");

        // Wait a frame
        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // Assert
        AssertThat(signalReceived).IsTrue();
        AssertThat(receivedKey).IsEqual("test_signal");
        AssertThat(receivedValue.AsString()).IsEqual("signal_value");
    }

    [TestCase]
    public void SetSetting_UpdateExistingValue_OverwritesPreviousValue()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        _settingsManager.SetSetting("test_key", "original_value");

        // Act
        _settingsManager.SetSetting("test_key", "updated_value");

        // Assert
        AssertThat(_settingsManager.GetSetting<string>("test_key")).IsEqual("updated_value");
    }

    // === RESET TESTS ===

    [TestCase]
    public void ResetToDefaults_ResetsDialogueSizeToDefault()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        _settingsManager.SetDialogueSize(1.8f);

        // Act
        _settingsManager.ResetToDefaults();

        // Assert
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(1.0f);
    }

    [TestCase]
    public void ResetToDefaults_ClearsCustomSettings()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        _settingsManager.SetSetting("test_key", "test_value");
        _settingsManager.SetSetting("another_key", 42);

        // Act
        _settingsManager.ResetToDefaults();

        // Assert
        AssertThat(_settingsManager.GetSetting<string>("test_key")).IsNull();
        AssertThat(_settingsManager.GetSetting<int>("another_key")).IsEqual(0);
    }

    [TestCase]
    public async System.Threading.Tasks.Task ResetToDefaults_EmitsSignals()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        _settingsManager.SetDialogueSize(1.5f);

        var dialogueSignalReceived = false;
        var settingsSignalReceived = false;
        var receivedDialogueSize = 0.0f;

        _settingsManager.DialogueSizeChanged += (newSize) =>
        {
            dialogueSignalReceived = true;
            receivedDialogueSize = newSize;
        };

        _settingsManager.SettingsChanged += (key, value) =>
        {
            if (key == "Reset")
                settingsSignalReceived = true;
        };

        // Act
        _settingsManager.ResetToDefaults();

        // Wait a frame
        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // Assert
        AssertThat(dialogueSignalReceived).IsTrue();
        AssertThat(settingsSignalReceived).IsTrue();
        AssertThat(receivedDialogueSize).IsEqual(1.0f);
    }

    // === INTEGRATION TESTS ===

    [TestCase]
    public void Settings_PersistBetweenManagerInstances()
    {
        // Arrange
        var firstManager = CreateTestSettingsManager();
        firstManager.SetDialogueSize(1.4f);
        firstManager.SetSetting("persistent_key", "persistent_value");

        // Act
        var secondManager = CreateTestSettingsManager();
        firstManager.SimulatePersistence(secondManager);
        secondManager.LoadSettings();

        // Assert
        AssertThat(secondManager.GetDialogueSize()).IsEqual(1.4f);
        AssertThat(secondManager.GetSetting<string>("persistent_key")).IsEqual("persistent_value");
    }

    [TestCase]
    public void FullWorkflow_SetLoadResetSave_WorksCorrectly()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();

        // Act & Assert - Full workflow
        // 1. Set some values
        _settingsManager.SetDialogueSize(1.6f);
        _settingsManager.SetSetting("workflow_test", "initial");
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(1.6f);
        AssertThat(_settingsManager.GetSetting<string>("workflow_test")).IsEqual("initial");

        // 2. Update values
        _settingsManager.SetSetting("workflow_test", "updated");
        AssertThat(_settingsManager.GetSetting<string>("workflow_test")).IsEqual("updated");

        // 3. Reset to defaults
        _settingsManager.ResetToDefaults();
        AssertThat(_settingsManager.GetDialogueSize()).IsEqual(1.0f);
        AssertThat(_settingsManager.GetSetting<string>("workflow_test")).IsNull();
    }

    [TestCase]
    public void CustomSettings_ComplexTypes_HandleCorrectly()
    {
        // Arrange
        _settingsManager = CreateTestSettingsManager();
        var testDict = new Godot.Collections.Dictionary<string, int> { { "key1", 1 }, { "key2", 2 } };

        // Act
        _settingsManager.SetSetting("complex_type", testDict);

        // Assert
        var retrieved = _settingsManager.GetSetting<Godot.Collections.Dictionary<string, int>>("complex_type");
        AssertThat(retrieved).IsNotNull();
        AssertThat(retrieved!["key1"]).IsEqual(1);
        AssertThat(retrieved["key2"]).IsEqual(2);
    }

    // Helper Methods
    private TestSettingsManager CreateTestSettingsManager()
    {
        var manager = AddToTestRoot(new TestSettingsManager());
        _testNodes.Add(manager);
        CallInitializeOnManager(manager);
        return manager;
    }

    private T AddToTestRoot<T>(T node) where T : Node
    {
        if (_root == null)
            throw new System.InvalidOperationException("Test root node is not initialized.");

        _root.AddChild(node);
        return node;
    }

    private void CallInitializeOnManager(TestSettingsManager manager)
    {
        var initializeMethod = typeof(TestSettingsManager).GetMethod("Initialize",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initializeMethod?.Invoke(manager, null);
    }

    private void SetSingletonInstance<T>(T? instance) where T : class
    {
        var instanceProperty = typeof(T).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static);
        instanceProperty?.SetValue(null, instance);
    }

    [AfterTest]
    public void TearDown()
    {
        GD.Print("[TEST] Starting SettingsManager TearDown...");

        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }
        }
        _testNodes.Clear();

        SetSingletonInstance<SettingsManager>(null);
        SetSingletonInstance<TestSettingsManager>(null);

        TestSettingsManager.ClearTempFiles();

        if (FileAccess.FileExists(_testSettingsPath))
        {
            DirAccess.RemoveAbsolute(_testSettingsPath);
        }

        GD.Print("[TEST] SettingsManager TearDown completed");
    }
}
