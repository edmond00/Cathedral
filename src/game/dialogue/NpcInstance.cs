namespace Cathedral.Game.Dialogue;

/// <summary>
/// A live NPC instance in a dialogue session.
/// Holds the persona, conversation graph position, per-instance affinity, and LLM slot.
/// </summary>
public class NpcInstance
{
    public string NpcId { get; }
    public string DisplayName => Persona.DisplayName;

    public NpcPersona Persona { get; }

    /// <summary>Root entry node for this NPC's conversation (produced by the graph factory).</summary>
    public ConversationSubjectNode ConversationRoot { get; }

    /// <summary>Current position in the conversation graph.</summary>
    public ConversationSubjectNode CurrentSubjectNode { get; set; }

    /// <summary>
    /// Per-instance affinity score (0–100).
    /// Initialized from the protagonist's Visage derived stat.
    /// Modified by dialogue outcomes.
    /// </summary>
    public float Affinity { get; set; }

    /// <summary>The affinity value the protagonist started with (used in difficulty formula).</summary>
    public float InitialAffinity { get; }

    /// <summary>LLM slot ID assigned by LlamaServerManager for the NPC's persona.</summary>
    public int LlmSlotId { get; set; } = -1;

    public NpcInstance(string npcId, NpcPersona persona, ConversationSubjectNode conversationRoot, float initialAffinity)
    {
        NpcId = npcId;
        Persona = persona;
        ConversationRoot = conversationRoot;
        CurrentSubjectNode = conversationRoot;
        Affinity = initialAffinity;
        InitialAffinity = initialAffinity;
    }
}
