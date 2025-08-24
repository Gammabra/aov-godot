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
        if (MenuManager.Instance == null)
        {
            GD.PrintErr("MenuManager not found!");
            return;
        }
    }

    /// <summary>
    /// Handles the "Play" button click event.
    /// Navigates to the game start or next scene.
    /// </summary>
    /// <remarks>
    /// This method is connected to the Play button's "button_up" signal.
    /// It triggers the game start process by calling the MenuManager to show the next scene.
    /// </remarks>
    private void OnOptionsButtonButtonUp()
    {
        GD.Print("LOAD OPTIONS MENU");

        if (MenuManager.Instance == null)
        {
            GD.PrintErr("MenuManager not found!");
            return;
        }

        MenuManager.Instance.ShowMenu(MenuManager.OPTIONS_MENU);
    }

    private void OnExitButtonButtonUp()
    {
        GD.Print("EXIT GAME");
        GetTree().Quit();
    }
}
