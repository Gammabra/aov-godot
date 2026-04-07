namespace AshesOfVelsingrad.AI;

/// <summary>
/// Defines different AI personality types that determine decision-making behavior.
/// </summary>
public enum AIPersonality
{
    /// <summary>Attacks the nearest target, prefers close combat.</summary>
    Aggressive,

    /// <summary>Targets weakest enemies, tries to finish off low HP units.</summary>
    Opportunistic,

    /// <summary>Maintains distance, avoids direct combat when possible.</summary>
    Defensive,

    /// <summary>Balances offense and positioning, adapts to situation.</summary>
    Balanced
}

/// <summary>
/// Represents the types of actions an AI can take.
/// </summary>
public enum AIAction
{
    /// <summary>Move to a new position then use a skill.</summary>
    MoveAndSkill,

    /// <summary>Move to a new position only.</summary>
    Move,

    /// <summary>Use a skill or ability without moving.</summary>
    UseSkill,

    /// <summary>Do nothing and end turn.</summary>
    Pass
}
