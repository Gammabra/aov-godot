using System.Collections.Generic;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

public interface IMapSystem
{
    //var from IMapSystem
    int Width { get; }
    int Height { get; }
    int Depth { get; }

    // Methods from IMapSystem
    void InjectDependencies(StatusEffectSystem statusEffectSystem);
    bool IsEmpty(int x, int y, int z);
    bool IsWalkable(int x, int y, int z);
    void SetWalkable(int x, int y, int z);
    void SetStatusEffectOnCells(List<(int, int, int)> cells, StatusEffect<CellInformation> statusEffect);
    AovDataStructures.CellType GetCellType((int, int, int) position);

    // Methods from MapSystemUnitsHandling
    void PlaceUnits(List<IUnitSystem> playerUnits, List<IUnitSystem> enemyUnits);
    void MoveUnit(IUnitSystem unit, int newX, int newY, int newZ);
    void RemoveUnit(int x, int y, int z);
    (int, int, int)? GetUnitPosition(IUnitSystem unit);
    IUnitSystem? GetUnitAt(int x, int y, int z);
}
