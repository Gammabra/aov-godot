using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad;

public sealed partial class Map1 : MapSystem
{
    public override void PlaceUnits(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
    {
        foreach (UnitSystem unit in playerUnits)
        {
            CellInformation cell = CellsInformation[0];
            cell.Unit = unit;
            SetWalkable(cell.X, cell.Y, cell.Z);

            Vector3I pos = new(cell.X, cell.Y, cell.Z);
            Vector3 worldPos = MapToLocal(pos);
            worldPos.Y += CellSize.Y * 1.5f;

            unit.GlobalPosition = worldPos;
        }

        foreach (UnitSystem unit in enemyUnits)
        {
            CellInformation cell = CellsInformation[1];
            cell.Unit = unit;
            SetWalkable(cell.X, cell.Y, cell.Z);

            Vector3I pos = new(cell.X, cell.Y, cell.Z);
            Vector3 worldPos = MapToLocal(pos);
            worldPos.Y += CellSize.Y * 1.5f;

            unit.GlobalPosition = worldPos;
        }
    }
}
