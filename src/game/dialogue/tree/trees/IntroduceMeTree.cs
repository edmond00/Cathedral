using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Ask for an Introduction" — the first tree in the game about somebody who is not in the room.
///
/// <para>Every other conversation concerns the two people having it. This one has a subject, reached
/// through the <c>{third:*}</c> token family: <c>{third:role}</c>, <c>{third:craft}</c> and
/// <c>{third:relation}</c> let one tree serve an apprentice offering their master and a farmhand
/// offering the reeve, in each speaker's own terms.</para>
///
/// <para>Succeeding puts the player in front of the third party, already introduced — see
/// <see cref="IntroductionGrantedOutcome"/>. Failing costs a step with the go-between, because
/// asking somebody to spend their standing on you and being refused is a real thing to have
/// done.</para>
///
/// <para><see cref="BranchDifficulty.Hard"/> throughout: you are asking for the use of a
/// relationship you have no part in.</para>
/// </summary>
public class IntroduceMeTree : DialogueTree
{
    public override string TreeId           => "introduce_me";
    public override string DisplayName      => "Ask for an Introduction";
    public override string Description      => "asking them to present you to someone they have standing with";

    /// <summary>The other chair — and the pronouns invert with it: you are the one being asked.</summary>
    public override string NpcDescription   => "being asked to present this person to someone you have standing with";
    public override string AssociatedVerbId => "introduce_me";

    /// <summary>What succeeding teaches: getting into a room on somebody else's word.</summary>
    public override string? GrantedModusMentisId => "high_society_manners";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[]
    {
        new IntroductionGrantedOutcome(),
    };

    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[]
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
        replica:         "{third:relation} does not see people on my say-so.",
        replicaIndirect: "I tell them that {third:relation} does not see people on my say-so.",
        replicaHeard:    "They say the person I am asking about does not see people on their say-so.",

        new PlayerOption($"{prefix}_state_business", "tell them exactly what I want with {third:role}",
            "Then hear what I want, and judge whether it is worth carrying.",
            "I ask them to hear what I want and judge whether it is worth carrying.",
            End($"{prefix}_business", 3,
                "It is worth carrying. Come, I will walk you over.",
                "I tell them it is worth carrying, and offer to walk them over.",
                "It is not. Ask someone else.",
                "I tell them it is not worth carrying, and to ask someone else.")),

        new PlayerOption($"{prefix}_ask_small", "ask only that they say my name, nothing more",
            "Say my name and that I am not a fool. That is all I ask.",
            "I ask only that they say my name and that I am not a fool.",
            End($"{prefix}_small", 3,
                "That much I can do. Come with me.",
                "I tell them that much I can do, and to come with me.",
                "Even that is more than I will spend on a stranger.",
                "I tell them even that is more than I will spend on a stranger.")));

    private static readonly NpcLineNode Opening = new(
        nodeId:          "intro_opening",
        replica:         "You want something. Out with it.",
        replicaIndirect: "I tell them they want something and to say it.",
        replicaHeard:    "They say I want something, and tell me to say it.",

        new PlayerOption("ask_direct", "ask them outright to take me to {third:role}",
            "Take me to {third:name}. I would speak with them.",
            "I ask them to take me to {third:name}, as I would speak with them.",
            Weighing("direct")),

        new PlayerOption("ask_about_craft", "show that I know something of {third:craft} first",
            "I know a little of {third:craft}. Enough to want to know more.",
            "I tell them I know a little of {third:craft}, enough to want to know more.",
            Weighing("craft")),

        new PlayerOption("ask_favour", "ask it plainly as a favour",
            "It is a favour, and I know it. I am asking anyway.",
            "I tell them it is a favour, that I know it, and that I am asking anyway.",
            Weighing("favour")));

    public override NpcLineNode EntryNode => Opening;

    /// <summary>
    /// The verb decides this — it knows who is standing where and who has already been met.
    /// Duplicating any of it here would only let the two drift apart.
    /// </summary>
    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => true;
}
