using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Reconcile" — available when the NPC is an enemy of the protagonist or has
/// AnnoyingAcquaintance affinity.
///
/// Tree structure:
///   opening (entry)
///     ├─ apologize → negotiation
///     └─ explain   → negotiation
///
///   negotiation (terminal):
///     ✓ Success → reconciled: ClearEnemy + Suspicious affinity
///     ✗ Failure → rejected: stays enemy [IsBrave NPCs also demand a fight]
/// </summary>
public class ReconcileTree : DialogueTree
{
    public override string TreeId           => "reconcile";
    public override string DisplayName      => "Reconcile";
    public override string Description      => "attempting to end hostility and reach a fragile peace";
    public override string AssociatedVerbId => "reconcile";

    // ── Terminal node ─────────────────────────────────────────────────────────

    private static readonly DialogueTreeNode Negotiation = new(
        nodeId:      "negotiation",
        description: "pressing your case and trying to convince them to stand down",
        outcomes: new List<DialogueOutcomeCase>
        {
            // Success: clear enemy flag, set Suspicious affinity
            new(new ClearEnemyOutcome(),       BranchCondition.Success),
            new(new SuspiciousAffinityOutcome(), BranchCondition.Success),
            // Failure: stays enemy; brave NPCs demand a fight
            new(new FightRequestOutcome(),     BranchCondition.Failure),
        });

    // ── Intermediate nodes ────────────────────────────────────────────────────

    private static readonly DialogueTreeNode Apologize = new(
        nodeId:      "apologize",
        description: "offering a sincere apology and asking for a chance to make things right",
        branches: new List<DialogueBranch>
        {
            new(Negotiation, BranchCondition.Either),
        });

    private static readonly DialogueTreeNode Explain = new(
        nodeId:      "explain",
        description: "explaining your side of things and arguing that the hostility is unwarranted",
        branches: new List<DialogueBranch>
        {
            new(Negotiation, BranchCondition.Either),
        });

    // ── Entry node ────────────────────────────────────────────────────────────

    private static readonly DialogueTreeNode Opening = new(
        nodeId:      "opening",
        description: "opening the conversation and signalling you want to end the hostility",
        branches: new List<DialogueBranch>
        {
            new(Apologize, BranchCondition.Either),
            new(Explain,   BranchCondition.Either),
        });

    public override DialogueTreeNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
    {
        if (npc.AffinityTable.IsEnemy(partyMemberId)) return true;
        return npc.AffinityTable.GetLevel(partyMemberId) == AffinityLevel.AnnoyingAcquaintance;
    }
}
