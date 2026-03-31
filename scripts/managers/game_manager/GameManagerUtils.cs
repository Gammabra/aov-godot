using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Managers;

public partial class GameManager
{
    /// <summary>
    ///     Loads all player and enemy units from their respective container nodes.
    /// </summary>
    /// <remarks>
    ///     This function scans the Godot scene tree for <see cref="UnitSystem" /> nodes
    ///     and stores references to them for battle management.
    /// </remarks>
    private void LoadUnits()
    {
        if (_playerUnitsContainer == null)
        {
            GD.PrintErr("PlayerUnitsContainer not set");
            return;
        }

        if (_enemyUnitsContainer == null)
        {
            GD.PrintErr("EnemyUnitsContainer not set");
            return;
        }

        _playerUnits.Clear();
        _enemyUnits.Clear();

        foreach (Node child in _playerUnitsContainer.GetChildren())
            if (child is UnitSystem unit)
            {
                unit.InjectDependencies(_statusEffectSystem);
                _playerUnits.Add(unit);
            }

        foreach (Node child in _enemyUnitsContainer.GetChildren())
            if (child is UnitSystem unit)
            {
                unit.InjectDependencies(_statusEffectSystem);
                _enemyUnits.Add(unit);
            }
    }

    /// <summary>
    ///     Executes logic for moving the active player unit to a new cell on the map.
    /// </summary>
    /// <param name="cell">The grid cell to move the unit to.</param>
    private void HandlePlayerUnitMove((int, int, int) cell)
    {
        if (_mapSystemContainer == null)
        {
            GD.PrintErr("MapSystemContainer not set in GameManager.");
            return;
        }

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            return;
        }

        if (_battleInputSystemContainer == null)
        {
            GD.PrintErr("BattleInputSystemContainer not set in GameManager.");
            return;
        }

        if (!_currentUnitPossibleMoves.Contains(cell))
        {
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        try
        {
            if (!_turnManagerContainer.GetCurrentUnit().CanMoveTo(cell.Item1, cell.Item2, cell.Item3, _mapSystemContainer))
            {
                _battleInputSystemContainer.SetInputEnabled(true);
                return;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        _currentUnitPossibleMoves.Clear();
        MoveUnit(cell);
        _battleInputSystemContainer.SetInputEnabled(true);
    }

    /// <summary>
    ///     Handles logic when a player selects a cell as a skill target.
    /// </summary>
    /// <param name="cell">The grid position selected as a target.</param>
    private void HandlePlayerSelectTarget((int, int, int) cell)
    {
        UnitSystem? target;

        if (_mapSystemContainer == null)
        {
            GD.PrintErr("MapSystemContainer not set in GameManager.");
            return;
        }

        if (_turnManagerContainer == null)
        {
            GD.PrintErr("TurnManagerContainer not set in GameManager.");
            return;
        }

        if (_battleInputSystemContainer == null)
        {
            GD.PrintErr("BattleInputSystemContainer not set in GameManager.");
            return;
        }

        try
        {
            target = _mapSystemContainer.GetUnitAt(cell.Item1, cell.Item2, cell.Item3);
        }
        catch (ArgumentOutOfRangeException)
        {
            cell.Item2 -= 1;
            try
            {
                target = _mapSystemContainer.GetUnitAt(cell.Item1, cell.Item2, cell.Item3);
            }
            catch (ArgumentOutOfRangeException)
            {
                GD.PrintErr($"No target on the cell {cell}.");
                _battleInputSystemContainer.SetInputEnabled(true);
                return;
            }
        }

        if (!_currentUnitReachableCellsForCurrentSelectedSkill.Contains(new Vector3I(cell.Item1, cell.Item2, cell.Item3)))
        {
            GD.PrintErr("The cell/target is not reachable.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (target is null)
        {
            GD.PrintErr($"No target on the cell {cell}.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_selectedSkill == null)
        {
            GD.PrintErr("No selected skill.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_selectedSkill.EffectType == AovDataStructures.EffectType.Revive && target.IsAlive)
        {
            GD.PrintErr("Target is already alive.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_selectedSkill.EffectType != AovDataStructures.EffectType.Revive && !target.IsAlive)
        {
            GD.PrintErr("Target is dead.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        UseSkill(_turnManagerContainer.GetCurrentUnit(), target, _selectedSkill);
    }

    /// <summary>
    ///     Sets all units as dead.
    ///     (The called method verify if the unit HP is equal or less than 0)
    ///     This is typically used when evaluating defeat conditions
    ///     or performing a full cleanup of player-controlled units.
    /// </summary>
    private static void CheckUnitsLife(List<IUnitSystem> units)
    {
        GD.Print("Checking units life...");
        foreach (IUnitSystem unit in units)
            unit.SetIsAlive(false);
    }

    /// <summary>
    ///     Evaluates the win/lose conditions based on the number of living units.
    ///     If no player units remain, the result is a defeat.
    ///     If no enemy units remain, the result is a victory.
    ///     Ends the turn manager loop accordingly.
    /// </summary>
    private void CheckWinLoseCondition()
    {
        int alivePlayerUnits = 0;
        int aliveEnemies = 0;

        foreach (UnitSystem unit in _playerUnits)
            if (unit.IsAlive)
                alivePlayerUnits++;
        if (alivePlayerUnits == 0)
        {
            _gameOutcome = AovDataStructures.GameOutcome.Defeat;
            _turnManagerContainer?.EndTurnManagerLoop();
            GD.Print("Lose!");
            return;
        }

        foreach (UnitSystem unit in _enemyUnits)
            if (unit.IsAlive)
                aliveEnemies++;
        if (aliveEnemies == 0)
        {
            _gameOutcome = AovDataStructures.GameOutcome.Victory;
            _turnManagerContainer?.EndTurnManagerLoop();
            GD.Print("Win!");
        }
    }

    /// <summary>
    ///     Performs end-of-turn checks for all units.
    /// </summary>
    /// <remarks>
    ///     This method updates the <see cref="UnitSystem.IsAlive" /> state of all
    ///     player and enemy units, then evaluates the win/lose conditions.
    ///     It is typically called at the end of a unit's turn to ensure that
    ///     deaths, revives, and victory conditions are handled immediately.
    /// </remarks>
    private void CheckUnitTurnEnd()
    {
        CheckUnitsLife(_playerUnits);
        CheckUnitsLife(_enemyUnits);
        CheckWinLoseCondition();
    }
}
