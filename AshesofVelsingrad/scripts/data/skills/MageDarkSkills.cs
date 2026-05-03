using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Mage Ténèbres (Darkness) — cursed damage + corruption sources.
// Conversion Corrompue (the convert-ally-to-enemy spell) ships in Phase C
// alongside the corruption system since it depends on CorruptionSystem.
// =============================================================================

/// <summary>Orbe Maudit — Cursed Orb. Weakens the target.</summary>
public sealed class OrbeMaudit : SkillSystem
{
    public OrbeMaudit()
    {
        Name = "Orbe Maudit";
        Description = "Hurl a cursed orb for 80% INT and reduce target ATK by 20 for 3 turns.";
        ManaCost = 10; TotalCooldown = 2; Cooldown = 0; Range = 5;
        MagicType = AovDataStructures.MagicType.Dark;
        EffectType = AovDataStructures.EffectType.Debuff;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.Intelligence * 0.8f);
        targets[0].SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, -20));
        // TODO: roll corruption backlash on caster (Phase C will wire this).
    }
}

/// <summary>Lame d'Ombre — Shadow Blade. Ignores defense, drains life.</summary>
public sealed class LameDOmbre : SkillSystem
{
    public LameDOmbre()
    {
        Name = "Lame d'Ombre";
        Description = "Strike for 130% INT bypassing DEF. Heal caster for 25% damage dealt.";
        ManaCost = 12; TotalCooldown = 2; Cooldown = 0; Range = 1;
        MagicType = AovDataStructures.MagicType.Dark;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        float dmg = caster.Intelligence * 1.3f;
        targets[0].BypassDamage(dmg);
        caster.OnEffectHeal(dmg * 0.25f);
        // TODO: roll corruption backlash on caster (Phase C will wire this).
    }
}

/// <summary>Pacte Sombre — Dark Pact. Sacrifice HP to boost magic.</summary>
public sealed class PacteSombre : SkillSystem
{
    public PacteSombre()
    {
        Name = "Pacte Sombre";
        Description = "Sacrifice 15% max HP for +40 ATK and +40 INT for 3 turns. Heavy backlash risk.";
        ManaCost = 0; TotalCooldown = 5; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.Dark;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        // Pay HP cost.
        caster.OnEffectDamage(AovDataStructures.ModifierType.Percent, 15);
        // Buff ATK (and INT — until a true INT buffer lands, ATK is the cleanest stand-in).
        caster.SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 40));
        // TODO: roll corruption backlash (50% base) once Phase C lands.
    }
}

/// <summary>Sang Corrompu — Corrupted Blood. Reduces regen and healing on target.</summary>
public sealed class SangCorrompu : SkillSystem
{
    public SangCorrompu()
    {
        Name = "Sang Corrompu";
        Description = "Bleed the target for 12 damage / turn over 4 turns.";
        ManaCost = 8; TotalCooldown = 3; Cooldown = 0; Range = 5;
        MagicType = AovDataStructures.MagicType.Dark;
        EffectType = AovDataStructures.EffectType.Debuff;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].SetStatusEffectOnUnit(new BurningEffect(4, AovDataStructures.ModifierType.Flat, 12));
        // TODO: replace BurningEffect with a dedicated BleedEffect that also blocks healing.
        // TODO: roll corruption backlash (Phase C).
    }
}

/// <summary>Armée des Morts — Army of the Dead. Temporarily summons corpses (placeholder).</summary>
public sealed class ArmeeDesMorts : SkillSystem
{
    public ArmeeDesMorts()
    {
        Name = "Armée des Morts";
        Description = "Summon spectral allies. (Summon system TODO — currently buffs every ally.)";
        ManaCost = 22; TotalCooldown = 6; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.Dark;
        EffectType = AovDataStructures.EffectType.Summon;
        TargetType = AovDataStructures.TargetTypes.AllAllies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem ally in targets)
            ally.SetStatusEffectOnUnit(new AtkBuffer(3, AovDataStructures.ModifierType.Flat, 30));
        // TODO: replace with a real summon mechanic + corruption backlash (Phase C).
    }
}
