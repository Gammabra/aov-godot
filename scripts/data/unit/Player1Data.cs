using System.Collections.Generic;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using Godot;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad;

public sealed class Skill1 : SkillSystem
{
    public Skill1()
    {
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
        Range = 1;
    }

    public override void Use(List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].SetStatusEffectOnUnit(new BurningEffect(1, AovDataStructures.ModifierType.Flat, 10));
    }
}

public sealed class Skill2 : SkillSystem
{
    public Skill2()
    {
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
        Range = 1;
    }

    public override void Use(List<UnitSystem> targets, MapSystem? map)
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

    public override void Use(List<UnitSystem> targets, MapSystem? map)
    {
        GD.Print(targets[0].Name);
        targets[0].SetStatusEffectOnUnit(new Stun(1));
    }
}

public sealed class Skill4 : SkillSystem
{
    public Skill4()
    {
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
        Range = 4;
    }

    public override void Use(List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].TakeDamage(4);
    }
}

public sealed class Skill5 : SkillSystem
{
    public Skill5()
    {
        TargetType = AovDataStructures.TargetTypes.SingleEnemy;
        Range = 5;
    }

    public override void Use(List<UnitSystem> targets, MapSystem? map)
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
        ManaPoint = 200;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;
        ActiveSkills.Add(new Skill1());
        ActiveSkills.Add(new Skill2());
        ActiveSkills.Add(new Skill3());
        ActiveSkills.Add(new Skill4());
        ActiveSkills.Add(new Skill5());
    }
}
