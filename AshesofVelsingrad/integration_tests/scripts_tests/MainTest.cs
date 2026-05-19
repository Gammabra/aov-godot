using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Helpers;
using AshesOfVelsingrad.Helpers.Managers;
using AshesOfVelsingrad.Managers;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests;

[TestSuite]
[RequireGodotRuntime]
public class MainTest
{
    private TestableMain? _testableMain;
    private Control? _mockMenuContainer;
    private readonly List<Node> _testNodes = new();

    [BeforeTest]
    public void SetUp()
    {
        GD.Print("[TEST] Starting Main SetUp...");

        // Clear singleton instances before each test
        ClearAllSingletonInstances();

        _testableMain = AutoFree(new TestableMain());
        if (_testableMain == null)
            throw new System.InvalidOperationException("Failed to create TestableMain instance");

        // Pass the AutoFree callback to TestableMain
        _testableMain.SetAutoFreeCallback(AutoFree);

        _mockMenuContainer = AutoFree(new Control { Name = "MockMenuContainer" });

        GD.Print("[TEST] Main SetUp completed");
    }

    [TestCase]
    public void InitializeMenus_WithNullSettingsManager_DoesNotInitialize()
    {
        // Arrange
        SetupValidMenuManager();
        _testableMain!.SetMenuContainer(_mockMenuContainer);

        // Act
        _testableMain.InitializeMenus();

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testableMain.SettingsInstantiateCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuManager_DoesNotInitialize()
    {
        // Arrange
        SetupValidSettingsManager();
        _testableMain!.SetMenuContainer(_mockMenuContainer);

        // Act
        _testableMain.InitializeMenus();

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(0);
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testableMain.SettingsInstantiateCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_InstantiatesAndAddsMenus()
    {
        // Arrange
        SetupValidManagers();
        _testableMain!.SetMenuContainer(_mockMenuContainer);

        GD.Print($"[TEST] Before InitializeMenus - Main: {_testableMain.MainMenuInstantiateCount}, Settings: {_testableMain.SettingsInstantiateCount}");

        // Act
        _testableMain.InitializeMenus();

        GD.Print($"[TEST] After InitializeMenus - Main: {_testableMain.MainMenuInstantiateCount}, Settings: {_testableMain.SettingsInstantiateCount}");

        // Assert
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(2);
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(1);
        AssertThat(_testableMain.SettingsInstantiateCount).IsEqual(1);
    }

    [TestCase]
    public void InitializeMenus_WithNullMenuContainer_DoesNotInitialize()
    {
        // Arrange
        SetupValidManagers();
        _testableMain!.SetMenuContainer(null);

        // Act
        _testableMain.InitializeMenus();

        // Assert
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(0);
        AssertThat(_testableMain.SettingsInstantiateCount).IsEqual(0);
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_RegistersAndShowsMenus()
    {
        // Arrange
        var testMenuManager = CreateTestMenuManager();
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(testMenuManager);
        _testableMain!.SetMenuContainer(_mockMenuContainer);

        // Act
        _testableMain.InitializeMenus();

        // Assert
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.MAIN_MENU)).IsTrue();
        AssertThat(testMenuManager.RegisteredMenus.ContainsKey(MenuManager.OPTIONS_MENU)).IsTrue();
        AssertThat(testMenuManager.LastShownMenu).IsEqual(MenuManager.MAIN_MENU);
    }

    [TestCase]
    public void InitializeMenus_Integration_FullWorkflowSuccess()
    {
        // Arrange
        var testMenuManager = CreateTestMenuManager();
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(testMenuManager);
        _testableMain!.SetMenuContainer(_mockMenuContainer);

        var initialChildCount = _mockMenuContainer?.GetChildCount() ?? 0;
        GD.Print($"[TEST] Initial child count: {initialChildCount}");

        // Act
        _testableMain.InitializeMenus();

        // Assert - verify complete workflow
        AssertThat(_testableMain.MainMenuInstantiateCount).IsEqual(1);
        AssertThat(_testableMain.SettingsInstantiateCount).IsEqual(1);

        var finalChildCount = _mockMenuContainer?.GetChildCount() ?? 0;
        GD.Print($"[TEST] Final child count: {finalChildCount}");
        AssertThat(finalChildCount).IsEqual(initialChildCount + 2);

        AssertThat(testMenuManager.RegisteredMenus.Count).IsEqual(2);
        AssertThat(testMenuManager.LastShownMenu).IsEqual(MenuManager.MAIN_MENU);
    }

    // Helper Methods
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
            throw new System.InvalidOperationException("Failed to create TestSettingsManager instance");

        _testNodes.Add(manager);

        // Initialize the manager properly
        var initializeMethod = typeof(TestSettingsManager).GetMethod("Initialize",
            BindingFlags.NonPublic | BindingFlags.Instance);
        initializeMethod?.Invoke(manager, null);

        return manager;
    }

    private TestMenuManager CreateTestMenuManager()
    {
        var manager = AutoFree(new TestMenuManager());
        if (manager == null)
            throw new System.InvalidOperationException("Failed to create TestMenuManager instance");

        _testNodes.Add(manager);

        // Initialize the manager properly
        var initializeMethod = typeof(TestMenuManager).GetMethod("Initialize",
            BindingFlags.NonPublic | BindingFlags.Instance);
        initializeMethod?.Invoke(manager, null);

        return manager;
    }

    private void CleanupMenuContainer()
    {
        if (_mockMenuContainer == null) return;

        GD.Print($"[TEST] Cleaning menu container with {_mockMenuContainer.GetChildCount()} children");

        // Create a copy of children list to avoid concurrent modification
        var children = new List<Node>();
        foreach (Node child in _mockMenuContainer.GetChildren())
        {
            children.Add(child);
        }

        // Remove children from container - AutoFree will handle cleanup
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
            BindingFlags.Public | BindingFlags.Static);

        if (instanceProperty != null)
        {
            instanceProperty.SetValue(null, instance);
            GD.Print($"[TEST] Set {typeof(T).Name}.Instance to {(instance != null ? "valid instance" : "null")}");
        }
        else
        {
            GD.PrintErr($"[TEST] Could not find Instance property on {typeof(T).Name}");
        }
    }

    private void ClearAllSingletonInstances()
    {
        // Clear SettingsManager.Instance
        SetSingletonInstance<SettingsManager>(null);

        // Clear MenuManager.Instance  
        SetSingletonInstance<MenuManager>(null);

        // Clear TestSettingsManager.Instance
        TestSettingsManager.Instance = null;

        // Clear TestMenuManager.Instance
        TestMenuManager.Instance = null;

        GD.Print($"[TEST] Cleared all singletons");
    }

    [AfterTest]
    public void TearDown()
    {
        GD.Print("[TEST] Starting Main TearDown...");

        // 1. Clean menu container first (removes children)
        CleanupMenuContainer();

        // 2. Reset testable main (frees created nodes)
        _testableMain?.Reset();

        // 3. Clean up test nodes (managers, etc.)
        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
                GD.Print($"[TEST] Freed test node: {node.Name}");
            }
        }
        _testNodes.Clear();

        // 4. Clear all singleton instances
        ClearAllSingletonInstances();

        // 5. Clean up temp files from TestSettingsManager
        TestSettingsManager.ClearTempFiles();

        GD.Print("[TEST] Main TearDown completed");
    }
}
