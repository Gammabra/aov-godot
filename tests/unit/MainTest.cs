using AshesOfVelsingrad.Managers;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
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
            
            // Réinitialiser les singletons avant chaque test
            SetSingletonInstance<SettingsManager>(null);
            SetSingletonInstance<MenuManager>(null);
            
            _testableMain = AutoFree(new TestableMain());
            // Passer le callback AutoFree à TestableMain
            if (_testableMain == null)
                throw new System.Exception("TestableMain instantiation failed");
            _testableMain.SetAutoFreeCallback(AutoFree);
            
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
        _testableMain.SetMenuContainer(_mockMenuContainer);

        // Act
        _testableMain.InitializeMenus();

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testableMain.OptionsMenuInstantiateCount).IsEqual(0);

        // Clean up immediately after test
        CleanupMenuContainer();
        _testableMain.Reset();
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuManager_DoesNotInitialize()
    {
        // Arrange
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(null);

        if (_testableMain == null)
            throw new System.Exception("TestableMain is null");
        _testableMain.SetMenuContainer(_mockMenuContainer);

        // Act
        _testableMain.InitializeMenus();

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testableMain.OptionsMenuInstantiateCount).IsEqual(0);

        // Clean up immediately after test
        _testableMain.Reset();
        CleanupMenuContainer();
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_InstantiatesAndAddsMenus()
    {
        // Arrange
        SetupValidManagers();

        if (_testableMain == null)
            throw new System.Exception("TestableMain is null");
        _testableMain.SetMenuContainer(_mockMenuContainer);

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

        // Clean up immediately after test - IMPORTANT: ordre inversé
        CleanupMenuContainer();
        _testableMain.Reset();
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuContainer_DoesNotInitialize()
    {
        // Arrange
        SetupValidManagers();

        if (_testableMain == null)
            throw new System.Exception("TestableMain is null");
        _testableMain.SetMenuContainer(null);

        // Act
        _testableMain.InitializeMenus();

        // Assert
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testableMain.OptionsMenuInstantiateCount).IsEqual(0);

        // Clean up immediately after test
        _testableMain.Reset();
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
        _testableMain.SetMenuContainer(_mockMenuContainer);

        // Act
        _testableMain.InitializeMenus();

        // Assert
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.MAIN_MENU)).IsTrue();
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.OPTIONS_MENU)).IsTrue();
        AssertThat(testMenuManager.LastShownMenu).IsEqual(MenuManager.MAIN_MENU);

        // Clean up immediately after test - IMPORTANT: ordre inversé
        CleanupMenuContainer();
        _testableMain.Reset();
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
        _testableMain.SetMenuContainer(_mockMenuContainer);

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
        
        // Clean up immediately after test - IMPORTANT: ordre inversé
        CleanupMenuContainer();
        _testableMain.Reset();
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
        
        GD.Print($"[TEST] Cleaning menu container with {_mockMenuContainer.GetChildCount()} children");
        
        // Créer une copie de la liste des enfants pour éviter les modifications concurrentes
        var children = new List<Node>();
        foreach (Node child in _mockMenuContainer.GetChildren())
        {
            children.Add(child);
        }

        // Retirer les enfants du container - AutoFree s'occupera de les libérer
        foreach (Node child in children)
        {
            if (GodotObject.IsInstanceValid(child) && !child.IsQueuedForDeletion())
            {
                _mockMenuContainer.RemoveChild(child);
                GD.Print($"[TEST] Removed from container: {child.Name}");
            }
        }
        
        GD.Print($"[TEST] Menu container cleanup completed. Remaining children: {_mockMenuContainer.GetChildCount()}");
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
        GD.Print("[TEST] Starting TearDown...");
        
        // 1. Nettoyer d'abord le menu container (retire les enfants)
        CleanupMenuContainer();
        
        // 2. Ensuite nettoyer la classe testable (libère les nœuds créés)
        _testableMain?.Reset();
        
        // 3. Nettoyer les nœuds de test (managers, etc.)
        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
                GD.Print($"[TEST] Freed test node: {node.Name}");
            }
        }
        _testNodes.Clear();
        
        // 4. Réinitialiser les singletons
        SetSingletonInstance<SettingsManager>(null);
        SetSingletonInstance<MenuManager>(null);
        
        GD.Print("[TEST] TearDown completed");
    }
}
