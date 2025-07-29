using Godot;
using System.Collections.Generic;
using AshesOfVelsingrad.UI.Menus;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Manages menu navigation and state.
/// Coordinates between different menus following the Manager Pattern.
/// </summary>
public partial class MenuManager : BaseManager
{
    public static new MenuManager? Instance { get; private set; }

    [Signal]
    public delegate void MenuChangedEventHandler(string menuName);

    private readonly Dictionary<string, Control> _menus = new();
    private readonly Stack<string> _menuHistory = new();
    private string? _currentMenu;

    // Menu constants
    public const string MAIN_MENU = "main_menu";
    public const string OPTIONS_MENU = "options_menu";
    public const string PAUSE_MENU = "pause_menu";

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

    public void RegisterMenu(string menuName, Control menuControl)
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

    public void UnregisterMenu(string menuName)
    {
        if (_menus.ContainsKey(menuName))
        {
            _menus.Remove(menuName);
            GD.Print($"Menu '{menuName}' unregistered");
        }
    }

    public void ShowMenu(string menuName, bool addToHistory = true)
    {
        if (!_menus.ContainsKey(menuName))
        {
            GD.PrintErr($"Menu '{menuName}' not found!");
            return;
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

        // Add to history for back navigation
        if (addToHistory && !string.IsNullOrEmpty(_currentMenu))
        {
            _menuHistory.Push(_currentMenu);
        }

        // Show new menu
        _currentMenu = menuName;
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

    public void ClearHistory()
    {
        _menuHistory.Clear();
    }

    public string? GetCurrentMenu()
    {
        return _currentMenu;
    }

    public bool IsMenuActive(string menuName)
    {
        return _currentMenu == menuName;
    }

    // Input handling for global menu shortcuts
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            if (!string.IsNullOrEmpty(_currentMenu) && _currentMenu != MAIN_MENU)
            {
                GoBack();
                GetViewport().SetInputAsHandled();
            }
        }

        // ESC key to open/close options in-game
        if (@event.IsActionPressed("toggle_options"))
        {
            if (_currentMenu == OPTIONS_MENU)
            {
                GoBack();
            }
            else if (string.IsNullOrEmpty(_currentMenu) || _currentMenu == PAUSE_MENU)
            {
                ShowMenu(OPTIONS_MENU);
            }
            GetViewport().SetInputAsHandled();
        }
    }
}
