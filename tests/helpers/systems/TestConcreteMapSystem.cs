using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace UnitTests;

public sealed partial class TestConcreteMapSystem : MapSystem
{
    public new static TestConcreteMapSystem? Instance { get; set; }

    public bool IsInitialized { get; private set; }
    public bool IsCleanedUp { get; private set; }

    private readonly List<Vector3I> _manualCells = new();

    public TestConcreteMapSystem()
    {
        Name = "TestConcreteMapSystem";
        GD.Print("[TEST] TestConcreteMapSystem constructor called");
    }

    protected override void Initialize()
    {
        if (IsInitialized)
            return;
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected.");
            QueueFree();
            return;
        }

        MapSystem.Instance = this;
        Instance = this;

        MapCellSize = new Vector3(1, 1, 1);
        IsInitialized = true;

        GD.Print("[TEST] TestConcreteMapSystem initialized");
    }

    public override void PlaceUnits(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
    {
        // Ensure we have enough cells
        while (CellsInformation.Count < 2)
        {
            CellsInformation.Add(new CellInformation(CellsInformation.Count, 0, 0, CellType.Grass, true));
        }
        
        if (playerUnits.Count > 0)
            CellsInformation[0].Unit = playerUnits[0];
        
        if (enemyUnits.Count > 0)
            CellsInformation[1].Unit = enemyUnits[0];
    }

    public void AddUnit(UnitSystem unit)
    {
        CellsInformation[0].SetUnit(unit);

        GD.Print("[TEST] TestConcreteMapSystem AddUnits called");
    }

    protected override void Cleanup()
    {
        IsCleanedUp = true;
        _manualCells.Clear();
        CellsInformation.Clear();

        if (Instance == this)
            Instance = null;

        if (MapSystem.Instance == this)
            MapSystem.Instance = null;

        GD.Print("[TEST] TestConcreteMapSystem cleanup called");
    }

    public new Vector3I[] GetUsedCells()
    {
        GD.Print("[TEST] TestConcreteMapSystem GetUsedCells called");
        return _manualCells.ToArray();
    }

    private void AddCell(int x, int y, int z, AovDataStructures.CellType type, bool walkable)
    {
        _manualCells.Add(new Vector3I(x, y, z));
        CellsInformation.Add(new CellInformation(x, y, z, type, walkable));
        GD.Print("[TEST] TestConcreteMapSystem AddCell called");
    }

    public void AddEmptyCell(int x, int y, int z)
    {
        AddCell(x, y, z, AovDataStructures.CellType.Empty, false);
        GD.Print("[TEST] TestConcreteMapSystem AddEmptyCell called");
    }

    public void AddWalkableCell(int x, int y, int z)
    {
        AddCell(x, y, z, AovDataStructures.CellType.Grass, true);
        GD.Print("[TEST] TestConcreteMapSystem AddWalkableCell called");
    }

    public void CallInitialize()
    {
        Initialize();
    }

    public void CallCleanup()
    {
        Cleanup();
    }

    public void CallReady()
    {
        _Ready();
    }
}
