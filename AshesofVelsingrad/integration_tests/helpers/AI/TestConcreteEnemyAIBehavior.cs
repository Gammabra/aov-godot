using System.Threading.Tasks;
using AshesOfVelsingrad.AI;

namespace UnitTests;

public partial class TestConcreteEnemyAIBehavior : EnemyAIBehavior
{
    public bool DecideTurnWasCalled { get; private set; }
    public BattleState? LastBattleState { get; private set; }

    public override async Task<AIDecision> DecideTurn(BattleState battleState)
    {
        DecideTurnWasCalled = true;
        LastBattleState = battleState;
        return await base.DecideTurn(battleState);
    }
}
