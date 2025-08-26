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
        Instance = this;

        // Set the base class Instance as well
        var baseInstanceProperty = typeof(SettingsManager).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static);
        baseInstanceProperty?.SetValue(null, this);

        // Override the file path in the base class
        SetSettingsFilePath(_testFilePath);

        // Now call the base class load settings which will use our custom path
        base.LoadSettings();

        GD.Print("[TEST] TestSettingsManager initialized");
    }

    private void SetSettingsFilePath(string newPath)
    {
        var filePathField = typeof(SettingsManager).GetField("_settingsFilePath",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (filePathField != null && filePathField.IsStatic)
        {
            // If it's a static field, we need to be more careful
            GD.Print("[TEST] Warning: _settingsFilePath is static, using different approach");
        }
        else if (filePathField != null)
        {
            filePathField.SetValue(this, newPath);
            GD.Print($"[TEST] Set settings file path to: {newPath}");
        }
        else
        {
            // If we can't set the field, let's override the methods instead
            GD.Print("[TEST] Could not set file path field, will override methods");
        }
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
