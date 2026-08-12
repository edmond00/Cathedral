using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Wake" — the only conversation a sleeping person will have, and the only way to open any other.
///
/// <para>Every other dialogue verb refuses a sleeper, so at night in somebody's own room this tree is
/// the whole of what talking can be. Succeeding gets them upright and civil; failing means they come
/// out of sleep frightened and swinging, and the failure outcome is a fight.</para>
///
/// <para>Branches are short on purpose. There is no long approach to waking a stranger at night —
/// you say one thing, and how you say it decides everything — so every branch resolves at two or
/// three replies on the <see cref="BranchDifficulty.Hard"/> ladder. Hard because it deserves to be:
/// there is no innocent reading of a stranger standing over your bed.</para>
/// </summary>
public class WakeUpTree : DialogueTree
{
    public override string TreeId           => "wake_up";
    public override string DisplayName      => "Wake";
    public override string Description      => "waking a sleeping person without frightening them into violence";

    /// <summary>The other chair: you are the sleeper, woken by someone standing over you.</summary>
    public override string NpcDescription   => "being woken from sleep by a stranger standing over you";
    public override string AssociatedVerbId => "wake_up";

    /// <summary>What succeeding at this teaches: keeping somebody calm who has every reason not to be.</summary>
    public override string? GrantedModusMentisId => "murmur";

    /// <summary>Woken and willing to talk. The verb itself has already roused them.</summary>
    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[]
    {
        new SuspiciousAffinityOutcome(),
    };

    /// <summary>Woken badly. Somebody frightened in their own bed does not stop to ask questions.</summary>
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[]
    {
        new FightRequestOutcome(),
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

    // ── Branch: name yourself at once ─────────────────────────────────────────

    private static NpcLineNode Named(string prefix) => new(
        nodeId:          $"{prefix}_named",
        replica:         "Who is that? Stand where I can see you.",
        replicaIndirect: "I ask who is there and tell them to stand where I can see them.",
        replicaHeard:    "The sleeper asks who is there and tells me to stand where they can see me.",

        new PlayerOption($"{prefix}_step_into_light", "step into the light and let them see my face",
            "Here. Look at me. I mean you no harm.",
            "I step where they can see me and tell them I mean no harm.",
            End($"{prefix}_named_light", 2,
                "All right. All right. You could have knocked.",
                "I tell them it is all right, and that they could have knocked.",
                "Get back. Get back from me.",
                "I tell them to get back from me.")),

        new PlayerOption($"{prefix}_stay_still", "stay exactly where I am and keep talking",
            "I will not come closer. Only listen.",
            "I tell them I will not come closer, and ask them only to listen.",
            End($"{prefix}_named_still", 2,
                "Say it then. Quickly.",
                "I tell them to say it, and to be quick.",
                "You are in my house and you will not move? Get out.",
                "I tell them they are in my house and order them out.")));

    // ── Branch: apologise first ───────────────────────────────────────────────

    private static NpcLineNode Apology(string prefix) => new(
        nodeId:          $"{prefix}_apology",
        replica:         "You have no business here at this hour.",
        replicaIndirect: "I tell them they have no business here at this hour.",
        replicaHeard:    "The sleeper tells me I have no business here at this hour.",

        new PlayerOption($"{prefix}_agree_and_ask", "agree, and ask for a moment anyway",
            "I know it. Give me a moment and I will go.",
            "I agree, and ask for a moment before I go.",
            End($"{prefix}_apology_moment", 2,
                "A moment. Then you go.",
                "I give them a moment, and tell them to go after it.",
                "You will go now, or I will make you.",
                "I tell them to go now or be made to.")),

        new PlayerOption($"{prefix}_explain_urgency", "tell them it could not wait until morning",
            "It could not wait for morning. It would not have kept.",
            "I tell them it could not have waited until morning.",
            End($"{prefix}_apology_urgent", 2,
                "Then it had better be worth the waking. Speak.",
                "I tell them it had better be worth the waking, and to speak.",
                "Nothing keeps me from my bed. Nothing you have.",
                "I tell them nothing they have is worth my bed.")));

    // ── Entry ─────────────────────────────────────────────────────────────────

    private static readonly NpcLineNode Stirring = new(
        nodeId:          "stirring",
        replica:         "Mm. Who — who is there?",
        replicaIndirect: "I stir and ask who is there.",
        replicaHeard:    "The sleeper stirs and asks who is there.",

        new PlayerOption("speak_softly", "speak softly and keep still",
            "It is only me. Be easy.",
            "I speak softly and tell them to be easy.",
            Named("soft")),

        new PlayerOption("give_name", "give my name straight away",
            "My name is {you:name}. I am not here to harm you.",
            "I give them my name and tell them I am not here to harm them.",
            Named("name")),

        new PlayerOption("apologise", "apologise for the hour",
            "Forgive the hour. I would not have come otherwise.",
            "I apologise for the hour and say I would not have come otherwise.",
            Apology("sorry")),

        new PlayerOption("shake_awake", "shake them properly awake",
            "Up. Wake up.",
            "I tell them to wake up.",
            Apology("shake")));

    public override NpcLineNode EntryNode => Stirring;

    /// <summary>
    /// Availability is decided entirely by the verb, which knows about beds and hours. Affinity has
    /// nothing to say about it: an old friend woken at midnight is as startled as a stranger.
    /// </summary>
    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => true;
}
