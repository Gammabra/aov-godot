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

		foreach (IUnitSystem unit in playerUnits)
		{
			CellInformation cell = CellsInformation[0];
			cell.SetUnit(unit);
			SetWalkable(cell.X, cell.Y, cell.Z);
			List<(int, int, int)> cells = [];
			cells.Add((cell.X, cell.Y, cell.Z));
			SetStatusEffectOnCells(cells, new BurningCellEffect(10));

			Vector3I pos = new(cell.X, cell.Y, cell.Z);
			Vector3 worldPos = MapToLocal(pos);
			worldPos.Y += CellSize.Y * 1.5f;

			((CharacterBody3D)unit).GlobalPosition = worldPos;
			GD.Print("Placed player unit '" + unit.UnitName + "' at cell (" + cell.X + ", " + cell.Y + ", " + cell.Z + ") with world position " + ((CharacterBody3D)unit).GlobalPosition + " when assigned " + worldPos); // Debug log
		}

		foreach (IUnitSystem unit in enemyUnits)
		{
			CellInformation cell = CellsInformation[1];
			cell.SetUnit(unit);
			SetWalkable(cell.X, cell.Y, cell.Z);

			Vector3I pos = new(cell.X, cell.Y, cell.Z);
			Vector3 worldPos = MapToLocal(pos);
			worldPos.Y += CellSize.Y * 1.5f;

			((CharacterBody3D)unit).GlobalPosition = worldPos;
		}
	}
}
