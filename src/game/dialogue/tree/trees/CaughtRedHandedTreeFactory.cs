using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Builds a "caught red-handed" dialogue tree at runtime, parameterized by the crime the party
/// member was witnessed committing.
///
/// Tree shape: the witness confronts the player, who may apologise, lie, shift the blame, or
/// provoke. Apologising runs deepest — it is the only route where the witness can actually be
/// talked round — and provoking is a dead end by design:
///   apologise → success: the matter is dropped / failure: rejected, and they draw
///   lie       → success: believed              / failure: caught out, and they draw
///   deflect   → success: doubt takes hold      / failure: they are certain, and they draw
///   provoke   → every end is a forced failure: the witness draws
///
/// <para>
/// <b>Failure always means a fight.</b> The witness's nerve used to be a per-archetype flag, and a
/// timid one's failure outcome was an affinity write identical to the success outcome's — so against
/// roughly a third of the people in the game, losing this conversation after botching a burglary cost
/// exactly nothing, silently and with no way for a player to tell which kind of witness they had.
/// The consequence is now the same for everybody, and it lasts: an enemy stays an enemy across
/// visits, so walking out of the fight leaves somebody in that location who is still waiting.
/// </para>
///
/// <para>
/// The crime reaches into the text, not just the outcome: it decides what the witness accuses you of
/// at every level of the tree. Difficulty follows the <see cref="BranchDifficulty.Hard"/> ladder
/// throughout — talking your way out of a crime should never be small talk.
/// </para>
///
/// <para>
/// Every replica is the spoken line, plainly: a refusal says the matter will be settled by fighting,
/// and leaves the shouting to the persona. See "Authoring the neutral text" on
/// <see cref="DialogueTree"/>.
/// </para>
/// </summary>
public static class CaughtRedHandedTreeFactory
{
    public const string TreeIdPrefix = "caught_red_handed";

    /// <summary>
    /// Creates a unique caught-red-handed tree for the given crime type.
    /// The returned tree is NOT registered in <see cref="DialogueTreeRegistry"/> — it is used
    /// directly by the game controller when witness confrontation is triggered.
    /// </summary>
    public static DialogueTree Create(CriminalAffinityType criminalType)
        => new CaughtRedHandedTree(criminalType);

    // ── Private concrete tree ─────────────────────────────────────────────────

    private sealed class CaughtRedHandedTree : DialogueTree
    {
        private readonly NpcLineNode _entry;

        public override string TreeId           => $"{TreeIdPrefix}_{_criminalType.ToString().ToLowerInvariant()}";
        public override string DisplayName      => "Caught Red-Handed";
        public override string Description      => BuildDescription();

        /// <summary>
        /// The other chair, and the one that inverts hardest: the NPC is the <b>witness</b>, not the
        /// culprit. Handed the player's side, the confronting villager was told they had just been
        /// caught stealing.
        /// </summary>
        public override string NpcDescription   => BuildNpcDescription();
        public override string AssociatedVerbId => "";   // triggered programmatically, not by a verb
        public override NpcLineNode EntryNode   => _entry;

        public override IReadOnlyList<Outcome> SuccessOutcomes { get; }
        public override IReadOnlyList<Outcome> FailureOutcomes { get; }

        private readonly CriminalAffinityType _criminalType;

        internal CaughtRedHandedTree(CriminalAffinityType criminalType)
        {
            _criminalType = criminalType;

            // Two shared outcome sets. Apologise, lie and deflect are three routes to the same
            // success; a rejected apology, a caught-out lie, a disbelieved deflection and any
            // provocation all land on the same failure, and it draws steel every time.
            SuccessOutcomes = new Outcome[]
            {
                new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance),
            };
            FailureOutcomes = new Outcome[]
            {
                new FightRequestOutcome(),
            };

            // ── Local authoring helpers ─────────────────────────────────────
            //
            // Everything below is built with locals rather than fields: the tree depends on a
            // constructor argument, so it cannot be static, and a local graph reads in the order
            // the conversation actually runs.

            // A branch end on the hard ladder. `depth` is how many player replies reached it.
            ResolutionNode End(string id, int depth,
                              string success, string successIndirect,
                              string failure, string failureIndirect) => new(
                nodeId:                 id,
                difficulty:             BranchDifficulty.Hard(depth),
                successReplica:         success,
                successReplicaIndirect: successIndirect,
                failureReplica:         failure,
                failureReplicaIndirect: failureIndirect);

            // A branch end that cannot be won — the provoke route. The success line is never spoken.
            ResolutionNode Doomed(string id, string failure, string failureIndirect) => new(
                nodeId:                 id,
                difficulty:             1,
                successReplica:         failure,
                successReplicaIndirect: failureIndirect,
                failureReplica:         failure,
                failureReplicaIndirect: failureIndirect,
                mode:                   ResolutionMode.ForceFailure);

            string act      = CrimeAct(criminalType);        // "taking that", "coming in here"
            string actHeard = CrimeActHeard(criminalType);   // the same, as the accused heard it

            // ══════════════════════════════════════════════════════════════
            //  A — apologise (deepest: the only route that really opens)
            // ══════════════════════════════════════════════════════════════

            var apologyResult = End("apology_result", 2,
                "See that it never happens again. Now go.",
                "I tell {you:name} to see that it never happens again, and to go.",
                "Sorry means nothing to me now. You will answer for this here and now.",
                "I tell {you:name} sorry means nothing now, and that they will answer for this here.");

            var apologyOwnItResult = End("apology_own_it", 3,
                "You said it without my having to drag it out of you. That counts for something. Go.",
                "I tell {you:name} that saying it without my dragging it out counts for something, and let them go.",
                "You admit it to my face and expect mercy. Stand and answer for it.",
                "I tell {you:name} they admit it and expect mercy, and demand they stand and answer.");

            var apologyRestoreResult = End("apology_restore", 3,
                "Put it back and I will say I was looking elsewhere. Move.",
                "I tell {you:name} to put it back, and that I will claim I was looking elsewhere.",
                "You would hand it over now. No. It is settled by force or not at all.",
                "I tell {you:name} it is too late, and that this is settled by force or not at all.");

            var apologyNoExcuseResult = End("apology_no_excuse", 3,
                "No excuse offered at all. That is the first honest thing anyone has said to me here. Go.",
                "I tell {you:name} that no excuse at all is the first honest thing anyone has said to me here.",
                "Honest and still guilty. Honesty will not save you. Face me.",
                "I tell {you:name} honesty will not save them, and to face me.");

            var apologyNothingLeftResult = End("apology_nothing_left", 4,
                "I have been at the end of things myself. Go, before I change my mind, and do not come back to this door.",
                "I tell {you:name} I have been at the end of things myself, and to go before I change my mind.",
                "Do not make me the villain in this. Draw.",
                "I tell {you:name} not to make me the villain, and to draw.");

            var apologyNoDefenceResult = End("apology_no_defence", 4,
                "You will not even argue your own case. That is either shame or honesty, and I am tired enough to call it honesty. Go.",
                "I tell {you:name} that refusing to argue their case is either shame or honesty, and that I will call it honesty.",
                "You will not defend yourself with words. Then defend yourself properly.",
                "I tell {you:name} that if they will not defend themselves with words they may do it properly.");

            // The witness is wavering — the one place in this tree where a fourth reply exists.
            var apologyWeighing = new NpcLineNode(
                nodeId:          "apology_weighing",
                replica:         "Everyone here is in need. I am in need, and I do not go about "
                               + $"{ActPhrase(criminalType)}. Give me a reason that is not need.",
                replicaIndirect: "I tell {you:name} that I am in need too and do not go about "
                               + $"{ActPhrase(criminalType)}, and demand a reason that is not need.",
                replicaHeard:    "{npc:name} says they are in need too and do not do what I did, and demands a reason that is not need.",
                new PlayerOption("apology_nothing_left", "say there wasn't one — that you had run out",
                    "I have none. I had run out of everything, and this is what was left.",
                    "I tell {npc:name} I had run out of everything and this is what was left.",
                    apologyNothingLeftResult),
                new PlayerOption("apology_no_defence", "decline to defend yourself at all",
                    "Then I have nothing to give you. Do what you think is right.",
                    "I tell {npc:name} I have nothing to give them, and to do what they think is right.",
                    apologyNoDefenceResult));

            var apologyWhy = new NpcLineNode(
                nodeId:          "apology_why",
                replica:         $"Then tell me why, and do not invent a story. Why were you {ActPhrase(criminalType)}?",
                replicaIndirect: $"I demand {{you:name}} tell me why they were {ActPhrase(criminalType)}, and not invent a story.",
                replicaHeard:    $"{{npc:name}} demands I tell them why I was {actHeard}, and not invent a story.",
                new PlayerOption("apology_need", "admit you needed it",
                    "Because I needed it, and I had no better idea.",
                    "I tell {npc:name} I needed it and had no better idea.",
                    apologyWeighing),
                new PlayerOption("apology_no_excuse", "offer no excuse at all",
                    "There is no reason worth your hearing. I did it. That is all.",
                    "I tell {npc:name} there is no reason worth their hearing, and that I did it.",
                    apologyNoExcuseResult));

            var apologyOwn = new NpcLineNode(
                nodeId:          "apology_own",
                replica:         "Explain yourself. I saw it with my own eyes, so explaining is not the same as denying.",
                replicaIndirect: "I order {you:name} to explain themselves, and warn them I saw it with my own eyes.",
                replicaHeard:    "{npc:name} orders me to explain myself, and warns me they saw it with their own eyes.",
                new PlayerOption("apology_own_it", "own it flatly, without softening",
                    $"I will not deny it. You saw me {act}. It was my choice and I chose wrong.",
                    $"I tell {{npc:name}} I will not deny {act}, and that I chose wrong.",
                    apologyOwnItResult),
                new PlayerOption("apology_restore", "offer to put it right this instant",
                    "Then let me put it right now, in front of you, before anything else.",
                    "I offer {npc:name} to put it right now, in front of them.",
                    apologyRestoreResult),
                new PlayerOption("apology_why", "ask them to hear why before they decide",
                    "Hear why first. Then decide what to do with me.",
                    "I ask {npc:name} to hear why before deciding what to do with me.",
                    apologyWhy));

            // ══════════════════════════════════════════════════════════════
            //  B — lie your way out (rich)
            // ══════════════════════════════════════════════════════════════

            var lieResult = End("lie_result", 2,
                "Perhaps I saw it wrong. Go on, then.",
                "I allow that perhaps I saw it wrong, and tell {you:name} to go on.",
                "You are lying to my face. Now you will answer for it.",
                "I tell {you:name} they are lying to my face, and that they will answer for it.");

            var lieDetailResult = End("lie_detail", 3,
                "You have an answer for it, and it holds together. I will say no more.",
                "I allow that {you:name} has an answer for it that holds together, and let it be.",
                "That story is too well made. You had it ready. Face me.",
                "I tell {you:name} the story is too well made, and to face me.");

            var lieConfidentResult = End("lie_confident", 3,
                "You are either honest or the coolest liar I have met. I will take the first. Go.",
                "I tell {you:name} they are either honest or the coolest liar I have met, and that I will take the first.",
                "You stand there without shame. Then stand and fight.",
                "I tell {you:name} they stand there without shame, and to stand and fight.");

            var lieMistakenResult = End("lie_mistaken", 3,
                "It was dim, I will grant you that. Perhaps I misread it. Go on.",
                "I grant {you:name} that it was dim and I may have misread it.",
                "Do not tell me what I saw. Draw.",
                "I tell {you:name} not to tell me what I saw, and to draw.");

            var lieSomeoneElseResult = End("lie_someone_else", 3,
                "Someone else, then. I will be watching either way.",
                "I allow that it was someone else, and tell {you:name} I will be watching either way.",
                "You would blame a stranger to save yourself. Then answer to me.",
                "I tell {you:name} they would blame a stranger to save themselves, and to answer to me.");

            var lieDetails = new NpcLineNode(
                nodeId:          "lie_details",
                replica:         "Go on. Tell me what I really saw, if it was not what it looked like.",
                replicaIndirect: "I tell {you:name} to say what I really saw, if it was not what it looked like.",
                replicaHeard:    "{npc:name} tells me to say what they really saw, if it was not what it looked like.",
                new PlayerOption("lie_detail", "give a detailed, plausible account",
                    "Here is the whole of it, and it accounts for everything you saw.",
                    "I give {npc:name} the whole of it, accounting for everything they saw.",
                    lieDetailResult),
                new PlayerOption("lie_confident", "hold their eye and give a short flat answer",
                    "It was not what you think. That is all, and it is the truth.",
                    "I tell {npc:name} flatly it was not what they think.",
                    lieConfidentResult));

            var lieBlameSight = new NpcLineNode(
                nodeId:          "lie_blame_sight",
                replica:         "You are telling me my own eyes lied. Be careful. I have been looking at this place my whole life.",
                replicaIndirect: "I warn {you:name} to be careful in telling me my own eyes lied, since I have watched this place my whole life.",
                replicaHeard:    "{npc:name} warns me to be careful in telling them their own eyes lied, since they have watched this place their whole life.",
                new PlayerOption("lie_mistaken", "insist the light was against them",
                    "In that light, at that distance, anyone would have read it wrong.",
                    "I tell {npc:name} anyone would have read it wrong in that light.",
                    lieMistakenResult),
                new PlayerOption("lie_someone_else", "suggest they saw somebody else",
                    "Someone was here. It was not me. Look properly. Do I match what you saw?",
                    "I tell {npc:name} someone was here but not me, and ask whether I match what they saw.",
                    lieSomeoneElseResult));

            var lieOpening = new NpcLineNode(
                nodeId:          "lie_opening",
                replica:         $"It looked exactly like {act}, which is what it was. But go on, I will hear it.",
                replicaIndirect: $"I tell {{you:name}} it looked exactly like {act}, but that I will hear them out.",
                replicaHeard:    $"{{npc:name}} says it looked exactly like {actHeard}, but that they will hear me out.",
                new PlayerOption("lie_details", "commit to the story and fill it in",
                    "Then let me lay it out properly, because you have it wrong.",
                    "I offer to lay it out properly for {npc:name}, who has it wrong.",
                    lieDetails),
                new PlayerOption("lie_blame_sight", "cast doubt on what they actually saw",
                    "How close were you? And in this light?",
                    "I ask {npc:name} how close they were, and in what light.",
                    lieBlameSight),
                new PlayerOption("lie_result", "keep it short and let it stand",
                    "You have it wrong. It is not what it looked like. That is all I will say.",
                    "I tell {npc:name} it is not what it looked like, and that is all I will say.",
                    lieResult));

            // ══════════════════════════════════════════════════════════════
            //  C — shift the blame (short)
            // ══════════════════════════════════════════════════════════════

            var deflectResult = End("deflect_result", 2,
                "There have been others about, that is true enough. Go on, but I am watching.",
                "I allow that there have been others about, and tell {you:name} I am watching.",
                "You would point anywhere but at yourself. Then we will settle it with weapons.",
                "I tell {you:name} they would point anywhere but at themselves, and that we settle it with weapons.");

            var deflectSentResult = End("deflect_sent", 3,
                "Sent, were you. Then it is whoever sent you I want, not you. Go, and think about who you work for.",
                "I tell {you:name} it is whoever sent them I want, and to think about who they work for.",
                "Then I will start with the one standing in front of me. Draw.",
                "I tell {you:name} I will start with the one in front of me, and to draw.");

            var deflectPermissionResult = End("deflect_permission", 3,
                "You thought you had leave. That is a fool's mistake, not a thief's. Go, and ask next time.",
                "I tell {you:name} that is a fool's mistake and not a thief's, and to ask next time.",
                "Do not insult me with that. You will answer for it properly.",
                "I tell {you:name} not to insult me, and that they will answer for it properly.");

            var deflectExplain = new NpcLineNode(
                nodeId:          "deflect_explain",
                replica:         "Not your doing. Then whose? Give me a name or stop wasting my day.",
                replicaIndirect: "I ask {you:name} whose doing it was, and tell them to give me a name or stop wasting my day.",
                replicaHeard:    "{npc:name} asks whose doing it was, and tells me to give them a name or stop wasting their day.",
                new PlayerOption("deflect_sent", "say you were sent by someone else",
                    "I was sent. I will not name them here, but it was not my idea and not my gain.",
                    "I tell {npc:name} I was sent, that it was not my idea, and that I will not name them.",
                    deflectSentResult),
                new PlayerOption("deflect_permission", "claim you thought you had leave",
                    "I was told I had leave to. If I was lied to, that is on whoever told me.",
                    "I tell {npc:name} I was told I had leave, and that it is on whoever told me.",
                    deflectPermissionResult));

            var deflectOpening = new NpcLineNode(
                nodeId:          "deflect_opening",
                replica:         "Someone else's doing. That is always how it starts. I am listening, and it had better be good.",
                replicaIndirect: "I tell {you:name} someone else's doing is always how it starts, and that it had better be good.",
                replicaHeard:    "{npc:name} says someone else's doing is always how it starts, and that it had better be good.",
                new PlayerOption("deflect_result", "keep it vague and let the doubt do the work",
                    "There have been others about this place. I would look wider than me.",
                    "I tell {npc:name} there have been others about, and that I would look wider.",
                    deflectResult),
                new PlayerOption("deflect_explain", "give them somebody to be angry at instead",
                    "It was not my doing, and I can tell you enough to prove it.",
                    "I tell {npc:name} it was not my doing, and that I can prove it.",
                    deflectExplain),
                new PlayerOption("deflect_admit_anyway", "give it up and admit it after all",
                    "No. That is a lie and you would see through it. It was me.",
                    "I take it back and admit to {npc:name} that it was me.",
                    End("deflect_admit", 2,
                        "You caught yourself out before I had to. That is worth something. Go.",
                        "I tell {you:name} that catching themselves out before I had to is worth something.",
                        "You admit it and expect that to save you. Stand and answer.",
                        "I tell {you:name} that admitting it will not save them, and to stand and answer.")));

            // ══════════════════════════════════════════════════════════════
            //  D — provoke them (every end forced to failure)
            // ══════════════════════════════════════════════════════════════

            var provokeResult = Doomed("provoke_result",
                "You are mocking me. Then face me.",
                "I tell {you:name} they are mocking me, and to face me.");

            var provokeThreatResult = Doomed("provoke_threat",
                "A threat, in my own place, to my face. That is the last word you will get in.",
                "I tell {you:name} that a threat in my own place is the last word they will get in.");

            var provokeSneerResult = Doomed("provoke_sneer",
                "You sneer at me. I have had enough of that, and I will have no more.",
                "I tell {you:name} I have had enough of being sneered at, and will have no more.");

            var provokeDareResult = Doomed("provoke_dare",
                "You want it settled. Then it is settled, now.",
                "I tell {you:name} that if they want it settled, it is settled now.");

            var provokeMockResult = Doomed("provoke_mock",
                "Laugh, then. Laugh while you can.",
                "I tell {you:name} to laugh while they can.");

            var provokeEscalate = new NpcLineNode(
                nodeId:          "provoke_escalate",
                replica:         "You are enjoying this.",
                replicaIndirect: "I tell {you:name} they are enjoying this.",
                replicaHeard:    "{npc:name} says I am enjoying this.",
                new PlayerOption("provoke_dare", "dare them to do something about it",
                    "Then do something about it instead of standing there talking.",
                    "I dare {npc:name} to do something instead of standing there talking.",
                    provokeDareResult),
                new PlayerOption("provoke_mock", "laugh in their face",
                    "You have been shouting a while. Was there a point coming?",
                    "I ask {npc:name} whether there was a point coming to all this shouting.",
                    provokeMockResult));

            var provokeOpening = new NpcLineNode(
                nodeId:          "provoke_opening",
                replica:         "You will find out what I mean to do about it, and sooner than you would like.",
                replicaIndirect: "I tell {you:name} they will find out what I mean to do, and sooner than they would like.",
                replicaHeard:    "{npc:name} says I will find out what they mean to do, and sooner than I would like.",
                new PlayerOption("provoke_threat", "threaten them outright",
                    "Try it and see how it goes for you.",
                    "I tell {npc:name} to try it and see how it goes for them.",
                    provokeThreatResult),
                new PlayerOption("provoke_sneer", "look them up and down and dismiss them",
                    "You? I do not think you will do anything at all.",
                    "I tell {npc:name} I do not think they will do anything at all.",
                    provokeSneerResult),
                new PlayerOption("provoke_escalate", "keep pushing",
                    "Go on, then. I will wait here.",
                    "I tell {npc:name} to go on, and that I will wait here.",
                    provokeEscalate),
                new PlayerOption("provoke_result", "leave the taunt where it stands",
                    "Nothing, I expect.",
                    "I tell {npc:name} I expect they will do nothing.",
                    provokeResult));

            // ══════════════════════════════════════════════════════════════
            //  Entry: the witness confronts the player
            // ══════════════════════════════════════════════════════════════

            _entry = new NpcLineNode(
                nodeId:          "confrontation",
                replica:         BuildConfrontationReplica(criminalType),
                replicaIndirect: BuildConfrontationIndirect(criminalType),
                replicaHeard:    BuildConfrontationHeard(criminalType),
                new PlayerOption("apologize", "apologise and explain yourself",
                    "I am sorry. Let me explain myself.",
                    "I apologise to {npc:name} and ask to explain myself.",
                    apologyOwn),
                new PlayerOption("lie", "talk your way out with a story",
                    "You have it wrong. It is not what it looked like.",
                    "I tell {npc:name} they have it wrong and it is not what it looked like.",
                    lieOpening),
                new PlayerOption("deflect", "put the blame on somebody else",
                    "Whatever you saw, it was not my doing.",
                    "I tell {npc:name} that whatever they saw, it was not my doing.",
                    deflectOpening),
                new PlayerOption("provoke", "provoke them into a fight",
                    "And what do you mean to do about it?",
                    "I ask {npc:name} what they mean to do about it.",
                    provokeOpening));
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

        /// <summary>The same moment from the witness's side — you are the one who saw it.</summary>
        private string BuildNpcDescription() => _criminalType switch
        {
            CriminalAffinityType.Thief    => "having just caught someone stealing, and confronting them",
            CriminalAffinityType.Intruder => "having just caught someone where they have no business being, and confronting them",
            CriminalAffinityType.Murderer => "having just seen someone commit violence, and confronting them",
            _                             => "having just caught someone in an illegal act, and confronting them",
        };

        private static string BuildConfrontationReplica(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "Stop there. I saw you take that.",
            CriminalAffinityType.Intruder => "Stop. What are you doing here? You have no business in this place.",
            CriminalAffinityType.Murderer => "I saw what you just did.",
            _                             => "Stop there. I saw what you did.",
        };

        private static string BuildConfrontationIndirect(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "I order {you:name} to stop, and tell them I saw them take that.",
            CriminalAffinityType.Intruder => "I order {you:name} to stop, and tell them they have no business here.",
            CriminalAffinityType.Murderer => "I tell {you:name} that I saw what they just did.",
            _                             => "I order {you:name} to stop, and tell them I saw what they did.",
        };

        private static string BuildConfrontationHeard(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "{npc:name} orders me to stop, and says they saw me take that.",
            CriminalAffinityType.Intruder => "{npc:name} orders me to stop, and says I have no business here.",
            CriminalAffinityType.Murderer => "{npc:name} says they saw what I just did.",
            _                             => "{npc:name} orders me to stop, and says they saw what I did.",
        };

        /// <summary>The deed as a bare gerund phrase — "taking that", "coming in here".</summary>
        private static string CrimeAct(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "taking that",
            CriminalAffinityType.Intruder => "coming in here",
            CriminalAffinityType.Murderer => "striking somebody",
            _                             => "what you did",
        };

        /// <summary>The same deed as the accused would report hearing it described.</summary>
        private static string CrimeActHeard(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "taking that",
            CriminalAffinityType.Intruder => "coming in there",
            CriminalAffinityType.Murderer => "striking somebody",
            _                             => "what I did",
        };

        /// <summary>The same deed phrased for "why were you …?".</summary>
        private static string ActPhrase(CriminalAffinityType crime) => crime switch
        {
            CriminalAffinityType.Thief    => "taking what is not yours",
            CriminalAffinityType.Intruder => "in a place you had no right to be",
            CriminalAffinityType.Murderer => "attacking somebody",
            _                             => "doing what you did",
        };
    }
}
