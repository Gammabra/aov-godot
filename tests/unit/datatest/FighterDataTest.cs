using System;
using System.Collections.Generic;
using AshesOfVelsingrad;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class FighterDataTest
{
	private readonly List<Node> _testNodes = new();
	private Node? _root;
	private FighterData? _fighter;

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

	#region Initialization Tests

	[TestCase]
	public void Initialize_SetsCorrectStats()
	{
		_fighter = AddNodeToTestRoot(new FighterData());

		AssertThat(_fighter.UnitName).IsEqual("Fighter");
		AssertThat(_fighter.Type).IsEqual(AovDataStructures.UnitType.Fighter);
		AssertThat(_fighter.MaxHp).IsEqual(1200f);
		AssertThat(_fighter.Hp).IsEqual(1200f);
		AssertThat(_fighter.BaseAtk).IsEqual(180f);
		AssertThat(_fighter.BaseDef).IsEqual(60f);
		AssertThat(_fighter.BaseSpeed).IsEqual(80f);
		AssertThat(_fighter.ManaMax).IsEqual(120f);
		AssertThat(_fighter.Mana).IsEqual(120f);
		AssertThat(_fighter.PossibleMovesRange).IsEqual(2);
		AssertThat(_fighter.IsAlive).IsTrue();
	}

	[TestCase]
	public void Initialize_Has5Skills()
	{
		_fighter = AddNodeToTestRoot(new FighterData());

		AssertThat(_fighter.ActiveSkills.Count).IsEqual(5);
	}

	[TestCase]
	public void Initialize_HasCorrectSkills()
	{
		_fighter = AddNodeToTestRoot(new FighterData());

		AssertThat(_fighter.ActiveSkills[0]).IsInstanceOf<CrushingStrike>();
		AssertThat(_fighter.ActiveSkills[1]).IsInstanceOf<WarCry>();
		AssertThat(_fighter.ActiveSkills[2]).IsInstanceOf<StaggeringBlow>();
		AssertThat(_fighter.ActiveSkills[3]).IsInstanceOf<ShieldBash>();
		AssertThat(_fighter.ActiveSkills[4]).IsInstanceOf<CircularStrike>();
	}

	#endregion

	#region TakeDamage Tests

	[TestCase]
	public void TakeDamage_ReducesByDefense()
	{
		_fighter = AddNodeToTestRoot(new FighterData());

		_fighter.TakeDamage(100f);

		// 100 - 60 DEF = 40 damage
		AssertThat(_fighter.Hp).IsEqual(1160f);
	}

	[TestCase]
	public void TakeDamage_NeverNegativeDamage()
	{
		_fighter = AddNodeToTestRoot(new FighterData());

		_fighter.TakeDamage(30f); // Less than DEF

		// Should take 0 damage
		AssertThat(_fighter.Hp).IsEqual(1200f);
	}

	[TestCase]
	public void TakeDamage_SetsIsAliveFalse_WhenHpReachesZero()
	{
		_fighter = AddNodeToTestRoot(new FighterData());

		_fighter.TakeDamage(2000f);

		AssertThat(_fighter.Hp).IsEqual(0f);
		AssertThat(_fighter.IsAlive).IsFalse();
	}

	[TestCase]
	public void TakeDamage_ClampsHpToZero()
	{
		_fighter = AddNodeToTestRoot(new FighterData());

		_fighter.TakeDamage(5000f);

		AssertThat(_fighter.Hp).IsEqual(0f);
	}

	#endregion

	#region Skill Tests

	[TestCase]
	public void CrushingStrike_Deals150PercentDamage()
	{
		_fighter = AddNodeToTestRoot(new FighterData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem(unitName: "Target", maxHp: 500f));
		target.CallInitialize();

		var skill = _fighter.ActiveSkills[0]; // CrushingStrike
		float expectedDamage = _fighter.TotalAtk * 1.5f;

		skill.Use(_fighter, new List<UnitSystem> { target }, null);

		// Fighter ATK = 180, 180 * 1.5 = 270 damage
		// Target DEF = 5, so 270 - 5 = 265
		float expectedHp = target.MaxHp - (expectedDamage - target.TotalDef);
		AssertThat(target.Hp).IsEqual(expectedHp);
	}

	[TestCase]
	public void WarCry_BuffsAllAllies()
	{
		_fighter = AddNodeToTestRoot(new FighterData());
		var ally1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally1" });
		var ally2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally2" });
		ally1.CallInitialize();
		ally2.CallInitialize();

		var skill = _fighter.ActiveSkills[1]; // WarCry
		var initialAtk1 = ally1.TotalAtk;
		var initialAtk2 = ally2.TotalAtk;

		skill.Use(_fighter, new List<UnitSystem> { _fighter, ally1, ally2 }, null);

		// Should buff all 3 units by +30 ATK
		AssertThat(_fighter.TotalAtk).IsEqual(_fighter.BaseAtk + 30f);
		AssertThat(ally1.TotalAtk).IsEqual(initialAtk1 + 30f);
		AssertThat(ally2.TotalAtk).IsEqual(initialAtk2 + 30f);
	}

	[TestCase]
	public void StaggeringBlow_DealsDamageAndStuns()
	{
		_fighter = AddNodeToTestRoot(new FighterData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
		target.CallInitialize();

		var skill = _fighter.ActiveSkills[2]; // StaggeringBlow

		skill.Use(_fighter, new List<UnitSystem> { target }, null);

		// Should deal damage
		AssertThat(target.Hp).IsLess(target.MaxHp);

		// Should apply stun
		AssertThat(target.IsControlled).IsTrue();
	}

	[TestCase]
	public void ShieldBash_DealsDamage()
	{
		_fighter = AddNodeToTestRoot(new FighterData());
		var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
		target.CallInitialize();

		var skill = _fighter.ActiveSkills[3]; // ShieldBash
		var initialHp = target.Hp;

		skill.Use(_fighter, new List<UnitSystem> { target }, null);

		// Should deal 50% ATK damage
		AssertThat(target.Hp).IsLess(initialHp);
	}

	[TestCase]
	public void CircularStrike_HitsAllEnemies()
	{
		_fighter = AddNodeToTestRoot(new FighterData());
		var enemy1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy1" });
		var enemy2 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy2" });
		var enemy3 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Enemy3" });
		enemy1.CallInitialize();
		enemy2.CallInitialize();
		enemy3.CallInitialize();

		var skill = _fighter.ActiveSkills[4]; // CircularStrike

		skill.Use(_fighter, new List<UnitSystem> { enemy1, enemy2, enemy3 }, null);

		// All should take damage
		AssertThat(enemy1.Hp).IsLess(enemy1.MaxHp);
		AssertThat(enemy2.Hp).IsLess(enemy2.MaxHp);
		AssertThat(enemy3.Hp).IsLess(enemy3.MaxHp);
	}

	#endregion

	#region Skill Properties Tests

	[TestCase]
	public void CrushingStrike_HasCorrectProperties()
	{
		_fighter = AddNodeToTestRoot(new FighterData());
		var skill = _fighter.ActiveSkills[0] as CrushingStrike;

		AssertThat(skill).IsNotNull();
		AssertThat(skill!.Name).IsEqual("Crushing Strike");
		AssertThat(skill.ManaCost).IsEqual(15);
		AssertThat(skill.Range).IsEqual(1);
		AssertThat(skill.TargetType).IsEqual(AovDataStructures.TargetTypes.SingleEnemy);
	}

	[TestCase]
	public void WarCry_HasCorrectProperties()
	{
		_fighter = AddNodeToTestRoot(new FighterData());
		var skill = _fighter.ActiveSkills[1] as WarCry;

		AssertThat(skill).IsNotNull();
		AssertThat(skill!.Name).IsEqual("War Cry");
		AssertThat(skill.ManaCost).IsEqual(20);
		AssertThat(skill.TargetType).IsEqual(AovDataStructures.TargetTypes.AllAllies);
	}

	#endregion
}
