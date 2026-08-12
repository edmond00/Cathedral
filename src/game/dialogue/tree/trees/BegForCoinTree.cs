using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Beg" — asking a stranger for a coin you have no claim to.
///
/// <para>Success is a copper or two and nothing else; failure costs a step of affinity, because
/// being asked for money by somebody you barely know is not neutral. The asymmetry is the point:
/// begging is always available, always cheap, and slowly makes a place like you less.</para>
///
/// <para>The <see cref="BranchDifficulty.Easy"/> ladder throughout. Asking is not hard; being given
/// to is mostly about who you asked.</para>
/// </summary>
public class BegForCoinTree : DialogueTree
{
    public override string TreeId           => "beg_for_coin";
    public override string DisplayName      => "Beg";
    public override string Description      => "asking a stranger for a coin";

    /// <summary>The other chair: a stranger has stopped you and is asking for money.</summary>
    public override string NpcDescription   => "being asked for a coin by a stranger";
    public override string AssociatedVerbId => "beg_for_coin";

    /// <summary>What succeeding at this teaches: picking the face that will stop, and asking it.</summary>
    public override string? GrantedModusMentisId => "beggary";

    /// <summary>A couple of coppers, paid out by the controller once the conversation closes.</summary>
    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[]
    {
        new AlmsOutcome(2),
    };

    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[]
    {
        new AffinityIncrementOutcome(-1),
    };

    private static ResolutionNode End(string id, int depth,
                                      string success, string successIndirect,
                                      string failure, string failureIndirect) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect);

    private static NpcLineNode Pressed(string prefix) => new(
        nodeId:          $"{prefix}_pressed",
        replica:         "I have little enough myself.",
        replicaIndirect: "I tell them I have little enough myself.",
        replicaHeard:    "They tell me they have little enough themselves.",

        new PlayerOption($"{prefix}_accept", "accept that and ask for whatever they can spare",
            "Whatever you can spare. No more than that.",
            "I ask only for whatever they can spare.",
            End($"{prefix}_spare", 2,
                "Here. It is not much. Do not tell anyone I gave it.",
                "I give them a little and ask them not to tell anyone.",
                "Then you understand why the answer is no.",
                "I tell them they understand why the answer is no.")),

        new PlayerOption($"{prefix}_say_hungry", "tell them plainly that I have not eaten",
            "I have not eaten today.",
            "I tell them plainly that I have not eaten today.",
            End($"{prefix}_hungry", 2,
                "That I can do something about. Take it.",
                "I tell them that much I can do something about, and give them a coin.",
                "Nor have half the people in this place.",
                "I tell them half the people here have not eaten either.")));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "Yes? What is it you want?",
        replicaIndirect: "I ask them what it is they want.",
        replicaHeard:    "They ask me what it is I want.",

        new PlayerOption("ask_plain", "ask plainly for a coin",
            "A coin, if you have one to spare.",
            "I ask them plainly for a coin if they have one to spare.",
            Pressed("plain")),

        new PlayerOption("ask_for_work_first", "offer to do something for it first",
            "I will carry or fetch for you. A coin for the work.",
            "I offer to carry or fetch for them in return for a coin.",
            Pressed("work")),

        new PlayerOption("say_nothing_much", "make little of it and ask anyway",
            "Nothing much. A coin, if it is no trouble.",
            "I make little of it and ask for a coin if it is no trouble.",
            Pressed("light")));

    public override NpcLineNode EntryNode => Approach;

    /// <summary>Anybody can be asked. Whether they give is the roll.</summary>
    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => true;
}
