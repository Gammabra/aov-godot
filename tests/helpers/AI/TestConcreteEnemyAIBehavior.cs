using System.Threading.Tasks;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Managers;

namespace UnitTests;

public partial class TestConcreteEnemyAIBehavior : EnemyAIBehavior
{
    public bool ExecuteTurnWasCalled { get; private set; }
    public BattleState? LastBattleState { get; private set; }

    public override async Task ExecuteTurn(BattleState battleState)
    {
        ExecuteTurnWasCalled = true;
        LastBattleState = battleState;

        // Call base implementation to test actual AI logic
        await base.ExecuteTurn(battleState);
    }
}
