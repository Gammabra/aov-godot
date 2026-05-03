namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     Identifies which side a unit fights on during a battle.
/// </summary>
/// <remarks>
///     The combat loop uses this to decide turn ownership (who controls the unit
///     this turn — the player input layer or the AI), to evaluate victory
///     conditions, and to select valid targets for skills (single ally / single
///     enemy / etc.).
/// </remarks>
public enum Faction
{
    /// <summary>
    ///     Units that the human player commands directly via the HUD.
    /// </summary>
    Player,

    /// <summary>
    ///     AI-controlled units that fight on the player's side
    ///     (recruited mercenaries acting as guests, summoned creatures,
    ///     temporarily-friendly NPCs, etc.).
    /// </summary>
    Ally,

    /// <summary>
    ///     AI-controlled hostile units.
    /// </summary>
    Enemy
}

/// <summary>
///     Helper extensions for <see cref="Faction" />.
/// </summary>
public static class FactionExtensions
{
    /// <summary>
    ///     Returns <c>true</c> if both factions are on the same side
    ///     (<see cref="Faction.Player" /> and <see cref="Faction.Ally" /> are friendly).
    /// </summary>
    /// <param name="self">The faction to compare from.</param>
    /// <param name="other">The other faction.</param>
    /// <returns><c>true</c> if the two factions are friendly.</returns>
    public static bool IsFriendlyTo(this Faction self, Faction other)
    {
        if (self == other)
            return true;
        return (self == Faction.Player && other == Faction.Ally) ||
               (self == Faction.Ally && other == Faction.Player);
    }

    /// <summary>
    ///     Returns <c>true</c> if the faction is hostile to <paramref name="other" />.
    /// </summary>
    /// <param name="self">The faction to compare from.</param>
    /// <param name="other">The other faction.</param>
    /// <returns><c>true</c> if the two factions are enemies.</returns>
    public static bool IsHostileTo(this Faction self, Faction other)
    {
        return !self.IsFriendlyTo(other);
    }

    /// <summary>
    ///     Returns <c>true</c> if the faction is controlled by AI rather than
    ///     waiting for player input. Used by <c>TurnManager</c> to choose between
    ///     <c>WaitForActionAsync</c> and <c>RunAi</c>.
    /// </summary>
    /// <param name="faction">The faction to check.</param>
    /// <returns><c>true</c> for <see cref="Faction.Ally" /> or <see cref="Faction.Enemy" />.</returns>
    public static bool IsAiControlled(this Faction faction)
    {
        return faction != Faction.Player;
    }
}
