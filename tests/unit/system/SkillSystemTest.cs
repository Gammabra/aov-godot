using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class SkillSystemTest
{
    private readonly List<Node> _testNodes = new();
    private Node? _root;

    #region Private Methods

    private T CreateAndInitializeMap<T>()
        where T : MapSystem, new()
    {
        T mapSystem = AddNodeToTestRoot(new T());

        if (mapSystem is TestConcreteMapSystem tcms)
            tcms.CallInitialize();

        return mapSystem;
    }

    private T AddNodeToTestRoot<T>(T node)
        where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Test root node is not initialized.");
        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
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

    #region Tests

    [TestCase]
    public void Constructor_InitializesProperties()
    {
        TestConcreteSkillSystem skill = new();

        AssertThat(skill.Name).IsEqual("TestSkill");
        AssertThat(skill.Description).IsEqual("Test skill description");
        AssertThat(skill.ManaCost).IsEqual(10f);
        AssertThat(skill.TotalCooldown).IsEqual(0);
        AssertThat(skill.Cooldown).IsEqual(0);
        AssertThat(skill.Range).IsEqual(1);
        AssertThat(skill.MagicType).IsEqual(AovDataStructures.MagicType.None);
        AssertThat(skill.EffectType).IsEqual(AovDataStructures.EffectType.Damage);
        AssertThat(skill.TargetType).IsEqual(AovDataStructures.TargetTypes.SingleEnemy);
        AssertThat(skill.AreaEffect).IsNotNull();
    }

    [TestCase]
    public void Use_RegistersTargetsAndMap()
    {
        TestConcreteSkillSystem skill = new();
        TestConcreteUnitSystem unitA = AddNodeToTestRoot(new TestConcreteUnitSystem());
        TestConcreteUnitSystem unitB = AddNodeToTestRoot(new TestConcreteUnitSystem());
        List<UnitSystem> targets = new() { unitA, unitB };
        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();

        skill.Use(unitA, targets, map);

        AssertThat(skill.WasUsed).IsTrue();
        AssertThat(skill.LastTargets.Count).IsEqual(2);
        AssertThat(skill.LastTargets[0]).IsEqual(targets[0]);
        AssertThat(skill.LastTargets[1]).IsEqual(targets[1]);
        AssertThat(skill.LastMap).IsEqual(map);
    }

    [TestCase]
    public void SetCooldown_SetsCooldown_WhenZero()
    {
        TestConcreteSkillSystem skill = new("A", cooldown: 3);

        skill.SetCooldown();

        AssertThat(skill.Cooldown).IsEqual(3);
    }

    [TestCase]
    public void SetCooldown_DoesNotOverride_WhenAlreadySet()
    {
        TestConcreteSkillSystem skill = new("A", cooldown: 4);

        skill.SetCooldown(); // -> 4
        skill.SetCooldown(); // -> 4

        AssertThat(skill.Cooldown).IsEqual(4);
    }

    [TestCase]
    public void ReduceCooldown_Reduces_WhenAboveZero()
    {
        TestConcreteSkillSystem skill = new("A", cooldown: 3);

        skill.SetCooldown(); // 3
        skill.ReduceCooldown(); // 2

        AssertThat(skill.Cooldown).IsEqual(2);
    }

    [TestCase]
    public void ReduceCooldown_DoesNotGoNegative()
    {
        TestConcreteSkillSystem skill = new();

        skill.ReduceCooldown();

        AssertThat(skill.Cooldown).IsEqual(0);
    }

    [TestCase]
    public void ReduceCooldown_ReducesOncePerCall()
    {
        TestConcreteSkillSystem skill = new("A", cooldown: 2);

        skill.SetCooldown(); // 2

        skill.ReduceCooldown(); // -> 1
        skill.ReduceCooldown(); // -> 0
        skill.ReduceCooldown(); // -> 0

        AssertThat(skill.Cooldown).IsEqual(0);
    }

    #endregion

    [AfterTest]
    public void TearDown()
    {
        foreach (Node node in _testNodes)
            node.QueueFree();

        _testNodes.Clear();
        ResetSingletons();
    }
}
