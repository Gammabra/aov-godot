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
			CellsInformation[0].Unit = unit;
			Vector3I pos = new(CellsInformation[0].X, CellsInformation[0].Y, CellsInformation[0].Z);
			Vector3 worldPos = MapToLocal(pos);
			worldPos.Y += 1f;

			unit.GlobalPosition = worldPos;
			AddChild(unit);
		}

		foreach (UnitSystem unit in enemyUnits)
		{
			CellsInformation[1].Unit = unit;
			Vector3I pos = new(CellsInformation[1].X, CellsInformation[1].Y, CellsInformation[1].Z);
			Vector3 worldPos = MapToLocal(pos);
			worldPos.Y += 1f;

			unit.GlobalPosition = worldPos;
			unit.AddChild(unit);
		}
	}
}
