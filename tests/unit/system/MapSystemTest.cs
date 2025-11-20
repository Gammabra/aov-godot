using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Systems;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class MapSystemTest
{
    private List<Node> _testNodes = new();
    private Node? _root;

    #region Private Methods

    private T AddToTestRoot<T>(T node)
        where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Test root node is not initialized.");
        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    private T CreateAndInitialize<T>()
        where T : MapSystem, new()
    {
        T mapSystem = AddToTestRoot(new T());
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
    public void SetUp()
    {
        ResetSingletons();
        _testNodes.Clear();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);
    }

    [TestCase]
    public void MapSystem_IsAbstract()
    {
        AssertThat(typeof(MapSystem).IsAbstract).IsTrue();
    }

    [TestCase]
    public void ConcreteMapSystem_Initialize_SetsSingleton()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        AssertThat(TestConcreteMapSystem.Instance).IsEqual(map);
        AssertThat(map.IsInitialized).IsTrue();
    }

    [TestCase]
    public void ConcreteMapSystem_Cleanup_ClearsSingletonAndData()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);
        map.CallCleanup();

        AssertThat(map.IsCleanedUp).IsTrue();
        AssertThat(TestConcreteMapSystem.Instance).IsNull();
        AssertThat(map.CellsInformation.Count).IsEqual(0);
        AssertThat(map.GetUsedCells().Length).IsEqual(0);
    }

    [TestCase]
    public void SingletonPattern_PreventsMultipleInstances()
    {
        TestConcreteMapSystem first = CreateAndInitialize<TestConcreteMapSystem>();
        TestConcreteMapSystem second = AddToTestRoot(new TestConcreteMapSystem());
        second.CallInitialize();

        AssertThat(TestConcreteMapSystem.Instance).IsEqual(first);
        AssertThat(second.IsQueuedForDeletion()).IsTrue();
    }

    [TestCase]
    public void AddCell_AddsCorrectly()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        map.AddWalkableCell(1, 2, 3);

        AssertThat(map.CellsInformation.Count).IsEqual(1);
        AssertThat(map.GetUsedCells()[0]).IsEqual(new Vector3I(1, 2, 3));
        AssertThat(map.CellsInformation[0].IsWalkable).IsTrue();
    }

    [TestCase]
    public void AddEmptyCell_AddsNonWalkableCell()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        map.AddEmptyCell(4, 5, 6);

        AssertThat(map.CellsInformation[0].CellType).IsEqual(CellType.Empty);
        AssertThat(map.CellsInformation[0].IsWalkable).IsFalse();
    }

    [TestCase]
    public void GetUsedCells_ReturnsCorrectArray()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        map.AddWalkableCell(1, 0, 0);
        map.AddEmptyCell(0, 1, 0);

        Vector3I[] usedCells = map.GetUsedCells();
        AssertThat(usedCells.Length).IsEqual(2);
        AssertThat(usedCells[0]).IsEqual(new Vector3I(1, 0, 0));
        AssertThat(usedCells[1]).IsEqual(new Vector3I(0, 1, 0));
    }

    [TestCase]
    public void Ready_CallsInitialize_WhenInstanceIsNull()
    {
        TestConcreteMapSystem.Instance = null;
        TestConcreteMapSystem map = AddToTestRoot(new TestConcreteMapSystem());
        map.CallReady();

        AssertThat(TestConcreteMapSystem.Instance).IsEqual(map);
        AssertThat(map.IsInitialized).IsTrue();
    }

    [TestCase]
    public void IsEmpty_ReturnsCorrectValues()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        map.AddEmptyCell(0, 0, 0);
        map.AddWalkableCell(1, 0, 0);

        AssertThat(map.IsEmpty(0, 0, 0)).IsTrue();
        AssertThat(map.IsEmpty(1, 0, 0)).IsFalse();
    }

    [TestCase]
    public void IsWalkable_ReturnsCorrectValues()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        map.AddWalkableCell(2, 0, 0);
        map.AddEmptyCell(3, 0, 0);

        AssertThat(map.IsWalkable(2, 0, 0)).IsTrue();
        AssertThat(map.IsWalkable(3, 0, 0)).IsFalse();
    }

    [TestCase]
    public void SetWalkable_TogglesCorrectly()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 1, 0);

        AssertThat(map.IsWalkable(0, 1, 0)).IsTrue();

        map.SetWalkable(0, 1, 0);
        AssertThat(map.IsWalkable(0, 1, 0)).IsFalse();

        map.SetWalkable(0, 1, 0);
        AssertThat(map.IsWalkable(0, 1, 0)).IsTrue();
    }

    [TestCase]
    public void MoveUnit_MovesCorrectly()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        TestConcreteUnitSystem unit = AddToTestRoot(new TestConcreteUnitSystem());

        map.AddWalkableCell(0, 0, 0);
        map.AddWalkableCell(1, 0, 0);

        map.CellsInformation[0].Unit = unit;

        map.MoveUnit(unit, 1, 0, 0);

        AssertThat(map.GetUnitAt(1, 0, 0)).IsEqual(unit);
        AssertThat(map.GetUnitAt(0, 0, 0)).IsNull();
    }

    [TestCase]
    public void GetUnitPosition_ReturnsCorrectCoordinates()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        TestConcreteUnitSystem unit = AddToTestRoot(new TestConcreteUnitSystem());

        map.AddWalkableCell(5, 5, 5);
        map.CellsInformation[0].Unit = unit;

        (int, int, int)? pos = map.GetUnitPosition(unit);
        AssertThat(pos.HasValue).IsTrue();
        if (pos != null)
            AssertThat(pos.Value).IsEqual((5, 5, 5));
    }

    [TestCase]
    public void RemoveUnit_RemovesCorrectly()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        TestConcreteUnitSystem unit = AddToTestRoot(new TestConcreteUnitSystem());

        map.AddWalkableCell(1, 1, 1);
        map.CellsInformation[0].Unit = unit;

        map.RemoveUnit(1, 1, 1);

        AssertThat(map.GetUnitAt(1, 1, 1)).IsNull();
    }

    [TestCase]
    public void SetStatusEffectOnCells_AppliesEffect()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();
        StatusEffectSystem ses = new();
        map.InjectDependencies(ses);

        TestConcreteStatusEffect<CellInformation> effect = new();

        map.AddWalkableCell(0, 0, 0);
        map.AddWalkableCell(1, 0, 0);

        map.SetStatusEffectOnCells(
            new List<(int, int, int)> { (0, 0, 0), (1, 0, 0) },
            effect
        );

        AssertThat(effect.ApplyCalled).IsTrue();

        CellInformation? lastTarget = effect.LastApplyTarget as CellInformation;
        AssertThat(lastTarget).IsNotNull();
        if (lastTarget != null)
            AssertThat(
                    (lastTarget.X == 0 && lastTarget.Y == 0 && lastTarget.Z == 0) ||
                    (lastTarget.X == 1 && lastTarget.Y == 0 && lastTarget.Z == 0)
                )
                .IsTrue();
    }

    [TestCase]
    public void WidthHeightDepth_ZeroWhenNoCells()
    {
        TestConcreteMapSystem map = CreateAndInitialize<TestConcreteMapSystem>();

        AssertThat(map.Width).IsEqual(0);
        AssertThat(map.Height).IsEqual(0);
        AssertThat(map.Depth).IsEqual(0);
    }

    [AfterTest]
    public void TearDown()
    {
        foreach (Node node in _testNodes)
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
                node.QueueFree();

        _testNodes.Clear();
        ResetSingletons();
    }
}
