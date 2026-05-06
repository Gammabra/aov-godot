using System.Collections.Generic;
using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.Systems.Battle;

/// <summary>
///     Drop this on an NPC node in an exploration scene to make that NPC launch a
///     configurable battle when the player interacts with them.
/// </summary>
/// <remarks>
///     <para>
///         Designer flow:
///         <list type="number">
///             <item><description>Add an NPC <see cref="Node3D" /> (with whatever sprite / collider it needs) to the world scene.</description></item>
///             <item><description>Add this script as a child node, fill in the inspector exports (battle scene, enemies, party, allies).</description></item>
///             <item><description>From the player controller, call <see cref="Trigger" /> on the closest <see cref="NpcBattleTrigger" /> when an interact key is pressed.</description></item>
///         </list>
///     </para>
///     <para>
///         <see cref="Trigger" /> captures the player's current scene path + world position
///         as the "return point", builds a <see cref="BattleSetup" />, and calls
///         <see cref="BattleLauncher.Launch" />. If the player loses and presses Forfeit
///         on the GameOverScreen, <see cref="BattleLauncher" /> sends them back to that
///         exact scene + position so they can talk to the NPC again or walk away.
///     </para>
/// </remarks>
[GlobalClass]
public sealed partial class NpcBattleTrigger : Node3D
{
	/// <summary>Battle scene template — typically <c>res://scenes/Test/Test.tscn</c> or similar.</summary>
	[Export]
	public PackedScene? BattleScene { get; set; }

	/// <summary>Hostile units this NPC will spawn into <c>EnemyUnits</c>.</summary>
	[Export]
	public Godot.Collections.Array<PackedScene> EnemyUnits { get; set; } = new();

	/// <summary>Optional friendly AI guests this NPC brings along (recruited mercs, etc).</summary>
	[Export]
	public Godot.Collections.Array<PackedScene> AllyUnits { get; set; } = new();

	/// <summary>
	///     Player party scenes. Usually wired by a global PartyManager rather than
	///     hardcoded per NPC, but kept as an inspector option for one-off encounters
	///     where the encounter forces a specific lineup.
	/// </summary>
	[Export]
	public Godot.Collections.Array<PackedScene> PartyOverride { get; set; } = new();

	/// <summary>Display name shown on the battle banner / log header.</summary>
	[Export]
	public string EncounterName { get; set; } = "Encounter";

	/// <summary>
	///     Fired right before the scene transition kicks off. Listeners (sound effects,
	///     screen-fade tween, NPC dialogue close, …) hook here.
	/// </summary>
	[Signal]
	public delegate void BattleTriggeredEventHandler();

	/// <summary>
	///     Build a <see cref="BattleSetup" /> from this NPC's exported lists, capture the
	///     player's current world location as the return point, and launch.
	/// </summary>
	/// <param name="player">The player Node3D — used to capture the return position.</param>
	public void Trigger(Node3D player)
	{
		if (BattleScene is null)
		{
			GD.PrintErr($"NpcBattleTrigger '{Name}': BattleScene is not set; ignoring Trigger.");
			return;
		}
		if (BattleLauncher.Instance is null)
		{
			GD.PrintErr("NpcBattleTrigger: BattleLauncher autoload is not registered. Add res://scripts/managers/BattleLauncher.cs to Project Settings → AutoLoad.");
			return;
		}

		// Capture the return point — the player's current scene + world position. The
		// forfeit flow uses this to "respawn" the player exactly where the conversation
		// started, as a temporary checkpoint (NOT a real save).
		SceneTree tree = GetTree();
		string returnScene = tree.CurrentScene?.SceneFilePath ?? string.Empty;
		Vector3 returnPos = player.GlobalPosition;

		BattleSetup setup = new()
		{
			BattleScene = BattleScene,
			EnemyUnits = ToList(EnemyUnits),
			AllyUnits = ToList(AllyUnits),
			PlayerUnits = ToList(PartyOverride),
			ReturnScenePath = returnScene,
			ReturnPosition = returnPos,
			EncounterName = EncounterName,
		};

		EmitSignal(SignalName.BattleTriggered);
		BattleLauncher.Instance.Launch(setup);
	}

	/// <summary>Godot's <c>Array&lt;T&gt;</c> → plain <see cref="List{T}" /> for the C# data class.</summary>
	private static List<PackedScene> ToList(Godot.Collections.Array<PackedScene> arr)
	{
		var list = new List<PackedScene>(arr.Count);
		foreach (PackedScene s in arr)
			if (s is not null) list.Add(s);
		return list;
	}
}
