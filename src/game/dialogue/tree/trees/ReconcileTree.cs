using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Reconcile" — available when the NPC is an enemy or an AnnoyingAcquaintance.
/// The player tries to end the hostility. Success clears the enemy flag and sets a wary Suspicious
/// affinity; failure leaves them hostile and a brave NPC demands a fight.
///
/// <para>
/// Four ways in — apologise, explain, offer amends, or refuse to grovel — and none of them is the
/// "right" one. The apology runs deepest, because a real apology takes more than one sentence and
/// this tree is where that should be felt. Every branch uses the <see cref="BranchDifficulty.Hard"/>
/// ladder: peace with someone who hates you is not small talk.
/// </para>
///
/// <para>
/// Every replica is the spoken line, plainly. This tree is the hardest one to keep plain, because
/// the temptation is to author the hurt; the anger and the thaw belong to the persona speaking the
/// line. See "Authoring the neutral text" on <see cref="DialogueTree"/>.
/// </para>
/// </summary>
public class ReconcileTree : DialogueTree
{
    public override string TreeId           => "reconcile";
    public override string DisplayName      => "Reconcile";
    public override string Description      => "attempting to end hostility and reach a fragile peace";
    public override string AssociatedVerbId => "reconcile";

    /// <summary>What succeeding at this conversation teaches: talking someone down out of their anger.</summary>
    public override string? GrantedModusMentisId => "empathy";

    /// <summary>
    /// The affinity move comes FIRST, deliberately: it asks whether this was real hostility or mere
    /// irritation, and the flag it reads is the one <see cref="ClearEnemyOutcome"/> then removes.
    /// An enemy is talked down to the wary Suspicious; an annoyed acquaintance — who was never an
    /// enemy — is stepped one rung up instead, since Suspicious grants fewer dice than the state
    /// they were already in and a won conversation must not leave the player worse off.
    /// </summary>
    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[]
    {
        new SuspiciousAffinityOutcome(onlyWhenHostile: true),
        new ClearEnemyOutcome(),
    };

    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[]
    {
        new FightRequestOutcome(),
    };

    /// <summary>A branch end. Hostility is a hard check at every depth.</summary>
    private static ResolutionNode End(string id, int depth,
                                      string success, string successIndirect,
                                      string failure, string failureIndirect) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Hard(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect);

    // ══════════════════════════════════════════════════════════════════════════
    //  A — apologise (deepest)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode HearOut() => new(
        nodeId:          "hear_out",
        replica:         "Sorry, are you. Go on, I am listening. I have heard sorry before.",
        replicaIndirect: "I tell {you:name} to go on, and that I have heard sorry before.",
        replicaHeard:    "{npc:name} tells me to go on, and says they have heard sorry before.",

        new PlayerOption("press_peace", "press for peace between you",
            "Let us put it behind us, {npc:name}. There is no sense in bad blood.",
            "I ask {npc:name} to put it behind us, since there is no sense in bad blood.",
            End("apology_peace", 2,
                "I will let it lie for now. Do not make me regret it.",
                "I tell {you:name} I will let it lie for now, and not to make me regret it.",
                "Those are empty words. If you want a reckoning, you will have one.",
                "I tell {you:name} that if they want a reckoning they will have one.")),

        new PlayerOption("name_the_wrong", "name exactly what you did wrong",
            "I was in the wrong, and I know which part was worst.",
            "I tell {npc:name} I was in the wrong and know which part was worst.",
            ApologyNamed()),

        new PlayerOption("ask_their_side", "ask them to tell you how it looked from where they stood",
            "Tell me how it looked from where you stood. I will not argue with it.",
            "I ask {npc:name} how it looked from where they stood, and promise not to argue.",
            ApologyTheirSide()));

    private static NpcLineNode ApologyNamed() => new(
        nodeId:          "apology_named",
        replica:         "Most people apologise for all of it at once, so they need not look at any of it. Say the worst part.",
        replicaIndirect: "I tell {you:name} most apologise for all of it at once so they need not look at any of it, and ask for the worst part.",
        replicaHeard:    "{npc:name} says most apologise for all of it at once so they need not look at any of it, and asks me for the worst part.",

        new PlayerOption("say_worst_part", "say the worst part out loud without softening it",
            "The worst of it was not what I did. It was that you had trusted me not to.",
            "I tell {npc:name} the worst of it was that they had trusted me not to.",
            End("apology_worst", 3,
                "That was it. I did not think you knew that. We will let it lie.",
                "I tell {you:name} that was it, that I did not think they knew, and that we will let it lie.",
                "Knowing it changes nothing. You still did it.",
                "I tell {you:name} that knowing it changes nothing.")),

        new PlayerOption("ask_what_worst", "ask them which part cut deepest",
            "You tell me which part was worst. I would rather hear it than guess.",
            "I ask {npc:name} which part was worst, rather than guess.",
            End("apology_ask_worst", 3,
                "That you did not come back afterwards. Now you have. That is enough.",
                "I tell {you:name} it was that they did not come back afterwards, and that now they have it is enough.",
                "You want me to do the work of your own conscience. Get out.",
                "I tell {you:name} they want me to do the work of their own conscience.")));

    private static NpcLineNode ApologyTheirSide() => new(
        nodeId:          "apology_their_side",
        replica:         "It looked as though you weighed me against something else and chose the other.",
        replicaIndirect: "I tell {you:name} it looked as though they weighed me against something else and chose the other.",
        replicaHeard:    "{npc:name} says it looked as though I weighed them against something else and chose the other.",

        new PlayerOption("accept_it", "accept that without excusing yourself",
            "Then that is how it was. I will not dress it up.",
            "I tell {npc:name} that is how it was and I will not dress it up.",
            End("apology_accept", 3,
                "No excuses. I had expected a speech. We will leave it there.",
                "I tell {you:name} I had expected a speech, and that we will leave it there.",
                "No excuses, and no change either. You have said nothing.",
                "I tell {you:name} there is no change either, and that they have said nothing.")),

        new PlayerOption("say_what_changed", "say what would be different now",
            "It would not happen the same way twice. I cannot prove that standing here. I can only start.",
            "I tell {npc:name} it would not happen twice, and that I can only start.",
            ApologyProve()));

    private static NpcLineNode ApologyProve() => new(
        nodeId:          "apology_prove",
        replica:         "Start. That is a small word for what you are asking. What does starting mean?",
        replicaIndirect: "I tell {you:name} start is a small word for what they are asking, and ask what it means.",
        replicaHeard:    "{npc:name} says start is a small word for what I am asking, and asks what it means.",

        new PlayerOption("offer_time", "offer to earn it back slowly rather than all at once",
            "Slowly. I will come back, and keep coming back, until you have decided for yourself.",
            "I tell {npc:name} I will keep coming back until they have decided for themselves.",
            End("apology_time", 4,
                "Then come back. I will not promise what I say when you do, but come back.",
                "I tell {you:name} to come back, though I will not promise what I say when they do.",
                "You will come back and I will still remember. Save yourself the walk.",
                "I tell {you:name} I will still remember, and to save themselves the walk.")),

        new PlayerOption("offer_now", "offer to do something for them today, before anything is settled",
            "It means asking what you need done today, before you have forgiven anything.",
            "I ask {npc:name} what they need done today, before they have forgiven anything.",
            End("apology_now", 4,
                "There is something. And you asked before I had softened, which is what decides it. We are not enemies.",
                "I tell {you:name} their asking before I had softened is what decides it, and that we are not enemies.",
                "You would buy your way out with a day's work. I am not for sale that cheaply.",
                "I tell {you:name} I am not for sale that cheaply.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — explain it was a misunderstanding (rich)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode ExplainOpening() => new(
        nodeId:          "explain_opening",
        replica:         "A misunderstanding. That is what it is always called afterwards. I have a moment. Make me understand it.",
        replicaIndirect: "I tell {you:name} that is what it is always called afterwards, and challenge them to make me understand it.",
        replicaHeard:    "{npc:name} says that is what it is always called afterwards, and challenges me to make them understand it.",

        new PlayerOption("explain_plainly", "lay out what actually happened, plainly",
            "Here is what actually happened, with nothing left out.",
            "I tell {npc:name} what actually happened, with nothing left out.",
            End("explain_plainly_end", 2,
                "Perhaps I judged you too quickly. We will leave it there.",
                "I tell {you:name} I may have judged them too quickly, and that we will leave it there.",
                "You put it well, but I am not convinced. Draw your weapon.",
                "I tell {you:name} I am not convinced, and to draw.")),

        new PlayerOption("explain_witness", "point out that whoever told them was not there",
            "Whoever told you that story was not standing where I was standing.",
            "I tell {npc:name} whoever told them that was not standing where I was.",
            ExplainWitness()),

        new PlayerOption("explain_admit_part", "admit the part that was genuinely your fault",
            "Some of it was an accident and some of it was me. I will not pretend otherwise.",
            "I tell {npc:name} some of it was an accident and some of it was me.",
            ExplainAdmitPart()));

    private static NpcLineNode ExplainWitness() => new(
        nodeId:          "explain_witness",
        replica:         "Nor were they. But they had no reason to invent it, and you have every reason to deny it.",
        replicaIndirect: "I tell {you:name} they had no reason to invent it, while {you:name} has every reason to deny it.",
        replicaHeard:    "{npc:name} says the other had no reason to invent it, while I have every reason to deny it.",

        new PlayerOption("explain_grant_that", "grant that they have no cause to trust you",
            "You have no reason to take my word over theirs. I know that.",
            "I grant {npc:name} they have no reason to take my word over the other's.",
            End("explain_grant_end", 3,
                "At least you know it. That is more honesty than the story had. We will let this rest.",
                "I tell {you:name} that is more honesty than the story had, and that we will let it rest.",
                "No reason at all. So why are you still talking?",
                "I ask {you:name} why they are still talking, then.")),

        new PlayerOption("explain_invite_check", "invite them to go and ask someone who was there",
            "Then do not take my word. Ask someone who was there, and I will wait.",
            "I tell {npc:name} to ask someone who was there, and that I will wait.",
            End("explain_check_end", 3,
                "A liar does not invite checking. I will ask, and until then we are not at war.",
                "I tell {you:name} a liar does not invite checking, and that until I have asked we are not at war.",
                "I will ask, and when the answer goes against you, I will come and find you.",
                "I tell {you:name} that when the answer goes against them I will come and find them.")));

    private static NpcLineNode ExplainAdmitPart() => new(
        nodeId:          "explain_admit",
        replica:         "That is not what I expected. Most either deny all of it or give way entirely. Which part was you?",
        replicaIndirect: "I tell {you:name} most either deny all of it or give way entirely, and ask which part was them.",
        replicaHeard:    "{npc:name} says most either deny all of it or give way entirely, and asks which part was me.",

        new PlayerOption("explain_own_temper", "own the part where you lost your temper",
            "The part where I lost my temper. The rest was chance. That was not.",
            "I tell {npc:name} it was the part where I lost my temper, and the rest was chance.",
            End("explain_temper_end", 3,
                "That was the part that stung. Owning it costs you something. It is done.",
                "I tell {you:name} that was the part that stung, and that owning it costs them something.",
                "Your temper. And what stops it happening again? Nothing.",
                "I ask {you:name} what stops their temper next time, and answer that nothing does.")),

        new PlayerOption("explain_own_silence", "own the part where you said nothing afterward",
            "The part where I said nothing afterwards and let you think the worst.",
            "I tell {npc:name} it was the part where I said nothing and let them think the worst.",
            End("explain_silence_end", 3,
                "That was worse than the thing itself, and you knew it. We will start again.",
                "I tell {you:name} that was worse than the thing itself, and that we will start again.",
                "You let it stand for weeks. Do not expect me to undo that in a minute.",
                "I tell {you:name} they let it stand for weeks.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — offer amends (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AmendsOpening() => new(
        nodeId:          "amends_opening",
        replica:         "Amends. So you want a bargain, not a peace. What are you offering?",
        replicaIndirect: "I tell {you:name} they want a bargain and not a peace, and ask what they are offering.",
        replicaHeard:    "{npc:name} says I want a bargain and not a peace, and asks what I am offering.",

        new PlayerOption("amends_labour", "offer your own labour to set it right",
            "My work. Whatever needs doing, for as long as it takes.",
            "I offer {npc:name} my work, whatever needs doing, for as long as it takes.",
            End("amends_labour_end", 2,
                "Work, not words. I can weigh that. Come at first light and we will call it settled.",
                "I tell {you:name} work I can weigh, and to come at first light.",
                "Your work is worth less than what you cost me. Try again, or do not.",
                "I tell {you:name} their work is worth less than what they cost me.")),

        new PlayerOption("amends_ask_what", "ask them to name what would settle it",
            "You name it. Whatever settles this, I will hear it.",
            "I ask {npc:name} to name whatever would settle this.",
            AmendsNamed()),

        new PlayerOption("amends_goods", "offer what you carry",
            "I have what I am carrying. Take whatever makes it right.",
            "I offer {npc:name} whatever of my goods makes it right.",
            End("amends_goods_end", 2,
                "Keep your goods. But you offered them without haggling, and that counts. We will leave it.",
                "I tell {you:name} to keep their goods, but that offering without haggling counts.",
                "You think this is about property. You understand nothing.",
                "I tell {you:name} they understand nothing if they think this is about property.")));

    private static NpcLineNode AmendsNamed() => new(
        nodeId:          "amends_named",
        replica:         "Nobody has asked me that. What I want is for it not to have happened, and you cannot give me that.",
        replicaIndirect: "I tell {you:name} what I want is for it not to have happened, and that they cannot give me that.",
        replicaHeard:    "{npc:name} says what they want is for it not to have happened, and that I cannot give them that.",

        new PlayerOption("amends_agree_cant", "agree that you cannot give them that",
            "No, I cannot. I will not pretend there is a price on it.",
            "I agree with {npc:name} that I cannot, and that there is no price on it.",
            End("amends_cant_end", 3,
                "Then we understand each other. That is not friendship, but it will do for peace.",
                "I tell {you:name} we understand each other, and that it will do for peace.",
                "Then there is nothing here for either of us. Go.",
                "I tell {you:name} there is nothing here for either of us.")),

        new PlayerOption("amends_offer_next", "offer the nearest thing you can give",
            "Then take the nearest thing I have: it will not happen again, and I will prove it.",
            "I offer {npc:name} the nearest thing I have, that it will not happen again.",
            End("amends_next_end", 3,
                "I will take the nearest thing and watch what you do with it.",
                "I tell {you:name} I will take the nearest thing and watch what they do with it.",
                "Promises about the future from a man who ruined the past. No.",
                "I tell {you:name} promises about the future are worth nothing from them.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — stand your ground (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode StandOpening() => new(
        nodeId:          "stand_opening",
        replica:         "No apology, then. You have nerve. Say what you came to say.",
        replicaIndirect: "I tell {you:name} they have nerve, and to say what they came to say.",
        replicaHeard:    "{npc:name} says I have nerve, and tells me to say what I came to say.",

        new PlayerOption("stand_want_peace", "say you want peace, not forgiveness",
            "I am not asking to be forgiven. I am asking that we stop this.",
            "I tell {npc:name} I am not asking to be forgiven, only that we stop this.",
            End("stand_peace_end", 2,
                "Peace without pardon. That is an honest thing to ask for. Let us stop it.",
                "I tell {you:name} peace without pardon is an honest thing to ask for, and that we will stop it.",
                "You will stop it when I say it is stopped, and I have not.",
                "I tell {you:name} they will stop it when I say it is stopped.")),

        new PlayerOption("stand_both_wrong", "point out that neither of you came out of it clean",
            "Neither of us came out of that clean, and we both know it.",
            "I tell {npc:name} neither of us came out of that clean.",
            StandBothWrong()),

        new PlayerOption("stand_tired", "say plainly that you are tired of carrying it",
            "I am tired of carrying this. Are you not?",
            "I tell {npc:name} I am tired of carrying this, and ask whether they are not.",
            End("stand_tired_end", 2,
                "Yes, I am tired of it. Enough. It is done.",
                "I admit I am tired of it too, and that it is done.",
                "I will carry it as long as it takes. Do not mistake me for you.",
                "I tell {you:name} I will carry it as long as it takes.")));

    private static NpcLineNode StandBothWrong() => new(
        nodeId:          "stand_both_wrong",
        replica:         "Both of us. Be careful. I have spent a long while certain it was all your doing.",
        replicaIndirect: "I tell {you:name} I have spent a long while certain it was all their doing, and would rather not have that disturbed.",
        replicaHeard:    "{npc:name} says they have spent a long while certain it was all my doing, and would rather not have that disturbed.",

        new PlayerOption("stand_hold_it", "hold to it without pushing",
            "I will say it once and then let it lie. It was both of us.",
            "I tell {npc:name} once that it was both of us, and then let it lie.",
            End("stand_hold_end", 3,
                "Once, and then you left it alone. That is what decides it. Both of us, then.",
                "I tell {you:name} that saying it once and leaving it alone is what decides it.",
                "You said it once, and I will answer once: get out of my sight.",
                "I tell {you:name} to get out of my sight.")),

        new PlayerOption("stand_ask_theirs", "ask what they would name as their own part",
            "What was your part? Name it, and I will name mine after.",
            "I ask {npc:name} to name their part, and offer to name mine after.",
            End("stand_theirs_end", 3,
                "I let it fester instead of coming to you. There, I have said it. We are even enough.",
                "I admit I let it fester instead of coming to them, and that we are even enough.",
                "Mine was trusting you at all, and I have corrected it.",
                "I tell {you:name} my part was trusting them at all, and that I have corrected it.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Opening = new(
        nodeId:          "opening",
        replica:         "You have nerve, showing your face to me. What do you want?",
        replicaIndirect: "I tell {you:name} they have nerve showing their face to me, and ask what they want.",
        replicaHeard:    "{npc:name} says I have nerve showing my face to them, and asks what I want.",

        new PlayerOption("apologize", "offer a sincere apology",
            "I have come to make peace. I am sorry for what happened between us.",
            "I tell {npc:name} I have come to make peace, and apologise for what happened.",
            HearOut()),

        new PlayerOption("explain", "explain that the hostility is a misunderstanding",
            "Hear me out. This quarrel between us is a misunderstanding.",
            "I tell {npc:name} this quarrel between us is a misunderstanding.",
            ExplainOpening()),

        new PlayerOption("offer_amends", "offer to make amends for what you did",
            "I would make it right, if you will tell me how.",
            "I offer {npc:name} to make it right, if they will tell me how.",
            AmendsOpening()),

        new PlayerOption("stand_ground", "refuse to grovel but ask for an end to it",
            "I will not grovel. But I would have this finished between us.",
            "I tell {npc:name} I will not grovel, but would have this finished.",
            StandOpening()));

    public override NpcLineNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
    {
        if (npc.AffinityTable.IsEnemy(partyMemberId)) return true;
        return npc.AffinityTable.GetLevel(partyMemberId) == AffinityLevel.AnnoyingAcquaintance;
    }
}
