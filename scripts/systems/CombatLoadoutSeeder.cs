using System.Collections.Generic;
using AshesOfVelsingrad.systems.items;
using AshesOfVelsingrad.systems.skills;
using Godot;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     Stateless helper that auto-equips a default skill loadout on units with empty
///     <see cref="UnitSystem.ActiveSkills" /> and seeds the shared inventory with starter items.
/// </summary>
/// <remarks>
///     Pragmatic fallback for unit data scripts that didn't declare bespoke skill kits.
///     Does nothing if the unit already has skills, so designer-authored loadouts win.
///     Lives outside <c>GameManager</c> so the orchestrator stays slim and so this logic is
///     trivially testable / re-usable from save-load code.
/// </remarks>
public static class CombatLoadoutSeeder
{
    /// <summary>
    ///     Default skill ids for Player-faction units that didn't declare their own kit.
    /// </summary>
    public static readonly string[] PlayerDefaultSkills =
    [
        "frappe_ecrasante",
        "boule_de_feu",
        "rayon_sacre",
        "fleche_perforante",
        "coup_critique",
    ];

    /// <summary>
    ///     Default skill ids for AI-controlled units (allies + enemies).
    /// </summary>
    public static readonly string[] AiDefaultSkills =
    [
        "frappe_ecrasante",
    ];

    /// <summary>
    ///     For every combatant whose <see cref="UnitSystem.ActiveSkills" /> is empty, equip the
    ///     faction-appropriate default loadout from <see cref="SkillRegistry" />.
    /// </summary>
    /// <param name="combatants">All units in the encounter (player + ally + enemy).</param>
    public static void AutoEquip(IEnumerable<UnitSystem> combatants)
    {
        if (SkillRegistry.Instance is null)
        {
            GD.PrintErr("CombatLoadoutSeeder.AutoEquip: SkillRegistry not initialised.");
            return;
        }

        foreach (UnitSystem unit in combatants)
        {
            if (unit.ActiveSkills.Count > 0) continue;
            string[] ids = unit.Faction == Faction.Player ? PlayerDefaultSkills : AiDefaultSkills;
            int equipped = EquipFromIds(unit, ids);
            GD.Print($"CombatLoadoutSeeder: equipped {equipped} skills on {unit.UnitName} ({unit.Faction}).");
        }
    }

    /// <summary>
    ///     Drop a starter set of items into <see cref="PartyInventory" /> if it's empty.
    /// </summary>
    /// <remarks>Idempotent: a non-empty inventory is left alone.</remarks>
    public static void SeedStarterItems()
    {
        PartyInventory? inv = PartyInventory.Instance;
        if (inv is null || inv.Items.Count > 0) return;

        inv.Add("healing_potion", 3);
        inv.Add("mana_potion", 2);
        inv.Add("antidote", 1);
        inv.Add("purifying_elixir", 1);
        GD.Print("CombatLoadoutSeeder: starter items added.");
    }

    /// <summary>
    ///     Resolve and equip every skill id, returning the count actually equipped.
    /// </summary>
    /// <param name="unit">Unit receiving the skills.</param>
    /// <param name="skillIds">Skill ids in load order.</param>
    /// <returns>Number of skills successfully equipped.</returns>
    private static int EquipFromIds(UnitSystem unit, string[] skillIds)
    {
        int equipped = 0;
        foreach (string id in skillIds)
        {
            SkillDefinition? def = SkillRegistry.Instance!.GetDefinition(id);
            if (def is null) continue;
            DataDrivenSkill? skill = DataDrivenSkill.From(def);
            if (skill is null) continue;
            unit.ActiveSkills.Add(skill);
            equipped++;
        }
        return equipped;
    }
}
