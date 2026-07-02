using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Request job" — the player asks a master or reeve to take them on for a specific job.
///
/// Tree structure:
///   opening (entry)
///     ├─ ask_plainly    → decide
///     └─ show_willing   → decide
///   decide (terminal):
///     ✓ Success → OpenJobMenuOutcome  (the work menu opens after dialogue)
///     ✗ Failure → nothing (the NPC turns you away for now)
/// </summary>
public class RequestJobTree : DialogueTree
{
    public override string TreeId           => "request_job";
    public override string DisplayName      => "Request Job";
    public override string Description      => "asking a master or reeve to take you on for work";
    public override string AssociatedVerbId => "request_job";

    private static readonly DialogueTreeNode Decide = new(
        nodeId:      "decide",
        description: "waiting on their answer as they weigh whether to take you on",
        outcomes: new List<DialogueOutcomeCase>
        {
            new(new OpenJobMenuOutcome(), BranchCondition.Success),
        });

    private static readonly DialogueTreeNode AskPlainly = new(
        nodeId:      "ask_plainly",
        description: "asking plainly for the work and what it pays",
        branches: new List<DialogueBranch> { new(Decide, BranchCondition.Either) });

    private static readonly DialogueTreeNode ShowWilling = new(
        nodeId:      "show_willing",
        description: "showing you are willing and able for the labour",
        branches: new List<DialogueBranch> { new(Decide, BranchCondition.Either) });

    private static readonly DialogueTreeNode Opening = new(
        nodeId:      "opening",
        description: "opening the conversation with a request for work",
        isEntry:     true,
        branches: new List<DialogueBranch>
        {
            new(AskPlainly,  BranchCondition.Either),
            new(ShowWilling, BranchCondition.Either),
        });

    public override DialogueTreeNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
    {
        if (npc.AffinityTable.IsEnemy(partyMemberId)) return false;
        var level = npc.AffinityTable.GetLevel(partyMemberId);
        return level is AffinityLevel.DistantAcquaintance
                     or AffinityLevel.CloseAcquaintance
                     or AffinityLevel.DistantFriend
                     or AffinityLevel.CloseFriend;
    }
}
