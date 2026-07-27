using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Reconcile" — available when the NPC is an enemy or an AnnoyingAcquaintance.
/// The player tries to end the hostility. Success clears the enemy flag and sets a wary Suspicious
/// affinity; failure leaves them hostile and a brave NPC demands a fight. A hard check (difficulty 2).
/// </summary>
public class ReconcileTree : DialogueTree
{
    public override string TreeId           => "reconcile";
    public override string DisplayName      => "Reconcile";
    public override string Description      => "attempting to end hostility and reach a fragile peace";
    public override string AssociatedVerbId => "reconcile";

    public override IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; } = new IDialogueOutcome[]
    {
        new ClearEnemyOutcome(),
        new SuspiciousAffinityOutcome(),
    };

    public override IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; } = new IDialogueOutcome[]
    {
        new FightRequestOutcome(),
    };

    // ── Resolution nodes ────────────────────────────────────────────────────────

    private static ResolutionNode Outcome(string id, string success, string failure) => new(
        nodeId:         id,
        difficulty:     2,
        successReplica: success,
        failureReplica: failure);

    private static readonly ResolutionNode ApologyOutcome = Outcome(
        "apology_outcome",
        "...Fine. I'll let it lie — for now. Don't make me regret it.",
        "Empty words. If it's a reckoning you're after, you'll have one!");

    private static readonly ResolutionNode ExplainOutcome = Outcome(
        "explain_outcome",
        "Hm. Perhaps I judged you too quickly. We'll leave it there, then.",
        "You twist your words prettily, but I'm not fooled. Draw, if you dare!");

    // ── Intermediate NPC response (for the "apologize" branch) ──────────────────

    private static readonly NpcLineNode HearOut = new(
        nodeId:  "hear_out",
        replica: "...Sorry, are you? Go on, then. I'm listening.",
        new PlayerOption("press_peace", "press for peace between you",
            "Let's put it behind us, {npc:name}. There's no sense in bad blood.", ApologyOutcome));

    // ── Entry ───────────────────────────────────────────────────────────────────

    private static readonly NpcLineNode Opening = new(
        nodeId:  "opening",
        replica: "You've a nerve, showing your face to me. What do you want?",
        new PlayerOption("apologize", "offer a sincere apology",
            "I've come to make peace. I'm sorry for what passed between us.", HearOut),
        new PlayerOption("explain", "explain that the hostility is a misunderstanding",
            "Hear me out — this quarrel between us is a misunderstanding.", ExplainOutcome));

    public override NpcLineNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
    {
        if (npc.AffinityTable.IsEnemy(partyMemberId)) return true;
        return npc.AffinityTable.GetLevel(partyMemberId) == AffinityLevel.AnnoyingAcquaintance;
    }
}
