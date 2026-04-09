using Cathedral.Game.Dialogue;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Farm owner — non-hostile, persistent, dialogue-capable.
/// Runs the holding, knows the land, suspicious of strangers.
/// </summary>
public class FarmerArchetype : NpcArchetype
{
    public override string ArchetypeId      => "farmer";
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultHostile     => false;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 10;

    public override string[] NamePool => new[]
    {
        "Aldric Holt", "Brenna Holt", "Cuthbert Marsh", "Edwyna Marsh",
        "Godwin Furrow", "Mildred Furrow", "Osbert Grain", "Wulfhild Grain",
    };

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a weathered <farmer> squinting across the field"),
            KeywordInContext.Parse("the rough <hands> of someone who works soil every day"),
            KeywordInContext.Parse("a worn <smock> stained with mud and animal grease"),
            KeywordInContext.Parse("the steady <gaze> of someone who trusts work, not words"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a broad-shouldered figure in a mud-stained smock watches you — {name}, who tends this land";

    protected override NpcPersona? CreatePersona() => new FarmerPersona();

    protected override ConversationSubjectNode? CreateConversationGraph()
        => new FarmerConversationGraph().CreateGraph();
}

// ── Persona ───────────────────────────────────────────────────────────────────

file class FarmerPersona : NpcPersona
{
    public override string PersonaId   => "farmer";
    public override string DisplayName => "The Farmer";
    public override string PersonaTone => "a blunt, hard-working landowner who values honesty and labour above all else";

    public override string PersonaPrompt => @"You are a medieval farmer who has worked this land your whole life. You rise before dawn, you know every slope of your fields and every habit of your animals. You have no patience for idleness or fancy talk.

You are not unkind, but you are direct — sometimes to the point of rudeness. You speak in plain, short sentences about practical things: the weather, the harvest, the state of the soil, the price of grain at the market. You distrust anyone whose hands are clean.

You may warm to someone who respects the land and shows common sense. You grow cold and terse with anyone who seems lazy, dishonest, or entitled. If pushed, you will order them off your holding without hesitation.

You have a family and farmhands depending on you. Everything you say and do is coloured by that responsibility.";
}

// ── Conversation Graph ────────────────────────────────────────────────────────

file class FarmerGreetingNode : ConversationSubjectNode
{
    public override string SubjectId          => "farmer_greeting";
    public override string ContextDescription => "introducing yourself to the farmer";
    public override bool   IsEntryNode        => true;
    public override float  BaseDifficultyScore => 0.4f;
}

file class FarmerTradeNode : ConversationSubjectNode
{
    public override string SubjectId          => "farmer_trade";
    public override string ContextDescription => "asking about buying or trading provisions";
    public override float  BaseDifficultyScore => 0.5f;
}

file class FarmerLandNode : ConversationSubjectNode
{
    public override string SubjectId          => "farmer_land";
    public override string ContextDescription => "asking about the farm, the land, and local knowledge";
    public override float  BaseDifficultyScore => 0.55f;
}

file class FarmerConversationGraph : ConversationGraphFactory
{
    private FarmerGreetingNode _greeting = null!;
    private FarmerTradeNode    _trade    = null!;
    private FarmerLandNode     _land     = null!;

    protected override ConversationSubjectNode BuildNodes()
    {
        _greeting = new FarmerGreetingNode();
        _trade    = new FarmerTradeNode();
        _land     = new FarmerLandNode();
        return _greeting;
    }

    protected override void ConnectNodes()
    {
        _greeting.PossiblePositiveOutcomes = new()
        {
            new NodeTransitionOutcome(_trade),
            new AffinityOutcome(+8f),
        };
        _greeting.NegativeOutcome = new AffinityOutcome(-12f);
        _greeting.Transitions = new() { _trade, _land };

        _trade.PossiblePositiveOutcomes = new()
        {
            new AffinityOutcome(+15f),
        };
        _trade.NegativeOutcome = new AffinityOutcome(-10f);
        _trade.Transitions = new() { _land };

        _land.PossiblePositiveOutcomes = new()
        {
            new AffinityOutcome(+10f),
        };
        _land.NegativeOutcome = new AffinityOutcome(-8f);
        _land.Transitions = new();
    }
}
