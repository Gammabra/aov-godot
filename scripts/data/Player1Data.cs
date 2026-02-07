using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad;

public sealed class Skill1 : SkillSystem
{
    public Skill1()
    {
        TargetType = TargetTypes.SingleEnemy;
        Range = 1;
    }

    public override void Use(List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].TakeDamage(101);
        GD.Print(targets[0].Hp);
    }
}

public sealed class Skill2 : SkillSystem
{
    public Skill2()
    {
        TargetType = TargetTypes.SingleEnemy;
        Range = 2;
    }

    public override void Use(List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].TakeDamage(2);
    }
}

public sealed class Skill3 : SkillSystem
{
    public Skill3()
    {
        TargetType = TargetTypes.SingleEnemy;
        Range = 3;
    }

    public override void Use(List<UnitSystem> targets, MapSystem? map)
    {
        targets[0].TakeDamage(3);
    }
}

public sealed class Skill4 : SkillSystem
{
    public Skill4()
    {
        TargetType = TargetTypes.SingleEnemy;
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
        TargetType = TargetTypes.SingleEnemy;
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
        MaxHp = 2000;
        Hp = MaxHp;
        BaseAtk = 200;
        BaseDef = 200;
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
