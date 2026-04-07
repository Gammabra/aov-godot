using System;
using System.Collections.Generic;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Helpers.Systems;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Data;

[TestSuite]
[RequireGodotRuntime]
public class EnemyUnitsTest
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

    #region Enemy Fighter Tests

    [TestCase]
    public void EnemyFighter_Initialize_SetsCorrectStats()
    {
        var enemy = AddNodeToTestRoot(new EnemyFighter());

        AssertThat(enemy.UnitName).IsEqual("Enemy Fighter");
        AssertThat(enemy.Type).IsEqual(AovDataStructures.UnitType.Fighter);
        AssertThat(enemy.MaxHp).IsEqual(1000f);
        AssertThat(enemy.BaseAtk).IsEqual(150f);
        AssertThat(enemy.BaseDef).IsEqual(50f);
        AssertThat(enemy.Personality).IsEqual(AIPersonality.Aggressive);
    }

    [TestCase]
    public void EnemyFighter_Has2Skills()
    {
        var enemy = AddNodeToTestRoot(new EnemyFighter());

        AssertThat(enemy.ActiveSkills.Count).IsEqual(2);
    }

    [TestCase]
    public void EnemyFighter_BasicAttack_DealsDamage()
    {
        var enemy = AddNodeToTestRoot(new EnemyFighter());
        var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
        target.CallInitialize();

        var skill = enemy.ActiveSkills[0]; // FighterMeleeAttack
        var initialHp = target.Hp;

        skill.Use(enemy, new List<IUnitSystem> { target }, null);

        AssertThat(target.Hp).IsLess(initialHp);
    }

    [TestCase]
    public void EnemyFighter_IronBash_DealsDamageAndStuns()
    {
        var enemy = AddNodeToTestRoot(new EnemyFighter());
        var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
        target.CallInitialize();

        var skill = enemy.ActiveSkills[1]; // EnemyStaggeringBlow
        var initialHp = target.Hp;

        skill.Use(enemy, new List<IUnitSystem> { target }, null);

        AssertThat(target.Hp).IsLess(initialHp);
        AssertThat(target.IsControlled).IsTrue();
    }

    [TestCase]
    public void EnemyFighter_TakeDamage_AppliesDefense()
    {
        var enemy = AddNodeToTestRoot(new EnemyFighter());

        enemy.TakeDamage(100f);

        // 100 - 50 DEF = 50 damage
        AssertThat(enemy.Hp).IsEqual(950f);
    }

    #endregion

    #region Enemy Swordsman Tests

    [TestCase]
    public void EnemySwordsman_Initialize_SetsCorrectStats()
    {
        var enemy = AddNodeToTestRoot(new EnemySwordsman());

        AssertThat(enemy.UnitName).IsEqual("Enemy Swordsman");
        AssertThat(enemy.Type).IsEqual(AovDataStructures.UnitType.Swordsman);
        AssertThat(enemy.MaxHp).IsEqual(750f);
        AssertThat(enemy.BaseAtk).IsEqual(170f);
        AssertThat(enemy.BaseDef).IsEqual(30f);
        AssertThat(enemy.Personality).IsEqual(AIPersonality.Balanced);
    }

    [TestCase]
    public void EnemySwordsman_Has2Skills()
    {
        var enemy = AddNodeToTestRoot(new EnemySwordsman());

        AssertThat(enemy.ActiveSkills.Count).IsEqual(2);
    }

    [TestCase]
    public void EnemySwordsman_QuickSlash_DealsDamage()
    {
        var enemy = AddNodeToTestRoot(new EnemySwordsman());
        var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
        target.CallInitialize();

        var skill = enemy.ActiveSkills[0]; // SwordsmanSlash
        var initialHp = target.Hp;

        skill.Use(enemy, new List<IUnitSystem> { target }, null);

        AssertThat(target.Hp).IsLess(initialHp);
    }

    [TestCase]
    public void EnemySwordsman_FlameSlash_DealsDamageAndBurns()
    {
        var enemy = AddNodeToTestRoot(new EnemySwordsman());
        var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
        target.CallInitialize();

        var skill = enemy.ActiveSkills[1]; // EnemyBurningSlash
        var initialHp = target.Hp;

        skill.Use(enemy, new List<IUnitSystem> { target }, null);

        AssertThat(target.Hp).IsLess(initialHp);
        // Should have burning effect
        AssertThat(target.GetActiveEffects().Count).IsGreater(0);
    }

    #endregion

    #region Enemy Assassin Tests

    [TestCase]
    public void EnemyAssassin_Initialize_SetsCorrectStats()
    {
        var enemy = AddNodeToTestRoot(new EnemyAssassin());

        AssertThat(enemy.UnitName).IsEqual("Enemy Assassin");
        AssertThat(enemy.Type).IsEqual(AovDataStructures.UnitType.Assassin);
        AssertThat(enemy.MaxHp).IsEqual(500f);
        AssertThat(enemy.BaseAtk).IsEqual(230f);
        AssertThat(enemy.BaseDef).IsEqual(10f);
        AssertThat(enemy.Personality).IsEqual(AIPersonality.Opportunistic);
    }

    [TestCase]
    public void EnemyAssassin_Has2Skills()
    {
        var enemy = AddNodeToTestRoot(new EnemyAssassin());

        AssertThat(enemy.ActiveSkills.Count).IsEqual(2);
    }

    [TestCase]
    public void EnemyAssassin_ShadowStab_DealsDamage()
    {
        var enemy = AddNodeToTestRoot(new EnemyAssassin());
        var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
        target.CallInitialize();

        var skill = enemy.ActiveSkills[0]; // AssassinStab
        var initialHp = target.Hp;

        skill.Use(enemy, new List<IUnitSystem> { target }, null);

        AssertThat(target.Hp).IsLess(initialHp);
    }

    [TestCase]
    public void EnemyAssassin_VenomousFang_DealsDamageAndPoisons()
    {
        var enemy = AddNodeToTestRoot(new EnemyAssassin());
        var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
        target.CallInitialize();

        var skill = enemy.ActiveSkills[1]; // EnemyPoisonStrike
        var initialHp = target.Hp;

        skill.Use(enemy, new List<IUnitSystem> { target }, null);

        AssertThat(target.Hp).IsLess(initialHp);
        // Should have poison effect (currently BurningEffect as placeholder)
        AssertThat(target.GetActiveEffects().Count).IsGreater(0);
    }

    [TestCase]
    public void EnemyAssassin_LowDefense_TakesMoreDamage()
    {
        var enemy = AddNodeToTestRoot(new EnemyAssassin());

        enemy.TakeDamage(100f);

        // 100 - 10 DEF = 90 damage (very fragile)
        AssertThat(enemy.Hp).IsEqual(410f);
    }

    #endregion

    #region Enemy Light Mage Tests

    [TestCase]
    public void EnemyLightMage_Initialize_SetsCorrectStats()
    {
        var enemy = AddNodeToTestRoot(new EnemyLightMage());

        AssertThat(enemy.UnitName).IsEqual("Enemy Light Mage");
        AssertThat(enemy.Type).IsEqual(AovDataStructures.UnitType.Mage);
        AssertThat(enemy.MaxHp).IsEqual(600f);
        AssertThat(enemy.BaseAtk).IsEqual(80f);
        AssertThat(enemy.Intelligence).IsEqual(200f);
        AssertThat(enemy.Personality).IsEqual(AIPersonality.Defensive);
    }

    [TestCase]
    public void EnemyLightMage_Has2Skills()
    {
        var enemy = AddNodeToTestRoot(new EnemyLightMage());

        AssertThat(enemy.ActiveSkills.Count).IsEqual(2);
    }

    [TestCase]
    public void EnemyLightMage_LightBolt_DealsFixedDamage()
    {
        var enemy = AddNodeToTestRoot(new EnemyLightMage());
        var target = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Target" });
        target.CallInitialize();

        var skill = enemy.ActiveSkills[0]; // LightBolt
        var initialHp = target.Hp;

        skill.Use(enemy, new List<IUnitSystem> { target }, null);

        // Should deal 80 damage - target DEF
        float expectedDamage = 80f - target.BaseDef;
        AssertThat(target.Hp).IsEqual(initialHp - expectedDamage);
    }

    [TestCase]
    public void EnemyLightMage_MendingLight_HealsAlly()
    {
        var enemy = AddNodeToTestRoot(new EnemyLightMage());
        var ally = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally" });
        ally.CallInitialize();

        // Damage ally first
        ally.TakeDamage(50f);
        var damagedHp = ally.Hp;

        var skill = enemy.ActiveSkills[1]; // EnemyHealAlly

        skill.Use(enemy, new List<IUnitSystem> { ally }, null);

        // Should heal 100 HP
        float expectedHp = Math.Min(damagedHp + 150f, ally.MaxHp);
        AssertThat(ally.Hp).IsEqual(expectedHp);
    }

    #endregion

    #region Personality Tests

    [TestCase]
    public void EnemyUnits_HaveDifferentPersonalities()
    {
        var fighter = AddNodeToTestRoot(new EnemyFighter());
        var swordsman = AddNodeToTestRoot(new EnemySwordsman());
        var assassin = AddNodeToTestRoot(new EnemyAssassin());
        var mage = AddNodeToTestRoot(new EnemyLightMage());

        AssertThat(fighter.Personality).IsEqual(AIPersonality.Aggressive);
        AssertThat(swordsman.Personality).IsEqual(AIPersonality.Balanced);
        AssertThat(assassin.Personality).IsEqual(AIPersonality.Opportunistic);
        AssertThat(mage.Personality).IsEqual(AIPersonality.Defensive);
    }

    #endregion

    #region Skill Cost Tests

    [TestCase]
    public void EnemyUnits_BasicAttacks_CostZeroMana()
    {
        var fighter = AddNodeToTestRoot(new EnemyFighter());
        var swordsman = AddNodeToTestRoot(new EnemySwordsman());
        var assassin = AddNodeToTestRoot(new EnemyAssassin());
        var mage = AddNodeToTestRoot(new EnemyLightMage());

        // All basic attacks should be index 0 and cost 0 mana
        AssertThat(fighter.ActiveSkills[0].ManaCost).IsEqual(0);
        AssertThat(swordsman.ActiveSkills[0].ManaCost).IsEqual(0);
        AssertThat(assassin.ActiveSkills[0].ManaCost).IsEqual(0);
        AssertThat(mage.ActiveSkills[0].ManaCost).IsEqual(0);
    }

    [TestCase]
    public void EnemyUnits_SpecialSkills_CostMana()
    {
        var fighter = AddNodeToTestRoot(new EnemyFighter());
        var swordsman = AddNodeToTestRoot(new EnemySwordsman());
        var assassin = AddNodeToTestRoot(new EnemyAssassin());
        var mage = AddNodeToTestRoot(new EnemyLightMage());

        // All special attacks should be index 1 and cost mana
        AssertThat(fighter.ActiveSkills[1].ManaCost).IsGreater(0);
        AssertThat(swordsman.ActiveSkills[1].ManaCost).IsGreater(0);
        AssertThat(assassin.ActiveSkills[1].ManaCost).IsGreater(0);
        AssertThat(mage.ActiveSkills[1].ManaCost).IsGreater(0);
    }

    #endregion

    #region Death Tests

    [TestCase]
    public void EnemyUnits_Die_WhenHpReachesZero()
    {
        var fighter = AddNodeToTestRoot(new EnemyFighter());

        fighter.TakeDamage(5000f);

        AssertThat(fighter.Hp).IsEqual(0f);
        AssertThat(fighter.IsAlive).IsFalse();
    }

    #endregion
}
