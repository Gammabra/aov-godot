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
    private Main? _main;
    private Control? _mockMenuContainer;
    private TestSceneInstantiator? _testInstantiator;
    private readonly List<Node> _testNodes = new();

    [Before]
    public void SetUp()
    {
        try
        {
            GD.Print("Starting SetUp...");
            _main = AutoFree(new Main());
            _mockMenuContainer = AutoFree(new Control());
            _testInstantiator = new TestSceneInstantiator();
            
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
        _main.SetSceneInstantiator(_testInstantiator);
        SetPrivateField(_main, "_menuContainer", _mockMenuContainer);

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);
        AssertThat(_testInstantiator.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testInstantiator.OptionsMenuInstantiateCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuManager_DoesNotInitialize()
    {
        // Arrange
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(null);
        _main.SetSceneInstantiator(_testInstantiator);
        SetPrivateField(_main, "_menuContainer", _mockMenuContainer);

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);
        AssertThat(_testInstantiator.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testInstantiator.OptionsMenuInstantiateCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_InstantiatesAndAddsMenus()
    {
        // Arrange
        SetupValidManagers();
        
        var testInstantiator = new TestSceneInstantiator();
        _main.SetSceneInstantiator(testInstantiator);
        
        SetPrivateField(_main, "_menuContainer", _mockMenuContainer);

        GD.Print($"[TEST] Before call - Main instantiate count: {testInstantiator.MainMenuInstantiateCount}");
        GD.Print($"[TEST] Before call - Options instantiate count: {testInstantiator.OptionsMenuInstantiateCount}");

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        GD.Print($"[TEST] After call - Main instantiate count: {testInstantiator.MainMenuInstantiateCount}");
        GD.Print($"[TEST] After call - Options instantiate count: {testInstantiator.OptionsMenuInstantiateCount}");

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(2);
        AssertThat(testInstantiator.MainMenuInstantiateCount).IsEqual(1);
        AssertThat(testInstantiator.OptionsMenuInstantiateCount).IsEqual(1);

        // Clean up immédiatement après le test
        testInstantiator.FreeAllNodes();
        
        // Nettoyer aussi les enfants du container
        CleanupMenuContainer();
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuContainer_DoesNotInitialize()
    {
        // Arrange
        SetupValidManagers();
        _main.SetSceneInstantiator(_testInstantiator);
        SetPrivateField(_main, "_menuContainer", null);

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert
        AssertThat(_testInstantiator.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testInstantiator.OptionsMenuInstantiateCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_RegistersAndShowsMenus()
    {
        // Arrange
        var testMenuManager = CreateTestMenuManager();
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(testMenuManager);
        _main.SetSceneInstantiator(_testInstantiator);
        SetPrivateField(_main, "_menuContainer", _mockMenuContainer);

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.MAIN_MENU)).IsTrue();
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.OPTIONS_MENU)).IsTrue();
        AssertThat(testMenuManager.LastShownMenu).IsEqual(MenuManager.MAIN_MENU);

        // Clean up
        _testInstantiator.FreeAllNodes();
        CleanupMenuContainer();
    }

    [TestCase]
    public void InitializeMenus_Integration_FullWorkflowSuccess()
    {
        // Arrange
        var testMenuManager = CreateTestMenuManager();
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(testMenuManager);
        
        var testInstantiator = new TestSceneInstantiator();
        _main.SetSceneInstantiator(testInstantiator);
        SetPrivateField(_main, "_menuContainer", _mockMenuContainer);

        // Vérifier l'état initial
        var initialChildCount = _mockMenuContainer?.GetChildCount() ?? 0;
        GD.Print($"[TEST] Initial child count: {initialChildCount}");

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert - verify complete workflow
        AssertThat(testInstantiator.MainMenuInstantiateCount).IsEqual(1);
        AssertThat(testInstantiator.OptionsMenuInstantiateCount).IsEqual(1);
        
        var finalChildCount = _mockMenuContainer?.GetChildCount() ?? 0;
        GD.Print($"[TEST] Final child count: {finalChildCount}");
        AssertThat(finalChildCount).IsEqual(initialChildCount + 2); // Correction ici
        
        AssertThat(testMenuManager.RegisteredMenus.Count).IsEqual(2);
        AssertThat(testMenuManager.LastShownMenu).IsEqual(MenuManager.MAIN_MENU);
        
        // Clean up
        testInstantiator.FreeAllNodes();
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
        
        // Libérer tous les enfants du menu container
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

    private void SetPrivateField(object? obj, string fieldName, object? value)
    {
        if (obj == null) return;

        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
            GD.Print($"Successfully set field '{fieldName}' to {value?.GetType().Name ?? "null"}");
        }
        else
        {
            GD.PrintErr($"Field '{fieldName}' not found in {obj.GetType().Name}");
        }
    }

    private void CallPrivateMethod(object? obj, string methodName, params object[] parameters)
    {
        if (obj == null) 
        {
            GD.PrintErr("Object is null in CallPrivateMethod");
            return;
        }

        var method = obj.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null)
        {
            GD.PrintErr($"Method '{methodName}' not found in {obj.GetType().Name}");
            return;
        }

        GD.Print($"About to invoke method '{methodName}'");
        
        // Debug state before calling InitializeMenus
        if (methodName == "InitializeMenus")
        {
            var menuContainer = GetPrivateField(obj, "_menuContainer");
            var mainMenuScene = GetPrivateField(obj, "_mainMenuScene");
            var optionsMenuScene = GetPrivateField(obj, "_optionsMenuScene");
            
            GD.Print($"[TEST] _menuContainer is: {(menuContainer != null ? menuContainer.GetType().Name : "null")}");
            GD.Print($"[TEST] _mainMenuScene is: {(mainMenuScene != null ? mainMenuScene.GetType().Name : "null")}");
            GD.Print($"[TEST] _optionsMenuScene is: {(optionsMenuScene != null ? optionsMenuScene.GetType().Name : "null")}");
            GD.Print($"[TEST] SettingsManager.Instance is: {(SettingsManager.Instance != null ? "not null" : "null")}");
            GD.Print($"[TEST] MenuManager.Instance is: {(MenuManager.Instance != null ? "not null" : "null")}");
        }
        
        try
        {
            method.Invoke(obj, parameters);
            GD.Print($"Method '{methodName}' invoked successfully");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Exception invoking '{methodName}': {ex.Message}");
            if (ex.InnerException != null)
            {
                GD.PrintErr($"Inner exception: {ex.InnerException.Message}");
                GD.PrintErr($"Inner stack trace: {ex.InnerException.StackTrace}");
            }
            throw;
        }
    }

    private object? GetPrivateField(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(obj);
    }

    [After]
    public void TearDown()
    {
        // Nettoyer les nœuds de test d'abord
        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }
        }
        _testNodes.Clear();

        // Nettoyer l'instantiateur de test
        _testInstantiator?.FreeAllNodes();
        
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
