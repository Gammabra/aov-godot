using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Combattant (Fighter) — full skill catalogue from the feature document.
// 5 actives + 3 passives. Use the French canonical names so these classes don't
// collide with the existing English placeholders in FighterData.cs.
// =============================================================================

#region Actives

/// <summary>Frappe Écrasante — Crushing Strike. +50% damage; can stun the target.</summary>
public sealed class FrappeEcrasante : SkillSystem
{
    public FrappeEcrasante()
    {
        Name = "Frappe Écrasante";
        Description = "Heavy melee blow dealing 150% ATK. 35% chance to stun for 1 turn.";
        ManaCost = 8;
        TotalCooldown = 2;
        Cooldown = 0;
        Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.TotalAtk * 1.5f);
        if (Random.Shared.NextDouble() < 0.35)
            targets[0].SetStatusEffectOnUnit(new Stun(1));
    }
}

/// <summary>Cri de Guerre — War Cry. +15% Atk/Def to nearby allies for 3 turns.</summary>
public sealed class CriDeGuerre : SkillSystem
{
    public CriDeGuerre()
    {
        Name = "Cri de Guerre";
        Description = "Buff every ally's ATK and DEF by 15 (flat) for 3 turns.";
        ManaCost = 10;
        TotalCooldown = 4;
        Cooldown = 0;
        Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.AllAllies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem ally in targets)
            ally.SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 15));
    }
}

/// <summary>
///     Charge — linear movement + attack. The target MUST be on the same row or column
///     as the caster (no diagonals). The caster slides along that axis and stops on the
///     tile immediately before the target, then strikes for 120% ATK.
/// </summary>
public sealed class Charge : SkillSystem
{
    public Charge()
    {
        Name = "Charge";
        Description = "Rush along a row or column up to 4 tiles, stop next to the target, strike for 120% ATK.";
        ManaCost = 6;
        TotalCooldown = 1;
        Cooldown = 0;
        Range = 4;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    /// <inheritdoc />
    public override bool IsTargetCellValid(IUnitSystem caster, int x, int y, int z, IMapSystem map)
    {
        // Cardinal alignment only — same row OR same column, same height.
        (int, int, int)? cp = map.GetUnitPosition(caster);
        if (cp is null) return true; // can't check; let the runtime resolve it
        (int cx, int cy, int cz) = cp.Value;
        if (cy != y) return false;
        return (cx == x) ^ (cz == z); // exactly one of the two axes matches
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0 || map is null) return;

        IUnitSystem target = targets[0];
        (int, int, int)? tp = map.GetUnitPosition(target);
        (int, int, int)? cp = map.GetUnitPosition(caster);
        if (tp is null || cp is null)
        {
            Godot.GD.PrintErr("Charge: missing grid positions; skipping movement.");
            target.TakeDamage(caster.TotalAtk * 1.2f);
            return;
        }

        (int cx, int cy, int cz) = cp.Value;
        (int tx, int ty, int tz) = tp.Value;

        // Cardinal alignment required: same row (Z) or same column (X), and same height (Y).
        if (cy != ty || (cx != tx && cz != tz))
        {
            Systems.Battle.BattleNotifications.Post(
                $"{caster.UnitName}: Charge requires the target on the same row or column.",
                Systems.Battle.BattleNotifications.Severity.Negative);
            // Skill consumed the cooldown via UseSkill, so still apply damage as a partial strike.
            target.TakeDamage(caster.TotalAtk * 1.2f);
            return;
        }

        // Step direction: one axis is zero, the other is sign(target - caster).
        int dx = tx == cx ? 0 : System.Math.Sign(tx - cx);
        int dz = tz == cz ? 0 : System.Math.Sign(tz - cz);

        // The "stop" cell is one step BEFORE the target on the line.
        int landX = tx - dx;
        int landZ = tz - dz;
        int landY = cy;

        // Sanity: if landX/landZ collapses to caster's tile, no movement needed.
        if ((landX, landY, landZ) == (cx, cy, cz))
        {
            target.TakeDamage(caster.TotalAtk * 1.2f);
            return;
        }

        // Verify the landing cell is walkable AND not blocked by another unit.
        bool cellWalkable;
        try { cellWalkable = map.IsWalkable(landX, landY, landZ); }
        catch (System.ArgumentOutOfRangeException) { cellWalkable = false; }

        bool cellEmpty;
        try { cellEmpty = map.IsEmpty(landX, landY, landZ); }
        catch (System.ArgumentOutOfRangeException) { cellEmpty = false; }

        IUnitSystem? blocker = null;
        try { blocker = map.GetUnitAt(landX, landY, landZ); }
        catch (System.ArgumentOutOfRangeException) { /* ignore */ }

        if (!cellWalkable || (blocker is not null && blocker != caster))
        {
            Systems.Battle.BattleNotifications.Post(
                $"{caster.UnitName}: Charge path is blocked.",
                Systems.Battle.BattleNotifications.Severity.Negative);
            target.TakeDamage(caster.TotalAtk * 1.2f);
            return;
        }

        Godot.GD.Print($"Charge: {caster.UnitName} ({cx},{cy},{cz}) → ({landX},{landY},{landZ}) attacking {target.UnitName} at ({tx},{ty},{tz}). cellEmpty={cellEmpty}");

        // Logical move: updates the unit's grid cell occupancy.
        bool moved = caster.MoveTo(landX, landY, landZ, map);
        if (!moved)
        {
            Godot.GD.PrintErr($"Charge: MoveTo({landX},{landY},{landZ}) returned false. Damaging without moving.");
            target.TakeDamage(caster.TotalAtk * 1.2f);
            return;
        }

        // Visual snap: CharacterBody3D global position.
        if (caster is Godot.CharacterBody3D body && map is Godot.GridMap grid)
        {
            Godot.Vector3 worldPos = grid.MapToLocal(new Godot.Vector3I(landX, landY, landZ));
            worldPos.Y += grid.CellSize.Y * 1.5f;
            body.GlobalPosition = worldPos;
        }

        target.TakeDamage(caster.TotalAtk * 1.2f);
    }
}

/// <summary>Blocage — Block. Apply a 50% damage-reduction shield to self for 1 turn.</summary>
/// <remarks>Currently modelled as a flat +DEF buff for 1 turn until a damage-reduction effect lands.</remarks>
public sealed class Blocage : SkillSystem
{
    public Blocage()
    {
        Name = "Blocage";
        Description = "Brace yourself: +60 DEF for 1 turn.";
        ManaCost = 4;
        TotalCooldown = 3;
        Cooldown = 0;
        Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        caster.SetStatusEffectOnUnit(new AtkBuffer(1, AovDataStructures.ModifierType.Flat, 60));
        // ^ TODO: replace with DefBuffer once a flat-DEF effect class lands.
    }
}

/// <summary>Frappe Circulaire — Circular Strike. Hits every adjacent enemy.</summary>
public sealed class FrappeCirculaire : SkillSystem
{
    public FrappeCirculaire()
    {
        Name = "Frappe Circulaire";
        Description = "Sweep every adjacent enemy for 100% ATK.";
        ManaCost = 12;
        TotalCooldown = 3;
        Cooldown = 0;
        Range = 1;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem t in targets)
            t.TakeDamage(caster.TotalAtk);
    }
}

#endregion

#region Passives

/// <summary>Force Brute — +10% melee damage. Apply at battle start; not invoked.</summary>
public sealed class ForceBrute : SkillSystem
{
    public ForceBrute()
    {
        Name = "Force Brute";
        Description = "Passive — +10% damage on melee skills (Range 1).";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

/// <summary>Témérité — +5% damage per 25% missing HP. Wired in damage formula, not as a Use().</summary>
public sealed class Temerite : SkillSystem
{
    public Temerite()
    {
        Name = "Témérité";
        Description = "Passive — +5% damage per 25% missing HP.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

/// <summary>Endurance Guerrière — -15% damage taken after a successful melee strike.</summary>
public sealed class EnduranceGuerriere : SkillSystem
{
    public EnduranceGuerriere()
    {
        Name = "Endurance Guerrière";
        Description = "Passive — -15% damage taken for the rest of the turn after landing a melee hit.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

#endregion
