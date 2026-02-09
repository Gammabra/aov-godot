using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;

namespace UnitTests;

public partial class TestConcreteTurnManager : TurnManager
{
	private UnitSystem? _currentUnit;

	public void SetCurrentUnit(UnitSystem unit)
	{
		_currentUnit = unit;
	}

	public new UnitSystem GetCurrentUnit()
	{
		return _currentUnit ?? base.GetCurrentUnit();
	}
}