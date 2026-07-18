using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Meet Stranger" — available only before any dialogue has occurred (Stranger affinity).
/// Tree structure:
///   greeting → first_self_presenting / first_ask_who_he_is
///   first_ask_who_he_is → salutation / then_self_presenting
///   first_self_presenting → salutation / then_ask_who_he_is
///   then_self_presenting → salutation
///   then_ask_who_he_is → salutation
///   salutation (terminal):
///     Success → DistantAcquaintance
///     Failure → AnnoyingAcquaintance
/// </summary>
public class MeetStrangerTree : DialogueTree
{
    public override string TreeId          => "meet_stranger";
    public override string DisplayName     => "Meet Stranger";
    public override string Description     => "meeting this person for the first time and exchanging introductions";
    public override string AssociatedVerbId => "meet_stranger";

    // ── Terminal node ─────────────────────────────────────────────────────────

    private static readonly DialogueTreeNode Salutation = new(
        nodeId:      "salutation",
        description: "exchanging farewells and parting words after the introductions",
        replica:     "Well then, I'll be on my way. Good day to you.",
        outcomes: new List<DialogueOutcomeCase>
        {
            new(new AffinityTransitionOutcome(AffinityLevel.DistantAcquaintance),  BranchCondition.Success),
            new(new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance), BranchCondition.Failure),
        });

    // ── Intermediate nodes ────────────────────────────────────────────────────

    private static readonly DialogueTreeNode ThenSelfPresenting = new(
        nodeId:      "then_self_presenting",
        description: "presenting yourself after the other person has spoken",
        replica:     "And I should tell you who I am.",
        branches: new List<DialogueBranch>
        {
            new(Salutation, BranchCondition.Either),
        });

    private static readonly DialogueTreeNode ThenAskWhoHeIs = new(
        nodeId:      "then_ask_who_he_is",
        description: "asking who the other person is after they have introduced themselves",
        replica:     "And who are you, then?",
        branches: new List<DialogueBranch>
        {
            new(Salutation, BranchCondition.Either),
        });

    private static readonly DialogueTreeNode FirstAskWhoHeIs = new(
        nodeId:      "first_ask_who_he_is",
        description: "asking who the stranger is before saying anything about yourself",
        replica:     "And who might you be?",
        branches: new List<DialogueBranch>
        {
            new(Salutation,          BranchCondition.Either),
            new(ThenSelfPresenting,  BranchCondition.Either),
        });

    private static readonly DialogueTreeNode FirstSelfPresenting = new(
        nodeId:      "first_self_presenting",
        description: "introducing yourself before asking who the stranger is",
        replica:     "Let me tell you who I am.",
        branches: new List<DialogueBranch>
        {
            new(Salutation,       BranchCondition.Either),
            new(ThenAskWhoHeIs,   BranchCondition.Either),
        });

    private static readonly DialogueTreeNode Greeting = new(
        nodeId:      "greeting",
        description: "opening the conversation with an unknown person",
        replica:     "Hello there, stranger.",
        isEntry:     true,
        branches: new List<DialogueBranch>
        {
            new(FirstSelfPresenting, BranchCondition.Either),
            new(FirstAskWhoHeIs,     BranchCondition.Either),
        });

    public override DialogueTreeNode EntryNode => Greeting;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => npc.AffinityTable.IsStranger(partyMemberId);
}
