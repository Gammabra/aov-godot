using System;
using System.Collections.Generic;

namespace AshesOfVelsingrad.systems.progression;

/// <summary>
///     Persistent state attached to a single character.
/// </summary>
/// <remarks>
///     <para>
///         A <see cref="CharacterProfile" /> outlives any single battle. It is
///         the canonical source of truth for level, XP, equipped skills, karma,
///         corruption progress, and unlocked passives — the data the save layer
///         needs to round-trip the character.
///     </para>
///     <para>
///         The runtime <c>UnitSystem</c> instance keeps a reference to its
///         profile and reads / writes it directly. Profiles are plain C# objects
///         (no Godot dependency) so they are easy to test and to serialize.
///     </para>
/// </remarks>
public sealed class CharacterProfile
{
    /// <summary>Stable id used by save data and BattleConfig.PlayerCharacterIds.</summary>
    public string CharacterId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Display name shown in HUD and dialogues.</summary>
    public string DisplayName { get; set; } = "Unnamed";

    /// <summary>Reference to the class progression rules.</summary>
    public CharacterClassDefinition? ClassDefinition { get; set; }

    /// <summary>Total cumulative experience earned.</summary>
    public int TotalExperience { get; private set; }

    /// <summary>Current character level, derived from <see cref="TotalExperience" />.</summary>
    public int Level => ExperienceCalculator.LevelForXp(TotalExperience);

    /// <summary>The five active skills the character will bring into battle.</summary>
    public SkillLoadout Loadout { get; } = new();

    /// <summary>Skill ids of all unlocked passives (auto-applied at battle start).</summary>
    public HashSet<string> UnlockedPassiveSkillIds { get; } = [];

    /// <summary>
    ///     Karma in <c>[-100, +100]</c>. Negative = corrupt-leaning,
    ///     positive = virtuous. Influences corruption-spell backlash chance.
    /// </summary>
    public int Karma { get; private set; }

    /// <summary>
    ///     Current corruption level (0..3) accumulated by this character.
    ///     Persisted across battles per the feature document.
    /// </summary>
    public int CorruptionLevel { get; internal set; }

    /// <summary>
    ///     Sub-level corruption progress. When this reaches
    ///     <see cref="CorruptionPointsPerLevel" />, the character advances one
    ///     <see cref="CorruptionLevel" /> and this resets to 0.
    /// </summary>
    public int CorruptionPoints { get; internal set; }

    /// <summary>
    ///     Number of corruption points required to advance one corruption level.
    /// </summary>
    public const int CorruptionPointsPerLevel = 4;

    /// <summary>
    ///     Maximum corruption level. Per the feature document.
    /// </summary>
    public const int MaxCorruptionLevel = 3;

    /// <summary>
    ///     Add experience and recompute the level. Returns the number of levels
    ///     gained so callers can play VFX / unlock new skills.
    /// </summary>
    /// <param name="amount">XP to add (must be &gt;= 0).</param>
    /// <returns>Number of levels gained as a result of this call.</returns>
    public int AddExperience(int amount)
    {
        if (amount <= 0)
            return 0;
        int oldLevel = Level;
        TotalExperience += amount;
        return Math.Max(0, Level - oldLevel);
    }

    /// <summary>
    ///     Set karma to a clamped value in <c>[-100, +100]</c>.
    /// </summary>
    /// <param name="value">New karma value.</param>
    public void SetKarma(int value)
    {
        Karma = Math.Clamp(value, -100, 100);
    }

    /// <summary>
    ///     Adjust karma by a delta, clamped to <c>[-100, +100]</c>.
    /// </summary>
    /// <param name="delta">Change to apply.</param>
    public void AdjustKarma(int delta)
    {
        SetKarma(Karma + delta);
    }
}
