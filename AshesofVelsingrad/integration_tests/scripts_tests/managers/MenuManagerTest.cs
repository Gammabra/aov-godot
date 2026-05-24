using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Managers;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Managers;

[TestSuite]
[RequireGodotRuntime]
public class MenuManagerTest
{
    private MenuManager? _menuManager;
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private Control? _mockMenu1;
    private Control? _mockMenu2;

    [BeforeTest]
    public void SetUp()
    {
        GD.Print("[TEST] Starting MenuManager SetUp...");

        // Reset singleton instance
        SetSingletonInstance<MenuManager>(null);

        // Create test root
        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        // Create test menus
        _mockMenu1 = AutoFree(new Control { Name = "MockMenu1" });
        _mockMenu2 = AutoFree(new Control { Name = "MockMenu2" });

        GD.Print("[TEST] MenuManager SetUp completed");
    }

    [TestCase]
    public void Initialize_FirstInstance_SetsInstanceCorrectly()
    {
        // Arrange & Act
        _menuManager = CreateMenuManager();

        // Assert
        AssertThat(MenuManager.Instance).IsEqual(_menuManager);
        AssertThat(MenuManager.Instance).IsNotNull();
    }

    [TestCase]
    public void Initialize_SecondInstance_RemovesDuplicate()
    {
        // Arrange
        var firstManager = CreateMenuManager();

        // Act - create second instance
        var secondManager = AddToTestRoot(new MenuManager());
        _testNodes.Add(secondManager);
        CallInitializeOnManager(secondManager);

        // Assert
        AssertThat(MenuManager.Instance).IsEqual(firstManager);
        AssertThat(secondManager.IsQueuedForDeletion()).IsTrue();
    }

    [TestCase]
    public void RegisterMenu_ValidMenu_AddsToCollection()
    {
        // Arrange
        _menuManager = CreateMenuManager();

        // Act
        _menuManager.RegisterMenu("test_menu", _mockMenu1!);

        // Assert
        AssertThat(_menuManager.IsMenuActive("test_menu")).IsFalse(); // Not shown yet
        AssertThat(_mockMenu1!.Visible).IsFalse(); // Should be hidden by default
    }

    [TestCase]
    public void RegisterMenu_DuplicateMenu_DoesNotAddDuplicate()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("test_menu", _mockMenu1!);

        // Act - try to register the same menu name again with different control
        _menuManager.RegisterMenu("test_menu", _mockMenu2!);

        // Assert - the original menu should still be the active one when shown
        _menuManager.ShowMenu("test_menu");
        AssertThat(_mockMenu1!.Visible).IsTrue();
        // Note: _mockMenu2 might be visible if it was added to the scene tree elsewhere
        // The key test is that _mockMenu1 is the one that responds to the "test_menu" key
    }

    [TestCase]
    public void ShowMenu_ValidMenu_ShowsAndHidesPrevious()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("menu1", _mockMenu1!);
        _menuManager.RegisterMenu("menu2", _mockMenu2!);

        _menuManager.ShowMenu("menu1");
        AssertThat(_mockMenu1!.Visible).IsTrue();
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu1");

        // Act
        _menuManager.ShowMenu("menu2");

        // Assert — plain Controls use Hide()/Show(), not SettingsPages.HideAll/ShowAll
        AssertThat(_mockMenu1.Visible).IsFalse();
        AssertThat(_mockMenu2!.Visible).IsTrue();
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu2");
        // MouseFilter should be set to Ignore when hidden
        AssertThat((int)_mockMenu1.MouseFilter).IsEqual((int)Control.MouseFilterEnum.Ignore);
    }

    [TestCase]
    public void ShowMenu_OptionsMenu_UsesShowMenuMethod()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu(MenuManager.OPTIONS_MENU, _mockMenu1!);

        // Act
        _menuManager.ShowMenu(MenuManager.OPTIONS_MENU);

        // Assert — options menu is shown like any other menu now
        // (no special OptionsMenu type handling)
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual(MenuManager.OPTIONS_MENU);
        AssertThat(_mockMenu1!.Visible).IsTrue();
    }

    [TestCase]
    public void ShowMenu_NonExistentMenu_DoesNotChangeCurrentMenu()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("existing_menu", _mockMenu1!);
        _menuManager.ShowMenu("existing_menu");

        var originalMenu = _menuManager.GetCurrentMenu();

        // Act
        _menuManager.ShowMenu("non_existent");

        // Assert
        if (originalMenu == null)
        {
            AssertThat(_menuManager.GetCurrentMenu()).IsNull();
            return;
        }
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual(originalMenu);
        AssertThat(_mockMenu1!.Visible).IsTrue(); // Original menu should still be visible
    }

    [TestCase]
    public void ShowMenu_WithHistory_AddsToHistoryStack()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("menu1", _mockMenu1!);
        _menuManager.RegisterMenu("menu2", _mockMenu2!);

        _menuManager.ShowMenu("menu1");

        // Act
        _menuManager.ShowMenu("menu2", true);

        // Assert
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu2");

        // Test going back
        _menuManager.GoBack();
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu1");
    }

    [TestCase]
    public void ShowMenu_WithoutHistory_DoesNotAddToHistoryStack()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("menu1", _mockMenu1!);
        _menuManager.RegisterMenu("menu2", _mockMenu2!);

        _menuManager.ShowMenu("menu1");

        // Act
        _menuManager.ShowMenu("menu2", false);
        _menuManager.GoBack(); // Should have no effect

        // Assert
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu2");
    }

    [TestCase]
    public void GoBack_WithHistory_NavigatesToPreviousMenu()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("menu1", _mockMenu1!);
        _menuManager.RegisterMenu("menu2", _mockMenu2!);

        _menuManager.ShowMenu("menu1");
        _menuManager.ShowMenu("menu2");

        // Act
        _menuManager.GoBack();

        // Assert
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu1");
        AssertThat(_mockMenu1!.Visible).IsTrue();
        AssertThat(_mockMenu2!.Visible).IsFalse();
    }

    [TestCase]
    public void GoBack_WithoutHistory_DoesNotChangeMenu()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("test_menu", _mockMenu1!);
        _menuManager.ShowMenu("test_menu");
        _menuManager.ClearHistory(); // Ensure no history

        var originalMenu = _menuManager.GetCurrentMenu();

        // Act
        _menuManager.GoBack();

        // Assert
        if (originalMenu == null)
        {
            AssertThat(_menuManager.GetCurrentMenu()).IsNull();
            return;
        }
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual(originalMenu);
        AssertThat(_mockMenu1!.Visible).IsTrue(); // Menu should still be visible
    }

    [TestCase]
    public void GoBack_MultipleMenus_NavigatesInCorrectOrder()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        var menu3 = AutoFree(new Control { Name = "MockMenu3" });

        if (menu3 == null)
            throw new System.InvalidOperationException("Failed to create MockMenu3.");

        _menuManager.RegisterMenu("menu1", _mockMenu1!);
        _menuManager.RegisterMenu("menu2", _mockMenu2!);
        _menuManager.RegisterMenu("menu3", menu3);

        _menuManager.ShowMenu("menu1");
        _menuManager.ShowMenu("menu2");
        _menuManager.ShowMenu("menu3");

        // Act & Assert
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu3");

        _menuManager.GoBack();
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu2");

        _menuManager.GoBack();
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu1");
    }

    [TestCase]
    public void ClearHistory_RemovesAllHistoryEntries()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("menu1", _mockMenu1!);
        _menuManager.RegisterMenu("menu2", _mockMenu2!);

        _menuManager.ShowMenu("menu1");
        _menuManager.ShowMenu("menu2");

        // Act
        _menuManager.ClearHistory();
        _menuManager.GoBack(); // Should have no effect

        // Assert
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("menu2");
    }

    [TestCase]
    public void GetCurrentMenu_NoMenuActive_ReturnsNull()
    {
        // Arrange
        _menuManager = CreateMenuManager();

        // Act & Assert
        AssertThat(_menuManager.GetCurrentMenu()).IsNull();
    }

    [TestCase]
    public void GetCurrentMenu_MenuActive_ReturnsCorrectName()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("test_menu", _mockMenu1!);
        _menuManager.ShowMenu("test_menu");

        // Act & Assert
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual("test_menu");
    }

    [TestCase]
    public void IsMenuActive_CurrentMenu_ReturnsTrue()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("test_menu", _mockMenu1!);
        _menuManager.ShowMenu("test_menu");

        // Act & Assert
        AssertThat(_menuManager.IsMenuActive("test_menu")).IsTrue();
    }

    [TestCase]
    public void IsMenuActive_NotCurrentMenu_ReturnsFalse()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("menu1", _mockMenu1!);
        _menuManager.RegisterMenu("menu2", _mockMenu2!);
        _menuManager.ShowMenu("menu1");

        // Act & Assert
        AssertThat(_menuManager.IsMenuActive("menu2")).IsFalse();
    }

    [TestCase]
    public void IsMenuActive_NoMenuActive_ReturnsFalse()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("test_menu", _mockMenu1!);

        // Act & Assert
        AssertThat(_menuManager.IsMenuActive("test_menu")).IsFalse();
    }

    [TestCase]
    public void UnregisterMenu_ExistingMenu_RemovesFromCollection()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("test_menu", _mockMenu1!);
        _menuManager.ShowMenu("test_menu");
        var originalMenu = _menuManager.GetCurrentMenu();

        // Act
        _menuManager.UnregisterMenu("test_menu");

        // Assert - trying to show the unregistered menu should not change current menu
        _menuManager.ShowMenu("test_menu");
        // If the menu was successfully unregistered, trying to show it should fail
        // and the current menu should remain unchanged
        if (originalMenu == null)
        {
            AssertThat(_menuManager.GetCurrentMenu()).IsNull();
            return;
        }
        AssertThat(_menuManager.GetCurrentMenu()).IsEqual(originalMenu);
    }

    [TestCase]
    public void UnregisterMenu_NonExistentMenu_DoesNothing()
    {
        // Arrange
        _menuManager = CreateMenuManager();

        // Act - should not throw or cause issues
        _menuManager.UnregisterMenu("non_existent");

        // Assert - no exception thrown, test passes
        AssertThat(true).IsTrue(); // Just to have an assertion
    }

    [TestCase]
    public async System.Threading.Tasks.Task ShowMenu_EmitsMenuChangedSignal()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("test_menu", _mockMenu1!);

        var signalReceived = false;
        var receivedMenuName = "";

        _menuManager.MenuChanged += (menuName) =>
        {
            signalReceived = true;
            receivedMenuName = menuName;
        };

        // Act
        _menuManager.ShowMenu("test_menu");

        // Wait a frame for signal processing
        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // Assert
        AssertThat(signalReceived).IsTrue();
        AssertThat(receivedMenuName).IsEqual("test_menu");
    }

    [TestCase]
    public void MenuConstants_HaveCorrectValues()
    {
        // Assert
        AssertThat(MenuManager.MAIN_MENU).IsEqual("main_menu");
        AssertThat(MenuManager.OPTIONS_MENU).IsEqual("options_menu");
        AssertThat(MenuManager.PAUSE_MENU).IsEqual("pause_menu");
    }

    [TestCase]
    public void ShowMenu_HiddenMenu_SetsMouseFilterToIgnore()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("menu1", _mockMenu1!);
        _menuManager.RegisterMenu("menu2", _mockMenu2!);

        _menuManager.ShowMenu("menu1");

        // Act — show menu2, which hides menu1
        _menuManager.ShowMenu("menu2");

        // Assert — hidden menu must not block mouse events
        AssertThat((int)_mockMenu1!.MouseFilter).IsEqual((int)Control.MouseFilterEnum.Ignore);
    }

    [TestCase]
    public void ShowMenu_ShowingMenu_SetsMouseFilterToPass()
    {
        // Arrange
        _menuManager = CreateMenuManager();
        _menuManager.RegisterMenu("menu1", _mockMenu1!);
        _menuManager.RegisterMenu("menu2", _mockMenu2!);

        _menuManager.ShowMenu("menu1");
        _menuManager.ShowMenu("menu2"); // menu1 is now hidden with Ignore

        // Act — go back to menu1
        _menuManager.GoBack();

        // Assert — restored menu gets Pass filter
        AssertThat((int)_mockMenu1!.MouseFilter).IsEqual((int)Control.MouseFilterEnum.Pass);
    }

    // Helper Methods
    private MenuManager CreateMenuManager()
    {
        var manager = AddToTestRoot(new MenuManager());
        _testNodes.Add(manager);
        CallInitializeOnManager(manager);
        return manager;
    }

    private T AddToTestRoot<T>(T node) where T : Node
    {
        if (_root == null)
            throw new System.InvalidOperationException("Test root node is not initialized.");

        _root.AddChild(node);
        return node;
    }

    private void CallInitializeOnManager(MenuManager manager)
    {
        var initializeMethod = typeof(MenuManager).GetMethod("Initialize",
            BindingFlags.NonPublic | BindingFlags.Instance);
        initializeMethod?.Invoke(manager, null);
    }

    private void SetSingletonInstance<T>(T? instance) where T : class
    {
        var instanceProperty = typeof(T).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static);
        instanceProperty?.SetValue(null, instance);
    }

    [AfterTest]
    public void TearDown()
    {
        GD.Print("[TEST] Starting MenuManager TearDown...");

        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }
        }
        _testNodes.Clear();

        SetSingletonInstance<MenuManager>(null);

        GD.Print("[TEST] MenuManager TearDown completed");
    }
}
