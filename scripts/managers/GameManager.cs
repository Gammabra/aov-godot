using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.status_effects;
using Godot;

namespace AshesOfVelsingrad.Managers;

public enum GameState
{
	Waiting,
	PlayerTurn,
	EnemyTurn
}

public enum GameOutcome
{
	Ongoing,
	Victory,
	Defeat
}

public partial class GameManager : BaseManager
{
	public EnemyAIManager? AIManager { get; private set; }

	#region Private Fields

	private GameState _gameState = GameState.Waiting;
	private GameOutcome _gameOutcome = GameOutcome.Ongoing;
	private readonly List<UnitSystem> _playerUnits = [];
	private readonly List<UnitSystem> _enemyUnits = [];
	private List<Vector3I> _currentUnitPossibleMoves = [];
	private SkillSystem? _selectedSkill;
	private UnitSystem? _selectedUnitForPlayedSkill;
	private StatusEffectSystem _statusEffectSystem = new();

	#endregion

	#region Godot Private Fields

	[Export]
	private NodePath? _playerUnitsPath;

	[Export]
	private NodePath? _enemyUnitsPath;

	[Export]
	private NodePath? _mapSystemPath;

	[Export]
	private NodePath? _turnManagerPath;

	[Export]
	private NodePath? _battleInputSystemPath;

	private Node? _playerUnitsContainer;
	private Node? _enemyUnitsContainer;
	private MapSystem? _mapSystemContainer;
	private TurnManager? _turnManagerContainer;
	private BattleInputSystem? _battleInputSystemContainer;

	#endregion

	#region Private Properties

	private new static GameManager? Instance { get; set; }

	#endregion

	#region Class initialization

	public override void _Ready()
	{
		base._Ready();
		_ = StartBattleWhenReady();
	}

	protected override void Initialize()
	{
		if (Instance != null && Instance != this)
		{
			GD.PrintErr($"Multiple instances of {GetType().Name} detected. Removing duplicate.");
			QueueFree();
			return;
		}

		Instance = this;
		InitializeGameManager();
		GD.Print("GameManager initialized successfully");
	}

	#endregion

	#region Private Methods

	private async Task StartBattleWhenReady()
	{
		if (!IsInsideTree())
			await ToSignal(this, "ready");

		if (_turnManagerContainer == null)
		{
			GD.PrintErr("TurnManagerContainer not set");
			return;
		}

		GD.Print("All nodes should be ready. Start battle now.");
		_ = _turnManagerContainer.StartBattle();
	}

	private void InitializeGameManager()
	{
		_battleInputSystemContainer = GetNode<BattleInputSystem>(_battleInputSystemPath);
		_battleInputSystemContainer.OnPassTurnPressed += PlayerUnitPassedTurn;
		_battleInputSystemContainer.OnMoveUnitToPressed += PlayerUnitMoved;
		_battleInputSystemContainer.OnSelectedSkillPressed += PlayerSkillSelected;
		_playerUnitsContainer = GetNode<Node>(_playerUnitsPath);
		_enemyUnitsContainer = GetNode<Node>(_enemyUnitsPath);

		LoadUnits();

		_mapSystemContainer = GetNode<MapSystem>(_mapSystemPath);
		_mapSystemContainer.PlaceUnits(_playerUnits, _enemyUnits);
		_turnManagerContainer = GetNode<TurnManager>(_turnManagerPath);
		_turnManagerContainer.OnPlayerTurn += ActivatePlayerUnit;
		_turnManagerContainer.OnPlayerEndTurn += DeactivatePlayerUnit;
		_turnManagerContainer.InitializeTurnOrder(_playerUnits, _enemyUnits);

		if (_enemyUnits.Contains(_turnManagerContainer.GetCurrentUnit()))
			DeactivatePlayerUnit();
		else
			ActivatePlayerUnit();

		// Create EnemyAIManager as a battle-scoped instance (NOT a global singleton)
		AIManager = new EnemyAIManager(this);
		AIManager.SetUnitReferences(_playerUnits, _enemyUnits);
		
		if (_mapSystemContainer != null)
			AIManager.SetMapSystem(_mapSystemContainer);
		else
			GD.PrintErr("EnemyAIManager: MapSystem not available when setting up AI manager");
		
		_turnManagerContainer.SetAIManager(AIManager);
	}

	private void LoadUnits()
	{
		if (_playerUnitsContainer == null)
		{
			GD.PrintErr("PlayerUnitsContainer not set");
			return;
		}
		if (_enemyUnitsContainer == null)
		{
			GD.PrintErr("EnemyUnitsContainer not set");
			return;
		}
		_playerUnits.Clear();
		_enemyUnits.Clear();

		foreach (Node child in _playerUnitsContainer.GetChildren())
			if (child is UnitSystem unit)
				_playerUnits.Add(unit);

		foreach (Node child in _enemyUnitsContainer.GetChildren())
			if (child is UnitSystem unit)
				_enemyUnits.Add(unit);

		GD.Print($"Players count : {_playerUnits.Count} | Enemies count : {_enemyUnits.Count}");
	}

	private void ActivatePlayerUnit()
	{
		if (_battleInputSystemContainer == null)
		{
			GD.PrintErr("BattleInputSystemContainer not set in GameManager.");
			return;
		}

		if (_turnManagerContainer == null)
		{
			GD.PrintErr("TurnManagerContainer not set in GameManager.");
			return;
		}

		if (_mapSystemContainer == null)
		{
			GD.PrintErr("MapSystemContainer not set in GameManager.");
			return;
		}

		if (_currentUnitPossibleMoves.Count == 0)
			_currentUnitPossibleMoves = _turnManagerContainer.GetCurrentUnit().GetPossibleMoves(_mapSystemContainer);
		GD.Print("Current Unit Possible Moves: " + string.Join(", ", _currentUnitPossibleMoves));
		_battleInputSystemContainer.SetInputEnabled(true);
		GD.Print("Activate input");
	}

	private void DeactivatePlayerUnit()
	{
		if (_battleInputSystemContainer == null)
		{
			GD.PrintErr("BattleInputSystemContainer not set in GameManager.");
			return;
		}

		if (_turnManagerContainer == null)
		{
			GD.PrintErr("TurnManagerContainer not set in GameManager.");
			return;
		}

		if (_mapSystemContainer == null)
		{
			GD.PrintErr("MapSystemContainer not set in GameManager.");
			return;
		}

		_currentUnitPossibleMoves.Clear();
		GD.Print("Deactivate input");
		_battleInputSystemContainer.SetInputEnabled(false);
	}

	private void PlayerSkillSelected(int skillId)
	{
		if (_turnManagerContainer == null)
		{
			GD.PrintErr("TurnManagerContainer not set in GameManager.");
			return;
		}

		if (skillId >= _turnManagerContainer.GetCurrentUnit().ActiveSkills.Count)
		{
			GD.PrintErr("Skill Id is out of Skill List");
			return;
		}

		_selectedSkill = _turnManagerContainer.GetCurrentUnit().ActiveSkills[skillId];
	}

	private void PlayerTargetSelected(UnitSystem unit)
	{
		if (_selectedSkill == null)
		{
			GD.PrintErr("Skill not selected");
			return;
		}

		if (_turnManagerContainer == null)
		{
			GD.PrintErr("TurnManagerContainer not set");
			return;
		}

		UseSkill(_turnManagerContainer.GetCurrentUnit(), unit, _selectedSkill);
	}

	private void PlayerUnitPassedTurn()
	{
		if (_turnManagerContainer == null)
		{
			GD.PrintErr("TurnManagerContainer not set");
			return;
		}

		_turnManagerContainer.GetCurrentUnit().PassTurn();
	}

	private void PlayerUnitMoved(Vector3I cell)
	{
		if (_mapSystemContainer == null)
		{
			GD.PrintErr("MapSystemContainer not set in GameManager.");
			return;
		}

		if (_turnManagerContainer == null)
		{
			GD.PrintErr("TurnManagerContainer not set in GameManager.");
			return;
		}

		if (!_currentUnitPossibleMoves.Contains(cell))
		{
			ActivatePlayerUnit();
			return;
		}

		try
		{
			if (!_turnManagerContainer.GetCurrentUnit().CanMoveTo(cell.X, cell.Y, cell.Z, _mapSystemContainer))
			{
				ActivatePlayerUnit();
				return;
			}
		}
		catch (ArgumentOutOfRangeException)
		{
			ActivatePlayerUnit();
			return;
		}

		MoveUnit(cell);
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Moves the currently active unit to the specified cell on the map.
	/// This can be called by AI behaviors through BattleState.
	/// </summary>
	public void MoveUnit(Vector3I cell)
	{
		if (_mapSystemContainer == null)
		{
			GD.PrintErr("MapSystemContainer not set in GameManager.");
			return;
		}

		if (_turnManagerContainer == null)
		{
			GD.PrintErr("TurnManagerContainer not set in GameManager.");
			return;
		}

		Vector3I pos = new(cell.X, cell.Y, cell.Z);
		Vector3 worldPos = _mapSystemContainer.MapToLocal(pos);
		worldPos.Y += _mapSystemContainer.CellSize.Y * 0.5f;
		_turnManagerContainer.GetCurrentUnit().GlobalPosition = worldPos;

		GD.Print("Unit moved");
		_turnManagerContainer.GetCurrentUnit().MoveTo(cell.X, cell.Y, cell.Z, _mapSystemContainer);
	}

	/// <summary>
	/// Uses a skill from a source unit on a target unit.
	/// This can be called by AI behaviors through BattleState.
	/// </summary>
	public void UseSkill(UnitSystem sourceUnit, UnitSystem targetUnit, SkillSystem skill)
	{
		List<UnitSystem> allyUnits = [];
		List<UnitSystem> enemyUnits = [];
		List<UnitSystem> targetUnits = [];

		if (_turnManagerContainer == null)
		{
			GD.PrintErr("TurnManagerContainer not set in GameManager.");
			return;
		}

		if (_playerUnits.Contains(sourceUnit))
		{
			allyUnits = _playerUnits;
			enemyUnits = _enemyUnits;
		}
		else
		{
			allyUnits = _enemyUnits;
			enemyUnits = _playerUnits;
		}

		switch (skill.TargetType)
		{
			case TargetTypes.SingleAlly:
				if (!allyUnits.Contains(targetUnit))
				{
					GD.PrintErr($"Ally unit {targetUnit.UnitName} not found.");
					return;
				}

				targetUnits.Add(targetUnit);
				_turnManagerContainer.GetCurrentUnit().Play(targetUnits, _mapSystemContainer, skill);
				break;

			case TargetTypes.SingleEnemy:
				if (!enemyUnits.Contains(targetUnit))
				{
					GD.PrintErr($"Enemy unit {targetUnit.UnitName} not found.");
					return;
				}

				targetUnits.Add(targetUnit);
				_turnManagerContainer.GetCurrentUnit().Play(targetUnits, _mapSystemContainer, skill);
				break;

			case TargetTypes.AllAllies:
				if (!allyUnits.Contains(targetUnit))
				{
					GD.PrintErr($"Ally unit {targetUnit.UnitName} not found.");
					return;
				}

				_turnManagerContainer.GetCurrentUnit().Play(allyUnits, _mapSystemContainer, skill);
				break;

			case TargetTypes.AllEnemies:
				if (!enemyUnits.Contains(targetUnit))
				{
					GD.PrintErr($"Enemy unit {targetUnit.UnitName} not found.");
					return;
				}

				_turnManagerContainer.GetCurrentUnit().Play(enemyUnits, _mapSystemContainer, skill);
				break;
		}
	}

	#endregion
}
