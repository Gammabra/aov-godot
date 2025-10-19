using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.systems;

public sealed partial class BattleInputSystem : Control
{
    #region Private Fields

    private bool _inputEnabled = true;

    #endregion

    #region Godot Private Fields

    [Export]
    private NodePath _gameManagerPath;

    private Node _gameManagerContainer;

    #endregion

    #region Public Properties

    private static BattleInputSystem? Instance { get; set; }

    #endregion

    #region Class Initialization

    /// <summary>
    ///     Called when the node is added to the scene tree.
    ///     Initializes the <see cref="BattleInputSystem"/> instance and checks for duplicates.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is ready.
    ///     It ensures that only one instance of the <see cref="BattleInputSystem"/> exists in the scene tree.
    ///     If a duplicate instance is found, it removes the duplicate.
    /// </remarks>
    public override void _Ready()
    {
        // For AutoLoad, the initialization does immediately
        if (IsInsideTree() && GetParent() == GetTree().Root)
        {
            Initialize();
        }
        // For manual instances, check for duplicates.
        else if (Instance == null)
        {
            Initialize();
        }
        else
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
        }
    }

    /// <summary>
    ///     Initializes the <see cref="BattleInputSystem"/> instance
    ///     This method should be overridden in derived classes to set up specific functionality.
    /// </summary>
    /// <remarks>
    ///     This method is called by the _Ready method to initialize the map.
    ///     It should contain the logic necessary to set up the map's state and functionality.
    ///     Derived classes must implement this method to provide their specific initialization logic.
    /// </remarks>
    private void Initialize()
    {
        _gameManagerContainer.GetNode<GameManager>(_gameManagerPath);
        SetKeyBinding("attack", Key.J);
        GD.Print("CombatInputSystem initialized successfully");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Temporary function to test the input bindings, it must be replaced by a cleaner way to handle input in the game
    /// </summary>
    /// <param name="actionName">The action name</param>
    /// <param name="key">The keybind</param>
    private static void SetKeyBinding(string actionName, Key key)
    {
        if (InputMap.HasAction(actionName))
            InputMap.EraseAction(actionName);
        InputMap.AddAction(actionName);
        InputEventKey ev = new()
        {
            Keycode = key
        };
        InputMap.ActionAddEvent(actionName, ev);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (!_inputEnabled)
            return;
        if (@event.IsActionPressed("attack"))
        {
            GD.Print("J Key pressed.");
            EmitSignal(SignalName.OnAttackPressed);
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    #endregion

    #region Godot Public Events

    [Signal]
    public delegate void OnAttackPressedEventHandler();

    #endregion

    #region Class Destroyer

    /// <summary>
    ///     Called when the node is removed from the scene tree.
    ///     Cleans up the <see cref="BattleInputSystem"/> instance and sets it to null.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is removed from the scene tree.
    ///     It ensures that the <see cref="BattleInputSystem"/> instance is properly cleaned up and set to null.
    ///     This is important for preventing memory leaks and ensuring that the manager can be re-initialized later if needed.
    /// </remarks>
    public override void _ExitTree()
    {
        if (Instance != this)
            return;
        Cleanup();
        Instance = null;
    }

    /// <summary>
    ///     Cleans up the <see cref="BattleInputSystem"/> instance.
    ///     This method can be overridden in derived classes to implement specific cleanup logic.
    /// </summary>
    /// <remarks>
    ///     This method is called when the system is removed from the scene tree.
    ///     It provides a place for derived classes to implement any necessary cleanup logic,
    ///     such as disconnecting signals or releasing resources.
    ///     By default, it does nothing, but derived classes can override it to perform specific cleanup tasks.
    /// </remarks>
    private static void Cleanup()
    {
    }

    #endregion
}
