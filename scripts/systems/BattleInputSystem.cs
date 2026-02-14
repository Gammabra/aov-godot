using AshesOfVelsingrad.Managers;
using Godot;
using Godot.Collections;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Central system responsible for handling player inputs during battle phases.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="BattleInputSystem" /> captures and interprets user actions
///         (keyboard and mouse) related to combat gameplay, including:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Passing the turn</description>
///         </item>
///         <item>
///             <description>Moving a unit or selecting a target on the map</description>
///         </item>
///         <item>
///             <description>Selecting the move action</description>
///         </item>
///         <item>
///             <description>Selecting a skill</description>
///         </item>
///     </list>
///     <para>
///         This system acts as a single entry point for battle-related inputs and
///         communicates with other game systems through Godot signals.
///     </para>
///     <para>
///         The class follows a singleton-like pattern to ensure that only one active
///         instance exists within the scene tree.
///     </para>
/// </remarks>
public partial class BattleInputSystem : Node
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

    #region Private Properties

    private static BattleInputSystem? Instance { get; set; }

    #endregion

    #region Godot Public Events

    /// <summary>
    ///     Emitted when the player presses the "pass turn" input action.
    /// </summary>
    /// <remarks>
    ///     Used by the <see cref="GameManager" /> to indicate that the player
    ///     has chosen to skip their current turn and let control pass to the next entity.
    /// </remarks>
    [Signal]
    public delegate void OnPassTurnPressedEventHandler();

    /// <summary>
    ///     Emitted when the player clicks on a map cell
    ///     to move a unit or select a target.
    /// </summary>
    /// <param name="dest">
    ///     The target cell position on the grid, in map coordinates (<see cref="Vector3I" />).
    /// </param>
    /// <remarks>
    ///     This signal notifies systems responsible for unit movement or selection
    ///     that the player has requested a move or target action.
    /// </remarks>
    [Signal]
    public delegate void OnMoveUnitOrSelectTargetPressedEventHandler(Vector3I dest);

    /// <summary>
    ///     Emitted when the player selects a specific skill.
    /// </summary>
    /// <param name="skillId">The numerical identifier of the selected skill.</param>
    /// <remarks>
    ///     Used by the combat system or skill management system to determine which
    ///     skill the player intends to use. Skills are indexed from 0 to 4.
    /// </remarks>
    [Signal]
    public delegate void OnSelectedSkillPressedEventHandler(int skillId);

    /// <summary>
    ///     Emitted when the player selects the move action
    /// </summary>
    /// <remarks>
    ///     This signal is used to notify systems handling unit commands
    ///     that the player has initiated a move selection.
    /// </remarks>
    [Signal]
    public delegate void OnSelectMovePressedEventHandler();

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
    protected virtual void Initialize()
    {
        _mapSystemContainer = GetNode<MapSystem>(_mapSystemPath);
        _camera3DContainer = GetNode<Camera3D>(_camera3DPath);
        GD.Print("BattleInputSystem initialized successfully");
    }

    #endregion

    #region For testing methods

    /// <summary>
    ///     FOR TESTING ONLY: Manually sets the singleton instance.
    ///     This method should only be used in unit tests.
    /// </summary>
    /// <param name="instance">The instance to set as the singleton.</param>
    protected static void SetInstanceForTesting(BattleInputSystem? instance)
    {
        Instance = instance;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        // If an input must works even when the player inputs are disabled (like open settings input),
        // place it before this condition
        if (!_inputEnabled)
            return;
        if (@event.IsActionPressed("battle_pass_turn"))
        {
            _inputEnabled = false;
            EmitSignalOnPassTurnPressed();
            return;
        }

        if (@event.IsActionPressed("battle_move_unit_and_select_target"))
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
                _inputEnabled = false;
                EmitSignalOnMoveUnitOrSelectTargetPressed(cell);
            }
        }

        if (@event.IsActionPressed("battle_select_move"))
        {
            EmitSignalOnSelectMovePressed();
            return;
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
    public virtual void SetInputEnabled(bool enabled)
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
        Instance = null;
    }

    #endregion
}
