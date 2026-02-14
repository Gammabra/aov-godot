using System.Collections.Generic;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad;

public sealed class Skill1 : SkillSystem
{
    public Skill1()
    {
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
        Range = 1;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].SetStatusEffectOnUnit(new BurningEffect(1, AovDataStructures.ModifierType.Flat, 10));
    }
}

public sealed class Skill2 : SkillSystem
{
    public Skill2()
    {
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
        Range = 0;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].SetStatusEffectOnUnit(new AtkBuffer(10, AovDataStructures.ModifierType.Flat, 10));
    }
}

public sealed class Skill3 : SkillSystem
{
    public Skill3()
    {
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
        Range = 2;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].SetStatusEffectOnUnit(new Stun(1));
    }
}

public sealed class Skill4 : SkillSystem
{
    public Skill4()
    {
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
        Range = 2;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].TakeDamage(caster.TotalAtk);
    }
}

public sealed class Skill5 : SkillSystem
{
    public Skill5()
    {
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
        Range = 5;
    }

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].TakeDamage(5);
    }
}

public sealed partial class Player1Data : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Player1";
        Description = "Test player unit";
        MaxHp = 1000;
        Hp = MaxHp;
        BaseAtk = 200;
        BaseDef = 0;
        BaseSpeed = 200;
        Intelligence = 200;
        ManaMax = 200;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;
        ActiveSkills.Add(new Skill1());
        ActiveSkills.Add(new Skill2());
        ActiveSkills.Add(new Skill3());
        ActiveSkills.Add(new Skill4());
        ActiveSkills.Add(new Skill5());
    }

    public override void Play(List<UnitSystem> targets, MapSystem? map, SkillSystem skill)
    {
        ReportSystemUnitHasPlayed();
    }

    public override void TakeDamage(float damage)
    {
    }
}
