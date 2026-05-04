using System;

namespace AshesOfVelsingrad.Systems.Battle;

/// <summary>
///     Simple decoupled notification hub used to surface user-facing battle messages
///     (warnings, info, deaths, etc.) without coupling producers to the HUD widgets.
/// </summary>
/// <remarks>
///     <para>
///         A producer such as <c>GameManager</c> calls <see cref="Post" /> when it needs to
///         tell the player something — for example "Target out of range" or "Not enough mana".
///         Any subscriber (typically <c>BattleLog</c>) receives the message and renders it.
///     </para>
///     <para>
///         Pure C# (no Godot dependencies) so the producer logic can be unit-tested without
///         instantiating the HUD.
///     </para>
/// </remarks>
public static class BattleNotifications
{
    /// <summary>
    ///     Severity tag — the HUD usually colour-codes the line based on this.
    /// </summary>
    public enum Severity
    {
        /// <summary>Neutral message (turn started, move complete, etc.).</summary>
        Info,

        /// <summary>Positive event (heal, buff, victory).</summary>
        Positive,

        /// <summary>Negative event (damage, debuff, blocked action).</summary>
        Negative,

        /// <summary>Critical event (death, defeat, hard error).</summary>
        Critical,
    }

    /// <summary>
    ///     Fires whenever a producer pushes a new notification. Subscribers MUST unsubscribe
    ///     in their cleanup path to avoid leaks.
    /// </summary>
    public static event Action<string, Severity>? Posted;

    /// <summary>
    ///     Push a new notification to every subscriber.
    /// </summary>
    /// <param name="message">User-facing text.</param>
    /// <param name="severity">How to colour-code the line. Defaults to <see cref="Severity.Info" />.</param>
    public static void Post(string message, Severity severity = Severity.Info)
    {
        Posted?.Invoke(message, severity);
    }

    /// <summary>
    ///     Remove every subscriber. Call when transitioning out of a battle scene to avoid
    ///     dangling delegates pointing at freed nodes.
    /// </summary>
    public static void Clear()
    {
        Posted = null;
    }
}
