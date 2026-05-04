using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using Godot;
using Faction = AshesOfVelsingrad.Systems.Faction;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     Service that turns a <see cref="BattleConfig" /> into a running battle.
/// </summary>
/// <remarks>
///     <para>
///         Designed to be called from the exploration layer via a single line:
///     </para>
///     <code>
///         await BattleLauncher.Instance.StartBattle(myConfig);
///     </code>
///     <para>
///         The launcher is responsible for: loading the map scene, instantiating
///         allies and enemies, placing the player party, plumbing the
///         <see cref="TurnManager" />, raising <see cref="BattleEvents.BattleStarted" />,
///         awaiting the outcome, and emitting <see cref="BattleEvents.BattleEnded" />.
///     </para>
/// </remarks>
public sealed partial class BattleLauncher : Node
{
    #region Public Properties

    /// <summary>
    ///     Singleton instance, set once the AutoLoad node is ready.
    /// </summary>
    public static BattleLauncher? Instance { get; private set; }

    /// <summary>
    ///     The currently-running battle config, or null if no battle is active.
    /// </summary>
    public BattleConfig? ActiveConfig { get; private set; }

    /// <summary>
    ///     The player units participating in the active battle.
    ///     Empty when no battle is running.
    /// </summary>
    public IReadOnlyList<UnitSystem> PlayerUnits => _playerUnits;

    /// <summary>
    ///     The ally units participating in the active battle.
    /// </summary>
    public IReadOnlyList<UnitSystem> AllyUnits => _allyUnits;

    /// <summary>
    ///     The enemy units participating in the active battle.
    /// </summary>
    public IReadOnlyList<UnitSystem> EnemyUnits => _enemyUnits;

    /// <summary>
    ///     Whether a battle is currently in progress.
    /// </summary>
    public bool IsBattleActive { get; private set; }

    #endregion

    #region Private Fields

    private readonly List<UnitSystem> _playerUnits = [];
    private readonly List<UnitSystem> _allyUnits = [];
    private readonly List<UnitSystem> _enemyUnits = [];

    /// <summary>
    ///     Default condition cached so we don't allocate one every time
    ///     a config is missing one.
    /// </summary>
    private readonly DefeatAllEnemiesCondition _defaultCondition = new();

    #endregion

    #region Class Initialization

    /// <inheritdoc />
    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {nameof(BattleLauncher)} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Start a battle from a config and await its end.
    /// </summary>
    /// <param name="config">Encounter description.</param>
    /// <param name="playerUnits">
    ///     Units to use as the player party. Pass the runtime party roster from
    ///     the exploration layer; the launcher will place them on the map.
    /// </param>
    /// <returns>The battle outcome once the loop terminates.</returns>
    public async Task<BattleResult> StartBattle(BattleConfig config, IReadOnlyList<UnitSystem> playerUnits)
    {
        if (IsBattleActive)
        {
            GD.PrintErr("Cannot start a battle: another battle is already running.");
            return new BattleResult { Outcome = BattleOutcome.Aborted };
        }

        ActiveConfig = config;
        IsBattleActive = true;

        ResetRosters();
        _playerUnits.AddRange(playerUnits);

        // Spawn ally + enemy units from the config.
        SpawnNonPlayerUnits(config);

        // Place units on the map (delegated to MapSystem).
        if (MapSystem.Instance is not null)
        {
            List<IUnitSystem> friendly = [.._playerUnits, .._allyUnits];
            List<IUnitSystem> enemies = _enemyUnits.Cast<IUnitSystem>().ToList();
            MapSystem.Instance.PlaceUnits(friendly, enemies);
        }
        else
        {
            GD.PrintErr("BattleLauncher: no MapSystem.Instance available; units were not placed.");
        }

        // Initialise turn order and announce the battle has started.
        TurnManager? turn = TurnManager.Active;
        if (turn is null)
        {
            GD.PrintErr("BattleLauncher: TurnManager.Active is null; battle aborted.");
            BattleResult aborted = new() { Outcome = BattleOutcome.Aborted };
            FinishBattle(aborted);
            return aborted;
        }

        var playerAndAllies = new List<IUnitSystem>();
        playerAndAllies.AddRange(_playerUnits);
        playerAndAllies.AddRange(_allyUnits);
        var enemiesAsInterface = _enemyUnits.Cast<IUnitSystem>().ToList();
        turn.InitializeTurnOrder(playerAndAllies, enemiesAsInterface);
        turn.SetVictoryCondition(config.VictoryCondition ?? _defaultCondition);

        BattleEventBus.Instance?.Publish(new BattleEvents.BattleStarted(_playerUnits, _allyUnits, _enemyUnits));
        if (!string.IsNullOrEmpty(config.IntroText))
            BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(config.IntroText));

        // Await the actual loop. TurnManager terminates when victory/defeat reached.
        BattleResult result = await turn.RunBattleLoop();
        FinishBattle(result);
        return result;
    }

    /// <summary>
    ///     Forcefully end the running battle (e.g. for cutscenes that interrupt combat).
    /// </summary>
    /// <param name="outcome">The outcome to report.</param>
    public void AbortBattle(BattleOutcome outcome = BattleOutcome.Aborted)
    {
        if (!IsBattleActive)
            return;
        TurnManager.Active?.RequestAbort(outcome);
    }

    #endregion

    #region Private Methods

    private void ResetRosters()
    {
        _playerUnits.Clear();
        _allyUnits.Clear();
        _enemyUnits.Clear();
    }

    private void SpawnNonPlayerUnits(BattleConfig config)
    {
        foreach (UnitSpawn spawn in config.AlliedSpawns)
        {
            UnitSystem? unit = InstantiateUnit(spawn, Faction.Ally);
            if (unit is not null)
                _allyUnits.Add(unit);
        }

        foreach (UnitSpawn spawn in config.EnemySpawns)
        {
            UnitSystem? unit = InstantiateUnit(spawn, Faction.Enemy);
            if (unit is not null)
                _enemyUnits.Add(unit);
        }
    }

    private UnitSystem? InstantiateUnit(UnitSpawn spawn, Faction faction)
    {
        if (string.IsNullOrEmpty(spawn.UnitScenePath))
        {
            GD.PrintErr($"BattleLauncher: spawn {faction} skipped — empty scene path.");
            return null;
        }

        PackedScene? scene = ResourceLoader.Load<PackedScene>(spawn.UnitScenePath);
        if (scene is null)
        {
            GD.PrintErr($"BattleLauncher: failed to load unit scene {spawn.UnitScenePath}.");
            return null;
        }

        UnitSystem? unit = scene.Instantiate<UnitSystem>();
        if (unit is null)
        {
            GD.PrintErr($"BattleLauncher: scene {spawn.UnitScenePath} root is not a UnitSystem.");
            return null;
        }

        unit.AssignFaction(faction);
        if (!string.IsNullOrEmpty(spawn.DisplayNameOverride))
            unit.OverrideDisplayName(spawn.DisplayNameOverride);

        // Attach to scene tree so _Ready runs.
        GetTree().CurrentScene.AddChild(unit);

        return unit;
    }

    private void FinishBattle(BattleResult result)
    {
        BattleEventBus.Instance?.Publish(new BattleEvents.BattleEnded(result));
        IsBattleActive = false;
        ActiveConfig = null;
    }

    #endregion
}
