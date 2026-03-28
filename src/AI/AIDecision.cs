using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.AI;

/// <summary>
/// Represents a decision made by the AI, including the action and relevant parameters.
/// </summary>
public class AIDecision
{
	/// <summary>The action to perform.</summary>
	public AIAction Action { get; set; }

	/// <summary>The target unit (for attacks or skills).</summary>
	public UnitSystem? Target { get; set; }

	/// <summary>The position to move to (for movement).</summary>
	public (int, int,int)? MovePosition { get; set; }

	/// <summary>The skill to use (for skill actions).</summary>
	public SkillSystem? Skill { get; set; }

	/// <summary>The evaluation score for this decision. Higher is better.</summary>
	public float Score { get; set; }

	/// <summary>Debug description of why this decision was scored this way.</summary>
	public string Reasoning { get; set; } = string.Empty;
}