using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AshesOfVelsingrad.Systems;
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
        unit.CallInitialize();

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

    [AfterTest]
    public void TearDown()
    {
        foreach (Node node in _testNodes)
            node.QueueFree();

        _testNodes.Clear();
        ResetSingletons();
    }
}
