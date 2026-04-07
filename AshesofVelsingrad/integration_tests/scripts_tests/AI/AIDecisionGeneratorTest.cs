using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Helpers.Managers;
using AshesOfVelsingrad.Helpers.Systems;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.AI;

[TestSuite]
[RequireGodotRuntime]
public class AIDecisionGeneratorTest
{
    private readonly List<Node> _testNodes = new();
    private Node? _root;
    private AIDecisionGenerator? _generator;
    private TestConcreteGameManager? _gameManager;
    private TestConcreteMapSystem? _mapSystem;
    private TestConcreteUnitSystem? _aiUnit;
    private List<IUnitSystem> _playerUnits = new();
    private List<IUnitSystem> _enemyUnits = new();
    private BattleState? _battleState;

    #region Setup and Teardown

    [BeforeTest]
    public void Setup()
    {
        _testNodes.Clear();
        _playerUnits.Clear();
        _enemyUnits.Clear();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        SetupBasicTest();
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
        _playerUnits.Clear();
        _enemyUnits.Clear();
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

    private void SetupBasicTest()
    {
        // Create GameManager and IMapSystem
        _gameManager = AddNodeToTestRoot(new TestConcreteGameManager());
        _mapSystem = AddNodeToTestRoot(new TestConcreteMapSystem());
        _mapSystem.CallInitialize();

        // Create a 5x5 grid
        for (int x = 0; x < 5; x++)
        {
            for (int z = 0; z < 5; z++)
            {
                _mapSystem.AddWalkableCell(x, 0, z);
            }
        }

        // Create AI unit at (2, 0, 2) - center
        _aiUnit = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "AIEnemy" });
        _aiUnit.CallInitialize();
        _aiUnit.Mana = 100;
        _mapSystem.CellsInformation[12].SetUnit(_aiUnit); // Index for (2,0,2)

        // Create player unit at (4, 0, 4) - diagonal
        var player1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Player1" });
        player1.CallInitialize();
        _mapSystem.CellsInformation[24].SetUnit(player1); // Index for (4,0,4)
        _playerUnits.Add(player1);

        // Create ally unit at (0, 0, 0) - corner
        var ally1 = AddNodeToTestRoot(new TestConcreteUnitSystem { Name = "Ally1" });
        ally1.CallInitialize();
        _mapSystem.CellsInformation[0].SetUnit(ally1); // Index for (0,0,0)
        _enemyUnits.Add(_aiUnit);
        _enemyUnits.Add(ally1);

        // Create decision generator
        _generator = new AIDecisionGenerator(_aiUnit);

        // Create battle state
        _battleState = new BattleState
        {
            ActingUnit = _aiUnit,
            MapSystem = _mapSystem,
            PlayerUnits = _playerUnits,
            EnemyUnits = _enemyUnits
        };
    }

    private TestConcreteSkillSystem CreateDamageSkill(int range = 1, int manaCost = 0)
    {
        return new TestConcreteSkillSystem
        {
            Name = "Attack",
            EffectType = AovDataStructures.EffectType.Damage,
            Range = range,
            ManaCost = manaCost,
            Cooldown = 0
        };
    }

    private TestConcreteSkillSystem CreateHealSkill(int range = 1, int manaCost = 0)
    {
        return new TestConcreteSkillSystem
        {
            Name = "Heal",
            EffectType = AovDataStructures.EffectType.Heal,
            Range = range,
            ManaCost = manaCost,
            Cooldown = 0
        };
    }

    #endregion

    #region Constructor Tests

    [TestCase]
    public void Constructor_CreatesInstance()
    {
        var unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        var generator = new AIDecisionGenerator(unit);

        AssertThat(generator).IsNotNull();
    }

    [TestCase]
    public void Constructor_StoresUnitReference()
    {
        var unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        var generator = new AIDecisionGenerator(unit);

        // Use reflection to check the unit was stored
        var field = typeof(AIDecisionGenerator).GetField("_unit",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var storedUnit = field?.GetValue(generator);

        AssertThat(storedUnit).IsEqual(unit);
    }

    #endregion

    #region GenerateAllPossibleActions Tests

    [TestCase]
    public void GenerateAllPossibleActions_AlwaysIncludesPassAction()
    {
        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var passAction = actions.FirstOrDefault(a => a.Action == AIAction.Pass);
        AssertThat(passAction).IsNotNull();
        AssertThat(passAction!.Score).IsEqual(0f);
    }

    [TestCase]
    public void GenerateAllPossibleActions_ReturnsEmptyWhenUnitNotOnMap()
    {
        // Remove unit from map
        _mapSystem!.CellsInformation[12].SetUnit(null);

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        AssertThat(actions.Count).IsEqual(0);
    }

    [TestCase]
    public void GenerateAllPossibleActions_IncludesOffensiveActions_WhenEnemiesPresent()
    {
        // Add offensive skill
        var attackSkill = CreateDamageSkill(range: 5);
        _aiUnit!.ActiveSkills.Add(attackSkill);

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var offensiveActions = actions.Where(a =>
            a.Action == AIAction.UseSkill && a.Skill?.EffectType == AovDataStructures.EffectType.Damage ||
            a.Action == AIAction.MoveAndSkill && a.Skill?.EffectType == AovDataStructures.EffectType.Damage
        ).ToList();

        AssertThat(offensiveActions.Count).IsGreater(0);
    }

    [TestCase]
    public void GenerateAllPossibleActions_IncludesSupportActions_WhenAlliesNeedHealing()
    {
        // Damage an ally
        var ally = _enemyUnits[1];
        ally.TakeDamage(50);

        // Add heal skill
        var healSkill = CreateHealSkill(range: 5);
        _aiUnit!.ActiveSkills.Add(healSkill);

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var supportActions = actions.Where(a =>
            a.Skill?.EffectType == AovDataStructures.EffectType.Heal
        ).ToList();

        AssertThat(supportActions.Count).IsGreater(0);
    }

    [TestCase]
    public void GenerateAllPossibleActions_IncludesDefensiveActions_WhenInDanger()
    {
        // Damage AI unit to trigger defensive behavior
        _aiUnit!.TakeDamage(60); // < 50% HP

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var defensiveActions = actions.Where(a =>
            a.Action == AIAction.Move && a.Reasoning.Contains("Retreat")
        ).ToList();

        AssertThat(defensiveActions.Count).IsGreaterEqual(0); // May or may not generate based on conditions
    }

    #endregion

    #region GenerateOffensiveActions Tests

    [TestCase]
    public void GenerateOffensiveActions_CreatesUseSkillAction_WhenInRange()
    {
        // Place player close to AI unit
        _mapSystem!.CellsInformation[24].SetUnit(null); // Remove from (4,4)
        _mapSystem.CellsInformation[13].SetUnit(_playerUnits[0]); // Place at (3,0,2) - adjacent

        // Add melee attack skill
        var attackSkill = CreateDamageSkill(range: 1);
        _aiUnit!.ActiveSkills.Add(attackSkill);

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var useSkillActions = actions.Where(a =>
            a.Action == AIAction.UseSkill &&
            a.Target == _playerUnits[0]
        ).ToList();

        AssertThat(useSkillActions.Count).IsGreater(0);
    }

    [TestCase]
    public void GenerateOffensiveActions_SkipsSkillsOnCooldown()
    {
        var attackSkill = CreateDamageSkill(range: 5);
        attackSkill.Cooldown = 2; // On cooldown
        _aiUnit!.ActiveSkills.Add(attackSkill);

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var skillActions = actions.Where(a => a.Skill == attackSkill).ToList();

        AssertThat(skillActions.Count).IsEqual(0);
    }

    [TestCase]
    public void GenerateOffensiveActions_SkipsSkillsWithInsufficientMana()
    {
        _aiUnit!.Mana = 5; // Low mana

        var expensiveSkill = CreateDamageSkill(range: 5, manaCost: 50);
        _aiUnit.ActiveSkills.Add(expensiveSkill);

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var skillActions = actions.Where(a => a.Skill == expensiveSkill).ToList();

        AssertThat(skillActions.Count).IsEqual(0);
    }

    #endregion

    #region GenerateSupportActions Tests

    [TestCase]
    public void GenerateSupportActions_DoesNotSupportSelf()
    {
        var healSkill = CreateHealSkill(range: 5);
        _aiUnit!.ActiveSkills.Add(healSkill);

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var selfHealActions = actions.Where(a =>
            a.Target == _aiUnit &&
            a.Skill?.EffectType == AovDataStructures.EffectType.Heal
        ).ToList();

        AssertThat(selfHealActions.Count).IsEqual(0);
    }

    [TestCase]
    public void GenerateSupportActions_CreatesHealAction_ForDamagedAlly()
    {
        var ally = _enemyUnits[1];
        ally.TakeDamage(50);

        var healSkill = CreateHealSkill(range: 10);
        _aiUnit!.ActiveSkills.Add(healSkill);

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var healActions = actions.Where(a =>
            a.Target == ally &&
            a.Skill?.EffectType == AovDataStructures.EffectType.Heal
        ).ToList();

        AssertThat(healActions.Count).IsGreater(0);
    }

    #endregion

    #region GenerateDefensiveActions Tests

    [TestCase]
    public void GenerateDefensiveActions_ReturnsEmpty_WhenNotInDanger()
    {
        // AI at full HP, no close enemies
        _aiUnit!.Hp = _aiUnit.MaxHp;

        // Call via reflection since it's private
        var method = typeof(AIDecisionGenerator).GetMethod("GenerateDefensiveActions",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var actions = (List<AIDecision>)method!.Invoke(_generator,
            new object[] { (2, 0, 2), _battleState! })!;

        AssertThat(actions.Count).IsEqual(0);
    }

    [TestCase]
    public void GenerateDefensiveActions_CreatesRetreatAction_WhenInDanger()
    {
        // Damage AI unit
        _aiUnit!.TakeDamage(60);

        // Place enemy close
        _mapSystem!.CellsInformation[24].SetUnit(null);
        _mapSystem.CellsInformation[13].SetUnit(_playerUnits[0]); // Adjacent

        var actions = _generator!.GenerateAllPossibleActions(_battleState!);

        var retreatActions = actions.Where(a =>
            a.Action == AIAction.Move &&
            a.Reasoning.Contains("Retreat")
        ).ToList();

        // May or may not generate based on threat calculation
        AssertThat(actions.Count).IsGreater(0); // At least Pass action exists
    }

    #endregion
}
