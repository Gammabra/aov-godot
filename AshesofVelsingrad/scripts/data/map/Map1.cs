using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.Data.StatusEffect;

public sealed partial class Map1 : MapSystem
{
    public override void PlaceUnits(List<IUnitSystem> playerUnits, List<IUnitSystem> enemyUnits)
    {
        if (playerUnits.Count == 0 || enemyUnits.Count == 0)
        {
            GD.PrintErr("Map1.PlaceUnits: No units to place. PlayerUnits: "
                + playerUnits.Count + ", EnemyUnits: " + enemyUnits.Count);
            return;
        }

        // Spread friendlies across the first half of the cell list, enemies across the
        // last half. This keeps Player1 / Player2 / Ally1 from stacking on a single tile
        // and the enemies from stacking on another. Walks the cells with a stride so
        // they don't all spawn touching each other.
        int totalCells = CellsInformation.Count;
        if (totalCells == 0)
        {
            GD.PrintErr("Map1.PlaceUnits: no CellsInformation entries.");
            return;
        }

        // Friendlies start at cell 0 going forward; enemies start at the LAST cell going backward.
        for (int i = 0; i < playerUnits.Count; i++)
        {
            int cellIdx = Mathf.Clamp(i, 0, totalCells - 1);
            PlaceOne(playerUnits[i], CellsInformation[cellIdx], applyBurning: i == 0);
        }

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            int cellIdx = Mathf.Clamp(totalCells - 1 - i, 0, totalCells - 1);
            PlaceOne(enemyUnits[i], CellsInformation[cellIdx], applyBurning: false);
        }
    }

    /// <summary>Place a single unit on <paramref name="cell" /> and snap its world position.</summary>
    private void PlaceOne(IUnitSystem unit, CellInformation cell, bool applyBurning)
    {
        cell.SetUnit(unit);
        SetWalkable(cell.X, cell.Y, cell.Z);

        if (applyBurning)
        {
            List<(int, int, int)> cells = [];
            cells.Add((cell.X, cell.Y, cell.Z));
            SetStatusEffectOnCells(cells, new BurningCellEffect(10));
        }

        Vector3I pos = new(cell.X, cell.Y, cell.Z);
        Vector3 worldPos = MapToLocal(pos);
        worldPos.Y += CellSize.Y * 1.5f;

        ((CharacterBody3D)unit).GlobalPosition = worldPos;
        GD.Print($"Placed '{unit.UnitName}' (faction {unit.Faction}) at cell ({cell.X}, {cell.Y}, {cell.Z})");
    }
}
