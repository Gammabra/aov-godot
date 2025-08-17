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
    private TestPackedScene? _mockMainMenuScene;
    private TestPackedScene? _mockOptionsMenuScene;
    private readonly List<Node> _testNodes = new();

    [Before]
    public void SetUp()
    {
        try
        {
            GD.Print("Starting SetUp...");
            _main = AutoFree(new Main());
            GD.Print("Main created");
            
            _mockMenuContainer = AutoFree(new Control());
            GD.Print("Menu container created");

            // Create test PackedScenes with specific types
            _mockMainMenuScene = new TestPackedScene("mainmenu");
            _mockOptionsMenuScene = new TestPackedScene("optionsmenu");
            GD.Print("Test PackedScenes created");

            // Set the exported properties using reflection since they're private
            SetPrivateField(_main, "_menuContainer", _mockMenuContainer);
            SetPrivateField(_main, "_mainMenuScene", _mockMainMenuScene);
            SetPrivateField(_main, "_optionsMenuScene", _mockOptionsMenuScene);
            GD.Print("SetUp completed successfully");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Exception in SetUp: {ex.Message}");
            GD.PrintErr($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [TestCase]
    public void InitializeMenus_WithNullSettingsManager_DoesNotInitialize()
    {
        // Arrange
        SetSingletonInstance<SettingsManager>(null);
        SetupValidMenuManager();

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert - verify that no menus were added to container
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);

        // Verify no instantiation occurred
        var mainCallCount = _mockMainMenuScene?.InstantiateCallCount ?? 0;
        var optionsCallCount = _mockOptionsMenuScene?.InstantiateCallCount ?? 0;
        AssertThat(mainCallCount).IsEqual(0);
        AssertThat(optionsCallCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuManager_DoesNotInitialize()
    {
        // Arrange
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(null);

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert - verify that no menus were added to container
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);

        // Verify no instantiation occurred
        var mainCallCount = _mockMainMenuScene?.InstantiateCallCount ?? 0;
        var optionsCallCount = _mockOptionsMenuScene?.InstantiateCallCount ?? 0;
        AssertThat(mainCallCount).IsEqual(0);
        AssertThat(optionsCallCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithNullMainMenuScene_DoesNotInitialize()
    {
        // Arrange
        SetupValidManagers();
        EnsureFieldsAreSet(); // Ensure all fields are set first
        SetPrivateField(_main, "_mainMenuScene", null); // Then set only main to null

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);

        // Only options scene should not be called (main is null)
        var optionsCallCount = _mockOptionsMenuScene?.InstantiateCallCount ?? 0;
        AssertThat(optionsCallCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithNullOptionsMenuScene_DoesNotInitialize()
    {
        // Arrange
        SetupValidManagers();
        EnsureFieldsAreSet(); // Ensure all fields are set first
        SetPrivateField(_main, "_optionsMenuScene", null); // Then set only options to null

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);

        // Only main scene should not be called (options is null)
        var mainCallCount = _mockMainMenuScene?.InstantiateCallCount ?? 0;
        AssertThat(mainCallCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuContainer_DoesNotInitialize()
    {
        // Arrange
        SetupValidManagers();
        EnsureFieldsAreSet(); // Ensure all fields are set first
        SetPrivateField(_main, "_menuContainer", null); // Then set only container to null

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert - verify that the PackedScenes weren't called
        var mainCallCount = _mockMainMenuScene?.InstantiateCallCount ?? 0;
        var optionsCallCount = _mockOptionsMenuScene?.InstantiateCallCount ?? 0;
        AssertThat(mainCallCount).IsEqual(0);
        AssertThat(optionsCallCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_InstantiatesAndAddsMenus()
    {
        // Arrange
        SetupValidManagers();
        EnsureFieldsAreSet();

        GD.Print($"[TEST] Before call - Main instantiate count: {_mockMainMenuScene?.InstantiateCallCount ?? -1}");
        GD.Print($"[TEST] Before call - Options instantiate count: {_mockOptionsMenuScene?.InstantiateCallCount ?? -1}");
        GD.Print($"[TEST] Before call - Menu container child count: {_mockMenuContainer?.GetChildCount() ?? -1}");

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        GD.Print($"[TEST] After call - Main instantiate count: {_mockMainMenuScene?.InstantiateCallCount ?? -1}");
        GD.Print($"[TEST] After call - Options instantiate count: {_mockOptionsMenuScene?.InstantiateCallCount ?? -1}");
        GD.Print($"[TEST] After call - Menu container child count: {_mockMenuContainer?.GetChildCount() ?? -1}");

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(2);

        // Verify the correct menus were instantiated from the PackedScenes
        var mainCallCount = _mockMainMenuScene?.InstantiateCallCount ?? 0;
        var optionsCallCount = _mockOptionsMenuScene?.InstantiateCallCount ?? 0;
        AssertThat(mainCallCount).IsEqual(1);
        AssertThat(optionsCallCount).IsEqual(1);

        // Verify menus were added as children and are the correct types
        var child0 = _mockMenuContainer?.GetChild(0);
        var child1 = _mockMenuContainer?.GetChild(1);

        AssertThat(child0).IsNotNull();
        AssertThat(child1).IsNotNull();
        AssertThat(child0).IsInstanceOf<Control>();
        AssertThat(child1).IsInstanceOf<Control>();
        
        GD.Print($"[TEST] Child 0 type: {child0?.GetType().Name}, name: {child0?.Name}");
        GD.Print($"[TEST] Child 1 type: {child1?.GetType().Name}, name: {child1?.Name}");
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_RegistersAndShowsMenus()
    {
        // Arrange
        var testMenuManager = CreateTestMenuManager();
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(testMenuManager);
        EnsureFieldsAreSet();

        GD.Print($"Before call - Main instantiate count: {_mockMainMenuScene?.InstantiateCallCount ?? -1}");
        GD.Print($"Before call - Options instantiate count: {_mockOptionsMenuScene?.InstantiateCallCount ?? -1}");

        // Act
        try
        {
            CallPrivateMethod(_main, "InitializeMenus");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Exception during InitializeMenus: {ex.Message}");
            GD.PrintErr($"Stack trace: {ex.StackTrace}");
            throw;
        }

        GD.Print($"After call - Main instantiate count: {_mockMainMenuScene?.InstantiateCallCount ?? -1}");
        GD.Print($"After call - Options instantiate count: {_mockOptionsMenuScene?.InstantiateCallCount ?? -1}");
        GD.Print($"Menu container child count: {_mockMenuContainer?.GetChildCount() ?? -1}");
        GD.Print($"Registered menus count: {testMenuManager.RegisteredMenus.Count}");

        // Assert - verify menus were registered
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.MAIN_MENU)).IsTrue();
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.OPTIONS_MENU)).IsTrue();

        // Verify ShowMenu was called
        AssertThat(testMenuManager.LastShownMenu).IsEqual(MenuManager.MAIN_MENU);
    }

    [TestCase]
    public void InitializeMenus_Integration_FullWorkflowSuccess()
    {
        // Arrange
        var testMenuManager = CreateTestMenuManager();
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(testMenuManager);
        EnsureFieldsAreSet();

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert - verify the complete successful workflow
        // 1. Both PackedScenes instantiated
        var mainCallCount = _mockMainMenuScene?.InstantiateCallCount ?? 0;
        var optionsCallCount = _mockOptionsMenuScene?.InstantiateCallCount ?? 0;
        AssertThat(mainCallCount).IsEqual(1);
        AssertThat(optionsCallCount).IsEqual(1);

        // 2. Both menus added to container
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(2);

        // 3. Both menus registered with MenuManager
        AssertThat(testMenuManager.RegisteredMenus.Count).IsEqual(2);
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.MAIN_MENU)).IsTrue();
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.OPTIONS_MENU)).IsTrue();

        // 4. Main menu shown
        AssertThat(testMenuManager.LastShownMenu).IsEqual(MenuManager.MAIN_MENU);
    }

    // Helper methods
    private void SetupValidManagers()
    {
        SetupValidSettingsManager();
        SetupValidMenuManager();
    }

    private void EnsureFieldsAreSet()
    {
        SetPrivateField(_main, "_menuContainer", _mockMenuContainer);
        SetPrivateField(_main, "_mainMenuScene", _mockMainMenuScene);
        SetPrivateField(_main, "_optionsMenuScene", _mockOptionsMenuScene);
        GD.Print("[TEST] Fields re-ensured in EnsureFieldsAreSet()");
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
            
            // Test the condition manually
            bool condition = (mainMenuScene != null) && (optionsMenuScene != null) && (menuContainer != null);
            GD.Print($"[TEST] Combined condition (_mainMenuScene != null && _optionsMenuScene != null && _menuContainer != null) evaluates to: {condition}");
        }
        
        try
        {
            method.Invoke(obj, parameters);
            GD.Print($"Method '{methodName}' invoked successfully");
            
            // Add post-invocation debugging for InitializeMenus
            if (methodName == "InitializeMenus")
            {
                GD.Print($"[TEST] After InitializeMenus - Main instantiate count: {_mockMainMenuScene?.InstantiateCallCount ?? -1}");
                GD.Print($"[TEST] After InitializeMenus - Options instantiate count: {_mockOptionsMenuScene?.InstantiateCallCount ?? -1}");
                GD.Print($"[TEST] After InitializeMenus - Menu container child count: {_mockMenuContainer?.GetChildCount() ?? -1}");
            }
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
        // Clean up all manually created test nodes
        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }
        }
        _testNodes.Clear();

        // Clean up nodes created by TestPackedScenes
        _mockMainMenuScene?.FreeAllNodes();
        _mockOptionsMenuScene?.FreeAllNodes();

        // Clean up singleton instances
        SetSingletonInstance<SettingsManager>(null);
        SetSingletonInstance<MenuManager>(null);
    }
}

// Test helper classes
public partial class TestSettingsManager : SettingsManager
{
    public static new TestSettingsManager? Instance { get; set; }

    public TestSettingsManager()
    {
        Name = "TestSettingsManager";
        GD.Print("[TEST] TestSettingsManager constructor called");
        
        // Initialiser les propriétés de base pour éviter les NullReferenceException
        InitializeForTesting();
    }

    protected override void Initialize()
    {
        Instance = this;
        
        // Set the base class Instance using reflection
        var baseInstanceProperty = typeof(SettingsManager).GetProperty("Instance", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        baseInstanceProperty?.SetValue(null, this);
        
        InitializeForTesting();
        
        GD.Print("[TEST] TestSettingsManager initialized");
    }

    private void InitializeForTesting()
    {
        // Créer des settings par défaut pour les tests
        var settingsField = typeof(SettingsManager).GetField("_settings", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (settingsField != null)
        {
            var defaultSettings = new SettingsData();
            settingsField.SetValue(this, defaultSettings);
            GD.Print("[TEST] Default settings initialized for TestSettingsManager");
        }
    }

    // Override les méthodes virtuelles au lieu d'utiliser new
    public override void LoadSettings()
    {
        GD.Print("[TEST] TestSettingsManager.LoadSettings() called - using default settings");
        // Ne pas charger depuis un fichier, utiliser les defaults
        InitializeForTesting();
    }

    public override void SaveSettings()
    {
        GD.Print("[TEST] TestSettingsManager.SaveSettings() called - not saving to file in tests");
        // Ne pas sauvegarder dans un fichier pendant les tests
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
        
        // Set the base class Instance using reflection
        var baseInstanceProperty = typeof(MenuManager).GetProperty("Instance", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        baseInstanceProperty?.SetValue(null, this);
        
        GD.Print("[TEST] TestMenuManager initialized");
    }

    public override void RegisterMenu(string menuName, Control menuControl)
    {
        RegisteredMenus[menuName] = menuControl;
        GD.Print($"[TEST] TestMenuManager: Registered menu '{menuName}'");
        
        // Appeler la méthode de base pour garder la logique normale
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
        
        // Appeler la méthode de base pour garder la logique normale
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

// TestPackedScene avec approche simple pour éviter les problèmes de cast
public partial class TestPackedScene : PackedScene
{
    public int InstantiateCallCount { get; private set; }
    private readonly List<Node> _createdNodes = new();
    private readonly string _sceneType;

    public TestPackedScene(string sceneType = "")
    {
        _sceneType = sceneType;
        GD.Print($"[TEST] TestPackedScene created for type '{_sceneType}'");
    }

    // Override la méthode générique avec approche simple
    public T Instantiate<T>() where T : Node
    {
        try
        {
            InstantiateCallCount++;
            GD.Print($"[TEST] TestPackedScene.Instantiate<{typeof(T).Name}>() called. Call count: {InstantiateCallCount}");

            // Créer des Control simples pour éviter tous les problèmes de dépendances
            Node createdNode = new Control();

            if (typeof(T).Name == "MainMenu" || _sceneType == "mainmenu")
            {
                createdNode.Name = "MainMenu";
            }
            else if (typeof(T).Name == "OptionsMenu" || _sceneType == "optionsmenu")
            {
                createdNode.Name = "OptionsMenu";
            }
            else
            {
                createdNode.Name = $"Test{typeof(T).Name}";
            }

            _createdNodes.Add(createdNode);
            GD.Print($"[TEST] Created simple Control '{createdNode.Name}' as {typeof(T).Name} mock");

            // Cast vers T - cela marchera tant que T hérite de Node/Control
            return (T)createdNode;
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[TEST] Exception in Instantiate: {ex.Message}");
            throw;
        }
    }

    // Les autres overloads pour assurer la compatibilité
    public new Node Instantiate(PackedScene.GenEditState editState = PackedScene.GenEditState.Disabled)
    {
        InstantiateCallCount++;
        GD.Print($"[TEST] TestPackedScene.Instantiate() called for scene type '{_sceneType}'. Call count: {InstantiateCallCount}");

        var createdNode = new Control();
        createdNode.Name = $"Test{_sceneType}";
        _createdNodes.Add(createdNode);
        return createdNode;
    }

    public void FreeAllNodes()
    {
        foreach (var node in _createdNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }
        }
        _createdNodes.Clear();
    }
}
