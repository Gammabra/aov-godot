using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
using AshesOfVelsingrad.Utilities;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class GameManagerTest
{
    private Node? _root;
    private readonly List<Node> _testNodes = new();

    #region Helpers

    private T AddNode<T>(T node)
        where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Root is not initialized.");

        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    private void ResetSingleton()
    {
        typeof(GameManager)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null, null);
    }

    private TestConcreteUnitSystem CreateUnit(string name, float hp = 100, int speed = 4)
    {
        TestConcreteUnitSystem unit = new(hp: hp, baseSpeed: speed);
        unit.Name = name;
        return unit;
    }

    #endregion

    #region Setup / Teardown

    [BeforeTest]
    public void Setup()
    {
        ResetSingleton();
        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Clear();
        _testNodes.Add(_root);
    }

    [AfterTest]
    public void Cleanup()
    {
        foreach (Node node in _testNodes)
            node.QueueFree();
        _testNodes.Clear();
        ResetSingleton();
    }

    #endregion

    #region Tests

    [TestCase]
    public void Initialize_SetsSingleton()
    {
        GD.Print("[TEST] Start Initialize_SetsSingleton");

        GameManager manager = AddNode(new GameManager());
        manager.Call("Initialize");

        object? instance = typeof(GameManager)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null);

        AssertThat(instance).IsNotNull();
        AssertThat(instance).IsEqual(manager);
    }

    [TestCase]
    public void LoadUnits_AddsUnitsFromContainers()
    {
        GD.Print("[TEST] Start LoadUnits_AddsUnitsFromContainers");

        GameManager manager = AddNode(new GameManager());

        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());

        TestConcreteUnitSystem playerUnit = CreateUnit("Player");
        playerContainer.AddChild(playerUnit);

        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy");
        enemyContainer.AddChild(enemyUnit);

        manager.Set("_playerUnitsContainer", playerContainer);
        manager.Set("_enemyUnitsContainer", enemyContainer);

        manager.Call("LoadUnits");

        AssertThat(playerContainer.GetChildCount()).IsEqual(1);
        AssertThat(playerContainer.GetChildCount()).IsEqual(1);
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_playerUnits").Count).IsEqual(1);
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits").Count).IsEqual(1);
    }

    [TestCase]
    public void CheckWinLoseCondition_SetsVictory_WhenNoEnemiesAlive()
    {
        GD.Print("[TEST] Start CheckWinLoseCondition_SetsVictory_WhenNoEnemiesAlive");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player");
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy", 0);

        enemyUnit.SetIsAlive(false);

        SetPrivateField(manager, "_gameOutcome", AovDataStructures.GameOutcome.Ongoing);
        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);

        CallPrivateMethod(manager, "LoadUnits");

        CallPrivateMethod(manager, "CheckWinLoseCondition");

        AovDataStructures.GameOutcome outcome = GetPrivateField<AovDataStructures.GameOutcome>(manager, "_gameOutcome");
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_playerUnits")[0].IsAlive).IsTrue();
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits")[0].IsAlive).IsFalse();
        AssertThat(outcome).IsEqual(AovDataStructures.GameOutcome.Victory);
    }

    [TestCase]
    public void CheckWinLoseCondition_SetsDefeat_WhenNoPlayersAlive()
    {
        GD.Print("[TEST] Start CheckWinLoseCondition_SetsDefeat_WhenNoPlayersAlive");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player", 0);
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy");

        playerUnit.SetIsAlive(false);

        SetPrivateField(manager, "_gameOutcome", AovDataStructures.GameOutcome.Ongoing);
        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);

        CallPrivateMethod(manager, "LoadUnits");

        CallPrivateMethod(manager, "CheckWinLoseCondition");

        AovDataStructures.GameOutcome outcome = GetPrivateField<AovDataStructures.GameOutcome>(manager, "_gameOutcome");
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_playerUnits")[0].IsAlive).IsFalse();
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits")[0].IsAlive).IsTrue();
        AssertThat((int)outcome).IsEqual((int)AovDataStructures.GameOutcome.Defeat);
    }

    [TestCase]
    public void CheckUnitsLife_SetsAllUnitsDead()
    {
        GD.Print("[TEST] Start CheckUnitsLife_SetsAllUnitsDead");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player", 0);
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy", 0);

        SetPrivateField(manager, "_gameOutcome", AovDataStructures.GameOutcome.Ongoing);
        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);

        CallPrivateMethod(manager, "LoadUnits");

        List<UnitSystem> playerUnits = GetPrivateField<List<UnitSystem>>(manager, "_playerUnits");
        List<UnitSystem> enemyUnits = GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits");

        List<UnitSystem> units = playerUnits;
        units.AddRange(enemyUnits);

        CallPrivateMethod(manager, "CheckUnitsLife", units);

        AssertThat(playerUnit.IsAlive).IsFalse();
        AssertThat(enemyUnit.IsAlive).IsFalse();
    }

    [TestCase]
    public void CheckUnitTurnEnd_TriggersVictory()
    {
        GD.Print("[TEST] Start CheckUnitTurnEnd_TriggersVictory");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player");
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy", 0);

        SetPrivateField(manager, "_gameOutcome", AovDataStructures.GameOutcome.Victory);
        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);

        CallPrivateMethod(manager, "LoadUnits");

        CallPrivateMethod(manager, "CheckUnitTurnEnd");

        AovDataStructures.GameOutcome outcome = GetPrivateField<AovDataStructures.GameOutcome>(manager, "_gameOutcome");
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_playerUnits")[0].IsAlive).IsTrue();
        AssertThat(GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits")[0].IsAlive).IsFalse();
        AssertThat(outcome).IsEqual(AovDataStructures.GameOutcome.Victory);
    }

    [TestCase]
    public void HandlePlayerUnitMove_CellNotReachable_ReenablesInput()
    {
        GD.Print("[TEST] Start HandlePlayerUnitMove_CellNotReachable_ReenablesInput");

        GameManager manager = AddNode(new GameManager());
        TestConcreteMapSystem map = AddNode(new TestConcreteMapSystem());
        TurnManager turnManager = AddNode(new TurnManager());
        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());

        SetPrivateField(manager, "_battleInputSystemContainer", inputSystem);
        SetPrivateField(manager, "_mapSystemContainer", map);
        SetPrivateField(manager, "_turnManagerContainer", turnManager);
        SetPrivateField(
            manager,
            "_currentUnitPossibleMoves",
            new List<(int, int, int)>()
        );
        CallPrivateMethod(
            manager,
            "HandlePlayerUnitMove",
            new Vector3I(1, 0, 1)
        );

        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsTrue();
    }

    [TestCase]
    public void HandlePlayerUnitMove_ValidMove()
    {
        GD.Print("[TEST] Start HandlePlayerUnitMove_ValidMove_ClearsPossibleMoves");

        GameManager manager = AddNode(new GameManager());
        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());
        TestConcreteMapSystem mapSystem = AddNode(new TestConcreteMapSystem());
        TurnManager turnManager = new();

        turnManager.AddChild(turnManager);
        SetPrivateField(manager, "_battleInputSystemContainer", inputSystem);
        SetPrivateField(manager, "_mapSystemContainer", mapSystem);
        SetPrivateField(manager, "_turnManagerContainer", turnManager);
        SetPrivateField(
            manager,
            "_currentUnitPossibleMoves",
            new List<(int, int, int)> { (1, 0, 1) }
        );
        CallPrivateMethod(
            manager,
            "HandlePlayerUnitMove",
            new Vector3I(1, 0, 1)
        );

        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsTrue();
    }

    [TestCase]
    public void HandlePlayerSelectTarget_CellNotReachable_ReenablesInput()
    {
        GD.Print("[TEST] Start HandlePlayerSelectTarget_CellNotReachable_ReenablesInput");

        GameManager manager = AddNode(new GameManager());
        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());
        TestConcreteMapSystem mapSystem = AddNode(new TestConcreteMapSystem());
        TurnManager turnManager = new();

        turnManager.AddChild(turnManager);
        SetPrivateField(manager, "_battleInputSystemContainer", inputSystem);
        SetPrivateField(manager, "_mapSystemContainer", mapSystem);
        SetPrivateField(manager, "_turnManagerContainer", turnManager);
        SetPrivateField(
            manager,
            "_currentUnitReachableCellsForCurrentSelectedSkill",
            new List<(int, int, int)>()
        );
        CallPrivateMethod(
            manager,
            "HandlePlayerSelectTarget",
            new Vector3I(2, 0, 2)
        );
        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsTrue();
    }

    [TestCase]
    public void MoveUnit_MapSystemNull_ReenablesInput()
    {
        GameManager manager = AddNode(new GameManager());
        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());

        SetPrivateField(manager, "_battleInputSystemContainer", inputSystem);
        SetPrivateField(manager, "_isPlayerTurn", true);

        manager.MoveUnit(new Vector3I(1, 0, 1));

        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsTrue();
    }

    [TestCase]
    public void MoveUnit_AlreadyMoved_DoesNothing()
    {
        GameManager manager = AddNode(new GameManager());
        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());

        SetPrivateField(manager, "_battleInputSystemContainer", inputSystem);
        SetPrivateField(manager, "_unitMoved", true);
        SetPrivateField(manager, "_isPlayerTurn", true);

        manager.MoveUnit(new Vector3I(1, 0, 1));

        AssertThat(GetPrivateField<bool>(manager, "_unitMoved")).IsTrue();
        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsTrue();
    }

    [TestCase]
    public void ActivatePlayerUnit()
    {
        GD.Print("[TEST] Start ActivatePlayerUnit");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player", speed: 2);
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy", 1);
        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());
        TestConcreteMapSystem mapSystem = AddNode(new TestConcreteMapSystem());
        TurnManager turnManager = new();

        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);
        manager.AddChild(turnManager);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);
        SetPrivateField(manager, "_battleInputSystemContainer", inputSystem);
        SetPrivateField(manager, "_turnManagerContainer", turnManager);
        SetPrivateField(manager, "_mapSystemContainer", mapSystem);

        mapSystem.AddWalkableCell(0, 0, 0);
        mapSystem.AddWalkableCell(1, 0, 0);
        mapSystem.AddWalkableCell(0, 0, 1);
        mapSystem.AddWalkableCell(1, 0, 1);
        mapSystem.AddWalkableCell(-1, 0, 0);
        mapSystem.AddWalkableCell(0, 0, -1);
        mapSystem.AddWalkableCell(-1, 0, -1);
        mapSystem.AddUnit(playerUnit);
        SetPrivateField(manager, "_isPlayerTurn", false);
        inputSystem.SetInputEnabled(false);

        CallPrivateMethod(manager, "LoadUnits");
        turnManager.InitializeTurnOrder(
            GetPrivateField<List<UnitSystem>>(manager, "_playerUnits"),
            GetPrivateField<List<UnitSystem>>(manager, "_enemyUnits")
        );
        CallPrivateMethod(manager, "ActivatePlayerUnit");

        AssertThat(GetPrivateField<bool>(manager, "_isPlayerTurn")).IsTrue();
        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsTrue();
    }

    [TestCase]
    public void DeactivatePlayerUnit_And_Win()
    {
        GD.Print("[TEST] Start DeactivatePlayerUnit");

        GameManager manager = AddNode(new GameManager());
        Node playerContainer = AddNode(new Node());
        Node enemyContainer = AddNode(new Node());
        TestConcreteUnitSystem playerUnit = CreateUnit("Player", speed: 2);
        TestConcreteUnitSystem enemyUnit = CreateUnit("Enemy", 0, 1);
        BattleInputSystem inputSystem = AddNode(new BattleInputSystem());
        TestConcreteMapSystem mapSystem = AddNode(new TestConcreteMapSystem());
        TurnManager turnManager = new();

        playerContainer.AddChild(playerUnit);
        enemyContainer.AddChild(enemyUnit);
        manager.AddChild(turnManager);

        SetPrivateField(manager, "_playerUnitsContainer", playerContainer);
        SetPrivateField(manager, "_enemyUnitsContainer", enemyContainer);
        SetPrivateField(manager, "_battleInputSystemContainer", inputSystem);
        SetPrivateField(manager, "_turnManagerContainer", turnManager);
        SetPrivateField(manager, "_mapSystemContainer", mapSystem);

        SetPrivateField(manager, "_clickOnMapContext", AovDataStructures.ClickOnMapContext.SelectUnitTarget);
        SetPrivateField(manager, "_isPlayerTurn", true);
        SetPrivateField(manager, "_selectedSkill", new TestConcreteSkillSystem());
        SetPrivateField(manager, "_unitMoved", true);
        List<(int, int, int)> tupleList = [];
        tupleList.Add((1, 1, 1));
        SetPrivateField(manager, "_currentUnitPossibleMoves", tupleList);
        SetPrivateField(manager, "_currentUnitReachableCellsForCurrentSelectedSkill", tupleList);
        inputSystem.SetInputEnabled(true);

        CallPrivateMethod(manager, "LoadUnits");
        CallPrivateMethod(manager, "DeactivatePlayerUnit");

        AssertThat(GetPrivateField<AovDataStructures.ClickOnMapContext>(manager, "_clickOnMapContext"))
            .IsEqual(AovDataStructures.ClickOnMapContext.MoveUnit);
        AssertThat(GetPrivateField<bool>(manager, "_isPlayerTurn")).IsFalse();
        AssertThat(GetPrivateField<SkillSystem?>(manager, "_selectedSkill")).IsNull();
        AssertThat(GetPrivateField<bool>(manager, "_unitMoved")).IsFalse();
        AssertThat(GetPrivateField<List<(int, int, int)>>(manager, "_currentUnitPossibleMoves").Count).IsEqual(0);
        AssertThat(
                GetPrivateField<List<(int, int, int)>>(manager, "_currentUnitReachableCellsForCurrentSelectedSkill")
                    .Count
            )
            .IsEqual(0);
        AssertThat(GetPrivateField<bool>(inputSystem, "_inputEnabled")).IsFalse();
        AssertThat(playerUnit.IsAlive).IsTrue();
        AssertThat(enemyUnit.IsAlive).IsFalse();
        AssertThat(GetPrivateField<AovDataStructures.GameOutcome>(manager, "_gameOutcome"))
            .IsEqual(AovDataStructures.GameOutcome.Victory);
    }

    [TestCase]
    public void PlayerSelectedMove()
    {
        GD.Print("[TEST] Start DeactivatePlayerUnit");

        GameManager manager = AddNode(new GameManager());

        SetPrivateField(manager, "_clickOnMapContext", AovDataStructures.ClickOnMapContext.SelectUnitTarget);

        CallPrivateMethod(manager, "PlayerSelectedMove");

        AssertThat(GetPrivateField<AovDataStructures.ClickOnMapContext>(manager, "_clickOnMapContext"))
            .IsEqual(AovDataStructures.ClickOnMapContext.MoveUnit);
    }

    [TestCase]
    public void PlayerSelectedSkill_InvalidSkillId_DoesNothing()
    {
        GameManager manager = AddNode(new GameManager());
        TurnManager turnManager = new();
        TestConcreteMapSystem mapSystem = AddNode(new TestConcreteMapSystem());
        TestConcreteUnitSystem unit = CreateUnit("Player");

        turnManager.AddChild(turnManager);
        unit.ActiveSkills.Clear();

        SetPrivateField(manager, "_turnManagerContainer", turnManager);
        SetPrivateField(manager, "_mapSystemContainer", mapSystem);

        turnManager.InitializeTurnOrder(new List<UnitSystem> { unit }, new List<UnitSystem>());

        CallPrivateMethod(manager, "PlayerSelectedSkill", 0);

        AssertThat(GetPrivateField<SkillSystem?>(manager, "_selectedSkill")).IsNull();
    }

    [TestCase]
    public void PlayerSelectedSkill_SkillOnCooldown_IsRejected()
    {
        GameManager manager = AddNode(new GameManager());
        TurnManager turnManager = new();
        TestConcreteMapSystem mapSystem = AddNode(new TestConcreteMapSystem());
        TestConcreteSkillSystem skill = new(cooldown: 1);

        turnManager.AddChild(turnManager);
        skill.SetCooldown();

        TestConcreteUnitSystem unit = CreateUnit("Player");
        unit.ActiveSkills.Add(skill);

        SetPrivateField(manager, "_turnManagerContainer", turnManager);
        SetPrivateField(manager, "_mapSystemContainer", mapSystem);

        turnManager.InitializeTurnOrder(new List<UnitSystem> { unit }, new List<UnitSystem>());

        CallPrivateMethod(manager, "PlayerSelectedSkill", 0);

        AssertThat(GetPrivateField<SkillSystem?>(manager, "_selectedSkill")).IsNull();
    }

    [TestCase]
    public void EnemyTurnEnded_ResetsUnitMovedAndChecksTurnEnd()
    {
        GameManager manager = AddNode(new GameManager());
        TurnManager turnManager = new();
        TestConcreteUnitSystem enemy = CreateUnit("Enemy");

        turnManager.AddChild(turnManager);
        SetPrivateField(manager, "_turnManagerContainer", turnManager);
        SetPrivateField(manager, "_unitMoved", true);

        turnManager.InitializeTurnOrder(
            new List<UnitSystem>(),
            new List<UnitSystem> { enemy }
        );

        CallPrivateMethod(manager, "EnemyTurnEnded");

        AssertThat(GetPrivateField<bool>(manager, "_unitMoved")).IsFalse();
    }

    [TestCase]
    public void CurrentTurnEnded_ReducesAllCooldowns()
    {
        GameManager manager = AddNode(new GameManager());

        TestConcreteSkillSystem skill1 = new(cooldown: 2);
        skill1.SetCooldown();

        TestConcreteSkillSystem skill2 = new(cooldown: 1);
        skill2.SetCooldown();

        TestConcreteUnitSystem player = CreateUnit("Player");
        player.ActiveSkills.Add(skill1);

        TestConcreteUnitSystem enemy = CreateUnit("Enemy");
        enemy.ActiveSkills.Add(skill2);

        SetPrivateField(manager, "_playerUnits", new List<UnitSystem> { player });
        SetPrivateField(manager, "_enemyUnits", new List<UnitSystem> { enemy });

        CallPrivateMethod(manager, "CurrentTurnEnded");

        AssertThat(skill1.Cooldown).IsEqual(1);
        AssertThat(skill2.Cooldown).IsEqual(0);
    }

    #endregion

    #region Private Utilities

    private static T GetPrivateField<T>(object obj, string field)
    {
        return (T)obj
            .GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(obj)!;
    }

    private static void SetPrivateField(object obj, string field, object value)
    {
        obj
            .GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(obj, value);
    }

    private static object? CallPrivateMethod(object obj, string methodName, params object?[] parameters)
    {
        MethodInfo method = obj
                .GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException($"Method '{methodName}' not found on type '{obj.GetType().Name}'.");

        return method.Invoke(obj, parameters);
    }

    #endregion
}
