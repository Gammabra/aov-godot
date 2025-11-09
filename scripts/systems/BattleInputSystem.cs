using Godot;
using Godot.Collections;

namespace AshesOfVelsingrad.systems;

public sealed partial class BattleInputSystem : Node
{
    #region Godot Private Fields

    [Export]
    private NodePath? _mapSystemPath;

    [Export]
    private NodePath? _camera3DPath;

    private MapSystem? _mapSystemContainer;
    private Camera3D? _camera3DContainer;

    #endregion

    #region Private Fields

    private bool _inputEnabled = true;

    #endregion

    #region Godot Public Events

    [Signal]
    public delegate void OnAttackPressedEventHandler();

    [Signal]
    public delegate void OnMoveUnitToPressedEventHandler(Vector3I dest);

    [Signal]
    public delegate void OnSelectedSkillPressedEventHandler(int skillId);

    #endregion

    #region Public Properties

    private static BattleInputSystem? Instance { get; set; }

    #endregion

    #region Class Initialization

    /// <summary>
    ///     Called when the node is added to the scene tree.
    ///     Initializes the <see cref="BattleInputSystem" /> instance and checks for duplicates.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is ready.
    ///     It ensures that only one instance of the <see cref="BattleInputSystem" /> exists in the scene tree.
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
    ///     Initializes the <see cref="BattleInputSystem" /> instance
    ///     This method should be overridden in derived classes to set up specific functionality.
    /// </summary>
    /// <remarks>
    ///     This method is called by the _Ready method to initialize the map.
    ///     It should contain the logic necessary to set up the map's state and functionality.
    ///     Derived classes must implement this method to provide their specific initialization logic.
    /// </remarks>
    private void Initialize()
    {
        _mapSystemContainer = GetNode<MapSystem>(_mapSystemPath);
        _camera3DContainer = GetNode<Camera3D>(_camera3DPath);
        GD.Print("BattleInputSystem initialized successfully");
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
            _inputEnabled = false;
            EmitSignalOnAttackPressed();
            return;
        }

        if (@event.IsActionPressed("battle_move_unit_to"))
        {
            if (GetViewport().GuiGetHoveredControl() is not null ||
                _camera3DContainer is null ||
                _mapSystemContainer is null)
                return;

            Vector2 mouse = GetViewport().GetMousePosition();
            Vector3 from = _camera3DContainer.ProjectRayOrigin(mouse);
            Vector3 dir = _camera3DContainer.ProjectRayNormal(mouse);
            Vector3 to = from + dir * 2000f;

            PhysicsDirectSpaceState3D space = _camera3DContainer.GetWorld3D().DirectSpaceState;
            PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(
                from,
                to
            );
            Dictionary? result = space.IntersectRay(query);

            if (result.Count < 0)
                return;
            if (result.TryGetValue("position", out Variant position))
            {
                Vector3 worldPos = (Vector3)position;
                Vector3I cell = _mapSystemContainer.LocalToMap(worldPos);
                GD.Print($"Cellule cliquée : {cell}");
                int item = _mapSystemContainer.GetCellItem(cell);
                GD.Print($"Item index : {item}");
                _inputEnabled = false;
                EmitSignalOnMoveUnitToPressed(cell);
            }
        }

        if (@event.IsActionPressed("battle_select_skill1"))
        {
            EmitSignalOnSelectedSkillPressed(0);
            return;
        }

        if (@event.IsActionPressed("battle_select_skill2"))
        {
            EmitSignalOnSelectedSkillPressed(1);
            return;
        }

        if (@event.IsActionPressed("battle_select_skill3"))
        {
            EmitSignalOnSelectedSkillPressed(2);
            return;
        }

        if (@event.IsActionPressed("battle_select_skill4"))
        {
            EmitSignalOnSelectedSkillPressed(3);
            return;
        }

        if (@event.IsActionPressed("battle_select_skill5"))
            EmitSignalOnSelectedSkillPressed(4);
    }

    /// <summary>
    ///     Set the input to enabled or not.
    /// </summary>
    /// <param name="enabled">A boolean that set the input to enabled or not.</param>
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    #endregion

    #region Class Destroyer

    /// <summary>
    ///     Called when the node is removed from the scene tree.
    ///     Cleans up the <see cref="BattleInputSystem" /> instance and sets it to null.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is removed from the scene tree.
    ///     It ensures that the <see cref="BattleInputSystem" /> instance is properly cleaned up and set to null.
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
    ///     Cleans up the <see cref="BattleInputSystem" /> instance.
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
