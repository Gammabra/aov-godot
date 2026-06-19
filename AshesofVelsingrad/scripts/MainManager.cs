using System;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.UI.Inventory;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
/// Global scene root manager. Owns the persistent canvas components,
/// handles global scene transitions within the WorldContainer, and coordinates
/// core UI visibility.
/// </summary>
public partial class MainManager : Node
{
    // Static instance for easy global access from buttons and launchers
    public static MainManager? Instance { get; private set; }

    // Containers & Layers
    [Export] protected Node? _worldContainer;
    [Export] protected Control? _menuContainer;

    // Persistent UI Elements
    [Export] protected CanvasLayer? _battleHud;
    [Export] protected BattleInventoryUI? _battleInventoryUi;
    [Export] protected ExplorationInventoryUI? _explorationInventoryUi;
    [Export] protected ColorRect? _screenTransition;

    // Menu Scenes
    [Export] protected PackedScene? _mainMenuScene;
    [Export] protected PackedScene? _settingsScene;

    private Node? _currentScene;
    private bool _isTransitioning;

    public ExplorationInventoryUI? GetExplorationInventoryUI()
    => _explorationInventoryUi as ExplorationInventoryUI;

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            GD.Print($"[MAIN MANAGER] Duplicate MainManager detected on {Name}. Removing.");
            QueueFree();
            return;
        }
        Instance = this;
    }

    public override void _Ready()
    {
        // Ensure transition screen starts transparent and ready
        if (_screenTransition != null)
        {
            _screenTransition.Visible = false;
            Color c = _screenTransition.Color;
            _screenTransition.Color = new Color(c.R, c.G, c.B, 0f);
        }

        CallDeferred(MethodName.InitializeMenus);
    }

    protected virtual void InitializeMenus()
    {
        _menuContainer ??= GetNodeOrNull<Control>("UILayer/MenuContainer");
        _worldContainer ??= GetNodeOrNull<Node>("WorldContainer");

        if (!ValidateInitialConditions())
            return;

        var (mainMenu, settings) = CreateMenus();
        if (mainMenu == null || settings == null) return;

        _menuContainer!.AddChild(mainMenu);
        _menuContainer.AddChild(settings);

        mainMenu.Visible = false;
        settings.Visible = false;

        MenuManager.Instance!.RegisterMenu(MenuManager.MAIN_MENU, mainMenu);
        MenuManager.Instance.RegisterMenu(MenuManager.OPTIONS_MENU, settings);

        // Hide gameplay UIs on boot up (Main Menu state)
        ToggleGameplayUIs(false);

        CallDeferred(MethodName.ShowMainMenu);
    }

    /// <summary>
    /// Swaps the scene inside the WorldContainer with a smooth fade transition.
    /// </summary>
    public virtual void LoadScene(string scenePath, bool showHud)
    {
        if (_isTransitioning)
        {
            GD.PrintErr($"[MAIN MANAGER] Already transitioning. Ignored load request for: {scenePath}");
            return;
        }

        if (_screenTransition == null)
        {
            // Fallback if no transition screen is configured
            PerformSceneSwap(scenePath, showHud);
            return;
        }

        _isTransitioning = true;
        _screenTransition.Visible = true;

        // Godot 4 smooth fade sequence using Tweens
        Tween fadeTween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);

        // 1. Fade to Black
        fadeTween.TweenProperty(_screenTransition, "color:a", 1.0f, 0.3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        // 2. Perform the actual resource swap while screen is black
        fadeTween.TweenCallback(Callable.From(() => PerformSceneSwap(scenePath, showHud)));

        // 3. Fade Back to Transparent
        fadeTween.TweenProperty(_screenTransition, "color:a", 0.0f, 0.3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);

        // 4. Clean up transition state
        fadeTween.TweenCallback(Callable.From(() =>
        {
            _screenTransition.Visible = false;
            _isTransitioning = false;
        }));
    }

    private void PerformSceneSwap(string scenePath, bool showHud)
    {
        // Clear previous scene content
        if (_currentScene != null)
        {
            _currentScene.QueueFree();
            _currentScene = null;
        }

        // Deactivate menus if we are moving into a map/level
        if (_menuContainer != null)
        {
            _menuContainer.Visible = false;
        }

        // Load and instantiate the new map/battle scene
        if (!string.IsNullOrEmpty(scenePath))
        {
            var sceneResource = GD.Load<PackedScene>(scenePath);
            if (sceneResource != null)
            {
                _currentScene = sceneResource.Instantiate();
                _worldContainer!.AddChild(_currentScene);
            }
            else
            {
                GD.PrintErr($"[MAIN MANAGER] Failed to load scene at path: {scenePath}");
            }
        }
        else
        {
            // If path is empty, we likely returned to Main Menu
            if (_menuContainer != null) _menuContainer.Visible = true;
            MenuManager.Instance?.ShowMenu(MenuManager.MAIN_MENU);
        }

        // Handle baseline UI visibility according to arguments passed
        ToggleGameplayUIs(showHud);

        // Always unpause the engine state on clean scene loads
        GetTree().Paused = false;
    }

    /// <summary>
    /// Global entry point to handle game pause state toggles.
    /// </summary>
    public void TogglePauseMenu()
    {
        // Stub: Implement when your Pause Menu UI is added to the UILayer
        GD.Print("[MAIN MANAGER] TogglePauseMenu called (Stub)");
    }

    private void ToggleGameplayUIs(bool visible)
    {
        // If explicitly requested off, force turn off all persistent gameplay components.
        // If requested on, let your GameManager/Player scripts handle specific initialization visibility.
        if (!visible)
        {
            if (_battleHud != null) _battleHud.Visible = false;
            if (_battleInventoryUi != null) _battleInventoryUi.Visible = false;
            if (_explorationInventoryUi != null) _explorationInventoryUi.Visible = false;
        }
    }

    private void ShowMainMenu()
    {
        MenuManager.Instance?.ShowMenu(MenuManager.MAIN_MENU);
    }

    protected virtual (Control? mainMenu, Control? settings) CreateMenus()
    {
        if (_menuContainer == null || _mainMenuScene == null || _settingsScene == null)
            return (null, null);

        var mainMenu = _mainMenuScene.Instantiate<Control>();
        var settings = _settingsScene.Instantiate<Control>();

        mainMenu.Hide();
        settings.Hide();

        return (mainMenu, settings);
    }

    private bool ValidateInitialConditions()
    {
        if (SettingsManager.Instance == null) GD.PrintErr("[MAIN MANAGER] SettingsManager autoload missing.");
        if (MenuManager.Instance == null) GD.PrintErr("[MAIN MANAGER] MenuManager autoload missing.");
        if (_menuContainer == null) GD.PrintErr("[MAIN MANAGER] MenuContainer node configuration missing.");
        if (_worldContainer == null) GD.PrintErr("[MAIN MANAGER] WorldContainer node configuration missing.");

        return SettingsManager.Instance != null && MenuManager.Instance != null && _menuContainer != null && _worldContainer != null;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;

        // Clean up current scene if still loaded
        if (_currentScene != null && IsInstanceValid(_currentScene))
        {
            _currentScene.QueueFree();
            _currentScene = null;
        }
    }
}
