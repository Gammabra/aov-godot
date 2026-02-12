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
        AssertThat(unit.Log).Contains("Initialized");
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
        AssertThat(unit.Mana).IsEqual(90);
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

        List<Vector3I> moves = unit.GetPossibleMoves(map);
        AssertThat(moves.Contains(new Vector3I(1, 0, 0))).IsTrue();
        AssertThat(moves.Contains(new Vector3I(0, 0, 0))).IsFalse();
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
    public void Initialize_SetsCharacterSprite_WhenSprite3DChildExists()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        
        // Add a Sprite3D child
        Sprite3D sprite = new Sprite3D { Name = "TestSprite" };
        unit.AddChild(sprite);
        
        unit.CallInitialize();

        AssertThat(unit.CharacterSprite).IsNotNull();
        AssertThat(unit.CharacterSprite).IsEqual(sprite);
    }

    [TestCase]
    public void Initialize_CharacterSpriteIsNull_WhenNoSprite3DChild()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        AssertThat(unit.CharacterSprite).IsNull();
    }

    [TestCase]
    public void CanMoveTo_ReturnsTrueForWalkableCell()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);

        bool canMove = unit.CanMoveTo(0, 0, 0, map);
        AssertThat(canMove).IsTrue();
    }

    [TestCase]
    public void CanMoveTo_ReturnsFalseForNonWalkableCell()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddEmptyCell(0, 0, 0);

        bool canMove = unit.CanMoveTo(0, 0, 0, map);
        AssertThat(canMove).IsFalse();
    }

    [TestCase]
    public void SetGridPosition_MovesUnitToCorrectPosition()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);
        map.AddWalkableCell(1, 0, 0);
        
        // Place unit at (0,0,0)
        map.CellsInformation[0].Unit = unit;

        // Move to (1,0,0)
        unit.SetGridPosition(1, 0, 0, map);

        AssertThat(map.GetUnitAt(1, 0, 0)).IsEqual(unit);
        AssertThat(map.GetUnitAt(0, 0, 0)).IsNull();
    }

    [TestCase]
    public void SetGridPosition_HandlesOutOfRangeGracefully()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);

        // Should not throw, just print error
        unit.SetGridPosition(99, 99, 99, map);
        
        // Unit should still be at original position (not moved)
        AssertThat(map.GetUnitAt(0, 0, 0)).IsNull();
    }

    [TestCase]
    public void TakeDamage_NeverReducesBelowZero()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        // Defense is 5, so 5 damage should result in 0 real damage
        unit.TakeDamage(5);
        AssertThat(unit.Hp).IsEqual(100); // No damage taken

        // Less than defense also results in 0 damage
        unit.TakeDamage(3);
        AssertThat(unit.Hp).IsEqual(100);
    }

    [TestCase]
    public void TakeDamage_CanReduceHpToZero()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        // Deal 105 damage (100 HP + 5 defense)
        unit.TakeDamage(105);
        AssertThat(unit.Hp).IsEqual(0);
    }

    [TestCase]
    public void TakeDamage_CanReduceHpBelowZero()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        // Deal 200 damage
        unit.TakeDamage(200);
        AssertThat(unit.Hp).IsLess(0);
    }

    [TestCase]
    public void SetIsAlive_OnlySetsFalseWhenHpIsZeroOrLess()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        // Try to set dead when HP is positive - should not work
        unit.SetIsAlive(false);
        AssertThat(unit.IsAlive).IsTrue(); // Should still be alive

        // Now reduce HP to 0
        unit.TakeDamage(105);
        unit.SetIsAlive(false);
        AssertThat(unit.IsAlive).IsFalse(); // Now it should work
    }

    [TestCase]
    public void SetIsAlive_OnlySetsTrueWhenHpIsPositive()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        // Kill the unit
        unit.TakeDamage(105);
        unit.SetIsAlive(false);

        // Try to revive when HP is still 0 - should not work
        unit.SetIsAlive(true);
        AssertThat(unit.IsAlive).IsFalse(); // Should still be dead
    }

    [TestCase]
    public void GetActiveEffects_ReturnsAllAppliedEffects()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteStatusEffect<UnitSystem> effect1 = new();
        TestConcreteStatusEffect<UnitSystem> effect2 = new();
        
        unit.ApplyEffect(effect1);
        unit.ApplyEffect(effect2);

        var activeEffects = unit.GetActiveEffects();
        AssertThat(activeEffects.Count).IsEqual(2);
        AssertThat(activeEffects.Contains(effect1)).IsTrue();
        AssertThat(activeEffects.Contains(effect2)).IsTrue();
    }

    [TestCase]
    public void GetActiveEffects_ReturnsEmptyListWhenNoEffects()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        var activeEffects = unit.GetActiveEffects();
        AssertThat(activeEffects.Count).IsEqual(0);
    }

    [TestCase]
    public void WaitForActionAsync_TaskCompletesOnPassTurn()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        Task task = unit.WaitForActionAsync();
        AssertThat(task.IsCompleted).IsFalse();

        unit.PassTurn();
        AssertThat(task.IsCompleted).IsTrue();
    }

    [TestCase]
    public void WaitForActionAsync_TaskCompletesOnPlay()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteSkillSystem skill = new();
        unit.ActiveSkills.Add(skill);

        Task task = unit.WaitForActionAsync();
        AssertThat(task.IsCompleted).IsFalse();

        unit.Play(new List<UnitSystem>(), null, skill);
        AssertThat(task.IsCompleted).IsTrue();
    }

    [TestCase]
    public void Play_MultipleSkills_ReducesManaCorrectly()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteSkillSystem skill1 = new();
        TestConcreteSkillSystem skill2 = new();
        unit.ActiveSkills.Add(skill1);
        unit.ActiveSkills.Add(skill2);

        unit.Play(new List<UnitSystem>(), null, skill1);
        AssertThat(unit.Mana).IsEqual(90);

        // In real scenario, mana would regenerate or this would be a new turn
        // But for testing, just verify it can be called again
        unit.Play(new List<UnitSystem>(), null, skill2);
        AssertThat(unit.Mana).IsEqual(80);
    }

    [TestCase]
    public void GetPossibleMoves_ReturnsEmptyWhenUnitNotOnMap()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);
        // Don't add unit to map

        List<Vector3I> moves = unit.GetPossibleMoves(map);
        AssertThat(moves.Count).IsEqual(0);
    }

    [TestCase]
    public void GetPossibleMoves_DoesNotIncludeCurrentPosition()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);
        map.AddWalkableCell(1, 0, 0);
        map.AddUnit(unit);

        List<Vector3I> moves = unit.GetPossibleMoves(map);
        
        Vector3I? unitPos = map.GetUnitPosition(unit);
        AssertThat(unitPos).IsNotNull();
        AssertThat(moves.Contains(unitPos!.Value)).IsFalse();
    }

    [TestCase]
    public void GetReachableCellsForSkills_ReturnsEmptyWhenUnitNotOnMap()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteSkillSystem skill = new();
        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);
        // Don't add unit to map

        List<(int, int, int)> reachable = unit.GetReachableCellsForSkills(map, skill);
        AssertThat(reachable.Count).IsEqual(0);
    }

    [TestCase]
    public void GetReachableCellsForSkills_OnlyReturnsNonEmptyCells()
    {
        TestConcreteUnitSystem unit = AddNodeToTestRoot(new TestConcreteUnitSystem());
        unit.CallInitialize();

        TestConcreteSkillSystem skill = new();
        TestConcreteMapSystem map = CreateAndInitializeMap<TestConcreteMapSystem>();
        map.AddWalkableCell(0, 0, 0);
        map.AddWalkableCell(1, 0, 0);
        map.AddEmptyCell(2, 0, 0); // This should not be included
        map.AddUnit(unit);

        List<(int, int, int)> reachable = unit.GetReachableCellsForSkills(map, skill);
        
        AssertThat(reachable.Contains((1, 0, 0))).IsTrue();
        AssertThat(reachable.Contains((2, 0, 0))).IsFalse(); // Empty cells filtered out
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
