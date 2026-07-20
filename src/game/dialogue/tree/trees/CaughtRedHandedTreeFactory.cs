using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Builds a "caught red-handed" dialogue tree at runtime, parameterized by:
/// - <paramref name="criminalType"/> — the crime the party member was witnessed committing
/// - <paramref name="witnessIsBrave"/> — whether the witness will escalate to a fight on failure
///
/// Tree shape (new model): the witness confronts the player, who may apologise, lie, or provoke.
/// Each reply leads to its own single-check resolution:
///   apologise → success: forgiven (crime cleared) / failure: rejected (recorded; fight if brave)
///   lie       → success: believed (no record)     / failure: caught out (recorded + fight)
///   provoke   → either: the witness draws (recorded + fight)
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
        private readonly NpcLineNode _entry;

        public override string TreeId           => $"{TreeIdPrefix}_{_criminalType.ToString().ToLowerInvariant()}";
        public override string DisplayName      => "Caught Red-Handed";
        public override string Description      => BuildDescription();
        public override string AssociatedVerbId => "";   // triggered programmatically, not by a verb
        public override NpcLineNode EntryNode   => _entry;

        private readonly CriminalAffinityType _criminalType;

        internal CaughtRedHandedTree(CriminalAffinityType criminalType, bool witnessIsBrave)
        {
            _criminalType = criminalType;

            // ── Apologise → forgiven / rejected ─────────────────────────────────
            var rejectedOutcomes = new List<DialogueOutcomeCase>
            {
                new(new CriminalAffinityOutcome(criminalType), BranchCondition.Failure),
            };
            rejectedOutcomes.Add(witnessIsBrave
                ? new(new FightRequestOutcome(), BranchCondition.Failure)
                : new(new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance), BranchCondition.Failure));

            var apologyOutcomes = new List<DialogueOutcomeCase>
            {
                new(new ClearCrimeOutcome(),                                           BranchCondition.Success),
                new(new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance), BranchCondition.Success),
            };
            apologyOutcomes.AddRange(rejectedOutcomes);

            var apologyResult = new ResolutionNode(
                nodeId:         "apology_result",
                difficulty:     2,
                successReplica: "...Fine. See that it never happens again. Now get out of my sight.",
                failureReplica: witnessIsBrave
                    ? "Sorry means nothing to me now. You'll answer for this — here and now!"
                    : "Spare me your words. Get out, and don't let me see you again.",
                outcomes: apologyOutcomes);

            // ── Lie → believed / caught out ─────────────────────────────────────
            var lieResult = new ResolutionNode(
                nodeId:         "lie_result",
                difficulty:     2,
                successReplica: "...Hm. Maybe I saw it wrong. Go on, then — off with you.",
                failureReplica: "You're lying to my face! Now you'll truly regret it.",
                outcomes: new List<DialogueOutcomeCase>
                {
                    // Success: the lie worked, no record.
                    new(new CriminalAffinityOutcome(criminalType), BranchCondition.Failure),
                    new(new FightRequestOutcome(),                  BranchCondition.Failure),
                });

            // ── Provoke → the witness draws (either result) ─────────────────────
            var provokeResult = new ResolutionNode(
                nodeId:         "provoke_result",
                difficulty:     1,
                successReplica: "That's the last insult I'll take from you. Draw!",
                failureReplica: "You dare mock me? Then face me, coward!",
                outcomes: new List<DialogueOutcomeCase>
                {
                    new(new CriminalAffinityOutcome(criminalType), BranchCondition.Either),
                    new(new FightRequestOutcome(),                  BranchCondition.Either),
                });

            // ── Entry: the witness confronts the player ─────────────────────────
            _entry = new NpcLineNode(
                nodeId:  "confrontation",
                replica: BuildConfrontationReplica(criminalType),
                new PlayerOption("apologize", "apologise and explain yourself",
                    "I'm sorry — please, let me explain myself.", apologyResult),
                new PlayerOption("lie", "talk your way out with a story",
                    "You've got it wrong — it isn't what it looked like.", lieResult),
                new PlayerOption("provoke", "provoke them into a fight",
                    "And what exactly do you mean to do about it?", provokeResult));
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

        private static string BuildConfrontationReplica(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "Stop right there — I saw you take that!",
            CriminalAffinityType.Intruder => "Hold! What are you doing here? You've no business in this place.",
            CriminalAffinityType.Murderer => "God above — I saw what you just did!",
            _                             => "Hold it right there. I saw what you did.",
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
