using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.ai;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.status_effects;
using AshesOfVelsingrad.systems.status_effects.effects;
using AshesOfVelsingrad.utilities;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     Defines the possible turn states in the battle system.
/// </summary>
public enum TurnState
{
    /// <summary>The player's turn to act.</summary>
    PlayerTurn,

    /// <summary>An allied (guest) AI unit's turn.</summary>
    AllyTurn,

    /// <summary>The enemy's turn to act.</summary>
    EnemyTurn,

    /// <summary>Idle state while waiting for setup or transitions.</summary>
    Waiting
}

/// <summary>
///     Manages the turn-based battle flow between player, allied, and enemy units.
/// </summary>
/// <remarks>
///     <para>
///         This is the single source of truth for "whose turn is it". It owns:
///         <list type="bullet">
///             <item>The initiative queue, sorted descending by <see cref="UnitSystem.BaseSpeed" />.</item>
///             <item>Per-turn cooldown ticks and status-effect ticks.</item>
///             <item>AI dispatch via <see cref="AiRegistry" /> for non-player factions.</item>
///             <item>Victory / defeat evaluation against the active <see cref="VictoryCondition" />.</item>
///             <item>Publication of every turn event on <see cref="BattleEventBus" /> so HUD widgets react.</item>
///         </list>
///     </para>
///     <para>
///         <b>Backward compatibility.</b> The legacy 2-argument
///         <see cref="InitializeTurnOrder(List{UnitSystem}, List{UnitSystem})" /> overload, the
///         <see cref="OnPlayerTurn" /> / <see cref="OnPlayerEndTurn" /> events, and
///         <see cref="StartBattle" /> are preserved so <c>GameManager</c> and other existing
///         call sites keep working.
///     </para>
/// </remarks>
public partial class TurnManager : BaseManager
{
    #region Private Fields

    private readonly StatusEffectSystem _statusEffectSystem = new();
    private TurnState _currentTurnState = TurnState.Waiting;
    private int _turn;
    private int _currentIndex;
    private List<UnitSystem> _turnOrder = [];
    private List<UnitSystem> _playerUnits = [];
    private List<UnitSystem> _allyUnits = [];
    private List<UnitSystem> _enemyUnits = [];
    private VictoryCondition _victoryCondition = new DefeatAllEnemiesCondition();
    private TaskCompletionSource<BattleResult>? _battleCompletion;
    private bool _abortRequested;
    private BattleOutcome _abortOutcome = BattleOutcome.Aborted;
    private bool _isRunning;

    #endregion

    #region Singleton

    /// <summary>
    ///     The active <see cref="TurnManager" /> instance. Both names resolve to the same
    ///     singleton; <see cref="Active" /> reads better at call sites and is what
    ///     <see cref="BattleLauncher" /> uses, while the inherited <see cref="BaseManager.Instance" />
    ///     is kept consistent so <see cref="BaseManager._ExitTree" />'s cleanup runs.
    /// </summary>
    public static TurnManager? Active { get; private set; }

    #endregion

    #region Public Properties

    /// <summary>The 1-based global turn counter.</summary>
    public int CurrentTurnNumber => _turn;

    /// <summary>The unit currently taking its turn, or null if no battle is running.</summary>
    public UnitSystem? CurrentUnit =>
        _turnOrder.Count > 0 && _currentIndex >= 0 && _currentIndex < _turnOrder.Count
            ? _turnOrder[_currentIndex]
            : null;

    /// <summary>Read-only snapshot of the initiative queue.</summary>
    public IReadOnlyList<UnitSystem> TurnOrder => _turnOrder;

    /// <summary>Triggered when a player-faction unit's turn begins. Legacy event, kept for GameManager.</summary>
    public event Action? OnPlayerTurn;

    /// <summary>Triggered when a player-faction unit's turn ends. Legacy event, kept for GameManager.</summary>
    public event Action? OnPlayerEndTurn;

    #endregion

    #region Class Initialization

    /// <inheritdoc />
    protected override void Initialize()
    {
        if (Active != null && Active != this)
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Active = this;
        // Note: we deliberately do NOT touch BaseManager.Instance. That static is shared
        // across every BaseManager subclass, and assigning to it here would cause the
        // duplicate-detection check in BaseManager._Ready to falsely trip for any other
        // manager not parented directly to the scene root (e.g. GameManager).
        GD.Print("TurnManager initialized successfully");
    }

    #endregion

    #region Public API — turn order setup

    /// <summary>
    ///     Initialize the turn order with player and enemy units. <b>Backward-compatible 2-arg overload.</b>
    /// </summary>
    /// <param name="playerUnits">All player-controlled units.</param>
    /// <param name="enemyUnits">All enemy units.</param>
    public void InitializeTurnOrder(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
    {
        InitializeTurnOrder(playerUnits, [], enemyUnits);
    }

    /// <summary>
    ///     Initialize the turn order with three factions. Allies are AI-controlled friendly units
    ///     (recruited mercenaries, summoned creatures, scripted guests).
    /// </summary>
    /// <param name="playerUnits">Player-controlled units.</param>
    /// <param name="allyUnits">AI-controlled friendly guest units (may be empty).</param>
    /// <param name="enemyUnits">AI-controlled hostile units.</param>
    /// <remarks>
    ///     The order is determined by each unit's <see cref="UnitSystem.BaseSpeed" />, descending.
    ///     Speed ties are broken by stable sort: player units win first, then allies, then enemies.
    ///     Also tags every unit with the correct <see cref="Faction" /> if not already set.
    /// </remarks>
    public void InitializeTurnOrder(
        List<UnitSystem> playerUnits,
        List<UnitSystem> allyUnits,
        List<UnitSystem> enemyUnits)
    {
        _playerUnits = playerUnits;
        _allyUnits = allyUnits;
        _enemyUnits = enemyUnits;

        // Defensive faction tagging: GameManager already does this, but if a caller forgets,
        // we make sure the AI dispatch picks the right strategy.
        foreach (UnitSystem u in playerUnits)
            if (u.Faction != Faction.Player) u.AssignFaction(Faction.Player);
        foreach (UnitSystem u in allyUnits)
            if (u.Faction != Faction.Ally) u.AssignFaction(Faction.Ally);
        foreach (UnitSystem u in enemyUnits)
            if (u.Faction != Faction.Enemy) u.AssignFaction(Faction.Enemy);

        _turnOrder = playerUnits
            .Concat(allyUnits)
            .Concat(enemyUnits)
            .OrderByDescending(u => u.BaseSpeed)
            .ToList();

        _currentIndex = 0;
        _turn = 0;

        BattleEventBus.Instance?.Publish(new BattleEvents.TurnOrderChanged(_turnOrder));
        GD.Print("Turn order initialized:");
        foreach (UnitSystem unit in _turnOrder)
            GD.Print($"  {unit.UnitName} ({unit.Faction}, Speed: {unit.BaseSpeed})");
    }

    /// <summary>Set the rule that decides battle outcome.</summary>
    /// <param name="condition">The condition. Defaults to <see cref="DefeatAllEnemiesCondition" />.</param>
    public void SetVictoryCondition(VictoryCondition condition)
    {
        _victoryCondition = condition;
    }

    #endregion

    #region Public API — battle loop

    /// <summary>
    ///     Start the battle and run the loop until victory / defeat / abort.
    ///     <b>Legacy entry point</b> — does not return a result. Prefer <see cref="RunBattleLoop" />.
    /// </summary>
    /// <returns>A task that completes when the battle ends.</returns>
    public Task StartBattle()
    {
        return RunBattleLoop();
    }

    /// <summary>
    ///     Run the turn loop and return the final outcome.
    /// </summary>
    /// <returns>The <see cref="BattleResult" />.</returns>
    public Task<BattleResult> RunBattleLoop()
    {
        if (_isRunning && _battleCompletion is not null)
            return _battleCompletion.Task;

        _battleCompletion = new TaskCompletionSource<BattleResult>();
        _abortRequested = false;
        _isRunning = true;

        // Fire-and-forget — we surface the result through the TaskCompletionSource.
        _ = ProcessTurns();
        return _battleCompletion.Task;
    }

    /// <summary>Forcefully end the running battle.</summary>
    /// <param name="outcome">The outcome to report.</param>
    public void RequestAbort(BattleOutcome outcome = BattleOutcome.Aborted)
    {
        if (!_isRunning) return;
        _abortRequested = true;
        _abortOutcome = outcome;
    }

    /// <summary>
    ///     Get the unit currently taking its turn. <b>Legacy</b>. Prefer <see cref="CurrentUnit" />.
    /// </summary>
    /// <returns>The active unit; if none, falls back to the first unit.</returns>
    public UnitSystem GetCurrentUnit() => CurrentUnit ?? _turnOrder[0];

    #endregion

    #region Battle loop internals

    private async Task ProcessTurns()
    {
        try
        {
            while (true)
            {
                if (_abortRequested)
                {
                    Complete(_abortOutcome);
                    return;
                }

                if (_turnOrder.Count == 0)
                {
                    Complete(BattleOutcome.Aborted);
                    return;
                }

                _currentIndex = ClampIndex(_currentIndex);
                UnitSystem unit = _turnOrder[_currentIndex];

                // Skip dead units.
                if (!unit.IsAlive)
                {
                    AdvanceIndex();
                    continue;
                }

                _turn++;

                // Pre-turn hooks.
                unit.BeginTurn();
                _currentTurnState = StateForFaction(unit.Faction);

                BattleEventBus.Instance?.Publish(new BattleEvents.TurnStarted(unit, _turn, unit.Faction));
                BattleEventBus.Instance?.Publish(new BattleEvents.TurnOrderChanged(GetUpcoming()));

                // Drive the turn.
                if (unit.HasEffect<StunEffect>())
                {
                    BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
                        $"{unit.UnitName} is stunned and skips their turn.", LogSeverity.Info));
                    unit.PassTurn();
                }
                else if (unit.Faction == Faction.Player)
                {
                    OnPlayerTurn?.Invoke();
                    await unit.WaitForActionAsync();
                    OnPlayerEndTurn?.Invoke();
                }
                else
                {
                    await RunAiTurn(unit);
                }

                BattleEventBus.Instance?.Publish(new BattleEvents.TurnEnded(unit));

                // Post-turn: tick this unit's effects only. We deliberately do NOT tick
                // every combatant on every turn — that would cause a stun(1) applied during
                // an enemy turn to expire before the player's turn ever starts (the famous
                // "self-purifying status" bug). Per-unit ticking preserves the standard
                // convention: an effect with duration N applies for N of the bearer's turns.
                TickEffectsOn(unit);

                // Victory check.
                BattleStateSnapshot snapshot = new(_playerUnits, _allyUnits, _enemyUnits, _turn);
                if (_victoryCondition.IsDefeatReached(in snapshot))
                {
                    Complete(BattleOutcome.Defeat);
                    return;
                }
                if (_victoryCondition.IsVictoryReached(in snapshot))
                {
                    Complete(BattleOutcome.Victory);
                    return;
                }

                AdvanceIndex();
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"TurnManager loop crashed: {e}");
            _isRunning = false;
            _battleCompletion?.TrySetException(e);
        }
    }

    private async Task RunAiTurn(UnitSystem unit)
    {
        IUnitAi? ai = AiRegistry.ResolveFor(unit);
        if (ai is null)
        {
            unit.PassTurn();
            return;
        }

        UnitSystem[] allies = _turnOrder
            .Where(u => u != unit && u.IsAlive && u.Faction.IsFriendlyTo(unit.Faction))
            .ToArray();
        UnitSystem[] enemies = _turnOrder
            .Where(u => u.IsAlive && u.Faction.IsHostileTo(unit.Faction))
            .ToArray();

        await ai.TakeTurnAsync(unit, allies, enemies, MapSystem.Instance);
    }

    private void Complete(BattleOutcome outcome)
    {
        _isRunning = false;

        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
            outcome switch
            {
                BattleOutcome.Victory => "Victory!",
                BattleOutcome.Defeat => "Defeat...",
                BattleOutcome.Retreat => "You retreated.",
                _ => "Battle ended."
            },
            outcome == BattleOutcome.Victory ? LogSeverity.Positive :
            outcome == BattleOutcome.Defeat ? LogSeverity.Critical : LogSeverity.Info));

        BattleResult result = new()
        {
            Outcome = outcome,
            ExperienceGained = outcome == BattleOutcome.Victory
                ? BattleLauncher.Instance?.ActiveConfig?.BaseExperienceReward ?? 0
                : 0,
            TurnsElapsed = _turn,
        };

        BattleEventBus.Instance?.Publish(new BattleEvents.BattleEnded(result));
        _battleCompletion?.TrySetResult(result);
    }

    private void AdvanceIndex()
    {
        if (_turnOrder.Count == 0) return;
        _currentIndex = (_currentIndex + 1) % _turnOrder.Count;
    }

    private int ClampIndex(int idx)
    {
        if (_turnOrder.Count == 0) return 0;
        return ((idx % _turnOrder.Count) + _turnOrder.Count) % _turnOrder.Count;
    }

    private static TurnState StateForFaction(Faction faction) => faction switch
    {
        Faction.Player => TurnState.PlayerTurn,
        Faction.Ally => TurnState.AllyTurn,
        _ => TurnState.EnemyTurn
    };

    /// <summary>
    ///     Tick every active status effect on a single unit, removing expired ones.
    /// </summary>
    /// <param name="unit">The unit whose effects should advance one tick.</param>
    /// <remarks>
    ///     Permanent effects (Duration == <see cref="Constants.PermanentStatusEffect" />)
    ///     are skipped. Iterating over a snapshot list lets effects safely call
    ///     <see cref="UnitSystem.RemoveEffect" /> from inside their callbacks if needed.
    /// </remarks>
    private static void TickEffectsOn(UnitSystem unit)
    {
        if (!unit.IsAlive) return;
        foreach (StatusEffect effect in unit.GetActiveEffects().ToList())
        {
            if (effect.Duration == Constants.PermanentStatusEffect)
                continue;
            effect.OnTurnPassed(unit);
            if (effect.Duration <= 0)
                unit.RemoveEffect(effect);
        }
    }

    /// <summary>
    ///     Returns up to 8 alive units in activation order, starting with the active one.
    /// </summary>
    /// <returns>Slice of upcoming units for the HUD turn-queue.</returns>
    private IReadOnlyList<UnitSystem> GetUpcoming()
    {
        const int look = 8;
        List<UnitSystem> next = [];
        if (_turnOrder.Count == 0) return next;
        int n = _turnOrder.Count;
        for (int i = 0; i < n * 2 && next.Count < look; i++)
        {
            UnitSystem u = _turnOrder[(_currentIndex + i) % n];
            if (u.IsAlive)
                next.Add(u);
        }
        return next;
    }

    #endregion
}
