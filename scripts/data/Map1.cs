using System.Collections.Generic;
using AshesOfVelsingrad.systems;

namespace AshesOfVelsingrad;

public sealed partial class Map1 : MapSystem
{
	public override void PlaceUnits(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
	{
		foreach (UnitSystem unit in playerUnits) CellsInformation[0].Unit = unit;

		foreach (UnitSystem unit in enemyUnits) CellsInformation[0].Unit = unit;
	}
}
