using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Helpers.Managers;

public partial class TestConcreteBattleInputSystem : BattleInputSystem
{
    public bool InputEnabled { get; private set; }

    public override void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
        base.SetInputEnabled(enabled);
    }
}
