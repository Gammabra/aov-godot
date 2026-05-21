using System.Collections.Generic;
using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.Helpers;

public partial class TestableMain : AshesOfVelsingrad.MainManager
{
    private readonly List<Node> _createdNodes = new();
    private System.Func<Node, Node?>? _autoFreeCallback;
    public int MainMenuInstantiateCount { get; private set; }
    public int SettingsInstantiateCount { get; private set; }

    // Track deferred calls instead of actually deferring them
    public bool ShowMainMenuWasCalled { get; private set; }

    public void SetAutoFreeCallback(System.Func<Node, Node?> callback)
    {
        _autoFreeCallback = callback;
    }

    public void SetMenuContainer(Control? menuContainer)
    {
        _menuContainer = menuContainer;
    }

    /// <summary>
    /// Provide a mock WorldContainer so ValidateInitialConditions passes.
    /// </summary>
    public void SetWorldContainer(Node? worldContainer)
    {
        _worldContainer = worldContainer;
    }

    protected override (Control? mainMenu, Control? settings) CreateMenus()
    {
        if (_menuContainer == null)
        {
            GD.Print("[TEST] MenuContainer is null, not creating test menus");
            return (null, null);
        }

        GD.Print("[TEST] Using test menu creation");

        MainMenuInstantiateCount++;
        var mainMenu = new Control { Name = "TestMainMenu" };
        _autoFreeCallback?.Invoke(mainMenu);
        _createdNodes.Add(mainMenu);

        SettingsInstantiateCount++;
        var settings = new Control { Name = "TestSettings" };
        _autoFreeCallback?.Invoke(settings);
        _createdNodes.Add(settings);

        return (mainMenu, settings);
    }

    /// <summary>
    /// Override InitializeMenus to skip CallDeferred — tests run synchronously.
    /// </summary>
    protected override void InitializeMenus()
    {
        // Resolve containers (same as base, but without CallDeferred for ShowMainMenu)
        _menuContainer ??= GetNodeOrNull<Control>("UILayer/MenuContainer");
        _worldContainer ??= GetNodeOrNull<Node>("WorldContainer");

        if (!CallValidateInitialConditions()) return;

        var (mainMenu, settings) = CreateMenus();
        if (mainMenu == null || settings == null) return;

        _menuContainer!.AddChild(mainMenu);
        _menuContainer.AddChild(settings);

        mainMenu.Visible = false;
        settings.Visible = false;

        MenuManager.Instance!.RegisterMenu(MenuManager.MAIN_MENU, mainMenu);
        MenuManager.Instance.RegisterMenu(MenuManager.OPTIONS_MENU, settings);

        // Record that ShowMainMenu would be called, but don't defer it
        ShowMainMenuWasCalled = true;
        MenuManager.Instance?.ShowMenu(MenuManager.MAIN_MENU);
    }

    /// <summary>
    /// Expose InitializeMenus publicly for tests.
    /// </summary>
    public void PublicInitializeMenus() => ((TestableMain)this).InitializeMenus();

    // Duplicate ValidateInitialConditions logic here so we don't depend
    // on the protected base method — keeps tests isolated
    private bool CallValidateInitialConditions()
    {
        if (SettingsManager.Instance == null)
        {
            GD.PrintErr("[TEST] SettingsManager missing");
            return false;
        }
        if (MenuManager.Instance == null)
        {
            GD.PrintErr("[TEST] MenuManager missing");
            return false;
        }
        if (_menuContainer == null)
        {
            GD.PrintErr("[TEST] MenuContainer missing");
            return false;
        }
        if (_worldContainer == null)
        {
            GD.PrintErr("[TEST] WorldContainer missing");
            return false;
        }
        return true;
    }

    public void FreeAllCreatedNodes()
    {
        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.GetParent()?.RemoveChild(node);
            }
        }
        _createdNodes.Clear();
    }

    public void Reset()
    {
        FreeAllCreatedNodes();
        MainMenuInstantiateCount = 0;
        SettingsInstantiateCount = 0;
        ShowMainMenuWasCalled = false;
    }

    public IReadOnlyList<Node> CreatedNodes => _createdNodes;
}
