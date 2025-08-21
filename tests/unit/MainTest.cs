using AshesOfVelsingrad;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.UI.Menus;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class MainTest
{
    private TestableMain? _testableMain;
    private Control? _mockMenuContainer;
    private readonly List<Node> _testNodes = new();

    [Before]
    public void SetUp()
    {
        try
        {
            GD.Print("Starting SetUp...");
            _testableMain = AutoFree(new TestableMain());
            _mockMenuContainer = AutoFree(new Control());
            
            GD.Print("SetUp completed successfully");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Exception in SetUp: {ex.Message}");
            throw;
        }
    }

    [TestCase]
    public void InitializeMenus_WithNullSettingsManager_DoesNotInitialize()
    {
        // Arrange
        SetSingletonInstance<SettingsManager>(null);
        SetupValidMenuManager();

        if (_testableMain == null)
            throw new System.Exception("TestableMain is null");
        _testableMain.SetMenuContainer(_mockMenuContainer);  // Utiliser la méthode publique

        // Act
        _testableMain.InitializeMenus();

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testableMain.OptionsMenuInstantiateCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuManager_DoesNotInitialize()
    {
        // Arrange
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(null);

        if (_testableMain == null)
            throw new System.Exception("TestableMain is null");
        _testableMain.SetMenuContainer(_mockMenuContainer);  // Utiliser la méthode publique

        // Act
        _testableMain.InitializeMenus();

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testableMain.OptionsMenuInstantiateCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_InstantiatesAndAddsMenus()
    {
        // Arrange
        SetupValidManagers();

        if (_testableMain == null)
            throw new System.Exception("TestableMain is null");
        _testableMain.SetMenuContainer(_mockMenuContainer);  // Utiliser la méthode publique

        GD.Print($"[TEST] Before call - Main instantiate count: {_testableMain.MainMenuInstantiateCount}");
        GD.Print($"[TEST] Before call - Options instantiate count: {_testableMain.OptionsMenuInstantiateCount}");

        // Act
        _testableMain.InitializeMenus();

        GD.Print($"[TEST] After call - Main instantiate count: {_testableMain.MainMenuInstantiateCount}");
        GD.Print($"[TEST] After call - Options instantiate count: {_testableMain.OptionsMenuInstantiateCount}");

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(2);
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(1);
        AssertThat(_testableMain.OptionsMenuInstantiateCount).IsEqual(1);

        // Clean up
        _testableMain.Reset();
        CleanupMenuContainer();
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuContainer_DoesNotInitialize()
    {
        // Arrange
        SetupValidManagers();

        if (_testableMain == null)
            throw new System.Exception("TestableMain is null");
        _testableMain.SetMenuContainer(null);  // Utiliser la méthode publique

        // Act
        _testableMain.InitializeMenus();

        // Assert
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testableMain.OptionsMenuInstantiateCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_RegistersAndShowsMenus()
    {
        // Arrange
        var testMenuManager = CreateTestMenuManager();
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(testMenuManager);

        if (_testableMain == null)
            throw new System.Exception("TestableMain is null");
        _testableMain.SetMenuContainer(_mockMenuContainer);  // Utiliser la méthode publique

        // Act
        _testableMain.InitializeMenus();

        // Assert
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.MAIN_MENU)).IsTrue();
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.OPTIONS_MENU)).IsTrue();
        AssertThat(testMenuManager.LastShownMenu).IsEqual(MenuManager.MAIN_MENU);

        // Clean up
        _testableMain.Reset();
        CleanupMenuContainer();
    }

    [TestCase]
    public void InitializeMenus_Integration_FullWorkflowSuccess()
    {
        // Arrange
        var testMenuManager = CreateTestMenuManager();
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(testMenuManager);

        if (_testableMain == null)
            throw new System.Exception("TestableMain is null");
        _testableMain.SetMenuContainer(_mockMenuContainer);  // Utiliser la méthode publique

        var initialChildCount = _mockMenuContainer?.GetChildCount() ?? 0;
        GD.Print($"[TEST] Initial child count: {initialChildCount}");

        // Act
        _testableMain.InitializeMenus();

        // Assert - verify complete workflow
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(1);
        AssertThat(_testableMain.OptionsMenuInstantiateCount).IsEqual(1);
        
        var finalChildCount = _mockMenuContainer?.GetChildCount() ?? 0;
        GD.Print($"[TEST] Final child count: {finalChildCount}");
        AssertThat(finalChildCount).IsEqual(initialChildCount + 2);
        
        AssertThat(testMenuManager.RegisteredMenus.Count).IsEqual(2);
        AssertThat(testMenuManager.LastShownMenu).IsEqual(MenuManager.MAIN_MENU);
        
        // Clean up
        _testableMain.Reset();
        CleanupMenuContainer();
    }

    // Helper methods
    private void SetupValidManagers()
    {
        SetupValidSettingsManager();
        SetupValidMenuManager();
    }

    private void SetupValidSettingsManager()
    {
        var testSettingsManager = CreateTestSettingsManager();
        SetSingletonInstance<SettingsManager>(testSettingsManager);
    }

    private void SetupValidMenuManager()
    {
        var testMenuManager = CreateTestMenuManager();
        SetSingletonInstance<MenuManager>(testMenuManager);
    }

    private TestSettingsManager CreateTestSettingsManager()
    {
        var manager = AutoFree(new TestSettingsManager());
        if (manager == null)
        {
            throw new System.Exception("TestSettingsManager creation failed");
        }
        _testNodes.Add(manager);
        return manager;
    }

    private TestMenuManager CreateTestMenuManager()
    {
        var manager = AutoFree(new TestMenuManager());
        if (manager == null)
        {
            throw new System.Exception("TestMenuManager creation failed");
        }
        _testNodes.Add(manager);
        return manager;
    }

    private void CleanupMenuContainer()
    {
        if (_mockMenuContainer == null) return;
        
        var children = _mockMenuContainer.GetChildren();
        foreach (Node child in children)
        {
            if (GodotObject.IsInstanceValid(child) && !child.IsQueuedForDeletion())
            {
                _mockMenuContainer.RemoveChild(child);
                child.QueueFree();
            }
        }
    }

    private void SetSingletonInstance<T>(T? instance) where T : class
    {
        var instanceProperty = typeof(T).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        if (instanceProperty != null)
        {
            instanceProperty.SetValue(null, instance);
            GD.Print($"[TEST] Set {typeof(T).Name}.Instance to {(instance != null ? "valid instance" : "null")}");
            
            var currentValue = instanceProperty.GetValue(null);
            GD.Print($"[TEST] Verification: {typeof(T).Name}.Instance is now {(currentValue != null ? "not null" : "null")}");
        }
        else
        {
            GD.PrintErr($"[TEST] Could not find Instance property on {typeof(T).Name}");
        }
    }

    [After]
    public void TearDown()
    {
        // Nettoyer les nœuds de test
        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }
        }
        _testNodes.Clear();

        // Nettoyer la classe testable
        _testableMain?.Reset();
        
        // Nettoyer le menu container
        CleanupMenuContainer();
        
        // Réinitialiser les singletons
        SetSingletonInstance<SettingsManager>(null);
        SetSingletonInstance<MenuManager>(null);
        
        GD.Print("[TEST] TearDown completed");
    }
}

// Test helper classes - inchangées
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

public partial class TestMenuManager : MenuManager
{
    public Dictionary<string, Control> RegisteredMenus { get; } = new();
    public string? LastShownMenu { get; private set; }
    public static new TestMenuManager? Instance { get; set; }

    public TestMenuManager()
    {
        Name = "TestMenuManager";
        GD.Print("[TEST] TestMenuManager constructor called");
    }

    protected override void Initialize()
    {
        Instance = this;

        var baseInstanceProperty = typeof(MenuManager).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        baseInstanceProperty?.SetValue(null, this);

        GD.Print("[TEST] TestMenuManager initialized");
    }

    public override void RegisterMenu(string menuName, Control menuControl)
    {
        RegisteredMenus[menuName] = menuControl;
        GD.Print($"[TEST] TestMenuManager: Registered menu '{menuName}'");

        try
        {
            base.RegisterMenu(menuName, menuControl);
        }
        catch (System.Exception ex)
        {
            GD.Print($"[TEST] Exception in base.RegisterMenu: {ex.Message} - continuing with test");
        }
    }

    public override void ShowMenu(string menuName, bool addToHistory = true)
    {
        LastShownMenu = menuName;
        GD.Print($"[TEST] TestMenuManager: Showed menu '{menuName}'");

        try
        {
            base.ShowMenu(menuName, addToHistory);
        }
        catch (System.Exception ex)
        {
            GD.Print($"[TEST] Exception in base.ShowMenu: {ex.Message} - continuing with test");
        }
    }
}
