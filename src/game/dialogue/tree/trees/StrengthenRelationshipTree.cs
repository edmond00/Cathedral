using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Strengthen Relationship" — available once the party member is no longer a Stranger.
/// A friendly catch-up: success nudges affinity up one step (max CloseFriend), failure down one
/// (min AnnoyingAcquaintance).
/// </summary>
public class StrengthenRelationshipTree : DialogueTree
{
    public override string TreeId           => "strengthen_relationship";
    public override string DisplayName      => "Strengthen Relationship";
    public override string Description      => "deepening the bond with someone you already know";
    public override string AssociatedVerbId => "strengthen_relationship";

    // ── Resolution nodes ────────────────────────────────────────────────────────

    private static ResolutionNode Parting(string id, string success, string failure) => new(
        nodeId:         id,
        difficulty:     1,
        successReplica: success,
        failureReplica: failure,
        outcomes: new List<DialogueOutcomeCase>
        {
            new(new AffinityIncrementOutcome(+1, AffinityLevel.AnnoyingAcquaintance, AffinityLevel.CloseFriend),
                BranchCondition.Success),
            new(new AffinityIncrementOutcome(-1, AffinityLevel.AnnoyingAcquaintance, AffinityLevel.CloseFriend),
                BranchCondition.Failure),
        });

    private static readonly ResolutionNode ComplimentParting = Parting(
        "compliment_parting",
        "Well — that's kind of you to say. It gladdens me. Take care, now.",
        "Flattery, is it? Save your breath.");

    private static readonly ResolutionNode SmallTalkParting = Parting(
        "small_talk_parting",
        "Aye, a fine day for it. Good to share a word with you.",
        "Mm. Was there anything else?");

    private static readonly ResolutionNode CheckInParting = Parting(
        "check_in_parting",
        "Kind of you to ask after me. It means more than you know. Fare well.",
        "I've no time for idle chatter today.");

    // ── Intermediate NPC response (for the "check in" branch) ───────────────────

    private static readonly NpcLineNode WellEnough = new(
        nodeId:  "well_enough",
        replica: "Well enough, thank you for asking. And yourself?",
        new PlayerOption("share_warmly", "answer warmly and share a little",
            "Can't complain — and all the better for seeing a friendly face.", CheckInParting));

    // ── Entry ───────────────────────────────────────────────────────────────────

    private static readonly NpcLineNode Greeting = new(
        nodeId:  "greeting",
        replica: "Ah, {you:name}! Good to see you again.",
        new PlayerOption("compliment", "offer a genuine compliment",
            "You're looking well, {npc:name}. Truly.", ComplimentParting),
        new PlayerOption("check_in", "ask how they have been",
            "How have you been keeping, {npc:name}?", WellEnough),
        new PlayerOption("small_talk", "make pleasant small talk",
            "Fine weather we're having, isn't it?", SmallTalkParting));

    public override NpcLineNode EntryNode => Greeting;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => !npc.AffinityTable.IsStranger(partyMemberId);
}
