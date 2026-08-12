using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative;

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
///
/// <para>
/// Every replica is the spoken line, plainly — the gruffness of a master and the nerve of an
/// applicant are the personas' to add. See "Authoring the neutral text" on <see cref="DialogueTree"/>.
/// </para>
/// </summary>
public class RequestJobTree : DialogueTree
{
    public override string TreeId           => "request_job";
    public override string DisplayName      => "Request Job";
    public override string Description      => "asking a master or reeve to take you on for work";

    /// <summary>The other chair: you ARE the master or reeve being asked.</summary>
    public override string NpcDescription   => "being asked for work by someone looking to be taken on";
    public override string AssociatedVerbId => "request_job";

    /// <summary>What succeeding at this conversation teaches: putting yourself forward for work.</summary>
    public override string? GrantedModusMentisId => "enterprise";

    // Success opens the work menu; a routine bakes in that success so replaying opens work directly.
    public override DialogueRoutineBehavior RoutineBehavior => DialogueRoutineBehavior.IncludeSuccess;

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[]
    {
        new OpenJobMenuOutcome(),
    };

    public override IReadOnlyList<Outcome> FailureOutcomes => System.Array.Empty<Outcome>();

    /// <summary>A branch end. Being taken on is a hard check at every depth.</summary>
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
    //  A — ask plainly (deepest)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskPlainly() => new(
        nodeId:          "ask_plainly",
        replica:         "Work as {npc:job_offer}. I have that going. It pays {npc:job_pay}, and it is hard work.",
        replicaIndirect: "I tell {you:name} I have work going as {npc:job_offer}, that it pays {npc:job_pay}, and that it is hard work.",
        replicaHeard:    "{npc:name} says they have work going as {npc:job_offer}, that it pays {npc:job_pay}, and that it is hard work.",

        new PlayerOption("plain_accept_pay", "accept the pay as stated",
            "That is the rate. I will not haggle over it.",
            "I tell {npc:name} I will not haggle over the rate.",
            End("plain_accept_end", 2,
                "You look like a worker. Come, there is work for willing hands.",
                "I tell {you:name} they look like a worker, and that there is work for willing hands.",
                "I have nothing for you today. Try elsewhere.",
                "I tell {you:name} I have nothing for them today.")),

        new PlayerOption("plain_ask_days", "ask what the days actually look like",
            "What does a day of it involve?",
            "I ask {npc:name} what a day of it involves.",
            PlainDays()),

        new PlayerOption("plain_ask_why_open", "ask why the post is going begging",
            "Why is it still open? A post open this long is open for a reason.",
            "I ask {npc:name} why the post is still open.",
            PlainWhyOpen()));

    private static NpcLineNode PlainDays() => new(
        nodeId:          "plain_days",
        replica:         "First light to last, at {npc:workplace}, whatever the weather. {npc:opinion_work}",
        replicaIndirect: "I tell {you:name} it runs from first light to last at {npc:workplace} whatever the weather, and that of the work I think {npc:opinion_work}.",
        replicaHeard:    "{npc:name} says it runs from first light to last whatever the weather, and tells me what they think of the work.",

        new PlayerOption("days_say_done_worse", "say you have worked worse",
            "I have worked worse for less. That does not frighten me.",
            "I tell {npc:name} I have worked worse for less.",
            End("days_worse_end", 3,
                "Then you will do. I would rather have a hand who is used to it.",
                "I tell {you:name} they will do, since I would rather have a hand who is used to it.",
                "Everyone claims to have worked worse. I have heard it too often.",
                "I tell {you:name} everyone claims to have worked worse.")),

        new PlayerOption("days_ask_learn", "ask whether you would learn anything from it",
            "Would I learn anything from it that I do not already know?",
            "I ask {npc:name} whether I would learn anything from it.",
            End("days_learn_end", 3,
                "You would. Nobody asks me that; they ask about the pay. I will take you on.",
                "I tell {you:name} they would, and that nobody asks me that but only about the pay.",
                "You would come out of it tired. That is all I promise.",
                "I tell {you:name} they would come out of it tired, and that is all I promise.")));

    private static NpcLineNode PlainWhyOpen() => new(
        nodeId:          "plain_why_open",
        replica:         "Because the last one left in the middle of it and I was short-handed at the worst time.",
        replicaIndirect: "I tell {you:name} the last one left in the middle of it and left me short-handed at the worst time.",
        replicaHeard:    "{npc:name} says the last one left in the middle of it and left them short-handed at the worst time.",

        new PlayerOption("why_promise_stay", "promise you would see it through",
            "You will get the opposite from me. I finish what I start.",
            "I tell {npc:name} I finish what I start.",
            End("why_stay_end", 3,
                "We will see whether the work matches the promise. There is work to be had.",
                "I tell {you:name} we will see whether the work matches the promise.",
                "So did he, until the morning he was not there.",
                "I tell {you:name} that so did he, until the morning he was not there.")),

        new PlayerOption("why_ask_what_broke", "ask what drove them off",
            "What drove him off? I would rather know before I take it on.",
            "I ask {npc:name} what drove him off.",
            WhyWhatBroke()));

    private static NpcLineNode WhyWhatBroke() => new(
        nodeId:          "why_what_broke",
        replica:         "I was hard on him, and he was young, and I had not the patience the work needed. The other half was him.",
        replicaIndirect: "I tell {you:name} I was hard on him and had not the patience the work needed, and that the other half of it was him.",
        replicaHeard:    "{npc:name} says they were hard on him and had not the patience the work needed, and that the other half of it was him.",

        new PlayerOption("broke_say_fair", "say that was fairly told",
            "That is fairly told. Most masters would put all of it on the lad.",
            "I tell {npc:name} most masters would put all of it on the lad.",
            End("broke_fair_end", 4,
                "I have had time to think about it. Come, and I will try to do better this time.",
                "I tell {you:name} I have had time to think about it, and will try to do better this time.",
                "Fairly told, and none of your business. Are you working or judging?",
                "I ask {you:name} whether they are working or judging.")),

        new PlayerOption("broke_ask_patience", "ask whether the patience is there now",
            "Is the patience there now? I would rather know what I am walking into.",
            "I ask {npc:name} whether the patience is there now.",
            End("broke_patience_end", 4,
                "That is a fair question and a hard one to answer. Some days. Start tomorrow.",
                "I tell {you:name} it is there some days, and to start tomorrow.",
                "You will take the post as it is or leave it. I will not be questioned in my own yard.",
                "I tell {you:name} I will not be questioned in my own yard.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — show you are willing and able (rich)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode ShowWilling() => new(
        nodeId:          "show_willing",
        replica:         "Strong and willing. So is every third person who comes here, and half of them leave by the second week.",
        replicaIndirect: "I tell {you:name} every third person who comes here is strong and willing, and half of them leave by the second week.",
        replicaHeard:    "{npc:name} says every third person who comes there is strong and willing, and half of them leave by the second week.",

        new PlayerOption("willing_stand_by", "stand by what you said",
            "Then judge me by the second week, not by today.",
            "I tell {npc:name} to judge me by the second week, not by today.",
            End("willing_stand_end", 2,
                "We will see whether the work matches the promise. There is work to be had.",
                "I tell {you:name} we will see whether the work matches the promise.",
                "Willing or not, I have nothing for you just now.",
                "I tell {you:name} I have nothing for them just now.")),

        new PlayerOption("willing_name_experience", "name what you have actually done",
            "Then I will be plainer. This is the work I have actually done.",
            "I tell {npc:name} what work I have actually done.",
            WillingExperience()),

        new PlayerOption("willing_admit_green", "admit you are new to this kind of work",
            "I have not done this work before. I learn quickly.",
            "I tell {npc:name} I have not done this work before but learn quickly.",
            WillingGreen()));

    private static NpcLineNode WillingExperience() => new(
        nodeId:          "willing_experience",
        replica:         "That is more than most can list without inventing. And you would take {npc:job_offer} after that?",
        replicaIndirect: "I tell {you:name} that is more than most can list without inventing, and ask whether they would still take {npc:job_offer}.",
        replicaHeard:    "{npc:name} says that is more than most can list without inventing, and asks whether I would still take {npc:job_offer}.",

        new PlayerOption("exp_no_pride", "say you have no pride about the work",
            "I have no pride about it. I would rather have work than not.",
            "I tell {npc:name} I would rather have work than not.",
            End("exp_pride_end", 3,
                "That is the answer I wanted. Start when you like.",
                "I tell {you:name} that is the answer I wanted, and to start when they like.",
                "No pride and no standards. I will pass.",
                "I tell {you:name} that no pride means no standards.")),

        new PlayerOption("exp_want_foot_in", "say you want a foot in the door here",
            "I would take it to get a start here. I will not pretend otherwise.",
            "I tell {npc:name} I would take it to get a start here.",
            End("exp_foot_end", 3,
                "You are honest about your own scheming. I can work with that. The post is yours.",
                "I tell {you:name} the post is theirs, since I can work with honest scheming.",
                "A start here, and gone the moment something better comes. No.",
                "I tell {you:name} they would be gone the moment something better came.")));

    private static NpcLineNode WillingGreen() => new(
        nodeId:          "willing_green",
        replica:         "At least you said so. I have had men swear they knew the work and ruin a day proving otherwise.",
        replicaIndirect: "I tell {you:name} I have had men swear they knew the work and ruin a day proving otherwise.",
        replicaHeard:    "{npc:name} says they have had men swear they knew the work and ruin a day proving otherwise.",

        new PlayerOption("green_ask_teach", "ask them to show you once and no more",
            "Show me once. If I need showing twice, send me off and keep the day's pay.",
            "I ask {npc:name} to show me once, and to keep the day's pay if I need showing twice.",
            End("green_teach_end", 3,
                "Once, with your own pay against it. That is a wager I will take.",
                "I tell {you:name} that is a wager I will take.",
                "I have no time to teach. Come back when someone else has trained you.",
                "I tell {you:name} I have no time to teach.")),

        new PlayerOption("green_offer_low", "offer to be paid less until you are worth it",
            "Pay me under the rate until I am worth it. I will not argue the difference.",
            "I offer {npc:name} to be paid under the rate until I am worth it.",
            End("green_low_end", 3,
                "Under the rate by your own asking. You will be on full pay sooner than you think.",
                "I tell {you:name} they will be on full pay sooner than they think.",
                "If you will work for under, you will work for anyone. That tells me what you are worth.",
                "I tell {you:name} that someone who will work for under will work for anyone.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — ask about the post itself (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskAboutPost() => new(
        nodeId:          "ask_about_post",
        replica:         "The post is what it says: {npc:job_offer}, {npc:labour}, and it pays {npc:job_pay}.",
        replicaIndirect: "I tell {you:name} the post is {npc:job_offer} — {npc:labour} — and that it pays {npc:job_pay}.",
        replicaHeard:    "{npc:name} says the post is {npc:job_offer}, describes the work of it, and says it pays {npc:job_pay}.",

        new PlayerOption("post_ask_hours", "ask how long a stint runs",
            "How long a stint would you want from me?",
            "I ask {npc:name} how long a stint they want from me.",
            End("post_hours_end", 2,
                "As long as you will give, and no less than a few days. We will settle it inside.",
                "I tell {you:name} as long as they will give, and no less than a few days.",
                "Longer than you would give, by the sound of you. Off with you.",
                "I tell {you:name} it would be longer than they would give.")),

        new PlayerOption("post_ask_pay_fair", "ask whether the pay is what others get",
            "Is that what the others get, or is that the newcomer's rate?",
            "I ask {npc:name} whether that is the rate or the newcomer's rate.",
            PostPayFair()),

        new PlayerOption("post_say_suits", "say it suits you and ask to start",
            "That suits me. When can I start?",
            "I tell {npc:name} it suits me, and ask when I can start.",
            End("post_suits_end", 2,
                "Now, if you are up to it. Come along.",
                "I tell {you:name} they may start now if they are up to it.",
                "You can start looking elsewhere. I have nothing.",
                "I tell {you:name} they can start looking elsewhere.")));

    private static NpcLineNode PostPayFair() => new(
        nodeId:          "post_pay_fair",
        replica:         "That is a sharp question to put to a man in his own yard. It is the rate, the same as the last one had.",
        replicaIndirect: "I tell {you:name} that is a sharp question to put to a man in his own yard, and that it is the rate the last one had.",
        replicaHeard:    "{npc:name} says that is a sharp question to put to a man in his own yard, and that it is the rate the last one had.",

        new PlayerOption("pay_accept_word", "take them at their word",
            "Then I will take your word on it and say no more.",
            "I tell {npc:name} I will take their word on it.",
            End("pay_word_end", 3,
                "You will find that I keep it. Come, there is work waiting.",
                "I tell {you:name} they will find that I keep it.",
                "You take my word and question it in the same breath. No.",
                "I tell {you:name} they take my word and question it in the same breath.")),

        new PlayerOption("pay_say_why_ask", "explain why you asked",
            "I ask because I was paid the newcomer's rate for a year once.",
            "I tell {npc:name} I was paid the newcomer's rate for a year once.",
            End("pay_why_end", 3,
                "Then someone used you badly. Not here. The rate is the rate.",
                "I tell {you:name} someone used them badly, and that the rate is the rate here.",
                "Then take it up with whoever did that, not with me.",
                "I tell {you:name} to take it up with whoever did that.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — offer a day for nothing (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode OfferTrial() => new(
        nodeId:          "offer_trial",
        replica:         "A day for nothing. That is either desperation or confidence, and I have been fooled by both.",
        replicaIndirect: "I tell {you:name} a day for nothing is either desperation or confidence, and that I have been fooled by both.",
        replicaHeard:    "{npc:name} says a day for nothing is either desperation or confidence, and that they have been fooled by both.",

        new PlayerOption("trial_confidence", "say it is confidence",
            "Confidence. Watch me for a day and you will not send me off.",
            "I tell {npc:name} it is confidence, and that a day of watching will settle it.",
            End("trial_confidence_end", 2,
                "Then I will watch. Come at first light and do not be late. That is the whole test.",
                "I tell {you:name} to come at first light and not be late, since that is the whole test.",
                "I have seen confidence. It is endurance I am short of. Move along.",
                "I tell {you:name} it is endurance I am short of, not confidence.")),

        new PlayerOption("trial_admit_need", "admit you need the work",
            "Desperation, then. I need the work and I would rather earn it than beg.",
            "I tell {npc:name} I need the work and would rather earn it than beg.",
            TrialNeed()),

        new PlayerOption("trial_no_obligation", "make clear they would owe you nothing",
            "If I am no good, you owe me nothing and I will go without argument.",
            "I tell {npc:name} they owe me nothing if I am no good.",
            End("trial_no_obligation_end", 2,
                "No argument either way. I can agree to that. Come, then.",
                "I tell {you:name} I can agree to no argument either way.",
                "You say that now. They always find something to say afterwards.",
                "I tell {you:name} they always find something to say afterwards.")));

    private static NpcLineNode TrialNeed() => new(
        nodeId:          "trial_need",
        replica:         "I stood on that side of a yard myself, a long time ago, and I have not forgotten it.",
        replicaIndirect: "I tell {you:name} I stood on that side of a yard myself long ago, and have not forgotten it.",
        replicaHeard:    "{npc:name} says they stood on that side of a yard themselves long ago, and have not forgotten it.",

        new PlayerOption("need_thank", "thank them for hearing you out",
            "Then you know what it took to ask. Thank you for hearing it.",
            "I thank {npc:name} for hearing what it took to ask.",
            End("need_thank_end", 3,
                "I do. The work is yours, and we will say nothing more about how it started.",
                "I tell {you:name} the work is theirs, and that we will say no more about how it started.",
                "Knowing it does not oblige me. I have a holding to run.",
                "I tell {you:name} that knowing it does not oblige me.")),

        new PlayerOption("need_press_case", "press your case while they are listening",
            "Then take me on. Nobody will work harder for the first month.",
            "I ask {npc:name} to take me on, since nobody will work harder for the first month.",
            End("need_press_end", 3,
                "The first month. We will see about the second. You are taken on.",
                "I tell {you:name} we will see about the second month, and that they are taken on.",
                "The first month, and then what? I need hands, not enthusiasm.",
                "I tell {you:name} I need hands, not enthusiasm.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Opening = new(
        nodeId:          "opening",
        replica:         "{you:name}. What brings you to me?",
        replicaIndirect: "I greet {you:name} and ask what brings them to me.",
        replicaHeard:    "{npc:name} greets me and asks what brings me to them.",

        new PlayerOption("ask_plainly", "ask plainly for the work and its pay",
            "I am after work as {npc:job_offer}, {npc:name}. What does it pay?",
            "I ask {npc:name} for work as {npc:job_offer}, and what it pays.",
            AskPlainly()),

        new PlayerOption("show_willing", "show you are willing and able",
            "I am strong and willing. Put me to any task you like.",
            "I tell {npc:name} I am strong and willing, and to put me to any task.",
            ShowWilling()),

        new PlayerOption("ask_about_post", "ask what the post involves before asking for it",
            "Before I ask for it, what does {npc:job_offer} involve here?",
            "I ask {npc:name} what {npc:job_offer} involves here.",
            AskAboutPost()),

        new PlayerOption("offer_trial", "offer to work a day for nothing first",
            "Give me a day of it for nothing. Judge me on that.",
            "I offer {npc:name} a day of the work for nothing, to be judged on it.",
            OfferTrial()));

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
