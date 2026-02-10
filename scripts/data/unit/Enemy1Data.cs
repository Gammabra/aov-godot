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
        ManaPoint = 100;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;
    }
}
