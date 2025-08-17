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

    // Pour les tests
    private ISceneInstantiator? _sceneInstantiator;

    public override void _Ready()
    {
        CallDeferred(MethodName.InitializeMenus);
    }

    // Méthode pour injecter l'instantiateur (utilisée uniquement pour les tests)
    public void SetSceneInstantiator(ISceneInstantiator instantiator)
    {
        _sceneInstantiator = instantiator;
    }

    private void InitializeMenus()
    {
        GD.Print("[MAIN] InitializeMenus started");
        
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

        if (_menuContainer == null)
        {
            GD.Print("[MAIN] MenuContainer is null");
            return;
        }

        try
        {
            Node mainMenu;
            Node optionsMenu;

            // Utiliser l'instantiateur de test si injecté, sinon utiliser la logique normale
            if (_sceneInstantiator != null)
            {
                GD.Print("[MAIN] Using injected scene instantiator");
                mainMenu = _sceneInstantiator.InstantiateMainMenu();
                optionsMenu = _sceneInstantiator.InstantiateOptionsMenu();
            }
            else
            {
                GD.Print("[MAIN] Using default scene instantiation");
                if (_mainMenuScene == null || _optionsMenuScene == null)
                {
                    GD.PrintErr("[MAIN] PackedScenes are null");
                    return;
                }

                mainMenu = _mainMenuScene.Instantiate();
                optionsMenu = _optionsMenuScene.Instantiate();
            }

            if (mainMenu == null || optionsMenu == null)
            {
                GD.PrintErr("[MAIN] Failed to instantiate menus");
                return;
            }

            GD.Print($"[MAIN] MainMenu instantiated: {mainMenu.GetType().Name}");
            GD.Print($"[MAIN] OptionsMenu instantiated: {optionsMenu.GetType().Name}");

            _menuContainer.AddChild(mainMenu);
            _menuContainer.AddChild(optionsMenu);

            if (mainMenu is Control mainMenuControl && optionsMenu is Control optionsMenuControl)
            {
                MenuManager.Instance.RegisterMenu(MenuManager.MAIN_MENU, mainMenuControl);
                MenuManager.Instance.RegisterMenu(MenuManager.OPTIONS_MENU, optionsMenuControl);
                MenuManager.Instance.ShowMenu(MenuManager.MAIN_MENU);
                GD.Print("Menus initialized successfully");
            }
            else
            {
                GD.PrintErr("[MAIN] Menus are not Control nodes");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[MAIN] Exception during menu initialization: {ex.Message}");
            throw;
        }
    }
}
