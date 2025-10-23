using System.Collections.Generic;
using AshesOfVelsingrad.systems;

namespace AshesOfVelsingrad;

public sealed partial class Enemy1Data : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Enemy1";
        BaseSpeed = 300;
    }

    public override void Attack(List<UnitSystem> targets, MapSystem? map)
    {
        ReportSystemUnitHasPlayed();
    }

    public override void TakeDamage(float damage)
    {
    }
}
