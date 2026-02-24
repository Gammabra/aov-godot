using System;
using System.Collections.Generic;
using AshesOfVelsingrad;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using AshesOfVelsingrad.Data;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class PlayerUnitsTest
{
	private readonly List<Node> _testNodes = new();
	private Node? _root;

	#region Setup and Teardown

	[BeforeTest]
	public void Setup()
	{
		_testNodes.Clear();

		_root = new Node { Name = "TestRoot" };
		((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
		_testNodes.Add(_root);
	}

	[AfterTest]
	public void TearDown()
	{
		foreach (Node node in _testNodes)
		{
			if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
				node.QueueFree();
		}

		_testNodes.Clear();
	}

	#endregion

	#region Helper Methods

	private T AddNodeToTestRoot<T>(T node) where T : Node
	{
		if (_root == null)
			throw new InvalidOperationException("Test root node is not initialized.");
		_root.AddChild(node);
		_testNodes.Add(node);
		return node;
	}

	#endregion

	#region Swordsman Tests

	[TestCase]
	public void Swordsman_Initialize_SetsCorrectStats()
	{
		var swordsman = AddNodeToTestRoot(new SwordsmanData());

		AssertThat(swordsman.UnitName).IsEqual("Swordsman");
		AssertThat(swordsman.Type).IsEqual(AovDataStructures.UnitType.Swordsman);
		AssertThat(swordsman.MaxHp).IsEqual(900f);
		AssertThat(swordsman.BaseAtk).IsEqual(200f);
		AssertThat(swordsman.BaseDef).IsEqual(35f);
		AssertThat(swordsman.BaseSpeed).IsEqual(130f);
		AssertThat(swordsman.PossibleMovesRange).IsEqual(3);
	}

	[TestCase]
	public void Swordsman_Has5Skills()
	{
		var swordsman = AddNodeToTestRoot(new SwordsmanData());

		AssertThat(swordsman.ActiveSkills.Count).IsEqual(5);
	}

	[TestCase]
	public void Swordsman_BladeDance_HitsMultipleEnemies()
	{
		var swordsman = AddNodeToTestRoot(new SwordsmanData());
		var enemy1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy1" });
		var enemy2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy2" });
		var enemy3 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy3" });
		enemy1.CallInitialize();
		enemy2.CallInitialize();
		enemy3.CallInitialize();

		var skill = swordsman.ActiveSkills[0]; // BladeDance

		skill.Use(swordsman, new List<UnitSystem> { enemy1, enemy2, enemy3 }, null);

		// All 3 should take damage
		AssertThat(enemy1.Hp).IsLess(enemy1.MaxHp);
		AssertThat(enemy2.Hp).IsLess(enemy2.MaxHp);
		AssertThat(enemy3.Hp).IsLess(enemy3.MaxHp);
	}

	[TestCase]
	public void Swordsman_PhantomStrike_BypassesDefense()
	{
		var swordsman = AddNodeToTestRoot(new SwordsmanData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem(unitName: "Target", maxHp: 500f));
		target.CallInitialize();

		var skill = swordsman.ActiveSkills[1]; // PhantomStrike
		float expectedDamage = swordsman.TotalAtk * 1.2f;

		skill.Use(swordsman, new List<UnitSystem> { target }, null);

		// Should deal raw damage without DEF reduction
		float expectedHp = target.MaxHp - expectedDamage;
		AssertThat(target.Hp).IsEqual(expectedHp);
	}

	[TestCase]
	public void Swordsman_ExecutionBlade_DealsBonusDamage_WhenTargetLowHp()
	{
		var swordsman = AddNodeToTestRoot(new SwordsmanData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem(unitName: "Target", maxHp: 10000f));
		target.CallInitialize();

		// Reduce target to below 30% HP
		target.TakeDamage(7100f);

		var skill = swordsman.ActiveSkills[2]; // ExecutionBlade
		var hpBefore = target.Hp;

		skill.Use(swordsman, new List<UnitSystem> { target }, null);

		// Should deal 200% ATK damage (doubled)
		float expectedDamage = (swordsman.TotalAtk * 2.0f) - target.BaseDef;
		AssertThat(target.Hp).IsEqual(hpBefore - expectedDamage);
	}

	[TestCase]
	public void Swordsman_CounterStance_BuffsSelf()
	{
		var swordsman = AddNodeToTestRoot(new SwordsmanData());
		var initialAtk = swordsman.TotalAtk;

		var skill = swordsman.ActiveSkills[3]; // CounterStance

		skill.Use(swordsman, new List<UnitSystem> { swordsman }, null);

		// Should buff ATK by 40
		AssertThat(swordsman.TotalAtk).IsEqual(initialAtk + 40f);
	}

	[TestCase]
	public void Swordsman_BurningSlash_AppliesBurning()
	{
		var swordsman = AddNodeToTestRoot(new SwordsmanData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
		target.CallInitialize();

		var skill = swordsman.ActiveSkills[4]; // BurningSlash

		skill.Use(swordsman, new List<UnitSystem> { target }, null);

		// Should have burning effect
		AssertThat(target.GetActiveEffects().Count).IsGreater(0);
	}

	#endregion

	#region Assassin Tests

	[TestCase]
	public void Assassin_Initialize_SetsCorrectStats()
	{
		var assassin = AddNodeToTestRoot(new AssassinData());

		AssertThat(assassin.UnitName).IsEqual("Assassin");
		AssertThat(assassin.Type).IsEqual(AovDataStructures.UnitType.Assassin);
		AssertThat(assassin.MaxHp).IsEqual(600f);
		AssertThat(assassin.BaseAtk).IsEqual(280f);
		AssertThat(assassin.BaseDef).IsEqual(15f);
		AssertThat(assassin.BaseSpeed).IsEqual(200f);
		AssertThat(assassin.PossibleMovesRange).IsEqual(4);
	}

	[TestCase]
	public void Assassin_Has5Skills()
	{
		var assassin = AddNodeToTestRoot(new AssassinData());

		AssertThat(assassin.ActiveSkills.Count).IsEqual(5);
	}

	[TestCase]
	public void Assassin_CriticalStrike_Deals200PercentDamage()
	{
		var assassin = AddNodeToTestRoot(new AssassinData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem(unitName: "Target", maxHp: 1000f));
		target.CallInitialize();

		var skill = assassin.ActiveSkills[0]; // CriticalStrike
		float expectedDamage = (assassin.TotalAtk * 2.0f) - target.BaseDef;

		skill.Use(assassin, new List<UnitSystem> { target }, null);

		float expectedHp = target.MaxHp - expectedDamage;
		AssertThat(target.Hp).IsEqual(expectedHp);
	}

	[TestCase]
	public void Assassin_InstantKill_KillsLowHpTarget()
	{
		var assassin = AddNodeToTestRoot(new AssassinData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
		target.CallInitialize();

		// Reduce target to 10% HP (below 15% threshold)
		target.TakeDamage(90f);

		var skill = assassin.ActiveSkills[1]; // InstantKill

		skill.Use(assassin, new List<UnitSystem> { target }, null);

		// Should instantly kill
		AssertThat(target.Hp).IsEqual(0f);
		AssertThat(target.IsAlive).IsFalse();
	}

	[TestCase]
	public void Assassin_InstantKill_DoesNotKill_WhenAboveThreshold()
	{
		var assassin = AddNodeToTestRoot(new AssassinData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem(unitName: "Target", maxHp: 1000f));
		target.CallInitialize();

		// Keep target at 50% HP (above threshold)
		target.TakeDamage(500f);

		var skill = assassin.ActiveSkills[1]; // InstantKill

		skill.Use(assassin, new List<UnitSystem> { target }, null);

		// Should still be alive
		AssertThat(target.IsAlive).IsTrue();
		AssertThat(target.Hp).IsGreater(0f);
	}

	[TestCase]
	public void Assassin_ShadowStrike_BypassesDefense()
	{
		var assassin = AddNodeToTestRoot(new AssassinData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem(unitName: "Target", maxHp: 500f));
		target.CallInitialize();

		var skill = assassin.ActiveSkills[2]; // ShadowStrike
		float expectedDamage = assassin.TotalAtk * 1.5f;

		skill.Use(assassin, new List<UnitSystem> { target }, null);

		// Should deal raw damage without DEF reduction
		float expectedHp = target.MaxHp - expectedDamage;
		AssertThat(target.Hp).IsEqual(expectedHp);
	}

	[TestCase]
	public void Assassin_BloodDrain_HealsAssassin()
	{
		var assassin = AddNodeToTestRoot(new AssassinData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
		target.CallInitialize();

		// Damage assassin first
		assassin.TakeDamage(100f);
		var damagedHp = assassin.Hp;

		var skill = assassin.ActiveSkills[3]; // BloodDrain

		skill.Use(assassin, new List<UnitSystem> { target }, null);

		// Should heal for 30% of damage dealt
		AssertThat(assassin.Hp).IsGreater(damagedHp);
	}

	[TestCase]
	public void Assassin_PoisonBlade_AppliesPoison()
	{
		var assassin = AddNodeToTestRoot(new AssassinData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
		target.CallInitialize();

		var skill = assassin.ActiveSkills[4]; // PoisonBlade

		skill.Use(assassin, new List<UnitSystem> { target }, null);

		// Should have poison effect (currently BurningEffect as placeholder)
		AssertThat(target.GetActiveEffects().Count).IsGreater(0);
	}

	[TestCase]
	public void Assassin_HighSpeed_LowDefense()
	{
		var assassin = AddNodeToTestRoot(new AssassinData());

		// Verify glass cannon stats
		AssertThat(assassin.BaseSpeed).IsEqual(200f); // Fastest
		AssertThat(assassin.BaseDef).IsEqual(15f); // Lowest defense

		// Should take heavy damage
		assassin.TakeDamage(100f);
		AssertThat(assassin.Hp).IsEqual(600f - (100f - 15f)); // 515
	}

	#endregion

	#region Light Mage Tests

	[TestCase]
	public void LightMage_Initialize_SetsCorrectStats()
	{
		var mage = AddNodeToTestRoot(new LightMageData());

		AssertThat(mage.UnitName).IsEqual("Light Mage");
		AssertThat(mage.Type).IsEqual(AovDataStructures.UnitType.Mage);
		AssertThat(mage.MaxHp).IsEqual(700f);
		AssertThat(mage.BaseAtk).IsEqual(100f);
		AssertThat(mage.Intelligence).IsEqual(250f);
		AssertThat(mage.ManaMax).IsEqual(300f);
	}

	[TestCase]
	public void LightMage_Has5Skills()
	{
		var mage = AddNodeToTestRoot(new LightMageData());

		AssertThat(mage.ActiveSkills.Count).IsEqual(5);
	}

	[TestCase]
	public void LightMage_SacredRay_DealsDamageAndHeals()
	{
		var mage = AddNodeToTestRoot(new LightMageData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
		target.CallInitialize();

		// Damage mage first
		mage.TakeDamage(50f);
		var damagedHp = mage.Hp;

		var skill = mage.ActiveSkills[0]; // SacredRay

		skill.Use(mage, new List<UnitSystem> { target }, null);

		// Should deal damage to target
		AssertThat(target.Hp).IsLess(target.MaxHp);

		// Should heal self
		AssertThat(mage.Hp).IsGreater(damagedHp);
	}

	[TestCase]
	public void LightMage_HealingTouch_HealsAlly()
	{
		var mage = AddNodeToTestRoot(new LightMageData());
		var ally = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally" });
		ally.CallInitialize();

		// Damage ally
		ally.TakeDamage(80f);
		var damagedHp = ally.Hp;

		var skill = mage.ActiveSkills[1]; // HealingTouch

		skill.Use(mage, new List<UnitSystem> { ally }, null);

		// Should heal 150 HP
		float expectedHp = Math.Min(damagedHp + 150f, ally.MaxHp);
		AssertThat(ally.Hp).IsEqual(expectedHp);
	}

	[TestCase]
	public void LightMage_CleansingLight_RemovesEffects()
	{
		var mage = AddNodeToTestRoot(new LightMageData());
		var ally = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally" });
		ally.CallInitialize();

		// Apply a status effect
		ally.SetStatusEffectOnUnit(new Stun(1));
		AssertThat(ally.GetActiveEffects().Count).IsGreater(0);

		var skill = mage.ActiveSkills[2]; // CleansingLight

		skill.Use(mage, new List<UnitSystem> { ally }, null);

		// Should remove all effects
		AssertThat(ally.GetActiveEffects().Count).IsEqual(0);
		AssertThat(ally.IsControlled).IsFalse();
	}

	[TestCase]
	public void LightMage_Resurrection_RevivesDeadAlly()
	{
		var mage = AddNodeToTestRoot(new LightMageData());
		var ally = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally" });
		ally.CallInitialize();

		// Kill ally
		ally.TakeDamage(1000f);
		ally.SetIsAlive(false);
		AssertThat(ally.IsAlive).IsFalse();

		var skill = mage.ActiveSkills[3]; // Resurrection

		skill.Use(mage, new List<UnitSystem> { ally }, null);

		// Should revive with 50% HP
		AssertThat(ally.IsAlive).IsTrue();
		AssertThat(ally.Hp).IsEqual(ally.MaxHp * 0.5f);
	}

	[TestCase]
	public void LightMage_DivinePrayer_BuffsAllAllies()
	{
		var mage = AddNodeToTestRoot(new LightMageData());
		var ally1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally1" });
		var ally2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally2" });
		ally1.CallInitialize();
		ally2.CallInitialize();

		var initialAtk1 = ally1.TotalAtk;
		var initialAtk2 = ally2.TotalAtk;

		var skill = mage.ActiveSkills[4]; // DivinePrayer

		skill.Use(mage, new List<UnitSystem> { mage, ally1, ally2 }, null);

		// Should buff all by +25 ATK
		AssertThat(mage.TotalAtk).IsEqual(mage.BaseAtk + 25f);
		AssertThat(ally1.TotalAtk).IsEqual(initialAtk1 + 25f);
		AssertThat(ally2.TotalAtk).IsEqual(initialAtk2 + 25f);
	}

	#endregion

	#region Cross-Unit Comparison Tests

	[TestCase]
	public void PlayerUnits_HaveDifferentStatProfiles()
	{
		var fighter = AddNodeToTestRoot(new FighterData());
		var swordsman = AddNodeToTestRoot(new SwordsmanData());
		var assassin = AddNodeToTestRoot(new AssassinData());
		var mage = AddNodeToTestRoot(new LightMageData());

		// Fighter: Tank (high HP/DEF)
		AssertThat(fighter.MaxHp).IsGreater(swordsman.MaxHp);
		AssertThat(fighter.BaseDef).IsGreater(swordsman.BaseDef);

		// Assassin: Glass cannon (high ATK, low DEF)
		AssertThat(assassin.BaseAtk).IsGreater(fighter.BaseAtk);
		AssertThat(assassin.BaseDef).IsLess(fighter.BaseDef);

		// Mage: Support (high INT, low ATK)
		AssertThat(mage.Intelligence).IsGreater(fighter.Intelligence);
		AssertThat(mage.BaseAtk).IsLess(fighter.BaseAtk);
	}

	[TestCase]
	public void PlayerUnits_AllTakeDamageCorrectly()
	{
		var fighter = AddNodeToTestRoot(new FighterData());
		var swordsman = AddNodeToTestRoot(new SwordsmanData());
		var assassin = AddNodeToTestRoot(new AssassinData());
		var mage = AddNodeToTestRoot(new LightMageData());

		float damage = 100f;

		fighter.TakeDamage(damage);
		swordsman.TakeDamage(damage);
		assassin.TakeDamage(damage);
		mage.TakeDamage(damage);

		// All should have taken damage relative to their DEF
		AssertThat(fighter.Hp).IsLess(fighter.MaxHp);
		AssertThat(swordsman.Hp).IsLess(swordsman.MaxHp);
		AssertThat(assassin.Hp).IsLess(assassin.MaxHp);
		AssertThat(mage.Hp).IsLess(mage.MaxHp);

		// Fighter should have taken least damage (highest DEF)
		float fighterDamageTaken = fighter.MaxHp - fighter.Hp;
		float assassinDamageTaken = assassin.MaxHp - assassin.Hp;
		AssertThat(fighterDamageTaken).IsLess(assassinDamageTaken);
	}

	#endregion
}
