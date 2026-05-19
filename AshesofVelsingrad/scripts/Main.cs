using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
///     Global scene root. Owns the persistent canvas (MenuContainer) and
///     delegates all scene transitions to <see cref="MenuManager" />.
///     Add new scenes here as the project grows (pause menu, HUD overlays, etc.)
/// </summary>
public partial class Main : Node
{
    [Export] protected Control? _menuContainer;
    [Export] protected PackedScene? _mainMenuScene;
    [Export] protected PackedScene? _settingsScene;

    public override void _Ready()
    {
        CallDeferred(MethodName.InitializeMenus);
    }

    protected virtual void InitializeMenus()
    {
        _menuContainer ??= GetNodeOrNull<Control>("MenuContainer");

        // Count what's already in the tree
        foreach (Node child in GetTree().Root.GetChildren())
            GD.Print($"  - {child.Name} ({child.GetType().Name}) visible={((child as CanvasItem)?.Visible)}");

        if (!ValidateInitialConditions())
            return;

        var (mainMenu, settings) = CreateMenus();
        if (mainMenu == null || settings == null) return;

        _menuContainer!.AddChild(mainMenu);
        _menuContainer.AddChild(settings);

        // Simple visibility set — don't call HideAll here since
        // SettingsPageManager._Ready hasn't run yet at this point
        mainMenu.Visible = false;
        settings.Visible = false;

        MenuManager.Instance!.RegisterMenu(MenuManager.MAIN_MENU, mainMenu);
        MenuManager.Instance.RegisterMenu(MenuManager.OPTIONS_MENU, settings);

        // Use CallDeferred so all _Ready calls finish before ShowMenu runs
        CallDeferred(MethodName.ShowMainMenu);
    }

    private void ShowMainMenu()
    {
        MenuManager.Instance?.ShowMenu(MenuManager.MAIN_MENU);
    }

    protected virtual (Control mainMenu, Control settings) CreateMenus()
    {
        if (_menuContainer == null || _mainMenuScene == null || _settingsScene == null)
            return (null!, null!);

        var mainMenu = _mainMenuScene.Instantiate<Control>();
        var settings = _settingsScene.Instantiate<Control>();

        // Hide immediately before entering the tree so no visible flash occurs
        mainMenu.Hide();
        settings.Hide();

        return (mainMenu, settings);
    }

    private bool ValidateInitialConditions()
    {
        if (SettingsManager.Instance == null)
        {
            GD.PrintErr("[MAIN] SettingsManager autoload missing.");
            return false;
        }
        if (MenuManager.Instance == null)
        {
            GD.PrintErr("[MAIN] MenuManager autoload missing.");
            return false;
        }
        if (_menuContainer == null)
        {
            GD.PrintErr("[MAIN] MenuContainer not set.");
            return false;
        }
        return true;
    }
}