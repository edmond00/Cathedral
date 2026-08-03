using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Ask to Join You" — asking somebody to leave their whole life and travel with you.
///
/// <para>The largest thing a conversation can achieve, and the verb gates it behind a real
/// relationship before this tree ever opens. Success sets the join flag the controller acts on once
/// the session closes; failure costs a step, because being asked and refusing is awkward for
/// everybody.</para>
///
/// <para><see cref="BranchDifficulty.Hard"/> throughout. Nobody walks away from a trade and a roof on
/// a well-turned sentence.</para>
/// </summary>
public class ProposeToJoinTree : DialogueTree
{
    public override string TreeId           => "propose_to_join";
    public override string DisplayName      => "Ask to Join You";
    public override string Description      => "asking them to leave this place and travel with you";
    public override string AssociatedVerbId => "propose_to_join";

    /// <summary>What succeeding at this teaches: asking somebody for everything and being told yes.</summary>
    public override string? GrantedModusMentisId => "friendship";

    public override IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; } = new IDialogueOutcome[]
    {
        new JoinPartyOutcome(),
        new AffinityTransitionOutcome(AffinityLevel.CloseFriend),
    };

    public override IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; } = new IDialogueOutcome[]
    {
        new AffinityIncrementOutcome(-1),
    };

    private static ResolutionNode End(string id, int depth,
                                      string success, string successIndirect,
                                      string failure, string failureIndirect) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Hard(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect);

    private static NpcLineNode Weighing(string prefix) => new(
        nodeId:          $"{prefix}_weighing",
        replica:         "I have work here. A place. Why would I leave it?",
        replicaIndirect: "I tell them I have work here and a place, and ask why I would leave it.",
        replicaHeard:    "They say they have work here and a place, and ask why they would leave it.",

        new PlayerOption($"{prefix}_honest", "tell them honestly what the road is like",
            "It is hard going and I cannot promise you better. Only different.",
            "I tell them honestly it is hard going, and that I can promise only different, not better.",
            End($"{prefix}_honest_end", 3,
                "Different. Yes. I will come.",
                "I tell them different is enough, and that I will come.",
                "Then I will keep the place I have.",
                "I tell them I will keep the place I have.")),

        new PlayerOption($"{prefix}_need", "tell them I need them, and say why",
            "Because I cannot do it alone, and I would rather it were you.",
            "I tell them I cannot do it alone and would rather it were them.",
            End($"{prefix}_need_end", 3,
                "Then I had better come, had I not.",
                "I tell them I had better come, then.",
                "Wanting me is not the same as needing me. Ask someone else.",
                "I tell them wanting is not needing, and to ask someone else.")));

    private static readonly NpcLineNode Opening = new(
        nodeId:          "join_opening",
        replica:         "You have something on your mind. Say it.",
        replicaIndirect: "I tell them they have something on their mind and to say it.",
        replicaHeard:    "They say I have something on my mind, and tell me to say it.",

        new PlayerOption("ask_outright", "ask outright",
            "Come with me. Leave this and come with me.",
            "I ask them outright to leave this and come with me.",
            Weighing("outright")),

        new PlayerOption("ask_gently", "lead up to it",
            "There is a road out of here, and I would not walk it alone.",
            "I tell them there is a road out of here and that I would not walk it alone.",
            Weighing("gentle")),

        new PlayerOption("offer_share", "offer them a share of whatever comes",
            "Whatever I come by, you would have half of it.",
            "I offer them half of whatever I come by.",
            Weighing("share")));

    public override NpcLineNode EntryNode => Opening;

    /// <summary>
    /// The verb decides availability — it knows about affinity and about how full the party is.
    /// Repeating either here would only let the two drift apart.
    /// </summary>
    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => true;
}
