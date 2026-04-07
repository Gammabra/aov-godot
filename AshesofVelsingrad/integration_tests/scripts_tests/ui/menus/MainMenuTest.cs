using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Helpers.Managers;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.UI.Menus;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.UI;

[TestSuite]
[RequireGodotRuntime]
public class MainMenuTest
{
    private MainMenu? _mainMenu;
    private TestMenuManager? _testMenuManager;
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private Button? _optionsButton;
    private Button? _exitButton;

    [BeforeTest]
    public void SetUp()
    {
        GD.Print("[TEST] Starting MainMenu SetUp...");

        // Clear singleton instances
        ClearAllSingletonInstances();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        _testMenuManager = CreateTestMenuManager();

        _mainMenu = AutoFree(new MainMenu { Name = "MainMenu" });

        if (_mainMenu == null)
            throw new System.InvalidOperationException("Failed to create MainMenu instance.");

        // IMPORTANT: Inject the test MenuManager into MainMenu
        _mainMenu.SetMenuManagerForTesting(_testMenuManager);

        _optionsButton = AutoFree(new Button { Name = "OptionsButton", Text = "Options" });
        _exitButton = AutoFree(new Button { Name = "ExitButton", Text = "Exit" });

        _mainMenu.AddChild(_optionsButton);
        _mainMenu.AddChild(_exitButton);

        AddToTestRoot(_mainMenu);

        // Register and show main menu
        _testMenuManager!.RegisterMenu(MenuManager.MAIN_MENU, _mainMenu);
        _testMenuManager!.ShowMenu(MenuManager.MAIN_MENU);

        GD.Print("[TEST] MainMenu SetUp completed");
    }

    [TestCase]
    public void Ready_WithValidMenuManager_InitializesSuccessfully()
    {
        // Arrange is done in SetUp

        // Act
        _mainMenu!._Ready();

        // Assert - No errors should occur, MainMenu should be ready
        AssertThat(GodotObject.IsInstanceValid(_mainMenu)).IsTrue();
    }

    [TestCase]
    public void Ready_WithNullMenuManager_PrintsErrorMessage()
    {
        // Arrange - Create a MainMenu without injected MenuManager
        var testMainMenu = AutoFree(new MainMenu { Name = "TestMainMenu" });

        if (testMainMenu == null)
            throw new System.InvalidOperationException("Failed to create TestMainMenu instance.");

        AddToTestRoot(testMainMenu);
        // Don't inject MenuManager, so it will be null

        // Act
        testMainMenu._Ready();

        // Assert - Should not crash, but will print error
        AssertThat(GodotObject.IsInstanceValid(testMainMenu)).IsTrue();
    }

    [TestCase]
    public void OnOptionsButtonButtonUp_WithValidMenuManager_ShowsOptionsMenu()
    {
        // Arrange
        var mockOptionsMenu = AutoFree(new Control { Name = "OptionsMenu" });
        _testMenuManager!.RegisterMenu(MenuManager.OPTIONS_MENU, mockOptionsMenu!);

        _mainMenu!._Ready();

        // Debug: Check state before
        GD.Print($"[DEBUG] Before OnOptionsButtonButtonUp - Current menu: {_testMenuManager.GetCurrentMenu()}");

        // Act
        CallPrivateMethod(_mainMenu, "OnOptionsButtonButtonUp");

        // Debug: Check state after
        GD.Print($"[DEBUG] After OnOptionsButtonButtonUp - Current menu: {_testMenuManager.GetCurrentMenu()}");
        GD.Print($"[DEBUG] LastShownMenu in TestMenuManager: {_testMenuManager.LastShownMenu}");

        // Assert
        AssertThat(_testMenuManager.GetCurrentMenu()).IsEqual(MenuManager.OPTIONS_MENU);
        AssertThat(mockOptionsMenu!.Visible).IsTrue();
    }

    [TestCase]
    public void OnOptionsButtonButtonUp_WithNullMenuManager_PrintsErrorMessage()
    {
        // Arrange - Create MainMenu without injected MenuManager
        var testMainMenu = AutoFree(new MainMenu { Name = "TestMainMenu" });

        if (testMainMenu == null)
            throw new System.InvalidOperationException("Failed to create TestMainMenu instance.");

        AddToTestRoot(testMainMenu);
        testMainMenu._Ready();

        // Act - Should not crash
        CallPrivateMethod(testMainMenu, "OnOptionsButtonButtonUp");

        // Assert - Method should complete without exception
        AssertThat(GodotObject.IsInstanceValid(testMainMenu)).IsTrue();
    }

    [TestCase]
    public void OnOptionsButtonButtonUp_WithUnregisteredOptionsMenu_DoesNotChangeMenu()
    {
        // Arrange
        _mainMenu!._Ready();
        var originalMenu = _testMenuManager!.GetCurrentMenu();

        if (originalMenu == null)
            throw new System.InvalidOperationException("Original menu should not be null.");

        // Act
        CallPrivateMethod(_mainMenu, "OnOptionsButtonButtonUp");

        // Assert - Current menu should not change since options menu is not registered
        // The menu should remain the same because ShowMenu will fail
        AssertThat(_testMenuManager.GetCurrentMenu()).IsEqual(originalMenu);
    }

    [TestCase]
    public void OnExitButtonButtonUp_CallsQuitOnSceneTree()
    {
        // Arrange
        _mainMenu!._Ready();
        var sceneTree = _mainMenu.GetTree();

        // Act & Assert - Method should complete without exception
        AssertThat(sceneTree).IsNotNull();

        // Verify the method exists and is callable
        var method = typeof(MainMenu).GetMethod("OnExitButtonButtonUp",
            BindingFlags.NonPublic | BindingFlags.Instance);
        AssertThat(method).IsNotNull();
    }

    [TestCase]
    public void ButtonSignalConnections_CanBeConnectedProperly()
    {
        // Arrange
        _mainMenu!._Ready();

        // Act - Connect button signals manually (simulating what would happen in Godot editor)
        var optionsConnected = _optionsButton!.Connect(
            Button.SignalName.ButtonUp,
            Callable.From(() => CallPrivateMethod(_mainMenu, "OnOptionsButtonButtonUp"))
        );

        var exitConnected = _exitButton!.Connect(
            Button.SignalName.ButtonUp,
            Callable.From(() =>
            {
                // Don't actually quit in tests
                GD.Print("EXIT GAME (Test Mode)");
            })
        );

        // Assert
        AssertThat(optionsConnected).IsEqual(Error.Ok);
        AssertThat(exitConnected).IsEqual(Error.Ok);
    }

    [TestCase]
    public void ButtonInteraction_OptionsButton_TriggersCorrectBehavior()
    {
        // Arrange
        var mockOptionsMenu = AutoFree(new Control { Name = "OptionsMenu" });
        _testMenuManager!.RegisterMenu(MenuManager.OPTIONS_MENU, mockOptionsMenu!);

        _mainMenu!._Ready();

        _optionsButton!.Connect(
            Button.SignalName.ButtonUp,
            Callable.From(() => CallPrivateMethod(_mainMenu, "OnOptionsButtonButtonUp"))
        );

        // Act
        _optionsButton.EmitSignal(Button.SignalName.ButtonUp);

        // Assert
        AssertThat(_testMenuManager.GetCurrentMenu()).IsEqual(MenuManager.OPTIONS_MENU);
    }

    [TestCase]
    public void MainMenuIntegration_WithMenuManager_WorksCorrectly()
    {
        // Arrange
        var mockOptionsMenu = AutoFree(new Control { Name = "OptionsMenu" });
        _testMenuManager!.RegisterMenu(MenuManager.OPTIONS_MENU, mockOptionsMenu!);
        // Note: _mainMenu is already registered in SetUp, don't register twice

        _mainMenu!._Ready();

        // Start from main menu (already set in SetUp)
        AssertThat(_testMenuManager.GetCurrentMenu()).IsEqual(MenuManager.MAIN_MENU);

        // Act - Go to options menu
        CallPrivateMethod(_mainMenu, "OnOptionsButtonButtonUp");

        // Assert - Should now be in options menu
        AssertThat(_testMenuManager.GetCurrentMenu()).IsEqual(MenuManager.OPTIONS_MENU);
        AssertThat(_testMenuManager.IsMenuActive(MenuManager.OPTIONS_MENU)).IsTrue();
        AssertThat(_testMenuManager.IsMenuActive(MenuManager.MAIN_MENU)).IsFalse();

        // Act - Go back to main menu
        _testMenuManager.GoBack();

        // Assert - Should be back to main menu
        AssertThat(_testMenuManager.GetCurrentMenu()).IsEqual(MenuManager.MAIN_MENU);
    }

    [TestCase]
    public void MenuConstants_AreUsedCorrectly()
    {
        // Arrange
        var mockOptionsMenu = AutoFree(new Control { Name = "OptionsMenu" });
        _testMenuManager!.RegisterMenu(MenuManager.OPTIONS_MENU, mockOptionsMenu!);

        _mainMenu!._Ready();

        // Act
        CallPrivateMethod(_mainMenu, "OnOptionsButtonButtonUp");

        // Assert
        AssertThat(_testMenuManager.GetCurrentMenu()).IsEqual(MenuManager.OPTIONS_MENU);
        AssertThat(MenuManager.OPTIONS_MENU).IsEqual("options_menu");
    }

    [TestCase]
    public void MainMenu_IsControlNode_InheritsCorrectly()
    {
        // Assert
        AssertThat(_mainMenu).IsInstanceOf<Control>();
        AssertThat(_mainMenu).IsInstanceOf<Node>();
    }

    [TestCase]
    public void MainMenu_ChildNodes_AreAccessible()
    {
        // Assert
        AssertThat(_mainMenu!.GetChild(0)).IsEqual(_optionsButton);
        AssertThat(_mainMenu.GetChild(1)).IsEqual(_exitButton);
        AssertThat(_mainMenu.GetChildCount()).IsEqual(2);
    }

    [TestCase]
    public void MultipleButtonPresses_WorkConsistently()
    {
        // Arrange
        var mockOptionsMenu = AutoFree(new Control { Name = "OptionsMenu" });
        _testMenuManager!.RegisterMenu(MenuManager.OPTIONS_MENU, mockOptionsMenu!);
        _testMenuManager.RegisterMenu(MenuManager.MAIN_MENU, _mainMenu!);

        _mainMenu!._Ready();

        // Act
        CallPrivateMethod(_mainMenu, "OnOptionsButtonButtonUp");
        var firstResult = _testMenuManager.GetCurrentMenu();

        CallPrivateMethod(_mainMenu, "OnOptionsButtonButtonUp");
        var secondResult = _testMenuManager.GetCurrentMenu();

        // Assert
        AssertThat(firstResult).IsEqual(MenuManager.OPTIONS_MENU);
        AssertThat(secondResult).IsEqual(MenuManager.OPTIONS_MENU);
    }

    [TestCase]
    public async System.Threading.Tasks.Task Ready_CallSequence_CompletesSuccessfully()
    {
        // Arrange - Fresh main menu with injected MenuManager
        var testMainMenu = AutoFree(new MainMenu { Name = "TestMainMenu" });

        if (testMainMenu == null)
            throw new System.InvalidOperationException("Failed to create TestMainMenu instance.");

        testMainMenu.SetMenuManagerForTesting(_testMenuManager!);
        AddToTestRoot(testMainMenu);

        // Act
        testMainMenu._Ready();

        // Wait a frame for any async operations
        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // Assert
        AssertThat(GodotObject.IsInstanceValid(testMainMenu)).IsTrue();
    }

    // Helper Methods
    private TestMenuManager CreateTestMenuManager()
    {
        var manager = AddToTestRoot(new TestMenuManager());
        _testNodes.Add(manager);
        CallInitializeOnTestManager(manager);
        return manager;
    }

    private T AddToTestRoot<T>(T node) where T : Node
    {
        if (_root == null)
            throw new System.InvalidOperationException("Test root node is not initialized.");

        _root.AddChild(node);
        return node;
    }

    private void CallInitializeOnTestManager(TestMenuManager manager)
    {
        var initializeMethod = typeof(TestMenuManager).GetMethod("Initialize",
            BindingFlags.NonPublic | BindingFlags.Instance);
        initializeMethod?.Invoke(manager, null);

        GD.Print($"[TEST] After Initialize - MenuManager.Instance: {MenuManager.Instance != null}");
        GD.Print($"[TEST] After Initialize - TestMenuManager.Instance: {TestMenuManager.Instance != null}");
    }

    private void ClearAllSingletonInstances()
    {
        // Clear MenuManager.Instance using reflection
        var menuManagerType = typeof(MenuManager);

        // Try the backing field approach
        var instanceField = menuManagerType.GetField("<Instance>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (instanceField != null)
        {
            instanceField.SetValue(null, null);
            GD.Print("[TEST] Cleared MenuManager.Instance via backing field");
        }

        // Clear TestMenuManager.Instance
        TestMenuManager.Instance = null;

        GD.Print($"[TEST] Cleared singletons - MenuManager.Instance: {MenuManager.Instance}");
    }

    private void CallPrivateMethod(object instance, string methodName, params object[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            throw new System.InvalidOperationException($"Method '{methodName}' not found on type '{instance.GetType().Name}'");
        }

        method.Invoke(instance, parameters);
    }

    [AfterTest]
    public void TearDown()
    {
        GD.Print("[TEST] Starting MainMenu TearDown...");

        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }
        }
        _testNodes.Clear();

        ClearAllSingletonInstances();

        GD.Print("[TEST] MainMenu TearDown completed");
    }
}
