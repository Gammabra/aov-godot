using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Base class for all states used in a <see cref="StateMachine"/>.
/// Provides a lifecycle and hooks for behavior implementation.
/// </summary>
public partial class State : Node
{
    /// <summary>
    /// Reference to the owning state machine.
    /// </summary>
    public StateMachine? fsm;

    /// <summary>
    /// Called when the state is entered.
    /// Override to implement enter behavior.
    /// </summary>
    public virtual void Enter()
    {
    }

    /// <summary>
    /// Called when the state is exited.
    /// Override to implement cleanup logic.
    /// </summary>
    public virtual void Exit()
    {
    }

    /// <summary>
    /// Called during the initialization phase of the state machine.
    /// </summary>
    public virtual void OnStateReady()
    {
    }

    /// <summary>
    /// Called every frame while this state is active.
    /// </summary>
    /// <param name="delta">Elapsed time since the previous frame.</param>
    public virtual void Update(double delta)
    {
    }

    /// <summary>
    /// Called at a fixed interval while this state is active.
    /// Intended for physics-related updates.
    /// </summary>
    /// <param name="delta">Elapsed time since the previous physics frame.</param>
    public virtual void PhysicsUpdate(double delta)
    {
    }

    /// <summary>
    /// Handles input events while this state is active.
    /// </summary>
    /// <param name="event">The input event.</param>
    public virtual void HandleInput(InputEvent @event)
    {
    }
}
