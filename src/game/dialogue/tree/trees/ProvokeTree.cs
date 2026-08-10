using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Provoke" — saying the thing that cannot be let pass.
///
/// <para>Inverted outcomes, and deliberately so: <b>success starts the fight</b>. This is the one
/// tree where the player is trying to fail a social interaction on purpose, and the check is whether
/// the goad lands well enough that the other person has to answer it. Failing means the insult
/// missed and you are merely disliked, which is the worse result for whoever tried it.</para>
///
/// <para>The fight it produces is one-on-one — see <c>FightRequestOutcome</c> and the no-allies path
/// in the controller. That is the whole reason to provoke rather than simply attack: an attack in a
/// village square brings the section down on you, and a provocation gets one person on their own.</para>
///
/// <para><see cref="BranchDifficulty.Hard"/> throughout. Making somebody swing first, in front of
/// witnesses, without swinging yourself, is not easy.</para>
/// </summary>
public class ProvokeTree : DialogueTree
{
    public override string TreeId           => "provoke";
    public override string DisplayName      => "Provoke";
    public override string Description      => "goading someone into striking the first blow";
    public override string AssociatedVerbId => "provoke";

    /// <summary>What succeeding at this teaches: finding the words that cannot be ignored.</summary>
    public override string? GrantedModusMentisId => "invective";

    /// <summary>The goad landed. They come at you, and they come alone.</summary>
    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[]
    {
        new FightRequestOutcome(personal: true),
    };

    /// <summary>It missed. No fight, and now they think less of you.</summary>
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

    private static NpcLineNode Warned(string prefix) => new(
        nodeId:          $"{prefix}_warned",
        replica:         "You want to be careful how you go on.",
        replicaIndirect: "I warn them to be careful how they go on.",
        replicaHeard:    "They warn me to be careful how I go on.",

        new PlayerOption($"{prefix}_press", "press it, and make it personal",
            "Or what? Say the rest of it.",
            "I press them to say the rest of it.",
            End($"{prefix}_press_end", 2,
                "Right. Outside, then. Now.",
                "I tell them to come outside, now.",
                "No. You are not worth the trouble.",
                "I tell them they are not worth the trouble.")),

        new PlayerOption($"{prefix}_mock", "laugh at the warning",
            "That is the best you have?",
            "I laugh and ask if that is the best they have.",
            End($"{prefix}_mock_end", 2,
                "You will find out what the best I have is.",
                "I tell them they will find out what the best I have is.",
                "Laugh, then. Laugh on your own.",
                "I tell them to laugh on their own.")));

    private static readonly NpcLineNode Opening = new(
        nodeId:          "opening",
        replica:         "Was that meant for me?",
        replicaIndirect: "I ask whether that was meant for me.",
        replicaHeard:    "They ask whether that was meant for them.",

        new PlayerOption("say_yes", "say yes, and hold their eye",
            "It was. And you know it was true.",
            "I say it was meant for them, and that they know it was true.",
            Warned("yes")),

        new PlayerOption("insult_work", "say something about their work",
            "I have seen better done by children.",
            "I tell them I have seen better done by children.",
            Warned("work")),

        new PlayerOption("insult_standing", "say something about their standing here",
            "Nobody in this place would miss you.",
            "I tell them nobody here would miss them.",
            Warned("standing")));

    public override NpcLineNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => true;
}
