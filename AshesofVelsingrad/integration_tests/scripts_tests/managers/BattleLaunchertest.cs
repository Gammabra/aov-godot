using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems.Battle;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Managers;

[TestSuite]
[RequireGodotRuntime]
public partial class BattleLauncherTest
{
    private List<Node> _testNodes = new();
    private Node? _root;

    private BattleLauncher? _originalLauncherInstance;
    private object? _originalMainInstance;

    #region Setup / Teardown

    [BeforeTest]
    public void SetUp()
    {
        _testNodes.Clear();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        // Capture original system autoload states
        _originalLauncherInstance = BattleLauncher.Instance;
        _originalMainInstance = GetStaticInstanceField(typeof(MainManager));

        // Enforce a completely clean, isolated environment
        SetStaticInstanceField(typeof(MainManager), null);
        ClearBattleLauncherInstance();
    }

    [AfterTest]
    public void TearDown()
    {
        // Fully restore execution state to preserve runner integrity
        SetStaticInstanceField(typeof(MainManager), _originalMainInstance);
        RestoreBattleLauncherInstance(_originalLauncherInstance);

        foreach (Node node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
                node.QueueFree();
        }

        _testNodes.Clear();
    }

    #endregion

    #region Reflection Helpers

    private object? GetStaticInstanceField(Type type)
    {
        FieldInfo? field = type.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
            ?? type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
        return field?.GetValue(null);
    }

    private void SetStaticInstanceField(Type type, object? value)
    {
        FieldInfo? field = type.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
            ?? type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, value);
    }

    private void ClearBattleLauncherInstance()
    {
        PropertyInfo? prop = typeof(BattleLauncher).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        prop?.SetValue(null, null);
    }

    private void RestoreBattleLauncherInstance(BattleLauncher? instance)
    {
        PropertyInfo? prop = typeof(BattleLauncher).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        prop?.SetValue(null, instance);
    }

    #endregion

    #region Integration Tests

    [TestCase]
    public void Ready_EstablishesGlobalSingletonInstance()
    {
        // Arrange
        BattleLauncher launcher = new();
        
        // Act
        _root!.AddChild(launcher);
        _testNodes.Add(launcher);

        // Assert
        AssertThat(BattleLauncher.Instance).IsEqual(launcher);
    }

    [TestCase]
    public void Launch_AbortsEarly_WhenBattleSceneIsMissing()
    {
        // Arrange
        BattleLauncher launcher = new();
        _root!.AddChild(launcher);
        _testNodes.Add(launcher);

        BattleSetup emptySetup = new()
        {
            BattleScene = null,
            EncounterName = "Empty Test Encounter"
        };

        // Act
        launcher.Launch(emptySetup);

        // Assert
        AssertThat(launcher.PendingSetup).IsNull();
    }

    [TestCase]
    public void Launch_PopulatesPendingSetup_AndStateData_OnValidCall()
    {
        // Arrange
        BattleLauncher launcher = new();
        _root!.AddChild(launcher);
        _testNodes.Add(launcher);

        PackedScene mockScene = new(); // Safe standard empty structure
        BattleSetup validSetup = new()
        {
            BattleScene = mockScene,
            EncounterName = "Prison Brawl",
            ReturnScenePath = "res://scenes/Level/Prison.tscn",
            ReturnPosition = new Vector3(10f, 1.5f, -5f)
        };

        // Act
        launcher.Launch(validSetup);

        // Assert
        AssertThat(launcher.PendingSetup).IsEqual(validSetup);
    }

    [TestCase]
    public void Forfeit_ClearsPendingSetup_AndPreparesCorrectReturnPosition()
    {
        // Arrange
        BattleLauncher launcher = new();
        _root!.AddChild(launcher);
        _testNodes.Add(launcher);

        Vector3 expectedReturnPos = new Vector3(4f, 0f, 12f);
        BattleSetup setup = new()
        {
            BattleScene = new PackedScene(),
            ReturnScenePath = "res://scenes/Level/Prison.tscn",
            ReturnPosition = expectedReturnPos,
            EncounterName = "Forfeit Match"
        };

        launcher.Launch(setup);

        // Act
        launcher.Forfeit();

        // Assert
        // The active battle state payload must be completely cleared out
        AssertThat(launcher.PendingSetup).IsNull();

        // Verify the coordinate cache matches and is successfully flushed upon read
        Vector3? consumedPos = launcher.ConsumePendingReturnPosition();
        AssertThat(consumedPos).IsEqual(expectedReturnPos);
        
        // Subsequent checks must be completely blanked out (Idempotent tracking)
        AssertThat(launcher.ConsumePendingReturnPosition()).IsNull();
    }

    [TestCase]
    public void VictoryReturn_ClearsPendingSetup_AndBuffersReturnPosition()
    {
        // Arrange
        BattleLauncher launcher = new();
        _root!.AddChild(launcher);
        _testNodes.Add(launcher);

        Vector3 expectedReturnPos = new Vector3(-8f, 2f, 0f);
        BattleSetup setup = new()
        {
            BattleScene = new PackedScene(),
            ReturnScenePath = "res://scenes/Level/Prison.tscn",
            ReturnPosition = expectedReturnPos,
            EncounterName = "Victory Match"
        };

        launcher.Launch(setup);

        // Act
        launcher.VictoryReturn();

        // Assert
        AssertThat(launcher.PendingSetup).IsNull();

        Vector3? consumedPos = launcher.ConsumePendingReturnPosition();
        AssertThat(consumedPos).IsEqual(expectedReturnPos);
        AssertThat(launcher.ConsumePendingReturnPosition()).IsNull();
    }

    #endregion
}
