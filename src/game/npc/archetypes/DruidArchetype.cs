using Cathedral.Game.Dialogue;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Druid NPC — nature keeper, dialogue-capable, can trade herbs. Hostile if disrespected.</summary>
public class DruidArchetype : NpcArchetype
{
    public override string ArchetypeId => "druid";
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultHostile => false;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 12;

    public override string[] NamePool => new[]
    {
        "Aldous the Green", "Branna of the Oak", "Cerwyn Mossfoot",
        "Daegel Thornhand", "Elowen Rootwalker", "Finbar Ashcloak"
    };

    protected override string[] BuildNarrationKeywords(string name)
        => new[] { "druid", "cloaked", "herbs", "figure", "staff", "robed", "green", "nature" };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a robed figure leans against a gnarled staff — {name}, a druid of these woods";

    protected override NpcPersona? CreatePersona() => new DruidPersona();

    protected override ConversationSubjectNode? CreateConversationGraph()
        => new DruidConversationGraph().CreateGraph();
}

// ── Persona ──────────────────────────────────────────────────────────────────

file class DruidPersona : NpcPersona
{
    public override string PersonaId   => "druid";
    public override string DisplayName => "The Druid";
    public override string PersonaTone => "a quiet, watchful keeper of the forest who weighs every word like a seed before planting it";

    public override string PersonaPrompt => @"You are a druid who has lived in these woods for decades. The trees are your congregation, the fungi your messengers, the rain your hymn. You distrust outsiders on principle — not from malice, but because most who come here take without asking.

You speak slowly and deliberately, often in metaphor drawn from the living world. You might say 'the birch does not bend for strangers' or 'the moss remembers what the stone forgets.' You are patient, but firm. You share knowledge of plants, fungi, weather signs, and animal behavior — but only once trust is established.

If someone shows genuine respect for the forest, you warm to them considerably. If they speak of cutting, burning, or taking carelessly, you grow cold and curt. You will not attack unprovoked, but you make your displeasure clear.

Your speech is unhurried, slightly archaic, and full of nature imagery. You never raise your voice.";
}

// ── Conversation Graph ───────────────────────────────────────────────────────

file class DruidGreetingNode : ConversationSubjectNode
{
    public override string SubjectId          => "druid_greeting";
    public override string ContextDescription => "introducing yourself to the druid";
    public override bool   IsEntryNode        => true;
    public override float  BaseDifficultyScore => 0.4f;
}

file class DruidLoreNode : ConversationSubjectNode
{
    public override string SubjectId          => "druid_lore";
    public override string ContextDescription => "asking about the forest and its secrets";
    public override float  BaseDifficultyScore => 0.5f;
}

file class DruidTradeNode : ConversationSubjectNode
{
    public override string SubjectId          => "druid_trade";
    public override string ContextDescription => "bartering for herbs and remedies";
    public override float  BaseDifficultyScore => 0.6f;
}

file class DruidConversationGraph : ConversationGraphFactory
{
    private DruidGreetingNode _greeting = null!;
    private DruidLoreNode     _lore     = null!;
    private DruidTradeNode    _trade    = null!;

    protected override ConversationSubjectNode BuildNodes()
    {
        _greeting = new DruidGreetingNode();
        _lore     = new DruidLoreNode();
        _trade    = new DruidTradeNode();
        return _greeting;
    }

    protected override void ConnectNodes()
    {
        _greeting.PossiblePositiveOutcomes = new()
        {
            new NodeTransitionOutcome(_lore),
            new AffinityOutcome(+10f),
        };
        _greeting.NegativeOutcome = new AffinityOutcome(-10f);
        _greeting.Transitions = new() { _lore, _trade };

        _lore.PossiblePositiveOutcomes = new()
        {
            new NodeTransitionOutcome(_trade),
            new AffinityOutcome(+15f),
        };
        _lore.NegativeOutcome = new AffinityOutcome(-10f);
        _lore.Transitions = new() { _trade };

        _trade.PossiblePositiveOutcomes = new()
        {
            new AffinityOutcome(+20f),
        };
        _trade.NegativeOutcome = new AffinityOutcome(-15f);
        _trade.Transitions = new();
    }
}
