using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

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

    private const string _settingsFilePath = "user://settings.json";
    private SettingsData? _settings;

    /// <summary>
    /// Initializes the SettingsManager singleton instance.
    /// Loads settings from the JSON file or creates a new default settings object.
    /// </summary>
    /// <remarks>
    /// This method is called automatically by Godot when the node is ready.
    /// It ensures that only one instance of SettingsManager exists and initializes the settings data.
    /// If the settings file does not exist, it creates a new default settings object and saves it.
    /// </remarks>
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

    /// <summary>
    /// Loads settings from the JSON file.
    /// If the file does not exist, creates a new default settings object.
    /// </summary>
    /// <remarks>
    /// This method reads the settings from a JSON file located at user://settings.json.
    /// If the file is not found, it initializes a new SettingsData object with default values
    /// and saves it to create the settings file.
    /// </remarks>
    public virtual void LoadSettings()
    {
        try
        {
            if (FileAccess.FileExists(_settingsFilePath))
            {
                using var file = FileAccess.Open(_settingsFilePath, FileAccess.ModeFlags.Read);
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

    /// <summary>
    /// Saves the current settings to the JSON file.
    /// </summary>
    /// <remarks>
    /// This method serializes the current settings data to a JSON string and writes it to the
    /// user://settings.json file. It handles any exceptions that may occur during the file operations.
    /// </remarks>
    public virtual void SaveSettings()
    {
        if (_settings == null) return;

        try
        {
            var jsonString = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            using var file = FileAccess.Open(_settingsFilePath, FileAccess.ModeFlags.Write);
            file?.StoreString(jsonString);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current dialogue size setting.
    /// </summary>
    /// <returns>The dialogue size as a float, defaulting to 1.0f if not set.</returns>
    /// <remarks>
    /// This method retrieves the dialogue size from the settings data.
    /// If the dialogue size is not set, it returns a default value of 1.0f.
    /// </remarks>
    public float GetDialogueSize()
    {
        return _settings?.DialogueSize ?? 1.0f;
    }

    /// <summary>
    /// Sets the dialogue size and saves the settings.
    /// Emits a signal to notify other components of the change.
    /// </summary>
    /// <param name="size">The new dialogue size to set.</param>
    /// <remarks>
    /// This method updates the dialogue size in the settings data and saves it to the JSON file.
    /// It also emits a SettingsChanged signal to notify other components of the change.
    /// The size is clamped between 0.5f and 2.0f to ensure it remains within a reasonable range.
    /// </remarks>
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

    /// <summary>
    /// Gets a custom setting by key, with an optional default value.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the setting is not found.</param>
    /// <returns>The setting value if found, otherwise the default value.</returns>
    /// <remarks>
    /// This method retrieves a custom setting from the settings data.
    /// If the setting is not found, it returns the provided default value.
    /// It uses JSON serialization to convert the setting value to the specified type.
    /// </remarks>
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

    /// <summary>
    /// Sets a custom setting by key and saves the settings.
    /// Emits a signal to notify other components of the change.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The key of the setting to set.</param>
    /// <param name="value">The value to set for the setting.</param>
    /// <remarks>
    /// This method updates a custom setting in the settings data and saves it to the JSON file.
    /// It also emits a SettingsChanged signal to notify other components of the change.
    /// The value is serialized to JSON format before being stored.
    /// </remarks>
    public void SetSetting<T>(string key, T value)
    {
        if (_settings == null) return;

        var jsonValue = JsonSerializer.Serialize(value);
        _settings.CustomSettings[key] = jsonValue;
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged, key, Variant.From(value));
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    /// <remarks>
    /// This method clears the current settings and initializes a new SettingsData object with default values.
    /// It saves the new settings to the JSON file and emits a SettingsChanged signal to notify other components.
    /// </remarks>
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
/// <remarks>
/// This class holds the settings data, including dialogue size and custom settings.
/// It is serialized to and from JSON format for persistence.
/// </remarks>
public class SettingsData
{
    public float DialogueSize { get; set; } = 1.0f;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}
