using System;

namespace AshesOfVelsingrad.systems.progression;

/// <summary>
///     Stateless helpers for converting raw experience points into character levels.
/// </summary>
/// <remarks>
///     <para>
///         The feature document fixes <c>MaxLevel = 30</c> and the passive-unlock
///         schedule (3 / 15 / 22) but does not specify an XP curve. We use a smooth
///         polynomial curve calibrated so:
///         <list type="bullet">
///             <item><description>Level 2 requires ~50 XP (one easy fight).</description></item>
///             <item><description>Level 10 requires ~3,000 cumulative XP.</description></item>
///             <item><description>Level 30 requires ~50,000 cumulative XP (long-tail growth).</description></item>
///         </list>
///         Designers can replace <see cref="XpRequiredForLevel" /> in one spot
///         without touching the rest of the progression code.
///     </para>
/// </remarks>
public static class ExperienceCalculator
{
    /// <summary>
    ///     Hard cap on character level. Per <c>Feature Document § 7</c>.
    /// </summary>
    public const int MaxLevel = 30;

    /// <summary>
    ///     Returns the cumulative XP required to reach the start of <paramref name="level" />.
    /// </summary>
    /// <param name="level">Target level (1-based; level 1 always requires 0 XP).</param>
    /// <returns>Total XP needed from level 1.</returns>
    public static int XpRequiredForLevel(int level)
    {
        if (level <= 1)
            return 0;
        if (level > MaxLevel)
            level = MaxLevel;

        // Cubic-ish curve: f(L) = round(25 * (L-1)^2 + 5 * (L-1)^3 / 4).
        int delta = level - 1;
        double xp = 25.0 * delta * delta + 1.25 * delta * delta * delta;
        return (int)Math.Round(xp);
    }

    /// <summary>
    ///     Returns the highest level whose XP requirement is &lt;= <paramref name="totalXp" />.
    /// </summary>
    /// <param name="totalXp">Cumulative XP earned by the character.</param>
    /// <returns>The character's current level, in <c>[1, MaxLevel]</c>.</returns>
    public static int LevelForXp(int totalXp)
    {
        if (totalXp <= 0)
            return 1;

        for (int level = MaxLevel; level >= 2; level--)
            if (totalXp >= XpRequiredForLevel(level))
                return level;
        return 1;
    }

    /// <summary>
    ///     XP needed to advance from the current total to the next level.
    /// </summary>
    /// <param name="totalXp">The character's cumulative XP.</param>
    /// <returns>XP remaining to next level, or 0 at the cap.</returns>
    public static int XpToNextLevel(int totalXp)
    {
        int currentLevel = LevelForXp(totalXp);
        if (currentLevel >= MaxLevel)
            return 0;
        return XpRequiredForLevel(currentLevel + 1) - totalXp;
    }
}
