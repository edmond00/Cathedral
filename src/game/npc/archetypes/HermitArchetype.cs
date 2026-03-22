using Cathedral.Game.Dialogue;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Hermit NPC — reclusive sage, dialogue-capable, knows mountain secrets. Generally peaceful.</summary>
public class HermitArchetype : NpcArchetype
{
    public override string ArchetypeId => "hermit";
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultHostile => false;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 12;

    public override string[] NamePool => new[]
    {
        "Old Wynn", "Silence", "Brother Ashmore", "The Recluse",
        "Crag-Sitter", "Maelis the Quiet", "Barefoot Herne"
    };

    protected override string[] BuildNarrationKeywords(string name)
        => new[] { "hermit", "recluse", "figure", "hut", "smoke", "old", "solitary", "seated" };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"an old solitary figure sits by a smouldering fire — {name}, a hermit of these heights";

    protected override NpcPersona? CreatePersona() => new HermitPersona();

    protected override ConversationSubjectNode? CreateConversationGraph()
        => new HermitConversationGraph().CreateGraph();
}

// ── Persona ──────────────────────────────────────────────────────────────────

file class HermitPersona : NpcPersona
{
    public override string PersonaId   => "hermit";
    public override string DisplayName => "The Hermit";
    public override string PersonaTone => "a cryptic recluse who speaks in riddles and hard-won truths, rarely welcoming but never truly cruel";

    public override string PersonaPrompt => @"You are a hermit who retreated from civilization long ago. You live alone in the mountains, eating what the rock gives you, sleeping where the wind allows. Visitors are rare — and rarely welcome.

You speak in fragments and riddles. Your sentences are short, sometimes incomplete. You might trail off mid-sentence. You often answer questions with other questions. You are not hostile — just deeply uninterested in small talk. If someone persists with patience and genuine curiosity, however, you may share fragments of hard-won knowledge about the mountain paths, hidden caves, weather patterns, or old stories carved into the stone.

Your speech is sparse and cryptic: 'The peak doesn't care about your name.' or 'Three moons since anyone came this way. What does that tell you?' You do not volunteer information. You test whether the visitor is worth speaking to.";
}

// ── Conversation Graph ───────────────────────────────────────────────────────

file class HermitGreetingNode : ConversationSubjectNode
{
    public override string SubjectId          => "hermit_greeting";
    public override string ContextDescription => "approaching the hermit cautiously";
    public override bool   IsEntryNode        => true;
    public override float  BaseDifficultyScore => 0.5f;
}

file class HermitWisdomNode : ConversationSubjectNode
{
    public override string SubjectId          => "hermit_wisdom";
    public override string ContextDescription => "probing the hermit for mountain knowledge";
    public override float  BaseDifficultyScore => 0.6f;
}

file class HermitPastNode : ConversationSubjectNode
{
    public override string SubjectId          => "hermit_past";
    public override string ContextDescription => "asking about the hermit's past";
    public override float  BaseDifficultyScore => 0.8f;
}

file class HermitConversationGraph : ConversationGraphFactory
{
    private HermitGreetingNode _greeting = null!;
    private HermitWisdomNode   _wisdom   = null!;
    private HermitPastNode     _past     = null!;

    protected override ConversationSubjectNode BuildNodes()
    {
        _greeting = new HermitGreetingNode();
        _wisdom   = new HermitWisdomNode();
        _past     = new HermitPastNode();
        return _greeting;
    }

    protected override void ConnectNodes()
    {
        _greeting.PossiblePositiveOutcomes = new()
        {
            new NodeTransitionOutcome(_wisdom),
            new AffinityOutcome(+10f),
        };
        _greeting.NegativeOutcome = new AffinityOutcome(-15f);
        _greeting.Transitions = new() { _wisdom, _past };

        _wisdom.PossiblePositiveOutcomes = new()
        {
            new NodeTransitionOutcome(_past),
            new AffinityOutcome(+20f),
        };
        _wisdom.NegativeOutcome = new AffinityOutcome(-10f);
        _wisdom.Transitions = new() { _past };

        _past.PossiblePositiveOutcomes = new()
        {
            new AffinityOutcome(+25f),
        };
        _past.NegativeOutcome = new AffinityOutcome(-20f);
        _past.Transitions = new();
    }
}
