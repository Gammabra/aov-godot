using System.Collections.Generic;
using System.Linq;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.AI;

/// <summary>
/// Provides visual debugging tools for AI decision-making.
/// </summary>
public partial class AIDebugVisualizer : Node3D
{
	#region Private Fields

	private MeshInstance3D? _targetIndicator;
	private MeshInstance3D? _moveIndicator;
	private Label3D? _scoreLabel;
	private readonly List<MeshInstance3D> _rangeIndicators = new();
	private readonly List<Node3D> _debugNodes = new(); // Changed to Node3D to hold both meshes and labels

	#endregion

	#region Initialization

	public override void _Ready()
	{
		// Create target indicator (sphere at target location)
		_targetIndicator = CreateIndicatorSphere(0.3f, new Color(1, 0, 0, 0.7f));
		AddChild(_targetIndicator);
		_targetIndicator.Visible = false;

		// Create move indicator (arrow showing movement path)
		_moveIndicator = CreateIndicatorSphere(0.2f, new Color(0, 1, 0, 0.7f));
		AddChild(_moveIndicator);
		_moveIndicator.Visible = false;

		// Create score label
		_scoreLabel = new Label3D
		{
			PixelSize = 0.01f,
			OutlineSize = 2,
			Modulate = new Color(1, 1, 1),
			Visible = false
		};
		AddChild(_scoreLabel);
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Visualizes an AI decision in the 3D world.
	/// </summary>
	public void VisualizeDecision(AIDecision decision, BattleState battleState)
	{
		ClearPreviousVisualization();

		switch (decision.Action)
		{
			case AIAction.UseSkill:
			case AIAction.MoveAndSkill:
				VisualizeSkillAction(decision, battleState);
				break;

			case AIAction.Move:
				VisualizeMovementAction(decision, battleState);
				break;

			case AIAction.Pass:
				// No visualization for pass
				break;
		}
	}

	/// <summary>
	/// Displays reasoning text above the AI unit.
	/// </summary>
	public void ShowReasoningText(string reasoning, Vector3 position, float duration = 2.0f)
	{
		if (_scoreLabel == null) return;

		_scoreLabel.Text = reasoning;
		_scoreLabel.GlobalPosition = position + new Vector3(0, 2, 0);
		_scoreLabel.Visible = true;

		// Hide after duration
		GetTree().CreateTimer(duration).Timeout += () => 
		{
			if (_scoreLabel != null)
				_scoreLabel.Visible = false;
		};
	}

	/// <summary>
	/// Visualizes threat levels around the unit.
	/// </summary>
	public void VisualizeThreatMap(UnitSystem unit, BattleState battleState, int range = 5)
	{
		Vector3I? unitPos = battleState.MapSystem.GetUnitPosition(unit);
		if (unitPos == null) return;

		List<Vector3I> possibleMoves = unit.GetPossibleMoves(battleState.MapSystem);

		foreach (var move in possibleMoves)
		{
			float threatLevel = AIUtilities.CalculateThreatLevel(move, battleState, range);
			
			// Normalize threat level to color intensity
			float intensity = Mathf.Clamp(threatLevel / 100f, 0f, 1f);
			Color threatColor = new Color(intensity, 1f - intensity, 0f, 0.3f);

			var indicator = CreateIndicatorSphere(0.15f, threatColor);
			Vector3 worldPos = battleState.MapSystem.MapToLocal(move);
			worldPos.Y += battleState.MapSystem.CellSize.Y * 0.1f;
			indicator.GlobalPosition = worldPos;
			
			AddChild(indicator);
			_rangeIndicators.Add(indicator);
		}

		// Auto-clear after 3 seconds
		GetTree().CreateTimer(3.0f).Timeout += ClearPreviousVisualization;
	}

	/// <summary>
	/// Visualizes all possible action scores.
	/// </summary>
	public void VisualizeActionScores(List<AIDecision> decisions, BattleState battleState)
	{
		ClearPreviousVisualization();

		// Sort by score
		var topDecisions = decisions.OrderByDescending(d => d.Score).Take(5);

		int index = 0;
		foreach (var decision in topDecisions)
		{
			Vector3? targetPos = null;

			if (decision.Target != null)
			{
				Vector3I? gridPos = battleState.MapSystem.GetUnitPosition(decision.Target);
				if (gridPos != null)
					targetPos = battleState.MapSystem.MapToLocal(gridPos.Value);
			}
			else if (decision.MovePosition.HasValue)
			{
				targetPos = battleState.MapSystem.MapToLocal(decision.MovePosition.Value);
			}

			if (targetPos != null)
			{
				// Create numbered label showing rank and score
				var label = new Label3D
				{
					Text = $"#{index + 1}: {decision.Score:F0}",
					PixelSize = 0.008f,
					OutlineSize = 2,
					Modulate = GetColorForRank(index),
					GlobalPosition = targetPos.Value + new Vector3(0, 1.5f + index * 0.3f, 0)
				};
				AddChild(label);
				_debugNodes.Add(label); // Fixed: Add to _debugNodes instead
			}

			index++;
		}

		// Auto-clear after 4 seconds
		GetTree().CreateTimer(4.0f).Timeout += ClearPreviousVisualization;
	}

	#endregion

	#region Private Methods

	private void VisualizeSkillAction(AIDecision decision, BattleState battleState)
	{
		if (decision.Target == null || _targetIndicator == null)
			return;

		// Show target indicator
		Vector3I? targetGridPos = battleState.MapSystem.GetUnitPosition(decision.Target);
		if (targetGridPos != null)
		{
			Vector3 targetWorldPos = battleState.MapSystem.MapToLocal(targetGridPos.Value);
			targetWorldPos.Y += battleState.MapSystem.CellSize.Y * 0.5f;
			_targetIndicator.GlobalPosition = targetWorldPos;
			_targetIndicator.Visible = true;

			// Color based on action type
			if (decision.Skill != null)
			{
				Color color = GetColorForEffectType(decision.Skill.EffectType);
				var material = (StandardMaterial3D)_targetIndicator.GetActiveMaterial(0);
				if (material != null)
					material.AlbedoColor = color;
			}
		}

		// Show movement path if moving
		if (decision.Action == AIAction.MoveAndSkill && decision.MovePosition.HasValue)
		{
			VisualizePath(battleState.ActingUnit, decision.MovePosition.Value, battleState);
		}

		// Show skill range
		if (decision.Skill != null)
		{
			VisualizeSkillRange(decision.Target, decision.Skill, battleState);
		}

		// Show score
		if (_scoreLabel != null && targetGridPos != null)
		{
			Vector3 labelPos = battleState.MapSystem.MapToLocal(targetGridPos.Value);
			labelPos.Y += battleState.MapSystem.CellSize.Y * 1.5f;
			_scoreLabel.GlobalPosition = labelPos;
			_scoreLabel.Text = $"Score: {decision.Score:F1}\n{decision.Reasoning}";
			_scoreLabel.Visible = true;
		}

		// Auto-hide after 2 seconds
		GetTree().CreateTimer(2.0f).Timeout += ClearPreviousVisualization;
	}

	private void VisualizeMovementAction(AIDecision decision, BattleState battleState)
	{
		if (!decision.MovePosition.HasValue)
			return;

		VisualizePath(battleState.ActingUnit, decision.MovePosition.Value, battleState);

		// Show score at destination
		if (_scoreLabel != null)
		{
			Vector3 destPos = battleState.MapSystem.MapToLocal(decision.MovePosition.Value);
			destPos.Y += battleState.MapSystem.CellSize.Y * 1.0f;
			_scoreLabel.GlobalPosition = destPos;
			_scoreLabel.Text = $"Move Score: {decision.Score:F1}";
			_scoreLabel.Visible = true;
		}

		// Auto-hide after 2 seconds
		GetTree().CreateTimer(2.0f).Timeout += ClearPreviousVisualization;
	}

	private void VisualizePath(UnitSystem unit, Vector3I destination, BattleState battleState)
	{
		Vector3I? startGridPos = battleState.MapSystem.GetUnitPosition(unit);
		if (startGridPos == null)
			return;

		Vector3 startPos = battleState.MapSystem.MapToLocal(startGridPos.Value);
		Vector3 endPos = battleState.MapSystem.MapToLocal(destination);

		// Create arrow from start to end
		var arrow = CreateArrow(startPos, endPos, new Color(0, 1, 0, 0.6f));
		AddChild(arrow);
		_rangeIndicators.Add(arrow);
	}

	private void VisualizeSkillRange(UnitSystem target, SkillSystem skill, BattleState battleState)
	{
		Vector3I? targetPos = battleState.MapSystem.GetUnitPosition(target);
		if (targetPos == null)
			return;

		// Show AOE if skill has area effect
		if (skill.AreaEffect.Count > 0)
		{
			foreach (var offset in skill.AreaEffect)
			{
				Vector3I aoePos = targetPos.Value + offset;
				Vector3 worldPos = battleState.MapSystem.MapToLocal(aoePos);
				worldPos.Y += battleState.MapSystem.CellSize.Y * 0.1f;

				var indicator = CreateIndicatorSphere(0.2f, new Color(1, 0.5f, 0, 0.4f));
				indicator.GlobalPosition = worldPos;
				AddChild(indicator);
				_rangeIndicators.Add(indicator);
			}
		}
	}

	private void ClearPreviousVisualization()
	{
		if (_targetIndicator != null)
			_targetIndicator.Visible = false;
		
		if (_moveIndicator != null)
			_moveIndicator.Visible = false;

		if (_scoreLabel != null)
			_scoreLabel.Visible = false;

		foreach (var indicator in _rangeIndicators)
		{
			indicator?.QueueFree();
		}
		_rangeIndicators.Clear();

		// Also clear debug nodes (labels, etc.)
		foreach (var node in _debugNodes)
		{
			node?.QueueFree();
		}
		_debugNodes.Clear();
	}

	#endregion

	#region Helper Methods

	private MeshInstance3D CreateIndicatorSphere(float radius, Color color)
	{
		var mesh = new SphereMesh
		{
			Radius = radius,
			Height = radius * 2
		};

		var material = new StandardMaterial3D
		{
			AlbedoColor = color,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
		};

		var meshInstance = new MeshInstance3D
		{
			Mesh = mesh
		};
		meshInstance.SetSurfaceOverrideMaterial(0, material);

		return meshInstance;
	}

	private MeshInstance3D CreateArrow(Vector3 from, Vector3 to, Color color)
	{
		// Create a simple cylinder as an arrow
		var distance = from.DistanceTo(to);
		var midpoint = (from + to) / 2;

		var mesh = new CylinderMesh
		{
			TopRadius = 0.05f,
			BottomRadius = 0.05f,
			Height = distance
		};

		var material = new StandardMaterial3D
		{
			AlbedoColor = color,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
		};

		var meshInstance = new MeshInstance3D
		{
			Mesh = mesh,
			GlobalPosition = midpoint
		};
		meshInstance.SetSurfaceOverrideMaterial(0, material);

		// Rotate to point from start to end
		meshInstance.LookAt(to, Vector3.Up);
		meshInstance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);

		return meshInstance;
	}

	private Color GetColorForEffectType(EffectType effectType)
	{
		return effectType switch
		{
			EffectType.Damage => new Color(1, 0, 0, 0.7f),     // Red
			EffectType.Heal => new Color(0, 1, 0, 0.7f),       // Green
			EffectType.Buff => new Color(0, 0, 1, 0.7f),       // Blue
			EffectType.Debuff => new Color(0.5f, 0, 0.5f, 0.7f), // Purple
			EffectType.Control => new Color(0.5f, 0.5f, 0, 0.7f), // Olive
			_ => new Color(1, 1, 1, 0.5f)                      // White
		};
	}

	private Color GetColorForRank(int rank)
	{
		return rank switch
		{
			0 => new Color(1, 0.84f, 0),      // Gold - #1
			1 => new Color(0.75f, 0.75f, 0.75f), // Silver - #2
			2 => new Color(0.8f, 0.5f, 0.2f),    // Bronze - #3
			_ => new Color(0.7f, 0.7f, 0.7f)     // Gray - rest
		};
	}

	#endregion

	#region Cleanup

	public override void _ExitTree()
	{
		ClearPreviousVisualization();
	}

	#endregion
}
