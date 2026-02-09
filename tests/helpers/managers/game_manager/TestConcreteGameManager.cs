using System.Collections.Generic;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using Godot;

namespace UnitTests;

public partial class TestConcreteGameManager : GameManager
{
	public bool IsInitialized { get; private set; }
	private List<UnitSystem> _playerUnits = new();
	private List<UnitSystem> _enemyUnits = new();
	private bool _unitMoved;

	public int PlayerUnitsCount => _playerUnits.Count;
	public int EnemyUnitsCount => _enemyUnits.Count;
	public bool UnitMoved => _unitMoved;

	public void SetNodePaths(
		NodePath playerUnitsPath,
		NodePath enemyUnitsPath,
		NodePath mapSystemPath,
		NodePath turnManagerPath,
		NodePath battleInputSystemPath)
	{
		// Use reflection to set private fields
		var playerField = typeof(GameManager).GetField("_playerUnitsPath", 
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		playerField?.SetValue(this, playerUnitsPath);

		var enemyField = typeof(GameManager).GetField("_enemyUnitsPath",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		enemyField?.SetValue(this, enemyUnitsPath);

		var mapField = typeof(GameManager).GetField("_mapSystemPath",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		mapField?.SetValue(this, mapSystemPath);

		var turnField = typeof(GameManager).GetField("_turnManagerPath",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		turnField?.SetValue(this, turnManagerPath);

		var inputField = typeof(GameManager).GetField("_battleInputSystemPath",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		inputField?.SetValue(this, battleInputSystemPath);
	}

	public void CallInitialize()
	{
		// Call protected Initialize method
		var method = typeof(GameManager).GetMethod("Initialize",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		method?.Invoke(this, null);

		IsInitialized = true;

		// Capture unit lists using reflection
		var playerUnitsField = typeof(GameManager).GetField("_playerUnits",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		_playerUnits = (List<UnitSystem>)playerUnitsField?.GetValue(this)!;

		var enemyUnitsField = typeof(GameManager).GetField("_enemyUnits",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		_enemyUnits = (List<UnitSystem>)enemyUnitsField?.GetValue(this)!;
	}

	public UnitSystem GetPlayerUnit(int index)
	{
		return _playerUnits[index];
	}

	public UnitSystem GetEnemyUnit(int index)
	{
		return _enemyUnits[index];
	}

	public void ClearMapSystem()
	{
		var field = typeof(GameManager).GetField("_mapSystemContainer",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		field?.SetValue(this, null);
	}

	public void ClearTurnManager()
	{
		var field = typeof(GameManager).GetField("_turnManagerContainer",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		field?.SetValue(this, null);
	}

	// Override MoveUnit to track if unit moved
	public new void MoveUnit(Vector3I cell)
	{
		base.MoveUnit(cell);
		
		var field = typeof(GameManager).GetField("_unitMoved",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		_unitMoved = (bool)field?.GetValue(this)!;
	}
}