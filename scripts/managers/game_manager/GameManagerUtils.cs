using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
/// Represents the outcome of a battle.
/// </summary>
public enum GameOutcome
{
    /// <summary>The battle is ongoing; no winner yet.</summary>
    Ongoing,

    /// <summary>The player has won the battle.</summary>
    Victory,

    /// <summary>The player has lost the battle.</summary>
    Defeat
}

/// <summary>
/// Defines the current interaction mode when clicking on the map.
/// </summary>
public enum ClickOnMapContext
{
    /// <summary>The player is selecting a cell to move a unit.</summary>
    MoveUnit,

    /// <summary>The player is selecting a unit or target cell for a skill.</summary>
    SelectUnitTarget
}

public partial class GameManager
{
    /// <summary>
    /// Loads all player and enemy units from their respective container nodes.
    /// </summary>
    /// <remarks>
    /// This function scans the Godot scene tree for <see cref="UnitSystem"/> nodes
    /// and stores references to them for battle management.
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

        GD.Print($"Players count : {_playerUnits.Count} | Enemies count : {_enemyUnits.Count}");
    }

    /// <summary>
    /// Executes logic for moving the active player unit to a new cell on the map.
    /// </summary>
    /// <param name="cell">The grid cell to move the unit to.</param>
    private void HandlePlayerUnitMove(Vector3I cell)
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

        if (!_currentUnitPossibleMoves.Contains((cell.X, cell.Y, cell.Z)))
        {
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        try
        {
            if (!_turnManagerContainer.GetCurrentUnit().CanMoveTo(cell.X, cell.Y, cell.Z, _mapSystemContainer))
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
    /// Handles logic when a player selects a cell as a skill target.
    /// </summary>
    /// <param name="cell">The grid position selected as a target.</param>
    private void HandlePlayerSelectTarget(Vector3I cell)
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
            target = _mapSystemContainer.GetUnitAt(cell.X, cell.Y, cell.Z);
        }
        catch (ArgumentOutOfRangeException)
        {
            cell.Y -= 1;
            try
            {
                target = _mapSystemContainer.GetUnitAt(cell.X, cell.Y, cell.Z);
            }
            catch (ArgumentOutOfRangeException)
            {
                GD.PrintErr($"No target on the cell {cell}.");
                _battleInputSystemContainer.SetInputEnabled(true);
                return;
            }
        }

        if (!_currentUnitReachableCellsForCurrentSelectedSkill.Contains((cell.X, cell.Y, cell.Z)))
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

        if (_selectedSkill.EffectType == EffectType.Revive && target.IsAlive)
        {
            GD.PrintErr("Target is already alive.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_selectedSkill.EffectType != EffectType.Revive && !target.IsAlive)
        {
            GD.PrintErr("Target is dead.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        UseSkill(_turnManagerContainer.GetCurrentUnit(), target, _selectedSkill);
    }

    /// <summary>
    /// Sets all units as dead.
    /// (The called method verify if the unit HP is equal or less than 0)
    /// This is typically used when evaluating defeat conditions
    /// or performing a full cleanup of player-controlled units.
    /// </summary>
    private void CheckUnitsLife(List<UnitSystem> units)
    {
        foreach (UnitSystem unit in units)
            unit.SetIsAlive(false);
    }

    /// <summary>
    /// Evaluates the win/lose conditions based on the number of living units.
    /// If no player units remain, the result is a defeat.
    /// If no enemy units remain, the result is a victory.
    /// Ends the turn manager loop accordingly.
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
            _gameOutcome = GameOutcome.Defeat;
            _turnManagerContainer?.EndTurnManagerLoop();
            GD.Print("Lose!");
            return;
        }

        foreach (UnitSystem unit in _enemyUnits)
            if (unit.IsAlive)
                aliveEnemies++;
        if (aliveEnemies == 0)
        {
            _gameOutcome = GameOutcome.Victory;
            _turnManagerContainer?.EndTurnManagerLoop();
            GD.Print("Win!");
        }
    }

    private void CheckUnitTurnEnd()
    {
        CheckUnitsLife(_playerUnits);
        CheckUnitsLife(_enemyUnits);
        CheckWinLoseCondition();
    }
}
