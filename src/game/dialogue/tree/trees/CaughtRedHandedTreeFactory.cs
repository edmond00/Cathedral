using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Builds a "caught red-handed" dialogue tree at runtime, parameterized by:
/// - <paramref name="criminalType"/> — the crime the party member was witnessed committing
/// - <paramref name="witnessIsBrave"/> — whether the witness will escalate to a fight on failure
///
/// Tree structure:
///   confrontation (entry)
///   ├─ apologize
///   │   ├─ Success → forgiven         (ClearCrime + set AnnoyingAcquaintance)
///   │   └─ Failure → rejected         (RecordCrime [+ FightRequest if brave])
///   ├─ lie
///   │   ├─ Success → believed         (no record — you got away with it)
///   │   └─ Failure → caught_lying     (RecordCrime + FightRequest)
///   └─ provoke
///       └─ Either  → fight_demanded   (RecordCrime + FightRequest)
/// </summary>
public static class CaughtRedHandedTreeFactory
{
    public const string TreeIdPrefix = "caught_red_handed";

    /// <summary>
    /// Creates a unique caught-red-handed tree for the given crime type and witness bravery.
    /// The returned tree is NOT registered in <see cref="DialogueTreeRegistry"/> — it is used
    /// directly by the game controller when witness confrontation is triggered.
    /// </summary>
    public static DialogueTree Create(CriminalAffinityType criminalType, bool witnessIsBrave)
        => new CaughtRedHandedTree(criminalType, witnessIsBrave);

    // ── Private concrete tree ─────────────────────────────────────────────────

    private sealed class CaughtRedHandedTree : DialogueTree
    {
        private readonly DialogueTreeNode _entry;

        public override string TreeId          => $"{TreeIdPrefix}_{_criminalType.ToString().ToLowerInvariant()}";
        public override string DisplayName     => "Caught Red-Handed";
        public override string Description     => BuildDescription();
        public override string AssociatedVerbId => "";   // triggered programmatically, not by a verb
        public override DialogueTreeNode EntryNode => _entry;

        private readonly CriminalAffinityType _criminalType;

        internal CaughtRedHandedTree(CriminalAffinityType criminalType, bool witnessIsBrave)
        {
            _criminalType = criminalType;

            // ── Terminal nodes ──────────────────────────────────────────────────

            var forgiven = new DialogueTreeNode(
                nodeId:      "forgiven",
                description: "the witness accepts your apology and lets you go with a stern warning",
                outcomes: new List<DialogueOutcomeCase>
                {
                    new(new ClearCrimeOutcome(),                                              BranchCondition.Either),
                    new(new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance),   BranchCondition.Either),
                });

            List<DialogueOutcomeCase> rejectedOutcomes = [
                new(new CriminalAffinityOutcome(criminalType), BranchCondition.Either),
            ];
            if (witnessIsBrave)
                rejectedOutcomes.Add(new(new FightRequestOutcome(), BranchCondition.Either));
            else
                rejectedOutcomes.Add(new(new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance), BranchCondition.Either));

            var rejected = new DialogueTreeNode(
                nodeId:      "rejected",
                description: witnessIsBrave
                    ? "the witness rejects your apology and demands you answer for your actions"
                    : "the witness rejects your apology but lets you go with cold contempt",
                outcomes: rejectedOutcomes);

            var believed = new DialogueTreeNode(
                nodeId:      "believed",
                description: "the witness believes your story and you escape without consequence",
                outcomes: new List<DialogueOutcomeCase>
                {
                    // No record — the lie worked.
                });

            var caughtLying = new DialogueTreeNode(
                nodeId:      "caught_lying",
                description: "the witness sees through your lie and is now doubly enraged",
                outcomes: new List<DialogueOutcomeCase>
                {
                    new(new CriminalAffinityOutcome(criminalType), BranchCondition.Either),
                    new(new FightRequestOutcome(),                  BranchCondition.Either),
                });

            var fightDemanded = new DialogueTreeNode(
                nodeId:      "fight_demanded",
                description: "your provocations push the witness over the edge — they draw their weapon",
                outcomes: new List<DialogueOutcomeCase>
                {
                    new(new CriminalAffinityOutcome(criminalType), BranchCondition.Either),
                    new(new FightRequestOutcome(),                  BranchCondition.Either),
                });

            // ── Intermediate nodes ──────────────────────────────────────────────

            var apologize = new DialogueTreeNode(
                nodeId:      "apologize",
                description: "attempting to defuse the situation by apologising and explaining yourself",
                branches: new List<DialogueBranch>
                {
                    new(forgiven,  BranchCondition.Success),
                    new(rejected,  BranchCondition.Failure),
                });

            var lie = new DialogueTreeNode(
                nodeId:      "lie",
                description: "trying to talk your way out by spinning a plausible story",
                branches: new List<DialogueBranch>
                {
                    new(believed,     BranchCondition.Success),
                    new(caughtLying,  BranchCondition.Failure),
                });

            var provoke = new DialogueTreeNode(
                nodeId:      "provoke",
                description: "deliberately aggravating the witness to force a confrontation on your own terms",
                branches: new List<DialogueBranch>
                {
                    new(fightDemanded, BranchCondition.Either),
                });

            // ── Entry ───────────────────────────────────────────────────────────

            _entry = new DialogueTreeNode(
                nodeId:      "confrontation",
                description: BuildConfrontationDescription(criminalType),
                isEntry:     true,
                branches: new List<DialogueBranch>
                {
                    new(apologize, BranchCondition.Either),
                    new(lie,       BranchCondition.Either),
                    new(provoke,   BranchCondition.Either),
                });
        }

        // The tree is triggered programmatically — IsAvailable is never checked.
        public override bool IsAvailable(NpcEntity npc, string partyMemberId) => false;

        private string BuildDescription() => _criminalType switch
        {
            CriminalAffinityType.Thief    => "being caught stealing by a witness",
            CriminalAffinityType.Intruder => "being caught trespassing by a witness",
            CriminalAffinityType.Murderer => "being caught committing violence by a witness",
            _                             => "being caught in an illegal act by a witness",
        };

        private static string BuildConfrontationDescription(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "the witness confronts you about the theft they just witnessed",
            CriminalAffinityType.Intruder => "the witness demands to know what you are doing in a restricted area",
            CriminalAffinityType.Murderer => "the witness confronts you in horror over what they just saw you do",
            _                             => "the witness confronts you about what they just witnessed",
        };
    }

    // ── Inner outcome: clear crime ────────────────────────────────────────────

    private sealed class ClearCrimeOutcome : IDialogueOutcome
    {
        public string Description => "crime record cleared (apology accepted)";

        public void Apply(NpcEntity npc, string partyMemberId)
            => npc.AffinityTable.ClearCrime(partyMemberId);
    }
}
