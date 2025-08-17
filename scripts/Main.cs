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
        GD.Print("[MAIN] InitializeMenus started");
        
        if (SettingsManager.Instance == null)
        {
            GD.PrintErr("SettingsManager not available! Check AutoLoad configuration.");
            GD.Print("[MAIN] InitializeMenus exiting - SettingsManager is null");
            return;
        }
        GD.Print("[MAIN] SettingsManager check passed");

        if (MenuManager.Instance == null)
        {
            GD.PrintErr("MenuManager not available! Check AutoLoad configuration.");
            GD.Print("[MAIN] InitializeMenus exiting - MenuManager is null");
            return;
        }
        GD.Print("[MAIN] MenuManager check passed");

        // Debug the individual conditions
        bool mainMenuSceneValid = _mainMenuScene != null;
        bool optionsMenuSceneValid = _optionsMenuScene != null;
        bool menuContainerValid = _menuContainer != null;
        
        GD.Print($"[MAIN] _mainMenuScene != null: {mainMenuSceneValid}");
        GD.Print($"[MAIN] _optionsMenuScene != null: {optionsMenuSceneValid}");
        GD.Print($"[MAIN] _menuContainer != null: {menuContainerValid}");
        
        bool allConditionsMet = mainMenuSceneValid && optionsMenuSceneValid && menuContainerValid;
        GD.Print($"[MAIN] All conditions met: {allConditionsMet}");

        if (_mainMenuScene != null && _optionsMenuScene != null && _menuContainer != null)
        {
            GD.Print("[MAIN] Entering main initialization block");
            
            try
            {
                GD.Print("[MAIN] About to instantiate MainMenu");
                var mainMenu = _mainMenuScene.Instantiate<MainMenu>();
                GD.Print($"[MAIN] MainMenu instantiated: {mainMenu?.GetType().Name ?? "null"}");
                
                GD.Print("[MAIN] About to instantiate OptionsMenu");
                var optionsMenu = _optionsMenuScene.Instantiate<OptionsMenu>();
                GD.Print($"[MAIN] OptionsMenu instantiated: {optionsMenu?.GetType().Name ?? "null"}");

                GD.Print($"[MAIN] _mainMenuScene type is: {_mainMenuScene.GetType().FullName}");
                GD.Print($"[MAIN] _optionsMenuScene type is: {_optionsMenuScene.GetType().FullName}");

                GD.Print("[MAIN] About to add MainMenu to container");
                _menuContainer.AddChild(mainMenu);
                GD.Print("[MAIN] MainMenu added to container");
                
                GD.Print("[MAIN] About to add OptionsMenu to container");
                _menuContainer.AddChild(optionsMenu);
                GD.Print("[MAIN] OptionsMenu added to container");

                // Enregistrer le menu auprès du MenuManager
                GD.Print("[MAIN] About to register MainMenu with MenuManager");
                MenuManager.Instance.RegisterMenu(MenuManager.MAIN_MENU, mainMenu);
                GD.Print("[MAIN] MainMenu registered");
                
                GD.Print("[MAIN] About to register OptionsMenu with MenuManager");
                MenuManager.Instance.RegisterMenu(MenuManager.OPTIONS_MENU, optionsMenu);
                GD.Print("[MAIN] OptionsMenu registered");

                GD.Print("Menus initialized successfully");

                GD.Print("[MAIN] About to show MainMenu");
                MenuManager.Instance?.ShowMenu(MenuManager.MAIN_MENU);
                GD.Print("[MAIN] MainMenu shown");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"[MAIN] Exception during menu initialization: {ex.Message}");
                GD.PrintErr($"[MAIN] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    GD.PrintErr($"[MAIN] Inner exception: {ex.InnerException.Message}");
                }
                throw; // Re-throw for test to catch
            }
        }
        else
        {
            GD.Print("[MAIN] Conditions not met, not initializing menus");
        }
        
        GD.Print("[MAIN] InitializeMenus completed");
    }
}