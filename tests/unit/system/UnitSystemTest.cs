using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class UnitSystemTest
{
    private readonly List<Node> _testNodes = new();
    private Node? _root;

    #region Private Methods

    private T AddNodeToTestRoot<T>(T node)
        where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Test root node is not initialized.");
        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    private T CreateAndInitializeMap<T>()
        where T : MapSystem, new()
    {
        T mapSystem = AddNodeToTestRoot(new T());

        if (mapSystem is TestConcreteMapSystem tcms)
            tcms.CallInitialize();

        return mapSystem;
    }

    private void SetSingletonInstance<T>(T? instance)
        where T : class
    {
        PropertyInfo? instanceProperty = typeof(T).GetProperty(
            "Instance",
            BindingFlags.Public | BindingFlags.Static
        );
        instanceProperty?.SetValue(null, instance);
    }

    private void ResetSingletons()
    {
        TestConcreteMapSystem.Instance = null;
        SetSingletonInstance<MapSystem>(null);
    }

    #endregion

    [BeforeTest]
    public void Setup()
    {
        ResetSingletons();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Clear();
        _testNodes.Add(_root);
    }

    [TestCase]
    public void Initialize_IsCalled_SetsFlagsAndDefaults()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        AssertThat(unit.IsInitialized).IsTrue();
        AssertThat(unit.UnitName).IsEqual("TestUnit");
        AssertThat(unit.MaxHp).IsEqual(100);
        AssertThat(unit.Hp).IsEqual(100);
    }

    [TestCase]
    public void Cleanup_IsCalled_SetsFlag()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();
        unit.CallCleanup();

        AssertThat(unit.IsCleanedUp).IsTrue();
        AssertThat(unit.Log).Contains("CleanedUp");
    }

    [TestCase]
    public void InjectDependencies_SetsStatusEffectSystem()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        StatusEffectSystem ses = new();
        unit.InjectDependencies(ses);

        AssertThat(unit.InjectedStatusEffectSystem).IsEqual(ses);
        AssertThat(unit.Log).Contains("StatusEffectSystem injected");
    }

    [TestCase]
    public void SetIsAlive_UpdatesIsAliveFlag()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        unit.SetIsAlive(true);
        AssertThat(unit.IsAlive).IsTrue();

        unit.TakeDamage(200);
        unit.SetIsAlive(false);
        AssertThat(unit.IsAlive).IsFalse();
    }

    [TestCase]
    public void TakeDamage_ReducesHpBasedOnDefense()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());

        unit.TakeDamage(20);
        AssertThat(unit.Hp).IsEqual(85);
    }

    [TestCase]
    public void SetStatusEffectOnUnit_UsesInjectedStatusEffectSystem()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        StatusEffectSystem ses = new();
        unit.InjectDependencies(ses);

        TestConcreteStatusEffect<UnitSystem> effect = new();
        unit.SetStatusEffectOnUnit(effect);

        AssertThat(unit.HasEffect<TestConcreteStatusEffect<UnitSystem>>()).IsTrue();
    }

    [TestCase]
    public void PassTurn_CompletesActionTask()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        Task task = unit.WaitForActionAsync();
        unit.PassTurn();
        AssertThat(task.IsCompleted).IsTrue();
    }

    [TestCase]
    public void Play_UsesSkillAndReducesMana()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteSkillSystem skill = new();
        unit.ActiveSkills.Add(skill);

        unit.Play(new List<UnitSystem>(), null, skill);

        AssertThat(skill.WasUsed).IsTrue();
        AssertThat(unit.ManaPoint).IsEqual(95);
    }

    [TestCase]
    public void Play_SkillNotActive_DoesNotUseSkill()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteSkillSystem skill = new();
        unit.Play(new List<UnitSystem>(), null, skill);

        AssertThat(skill.WasUsed).IsFalse();
    }

    [TestCase]
    public void MoveTo_CallsSetGridPositionIfCanMove()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);

        bool result = unit.MoveTo(0, 0, 0, map);
        AssertThat(result).IsTrue();
        AssertThat(map.GetUnitAt(0, 0, 0)).Equals(unit);
    }

    [TestCase]
    public void MoveTo_ReturnsFalseIfCannotMove()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);
        map.AddEmptyCell(1, 0, 0);

        bool result = unit.MoveTo(1, 0, 0, map);
        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void StatusEffect_ApplyRemoveHasEffect()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteStatusEffect<UnitSystem> effect = new();
        unit.ApplyEffect(effect);

        AssertThat(unit.HasEffect<TestConcreteStatusEffect<UnitSystem>>()).IsTrue();

        unit.RemoveEffect(effect);
        AssertThat(unit.HasEffect<TestConcreteStatusEffect<UnitSystem>>()).IsFalse();
    }

    [TestCase]
    public void GetPossibleMoves_ReturnsExpectedCells()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);
        map.AddWalkableCell(1, 0, 0);
        map.AddUnit(unit);

        List<(int, int, int)> moves = unit.GetPossibleMoves(map);
        AssertThat(moves.Contains((1, 0, 0))).IsTrue();
        AssertThat(moves.Contains((0, 0, 0))).IsFalse();
    }

    [TestCase]
    public void GetReachableCellsForSkills_ReturnsCellsWithinRange()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteSkillSystem skill = new();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);
        map.AddWalkableCell(1, 0, 0);
        map.AddUnit(unit);

        List<(int, int, int)> reachable = unit.GetReachableCellsForSkills(map, skill);
        AssertThat(reachable.Contains((1, 0, 0))).IsTrue();
    }

    [TestCase]
    public void OnEffectDamageFlat()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem());
        TestConcreteUnitSystem unit2 = AddNodeToTestRoot(new TestConcreteUnitSystem(maxHp: 150, hp: 150));
        unit1.CallInitialize();

        unit1.OnEffectDamage(AovDataStructures.ModifierType.Flat, 10);
        unit2.OnEffectDamage(AovDataStructures.ModifierType.Flat, 10);
        AssertThat(unit1.Hp).IsEqual(90);
        AssertThat(unit2.Hp).IsEqual(140);
    }

    [TestCase]
    public void OnEffectDamagePercent()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem());
        TestConcreteUnitSystem unit2 = AddNodeToTestRoot(new TestConcreteUnitSystem(maxHp: 150, hp: 150));
        unit1.CallInitialize();

        unit1.OnEffectDamage(AovDataStructures.ModifierType.Percent, 10);
        unit2.OnEffectDamage(AovDataStructures.ModifierType.Percent, 10);
        AssertThat(unit1.Hp).IsEqual(90);
        AssertThat(unit2.Hp).IsEqual(135);
    }

    [TestCase]
    public void OnEffectHeal()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem(hp: 40));
        TestConcreteUnitSystem unit2 = AddNodeToTestRoot(new TestConcreteUnitSystem(hp: 90));
        unit1.CallInitialize();

        unit1.OnEffectHeal(30);
        unit2.OnEffectHeal(100);
        AssertThat(unit1.Hp).IsEqual(70);
        AssertThat(unit2.Hp).IsEqual(100);
    }

    [TestCase]
    public void OnEffectHeal_UnitNotAlive()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem(hp: 0, isAlive: false));
        unit1.CallInitialize();

        unit1.OnEffectHeal(30);
        AssertThat(unit1.Hp).IsEqual(0);
    }

    [TestCase]
    public void OnEffectRevive()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem(hp: 0, isAlive: false));
        TestConcreteUnitSystem unit2 = AddNodeToTestRoot(new TestConcreteUnitSystem(maxHp: 241, hp: 0, isAlive: false));
        unit1.CallInitialize();

        unit1.OnEffectRevive(AovDataStructures.ModifierType.Flat, 30);
        unit2.OnEffectRevive(AovDataStructures.ModifierType.Percent, 70);
        AssertThat(unit1.Hp).IsEqual(30);
        AssertThat(unit1.IsAlive).IsEqual(true);
        AssertThat(unit2.Hp).IsEqual(168.7f);
        AssertThat(unit2.IsAlive).IsEqual(true);
    }

    [TestCase]
    public void OnEffectRevive_UnitAlreadyAlive()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem(hp: 40));
        unit1.CallInitialize();

        unit1.OnEffectRevive(AovDataStructures.ModifierType.Flat, 10);
        AssertThat(unit1.Hp).IsEqual(40);
    }

    [TestCase]
    public void OnEffectModifierApplied_Atk()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseAtk: 40));
        TestConcreteUnitSystem unit2 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseAtk: 88));
        TestConcreteUnitSystem unit3 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseAtk: 64));
        TestConcreteUnitSystem unit4 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseAtk: 111));
        unit1.CallInitialize();
        unit2.CallInitialize();
        unit3.CallInitialize();
        unit4.CallInitialize();

        unit1.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Flat,
            10
        );
        unit2.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Percent,
            20
        );
        unit3.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Flat,
            -15
        );
        unit4.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Percent,
            -3
        );
        AssertThat(unit1.TotalAtk).IsEqual(50);
        AssertThat(unit2.TotalAtk).IsEqual(105.6f);
        AssertThat(unit3.TotalAtk).IsEqual(49);
        AssertThat(unit4.TotalAtk).IsEqual(107.67f);
    }

    [TestCase]
    public void OnEffectModifierApplied_Def()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseDef: 40));
        TestConcreteUnitSystem unit2 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseDef: 88));
        TestConcreteUnitSystem unit3 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseDef: 64));
        TestConcreteUnitSystem unit4 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseDef: 111));
        unit1.CallInitialize();
        unit2.CallInitialize();
        unit3.CallInitialize();
        unit4.CallInitialize();

        unit1.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Flat,
            10
        );
        unit2.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Percent,
            20
        );
        unit3.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Flat,
            -15
        );
        unit4.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Percent,
            -3
        );
        AssertThat(unit1.TotalDef).IsEqual(50);
        AssertThat(unit2.TotalDef).IsEqual(105.6f);
        AssertThat(unit3.TotalDef).IsEqual(49);
        AssertThat(unit4.TotalDef).IsEqual(107.67f);
    }

    [TestCase]
    public void OnEffectModifierRemoved_Atk()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseAtk: 40));
        TestConcreteUnitSystem unit2 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseAtk: 88));
        TestConcreteUnitSystem unit3 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseAtk: 64));
        TestConcreteUnitSystem unit4 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseAtk: 111));
        unit1.CallInitialize();
        unit2.CallInitialize();
        unit3.CallInitialize();
        unit4.CallInitialize();

        unit1.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Flat,
            10
        );
        unit2.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Percent,
            20
        );
        unit3.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Flat,
            -15
        );
        unit4.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Percent,
            -3
        );
        unit1.OnEffectModifierRemoved(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Flat,
            10
        );
        unit2.OnEffectModifierRemoved(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Percent,
            20
        );
        unit3.OnEffectModifierRemoved(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Flat,
            -15
        );
        unit4.OnEffectModifierRemoved(
            AovDataStructures.StatTypeWithModifier.Atk,
            AovDataStructures.ModifierType.Percent,
            -3
        );
        AssertThat(unit1.TotalAtk).IsEqual(40);
        AssertThat(unit2.TotalAtk).IsEqual(88);
        AssertThat(unit3.TotalAtk).IsEqual(64);
        AssertThat(unit4.TotalAtk).IsEqual(111);
    }

    [TestCase]
    public void OnEffectModifierRemoved_Def()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseDef: 40));
        TestConcreteUnitSystem unit2 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseDef: 88));
        TestConcreteUnitSystem unit3 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseDef: 64));
        TestConcreteUnitSystem unit4 = AddNodeToTestRoot(new TestConcreteUnitSystem(baseDef: 111));
        unit1.CallInitialize();
        unit2.CallInitialize();
        unit3.CallInitialize();
        unit4.CallInitialize();

        unit1.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Flat,
            10
        );
        unit2.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Percent,
            20
        );
        unit3.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Flat,
            -15
        );
        unit4.OnEffectModifierApplied(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Percent,
            -3
        );
        unit1.OnEffectModifierRemoved(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Flat,
            10
        );
        unit2.OnEffectModifierRemoved(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Percent,
            20
        );
        unit3.OnEffectModifierRemoved(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Flat,
            -15
        );
        unit4.OnEffectModifierRemoved(
            AovDataStructures.StatTypeWithModifier.Def,
            AovDataStructures.ModifierType.Percent,
            -3
        );
        AssertThat(unit1.TotalDef).IsEqual(40);
        AssertThat(unit2.TotalDef).IsEqual(88);
        AssertThat(unit3.TotalDef).IsEqual(64);
        AssertThat(unit4.TotalDef).IsEqual(111);
    }

    [TestCase]
    public void OnEffectControlApplied()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit1.CallInitialize();

        unit1.OnEffectControlApplied();
        AssertThat(unit1.IsControlled).IsTrue();
    }

    [TestCase]
    public void OnEffectControlRemoved()
    {
        TestConcreteUnitSystem unit1 = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit1.CallInitialize();

        unit1.OnEffectControlRemoved();
        AssertThat(unit1.IsControlled).IsFalse();
    }

    [AfterTest]
    public void TearDown()
    {
        foreach (Node node in _testNodes)
            node.QueueFree();

        _testNodes.Clear();
        ResetSingletons();
    }
}
