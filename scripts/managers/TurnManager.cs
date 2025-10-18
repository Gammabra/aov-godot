using System.Collections.Generic;
using System.Linq;
using AshesOfVelsingrad.systems;
using Godot;

namespace AshesOfVelsingrad.Managers;

public partial class TurnManager : BaseManager
{
	#region Private Fields

	private int _turn;
	private List<UnitSystem> _unitsTurnOrder = [];
	private int _currentIndex;

	#endregion

	#region Public Properties

	private new static TurnManager? Instance { get; set; }

	#endregion

	#region Class Initialization

	/// <summary>
	///     Initializes the TurnManager singleton instance.
	///     Ensures only one instance exists and sets up the initial state.
	/// </summary>
	/// <remarks>
	///     This method is called automatically by Godot when the node is ready.
	///     It checks for duplicate instances and initializes the game system.
	///     If a duplicate instance is found, it removes the duplicate.
	/// </remarks>
	protected override void Initialize()
	{
		if (Instance != null && Instance != this)
		{
			GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
			QueueFree();
			return;
		}

		Instance = this;
		GD.Print("TurnManager initialized successfully");
	}

	#endregion

	#region Public Methods

	public void InitializeTurnOrder(List<UnitSystem> playerUnits, List<UnitSystem> enemyUnits)
	{
		_unitsTurnOrder = playerUnits.Concat(enemyUnits).OrderByDescending(unit => unit.BaseSpeed).ToList();

		GD.Print("Turn order initialized:");
		foreach (UnitSystem unit in _unitsTurnOrder)
			GD.Print($"{unit.Name} (Speed: {unit.BaseSpeed})");
	}

	#endregion
}
