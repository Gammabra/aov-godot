using AshesOfVelsingrad.Interfaces;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using Godot;

namespace AshesOfVelsingrad.data.npc;

/// <summary>
///     Hostile NPC that triggers a battle when the player interacts with them.
/// </summary>
/// <remarks>
///     <para>
///         Hands off to <see cref="BattleLauncher" /> rather than calling
///         <c>ChangeSceneToFile</c> directly — that's what gives Forfeit something to
///         return to. The launcher captures the current scene path + the interacting
///         player's <c>GlobalPosition</c> at the moment of <see cref="Interact" />, then
///         restores both when the player presses Forfeit on the GameOverScreen (or
///         Continue on the VictoryScreen).
///     </para>
/// </remarks>
public partial class Soldier : NpcSystem, IInteractable
{
	[Export]
	private NodePath? _interactTextPath;

	/// <summary>
	///     Path to the battle scene's <c>.tscn</c>. Kept as a string for backward compat
    ///     with the existing scene authoring; <see cref="Interact" /> loads it as a
    ///     <see cref="PackedScene" /> at trigger time.
    /// </summary>
    [Export]
    private string? _battleSceneToCharge;

    /// <summary>
	///     Player-controlled units to spawn in the battle scene's <c>PlayerUnits</c>
	///     container. Wire one PackedScene per character via the inspector.
	/// </summary>
	[Export]
	private Godot.Collections.Array<PackedScene> _playerUnits = new();

	/// <summary>
	///     AI-friendly guests (recruited mercs, summons) — spawned into <c>AlliedUnits</c>.
	///     Wire via the inspector; leave empty for solo encounters.
	/// </summary>
	[Export]
	private Godot.Collections.Array<PackedScene> _allyUnits = new();

	/// <summary>
	///     Hostile units the encounter spawns into <c>EnemyUnits</c>. The whole
	///     point of this NPC is to trigger a fight, so this list usually has at
	///     least one entry.
	/// </summary>
	[Export]
	private Godot.Collections.Array<PackedScene> _enemyUnits = new();

	[Export]
	private NodePath? _dialogPath;

	private Label3D? _interactText;
	private Node? _dialog;

	public override void _Ready()
	{
		_interactText = GetNode<Label3D>(_interactTextPath);
		_dialog = GetNode<Node>(_dialogPath);
		_dialog.Connect("battle_started", Callable.From<Node>(OnBattleStarted));
	}

	private void OnBattleStarted(Node interactorNode)
	{
		GD.Print($"Soldier '{Name}': OnBattleStarted received signal (interactor={interactorNode?.Name}).");
		if (interactorNode is not IInteractor interactor)
		{
			GD.PrintErr("battle_started: interactor does not implement IInteractor");
			return;
		}

		LaunchBattle(interactor);
	}

	/// <summary>
	///     Hard-coded prison roster used as a fallback when the inspector exports
	///     come back empty. This happens when the editor regenerates UIDs out from
	///     under <c>Soldier.tscn</c> — the typed PackedScene array dereferences a
	///     stale id and silently materialises as zero entries. Path lookups don't
    ///     have that problem.
    /// </summary>
    private static readonly (string PlayerPath, string AllyPath, string EnemyPath) _prisonFallbackPaths = (
        "res://scenes/unit/characters/kaelen_voss.tscn",
        "res://scenes/unit/characters/mercenary_ally.tscn",
        "res://scenes/unit/enemies/enemy_soldier.tscn"
    );

    private void LaunchBattle(IInteractor interactor)
    {
		GD.Print($"Soldier '{Name}': LaunchBattle invoked.");

        if (string.IsNullOrEmpty(_battleSceneToCharge))
        {
			GD.PrintErr($"Soldier '{Name}': _battleSceneToCharge is empty; cannot start a battle.");
            interactor.UnlockInteractor();
            return;
        }

        PackedScene? battleScene = ResourceLoader.Load<PackedScene>(_battleSceneToCharge);
        if (battleScene is null)
        {
			GD.PrintErr($"Soldier '{Name}': failed to load battle scene at '{_battleSceneToCharge}'.");
            interactor.UnlockInteractor();
            return;
        }

		// Capture the player's current scene + world position so Forfeit / Victory
		// Continue can drop them right back here, no save needed.
		SceneTree tree = GetTree();
		string returnScenePath = tree.CurrentScene?.SceneFilePath ?? string.Empty;
		Vector3 returnPosition = interactor is Node3D playerNode
			? playerNode.GlobalPosition
			: GlobalPosition; // fallback to NPC's own position if interactor isn't a Node3D

		var playerScenes = ToList(_playerUnits);
		var allyScenes = ToList(_allyUnits);
		var enemyScenes = ToList(_enemyUnits);

		// Defensive fallback: if the typed-array exports came back empty (e.g. the
		// editor regenerated a referenced scene's UID and the Soldier.tscn entry
        // stayed stale), load the canonical prison roster by path so the encounter
        // still triggers properly instead of dumping the player into an empty map.
        if (playerScenes.Count == 0 && allyScenes.Count == 0 && enemyScenes.Count == 0)
        {
			GD.PrintErr($"Soldier '{Name}': inspector unit lists are all empty — using path-based prison fallback.");
            var fallbackPlayer = ResourceLoader.Load<PackedScene>(_prisonFallbackPaths.PlayerPath);
            var fallbackAlly = ResourceLoader.Load<PackedScene>(_prisonFallbackPaths.AllyPath);
            var fallbackEnemy = ResourceLoader.Load<PackedScene>(_prisonFallbackPaths.EnemyPath);
            if (fallbackPlayer is not null) playerScenes.Add(fallbackPlayer);
            if (fallbackAlly is not null) allyScenes.Add(fallbackAlly);
            if (fallbackEnemy is not null) enemyScenes.Add(fallbackEnemy);
        }

        BattleSetup setup = new()
        {
            BattleScene = battleScene,
            PlayerUnits = playerScenes,
            AllyUnits = allyScenes,
            EnemyUnits = enemyScenes,
            ReturnScenePath = returnScenePath,
            ReturnPosition = returnPosition,
            EncounterName = Name.ToString(),
        };

		GD.Print($"Soldier '{Name}': BattleSetup ready — "
            + $"player={setup.PlayerUnits.Count}, ally={setup.AllyUnits.Count}, enemy={setup.EnemyUnits.Count}, "
			+ $"return='{returnScenePath}'.");

        if (setup.PlayerUnits.Count == 0)
        {
			GD.PrintErr($"Soldier '{Name}': BattleSetup has no PlayerUnits — battle will start with no controllable party.");
        }

        // Unlock here, after the setup is fully assembled but before the scene
		// change — if Launch is going to fail, it'll print and bail without us
		// leaving the player frozen. If Launch succeeds, the unit moves to the
		// new scene and the unlock just doesn't matter to the dead exploration
        // player.
        interactor.UnlockInteractor();

        if (BattleLauncher.Instance is null)
        {
            GD.PrintErr("Soldier: BattleLauncher autoload is not registered. Falling back to direct ChangeSceneToFile (no return state).");
            tree.ChangeSceneToFile(_battleSceneToCharge);
            return;
        }

        BattleLauncher.Instance.Launch(setup);
    }

    public void Interact(IInteractor interactor)
    {
		GD.Print($"Soldier '{Name}': Interact() called — starting dialog.");
        interactor.LockInteractor();
        if (interactor is Node node)
        {
            _dialog?.Call("set_interactor", node);
        }

        _dialog?.Call("talk");
    }

    public bool CanInteract() => true;

    /// <summary>
    ///     Materialise a Godot exported array into a fresh <see cref="System.Collections.Generic.List{T}" />
	///     so the immutable <see cref="BattleSetup" /> doesn't end up holding a reference
	///     to a mutable scene-resource collection. Filters out null slots a designer may
	///     have left empty in the inspector.
	/// </summary>
	private static System.Collections.Generic.List<PackedScene> ToList(
		Godot.Collections.Array<PackedScene>? source)
	{
		var output = new System.Collections.Generic.List<PackedScene>();
		if (source is null)
			return output;
		foreach (var scene in source)
		{
			if (scene is not null)
				output.Add(scene);
		}
		return output;
	}

	public void ShowPrompt()
	{
		if (_interactText is not null)
			_interactText.Visible = true;
	}

	public void HidePrompt()
	{
		if (_interactText is not null)
			_interactText.Visible = false;
	}
}
