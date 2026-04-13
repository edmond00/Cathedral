using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Strengthen Relationship" — available once the party member is no longer a Stranger.
/// Tree structure:
///   greeting → compliment / check_in / small_talk
///   compliment → salutation
///   check_in   → salutation
///   small_talk → salutation
///   salutation (terminal):
///     Success → affinity +1 step (max CloseFriend)
///     Failure → affinity -1 step (min AnnoyingAcquaintance)
/// </summary>
public class StrengthenRelationshipTree : DialogueTree
{
    public override string TreeId           => "strengthen_relationship";
    public override string DisplayName      => "Strengthen Relationship";
    public override string Description      => "deepening the bond with someone you already know";
    public override string AssociatedVerbId => "strengthen_relationship";

    // ── Terminal node ─────────────────────────────────────────────────────────

    private static readonly DialogueTreeNode Salutation = new(
        nodeId:      "salutation",
        description: "wrapping up the conversation and parting warmly — or not",
        outcomes: new List<DialogueOutcomeCase>
        {
            new(new AffinityIncrementOutcome(+1, AffinityLevel.AnnoyingAcquaintance, AffinityLevel.CloseFriend),
                BranchCondition.Success),
            new(new AffinityIncrementOutcome(-1, AffinityLevel.AnnoyingAcquaintance, AffinityLevel.CloseFriend),
                BranchCondition.Failure),
        });

    // ── Intermediate nodes ────────────────────────────────────────────────────

    private static readonly DialogueTreeNode Compliment = new(
        nodeId:      "compliment",
        description: "offering a genuine compliment or words of appreciation",
        branches: new List<DialogueBranch>
        {
            new(Salutation, BranchCondition.Either),
        });

    private static readonly DialogueTreeNode CheckIn = new(
        nodeId:      "check_in",
        description: "asking how the other person is doing and showing genuine interest",
        branches: new List<DialogueBranch>
        {
            new(Salutation, BranchCondition.Either),
        });

    private static readonly DialogueTreeNode SmallTalk = new(
        nodeId:      "small_talk",
        description: "making pleasant conversation about everyday topics",
        branches: new List<DialogueBranch>
        {
            new(Salutation, BranchCondition.Either),
        });

    private static readonly DialogueTreeNode Greeting = new(
        nodeId:      "greeting",
        description: "greeting someone you have already met before",
        isEntry:     true,
        branches: new List<DialogueBranch>
        {
            new(Compliment, BranchCondition.Either),
            new(CheckIn,    BranchCondition.Either),
            new(SmallTalk,  BranchCondition.Either),
        });

    public override DialogueTreeNode EntryNode => Greeting;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => !npc.AffinityTable.IsStranger(partyMemberId);
}
