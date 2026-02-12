using System.Threading.Tasks;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Managers;
using Godot;

namespace UnitTests;

public partial class TestConcreteEnemyAIBehavior : EnemyAIBehavior
{
	public bool ExecuteTurnWasCalled { get; private set; }
	public BattleState? LastBattleState { get; private set; }

	public override async Task ExecuteTurn(BattleState battleState)
	{
		ExecuteTurnWasCalled = true;
		LastBattleState = battleState;

        GD.Print("TestConcreteEnemyAIBehavior: ExecuteTurn called with BattleState: " + battleState);
		
		// Simulate AI thinking
		await Task.Delay(10);
		
		// Pass turn
		battleState.ActingUnit.PassTurn();
	}
}