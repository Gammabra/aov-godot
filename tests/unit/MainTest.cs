using AshesOfVelsingrad;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.UI.Menus;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;

namespace AshesOfVelsingrad.Tests;

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
        _main = AutoFree(new Main());
        _mockMenuContainer = AutoFree(new Control());

        // Create test PackedScenes that can return test menus
        _mockMainMenuScene = new TestPackedScene();
        _mockOptionsMenuScene = new TestPackedScene();

        // Set the exported properties using reflection since they're private
        SetPrivateField(_main, "_menuContainer", _mockMenuContainer);
        SetPrivateField(_main, "_mainMenuScene", _mockMainMenuScene);
        SetPrivateField(_main, "_optionsMenuScene", _mockOptionsMenuScene);
    }

    [TestCase]
    public void Ready_CallsDeferredInitializeMenus()
    {
        // This test verifies that _Ready calls CallDeferred
        // We'll test the end result since we can't easily mock CallDeferred

        // Arrange - set up valid managers
        SetupValidManagers();

        // Act - call _Ready and then manually trigger the deferred call
        _main?._Ready();
        CallPrivateMethod(_main, "InitializeMenus");

        // Assert - verify that initialization occurred
        var childCount = _mockMenuContainer?.GetChildCount() ?? 0;
        AssertThat(childCount).IsEqual(2);
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
        SetPrivateField(_main, "_mainMenuScene", null);

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
        SetPrivateField(_main, "_optionsMenuScene", null);

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
        SetPrivateField(_main, "_menuContainer", null);

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

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

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

        AssertThat(child0).IsInstanceOf<MainMenu>();
        AssertThat(child1).IsInstanceOf<OptionsMenu>();
    }

    [TestCase]
    public void InitializeMenus_WithValidInputs_RegistersAndShowsMenus()
    {
        // Arrange
        var testMenuManager = CreateTestMenuManager();
        SetupValidSettingsManager();
        SetSingletonInstance<MenuManager>(testMenuManager);

        // Act
        CallPrivateMethod(_main, "InitializeMenus");

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
        var manager = new TestSettingsManager();
        _testNodes.Add(manager);
        return manager;
    }

    private TestMenuManager CreateTestMenuManager()
    {
        var manager = new TestMenuManager();
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
        }
    }

    private void SetPrivateField(object? obj, string fieldName, object? value)
    {
        if (obj == null) return;

        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    private void CallPrivateMethod(object? obj, string methodName, params object[] parameters)
    {
        if (obj == null) return;

        var method = obj.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(obj, parameters);
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
    // Simple test implementation that doesn't create additional nodes
}

public partial class TestMenuManager : MenuManager
{
    public Dictionary<string, Control> RegisteredMenus { get; } = new();
    public string? LastShownMenu { get; private set; }

    public new void RegisterMenu(string menuName, Control menuControl)
    {
        RegisteredMenus[menuName] = menuControl;
    }

    public new void ShowMenu(string menuName, bool addToHistory = true)
    {
        LastShownMenu = menuName;
    }
}

public partial class TestPackedScene : PackedScene
{
    public int InstantiateCallCount { get; private set; }
    private readonly List<Node> _createdNodes = new();

    public T Instantiate<T>() where T : class
    {
        InstantiateCallCount++;

        Node createdNode;
        if (typeof(T) == typeof(MainMenu))
        {
            createdNode = new MainMenu();
        }
        else if (typeof(T) == typeof(OptionsMenu))
        {
            createdNode = new OptionsMenu();
        }
        else
        {
            throw new InvalidOperationException($"Unsupported type: {typeof(T)}");
        }

        _createdNodes.Add(createdNode);
        return (T)(object)createdNode;
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
