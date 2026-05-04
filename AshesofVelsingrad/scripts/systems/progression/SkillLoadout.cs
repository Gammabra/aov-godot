using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.systems.progression;

/// <summary>
///     The set of active skills the player has chosen to take into battle.
/// </summary>
/// <remarks>
///     <para>
///         Aligned with <see cref="systems.BattleInputSystem" />, which exposes
///         five hot-keys (<c>battle_select_skill1</c>..<c>battle_select_skill5</c>),
///         the loadout has exactly <see cref="MaxActiveSlots" /> slots.
///     </para>
///     <para>
///         A skill can only occupy a slot if the owner's level meets its unlock
///         requirement (<see cref="CharacterClassDefinition.IsSkillUnlocked" />).
///         Passive skills are NOT placed here — they auto-apply once unlocked
///         and are tracked on <see cref="CharacterProfile" />.
///     </para>
/// </remarks>
public sealed class SkillLoadout
{
    /// <summary>Maximum number of equipped active skills.</summary>
    public const int MaxActiveSlots = 5;

    private readonly string?[] _slots = new string?[MaxActiveSlots];

    /// <summary>
    ///     Read-only view over the slot array. <c>null</c> means "empty slot".
    /// </summary>
    public IReadOnlyList<string?> Slots => _slots;

    /// <summary>
    ///     Equip a skill into a specific slot. Replaces any existing skill in that slot.
    /// </summary>
    /// <param name="slotIndex">Slot to fill (0..<see cref="MaxActiveSlots" />-1).</param>
    /// <param name="skillId">Skill identifier, or null to clear the slot.</param>
    /// <param name="classDefinition">Owner's class definition, used for unlock check.</param>
    /// <param name="ownerLevel">Owner's current level.</param>
    /// <returns><c>true</c> if equipped successfully; <c>false</c> if rejected.</returns>
    public bool Equip(int slotIndex, string? skillId, CharacterClassDefinition? classDefinition, int ownerLevel)
    {
        if (slotIndex < 0 || slotIndex >= MaxActiveSlots)
        {
            GD.PrintErr($"SkillLoadout.Equip: invalid slot {slotIndex}.");
            return false;
        }

        if (skillId is null)
        {
            _slots[slotIndex] = null;
            return true;
        }

        if (classDefinition is not null && !classDefinition.IsSkillUnlocked(skillId, ownerLevel))
        {
            GD.PrintErr(
                $"SkillLoadout.Equip: skill '{skillId}' is locked for class '{classDefinition.ClassId}' " +
                $"at level {ownerLevel}."
            );
            return false;
        }

        _slots[slotIndex] = skillId;
        return true;
    }

    /// <summary>
    ///     Returns the skill id at the given slot, or <c>null</c> if empty / out of range.
    /// </summary>
    /// <param name="slotIndex">Slot to query.</param>
    /// <returns>Skill id or null.</returns>
    public string? GetEquipped(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxActiveSlots)
            return null;
        return _slots[slotIndex];
    }

    /// <summary>
    ///     Empties every slot.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < _slots.Length; i++)
            _slots[i] = null;
    }
}
