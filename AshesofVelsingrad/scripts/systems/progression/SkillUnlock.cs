using Godot;

namespace AshesOfVelsingrad.systems.progression;

/// <summary>
///     Pair of (level, skillId) describing when a skill becomes available to a class.
/// </summary>
/// <remarks>
///     Authored as a sub-resource of a <see cref="CharacterClassDefinition" />.
///     Levels at or below the unit's current level are considered unlocked.
///     <see cref="IsPassive" /> distinguishes passives (auto-applied at battle start)
///     from actives (must be equipped in a <see cref="SkillLoadout" /> slot).
/// </remarks>
[GlobalClass]
public sealed partial class SkillUnlock : Resource
{
    /// <summary>The level at which the skill becomes available.</summary>
    [Export] public int Level { get; set; } = 1;

    /// <summary>The id of the skill (matches <c>SkillDefinition.SkillId</c>).</summary>
    [Export] public string SkillId { get; set; } = string.Empty;

    /// <summary>Whether this is a passive (auto-applied) or active (selectable) skill.</summary>
    [Export] public bool IsPassive { get; set; }
}
