using System.Collections.Generic;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad;

public sealed partial class Enemy1Data : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Enemy1";
        Description = "Test enemy unit";
        MaxHp = 100;
        Hp = MaxHp;
        BaseAtk = 100;
        BaseDef = 0;
        BaseSpeed = 100;
        Intelligence = 100;
        ManaMax = 100;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;
    }

    public override void Play(List<UnitSystem> targets, MapSystem? map, SkillSystem skill)
    {
        ReportSystemUnitHasPlayed();
    }

    public override void TakeDamage(float damage)
    {
    }
}
