using System;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for all modiMentis.
/// ModiMentis define the protagonist's capabilities and narrative perspectives.
/// </summary>
public abstract class ModusMentis
{
    public abstract string ModusMentisId { get; }           // "observation", "algebraic_analysis"
    public abstract string DisplayName { get; }       // "Observation", "Algebraic Analysis"
    public abstract string ShortDescription { get; }  // 2-4 word description for prompts
    public abstract ModusMentisFunction[] Functions { get; } // Can have multiple functions (1-3)
    public abstract string[] Organs { get; }          // Associated organ ids (1-2)
    public int Level { get; set; }                    // 1-10, used for modusMentis checks (random initial)

    /// <summary>
    /// Which long-term memory module this modusMentis belongs to.
    /// Working and Residual modules accept any modusMentis regardless of this value.
    /// Every subclass must declare its memory type explicitly.
    /// </summary>
    public abstract ModusMentisMemoryType MemoryType { get; }
    
    /// <summary>
    /// Persona prompt for LLM (only for Observation and Thinking modiMentis).
    /// This is cached in the LLM slot and defines the modusMentis's narrative voice.
    /// </summary>
    public virtual string? PersonaPrompt => null;
    
    /// <summary>
    /// Short persona description for user prompts (e.g., "write like [PersonaTone]").
    /// Used as a quick reminder of the modusMentis's personality in individual LLM calls.
    /// </summary>
    public virtual string? PersonaTone => null;

    /// <summary>
    /// Very short phrase (3-5 words) used as "As a [PersonaReminder], what/why/..." in prompts.
    /// Example: "theatrical performance analyst", "relentless clinical investigator".
    /// </summary>
    public virtual string? PersonaReminder => null;
}

/// <summary>
/// ModusMentis functions determine when and how a modusMentis is used.
/// ModiMentis can have multiple functions.
/// </summary>
public enum ModusMentisFunction
{
    Observation,   // Generates perceptions of environment
    Thinking,      // Generates reasoning and actions (CoT)
    Action,        // Used for modusMentis checks when executing actions
    Speaking       // Generates player dialogue replicas in conversation
}
