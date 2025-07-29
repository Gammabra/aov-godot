using Godot;
using System;
using System.Text.Json;
using System.Collections.Generic;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Manages game settings and provides centralized access to configuration options.
/// Follows the Manager Pattern from your architecture.
/// </summary>
public partial class SettingsManager : BaseManager
{
    public static new SettingsManager? Instance { get; private set; }

    [Signal]
    public delegate void SettingsChangedEventHandler(string settingKey, Variant newValue);

    [Signal]
    public delegate void DialogueSizeChangedEventHandler(float newSize);

    private const string SettingsFilePath = "user://settings.json";
    private SettingsData? _settings;

    protected override void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
        LoadSettings();
        GD.Print("SettingsManager initialized successfully");
    }

    public void LoadSettings()
    {
        try
        {
            if (FileAccess.FileExists(SettingsFilePath))
            {
                using var file = FileAccess.Open(SettingsFilePath, FileAccess.ModeFlags.Read);
                var jsonString = file.GetAsText();
                _settings = JsonSerializer.Deserialize<SettingsData>(jsonString) ?? new SettingsData();
            }
            else
            {
                _settings = new SettingsData();
                SaveSettings(); // Create default settings file
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load settings: {ex.Message}");
            _settings = new SettingsData();
        }
    }

    public void SaveSettings()
    {
        if (_settings == null) return;

        try
        {
            var jsonString = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            using var file = FileAccess.Open(SettingsFilePath, FileAccess.ModeFlags.Write);
            file?.StoreString(jsonString);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save settings: {ex.Message}");
        }
    }

    // Dialogue size setting
    public float GetDialogueSize()
    {
        return _settings?.DialogueSize ?? 1.0f;
    }

    public void SetDialogueSize(float size)
    {
        if (_settings == null) return;

        var clampedSize = Mathf.Clamp(size, 0.5f, 2.0f);
        if (Mathf.Abs(_settings.DialogueSize - clampedSize) > 0.01f)
        {
            _settings.DialogueSize = clampedSize;
            SaveSettings();
            EmitSignal(SignalName.SettingsChanged, "DialogueSize", clampedSize);
            EmitSignal(SignalName.DialogueSizeChanged, clampedSize);
        }
    }

    // Generic setting access
    public T? GetSetting<T>(string key, T? defaultValue = default(T))
    {
        if (_settings?.CustomSettings.TryGetValue(key, out var value) == true)
        {
            try
            {
                var jsonString = value?.ToString();
                if (!string.IsNullOrEmpty(jsonString))
                {
                    return JsonSerializer.Deserialize<T>(jsonString) ?? defaultValue;
                }
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public void SetSetting<T>(string key, T value)
    {
        if (_settings == null) return;

        var jsonValue = JsonSerializer.Serialize(value);
        _settings.CustomSettings[key] = jsonValue;
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged, key, Variant.From(value));
    }

    // Reset to defaults
    public void ResetToDefaults()
    {
        _settings = new SettingsData();
        SaveSettings();
        EmitSignal(SignalName.DialogueSizeChanged, _settings.DialogueSize);
        EmitSignal(SignalName.SettingsChanged, "Reset", true);
    }
}

/// <summary>
/// Data structure for settings storage
/// </summary>
public class SettingsData
{
    public float DialogueSize { get; set; } = 1.0f;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}
