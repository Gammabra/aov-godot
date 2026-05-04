using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     <see cref="GameManager" /> partial — indicator updates, hover ray-cast, cancel,
///     warnings, A* tween-animated movement, and the HUD event handlers wired by
///     <see cref="GameManagerHud.WireHudEvents" />.
/// </summary>
public partial class GameManager
{
    private Camera3D? _cachedCamera;

    #region Indicator updates

    /// <summary>Light green tiles for the active unit's reachable cells.</summary>
    /// <param name="tiles">Move-eligible cells.</param>
    protected void ShowMoveIndicators(IReadOnlyList<(int X, int Y, int Z)> tiles)
        => _indicators?.ShowMoveTiles(tiles);

    /// <summary>Light red tiles where the queued skill can land.</summary>
    /// <param name="cells">Reachable cells per the skill's range.</param>
    protected void ShowTargetIndicators(IEnumerable<Vector3I> cells)
    {
        if (_indicators is null) return;
        var converted = new List<(int, int, int)>();
        foreach (Vector3I c in cells) converted.Add((c.X, c.Y, c.Z));
        _indicators.ShowTargetTiles(converted);
    }

    /// <summary>Hide every indicator overlay (move + target + hover).</summary>
    protected void HideAllIndicators() => _indicators?.HideAll();

    #endregion

    #region HUD event handlers

    private void OnHudAttackPressed()
    {
        // Treat "Attack" as "select skill 0" — the unit's first skill slot.
        if (_turnManagerContainer is null) return;
        IUnitSystem unit = _turnManagerContainer.GetCurrentUnit();
        if (unit.ActiveSkills.Count == 0)
        {
            BattleNotifications.Post("No skill in slot 1.", BattleNotifications.Severity.Negative);
            return;
        }
        PlayerSelectedSkill(0);
    }

    private void OnHudSkillPressed() { /* kept for future submenu UX */ }

    private void OnHudMovePressed() => PlayerSelectedMove();

    private void OnHudPassPressed() => PlayerPassedUnitTurn();

    private void OnHudCancelPressed() => CancelSkillTargeting();

    private void OnHudSkillSlotChosen(int slotIndex, ISkillSystem _)
    {
        PlayerSelectedSkill(slotIndex);
    }

    #endregion

    #region Cancel + warnings

    /// <summary>Exit skill-targeting mode and return to movement context.</summary>
    public void CancelSkillTargeting()
    {
        if (_clickOnMapContext != AovDataStructures.ClickOnMapContext.SelectUnitTarget) return;
        _clickOnMapContext = AovDataStructures.ClickOnMapContext.MoveUnit;
        _selectedSkill = null;
        _currentUnitReachableCellsForCurrentSelectedSkill.Clear();
        _battleHud?.ActionMenu?.ShowCancel(false);
        _battleHud?.ContextInfo?.ShowMovement(_currentUnitPossibleMoves.Count, GetActiveMoveBudget(), canMove: !_unitMoved);
        BattleNotifications.Post("Skill targeting cancelled.", BattleNotifications.Severity.Info);

        if (_unitMoved) HideAllIndicators();
        else ShowMoveIndicators(_currentUnitPossibleMoves);

        _battleInputSystemContainer?.SetInputEnabled(true);
    }

    /// <summary>Helper used by ContextInfoPanel updates.</summary>
    private int GetActiveMoveBudget()
    {
        try { return _turnManagerContainer?.GetCurrentUnit().PossibleMovesRange ?? 0; }
        catch { return 0; }
    }

    /// <summary>Post a negative-severity warning to the log (e.g. "Out of range").</summary>
    /// <param name="message">User-facing text.</param>
    protected static void Warn(string message)
        => BattleNotifications.Post(message, BattleNotifications.Severity.Negative);

    #endregion

    #region Hover ray + cancel keys (Esc / right-click)

    /// <inheritdoc />
    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (@event is InputEventMouseMotion motion)
        {
            UpdateHoverFromMouse(motion.Position);
            return;
        }

        if (_clickOnMapContext == AovDataStructures.ClickOnMapContext.SelectUnitTarget)
        {
            if ((@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
                || (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }))
            {
                CancelSkillTargeting();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void UpdateHoverFromMouse(Vector2 screenPos)
    {
        if (_indicators is null || !_isPlayerTurn) { _indicators?.ShowHover(null); return; }

        Camera3D? cam = _cachedCamera ?? GetViewport().GetCamera3D();
        if (cam is null) { _indicators.ShowHover(null); return; }
        _cachedCamera = cam;

        Vector3I? cell = _indicators.RaycastCell(cam, screenPos);
        if (cell is null) { _indicators.ShowHover(null); return; }

        bool eligible = _clickOnMapContext switch
        {
            AovDataStructures.ClickOnMapContext.MoveUnit =>
                !_unitMoved && _currentUnitPossibleMoves.Contains((cell.Value.X, cell.Value.Y, cell.Value.Z)),
            AovDataStructures.ClickOnMapContext.SelectUnitTarget =>
                _currentUnitReachableCellsForCurrentSelectedSkill.Contains(new Vector3I(cell.Value.X, cell.Value.Y, cell.Value.Z)),
            _ => false,
        };

        _indicators.ShowHover(eligible ? (cell.Value.X, 0, cell.Value.Z) : null);
    }

    #endregion

    #region Animated movement (A* + tween)

    /// <summary>
    ///     Move the active unit to <paramref name="cell" /> with an A* path-followed tween animation.
    /// </summary>
    /// <param name="cell">Destination cell.</param>
    /// <returns>A task that completes once the animation finishes.</returns>
    protected async Task AnimateUnitMove((int X, int Y, int Z) cell)
    {
        if (_mapSystemContainer is null || _turnManagerContainer is null) return;

        IUnitSystem activeUnit = _turnManagerContainer.GetCurrentUnit();
        (int, int, int)? from = _mapSystemContainer.GetUnitPosition(activeUnit);
        if (from is null) { TeleportUnitTo(activeUnit, cell); return; }

        List<(int X, int Y, int Z)>? path = Pathfinder.FindPath(_mapSystemContainer, from.Value, cell);
        if (path is null || path.Count == 0)
        {
            TeleportUnitTo(activeUnit, cell);
            return;
        }

        if (activeUnit is not CharacterBody3D body) return;

        foreach ((int x, int y, int z) step in path)
        {
            Vector3 worldPos = ((GridMap)_mapSystemContainer).MapToLocal(new Vector3I(step.x, step.y, step.z));
            worldPos.Y += ((GridMap)_mapSystemContainer).CellSize.Y * 0.5f;
            Tween tween = body.CreateTween();
            tween.TweenProperty(body, "global_position", worldPos, 0.14);
            await ToSignal(tween, Tween.SignalName.Finished);
        }
    }

    private void TeleportUnitTo(IUnitSystem unit, (int X, int Y, int Z) cell)
    {
        if (unit is not CharacterBody3D body || _mapSystemContainer is not GridMap grid) return;
        Vector3 worldPos = grid.MapToLocal(new Vector3I(cell.X, cell.Y, cell.Z));
        worldPos.Y += grid.CellSize.Y * 0.5f;
        body.GlobalPosition = worldPos;
    }

    #endregion
}
