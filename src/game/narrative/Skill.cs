using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for all skills.
/// Skills define the avatar's capabilities and narrative perspectives.
/// </summary>
public abstract class Skill
{
    public abstract string SkillId { get; }           // "observation", "algebraic_analysis"
    public abstract string DisplayName { get; }       // "Observation", "Algebraic Analysis"
    public abstract SkillFunction[] Functions { get; } // Can have multiple functions (1-3)
    public abstract string[] BodyParts { get; }       // Associated body parts (1-2)
    public int Level { get; set; }                    // 1-10, used for skill checks (random initial)
    
    /// <summary>
    /// Persona prompt for LLM (only for Observation and Thinking skills).
    /// This is cached in the LLM slot and defines the skill's narrative voice.
    /// </summary>
    public virtual string? PersonaPrompt => null;
}

/// <summary>
/// Skill functions determine when and how a skill is used.
/// Skills can have multiple functions.
/// </summary>
public enum SkillFunction
{
    Observation,   // Generates perceptions of environment
    Thinking,      // Generates reasoning and actions (CoT)
    Action         // Used for skill checks when executing actions
}
