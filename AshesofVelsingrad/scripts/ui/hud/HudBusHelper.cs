using System;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Shared helpers for HUD widgets that need to subscribe to <see cref="BattleEventBus" />.
/// </summary>
/// <remarks>
///     <para>
///         Godot calls a node's <c>_Ready</c> at a moment that is not synchronised with the
///         <c>_Ready</c> of nodes added to the tree on the same frame. So when a widget runs
///         its <c>_Ready</c>, <see cref="BattleEventBus.Instance" /> may not yet be populated
///         (the bus's <c>_Ready</c> is still queued).
///     </para>
///     <para>
///         <see cref="WhenReadyAsync" /> polls one frame at a time until the bus is alive, then
///         lets the caller perform its subscription. This is the canonical way for HUD widgets
///         to attach themselves to the bus from inside their own <c>_Ready</c>.
///     </para>
/// </remarks>
public static class HudBusHelper
{
    /// <summary>
    ///     Wait until <see cref="BattleEventBus.Instance" /> is non-null, then run
    ///     <paramref name="onReady" />. Aborts if the node leaves the tree first.
    /// </summary>
    /// <param name="node">The widget that owns the subscription.</param>
    /// <param name="onReady">Callback invoked once the bus is alive.</param>
    /// <returns>An awaitable task (callers usually fire-and-forget).</returns>
    public static async Task WhenReadyAsync(Node node, Action<BattleEventBus> onReady)
    {
        for (int safety = 0; safety < 240; safety++)
        {
            if (!IsInstanceValid(node) || !node.IsInsideTree())
                return;
            BattleEventBus? bus = BattleEventBus.Instance;
            if (bus is not null)
            {
                onReady(bus);
                return;
            }
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        GD.PrintErr($"{node.Name}: BattleEventBus never became available after 240 frames.");
    }

    private static bool IsInstanceValid(Node n) => GodotObject.IsInstanceValid(n);
}
