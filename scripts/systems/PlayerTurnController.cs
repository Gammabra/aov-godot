using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.systems.skills;
using AshesOfVelsingrad.ui.hud;
using Godot;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     The phase of the active player's turn — drives input gating and HUD context.
/// </summary>
public enum PlayerTurnPhase
{
    /// <summary>Not the player's turn — input ignored.</summary>
    Inactive,

    /// <summary>Player can still move (or pick an action).</summary>
    Movement,

    /// <summary>Player has already moved this turn — they can still pick an action.</summary>
    ActionPhase,

    /// <summary>Player picked a skill and is selecting a target.</summary>
    TargetingSkill,
}

/// <summary>
///     Owns every input handler that runs during a player-faction turn: skill selection,
///     skill targeting, movement clicks, item use, hover overlay, cancel, pass, flee.
/// </summary>
/// <remarks>
///     <para>
///         Built once per battle by <c>GameManager</c> and fed events from:
///         <list type="bullet">
///             <item><see cref="TurnManager.OnPlayerTurn" /> → <see cref="BeginTurn" /></item>
///             <item><see cref="TurnManager.OnPlayerEndTurn" /> → <see cref="EndTurn" /></item>
///             <item><see cref="systems.BattleInputSystem.OnPassTurnPressed" /> → <see cref="HandlePass" /></item>
///             <item><see cref="systems.BattleInputSystem.OnMoveUnitToPressed" /> → <see cref="HandleMapClick" /></item>
///             <item><see cref="systems.BattleInputSystem.OnSelectedSkillPressed" /> → <see cref="HandleSkillHotkey" /></item>
///             <item><see cref="ActionMenu" /> button events → respective <c>Handle*</c> methods</item>
///             <item><c>GameManager._UnhandledInput</c> → <see cref="HandleInput" /></item>
///         </list>
///     </para>
///     <para>
///         Splitting this off makes <c>GameManager</c> a thin orchestrator and isolates the
///         player input state machine in one focused, testable class.
///     </para>
/// </remarks>
public sealed class PlayerTurnController
{
    #region Dependencies

    private readonly MapSystem _map;
    private readonly BattleHud _hud;
    private readonly IndicatorOverlay _indicators;
    private readonly systems.BattleInputSystem _input;
    private readonly IReadOnlyList<UnitSystem> _players;
    private readonly IReadOnlyList<UnitSystem> _allies;
    private readonly IReadOnlyList<UnitSystem> _enemies;
    private Camera3D? _camera;

    #endregion

    #region State

    /// <summary>Current phase of the active player turn.</summary>
    public PlayerTurnPhase Phase { get; private set; } = PlayerTurnPhase.Inactive;

    private SkillSystem? _selectedSkill;
    private bool _hasMovedThisTurn;
    private List<(int X, int Y, int Z)> _currentMoves = [];

    #endregion

    /// <summary>
    ///     Build a controller for the active battle.
    /// </summary>
    /// <param name="map">Active battle map.</param>
    /// <param name="hud">Active battle HUD.</param>
    /// <param name="indicators">Spawned indicator overlay.</param>
    /// <param name="input">Battle input system the player drives.</param>
    /// <param name="players">Player faction roster.</param>
    /// <param name="allies">Ally faction roster (may be empty).</param>
    /// <param name="enemies">Enemy faction roster.</param>
    public PlayerTurnController(
        MapSystem map,
        BattleHud hud,
        IndicatorOverlay indicators,
        systems.BattleInputSystem input,
        IReadOnlyList<UnitSystem> players,
        IReadOnlyList<UnitSystem> allies,
        IReadOnlyList<UnitSystem> enemies)
    {
        _map = map;
        _hud = hud;
        _indicators = indicators;
        _input = input;
        _players = players;
        _allies = allies;
        _enemies = enemies;
    }

    #region Turn lifecycle

    /// <summary>Called when <see cref="TurnManager.OnPlayerTurn" /> fires.</summary>
    public void BeginTurn()
    {
        UnitSystem? current = TurnManager.Active?.CurrentUnit;
        if (current is null) return;

        Phase = PlayerTurnPhase.Movement;
        _hasMovedThisTurn = false;
        _selectedSkill = null;

        _currentMoves = current.GetPossibleMoves(_map);
        _input.SetInputEnabled(true);
        _indicators.ShowMoveTiles(_currentMoves);
        _hud.ContextInfo?.ShowMovement(_currentMoves.Count, current.PossibleMovesRange, canMove: true);
        _hud.ActionMenu?.ShowCancel(false);
    }

    /// <summary>Called when <see cref="TurnManager.OnPlayerEndTurn" /> fires.</summary>
    public void EndTurn()
    {
        Phase = PlayerTurnPhase.Inactive;
        _selectedSkill = null;
        _currentMoves.Clear();
        _hasMovedThisTurn = false;
        _indicators.HideAll();
        _hud.ActionMenu?.ShowCancel(false);
        _input.SetInputEnabled(false);
    }

    /// <summary>
    ///     Re-render indicators / HUD after an in-turn state change (e.g. after a move,
    ///     after a cancel) without crossing turn boundaries.
    /// </summary>
    private void RefreshUiForCurrentPhase()
    {
        UnitSystem? current = TurnManager.Active?.CurrentUnit;
        if (current is null) return;

        if (Phase == PlayerTurnPhase.TargetingSkill)
        {
            _indicators.ShowTargetTiles(_enemies);
            if (_selectedSkill is not null)
                _hud.ContextInfo?.ShowSkill(_selectedSkill);
            return;
        }

        if (_hasMovedThisTurn)
        {
            _currentMoves.Clear();
            _hud.ContextInfo?.ShowMovement(0, current.PossibleMovesRange, canMove: false);
        }
        else
        {
            _currentMoves = current.GetPossibleMoves(_map);
            _hud.ContextInfo?.ShowMovement(_currentMoves.Count, current.PossibleMovesRange, canMove: true);
        }
        _input.SetInputEnabled(true);
        _indicators.ShowMoveTiles(_currentMoves);
        _hud.ActionMenu?.ShowCancel(false);
    }

    #endregion

    #region BattleInputSystem events

    /// <summary>Player pressed the "pass turn" hotkey.</summary>
    public void HandlePass()
    {
        TurnManager.Active?.GetCurrentUnit().PassTurn();
    }

    /// <summary>Player clicked a map cell. Routes to movement or targeting depending on phase.</summary>
    /// <param name="cell">Cell coordinates from <c>BattleInputSystem</c>.</param>
    public void HandleMapClick(Vector3I cell)
    {
        if (Phase == PlayerTurnPhase.TargetingSkill && _selectedSkill is not null)
        {
            ResolveSkillTarget(cell);
            return;
        }

        // Movement path.
        if (_hasMovedThisTurn)
        {
            // Already moved — re-enable input so they can pick another action.
            _input.SetInputEnabled(true);
            return;
        }

        if (!_currentMoves.Contains((cell.X, cell.Y, cell.Z)))
        {
            // Click missed a valid tile; just re-enable input.
            _input.SetInputEnabled(true);
            return;
        }

        UnitSystem current = TurnManager.Active!.GetCurrentUnit();
        try
        {
            if (!current.CanMoveTo(cell.X, cell.Y, cell.Z, _map))
            {
                _input.SetInputEnabled(true);
                return;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            _input.SetInputEnabled(true);
            return;
        }

        MoveCurrentUnitTo(cell);
        _hasMovedThisTurn = true;
        Phase = PlayerTurnPhase.ActionPhase;
        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
            "Move complete. Pick a skill, use an item, or pass.", LogSeverity.Positive));
        RefreshUiForCurrentPhase();
    }

    /// <summary>Player pressed a skill hotkey (1-5).</summary>
    /// <param name="slotIndex">Slot index in <c>UnitSystem.ActiveSkills</c>.</param>
    public void HandleSkillHotkey(int slotIndex)
    {
        UnitSystem? current = TurnManager.Active?.CurrentUnit;
        if (current is null || slotIndex >= current.ActiveSkills.Count)
        {
            GD.PrintErr($"PlayerTurnController: skill slot {slotIndex} out of range.");
            return;
        }
        SelectSkill(current.ActiveSkills[slotIndex]);
    }

    #endregion

    #region ActionMenu / SkillSelector / InventoryPanel events

    /// <summary>HUD "Attack" button → uses the first equipped skill as a basic attack.</summary>
    public void HandleAttackPressed()
    {
        UnitSystem? current = TurnManager.Active?.CurrentUnit;
        if (current is null || current.ActiveSkills.Count == 0) return;
        SelectSkill(current.ActiveSkills[0]);
    }

    /// <summary>HUD "Skill" button. The skill selector is always visible, so this is a no-op hook.</summary>
    public void HandleSkillPressed()
    {
        // Reserved for a future "open submenu" expansion.
    }

    /// <summary>HUD "Item" button → toggles the inventory panel.</summary>
    public void HandleItemPressed()
    {
        _hud.InventoryPanel?.Toggle();
    }

    /// <summary>HUD "Flee" button → aborts the battle as a retreat.</summary>
    public void HandleFleePressed()
    {
        TurnManager? turns = TurnManager.Active;
        if (turns is null) return;
        turns.RequestAbort(BattleOutcome.Retreat);
        // Unblock the WaitForActionAsync the loop is sitting on.
        turns.GetCurrentUnit().PassTurn();
    }

    /// <summary>HUD Cancel button (or Esc / right-click) → exit skill targeting.</summary>
    public void HandleCancelPressed()
    {
        if (Phase != PlayerTurnPhase.TargetingSkill) return;
        _selectedSkill = null;
        Phase = _hasMovedThisTurn ? PlayerTurnPhase.ActionPhase : PlayerTurnPhase.Movement;
        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage("Skill targeting cancelled.", LogSeverity.Info));
        RefreshUiForCurrentPhase();
    }

    /// <summary>SkillSelector slot click.</summary>
    /// <param name="slotIndex">Slot index 0-4.</param>
    /// <param name="skill">Resolved skill from the active unit's loadout.</param>
    public void HandleSkillSlotChosen(int slotIndex, DataDrivenSkill skill)
    {
        _ = slotIndex;
        SelectSkill(skill);
    }

    /// <summary>InventoryPanel "Use" click. Default behaviour: target the active unit.</summary>
    /// <param name="itemId">Item identifier from <c>ItemRegistry</c>.</param>
    public void HandleItemChosen(string itemId)
    {
        UnitSystem? user = TurnManager.Active?.CurrentUnit;
        if (user is null) return;
        user.UseItem(itemId, [user]);
    }

    #endregion

    #region Raw input (Esc / right-click / mouse motion)

    /// <summary>
    ///     Forwarded from <c>GameManager._UnhandledInput</c>: handles cancel keys + hover ray.
    /// </summary>
    /// <param name="event">Raw input event.</param>
    /// <returns><c>true</c> if the event was consumed and should be marked handled.</returns>
    public bool HandleInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            UpdateHoverFromMouse(motion.Position);
            return false;
        }

        if (Phase != PlayerTurnPhase.TargetingSkill) return false;

        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            HandleCancelPressed();
            return true;
        }
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
        {
            HandleCancelPressed();
            return true;
        }
        return false;
    }

    private void UpdateHoverFromMouse(Vector2 screenPos)
    {
        if (Phase is PlayerTurnPhase.Inactive)
        {
            _indicators.ShowHover(null);
            return;
        }

        Camera3D? cam = _camera;
        if (cam is null && Engine.GetMainLoop() is SceneTree tree)
            cam = tree.Root.GetViewport().GetCamera3D();
        if (cam is null) { _indicators.ShowHover(null); return; }
        _camera = cam;

        Vector3I? cell = _indicators.RaycastCell(cam, screenPos);
        if (cell is null) { _indicators.ShowHover(null); return; }

        bool eligible = Phase switch
        {
            PlayerTurnPhase.Movement => !_hasMovedThisTurn && _currentMoves.Contains((cell.Value.X, cell.Value.Y, cell.Value.Z)),
            PlayerTurnPhase.TargetingSkill => FindUnitAtCellXZ(cell.Value.X, cell.Value.Z) is not null,
            _ => false,
        };

        _indicators.ShowHover(eligible ? (cell.Value.X, 0, cell.Value.Z) : null);
    }

    #endregion

    #region Helpers

    /// <summary>Enter targeting mode for <paramref name="skill" />.</summary>
    /// <param name="skill">Skill to queue.</param>
    private void SelectSkill(SkillSystem skill)
    {
        _selectedSkill = skill;
        Phase = PlayerTurnPhase.TargetingSkill;

        string cost = BuildSkillCostBlurb(skill);
        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
            $"Choose a target for {skill.Name}{cost}. Right-click or press Esc to cancel.",
            LogSeverity.Info));

        _indicators.ShowTargetTiles(_enemies);
        _hud.ActionMenu?.ShowCancel(true);
        _hud.ContextInfo?.ShowSkill(skill);
    }

    private void ResolveSkillTarget(Vector3I cell)
    {
        if (_selectedSkill is null) return;

        UnitSystem? clicked = FindUnitAtCellXZ(cell.X, cell.Z);
        if (clicked is null)
        {
            GD.Print($"No unit at cell ({cell.X}, *, {cell.Z}) — skill cast cancelled.");
            HandleCancelPressed();
            return;
        }

        UnitSystem source = TurnManager.Active!.GetCurrentUnit();
        UseSkill(source, clicked, _selectedSkill);
        _selectedSkill = null;
        Phase = _hasMovedThisTurn ? PlayerTurnPhase.ActionPhase : PlayerTurnPhase.Movement;
        _indicators.HideAll();
        _hud.ActionMenu?.ShowCancel(false);
    }

    /// <summary>
    ///     Apply <paramref name="skill" /> to the targets implied by its <see cref="SkillSystem.TargetType" />,
    ///     resolved against the current rosters.
    /// </summary>
    private void UseSkill(UnitSystem source, UnitSystem target, SkillSystem skill)
    {
        (List<UnitSystem> friendly, List<UnitSystem> hostile) = GetFactionsFor(source);
        List<UnitSystem> targets = [];

        switch (skill.TargetType)
        {
            case TargetTypes.SingleAlly:
                if (!friendly.Contains(target))
                {
                    GD.PrintErr($"Ally unit {target.UnitName} not found.");
                    return;
                }
                targets.Add(target);
                break;

            case TargetTypes.SingleEnemy:
                if (!hostile.Contains(target))
                {
                    GD.PrintErr($"Enemy unit {target.UnitName} not found.");
                    return;
                }
                targets.Add(target);
                break;

            case TargetTypes.AllAllies:
                targets.AddRange(friendly);
                break;

            case TargetTypes.AllEnemies:
                targets.AddRange(hostile);
                break;
        }

        source.Play(targets, _map, skill);
    }

    /// <summary>
    ///     Snapshot of the active unit's friendly + hostile rosters, regardless of which
    ///     faction is currently acting.
    /// </summary>
    private (List<UnitSystem> friendly, List<UnitSystem> hostile) GetFactionsFor(UnitSystem unit)
    {
        return unit.Faction switch
        {
            Faction.Player => ([.._players, .._allies], [.._enemies]),
            Faction.Ally => ([.._allies, .._players], [.._enemies]),
            Faction.Enemy => ([.._enemies], [.._players, .._allies]),
            _ => ([.._players], [.._enemies]),
        };
    }

    /// <summary>Move the active unit to <paramref name="cell" /> in world + grid space.</summary>
    private void MoveCurrentUnitTo(Vector3I cell)
    {
        Vector3 worldPos = _map.MapToLocal(cell);
        worldPos.Y += _map.CellSize.Y * 0.5f;

        UnitSystem current = TurnManager.Active!.GetCurrentUnit();
        current.GlobalPosition = worldPos;
        current.MoveTo(cell.X, cell.Y, cell.Z, _map);
    }

    /// <summary>
    ///     Return the first alive combatant whose grid X/Z column matches the click, ignoring Y.
    /// </summary>
    /// <param name="x">Grid X.</param>
    /// <param name="z">Grid Z.</param>
    /// <returns>Matching unit or null.</returns>
    private UnitSystem? FindUnitAtCellXZ(int x, int z)
    {
        foreach (UnitSystem u in EnumerateAllCombatants())
        {
            if (!u.IsAlive) continue;
            (int, int, int)? pos;
            try
            {
                pos = _map.GetUnitPosition(u);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }
            if (pos is { } p && p.Item1 == x && p.Item3 == z)
                return u;
        }
        return null;
    }

    private IEnumerable<UnitSystem> EnumerateAllCombatants()
    {
        foreach (UnitSystem u in _players) yield return u;
        foreach (UnitSystem u in _allies) yield return u;
        foreach (UnitSystem u in _enemies) yield return u;
    }

    /// <summary>Build a "(MP X, CD Y)" suffix for a skill, omitting parts when zero.</summary>
    private static string BuildSkillCostBlurb(SkillSystem skill)
    {
        bool mp = skill.ManaCost > 0;
        bool cd = skill.TotalCooldown > 0;
        if (!mp && !cd) return string.Empty;
        if (mp && cd) return $"  (MP {skill.ManaCost:F0}, CD {skill.TotalCooldown})";
        return mp ? $"  (MP {skill.ManaCost:F0})" : $"  (CD {skill.TotalCooldown})";
    }

    #endregion
}
