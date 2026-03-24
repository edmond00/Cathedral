using System.Collections.Generic;

namespace Cathedral.Game.Dialogue.Demo;

// ── Subject Nodes ────────────────────────────────────────────────────────────

/// <summary>Initial exchange — the innkeeper sizes up the stranger.</summary>
public class GreetingSubjectNode : ConversationSubjectNode
{
    public override string SubjectId          => "greeting";
    public override string ContextDescription => "breaking the ice with the innkeeper";
    public override bool   IsEntryNode        => true;
    public override float  BaseDifficultyScore => 0.3f;
}

/// <summary>Picking up local whispers and gossip about the road ahead.</summary>
public class RumorsSubjectNode : ConversationSubjectNode
{
    public override string SubjectId          => "rumors";
    public override string ContextDescription => "asking about local rumors and road conditions";
    public override float  BaseDifficultyScore => 0.5f;
}

/// <summary>Probing for paid work the innkeeper may know about.</summary>
public class JobSubjectNode : ConversationSubjectNode
{
    public override string SubjectId          => "job";
    public override string ContextDescription => "inquiring about available work in the area";
    public override float  BaseDifficultyScore => 0.7f;
}

// ── Factory ──────────────────────────────────────────────────────────────────

/// <summary>
/// Wires together the InnKeeper's 3-node conversation graph:
///   GreetingNode → RumorsNode → JobNode
/// </summary>
public class InnKeeperConversationGraph : ConversationGraphFactory
{
    private GreetingSubjectNode _greeting = null!;
    private RumorsSubjectNode   _rumors   = null!;
    private JobSubjectNode      _job      = null!;

    protected override ConversationSubjectNode BuildNodes()
    {
        _greeting = new GreetingSubjectNode();
        _rumors   = new RumorsSubjectNode();
        _job      = new JobSubjectNode();
        return _greeting;
    }

    protected override void ConnectNodes()
    {
        // Greeting: succeed → transition to Rumors, or just gain goodwill
        _greeting.PossiblePositiveOutcomes = new List<ConversationOutcome>
        {
            new NodeTransitionOutcome(_rumors),
            new AffinityOutcome(+15f),
        };
        _greeting.NegativeOutcome = new AffinityOutcome(-10f);
        _greeting.Transitions = new List<ConversationSubjectNode> { _rumors, _job };

        // Rumors: succeed → transition to Job, or strengthen relationship
        _rumors.PossiblePositiveOutcomes = new List<ConversationOutcome>
        {
            new NodeTransitionOutcome(_job),
            new AffinityOutcome(+15f),
        };
        _rumors.NegativeOutcome = new AffinityOutcome(-10f);
        _rumors.Transitions = new List<ConversationSubjectNode> { _job };

        // Job: succeed → significant affinity gain (quest hook)
        _job.PossiblePositiveOutcomes = new List<ConversationOutcome>
        {
            new AffinityOutcome(+25f),
        };
        _job.NegativeOutcome = new AffinityOutcome(-15f);
        _job.Transitions = new List<ConversationSubjectNode>();
    }
}
