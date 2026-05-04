namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Identifies which side a combatant fights for.
/// </summary>
/// <remarks>
///     <para>
///         Three factions so a "guest" friendly unit (a recruited mercenary fighting alongside
///         the player but not part of the persistent party) can be modelled separately from the
///         player party itself. <see cref="Player" /> and <see cref="Ally" /> both treat
///         <see cref="Enemy" /> as hostile; only <see cref="Player" /> units are driven by user input.
///     </para>
///     <para>
///         Lives in <c>AshesOfVelsingrad.Systems</c> rather than <c>AovDataStructures</c> because
///         it is fundamentally part of the combat system contract — units, turn manager and HUD
///         all read it.
///     </para>
/// </remarks>
public enum Faction
{
    /// <summary>The player-controlled party.</summary>
    Player,

    /// <summary>AI-controlled friendly guest units (recruited mercs, summons, scripted helpers).</summary>
    Ally,

    /// <summary>AI-controlled hostile units.</summary>
    Enemy,
}

/// <summary>
///     Convenience helpers for reasoning about <see cref="Faction" /> relationships.
/// </summary>
public static class FactionExtensions
{
    /// <summary>Returns <c>true</c> if the two factions are on the same side.</summary>
    /// <param name="self">Faction asking the question.</param>
    /// <param name="other">Faction being evaluated.</param>
    /// <returns><c>true</c> when both fight together (Player+Ally are friendly).</returns>
    public static bool IsFriendlyTo(this Faction self, Faction other)
    {
        if (self == other) return true;
        return (self == Faction.Player && other == Faction.Ally)
            || (self == Faction.Ally && other == Faction.Player);
    }

    /// <summary>Returns <c>true</c> if the two factions oppose each other.</summary>
    /// <param name="self">Faction asking the question.</param>
    /// <param name="other">Faction being evaluated.</param>
    /// <returns><c>true</c> when they are hostile.</returns>
    public static bool IsHostileTo(this Faction self, Faction other) => !self.IsFriendlyTo(other);

    /// <summary>Returns <c>true</c> for AI-controlled factions (Ally + Enemy).</summary>
    /// <param name="faction">Faction to check.</param>
    /// <returns><c>true</c> when the faction is AI-driven, not user-driven.</returns>
    public static bool IsAiControlled(this Faction faction) => faction != Faction.Player;
}
