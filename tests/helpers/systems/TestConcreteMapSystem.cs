using System.Collections.Generic;
using System.Linq;
using AshesOfVelsingrad.Systems;
using Godot;

namespace UnitTests;

public sealed partial class TestConcreteMapSystem : MapSystem
{
    public new static TestConcreteMapSystem? Instance { get; set; }

    public bool IsInitialized { get; private set; }
    public bool IsCleanedUp { get; private set; }

    public readonly List<Vector3I> ManualCells = new();

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
        CellsInformation[0].SetUnit(playerUnits[0]);
        CellsInformation[1].SetUnit(enemyUnits[0]);

        GD.Print("[TEST] TestConcreteMapSystem PlaceUnits called");
    }

    public void AddUnit(UnitSystem unit)
    {
        CellsInformation[0].SetUnit(unit);

        GD.Print("[TEST] TestConcreteMapSystem AddUnits called");
    }

    protected override void Cleanup()
    {
        IsCleanedUp = true;
        ManualCells.Clear();
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
        return ManualCells.ToArray();
    }

    public void AddCell(int x, int y, int z, CellType type, bool walkable)
    {
        ManualCells.Add(new Vector3I(x, y, z));
        CellsInformation.Add(new CellInformation(x, y, z, type, walkable));
        GD.Print("[TEST] TestConcreteMapSystem AddCell called");
    }

    public void AddEmptyCell(int x, int y, int z)
    {
        AddCell(x, y, z, CellType.Empty, false);
        GD.Print("[TEST] TestConcreteMapSystem AddEmptyCell called");
    }

    public void AddWalkableCell(int x, int y, int z)
    {
        AddCell(x, y, z, CellType.Grass, true);
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
