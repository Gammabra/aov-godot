using System.Threading.Tasks;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Base class for all units in the tactical grid-based system.
///     Handles stats, movement, effects, and Godot integration.
/// </summary>
/// <remarks>
///     This class provides fundamental behavior for all combat units, including:
///     - Base stats (HP, attack, defense, etc.)
///     - Turn logic (HasPlayed)
///     - Movement logic (BFS pathfinding in 3D)
///     - Integration with <see cref="MapSystem" /> and <see cref="StatusEffect{UnitSystem}" />
/// </remarks>
public abstract partial class UnitSystem : CharacterBody3D, IUnitSystem
{
    #region Private Fields

    private TaskCompletionSource? _actionTcs;
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    #endregion

    #region Godot properties

    /// <summary>
    ///     Emitted when the unit's portrait texture changes.
    /// </summary>
    /// <param name="texture">The new portrait texture.</param>
    [Signal]
    public delegate void PortraitChangedEventHandler(Texture2D texture);

    /// <summary>
    ///     Emitted when the unit's health changes.
    /// </summary>
    /// <param name="currentHp">The unit's current HP value.</param>
    /// <param name="maxHp">The unit's maximum HP value.</param>
    [Signal]
    public delegate void HealthChangedEventHandler(float currentHp, float maxHp);

    /// <summary>
    ///     The portrait texture displayed for this unit in the UI.
    /// </summary>
    public Texture2D? PortraitTexture { get; protected set; }

    /// <summary>
    ///     The 3D Sprite of the unit displayed in the UI.
    /// </summary>
    public Sprite3D? CharacterSprite { get; protected set; }

    #endregion

    #region Public Properties

    /// <summary>The name of the unit.</summary>
    public string UnitName { get; protected set; } = string.Empty;

    /// <summary>Descriptive text about the unit.</summary>
    public string Description { get; protected set; } = string.Empty;

    /// <summary>The type or archetype of the unit.</summary>
    public AovDataStructures.UnitType Type { get; protected set; }

    #endregion

    #region Class Initialization

    /// <summary>
    ///     Called when the node is added to the scene tree.
    ///     Initializes the unit instance.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is ready.
    /// </remarks>
    public override void _Ready()
    {
        Initialize();
    }

    /// <summary>
    ///     Initializes the unit instance
    ///     This method should be overridden in derived classes to set up specific functionality.
    /// </summary>
    /// <remarks>
    ///     This method is called by the _Ready method to initialize the map.
    ///     It should contain the logic necessary to set up the unit's state and functionality.
    ///     Derived classes must implement this method to provide their specific initialization logic.
    /// </remarks>
    protected virtual void Initialize()
    {
        foreach (Node child in GetChildren())
            if (child is Sprite3D sprite)
            {
                CharacterSprite = sprite;
                break;
            }

        TotalAtk = BaseAtk + _atkModifierAmount;
        TotalDef = BaseDef + _defModifierAmount;
    }

    /// <summary>
    ///     Injects an instance of the status effect system into this unit.
    /// </summary>
    /// <param name="statusEffectSystem">The status effect system to be used by this unit.</param>
    public virtual void InjectDependencies(StatusEffectSystem statusEffectSystem)
    {
        _statusEffectSystem = statusEffectSystem;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Set the result to unlock the system and clean the <see cref="TaskCompletionSource" />.
    /// </summary>
    private void CompleteAction()
    {
        _actionTcs?.TrySetResult();
        _actionTcs = null;
    }

    /// <summary>
    ///     Called by every <see cref="IUnitSystem" /> function with an action to report to the system that the unit has played.
    /// </summary>
    protected void ReportSystemUnitHasPlayed()
    {
        GD.Print($"{UnitName} has played");
        CompleteAction();
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Handles the physics of the unit in the UI.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor()) velocity.Y -= _gravity * (float)delta;

        Velocity = velocity;
        MoveAndSlide();
    }

    /// <summary>
    ///     Lock the system and wait the unit for an action
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> that completes when the unit finishes their action.
    /// </returns>
    public Task WaitForActionAsync()
    {
        _actionTcs = new TaskCompletionSource();
        return _actionTcs.Task;
    }

    /// <summary>
    ///     Marks the unit as having completed its turn.
    /// </summary>
    public void PassTurn()
    {
        ReportSystemUnitHasPlayed();
    }

    #endregion

    #region Class Destroyer

    /// <summary>
    ///     Called when the node is removed from the scene tree.
    ///     Cleans up the unit instance and sets it to null.
    /// </summary>
    /// <remarks>
    ///     This method is called automatically by Godot when the node is removed from the scene tree.
    ///     It ensures that the unit instance is properly cleaned up and set to null.
    ///     This is important for preventing memory leaks and ensuring that the manager can be re-initialized later if needed.
    /// </remarks>
    public override void _ExitTree()
    {
        Cleanup();
    }

    /// <summary>
    ///     Cleans up the unit instance.
    ///     This method can be overridden in derived classes to implement specific cleanup logic.
    /// </summary>
    /// <remarks>
    ///     This method is called when the unit is removed from the scene tree.
    ///     It provides a place for derived classes to implement any necessary cleanup logic,
    ///     such as disconnecting signals or releasing resources.
    ///     By default, it does nothing, but derived classes can override it to perform specific cleanup tasks.
    /// </remarks>
    protected virtual void Cleanup()
    {
        // Override in derived classes for cleanup logic
    }

    #endregion
}
