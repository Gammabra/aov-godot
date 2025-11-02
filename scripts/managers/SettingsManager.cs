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

    [Signal]
    public delegate void InputBindingChangedEventHandler(string action);

    private const string _settingsFilePath = "user://settings.json";
    private SettingsData? _settings;
    private Dictionary<string, InputEvent[]> _defaultInputBindings = new();

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
        SaveDefaultInputBindings();
        LoadSettings();
        ApplyInputBindings();
        GD.Print("SettingsManager initialized successfully");
        GD.Print($"Applied {_settings?.InputBindings.Count ?? 0} custom input bindings");
    }

    /// <summary>
    /// Saves the default input bindings from project settings before any modifications.
    /// </summary>
    private void SaveDefaultInputBindings()
    {
        InputMap.LoadFromProjectSettings();

        foreach (var action in InputMap.GetActions())
        {
            var actionName = action.ToString();
            var events = InputMap.ActionGetEvents(actionName);
            var eventArray = new InputEvent[events.Count];

            for (int i = 0; i < events.Count; i++)
            {
                eventArray[i] = (InputEvent)events[i];
            }

            _defaultInputBindings[actionName] = eventArray;
        }
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
    /// Applies saved input bindings from settings to the InputMap.
    /// </summary>
    private void ApplyInputBindings()
    {
        if (_settings?.InputBindings == null || _settings.InputBindings.Count == 0)
        {
            return;
        }

        foreach (var binding in _settings.InputBindings)
        {
            if (!InputMap.HasAction(binding.Key))
            {
                GD.PrintErr($"Action '{binding.Key}' not found in InputMap");
                continue;
            }

            // Clear existing events
            InputMap.ActionEraseEvents(binding.Key);

            // Add saved event
            var inputEvent = DeserializeInputEvent(binding.Value);

            if (inputEvent != null)
            {
                InputMap.ActionAddEvent(binding.Key, inputEvent);
                SetInputBinding(binding.Key, inputEvent);
                GD.Print($"Applied custom binding for '{binding.Key}': {inputEvent.AsText()}");
            }
            else
            {
                GD.PrintErr($"Failed to deserialize input event for action '{binding.Key}'");
            }
        }
    }

    /// <summary>
    /// Gets the current input binding for an action.
    /// </summary>
    /// <param name="action">The action name to retrieve the binding for.</param>
    /// <returns>The InputEvent bound to the action, or null if not found.</returns>
    public InputEvent? GetInputBinding(string action)
    {
        if (!InputMap.HasAction(action))
            return null;

        var events = InputMap.ActionGetEvents(action);
        return events.Count > 0 ? (InputEvent)events[0] : null;
    }

    /// <summary>
    /// Sets a new input binding for an action and saves the settings.
    /// Emits a signal to notify other components of the change.
    /// </summary>
    /// <param name="action">The action name to set the binding for.</param>
    /// <param name="inputEvent">The InputEvent to bind to the action.</param>
    public void SetInputBinding(string action, InputEvent? inputEvent)
    {
        if (_settings == null || !InputMap.HasAction(action))
            return;

        // Update InputMap
        InputMap.ActionEraseEvents(action);
        InputMap.ActionAddEvent(action, inputEvent);

        // Save to settings
        _settings.InputBindings[action] = SerializeInputEvent(inputEvent);
        SaveSettings();

        EmitSignal(SignalName.InputBindingChanged, action);
        EmitSignal(SignalName.SettingsChanged, $"Input_{action}", Variant.From(inputEvent?.AsText() ?? "Unbound"));
    }

    /// <summary>
    /// Serializes an InputEvent to a dictionary for JSON storage.
    /// </summary>
    /// <param name="inputEvent">The InputEvent to serialize.</param>
    /// <returns>A dictionary representing the serialized InputEvent.</returns>
    private Dictionary<string, object> SerializeInputEvent(InputEvent? inputEvent)
    {
        var data = new Dictionary<string, object>();

        if (inputEvent is InputEventKey keyEvent)
        {
            data["type"] = "key";
            data["keycode"] = (int)keyEvent.Keycode;
            data["physical_keycode"] = (int)keyEvent.PhysicalKeycode;
            data["unicode"] = keyEvent.Unicode;
            data["pressed"] = keyEvent.Pressed;
            data["ctrl"] = keyEvent.CtrlPressed;
            data["shift"] = keyEvent.ShiftPressed;
            data["alt"] = keyEvent.AltPressed;
            data["meta"] = keyEvent.MetaPressed;
        }
        else if (inputEvent is InputEventMouseButton mouseEvent)
        {
            data["type"] = "mouse_button";
            data["button_index"] = (int)mouseEvent.ButtonIndex;
            data["pressed"] = mouseEvent.Pressed;
            data["ctrl"] = mouseEvent.CtrlPressed;
            data["shift"] = mouseEvent.ShiftPressed;
            data["alt"] = mouseEvent.AltPressed;
            data["meta"] = mouseEvent.MetaPressed;
        }
        else if (inputEvent is InputEventJoypadButton joypadButton)
        {
            data["type"] = "joypad_button";
            data["button_index"] = joypadButton.ButtonIndex;
            data["pressed"] = joypadButton.Pressed;
            data["device"] = joypadButton.Device;
        }
        else if (inputEvent is InputEventJoypadMotion joypadMotion)
        {
            data["type"] = "joypad_motion";
            data["axis"] = joypadMotion.Axis;
            data["axis_value"] = joypadMotion.AxisValue;
            data["device"] = joypadMotion.Device;
        }
        else if (inputEvent != null)
        {
            GD.PrintErr($"Unsupported InputEvent type: {inputEvent.GetType().Name}");
        }
        else
        {
            GD.Print("Input unbound (null InputEvent)");
        }

        return data;
    }

    /// <summary>
    /// Deserializes an InputEvent from a dictionary.
    /// </summary>
    /// <param name="data">The dictionary containing the serialized InputEvent data.</param>
    /// <returns>The deserialized InputEvent, or null if deserialization failed.</returns>
    private InputEvent? DeserializeInputEvent(Dictionary<string, object> data)
    {
        string? GetString(string key)
        {
            if (!data.TryGetValue(key, out var val))
                return null;

            return val switch
            {
                JsonElement e when e.ValueKind == JsonValueKind.String => e.GetString(),
                JsonElement e => e.ToString(),
                _ => val?.ToString()
            };
        }

        int GetInt(string key)
        {
            if (!data.TryGetValue(key, out var val))
                return 0;

            return val switch
            {
                JsonElement e when e.ValueKind == JsonValueKind.Number => e.GetInt32(),
                _ => Convert.ToInt32(val)
            };
        }

        bool GetBool(string key)
        {
            if (!data.TryGetValue(key, out var val))
                return false;

            return val switch
            {
                JsonElement e when e.ValueKind == JsonValueKind.True => true,
                JsonElement e when e.ValueKind == JsonValueKind.False => false,
                _ => Convert.ToBoolean(val)
            };
        }

        var type = GetString("type");
        if (type == null)
        {
            GD.PrintErr("Missing 'type' field in input binding");
            return null;
        }

        if (type == "key")
        {
            var keyEvent = new InputEventKey
            {
                Keycode = (Key)GetInt("keycode"),
                PhysicalKeycode = (Key)GetInt("physical_keycode"),
                Unicode = GetInt("unicode"),
                Pressed = GetBool("pressed"),
                CtrlPressed = GetBool("ctrl"),
                ShiftPressed = GetBool("shift"),
                AltPressed = GetBool("alt"),
                MetaPressed = GetBool("meta"),
            };
            return keyEvent;
        }

        if (type == "mouse_button")
        {
            var mouseEvent = new InputEventMouseButton
            {
                ButtonIndex = (MouseButton)GetInt("button_index"),
                Pressed = GetBool("pressed"),
                CtrlPressed = GetBool("ctrl"),
                ShiftPressed = GetBool("shift"),
                AltPressed = GetBool("alt"),
                MetaPressed = GetBool("meta"),
            };
            return mouseEvent;
        }

        if (type == "joypad_button")
        {
            var joypadButton = new InputEventJoypadButton
            {
                ButtonIndex = (JoyButton)GetInt("button_index"),
                Pressed = GetBool("pressed"),
                Device = GetInt("device"),
            };
            return joypadButton;
        }

        if (type == "joypad_motion")
        {
            double axisValueDouble = 0.0;
            if (data.TryGetValue("axis_value", out var val))
            {
                axisValueDouble = val switch
                {
                    JsonElement e when e.ValueKind == JsonValueKind.Number => e.GetDouble(),
                    _ => Convert.ToDouble(val)
                };
            }

            var joypadMotion = new InputEventJoypadMotion
            {
                Axis = (JoyAxis)GetInt("axis"),
                AxisValue = (float)axisValueDouble,
                Device = GetInt("device"),
            };
            return joypadMotion;
        }

        GD.PrintErr($"Unknown input type '{type}'");
        return null;
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

        // Reset input bindings to defaults
        foreach (var binding in _defaultInputBindings)
        {
            InputMap.ActionEraseEvents(binding.Key);
            foreach (var inputEvent in binding.Value)
            {
                InputMap.ActionAddEvent(binding.Key, inputEvent);
            }
        }

        SaveSettings();
        EmitSignal(SignalName.DialogueSizeChanged, _settings.DialogueSize);
        EmitSignal(SignalName.SettingsChanged, "Reset", true);

        // Notify about all input bindings being reset
        foreach (var action in _defaultInputBindings.Keys)
        {
            EmitSignal(SignalName.InputBindingChanged, action);
        }
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
    public Dictionary<string, Dictionary<string, object>> InputBindings { get; set; } = new();
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}
