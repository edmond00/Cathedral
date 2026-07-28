using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

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

    /// <summary>A branch end. Hostility is a hard check at every depth.</summary>
    private static ResolutionNode End(string id, int depth, string success, string failure) => new(
        nodeId:         id,
        difficulty:     BranchDifficulty.Hard(depth),
        successReplica: success,
        failureReplica: failure);

    // ══════════════════════════════════════════════════════════════════════════
    //  A — apologise (deepest)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode HearOut() => new(
        nodeId:  "hear_out",
        replica: "...Sorry, are you? Go on, then. I'm listening. And I warn you — I've heard sorry before.",

        new PlayerOption("press_peace", "press for peace between you",
            "Let's put it behind us, {npc:name}. There's no sense in bad blood.",
            End("apology_peace", 2,
                "...Fine. I'll let it lie — for now. Don't make me regret it.",
                "Empty words. If it's a reckoning you're after, you'll have one!")),

        new PlayerOption("name_the_wrong", "name exactly what you did wrong",
            "I'll say it plain: I was in the wrong, and I know which part of it was worst.",
            ApologyNamed()),

        new PlayerOption("ask_their_side", "ask them to tell you how it looked from where they stood",
            "Tell me how it looked from where you were standing. I'll not argue with it.",
            ApologyTheirSide()));

    private static NpcLineNode ApologyNamed() => new(
        nodeId:  "apology_named",
        replica: "...Hm. Most people apologise for the whole of it at once, so they don't have to look at any of it. Go on, then. Say the worst part.",

        new PlayerOption("say_worst_part", "say the worst part out loud without softening it",
            "The worst of it wasn't what I did. It was that you'd trusted me not to.",
            End("apology_worst", 3,
                "...Aye. That was it. I didn't think you knew that. Alright. Alright — we'll let it lie.",
                "And knowing it changes nothing. You still did it.")),

        new PlayerOption("ask_what_worst", "ask them which part cut deepest",
            "You tell me which part cut deepest. I'd rather hear it from you than guess.",
            End("apology_ask_worst", 3,
                "...That you didn't come back after. That was the part. And now you have. Fine. Enough.",
                "You want me to do the work of your own conscience? Get out.")));

    private static NpcLineNode ApologyTheirSide() => new(
        nodeId:  "apology_their_side",
        replica: "From where I stood? It looked like you weighed me against something and I came out light. That's how it looked.",

        new PlayerOption("accept_it", "accept that without excusing yourself",
            "Then that's how it was. I'll not dress it up for you.",
            End("apology_accept", 3,
                "...No excuses. Huh. I'd braced for a speech. Go on, then — we'll leave it there.",
                "No excuses and no change either. You've said nothing.")),

        new PlayerOption("say_what_changed", "say what would be different now",
            "It wouldn't happen the same way twice. I can't prove that standing here. I can only start.",
            ApologyProve()));

    private static NpcLineNode ApologyProve() => new(
        nodeId:  "apology_prove",
        replica: "Start. That's a small word for what you're asking. What does starting look like, exactly?",

        new PlayerOption("offer_time", "offer to earn it back slowly rather than all at once",
            "Slowly. I'll come back, and keep coming back, until you've decided for yourself.",
            End("apology_time", 4,
                "...Then come back. I'll not promise what I'll say when you do. But come back.",
                "You'll come back and I'll still remember. Save yourself the walk.")),

        new PlayerOption("offer_now", "offer to do something for them today, before anything is settled",
            "It looks like me asking what you need done today, before you've forgiven anything.",
            End("apology_now", 4,
                "...There is something. And you asking before I'd softened — that's what does it. Alright. We're not enemies.",
                "You'd buy your way out with a day's work? I'm not for sale that cheap.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — explain it was a misunderstanding (rich)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode ExplainOpening() => new(
        nodeId:  "explain_opening",
        replica: "A misunderstanding. That's what they always call it after. Well — I've a moment. Make me understand it.",

        new PlayerOption("explain_plainly", "lay out what actually happened, plainly",
            "Here's what actually happened, without the shine on it.",
            End("explain_plainly_end", 2,
                "Hm. Perhaps I judged you too quickly. We'll leave it there, then.",
                "You twist your words prettily, but I'm not fooled. Draw, if you dare!")),

        new PlayerOption("explain_witness", "point out that whoever told them was not there",
            "Whoever carried that story to you wasn't standing where I was standing.",
            ExplainWitness()),

        new PlayerOption("explain_admit_part", "admit the part that was genuinely your fault",
            "Some of it was a mistake and some of it was me. I'll not pretend otherwise.",
            ExplainAdmitPart()));

    private static NpcLineNode ExplainWitness() => new(
        nodeId:  "explain_witness",
        replica: "...Nor were they. That's true enough. But they'd no reason to invent it, and you've every reason to deny it.",

        new PlayerOption("explain_grant_that", "grant that they have no cause to trust you",
            "You've no cause to take my word over theirs. I know that.",
            End("explain_grant_end", 3,
                "...At least you know it. That's more honesty than the story had in it. We'll let this rest.",
                "No cause at all. So why are you still talking?")),

        new PlayerOption("explain_invite_check", "invite them to go and ask someone who was there",
            "Then don't take my word. Go and ask someone who was there, and I'll wait.",
            End("explain_check_end", 3,
                "...A liar doesn't invite checking. Alright. I'll ask. And until I have — we're not at war.",
                "I'll ask, and when it comes back against you, I'll come and find you.")));

    private static NpcLineNode ExplainAdmitPart() => new(
        nodeId:  "explain_admit",
        replica: "Now that's not what I expected. Most either deny the lot or crumple entirely. Which part was you, then?",

        new PlayerOption("explain_own_temper", "own the part where you lost your temper",
            "The part where I lost my temper. The rest was chance; that wasn't.",
            End("explain_temper_end", 3,
                "...Aye. That was the part that stung, if I'm honest. Owning it costs you something. Fine — it's done.",
                "Your temper. And what's to stop it next time? Nothing. That's what I thought.")),

        new PlayerOption("explain_own_silence", "own the part where you said nothing afterward",
            "The part where I said nothing afterward and let you think the worst.",
            End("explain_silence_end", 3,
                "...That was worse than the thing itself. And you knew it. Alright. Alright, we'll start again.",
                "You let it stand for weeks. Don't expect me to undo that in a minute.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — offer amends (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AmendsOpening() => new(
        nodeId:  "amends_opening",
        replica: "Amends. So it's a bargain you're after, not a peace. Well — what is it you're offering?",

        new PlayerOption("amends_labour", "offer your own labour to set it right",
            "My hands. Whatever needs doing, for as long as it takes.",
            End("amends_labour_end", 2,
                "...Work, not words. That I can weigh. Come at first light and we'll call it square.",
                "Your labour's worth less than what you cost me. Try again — or don't.")),

        new PlayerOption("amends_ask_what", "ask them to name what would settle it",
            "You name it. Whatever settles this, I'll hear it.",
            AmendsNamed()),

        new PlayerOption("amends_goods", "offer what you carry",
            "I've what I'm carrying. Take what makes it right.",
            End("amends_goods_end", 2,
                "Keep your goods. But you offered them without haggling, and that counts. We'll leave it.",
                "You think this is about property? You understand nothing.")));

    private static NpcLineNode AmendsNamed() => new(
        nodeId:  "amends_named",
        replica: "...Nobody's asked me that. They always decide what I'm owed and hand it over. What I want is for it not to have happened, and you can't give me that.",

        new PlayerOption("amends_agree_cant", "agree that you cannot give them that",
            "No. I can't. I'll not pretend there's a price on it.",
            End("amends_cant_end", 3,
                "...Then we understand each other, at least. That's not friendship. It'll do for peace.",
                "Then there's nothing here for either of us. Go.")),

        new PlayerOption("amends_offer_next", "offer the nearest thing you can give",
            "Then take the nearest thing I have: it won't happen again, and I'll be about to prove it.",
            End("amends_next_end", 3,
                "The nearest thing. Aye. I'll take the nearest thing and watch what you do with it.",
                "Promises about the future from a man who ruined the past. No.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — stand your ground (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode StandOpening() => new(
        nodeId:  "stand_opening",
        replica: "No apology, then. You've a nerve — I'll give you that much and nothing else. Say your piece.",

        new PlayerOption("stand_want_peace", "say you want peace, not forgiveness",
            "I'm not asking to be forgiven. I'm asking that we stop this.",
            End("stand_peace_end", 2,
                "...Peace without pardon. That's an honest thing to ask for. Aye. Let's stop it.",
                "You'll stop it when I say it's stopped, and I haven't.")),

        new PlayerOption("stand_both_wrong", "point out that neither of you came out of it clean",
            "Neither of us came out of that clean, and we both know it.",
            StandBothWrong()),

        new PlayerOption("stand_tired", "say plainly that you are tired of carrying it",
            "I'm tired of carrying this about with me. Aren't you?",
            End("stand_tired_end", 2,
                "...God, yes. I'm exhausted with it. Fine. Enough. It's done.",
                "I'll carry it happily as long as it takes. Don't mistake me for you.")));

    private static NpcLineNode StandBothWrong() => new(
        nodeId:  "stand_both_wrong",
        replica: "Both of us. Careful. I've spent a good while being certain it was all your doing, and I'd rather not have it disturbed.",

        new PlayerOption("stand_hold_it", "hold to it without pushing",
            "I'll say it once and then let it lie. It was both of us.",
            End("stand_hold_end", 3,
                "...Once, and then you left it alone. That's what does it. Alright. Both of us.",
                "Say it once and I'll answer it once: get out of my sight.")),

        new PlayerOption("stand_ask_theirs", "ask what they would name as their own part",
            "So what was yours? Name it, and I'll name mine after.",
            End("stand_theirs_end", 3,
                "...I let it fester rather than come to you. There. I've said it, and I hated it. We're even enough.",
                "Mine? Trusting you at all. That's my part, and I've corrected it.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Opening = new(
        nodeId:  "opening",
        replica: "You've a nerve, showing your face to me. What do you want?",

        new PlayerOption("apologize", "offer a sincere apology",
            "I've come to make peace. I'm sorry for what passed between us.", HearOut()),

        new PlayerOption("explain", "explain that the hostility is a misunderstanding",
            "Hear me out — this quarrel between us is a misunderstanding.", ExplainOpening()),

        new PlayerOption("offer_amends", "offer to make amends for what you did",
            "I'd make it right, if you'll tell me how.", AmendsOpening()),

        new PlayerOption("stand_ground", "refuse to grovel but ask for an end to it",
            "I'll not crawl for you. But I'd have this finished between us.", StandOpening()));

    public override NpcLineNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
    {
        if (npc.AffinityTable.IsEnemy(partyMemberId)) return true;
        return npc.AffinityTable.GetLevel(partyMemberId) == AffinityLevel.AnnoyingAcquaintance;
    }
}
