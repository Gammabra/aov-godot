using Godot;
using Godot.Collections;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     Declarative description of a battle encounter.
/// </summary>
/// <remarks>
///     <para>
///         A <see cref="BattleConfig" /> bundles everything <see cref="BattleLauncher" />
///         needs to set up a fight: which map to load, which player units come from
///         the active party, which AI-controlled allies and enemies to spawn (and
///         where), the victory condition, and rewards / aftermath flags.
///     </para>
///     <para>
///         This resource can be authored in two equivalent ways:
///         <list type="bullet">
///             <item>
///                 <description>
///                     As a <c>.tres</c> file in the editor, for static encounters
///                     (e.g. main-quest fights, scripted ambushes).
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Constructed in code from exploration logic, for procedurally
///                     generated encounters.
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
[GlobalClass]
public sealed partial class BattleConfig : Resource
{
    /// <summary>
    ///     Display name of the encounter ("Prison Break", "Inquisitor Ambush", ...).
    ///     Used by save names and the in-game battle banner.
    /// </summary>
    [Export]
    public string EncounterName { get; set; } = "Untitled Encounter";

    /// <summary>
    ///     Path of the map scene to load. The scene's root must be a
    ///     <see cref="MapSystem" /> subclass.
    /// </summary>
    [Export(PropertyHint.File, "*.tscn")]
    public string MapScenePath { get; set; } = string.Empty;

    /// <summary>
    ///     Identifiers of player-party characters to bring into this fight.
    ///     Resolved against the persistent party roster.
    /// </summary>
    /// <remarks>
    ///     Empty means "use the entire current party".
    /// </remarks>
    [Export]
    public Array<string> PlayerCharacterIds { get; set; } = [];

    /// <summary>
    ///     Spawn entries for AI-controlled friendly units (e.g. recruited mercs
    ///     temporarily fighting alongside the player).
    /// </summary>
    [Export]
    public Array<UnitSpawn> AlliedSpawns { get; set; } = [];

    /// <summary>
    ///     Spawn entries for hostile units.
    /// </summary>
    [Export]
    public Array<UnitSpawn> EnemySpawns { get; set; } = [];

    /// <summary>
    ///     Spawn positions for the player party. The list is consumed in order;
    ///     if the party has more units than positions, extra units fall back to
    ///     <see cref="MapSystem.PlaceUnits" />'s default placement.
    /// </summary>
    [Export]
    public Array<Vector3I> PlayerSpawnPositions { get; set; } = [];

    /// <summary>
    ///     Victory / defeat rule. Defaults to "defeat all enemies" if null.
    /// </summary>
    [Export]
    public VictoryCondition? VictoryCondition { get; set; }

    /// <summary>
    ///     Base XP reward distributed equally across surviving player units on victory.
    /// </summary>
    [Export]
    public int BaseExperienceReward { get; set; }

    /// <summary>
    ///     Whether the player can voluntarily retreat from this battle.
    /// </summary>
    [Export]
    public bool AllowRetreat { get; set; } = true;

    /// <summary>
    ///     Optional intro / banner text shown when the battle begins.
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string IntroText { get; set; } = string.Empty;
}

/// <summary>
///     A single non-player unit to spawn at a fixed grid position.
/// </summary>
[GlobalClass]
public sealed partial class UnitSpawn : Resource
{
    /// <summary>
    ///     Path of the unit scene to instantiate. The root node must derive from
    ///     <see cref="UnitSystem" />.
    /// </summary>
    [Export(PropertyHint.File, "*.tscn")]
    public string UnitScenePath { get; set; } = string.Empty;

    /// <summary>
    ///     Optional overriding display name (the merc's actual name in lore).
    ///     If empty, the unit's scene-defined name is kept.
    /// </summary>
    [Export]
    public string DisplayNameOverride { get; set; } = string.Empty;

    /// <summary>
    ///     Grid position where the unit should spawn.
    /// </summary>
    [Export]
    public Vector3I GridPosition { get; set; } = Vector3I.Zero;

    /// <summary>
    ///     Level applied to the unit at spawn. Used to scale stats for the encounter.
    /// </summary>
    [Export]
    public int Level { get; set; } = 1;
}
