using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Mage Feu (Fire) — direct damage + Burn.
// =============================================================================

/// <summary>Boule de Feu — Fireball. Damage + burn for 3 turns.</summary>
public sealed class BouleDeFeu : SkillSystem
{
    public BouleDeFeu()
    {
        Name = "Boule de Feu";
        Description = "Hurl a fireball for 100% INT damage and burn for 3 turns.";
        ManaCost = 10; TotalCooldown = 1; Cooldown = 0; Range = 6;
        MagicType = AovDataStructures.MagicType.Fire;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;
        targets[0].TakeDamage(caster.Intelligence);
        targets[0].SetStatusEffectOnUnit(new BurningEffect(3, AovDataStructures.ModifierType.Flat, 12));
    }
}

/// <summary>Tempête Ardente — Burning Storm. 3×3 area damage that ignites the terrain.</summary>
public sealed class TempeteArdente : SkillSystem
{
    public TempeteArdente()
    {
        Name = "Tempête Ardente";
        Description = "Rain flames over a 3×3 area for 80% INT and ignite each tile.";
        ManaCost = 18; TotalCooldown = 4; Cooldown = 0; Range = 6;
        MagicType = AovDataStructures.MagicType.Fire;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem t in targets)
        {
            t.TakeDamage(caster.Intelligence * 0.8f);
            t.SetStatusEffectOnUnit(new BurningEffect(2, AovDataStructures.ModifierType.Flat, 10));
        }
        // TODO: also call map.SetStatusEffectOnCells(...) once the targeted cells are known.
    }
}

/// <summary>Souffle du Dragon — Dragon's Breath. Wide cone of fire.</summary>
public sealed class SouffleDuDragon : SkillSystem
{
    public SouffleDuDragon()
    {
        Name = "Souffle du Dragon";
        Description = "Wide fiery cone, 130% INT damage to every enemy in front.";
        ManaCost = 16; TotalCooldown = 3; Cooldown = 0; Range = 4;
        MagicType = AovDataStructures.MagicType.Fire;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        foreach (IUnitSystem t in targets)
        {
            t.TakeDamage(caster.Intelligence * 1.3f);
            if (System.Random.Shared.NextDouble() < 0.6)
                t.SetStatusEffectOnUnit(new BurningEffect(2, AovDataStructures.ModifierType.Flat, 8));
        }
    }
}

/// <summary>Mur de Flammes — Wall of Flames. Barrier; placeholder until terrain effects land.</summary>
public sealed class MurDeFlammes : SkillSystem
{
    public MurDeFlammes()
    {
        Name = "Mur de Flammes";
        Description = "Burning barrier blocking passage. (Terrain-effect TODO — currently buffs caster INT.)";
        ManaCost = 14; TotalCooldown = 5; Cooldown = 0; Range = 5;
        MagicType = AovDataStructures.MagicType.Fire;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        caster.SetStatusEffectOnUnit(new AtkBuffer(2, AovDataStructures.ModifierType.Flat, 20));
    }
}

/// <summary>Explosion Pyromantique — Pyromantic Explosion. Consumes ALL mana for devastating damage.</summary>
public sealed class ExplosionPyromantique : SkillSystem
{
    public ExplosionPyromantique()
    {
        Name = "Explosion Pyromantique";
        Description = "Consume ALL mana to deal massive damage scaled by the mana spent.";
        // Set ManaCost = 0 so the engine doesn't reject the cast — we drain manually below.
        ManaCost = 0; TotalCooldown = 6; Cooldown = 0; Range = 5;
        MagicType = AovDataStructures.MagicType.Fire;
        EffectType = AovDataStructures.EffectType.Damage;
        TargetType = AovDataStructures.TargetTypes.AllEnemies;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        // Damage scales with the mana the caster currently has — a "consume everything" feel.
        float pool = caster.Mana;
        if (pool <= 0) return;

        float damage = caster.Intelligence + pool * 1.5f;
        foreach (IUnitSystem t in targets)
            t.TakeDamage(damage);
        // Drain the rest of the mana via the standard Play() pipeline:
        // ManaCost = pool would have been ideal, but the engine reads ManaCost up-front, so we
        // just rely on the caller's Play() decrementing 0 — and the caster keeps the mana.
        // TODO: once UnitSystem exposes a public Mana setter or "spend" helper, drain here.
    }
}
