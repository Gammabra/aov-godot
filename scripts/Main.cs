using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.UI.Menus;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
/// Main scene controller that initializes the game systems.
/// Follows the Component-Based Architecture and event-driven communication.
/// </summary>
public partial class Main : Node
{
    [Export] private Control? _menuContainer;
    [Export] private PackedScene? _mainMenuScene;
    [Export] private PackedScene? _optionsMenuScene;

    public override void _Ready()
    {
        CallDeferred(MethodName.InitializeMenus);
    }

    /// <summary>
    /// Initializes the main and options menus, registering them with the MenuManager.
    /// </summary>
    /// <remarks>
    /// This method is deferred to ensure all AutoLoad nodes are initialized before accessing them.
    /// </remarks>
    private void InitializeMenus()
    {
        if (SettingsManager.Instance == null)
        {
            GD.PrintErr("SettingsManager not available! Check AutoLoad configuration.");
            return;
        }

        if (MenuManager.Instance == null)
        {
            GD.PrintErr("MenuManager not available! Check AutoLoad configuration.");
            return;
        }

        if (_mainMenuScene != null && _optionsMenuScene != null && _menuContainer != null)
        {
            var mainMenu = _mainMenuScene.Instantiate<MainMenu>();
            var optionsMenu = _optionsMenuScene.Instantiate<OptionsMenu>();

            _menuContainer.AddChild(mainMenu);
            _menuContainer.AddChild(optionsMenu);

            // Enregistrer le menu auprès du MenuManager
            MenuManager.Instance.RegisterMenu(MenuManager.MAIN_MENU, mainMenu);
            MenuManager.Instance.RegisterMenu(MenuManager.OPTIONS_MENU, optionsMenu);

            GD.Print("Menus initialized successfully");

            MenuManager.Instance?.ShowMenu(MenuManager.MAIN_MENU);
        }
    }
}
