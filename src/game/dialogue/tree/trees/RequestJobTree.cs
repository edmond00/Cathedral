using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Request job" — the player asks a master or reeve to take them on for work.
/// Success opens the work menu after the dialogue; failure turns the player away for now.
///
/// <para>
/// The REQUEST_JOB verb has already chosen <i>which</i> post is being asked for (one of the three
/// this NPC offers), so the conversation names it: <c>{npc:job_title}</c>, <c>{npc:job_offer}</c>
/// and <c>{npc:job_pay}</c> all read straight off the pending offer. Asking to be a bellows-hand and
/// asking to be a hay-hauler are the same tree and a different conversation.
/// </para>
///
/// <para>
/// Four ways to ask — plainly, by showing willing, by asking about the post itself, or by offering
/// to work a day for nothing first — on the <see cref="BranchDifficulty.Hard"/> ladder: being hired
/// is a real ask, not small talk.
/// </para>
/// </summary>
public class RequestJobTree : DialogueTree
{
    public override string TreeId           => "request_job";
    public override string DisplayName      => "Request Job";
    public override string Description      => "asking a master or reeve to take you on for work";
    public override string AssociatedVerbId => "request_job";

    // Success opens the work menu; a routine bakes in that success so replaying opens work directly.
    public override DialogueRoutineBehavior RoutineBehavior => DialogueRoutineBehavior.IncludeSuccess;

    public override IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; } = new IDialogueOutcome[]
    {
        new OpenJobMenuOutcome(),
    };

    public override IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; } = System.Array.Empty<IDialogueOutcome>();

    /// <summary>A branch end. Being taken on is a hard check at every depth.</summary>
    private static ResolutionNode End(string id, int depth, string success, string failure) => new(
        nodeId:         id,
        difficulty:     BranchDifficulty.Hard(depth),
        successReplica: success,
        failureReplica: failure);

    // ══════════════════════════════════════════════════════════════════════════
    //  A — ask plainly (deepest)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskPlainly() => new(
        nodeId:  "ask_plainly",
        replica: "Work as {npc:job_offer}. Aye, I've that going. It pays {npc:job_pay}, and it's no softer than it sounds.",

        new PlayerOption("plain_accept_pay", "accept the pay as stated",
            "That's the rate. I'll not haggle over it standing in your yard.",
            End("plain_accept_end", 2,
                "Well, you've the look of a worker. Come — there's labour enough for willing hands.",
                "I've naught for you today. Try your luck elsewhere.")),

        new PlayerOption("plain_ask_days", "ask what the days actually look like",
            "And what does a day of it actually look like? I'd rather know than guess.",
            PlainDays()),

        new PlayerOption("plain_ask_why_open", "ask why the post is going begging",
            "Why's it still going? A post that's open a while is open for a reason.",
            PlainWhyOpen()));

    private static NpcLineNode PlainDays() => new(
        nodeId:  "plain_days",
        replica: "First light to last, at {npc:workplace}, and no arguing about the weather. {npc:opinion_work}",

        new PlayerOption("days_say_done_worse", "say you have worked worse",
            "I've worked worse for less. That doesn't frighten me.",
            End("days_worse_end", 3,
                "Then you'll do. Come on — I'd rather have a hand who's been broken in already.",
                "Everyone's worked worse, to hear them tell it. I've heard it too often.")),

        new PlayerOption("days_ask_learn", "ask whether you would learn anything from it",
            "And would I come out of it knowing anything I didn't?",
            End("days_learn_end", 3,
                "...You would. Nobody asks me that — they ask about the pay. Aye, I'll take you on.",
                "You'd come out of it tired. That's the whole of what I promise.")));

    private static NpcLineNode PlainWhyOpen() => new(
        nodeId:  "plain_why_open",
        replica: "...Because the last one walked off in the middle of it and left me short at the worst hour. That's why.",

        new PlayerOption("why_promise_stay", "promise you would see it through",
            "Then you'll have the opposite from me. I finish what I start.",
            End("why_stay_end", 3,
                "Bold words. Let's see if your back matches your tongue — there's work to be had.",
                "So did he. Right up until the morning he wasn't there.")),

        new PlayerOption("why_ask_what_broke", "ask what drove them off",
            "What drove them off? I'd rather know before I take it on.",
            WhyWhatBroke()));

    private static NpcLineNode WhyWhatBroke() => new(
        nodeId:  "why_what_broke",
        replica: "...Honestly? I was hard on him, and he was young, and I'd not the patience the work needed. That's half of it. The other half was him.",

        new PlayerOption("broke_say_fair", "say that was fairly told",
            "That's fairly told. Most masters would have laid it all on the lad.",
            End("broke_fair_end", 4,
                "...Aye, well. I've had time to think on it. Come on then — and I'll try to be better at it this time.",
                "Fairly told and none of your business. Are you working or judging?")),

        new PlayerOption("broke_ask_patience", "ask whether the patience is there now",
            "And is the patience there now? I'd rather know what I'm walking into.",
            End("broke_patience_end", 4,
                "...That's a fair thing to ask a man and a hard one to answer. Some days. I'll not lie to you. Come — start tomorrow.",
                "You'll take the post as it is or leave it. I'll not be interviewed in my own yard.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — show you are willing and able (rich)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode ShowWilling() => new(
        nodeId:  "show_willing",
        replica: "Strong and willing. So's every third person who comes through here, and half of them are gone by the second week.",

        new PlayerOption("willing_stand_by", "stand by what you said",
            "Then judge me by the second week, not by the doorway.",
            End("willing_stand_end", 2,
                "Bold words. Let's see if your back matches your tongue — there's work to be had.",
                "Willing or no, I've nothing for you just now.")),

        new PlayerOption("willing_name_experience", "name what you have actually done",
            "I'll be plainer, then. Here's the sort of work I've actually done with these hands.",
            WillingExperience()),

        new PlayerOption("willing_admit_green", "admit you are new to this kind of work",
            "I'll be honest — I've not done this particular work. I learn fast and I don't sulk.",
            WillingGreen()));

    private static NpcLineNode WillingExperience() => new(
        nodeId:  "willing_experience",
        replica: "Hm. That's more than most can list without inventing. And you'd take {npc:job_offer} after that?",

        new PlayerOption("exp_no_pride", "say you have no pride about the work",
            "I've no pride about it. Work's work and I'd rather have it than not.",
            End("exp_pride_end", 3,
                "That's the answer I wanted. Come on — start when you like.",
                "No pride and no standards, more like. I'll pass.")),

        new PlayerOption("exp_want_foot_in", "say you want a foot in the door here",
            "I'd take it to get a foot in the door here. I'll not pretend otherwise.",
            End("exp_foot_end", 3,
                "...Honest about your own scheming. I can work with that. Come, the post's yours.",
                "A foot in the door. And out again the moment something better passes. No.")));

    private static NpcLineNode WillingGreen() => new(
        nodeId:  "willing_green",
        replica: "Green, then. At least you said so. I've had men swear they knew the work and ruin a day's worth proving otherwise.",

        new PlayerOption("green_ask_teach", "ask them to show you once and no more",
            "Show me once. If I need showing twice, send me off and keep the day's pay.",
            End("green_teach_end", 3,
                "...Once, and your own pay against it. That's a wager I'll take. Come on.",
                "I've no time to be teaching. Come back when someone else has trained you.")),

        new PlayerOption("green_offer_low", "offer to be paid less until you are worth it",
            "Pay me under the rate until I'm worth it. I'll not argue the difference.",
            End("green_low_end", 3,
                "Under the rate by your own asking. Hah — alright. You'll be on full pay sooner than you think.",
                "If you'll work for under, you'll work for anyone. That tells me what you're worth.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — ask about the post itself (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskAboutPost() => new(
        nodeId:  "ask_about_post",
        replica: "The post itself. Well — {npc:job_offer} is what it says: {npc:labour}, and it pays {npc:job_pay}.",

        new PlayerOption("post_ask_hours", "ask how long a stint runs",
            "And how long a stint would you want out of me?",
            End("post_hours_end", 2,
                "As long as you'll give and no less than a fair few days. Come — we'll settle it inside.",
                "Longer than you'd give, by the sound of you. Off with you.")),

        new PlayerOption("post_ask_pay_fair", "ask whether the pay is what others get",
            "Is that what the others get, or is that the newcomer's rate?",
            PostPayFair()),

        new PlayerOption("post_say_suits", "say it suits you and ask to start",
            "That suits me well enough. When can I start?",
            End("post_suits_end", 2,
                "Now, if you've the stomach for it. Come along.",
                "You can start looking elsewhere. I've nothing.")));

    private static NpcLineNode PostPayFair() => new(
        nodeId:  "post_pay_fair",
        replica: "...That's a sharp question to put to a man in his own yard. It's the rate. Same as the last one had, and the one before.",

        new PlayerOption("pay_accept_word", "take them at their word",
            "Then I'll take your word on it and we'll say no more.",
            End("pay_word_end", 3,
                "...You'll find I keep it. Come on, there's work waiting.",
                "You'll take my word and question it in the same breath. No.")),

        new PlayerOption("pay_say_why_ask", "explain why you asked",
            "I ask because I've been paid the newcomer's rate for a year before now.",
            End("pay_why_end", 3,
                "...Then someone used you badly. Not here. Come on — the rate's the rate.",
                "Then take it up with whoever did that, not with me.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — offer a day for nothing (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode OfferTrial() => new(
        nodeId:  "offer_trial",
        replica: "A day for nothing. That's either desperation or confidence, and I've been fooled by both.",

        new PlayerOption("trial_confidence", "say it is confidence",
            "Confidence. Watch me for a day and you'll not want to send me off.",
            End("trial_confidence_end", 2,
                "Then I'll watch. Come at first light and don't be late — that's the whole test.",
                "Confidence I've seen. It's endurance I'm short of. Move along.")),

        new PlayerOption("trial_admit_need", "admit you need the work",
            "Desperation, if I'm honest. I need the work and I'd rather earn it than beg it.",
            TrialNeed()),

        new PlayerOption("trial_no_obligation", "make clear they would owe you nothing",
            "And if I'm no good, you owe me nothing and I'll go without a word.",
            End("trial_no_obligation_end", 2,
                "No fuss either way. That I can agree to. Come on, then.",
                "You'd say that now. They always find a word to say afterward.")));

    private static NpcLineNode TrialNeed() => new(
        nodeId:  "trial_need",
        replica: "...Earn it rather than beg it. Hm. I've been on that side of a yard myself, a long time ago, and I've not forgotten it.",

        new PlayerOption("need_thank", "thank them for hearing you out",
            "Then you know what it took to ask. My thanks for hearing it.",
            End("need_thank_end", 3,
                "Aye. I do. Come on — the work's yours, and we'll say nothing more about how it started.",
                "Knowing it doesn't oblige me. I've a holding to run.")),

        new PlayerOption("need_press_case", "press your case while they are listening",
            "Then take me on. You'll not find anyone who'll work harder for the first month.",
            End("need_press_end", 3,
                "The first month, is it. Let's see about the second. Aye — you're taken on.",
                "The first month. And then what? I need hands, not a burst of enthusiasm.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Opening = new(
        nodeId:  "opening",
        replica: "Aye, {you:name}? What brings you to me?",

        new PlayerOption("ask_plainly", "ask plainly for the work and its pay",
            "I'm after work as {npc:job_offer}, {npc:name}. What does it pay?", AskPlainly()),

        new PlayerOption("show_willing", "show you are willing and able",
            "I'm strong and willing — put me to any task you like.", ShowWilling()),

        new PlayerOption("ask_about_post", "ask what the post involves before asking for it",
            "Before I ask for it — what does {npc:job_offer} actually involve here?", AskAboutPost()),

        new PlayerOption("offer_trial", "offer to work a day for nothing first",
            "Give me a day of it for nothing. Judge me on that.", OfferTrial()));

    public override NpcLineNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
    {
        if (npc.AffinityTable.IsEnemy(partyMemberId)) return false;
        var level = npc.AffinityTable.GetLevel(partyMemberId);
        return level is AffinityLevel.DistantAcquaintance
                     or AffinityLevel.CloseAcquaintance
                     or AffinityLevel.DistantFriend
                     or AffinityLevel.CloseFriend;
    }
}
