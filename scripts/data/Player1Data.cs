using System.Collections.Generic;
using AshesOfVelsingrad.systems;

namespace AshesOfVelsingrad;

public sealed partial class Player1Data : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Player1";
        BaseSpeed = 200;
    }

    public override void Attack(List<UnitSystem> targets, MapSystem? map)
    {
        ReportSystemUnitHasPlayed();
    }

    public override void TakeDamage(float damage)
    {
    }
}
