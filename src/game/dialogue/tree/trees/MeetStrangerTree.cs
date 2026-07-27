using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Meet Stranger" — available only before any dialogue has occurred (Stranger affinity).
/// A first meeting: the NPC greets, the player opens in one of three ways, and after a short
/// exchange the single check decides whether the acquaintance starts warm or sour.
/// </summary>
public class MeetStrangerTree : DialogueTree
{
    public override string TreeId           => "meet_stranger";
    public override string DisplayName      => "Meet Stranger";
    public override string Description      => "meeting this person for the first time and exchanging introductions";
    public override string AssociatedVerbId => "meet_stranger";

    public override IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; } = new IDialogueOutcome[]
    {
        new AffinityTransitionOutcome(AffinityLevel.DistantAcquaintance),
    };

    public override IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; } = new IDialogueOutcome[]
    {
        new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance),
    };

    // ── Resolution nodes (branch ends) ─────────────────────────────────────────

    private static ResolutionNode Parting(string id, string success, string failure) => new(
        nodeId:         id,
        difficulty:     1,
        successReplica: success,
        failureReplica: failure);

    private static readonly ResolutionNode WarmParting = Parting(
        "warm_parting",
        "Ha! A friendly sort. Well met indeed — safe roads to you.",
        "...Aye. Good day to you.");

    private static readonly ResolutionNode NamedParting = Parting(
        "named_parting",
        "{you:name}. I'll remember it. Fare well, then.",
        "Hm. Keep to yourself and we'll have no quarrel.");

    private static readonly ResolutionNode IntroParting = Parting(
        "intro_parting",
        "{you:name}, is it? A pleasure. Until next time.",
        "If you say so. Good day.");

    // ── Intermediate NPC response (for the "ask who they are" branch) ───────────

    private static readonly NpcLineNode WhoAmI = new(
        nodeId:  "who_am_i",
        replica: "Me? No one grand — just {npc:description}. And you?",
        new PlayerOption("give_name", "give your own name",
            "Well met. I'm {you:name}.", NamedParting));

    // ── Entry ───────────────────────────────────────────────────────────────────

    private static readonly NpcLineNode Greeting = new(
        nodeId:  "greeting",
        replica: "Well met. I don't believe we've crossed paths before.",
        new PlayerOption("greet_warmly", "greet them warmly",
            "Good day to you! Well met, friend.", WarmParting),
        new PlayerOption("ask_who", "ask who they are",
            "And who might you be?", WhoAmI),
        new PlayerOption("introduce", "introduce yourself first",
            "Let me introduce myself — I'm {you:name}.", IntroParting));

    public override NpcLineNode EntryNode => Greeting;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => npc.AffinityTable.IsStranger(partyMemberId);
}
