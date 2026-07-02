using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Represents a finite state machine that manages transitions between different states.
/// Each child node of this node is expected to be a <see cref="State"/>.
/// </summary>
public sealed partial class StateMachine : Node
{
    /// <summary>
    /// The initial state node path that will be entered when the state machine is ready.
    /// </summary>
    [Export]
    public required NodePath InitialState;

    /// <summary>
    /// Dictionary containing all registered states indexed by their name.
    /// </summary>
    private readonly Dictionary<string, State> _states = [];

    public State? CurrentState { get; private set; }

    /// <summary>
    /// Called when the node is added to the scene tree.
    /// Initializes all child states and enters the initial state.
    /// </summary>
    public override void _Ready()
    {
        foreach (Node node in GetChildren())
        {
            if (node is State state)
            {
                _states.Add(state.Name, state);
                state.fsm = this;
                state.OnStateReady();
                state.Exit();
            }
        }

        CurrentState = GetNode<State>(InitialState);
        CurrentState.Enter();
    }

    /// <summary>
    /// Called every frame.
    /// Delegates update logic to the current state.
    /// </summary>
    /// <param name="delta">Elapsed time since the previous frame.</param>
    public override void _Process(double delta)
    {
        if (CurrentState == null)
            return;

        CurrentState.Update(delta);
    }

    /// <summary>
    /// Called at a fixed interval synchronized with the physics engine.
    /// Delegates physics-related logic to the current state.
    /// </summary>
    /// <param name="delta">Elapsed time since the previous physics frame.</param>
    public override void _PhysicsProcess(double delta)
    {
        if (CurrentState == null)
            return;

        CurrentState.PhysicsUpdate(delta);
    }

    /// <summary>
    /// Handles unprocessed input events and forwards them to the current state.
    /// </summary>
    /// <param name="event">The input event.</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (CurrentState == null)
            return;

        CurrentState.HandleInput(@event);
        @event.Dispose();
    }

    /// <summary>
    /// Transitions from the current state to another state by its key.
    /// </summary>
    /// <param name="key">The name of the target state.</param>
    public void TransitionTo(string key)
    {
        if (!_states.TryGetValue(key, out State? state) || CurrentState == state || CurrentState == null)
            return;

        CurrentState.Exit();
        CurrentState = state;
        CurrentState.Enter();
    }
}
