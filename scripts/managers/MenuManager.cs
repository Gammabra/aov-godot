using System.Collections.Generic;
using AshesOfVelsingrad.UI.Menus;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Manages menu navigation and state.
/// Coordinates between different menus following the Manager Pattern.
/// </summary>
/// <remarks>
/// This class is responsible for showing, hiding, and navigating between menus.
/// It maintains a history of menus for back navigation and provides methods to register and unregister menus.
/// It also emits signals when the current menu changes.
/// </remarks>
public partial class MenuManager : BaseManager
{
    public static MenuManager? Instance { get; protected set; } // Changed from private to protected

    [Signal]
    public delegate void MenuChangedEventHandler(string menuName);

    private readonly Dictionary<string, Control> _menus = new();
    private readonly Stack<string> _menuHistory = new();
    private string? _currentMenu;

    // Menu constants
    public const string MAIN_MENU = "main_menu";
    public const string OPTIONS_MENU = "options_menu";
    public const string PAUSE_MENU = "pause_menu";

    /// <summary>
    /// Initializes the MenuManager singleton instance.
    /// Ensures only one instance exists and sets up the initial state.
    /// </summary>
    /// <remarks>
    /// This method is called automatically by Godot when the node is ready.
    /// It checks for duplicate instances and initializes the menu system.
    /// If a duplicate instance is found, it removes the duplicate.
    /// </remarks>
    protected override void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
        GD.Print("MenuManager initialized successfully");
    }

    /// <summary>
    /// FOR TESTING ONLY: Manually sets the singleton instance.
    /// This method should only be used in unit tests.
    /// </summary>
    /// <param name="instance">The instance to set as the singleton.</param>
    public static void SetInstanceForTesting(MenuManager? instance)
    {
        Instance = instance;
    }

    /// <summary>
    /// Registers a menu with the MenuManager.
    /// Adds the menu to the internal dictionary and hides it by default.
    /// </summary>
    /// <param name="menuName">The unique name of the menu to register.</param>
    /// <param name="menuControl">The Control instance representing the menu.</param>
    /// <remarks>
    /// This method checks if the menu is already registered to avoid duplicates.
    /// If the menu is already registered, it prints an error message and does not add it again.
    /// It also connects back signals for navigation menus, such as OptionsMenu, to handle back navigation.
    /// </remarks>
    public virtual void RegisterMenu(string menuName, Control menuControl)
    {
        if (_menus.ContainsKey(menuName))
        {
            GD.PrintErr($"Menu '{menuName}' is already registered!");
            return;
        }

        _menus[menuName] = menuControl;
        menuControl.Hide(); // Hide by default

        // Connect back signals for navigation menus
        if (menuControl is OptionsMenu optionsMenu)
        {
            optionsMenu.BackRequested += () => GoBack();
        }

        GD.Print($"Menu '{menuName}' registered successfully");
    }

    /// <summary>
    /// Unregisters a menu from the MenuManager.
    /// Removes the menu from the internal dictionary.
    /// </summary>
    /// <param name="menuName">The unique name of the menu to unregister.</param>
    /// <remarks>
    /// This method checks if the menu exists before attempting to remove it.
    /// If the menu is currently active, it will hide it before removing.
    /// </remarks>
    public void UnregisterMenu(string menuName)
    {
        if (_menus.ContainsKey(menuName))
        {
            _menus.Remove(menuName);
            GD.Print($"Menu '{menuName}' unregistered");
        }
    }

    /// <summary>
    /// Shows a menu by its name.
    /// Hides the current menu and shows the specified menu.
    /// </summary>
    /// <param name="menuName">The unique name of the menu to show.</param>
    /// <param name="addToHistory">Whether to add the current menu to the history stack for back navigation.</param>
    /// <remarks>
    /// This method checks if the specified menu exists in the registered menus.
    /// If it does, it hides the current menu and shows the specified menu.
    /// If the current menu is not empty, it adds it to the history stack for back navigation.
    /// If the specified menu is not found, it prints an error message.
    /// </remarks>
    public virtual void ShowMenu(string menuName, bool addToHistory = true)
    {
        if (!_menus.ContainsKey(menuName))
        {
            GD.PrintErr($"Menu '{menuName}' not found!");
            return;
        }

        // Add to history for back navigation BEFORE changing current menu
        if (addToHistory && !string.IsNullOrEmpty(_currentMenu))
        {
            _menuHistory.Push(_currentMenu);
        }

        // Hide current menu
        if (!string.IsNullOrEmpty(_currentMenu) && _menus.ContainsKey(_currentMenu))
        {
            var currentMenuControl = _menus[_currentMenu];
            if (currentMenuControl is OptionsMenu currentOptionsMenu)
            {
                currentOptionsMenu.HideMenu();
            }
            else
            {
                currentMenuControl.Hide();
            }
        }

        // Update current menu BEFORE showing new menu
        _currentMenu = menuName;

        // Show new menu
        var newMenuControl = _menus[menuName];

        if (newMenuControl is OptionsMenu optionsMenu)
        {
            optionsMenu.ShowMenu();
        }
        else
        {
            newMenuControl.Show();
        }

        EmitSignal(SignalName.MenuChanged, menuName);
        GD.Print($"Showing menu: {menuName}");
    }

    /// <summary>
    /// Goes back to the previous menu in the history stack.
    /// If there is no previous menu, it prints a message indicating that.
    /// </summary>
    /// <remarks>
    /// This method pops the last menu from the history stack and shows it.
    /// If the history stack is empty, it prints a message indicating that there is no previous menu to go back to.
    /// It does not add the previous menu back to the history stack when going back.
    /// </remarks>
    public void GoBack()
    {
        if (_menuHistory.Count > 0)
        {
            var previousMenu = _menuHistory.Pop();
            ShowMenu(previousMenu, false); // Don't add to history when going back
        }
        else
        {
            GD.Print("No previous menu to go back to");
        }
    }

    /// <summary>
    /// Clears the menu history stack.
    /// This method removes all entries from the history stack, effectively resetting it.
    /// </summary>
    /// <remarks>
    /// This method is useful when you want to reset the menu navigation state.
    /// It clears the history stack, so there are no previous menus to go back to.
    /// </remarks>
    public void ClearHistory()
    {
        _menuHistory.Clear();
    }

    /// <summary>
    /// Gets the name of the current active menu.
    /// </summary>
    /// <returns>The name of the current menu, or null if no menu is active.</returns>
    /// <remarks>
    /// This method returns the name of the currently active menu.
    /// If no menu is currently active, it returns null.
    /// This can be useful for checking the current state of the menu system.
    /// </remarks>
    public string? GetCurrentMenu()
    {
        return _currentMenu;
    }

    /// <summary>
    /// Checks if a specific menu is currently active.
    /// </summary>
    /// <param name="menuName">The unique name of the menu to check.</param>
    /// <returns>True if the specified menu is active, otherwise false.</returns>
    /// <remarks>
    /// This method checks if the specified menu is currently the active menu.
    /// It compares the provided menu name with the current active menu name.
    /// This can be useful for UI logic that needs to know if a specific menu is currently displayed.
    /// </remarks>
    public bool IsMenuActive(string menuName)
    {
        return _currentMenu == menuName;
    }
}
