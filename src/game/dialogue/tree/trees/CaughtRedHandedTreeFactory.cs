using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Builds a "caught red-handed" dialogue tree at runtime, parameterized by:
/// - <paramref name="criminalType"/> — the crime the party member was witnessed committing
/// - <paramref name="witnessIsBrave"/> — whether the witness will escalate to a fight on failure
///
/// Tree shape: the witness confronts the player, who may apologise, lie, shift the blame, or
/// provoke. Apologising runs deepest — it is the only route where the witness can actually be
/// talked round — and provoking is a dead end by design:
///   apologise → success: forgiven (crime cleared) / failure: rejected (recorded; fight if brave)
///   lie       → success: believed (no record)     / failure: caught out (recorded + fight)
///   deflect   → success: doubt takes hold         / failure: they are certain (recorded + fight)
///   provoke   → every end is a forced failure: the witness draws
///
/// <para>
/// Both parameters reach into the text, not just the outcomes: the crime decides what the witness
/// accuses you of at every level of the tree, and a brave witness's refusals threaten violence where
/// a timid one's merely dismiss you. Difficulty follows the <see cref="BranchDifficulty.Hard"/>
/// ladder throughout — talking your way out of a crime should never be small talk.
/// </para>
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

        public override IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; }
        public override IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; }

        private readonly CriminalAffinityType _criminalType;

        internal CaughtRedHandedTree(CriminalAffinityType criminalType, bool witnessIsBrave)
        {
            _criminalType = criminalType;

            // Two shared outcome sets. Apologise, lie and deflect are three routes to the same
            // success; a rejected apology, a caught-out lie, a disbelieved deflection and any
            // provocation all land on the same failure — which draws steel only against a brave
            // witness.
            SuccessOutcomes = new IDialogueOutcome[]
            {
                new ClearCrimeOutcome(),
                new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance),
            };
            FailureOutcomes = witnessIsBrave
                ? new IDialogueOutcome[]
                  {
                      new CriminalAffinityOutcome(criminalType),
                      new FightRequestOutcome(),
                  }
                : new IDialogueOutcome[]
                  {
                      new CriminalAffinityOutcome(criminalType),
                      new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance),
                  };

            // ── Local authoring helpers ─────────────────────────────────────
            //
            // Everything below is built with locals rather than fields: the tree depends on two
            // constructor arguments, so it cannot be static, and a local graph reads in the order
            // the conversation actually runs.

            // A branch end on the hard ladder. `depth` is how many player replies reached it.
            ResolutionNode End(string id, int depth, string success, string failure) => new(
                nodeId:         id,
                difficulty:     BranchDifficulty.Hard(depth),
                successReplica: success,
                failureReplica: failure);

            // A branch end that cannot be won — the provoke route. The success line is never spoken.
            ResolutionNode Doomed(string id, string failure) => new(
                nodeId:         id,
                difficulty:     1,
                successReplica: failure,
                failureReplica: failure,
                mode:           ResolutionMode.ForceFailure);

            // A brave witness answers a failure with violence; a timid one just turns you out.
            string Fail(string timid, string brave) => witnessIsBrave ? brave : timid;

            string act = CrimeAct(criminalType);   // "taking that", "being in here", "what you did"

            // ══════════════════════════════════════════════════════════════
            //  A — apologise (deepest: the only route that really opens)
            // ══════════════════════════════════════════════════════════════

            var apologyResult = End("apology_result", 2,
                "...Fine. See that it never happens again. Now get out of my sight.",
                Fail("Spare me your words. Get out, and don't let me see you again.",
                     "Sorry means nothing to me now. You'll answer for this — here and now!"));

            var apologyOwnItResult = End("apology_own_it", 3,
                "...You said it out loud without me dragging it out of you. That counts for something. Go on — get gone.",
                Fail("Saying it doesn't undo it. Out.",
                     "You admit it to my face and expect mercy? Stand and answer for it!"));

            var apologyRestoreResult = End("apology_restore", 3,
                "...Put it back and we'll pretend I was looking the other way. Move.",
                Fail("Too late for putting anything back. Out with you.",
                     "You'd hand it over now, would you? No. Now it's blood or nothing!"));

            var apologyNoExcuseResult = End("apology_no_excuse", 3,
                "...No excuse offered at all. That's the first honest thing anyone's said to me in this yard. Go.",
                Fail("No excuse and no pardon. Get out.",
                     "Honest and still guilty. Honesty won't save you now — face me!"));

            var apologyNothingLeftResult = End("apology_nothing_left", 4,
                "...God help me. I've been at the end of things myself. Go on, before I change my mind — and don't come back to this door.",
                Fail("Everyone's at the end of something. It doesn't make my losses smaller. Out.",
                     "Don't you dare make me the villain in this. Draw!"));

            var apologyNoDefenceResult = End("apology_no_defence", 4,
                "...You'll not even argue your own corner. That's either shame or honesty, and I'm tired enough to call it honesty. Go.",
                Fail("Then there's nothing to weigh in your favour, is there. Out.",
                     "You'll not defend yourself with words? Then defend yourself properly!"));

            // The witness is wavering — the one place in this tree where a fourth reply exists.
            var apologyWeighing = new NpcLineNode(
                nodeId:  "apology_weighing",
                replica: "...Need. Everyone in this place needs. I need, and I don't go about " +
                         $"{ActPhrase(criminalType)}. Give me one reason that isn't need.",
                new PlayerOption("apology_nothing_left", "say there wasn't one — that you had run out",
                    "I haven't got one. I'd run out of everything, and this is what was left.",
                    apologyNothingLeftResult),
                new PlayerOption("apology_no_defence", "decline to defend yourself at all",
                    "Then I've nothing to give you. Do what you think is right.",
                    apologyNoDefenceResult));

            var apologyWhy = new NpcLineNode(
                nodeId:  "apology_why",
                replica: $"Then tell me why. And don't give me a story — I'll know. Why were you {ActPhrase(criminalType)}?",
                new PlayerOption("apology_need", "admit you needed it",
                    "Because I needed it, and I'd run out of better ideas.", apologyWeighing),
                new PlayerOption("apology_no_excuse", "offer no excuse at all",
                    "There's no reason worth your hearing. I did it. That's all there is.", apologyNoExcuseResult));

            var apologyOwn = new NpcLineNode(
                nodeId:  "apology_own",
                replica: "Explain yourself, then. And be careful — I saw it with my own eyes, so explaining is not the same as denying.",
                new PlayerOption("apology_own_it", "own it flatly, without softening",
                    $"I'll not deny it. You saw me {act}. It was mine to choose and I chose wrong.", apologyOwnItResult),
                new PlayerOption("apology_restore", "offer to put it right this instant",
                    "Then let me put it right now, in front of you, before another word.", apologyRestoreResult),
                new PlayerOption("apology_why", "ask them to hear why before they decide",
                    "Hear why first. Then decide what to do with me.", apologyWhy));

            // ══════════════════════════════════════════════════════════════
            //  B — lie your way out (rich)
            // ══════════════════════════════════════════════════════════════

            var lieResult = End("lie_result", 2,
                "...Hm. Maybe I saw it wrong. Go on, then — off with you.",
                Fail("You're lying to my face. Get out before I fetch someone who cares more than I do.",
                     "You're lying to my face! Now you'll truly regret it."));

            var lieDetailResult = End("lie_detail", 3,
                "...You've an answer for it, and it hangs together. Alright. I'll say no more.",
                Fail("Too neat. Nobody has an answer that quick unless they've prepared one. Out.",
                     "That story's too well made. You had it ready! Face me, then!"));

            var lieConfidentResult = End("lie_confident", 3,
                "...You're either honest or the coolest liar I've met. I'll take the first and be done. Go.",
                Fail("Cool as you like. I know what I saw. Get out of my sight.",
                     "You stand there brazen as brass. Then stand and fight!"));

            var lieMistakenResult = End("lie_mistaken", 3,
                "...It was dim, I'll grant you. Perhaps I misread it. Go on.",
                Fail("I know my own eyes. Out.",
                     "Don't you tell me what I saw! Draw!"));

            var lieSomeoneElseResult = End("lie_someone_else", 3,
                "...Someone else, then. Someone else it is. I'll be watching either way.",
                Fail("There was nobody else. There's never anybody else. Out.",
                     "You'd hang a stranger to save yourself? Then you'll answer to me!"));

            var lieDetails = new NpcLineNode(
                nodeId:  "lie_details",
                replica: "Go on, then. Tell me what I really saw, if it wasn't what it looked like.",
                new PlayerOption("lie_detail", "give a detailed, plausible account",
                    "Here's the whole of it, start to finish, and it accounts for everything you saw.", lieDetailResult),
                new PlayerOption("lie_confident", "hold their eye and give a short flat answer",
                    "It wasn't what you think. That's all, and it's the truth.", lieConfidentResult));

            var lieBlameSight = new NpcLineNode(
                nodeId:  "lie_blame_sight",
                replica: "You're telling me my own eyes lied. Careful, now. I've been looking at this place my whole life.",
                new PlayerOption("lie_mistaken", "insist the light was against them",
                    "In that light, at that distance, anyone would have read it wrong.", lieMistakenResult),
                new PlayerOption("lie_someone_else", "suggest they saw somebody else",
                    "Someone was here. It wasn't me. Look properly — do I match what you saw?", lieSomeoneElseResult));

            var lieOpening = new NpcLineNode(
                nodeId:  "lie_opening",
                replica: $"Not what it looked like. It looked exactly like {act}, which is what it was. But go on — I'll hear it.",
                new PlayerOption("lie_details", "commit to the story and fill it in",
                    "Then let me lay it out properly, because you've got the wrong end of it.", lieDetails),
                new PlayerOption("lie_blame_sight", "cast doubt on what they actually saw",
                    "How close were you, exactly? And in this light?", lieBlameSight),
                new PlayerOption("lie_result", "keep it short and let it stand",
                    "You've got it wrong — it isn't what it looked like. That's all I'll say.", lieResult));

            // ══════════════════════════════════════════════════════════════
            //  C — shift the blame (short)
            // ══════════════════════════════════════════════════════════════

            var deflectResult = End("deflect_result", 2,
                "...Hm. There's been others about, that's true enough. Go on, then. But I'm watching.",
                Fail("Convenient. There's always someone else. Get out.",
                     "You'd point the finger anywhere but at yourself. Then point it at your blade — face me!"));

            var deflectSentResult = End("deflect_sent", 3,
                "...Sent, were you. Then it's whoever sent you I want, not you. Get out and think about who you work for.",
                Fail("Sent or not, it was your hands. Out.",
                     "Then I'll start with the one standing in front of me. Draw!"));

            var deflectPermissionResult = End("deflect_permission", 3,
                "...You thought you had leave to. That's a fool's mistake, not a thief's. Go, and ask next time.",
                Fail("Nobody gave you leave, and you knew it. Out.",
                     "Don't insult me with that. You'll answer for it properly!"));

            var deflectExplain = new NpcLineNode(
                nodeId:  "deflect_explain",
                replica: "Not your doing. Then whose? Say a name or stop wasting my daylight.",
                new PlayerOption("deflect_sent", "say you were sent by someone else",
                    "I was sent. I'll not name them here, but it wasn't my idea and it wasn't my gain.", deflectSentResult),
                new PlayerOption("deflect_permission", "claim you thought you had leave",
                    "I was told I had leave to. If I was lied to, that's on whoever told me.", deflectPermissionResult));

            var deflectOpening = new NpcLineNode(
                nodeId:  "deflect_opening",
                replica: "Someone else's doing. Aye, that's always how it starts. Well — I'm listening, and it had better be good.",
                new PlayerOption("deflect_result", "keep it vague and let the doubt do the work",
                    "There've been others about this place. I'd look wider than me, if I were you.", deflectResult),
                new PlayerOption("deflect_explain", "give them somebody to be angry at instead",
                    "It wasn't my doing, and I can tell you enough to prove it.", deflectExplain),
                new PlayerOption("deflect_admit_anyway", "give it up and admit it after all",
                    "...No. That's a lie and you'd see through it. It was me.",
                    End("deflect_admit", 2,
                        "...You caught yourself out before I had to. That's worth something. Go on — get gone.",
                        Fail("Aye, it was. Out with you.",
                             "You admit it and expect that to save you? Stand and answer!"))));

            // ══════════════════════════════════════════════════════════════
            //  D — provoke them (every end forced to failure)
            // ══════════════════════════════════════════════════════════════

            var provokeResult = Doomed("provoke_result",
                "You dare mock me? Then face me, coward!");

            var provokeThreatResult = Doomed("provoke_threat",
                "A threat, in my own place, to my own face. That's the last word you'll get in!");

            var provokeSneerResult = Doomed("provoke_sneer",
                "Look at you sneering. I've had enough of that from your sort — and I'll have no more!");

            var provokeDareResult = Doomed("provoke_dare",
                "You want it settled? Then it's settled. Now!");

            var provokeMockResult = Doomed("provoke_mock",
                "Laugh, then. Laugh while you can!");

            var provokeEscalate = new NpcLineNode(
                nodeId:  "provoke_escalate",
                replica: "...You're enjoying this. God above, you're actually enjoying it.",
                new PlayerOption("provoke_dare", "dare them to do something about it",
                    "Then do something about it, instead of standing there talking.", provokeDareResult),
                new PlayerOption("provoke_mock", "laugh in their face",
                    "You've been shouting for a while now. Was there a point coming?", provokeMockResult));

            var provokeOpening = new NpcLineNode(
                nodeId:  "provoke_opening",
                replica: "What do I mean to do about it? You'll find out, and sooner than you'd like.",
                new PlayerOption("provoke_threat", "threaten them outright",
                    "Try it and see how it goes for you.", provokeThreatResult),
                new PlayerOption("provoke_sneer", "look them up and down and dismiss them",
                    "You? I don't think you'll do anything at all.", provokeSneerResult),
                new PlayerOption("provoke_escalate", "keep pushing",
                    "Go on, then. I'll wait right here.", provokeEscalate),
                new PlayerOption("provoke_result", "leave the taunt where it stands",
                    "Nothing, is my guess.", provokeResult));

            // ══════════════════════════════════════════════════════════════
            //  Entry: the witness confronts the player
            // ══════════════════════════════════════════════════════════════

            _entry = new NpcLineNode(
                nodeId:  "confrontation",
                replica: BuildConfrontationReplica(criminalType),
                new PlayerOption("apologize", "apologise and explain yourself",
                    "I'm sorry — please, let me explain myself.", apologyOwn),
                new PlayerOption("lie", "talk your way out with a story",
                    "You've got it wrong — it isn't what it looked like.", lieOpening),
                new PlayerOption("deflect", "put the blame on somebody else",
                    "Whatever you saw, it wasn't my doing.", deflectOpening),
                new PlayerOption("provoke", "provoke them into a fight",
                    "And what exactly do you mean to do about it?", provokeOpening));
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

        /// <summary>The deed as a bare gerund phrase — "taking that", "coming in here".</summary>
        private static string CrimeAct(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "taking that",
            CriminalAffinityType.Intruder => "coming in here",
            CriminalAffinityType.Murderer => "raising your hand to somebody",
            _                             => "what you did",
        };

        /// <summary>The same deed phrased for "why were you …?".</summary>
        private static string ActPhrase(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "helping yourself to what isn't yours",
            CriminalAffinityType.Intruder => "in a place you'd no business being",
            CriminalAffinityType.Murderer => "putting hands on somebody",
            _                             => "doing what you did",
        };
    }

    // ── Inner outcome: clear crime ────────────────────────────────────────────

    private sealed class ClearCrimeOutcome : IDialogueOutcome
    {
        public string Description => "crime record cleared (apology accepted)";

        public Cathedral.Game.Narrative.OutcomeReport? Apply(NpcEntity npc, string partyMemberId)
        {
            npc.AffinityTable.ClearCrime(partyMemberId);
            return DialogueOutcomeReports.Relation(
                $"{npc.DisplayName} lets the matter drop",
                Cathedral.Game.Narrative.OutcomeReportSeverity.Positive);
        }
    }
}
