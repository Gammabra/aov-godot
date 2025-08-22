using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
/// Main scene controller that initializes the game systems.
/// Follows the Component-Based Architecture and event-driven communication.
/// </summary>
public partial class Main : Node
{
    [Export] protected Control? _menuContainer;  // protected au lieu de private
    [Export] protected PackedScene? _mainMenuScene;  // protected au lieu de private
    [Export] protected PackedScene? _optionsMenuScene;  // protected au lieu de private

    public override void _Ready()
    {
        CallDeferred(MethodName.InitializeMenus);
    }

    /// <summary>
    /// Initialize menus - virtual pour permettre l'override dans les tests
    /// </summary>
    protected virtual void InitializeMenus()
    {
        GD.Print("[MAIN] InitializeMenus started");

        if (!ValidateInitialConditions())
            return;

        try
        {
            var (mainMenu, optionsMenu) = CreateMenus();

            if (mainMenu == null || optionsMenu == null)
            {
                GD.PrintErr("[MAIN] Failed to instantiate menus");
                return;
            }

            AddMenusToContainer(mainMenu, optionsMenu);
            RegisterAndShowMenus(mainMenu, optionsMenu);

            GD.Print("Menus initialized successfully");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MAIN] Exception during menu initialization: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Create menu instances - virtual pour permettre l'override dans les tests
    /// </summary>
    protected virtual (Node mainMenu, Node optionsMenu) CreateMenus()
    {
        // Vérifier que le container existe avant de créer les menus
        if (_menuContainer == null)
        {
            GD.Print("[MAIN] MenuContainer is null, not creating menus");
            return (null!, null!);
        }

        if (_mainMenuScene == null || _optionsMenuScene == null)
        {
            GD.PrintErr("[MAIN] PackedScenes are null");
            return (null!, null!);
        }

        GD.Print("[MAIN] Using default scene instantiation");
        var mainMenu = _mainMenuScene.Instantiate();
        var optionsMenu = _optionsMenuScene.Instantiate();

        GD.Print($"[MAIN] MainMenu instantiated: {mainMenu.GetType().Name}");
        GD.Print($"[MAIN] OptionsMenu instantiated: {optionsMenu.GetType().Name}");

        return (mainMenu, optionsMenu);
    }

    private bool ValidateInitialConditions()
    {
        if (SettingsManager.Instance == null)
        {
            GD.PrintErr("SettingsManager not available! Check AutoLoad configuration.");
            return false;
        }

        if (MenuManager.Instance == null)
        {
            GD.PrintErr("MenuManager not available! Check AutoLoad configuration.");
            return false;
        }

        if (_menuContainer == null)
        {
            GD.Print("[MAIN] MenuContainer is null");
            return false;
        }

        return true;
    }

    private void AddMenusToContainer(Node mainMenu, Node optionsMenu)
    {
        _menuContainer!.AddChild(mainMenu);
        _menuContainer.AddChild(optionsMenu);
    }

    private void RegisterAndShowMenus(Node mainMenu, Node optionsMenu)
    {
        if (mainMenu is Control mainMenuControl && optionsMenu is Control optionsMenuControl)
        {
            MenuManager.Instance!.RegisterMenu(MenuManager.MAIN_MENU, mainMenuControl);
            MenuManager.Instance.RegisterMenu(MenuManager.OPTIONS_MENU, optionsMenuControl);
            MenuManager.Instance.ShowMenu(MenuManager.MAIN_MENU);
        }
        else
        {
            GD.PrintErr("[MAIN] Menus are not Control nodes");
        }
    }
}
