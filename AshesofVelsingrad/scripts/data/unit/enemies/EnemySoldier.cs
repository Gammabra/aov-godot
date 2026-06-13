using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;
using Catalog = AshesOfVelsingrad.Data.Skills;

namespace AshesOfVelsingrad;

/// <summary>
///     Generic configurable enemy soldier — every stat, name, and portrait can be
///     overridden from the Inspector so a designer can drop an instance into any
///     battle scene and shape it without touching the script.
/// </summary>
/// <remarks>
///     <para>
///         Default loadout is the Fighter catalogue's basic actives so each soldier
///         already feels like a competent threat in the test scene; if a designer wants
///         a softer or weirder enemy they can subclass and override <see cref="Initialize" />.
///     </para>
///     <para>
///         Stats use the same shape as <see cref="KaelenVoss" /> at level 1 but slightly
///         weaker so a single soldier is a fair fight for one player. Scale up for
///         later encounters by exporting <see cref="SoldierLevel" /> and a stat
///         multiplier later.
///     </para>
/// </remarks>
public sealed partial class EnemySoldier : UnitSystem
{
    /// <summary>Display name shown in the HUD. Overridable per-instance.</summary>
    [Export]
    public string SoldierName { get; set; } = "Soldier";

    /// <summary>Level shown next to the name in the turn-queue chip / status panel.</summary>
    [Export]
    public int SoldierLevel { get; set; } = 1;

    /// <summary>Portrait <c>res://</c> path. Falls back to a coloured square if empty.</summary>
    [Export(PropertyHint.File, "*.png,*.jpg,*.svg")]
    public string SoldierPortraitPath { get; set; } = "res://assets/Krita/icone_solder.png";

    /// <summary>Max HP. Adjust per-instance for tougher / weaker variants.</summary>
    [Export]
    public float SoldierMaxHp { get; set; } = 800f;

    /// <summary>Base attack. Default sized to be a fair fight for a level-1 fighter.</summary>
    [Export]
    public float SoldierBaseAtk { get; set; } = 140f;

    /// <summary>Base defence.</summary>
    [Export]
    public float SoldierBaseDef { get; set; } = 25f;

	/// <summary>Base speed — drives turn order. Lower than Kaelen's 180 so the player acts first.</summary>
	[Export]
	public float SoldierBaseSpeed { get; set; } = 120f;

	/// <summary>Tactical-AI personality: Aggressive / Defensive / Opportunistic / Balanced.</summary>
	[Export]
	public AIPersonality SoldierPersonality { get; set; } = AIPersonality.Balanced;

	/// <inheritdoc />
	protected override void Initialize()
	{
		UnitName = SoldierName;
		Description = "Velsingrad regular. Doesn't ask questions.";
		MaxHp = SoldierMaxHp;
		Hp = MaxHp;
		BaseAtk = SoldierBaseAtk;
		BaseDef = SoldierBaseDef;
		BaseSpeed = SoldierBaseSpeed;
		Intelligence = 40;
		ManaMax = 80;
		Mana = ManaMax;
		IsAlive = true;
		PossibleMovesRange = 2;
		Curse = 0;
		Personality = SoldierPersonality;
		Type = AovDataStructures.UnitType.Fighter;

		ActiveSkills.Add(new Catalog.CrushingStrike());
		ActiveSkills.Add(new Catalog.Charge());
		ActiveSkills.Add(new Catalog.Block());

		PassiveSkills.Add(new Catalog.BruteForce());

		base.Initialize();

		var statusEffectSystem = new StatusEffectSystem();
		InjectDependencies(statusEffectSystem);

		SetEntityProfile(new EntityProfile
		{
			DisplayName = SoldierName,
			ClassName = "Soldier",
			Level = SoldierLevel,
			PortraitPath = SoldierPortraitPath,
			Bio = "A faceless trooper of the Velsingrad guard.",
		});
	}
}
