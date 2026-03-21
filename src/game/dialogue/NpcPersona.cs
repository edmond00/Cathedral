namespace Cathedral.Game.Dialogue;

/// <summary>
/// Defines the personality and narrative voice of an NPC.
/// The PersonaPrompt is used as the LLM system prompt for the NPC's slot,
/// mirroring how ModusMentis.PersonaPrompt works for skill slots.
/// </summary>
public abstract class NpcPersona
{
    /// <summary>Unique identifier for this persona archetype (e.g., "innkeeper").</summary>
    public abstract string PersonaId { get; }

    /// <summary>Display name used in UI headers (e.g., "The InnKeeper").</summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Full LLM system prompt defining the NPC's character, speech patterns, knowledge, and attitude.
    /// Cached in the NPC's dedicated LLM slot.
    /// </summary>
    public abstract string PersonaPrompt { get; }

    /// <summary>
    /// Short one-line tone description used in user-facing prompts.
    /// (e.g., "a gruff but fair innkeeper who speaks in short, direct sentences")
    /// </summary>
    public abstract string PersonaTone { get; }
}
