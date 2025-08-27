using Godot;
using AshesOfVelsingrad.Managers;
using System.Collections.Generic;
using System.Text.Json;
using System.Reflection;

namespace UnitTests;

public partial class TestSettingsManager : SettingsManager
{
    public static new TestSettingsManager? Instance { get; set; }

    // Each test manager gets its own temp file
    private readonly string _testFilePath;
    private static readonly Dictionary<string, string> _tempFiles = new();

    public TestSettingsManager()
    {
        Name = "TestSettingsManager";
        // Create unique temp file path for this instance
        _testFilePath = $"user://test_settings_{GetInstanceId()}.json";
        _tempFiles[GetInstanceId().ToString()] = _testFilePath;
        GD.Print($"[TEST] TestSettingsManager constructor called - using file: {_testFilePath}");
    }

    protected override void Initialize()
    {
        // Check for duplicate instances - if there's already a base instance, remove this one
        if (SettingsManager.Instance != null && SettingsManager.Instance != this)
        {
            GD.Print($"[TEST] Duplicate TestSettingsManager detected. Base instance exists: {SettingsManager.Instance.GetInstanceId()}, this instance: {GetInstanceId()}. Queueing for deletion.");
            QueueFree();
            return;
        }

        // Check for duplicate TestSettingsManager instances
        if (Instance != null && Instance != this)
        {
            GD.Print($"[TEST] Duplicate TestSettingsManager detected. TestSettings instance exists: {Instance.GetInstanceId()}, this instance: {GetInstanceId()}. Queueing for deletion.");
            QueueFree();
            return;
        }

        // Set both instances to this
        Instance = this;
        SetBaseInstance(this);

        // Load settings using our custom implementation
        LoadSettings();

        GD.Print("[TEST] TestSettingsManager initialized");
    }

    private void SetBaseInstance(TestSettingsManager instance)
    {
        var baseInstanceProperty = typeof(SettingsManager).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static);
        baseInstanceProperty?.SetValue(null, instance);
    }

    public override void LoadSettings()
    {
        GD.Print($"[TEST] TestSettingsManager.LoadSettings() called for file: {_testFilePath}");

        try
        {
            if (FileAccess.FileExists(_testFilePath))
            {
                using var file = FileAccess.Open(_testFilePath, FileAccess.ModeFlags.Read);
                var jsonString = file.GetAsText();
                var settings = JsonSerializer.Deserialize<SettingsData>(jsonString) ?? new SettingsData();
                SetSettingsField(settings);
                GD.Print($"[TEST] Loaded settings from file - DialogueSize: {settings.DialogueSize}");
            }
            else
            {
                var settings = new SettingsData();
                SetSettingsField(settings);
                SaveSettings(); // Create default settings file
                GD.Print("[TEST] Created new default settings file");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[TEST] Failed to load settings: {ex.Message}");
            var settings = new SettingsData();
            SetSettingsField(settings);
        }
    }

    public override void SaveSettings()
    {
        var settings = GetSettingsField();
        if (settings == null)
        {
            GD.Print("[TEST] Warning: settings is null in SaveSettings");
            return;
        }

        try
        {
            var jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            using var file = FileAccess.Open(_testFilePath, FileAccess.ModeFlags.Write);
            file?.StoreString(jsonString);

            GD.Print($"[TEST] SaveSettings() to {_testFilePath} - DialogueSize: {settings.DialogueSize}, CustomSettings count: {settings.CustomSettings.Count}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[TEST] Failed to save settings: {ex.Message}");
        }
    }

    // Override SetDialogueSize to ensure it works with our test setup
    public new void SetDialogueSize(float size)
    {
        var settings = GetSettingsField();
        if (settings == null)
        {
            settings = new SettingsData();
            SetSettingsField(settings);
        }

        var clampedSize = Mathf.Clamp(size, 0.5f, 2.0f);
        if (Mathf.Abs(settings.DialogueSize - clampedSize) > 0.01f)
        {
            settings.DialogueSize = clampedSize;
            SaveSettings();
            EmitSignal(SignalName.SettingsChanged, "DialogueSize", clampedSize);
            EmitSignal(SignalName.DialogueSizeChanged, clampedSize);
            GD.Print($"[TEST] SetDialogueSize called with {size}, clamped to {clampedSize}");
        }
    }

    // Override SetSetting to ensure it works with our test setup
    public new void SetSetting<T>(string key, T value)
    {
        var settings = GetSettingsField();
        if (settings == null)
        {
            settings = new SettingsData();
            SetSettingsField(settings);
        }

        var jsonValue = JsonSerializer.Serialize(value);
        settings.CustomSettings[key] = jsonValue;
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged, key, Variant.From(value));
        GD.Print($"[TEST] SetSetting called with key: {key}, value: {value}");
    }

    // Override GetSetting to ensure it works with our test setup
    public new T? GetSetting<T>(string key, T? defaultValue = default(T))
    {
        var settings = GetSettingsField();
        if (settings?.CustomSettings.TryGetValue(key, out var value) == true)
        {
            try
            {
                var jsonString = value?.ToString();
                if (!string.IsNullOrEmpty(jsonString))
                {
                    var result = JsonSerializer.Deserialize<T>(jsonString);
                    GD.Print($"[TEST] GetSetting({key}) returning: {result}");
                    return result ?? defaultValue;
                }
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"[TEST] Error deserializing setting {key}: {ex.Message}");
                return defaultValue;
            }
        }
        GD.Print($"[TEST] GetSetting({key}) returning default: {defaultValue}");
        return defaultValue;
    }

    // Override GetDialogueSize to ensure it works with our test setup
    public new float GetDialogueSize()
    {
        var settings = GetSettingsField();
        var result = settings?.DialogueSize ?? 1.0f;
        GD.Print($"[TEST] GetDialogueSize() called, returning: {result}");
        return result;
    }

    // Override ResetToDefaults to ensure it works with our test setup
    public new void ResetToDefaults()
    {
        var settings = new SettingsData();
        SetSettingsField(settings);
        SaveSettings();
        EmitSignal(SignalName.DialogueSizeChanged, settings.DialogueSize);
        EmitSignal(SignalName.SettingsChanged, "Reset", true);
        GD.Print("[TEST] ResetToDefaults called");
    }

    private void SetSettingsField(SettingsData settings)
    {
        var settingsField = typeof(SettingsManager).GetField("_settings",
            BindingFlags.NonPublic | BindingFlags.Instance);
        settingsField?.SetValue(this, settings);
    }

    private SettingsData? GetSettingsField()
    {
        var settingsField = typeof(SettingsManager).GetField("_settings",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return settingsField?.GetValue(this) as SettingsData;
    }

    // Method to simulate persistence between manager instances
    public void SimulatePersistence(TestSettingsManager otherManager)
    {
        if (FileAccess.FileExists(_testFilePath))
        {
            // Copy our temp file to the other manager's temp file
            using var sourceFile = FileAccess.Open(_testFilePath, FileAccess.ModeFlags.Read);
            if (sourceFile != null)
            {
                var content = sourceFile.GetAsText();
                using var destFile = FileAccess.Open(otherManager._testFilePath, FileAccess.ModeFlags.Write);
                destFile?.StoreString(content);
                GD.Print($"[TEST] Copied settings from {_testFilePath} to {otherManager._testFilePath}");
            }
        }
    }

    // Clear all temp files
    public static void ClearTempFiles()
    {
        foreach (var filePath in _tempFiles.Values)
        {
            if (FileAccess.FileExists(filePath))
            {
                DirAccess.RemoveAbsolute(filePath);
                GD.Print($"[TEST] Deleted temp file: {filePath}");
            }
        }
        _tempFiles.Clear();
        GD.Print("[TEST] Cleared all temp files");
    }

    public override void _ExitTree()
    {
        // Clean up our temp file when this instance is destroyed
        if (FileAccess.FileExists(_testFilePath))
        {
            DirAccess.RemoveAbsolute(_testFilePath);
        }
        base._ExitTree();
    }
}
