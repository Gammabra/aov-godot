using Godot;
using Godot.Collections;

namespace AshesOfVelsingrad.systems.progression;

/// <summary>
///     Defines the progression curve and skill catalogue for a class
///     (Combattant, Épéiste, Assassin, Archer, Mage-Fire, Mage-Water, ...).
/// </summary>
/// <remarks>
///     <para>
///         Authored as a <c>.tres</c> resource in <c>res://data/classes/</c>.
///         At runtime, <see cref="CharacterProfile" /> queries this to know
///         which skills are unlocked and which active loadout slots are valid.
///     </para>
///     <para>
///         The feature document specifies passive unlocks at levels 3, 15, 22.
///         Active unlock levels are designer-chosen; default seed values are 1, 5,
///         11, 18, 25 — one per active in the doc's "5 actives per class" budget.
///     </para>
/// </remarks>
[GlobalClass]
public sealed partial class CharacterClassDefinition : Resource
{
    /// <summary>Class identifier ("fighter", "swordsman", "assassin", ...).</summary>
    [Export] public string ClassId { get; set; } = string.Empty;

    /// <summary>Display name shown in the HUD ("Combattant", "Épéiste", ...).</summary>
    [Export] public string DisplayName { get; set; } = string.Empty;

    /// <summary>Description shown in the class-selection screen.</summary>
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     The schedule of active and passive unlocks for this class.
    /// </summary>
    [Export]
    public Array<SkillUnlock> SkillUnlocks { get; set; } = [];

    /// <summary>
    ///     Per-level base stat scaling. Stat name → growth-per-level.
    ///     A blank dictionary means "no automatic stat growth — handled by the unit scene."
    /// </summary>
    [Export]
    public Dictionary<string, float> StatGrowth { get; set; } = [];

    /// <summary>
    ///     Returns <c>true</c> if a skill of <paramref name="skillId" /> is
    ///     unlocked at <paramref name="characterLevel" />.
    /// </summary>
    /// <param name="skillId">The skill identifier to check.</param>
    /// <param name="characterLevel">The character's current level.</param>
    /// <returns><c>true</c> if available.</returns>
    public bool IsSkillUnlocked(string skillId, int characterLevel)
    {
        foreach (SkillUnlock unlock in SkillUnlocks)
            if (unlock.SkillId == skillId && unlock.Level <= characterLevel)
                return true;
        return false;
    }
}
