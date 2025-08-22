using Godot;
using AshesOfVelsingrad.Managers;

namespace UnitTests;

public partial class TestSettingsManager : SettingsManager
{
    public static new TestSettingsManager? Instance { get; set; }

    public TestSettingsManager()
    {
        Name = "TestSettingsManager";
        GD.Print("[TEST] TestSettingsManager constructor called");

        InitializeForTesting();
    }

    protected override void Initialize()
    {
        Instance = this;

        var baseInstanceProperty = typeof(SettingsManager).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        baseInstanceProperty?.SetValue(null, this);

        InitializeForTesting();

        GD.Print("[TEST] TestSettingsManager initialized");
    }

    private void InitializeForTesting()
    {
        var settingsField = typeof(SettingsManager).GetField("_settings",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (settingsField != null)
        {
            var defaultSettings = new SettingsData();
            settingsField.SetValue(this, defaultSettings);
            GD.Print("[TEST] Default settings initialized for TestSettingsManager");
        }
    }

    public override void LoadSettings()
    {
        GD.Print("[TEST] TestSettingsManager.LoadSettings() called - using default settings");
        InitializeForTesting();
    }

    public override void SaveSettings()
    {
        GD.Print("[TEST] TestSettingsManager.SaveSettings() called - not saving to file in tests");
    }
}
