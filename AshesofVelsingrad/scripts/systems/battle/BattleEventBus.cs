using System;
using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     Central typed event dispatcher for the combat layer.
/// </summary>
/// <remarks>
///     <para>
///         The bus is a Godot <see cref="Node" /> intended to be registered as an
///         AutoLoad singleton. Combat systems publish events through it; HUD widgets,
///         audio reactors, achievement tracking, save-state recorders, etc. subscribe.
///     </para>
///     <para>
///         A typed event API was chosen over plain Godot signals because:
///         <list type="bullet">
///             <item>Events carry rich payloads (lists, enums, records) without conversion.</item>
///             <item>Subscribers get IDE autocomplete and refactor support.</item>
///             <item>Test stubs can subscribe with simple lambdas, no scene-tree required.</item>
///         </list>
///     </para>
///     <para>
///         Subscribers should always call <see cref="Unsubscribe{T}" /> in
///         <c>_ExitTree</c> / <c>Dispose</c> to avoid dangling delegates.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         BattleEventBus.Instance.Subscribe&lt;BattleEvents.HpChanged&gt;(OnHpChanged);
///         BattleEventBus.Instance.Publish(new BattleEvents.HpChanged(unit, -10, 50, 100));
///     </code>
/// </example>
public sealed partial class BattleEventBus : Node
{
    #region Private Fields

    /// <summary>
    ///     One delegate list per event type.
    ///     Stored as <see cref="Delegate" /> so we can host arbitrary
    ///     <c>Action&lt;T&gt;</c> handlers without reflection at publish time.
    /// </summary>
    private readonly Dictionary<Type, Delegate?> _handlers = new();

    #endregion

    #region Public Properties

    /// <summary>
    ///     The active singleton instance, set in <c>_Ready</c>.
    ///     Null until the AutoLoad node has entered the tree.
    /// </summary>
    public static BattleEventBus? Instance { get; private set; }

    #endregion

    #region Class Initialization

    /// <inheritdoc />
    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {nameof(BattleEventBus)} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
        GD.Print("BattleEventBus._Ready: Instance set.");
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Subscribe a handler to an event type.
    /// </summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="handler">Callback invoked when an event of type <typeparamref name="T" /> is published.</param>
    public void Subscribe<T>(Action<T> handler)
    {
        if (handler == null)
            return;

        Type type = typeof(T);
        _handlers.TryGetValue(type, out Delegate? existing);
        _handlers[type] = existing == null ? handler : Delegate.Combine(existing, handler);
    }

    /// <summary>
    ///     Unsubscribe a previously-registered handler.
    /// </summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="handler">The same delegate that was passed to <see cref="Subscribe{T}" />.</param>
    public void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null)
            return;

        Type type = typeof(T);
        if (!_handlers.TryGetValue(type, out Delegate? existing) || existing == null)
            return;

        Delegate? remaining = Delegate.Remove(existing, handler);
        if (remaining == null)
            _handlers.Remove(type);
        else
            _handlers[type] = remaining;
    }

    /// <summary>
    ///     Publish an event to all subscribers.
    /// </summary>
    /// <typeparam name="T">The event payload type (inferred from <paramref name="payload" />).</typeparam>
    /// <param name="payload">The event data.</param>
    /// <remarks>
    ///     Exceptions thrown by handlers are caught and logged so a single buggy
    ///     subscriber does not break the entire combat loop.
    /// </remarks>
    public void Publish<T>(T payload)
    {
        if (payload == null)
            return;

        if (!_handlers.TryGetValue(typeof(T), out Delegate? handler) || handler == null)
            return;

        foreach (Delegate sub in handler.GetInvocationList())
        {
            try
            {
                ((Action<T>)sub).Invoke(payload);
            }
            catch (Exception e)
            {
                GD.PrintErr($"BattleEventBus subscriber threw on {typeof(T).Name}: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    /// <summary>
    ///     Remove every subscription. Intended for scene transitions or tests.
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
    }

    #endregion

    #region Class Destroyer

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (Instance != this)
            return;
        Clear();
        Instance = null;
    }

    #endregion
}
