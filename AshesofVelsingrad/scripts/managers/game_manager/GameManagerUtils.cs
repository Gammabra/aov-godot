using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using AshesOfVelsingrad.UI.Hud;
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
        _allyUnits.Clear();
        _enemyUnits.Clear();

        // If a BattleSetup was queued by BattleLauncher, instantiate its PackedScenes
        // into the matching containers BEFORE the scene-tree pass picks them up. This
        // lets exploration-driven encounters compose the roster at runtime; standalone
        // launches of Test.tscn (no launcher / no setup) skip this and fall back to
        // whatever units the .tscn already has under the containers.
        SpawnUnitsFromPendingSetup();

        foreach (Node child in _playerUnitsContainer.GetChildren())
            if (child is UnitSystem unit)
            {
                unit.InjectDependencies(_statusEffectSystem);
                unit.SetFaction(Faction.Player);
                _playerUnits.Add(unit);
                AttachFactionMarker(unit);
            }

        foreach (Node child in _enemyUnitsContainer.GetChildren())
            if (child is UnitSystem unit)
            {
                unit.InjectDependencies(_statusEffectSystem);
                unit.SetFaction(Faction.Enemy);
                _enemyUnits.Add(unit);
                AttachFactionMarker(unit);
            }

        if (_alliedUnitsContainer is not null)
            foreach (Node child in _alliedUnitsContainer.GetChildren())
                if (child is UnitSystem unit)
                {
                    unit.InjectDependencies(_statusEffectSystem);
                    unit.SetFaction(Faction.Ally);
                    _allyUnits.Add(unit);
                    AttachFactionMarker(unit);
                }
    }

    /// <summary>
    ///     If <see cref="BattleLauncher.PendingSetup" /> is non-null, instantiate the
    ///     setup's PackedScenes into the matching unit containers. Called once at the
    ///     top of <see cref="LoadUnits" />, before the scene-tree iteration.
    /// </summary>
    /// <remarks>
    ///     Standalone launches of a battle scene (F6 on Test.tscn with no exploration
    ///     scene to launch from) have no pending setup, so this is a no-op and the
    ///     scene's pre-baked units are used as before. The two paths coexist —
    ///     designers can keep authoring static .tscn battles, while the runtime gets
    ///     procedural encounters via BattleLauncher.
    /// </remarks>
    private void SpawnUnitsFromPendingSetup()
    {
        BattleSetup? setup = BattleLauncher.Instance?.PendingSetup;
        if (setup is null) return;

        SpawnInto(setup.PlayerUnits, _playerUnitsContainer);
        SpawnInto(setup.AllyUnits, _alliedUnitsContainer);
        SpawnInto(setup.EnemyUnits, _enemyUnitsContainer);
        GD.Print($"GameManager: spawned setup '{setup.EncounterName}' "
            + $"(P={setup.PlayerUnits.Count}, A={setup.AllyUnits.Count}, E={setup.EnemyUnits.Count})");

        static void SpawnInto(System.Collections.Generic.List<PackedScene> scenes, Node? container)
        {
            if (container is null) return;
            foreach (PackedScene packed in scenes)
            {
                if (packed is null) continue;
                Node instance = packed.Instantiate();
                container.AddChild(instance);
            }
        }
    }

    /// <summary>Spawn a <see cref="FactionMarker" /> child on a unit and bind its colour.</summary>
    private static void AttachFactionMarker(UnitSystem unit)
    {
        // Skip if already present (defensive — LoadUnits should only run once per battle).
        if (unit.HasNode("FactionMarker")) return;
        FactionMarker marker = new() { Name = "FactionMarker" };
        unit.AddChild(marker);
        marker.Bind(unit.Faction);
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
        IUnitSystem mover = _turnManagerContainer.GetCurrentUnit();
        BattleNotifications.Post(
            $"{mover.UnitName} moves to ({cell.Item1}, {cell.Item3})",
            BattleNotifications.Severity.Info);
        _ = AnimateUnitMove(cell);
        _unitMoved = true;
        mover.MoveTo(cell.Item1, cell.Item2, cell.Item3, _mapSystemContainer);
        HideAllIndicators();
        _battleHud?.ContextInfo?.ShowMovement(0, mover.PossibleMovesRange, canMove: false);
        _battleInputSystemContainer.SetInputEnabled(true);
    }

    /// <summary>
    ///     Handles logic when a player selects a cell as a skill target.
    /// </summary>
    /// <param name="cell">The grid position selected as a target.</param>
    private void HandlePlayerSelectTarget((int, int, int) cell)
    {
        IUnitSystem? target;

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
                Warn("No unit on that cell.");
                _battleInputSystemContainer.SetInputEnabled(true);
                return;
            }
        }

        if (!_currentUnitReachableCellsForCurrentSelectedSkill.Contains(new Vector3I(cell.Item1, cell.Item2, cell.Item3)))
        {
            Warn("Target out of range.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (target is null)
        {
            Warn("No unit on that cell.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_selectedSkill == null)
        {
            Warn("No skill selected.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_selectedSkill.EffectType == AovDataStructures.EffectType.Revive && target.IsAlive)
        {
            Warn("Target is not dead.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_selectedSkill.EffectType != AovDataStructures.EffectType.Revive && !target.IsAlive)
        {
            Warn("Target is dead — pick another.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_selectedSkill.Cooldown > 0)
        {
            Warn($"{_selectedSkill.Name} is on cooldown.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        if (_turnManagerContainer.GetCurrentUnit().Mana < _selectedSkill.ManaCost)
        {
            Warn("Not enough mana.");
            _battleInputSystemContainer.SetInputEnabled(true);
            return;
        }

        // Faction validation — a SingleEnemy / AllEnemies skill must hit a hostile target;
        // a SingleAlly / AllAllies skill must hit a friendly (or self). Without this, the
        // click reaches UseSkill which then silently bails via GD.PrintErr, leaving the
        // player wondering why nothing happened.
        IUnitSystem caster = _turnManagerContainer.GetCurrentUnit();
        bool isHostile = caster.Faction.IsHostileTo(target.Faction);
        bool isFriendly = caster.Faction.IsFriendlyTo(target.Faction);
        switch (_selectedSkill.TargetType)
        {
            case AovDataStructures.TargetTypes.SingleEnemy:
            case AovDataStructures.TargetTypes.AllEnemies:
                if (!isHostile)
                {
                    Warn(target == caster
                        ? "You can't target yourself with an offensive skill."
                        : $"{target.UnitName} is on your side.");
                    _battleInputSystemContainer.SetInputEnabled(true);
                    return;
                }
                break;
            case AovDataStructures.TargetTypes.SingleAlly:
            case AovDataStructures.TargetTypes.AllAllies:
                if (!isFriendly)
                {
                    Warn($"{target.UnitName} is hostile — can't use a friendly skill on them.");
                    _battleInputSystemContainer.SetInputEnabled(true);
                    return;
                }
                break;
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
            BattleNotifications.Post("Defeat — your party has fallen.", BattleNotifications.Severity.Critical);
            ShowGameOverScreen();
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
            BattleNotifications.Post("Victory!", BattleNotifications.Severity.Positive);
            ShowVictoryScreen();
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
