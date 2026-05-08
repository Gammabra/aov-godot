using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using Godot;
using Catalog = AshesOfVelsingrad.Data.Skills;

namespace AshesOfVelsingrad;

/// <summary>
///     Mercenary — level 1 fighter, AI-controlled friendly guest.
/// </summary>
/// <remarks>
///     <para>
///         Sits under the <c>AlliedUnits</c> container so <c>GameManager.LoadUnits</c>
///         tags it with <see cref="Faction.Ally" />. The player cannot select or steer
///         this unit — it acts on the <c>OnAllyTurn</c> event via an
///         <see cref="EnemyAIBehavior" /> child node, picking targets from whatever the
///         current battle's hostile-faction list contains.
///     </para>
///     <para>
///         Personality is <see cref="AIPersonality.Aggressive" /> so the merc closes in
///         on enemies rather than holding back. Loadout mirrors the Fighter archetype
///         minus the buff utility — pure brawler.
///     </para>
/// </remarks>
public sealed partial class MercenaryAlly : UnitSystem
{
	protected override void Initialize()
	{
		UnitName = "Mercenary";
		Description = "A hired sword. Doesn't talk much, doesn't need to.";
		MaxHp = 950;
		Hp = MaxHp;
		BaseAtk = 170;
		BaseDef = 30;
		BaseSpeed = 150;
		Intelligence = 60;
		ManaMax = 100;
		Mana = ManaMax;
		IsAlive = true;
		PossibleMovesRange = 2;
		Curse = 0;
		Personality = AIPersonality.Aggressive;

		ActiveSkills.Add(new Catalog.CrushingStrike());
		ActiveSkills.Add(new Catalog.Charge());
		ActiveSkills.Add(new Catalog.Block());
		ActiveSkills.Add(new Catalog.CircularStrike());

		PassiveSkills.Add(new Catalog.BruteForce());
		PassiveSkills.Add(new Catalog.WarriorEndurance());

		base.Initialize();

		var statusEffectSystem = new StatusEffectSystem();
		InjectDependencies(statusEffectSystem);

		SetEntityProfile(new EntityProfile
		{
			DisplayName = "Mercenary",
			ClassName = "Fighter",
			Level = 1,
			PortraitPath = "res://assets/Krita/icone_mercenaire.png",
			Bio = "Coin-and-blade type. Loyal until the contract ends.",
		});
	}
}
