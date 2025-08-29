using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.UI.Menus;

/// <summary>
/// Main menu controller that handles user interactions and navigation.
/// Implements the Manager Pattern from your architecture documentation.
/// </summary>
/// <remarks>
/// This class is responsible for displaying the main menu and handling user input.
/// It interacts with the MenuManager to show or hide menus as needed.
/// </remarks>
public partial class MainMenu : Control
{
    // For dependency injection in tests
    private MenuManager? _menuManagerOverride;

    /// <summary>
    /// FOR TESTING ONLY: Sets a custom MenuManager instance.
    /// This allows tests to inject a test double without relying on the singleton.
    /// </summary>
    /// <param name="menuManager">The MenuManager instance to use.</param>
    public void SetMenuManagerForTesting(MenuManager menuManager)
    {
        _menuManagerOverride = menuManager;
    }

    /// <summary>
    /// Gets the MenuManager instance, either from override (testing) or singleton (production).
    /// </summary>
    /// <returns>The MenuManager instance to use.</returns>
    private MenuManager? GetMenuManager()
    {
        return _menuManagerOverride ?? MenuManager.Instance;
    }

    /// <summary>
    /// Called when the node is added to the scene tree.
    /// Initializes the menu and connects signals for button interactions.
    /// </summary>
    /// <remarks>
    /// This method is called automatically by Godot when the node is ready.
    /// It sets up the initial state of the menu and connects button signals to their handlers.
    /// </remarks>
    public override void _Ready()
    {
        var menuManager = GetMenuManager();
        if (menuManager == null)
        {
            GD.PrintErr("MenuManager not found!");
            return;
        }
    }

    /// <summary>
    /// Handles the "Options" button click event.
    /// Navigates to the options menu.
    /// </summary>
    /// <remarks>
    /// This method is connected to the Options button's "button_up" signal.
    /// It triggers the options menu display by calling the MenuManager.
    /// </remarks>
    private void OnOptionsButtonButtonUp()
    {
        GD.Print("LOAD OPTIONS MENU");

        var menuManager = GetMenuManager();
        if (menuManager == null)
        {
            GD.PrintErr("MenuManager not found!");
            return;
        }

        menuManager.ShowMenu(MenuManager.OPTIONS_MENU);
    }

    private void OnExitButtonButtonUp()
    {
        GD.Print("EXIT GAME");
        GetTree().Quit();
    }
}
