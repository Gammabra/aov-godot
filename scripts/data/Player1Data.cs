using System.Collections.Generic;
using AshesOfVelsingrad.systems;

namespace AshesOfVelsingrad;

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
        HasPlayed = false;
        PossibleMovesRange = 2;
        Curse = 0;
    }
}
