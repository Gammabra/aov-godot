using System.Collections.Generic;
using AshesOfVelsingrad.systems;
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

			Vector3I pos = new(cell.X, cell.Y, cell.Z);
			Vector3 worldPos = MapToLocal(pos);
			//worldPos += new Vector3(CellSize.X * 0.5f, 0f, CellSize.Z * 0.5f);
			worldPos.Y += CellSize.Y * 2f;

			unit.GlobalPosition = worldPos;
		}

		foreach (UnitSystem unit in enemyUnits)
		{
			CellInformation cell = CellsInformation[1];
			cell.Unit = unit;

			Vector3I pos = new(cell.X, cell.Y, cell.Z);
			Vector3 worldPos = MapToLocal(pos);
			//worldPos += new Vector3(CellSize.X * 0.5f, 0f, CellSize.Z * 0.5f);
			worldPos.Y += CellSize.Y * 2f;

			unit.GlobalPosition = worldPos;
		}
	}
}
