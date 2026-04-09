using Cathedral.Game.Dialogue;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Farm labourer — non-hostile, persistent, dialogue-capable.
/// Works for the farmer, knows the daily routine, wary but friendly if treated well.
/// </summary>
public class FarmhandArchetype : NpcArchetype
{
    public override string ArchetypeId      => "farmhand";
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultHostile     => false;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 8;

    public override string[] NamePool => new[]
    {
        "Wat Cooper", "Agnes Rowe", "Tom Barley", "Matilda Webb",
        "Hugh Swindle", "Joan Thatcher", "Robin Clod", "Cecily Field",
    };

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a young <farmhand> pausing their work to watch you"),
            KeywordInContext.Parse("a rough-woven <tunic> belted with a length of rope"),
            KeywordInContext.Parse("the mud-caked <boots> of someone who walks soft ground all day"),
            KeywordInContext.Parse("a pair of <hands> perpetually dusted with chaff and dirt"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a young labourer straightens from their work and eyes you warily — {name}, a hand on this farm";

    protected override NpcPersona? CreatePersona() => new FarmhandPersona();

    protected override ConversationSubjectNode? CreateConversationGraph()
        => new FarmhandConversationGraph().CreateGraph();
}

// ── Persona ───────────────────────────────────────────────────────────────────

file class FarmhandPersona : NpcPersona
{
    public override string PersonaId   => "farmhand";
    public override string DisplayName => "The Farmhand";
    public override string PersonaTone => "a tired but good-natured labourer with little power and a lot of gossip";

    public override string PersonaPrompt => @"You are a farmhand — a hired labourer on a small medieval farm. Your days are long, your pay is modest, and your complaints are many, though you air them quietly. You know every corner of this farm and most of the local gossip, but you're careful about who you share it with.

You speak in a familiar, slightly weary way. You're not stupid, just tired. You notice things: which animals are sick, who came through the village last week, which fields the farmer has been neglecting. You're happy to talk to someone who treats you as an equal, and deeply suspicious of anyone who looks down at you.

You defer to the farmer but don't worship them. If someone asks you for information the farmer wouldn't want shared, you'll hesitate — but might share it for the right reason.

Your speech is informal, sometimes grammatically loose, with occasional sighs and dry observations about farm life.";
}

// ── Conversation Graph ────────────────────────────────────────────────────────

file class FarmhandGreetingNode : ConversationSubjectNode
{
    public override string SubjectId          => "farmhand_greeting";
    public override string ContextDescription => "exchanging greetings with the farmhand";
    public override bool   IsEntryNode        => true;
    public override float  BaseDifficultyScore => 0.3f;
}

file class FarmhandGossipNode : ConversationSubjectNode
{
    public override string SubjectId          => "farmhand_gossip";
    public override string ContextDescription => "getting local gossip and farm news from the farmhand";
    public override float  BaseDifficultyScore => 0.45f;
}

file class FarmhandWorkNode : ConversationSubjectNode
{
    public override string SubjectId          => "farmhand_work";
    public override string ContextDescription => "asking the farmhand about work and life on the farm";
    public override float  BaseDifficultyScore => 0.35f;
}

file class FarmhandConversationGraph : ConversationGraphFactory
{
    private FarmhandGreetingNode _greeting = null!;
    private FarmhandGossipNode   _gossip   = null!;
    private FarmhandWorkNode     _work     = null!;

    protected override ConversationSubjectNode BuildNodes()
    {
        _greeting = new FarmhandGreetingNode();
        _gossip   = new FarmhandGossipNode();
        _work     = new FarmhandWorkNode();
        return _greeting;
    }

    protected override void ConnectNodes()
    {
        _greeting.PossiblePositiveOutcomes = new()
        {
            new NodeTransitionOutcome(_gossip),
            new AffinityOutcome(+10f),
        };
        _greeting.NegativeOutcome = new AffinityOutcome(-8f);
        _greeting.Transitions = new() { _gossip, _work };

        _gossip.PossiblePositiveOutcomes = new()
        {
            new AffinityOutcome(+12f),
        };
        _gossip.NegativeOutcome = new AffinityOutcome(-5f);
        _gossip.Transitions = new() { _work };

        _work.PossiblePositiveOutcomes = new()
        {
            new AffinityOutcome(+8f),
        };
        _work.NegativeOutcome = new AffinityOutcome(-5f);
        _work.Transitions = new();
    }
}
