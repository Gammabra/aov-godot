using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.Managers;

public class EnemyAIManager
{
    #region Private Fields

    private MapSystem? _mapSystem;
    private List<UnitSystem> _playerUnits = [];
    private List<UnitSystem> _enemyUnits = [];
    private GameManager? _gameManager;

    #endregion

    #region Constructor

    public EnemyAIManager(GameManager gameManager)
    {
        _gameManager = gameManager;
        GD.Print("EnemyAIManager created for battle");
    }

    #endregion

    #region Public Methods

    public void SetMapSystem(MapSystem map)
    {
        _mapSystem = map;
        GD.Print("EnemyAIManager: MapSystem reference set");
    }

    public void SetUnitReferences(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
    {
        _playerUnits = playerUnits;
        _enemyUnits = enemyUnits;
        GD.Print($"EnemyAIManager: Tracking {_playerUnits.Count} player units and {_enemyUnits.Count} enemy units");
    }

    public async Task ExecuteAITurn(UnitSystem unit)
    {
        if (_mapSystem == null)
        {
            GD.PrintErr("EnemyAIManager: MapSystem not set");
            unit.PassTurn();
            return;
        }

        if (_gameManager == null)
        {
            GD.PrintErr("EnemyAIManager: GameManager reference lost");
            unit.PassTurn();
            return;
        }

        GD.Print($"EnemyAIManager: {unit.Name} is thinking...");
        await unit.ToSignal(unit.GetTree(), SceneTree.SignalName.ProcessFrame);

        EnemyAIBehavior? aiBehavior = GetAIBehavior(unit);

        if (aiBehavior == null)
        {
            GD.PrintErr($"EnemyAIManager: No AI behavior found for {unit.Name}, using default behavior");
            await ExecuteDefaultBehavior(unit);
            return;
        }

        // BattleState is now the Core version - no GameManager reference
        BattleState battleState = CreateBattleState(unit);

        // aiBehavior returns a decision, EnemyAIManager executes it
        AIDecision decision = await aiBehavior.DecideTurn(battleState);
        await ExecuteDecision(decision, unit);
    }

    public List<UnitSystem> GetAlivePlayerUnits() =>
        _playerUnits.Where(u => u.IsAlive).ToList();

    public List<UnitSystem> GetAliveEnemyUnits() =>
        _enemyUnits.Where(u => u.IsAlive).ToList();

    #endregion

    #region Private Methods

    private static EnemyAIBehavior? GetAIBehavior(UnitSystem unit)
    {
        foreach (Node child in unit.GetChildren())
            if (child is EnemyAIBehavior behavior)
                return behavior;
        return null;
    }

    private BattleState CreateBattleState(UnitSystem actingUnit)
    {
        return new BattleState
        {
            ActingUnit = actingUnit,
            MapSystem = _mapSystem!,
            PlayerUnits = GetAlivePlayerUnits(),
            EnemyUnits = GetAliveEnemyUnits()
            // No GameManager here - Core BattleState is a pure data snapshot
        };
    }

    // GameManager actions now live here instead of inside BattleState
    private async Task ExecuteDecision(AIDecision decision, UnitSystem unit)
    {
        switch (decision.Action)
        {
            case AIAction.MoveAndSkill:
                if (decision.MovePosition.HasValue)
                    _gameManager!.MoveUnit(decision.MovePosition.Value);
                if (decision.Target != null && decision.Skill != null)
                    _gameManager!.UseSkill(unit, decision.Target, decision.Skill);
                break;
            case AIAction.Move:
                if (decision.MovePosition.HasValue)
                    _gameManager!.MoveUnit(decision.MovePosition.Value);
                break;
            case AIAction.UseSkill:
                if (decision.Target != null && decision.Skill != null)
                    _gameManager!.UseSkill(unit, decision.Target, decision.Skill);
                break;
            case AIAction.Pass:
            default:
                GD.Print($"EnemyAIManager: {unit.Name} passes turn");
                break;
        }

        await Task.Delay(500);
        unit.PassTurn();
    }

    private async Task ExecuteDefaultBehavior(UnitSystem unit)
    {
        await Task.Delay(1000);
        GD.Print($"EnemyAIManager: {unit.Name} passes turn (default behavior)");
        unit.PassTurn();
    }

    #endregion
}