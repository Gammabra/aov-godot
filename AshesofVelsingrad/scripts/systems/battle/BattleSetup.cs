using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.Systems.Battle;

/// <summary>
///     Declarative description of an encounter — who fights, on which map, and where to
///     return when the battle resolves. Built by the exploration layer (an
///     <see cref="NpcBattleTrigger" /> on a story-quest NPC, a procedural encounter
///     generator, etc.) and handed to <see cref="Managers.BattleLauncher" />.
/// </summary>
/// <remarks>
///     <para>
///         Designed to be reusable: the same battle scene can host many different
///         encounters by swapping the <see cref="EnemyUnits" /> list. The fields are
///         <c>init</c>-only properties so a setup is immutable once constructed —
///         <see cref="Managers.BattleLauncher" /> stores the active setup and reads from
///         it; nothing should mutate it mid-battle.
///     </para>
///     <para>
///         <see cref="ReturnScenePath" /> + <see cref="ReturnPosition" /> are how the
///         "Forfeit" flow gets the player back to where they were when they triggered the
///         fight. Treat it as a temporary checkpoint — not a save file. If you need
///         persistent save behaviour, layer a real save system on top.
///     </para>
/// </remarks>
public sealed class BattleSetup
{
    /// <summary>
    ///     The battle scene to load (a <c>.tscn</c> with a <see cref="Managers.GameManager" />,
    ///     <c>GridMap</c>, and the empty <c>PlayerUnits</c> / <c>AlliedUnits</c> /
    ///     <c>EnemyUnits</c> containers ready to receive instantiated units).
    /// </summary>
    public PackedScene? BattleScene { get; init; }

    /// <summary>
    ///     PackedScenes for each player-controlled unit (the active party). Each scene's
    ///     root must extend <c>UnitSystem</c>. The launcher instantiates one of each and
    ///     drops them into the battle scene's <c>PlayerUnits</c> container.
    /// </summary>
    public List<PackedScene> PlayerUnits { get; init; } = new();

    /// <summary>
    ///     AI-controlled friendly guests (recruited mercs, summoned creatures, scripted
    ///     helpers). Spawned into <c>AlliedUnits</c>.
    /// </summary>
    public List<PackedScene> AllyUnits { get; init; } = new();

    /// <summary>
    ///     The hostile units the encounter spawns. Spawned into <c>EnemyUnits</c>.
    /// </summary>
    public List<PackedScene> EnemyUnits { get; init; } = new();

    /// <summary>
    ///     <c>res://</c> path of the scene to return to once the battle ends (forfeit
    ///     or post-victory). Empty string disables the return flow — the launcher will
    ///     just stay on the battle scene's end-screen.
    /// </summary>
    public string ReturnScenePath { get; init; } = string.Empty;

    /// <summary>
    ///     World position to spawn the player at on return. Typically the position the
    ///     player had when they triggered the encounter — captured by the
    ///     <see cref="NpcBattleTrigger" /> at <c>Trigger</c> time.
    /// </summary>
    public Vector3 ReturnPosition { get; init; } = Vector3.Zero;

    /// <summary>Display name shown in the battle banner / log header. Optional.</summary>
    public string EncounterName { get; init; } = "Battle";
}
