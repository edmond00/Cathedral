using System;
using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Ask what they know" — getting somebody to tell you something worth knowing.
///
/// <para>One tree serves every speaking archetype, filled from the NPC through the existing token
/// family: <c>{npc:craft}</c>, <c>{npc:workplace}</c>, <c>{npc:location}</c>. A blacksmith and a
/// shepherd walk the same branches and say entirely different things, which is what the draft's
/// "general template tree with many fields" meant and is far less content to keep straight than one
/// tree per trade.</para>
///
/// <para><b>Two lessons on success.</b> <see cref="GrantedModusMentisId"/> gives the general skill
/// of drawing somebody out, which is the same whoever you drew out; the branch then gives what you
/// actually learned, decided from the topic and the speaker — see
/// <see cref="AdditionalGrantedModusMentisIds"/>. Asking a blacksmith about his trade teaches
/// metalwork; asking a shepherd the same question teaches husbandry.</para>
///
/// <para>All three topics are always offered. A stranger being cagey about the neighbours is the
/// dice check's job, not the menu's.</para>
/// </summary>
public class GatherKnowledgeTree : DialogueTree
{
    /// <summary>The three things anybody can be asked about. Carried on each resolution's topic tag.</summary>
    public const string TopicTrade  = "trade";
    public const string TopicPlace  = "place";
    public const string TopicPeople = "people";

    public override string TreeId           => "gather_knowledge";
    public override string DisplayName      => "Ask What They Know";
    public override string Description      => "drawing out what this person knows";
    public override string AssociatedVerbId => "gather_knowledge";

    /// <summary>The general skill: getting somebody to talk. The same whatever they talked about.</summary>
    public override string? GrantedModusMentisId => "inquiry";

    /// <summary>
    /// The substance of what was learned. Trade knowledge is whatever this person's work actually is
    /// (<c>NamedNpcArchetype.TradeModusMentisId</c>); the lie of the land is the lie of the land
    /// whoever describes it; and who-is-who in a place is streetwise wherever the place is.
    /// </summary>
    public override IEnumerable<string> AdditionalGrantedModusMentisIds(NpcEntity npc, ResolutionNode resolution)
    {
        string? id = resolution.Topic switch
        {
            TopicTrade  => (npc.Archetype as NamedNpcArchetype)?.TradeModusMentisId ?? "peasantry",
            TopicPlace  => "topographia",
            TopicPeople => "streetwise",
            _           => null,
        };

        if (id != null) yield return id;
    }

    /// <summary>Being treated as somebody worth asking is not nothing.</summary>
    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[]
    {
        new AffinityIncrementOutcome(0),
    };

    /// <summary>Being pumped for information by somebody who got nothing is merely tiresome.</summary>
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[]
    {
        new AffinityIncrementOutcome(0),
    };

    private static ResolutionNode End(string id, int depth, string topic,
                                      string success, string successIndirect,
                                      string failure, string failureIndirect) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        topic:                  topic);

    // ── Their trade ───────────────────────────────────────────────────────────

    private static NpcLineNode Trade() => new(
        nodeId:          "know_trade",
        replica:         "{npc:craft}. I have done it long enough to have opinions about it.",
        replicaIndirect: "I tell them my work is {npc:craft} and that I have opinions about it.",
        replicaHeard:    "They tell me what their work is and say they have opinions about it.",

        new PlayerOption("trade_ask_hardest", "ask what the hardest part of it is",
            "What is the part of it nobody sees?",
            "I ask them what part of the work nobody sees.",
            End("trade_hardest", 2, TopicTrade,
                "The waiting. Everyone thinks it is the doing. Let me show you.",
                "I tell them it is the waiting, not the doing, and show them what I mean.",
                "You would not follow it if I told you.",
                "I tell them they would not follow it if I explained.")),

        new PlayerOption("trade_ask_learned", "ask how they learned it",
            "Who taught you, and how long did it take?",
            "I ask who taught them and how long it took.",
            End("trade_learned", 2, TopicTrade,
                "Seven years, and I still get it wrong. Here is what I would tell a beginner.",
                "I tell them it took seven years, that I still get it wrong, and what I would tell a beginner.",
                "Long enough that I will not hand it to you over a fence.",
                "I tell them it took long enough that I will not hand it over a fence.")));

    // ── This place ────────────────────────────────────────────────────────────

    private static NpcLineNode Place() => new(
        nodeId:          "know_place",
        replica:         "I have been here long enough. What of it?",
        replicaIndirect: "I tell them I have been here long enough, and ask what of it.",
        replicaHeard:    "They say they have been here long enough, and ask what of it.",

        new PlayerOption("place_ask_layout", "ask how the place is laid out",
            "What lies which way from here?",
            "I ask them what lies which way from here.",
            End("place_layout", 2, TopicPlace,
                "That way for the water, that way for the high ground. I will walk you through it.",
                "I tell them which way the water and the high ground lie, and walk them through it.",
                "You have eyes. Use them.",
                "I tell them they have eyes and can use them.")),

        new PlayerOption("place_ask_avoid", "ask what a stranger should keep away from",
            "What would you tell a stranger to keep away from?",
            "I ask what they would tell a stranger to keep away from.",
            End("place_avoid", 2, TopicPlace,
                "Two things, and you would not guess either. Listen.",
                "I tell them there are two things they would not guess, and to listen.",
                "Keep away from asking that, for one.",
                "I tell them to keep away from asking that, for a start.")));

    // ── The people here ───────────────────────────────────────────────────────

    private static NpcLineNode People() => new(
        nodeId:          "know_people",
        replica:         "People. There are a few. Which of them?",
        replicaIndirect: "I say there are a few people here and ask which of them.",
        replicaHeard:    "They say there are a few people here and ask which of them.",

        new PlayerOption("people_ask_who_counts", "ask who actually decides things here",
            "Who is it that actually decides things?",
            "I ask who it is that actually decides things here.",
            End("people_who", 2, TopicPeople,
                "Not the one you would think. I will tell you how it really runs.",
                "I tell them it is not who they would think, and explain how it really runs.",
                "Whoever it is, it is not my business to say.",
                "I tell them it is not my business to say.")),

        new PlayerOption("people_ask_trust", "ask who is worth trusting",
            "Who here would you take at their word?",
            "I ask who here they would take at their word.",
            End("people_trust", 2, TopicPeople,
                "Two of them. And I will tell you which two to watch instead.",
                "I name two, and tell them which two to watch instead.",
                "I would take myself at my word. Beyond that, find out.",
                "I tell them to find that out for themselves.")));

    // ── Entry ─────────────────────────────────────────────────────────────────

    private static readonly NpcLineNode Opening = new(
        nodeId:          "knowledge_opening",
        replica:         "You are wanting to know something.",
        replicaIndirect: "I tell them they are wanting to know something.",
        replicaHeard:    "They say I am wanting to know something.",

        new PlayerOption("about_trade", "ask about their work",
            "Tell me about your work.",
            "I ask them to tell me about their work.",
            Trade()),

        new PlayerOption("about_place", "ask about this place",
            "Tell me about this place.",
            "I ask them to tell me about this place.",
            Place()),

        new PlayerOption("about_people", "ask about the people here",
            "Tell me about the people here.",
            "I ask them to tell me about the people here.",
            People()));

    public override NpcLineNode EntryNode => Opening;

    /// <summary>Anybody who talks can be asked. What they will say is the roll.</summary>
    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => true;
}
