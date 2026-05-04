using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Mage Terre (Earth) — defense / control + armor smash.
// =============================================================================

/// <summary>Lance de Roc — Rock Spear. Pierces armor.</summary>
public sealed class LanceDeRoc : SkillSystem
{
    public LanceDeRoc()
    {
        Name = SkillStrings.StoneSpikeName;
        Description = "Pierce a target for 110% INT (ignores DEF).";
        ManaCost = 10; TotalCooldown = 2; Cooldown = 0; Range = 5;
        MagicType = AovDataStructures.MagicType.Earth;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].BypassDamage(caster.Intelligence * 1.1f);
    }
}

/// <summary>Onde Sismique — Seismic Wave. Knocks enemies down in a line; 70% Stun chance.</summary>
public sealed class OndeSismique : SkillSystem
{
    public OndeSismique()
    {
        Name = "Seismic Wave";
        Description = "Quake a line for 80% INT with a 70% chance to stun for 1 turn.";
        ManaCost = 14; TotalCooldown = 4; Cooldown = 0; Range = 5;
        MagicType = AovDataStructures.MagicType.Earth;
        EffectType = AovDataStructures.EffectType.Control;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem t in targets)
        {
            t.TakeDamage(caster.Intelligence * 0.8f);
            if (System.Random.Shared.NextDouble() < 0.7)
                t.SetStatusEffectOnUnit(new Stun(1));
        }
    }
}

/// <summary>Forteresse de Pierre — Stone Fortress. Wall blocking movement (placeholder).</summary>
public sealed class ForteresseDePierre : SkillSystem
{
    public ForteresseDePierre()
    {
        Name = "Stone Fortress";
        Description = "Raise a stone wall blocking movement. (Terrain TODO — currently buffs caster DEF.)";
        ManaCost = 12; TotalCooldown = 5; Cooldown = 0; Range = 4;
        MagicType = AovDataStructures.MagicType.Earth;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        caster.SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 60));
    }
}

/// <summary>Peau de Granit — Granite Skin. +DEF to ally for 3 turns.</summary>
public sealed class PeauDeGranit : SkillSystem
{
    public PeauDeGranit()
    {
        Name = "Granite Skin";
        Description = "Toughen an ally: +60 DEF for 3 turns.";
        ManaCost = 8; TotalCooldown = 4; Cooldown = 0; Range = 3;
        MagicType = AovDataStructures.MagicType.Earth;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 60));
    }
}

/// <summary>Avatar du Titan — Titan Avatar. Temporary stone-colossus form.</summary>
public sealed class AvatarDuTitan : SkillSystem
{
    public AvatarDuTitan()
    {
        Name = "Titan Avatar";
        Description = "Become a colossus: +100 ATK and +100 DEF for 3 turns.";
        ManaCost = 24; TotalCooldown = 6; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.Earth;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        caster.SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 100));
    }
}
