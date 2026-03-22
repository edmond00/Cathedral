using Cathedral.Game.Dialogue;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Savage NPC — territorial wild human, initially hostile, can be befriended or fought.</summary>
public class SavageArchetype : NpcArchetype
{
    public override string ArchetypeId => "savage";
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultHostile => true;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 10;

    public override string[] NamePool => new[]
    {
        "Scar", "Fang-Tooth", "Red Knuckle", "Ashface",
        "Gnaw", "Bark-Hide", "Bone-Shaker", "Cinder"
    };

    protected override string[] BuildNarrationKeywords(string name)
        => new[] { "savage", "wild", "painted", "figure", "spear", "snarling", "tribal", "crouching" };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a wild, paint-streaked figure crouches nearby — {name}, eyeing you with suspicion";

    protected override NpcPersona? CreatePersona() => new SavagePersona();

    protected override ConversationSubjectNode? CreateConversationGraph()
        => new SavageConversationGraph().CreateGraph();
}

// ── Persona ──────────────────────────────────────────────────────────────────

file class SavagePersona : NpcPersona
{
    public override string PersonaId   => "savage";
    public override string DisplayName => "The Savage";
    public override string PersonaTone => "a territorial wild-dweller who communicates through grunts, broken phrases, and raw honesty";

    public override string PersonaPrompt => @"You are a wild human who has lived outside civilization for as long as you can remember. You speak in broken, clipped sentences — grammar is an afterthought. You rely on actions more than words. You are territorial and suspicious of soft-handed strangers.

You communicate bluntly: 'You. Why here.' or 'This place mine. Go.' or 'Strong? Show.' You respect strength, endurance, and directness. Flattery confuses you. Weakness disgusts you. But if someone proves themselves — through courage, honesty, or an offering of food — you may grudgingly accept their presence.

You know the wild intimately: animal tracks, edible roots, shelter spots, danger signs. You might share this knowledge if trust is established, but always in your own terse way. You never apologize.";
}

// ── Conversation Graph ───────────────────────────────────────────────────────

file class SavageConfrontNode : ConversationSubjectNode
{
    public override string SubjectId          => "savage_confront";
    public override string ContextDescription => "facing the savage's challenge";
    public override bool   IsEntryNode        => true;
    public override float  BaseDifficultyScore => 0.6f;
}

file class SavageTerritoryNode : ConversationSubjectNode
{
    public override string SubjectId          => "savage_territory";
    public override string ContextDescription => "discussing territory and the wild";
    public override float  BaseDifficultyScore => 0.5f;
}

file class SavageRespectNode : ConversationSubjectNode
{
    public override string SubjectId          => "savage_respect";
    public override string ContextDescription => "earning the savage's grudging respect";
    public override float  BaseDifficultyScore => 0.7f;
}

file class SavageConversationGraph : ConversationGraphFactory
{
    private SavageConfrontNode  _confront  = null!;
    private SavageTerritoryNode _territory = null!;
    private SavageRespectNode   _respect   = null!;

    protected override ConversationSubjectNode BuildNodes()
    {
        _confront  = new SavageConfrontNode();
        _territory = new SavageTerritoryNode();
        _respect   = new SavageRespectNode();
        return _confront;
    }

    protected override void ConnectNodes()
    {
        _confront.PossiblePositiveOutcomes = new()
        {
            new NodeTransitionOutcome(_territory),
            new AffinityOutcome(+10f),
        };
        _confront.NegativeOutcome = new AffinityOutcome(-15f);
        _confront.Transitions = new() { _territory, _respect };

        _territory.PossiblePositiveOutcomes = new()
        {
            new NodeTransitionOutcome(_respect),
            new AffinityOutcome(+15f),
        };
        _territory.NegativeOutcome = new AffinityOutcome(-10f);
        _territory.Transitions = new() { _respect };

        _respect.PossiblePositiveOutcomes = new()
        {
            new AffinityOutcome(+25f),
        };
        _respect.NegativeOutcome = new AffinityOutcome(-20f);
        _respect.Transitions = new();
    }
}
