using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Crafting;
using Cathedral.Game.Narrative.ModiMentis;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Ask to use the workshop" — the player asks a master for the use of their equipment: the anvil and
/// the hearth of a forge, the bench of a carpenter's shop, the loom of a weaving shed. Success gives the
/// leave for good (<see cref="WorkshopAuthorizedOutcome"/>), for every station of that workshop; failure
/// is a refusal for now, and asking again is another conversation.
///
/// <para>Only <see cref="Scene.Verbs.AskToUseWorkshopVerb"/> opens it, and that verb is offered only to
/// somebody holding a trade that works a station of this workshop — a master is not asked for the loan of
/// an anvil by somebody who could not use one.</para>
///
/// <para>Three ways to ask — plainly, by naming the trade, or by offering something for it — on the
/// <see cref="BranchDifficulty.Hard"/> ladder: a master's tools are their living, and a stranger at the
/// anvil is a risk to both.</para>
/// </summary>
public sealed class AskToUseWorkshopTree : DialogueTree
{
    public override string TreeId           => Scene.Verbs.AskToUseWorkshopVerb.TreeId;
    public override string DisplayName      => "Ask To Use Workshop";
    public override string Description      => "asking a master for the use of their workshop and its equipment";
    public override string NpcDescription   => "being asked by someone with a trade for the use of your workshop and your tools";
    public override string AssociatedVerbId => "ask_to_use_workshop";

    /// <summary>What succeeding teaches: putting a request to somebody who owes you nothing.</summary>
    public override string? GrantedModusMentisId => "enterprise";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new WorkshopAuthorizedOutcome() };
    public override IReadOnlyList<Outcome> FailureOutcomes => System.Array.Empty<Outcome>();

    private static ResolutionNode End(string id, int depth,
                                      string success, string successIndirect,
                                      string failure, string failureIndirect,
                                      params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Hard(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    // ── A — ask plainly ──────────────────────────────────────────────────────

    private static NpcLineNode AskPlainly() => new(
        nodeId:          "ask_plainly",
        replica:         "My tools are my living. Why should I let a stranger at them?",
        replicaIndirect: "I tell {you:name} my tools are my living, and ask why I should let a stranger at them.",
        replicaHeard:    "{npc:name} says their tools are their living, and asks why they should let a stranger at them.",

        new PlayerOption("plain_promise_care", "promise to leave everything as you found it",
            "I will leave everything as I found it, swept and cold.",
            "I tell {npc:name} I will leave everything as I found it, swept and cold.",
            End("plain_care_end", 2,
                "Swept and cold, then. The workshop is yours when I am not at it.",
                "I tell {you:name} the workshop is theirs when I am not at it, so long as they leave it swept and cold.",
                "Everyone promises that. I have mended too many broken tools to believe it.",
                "I tell {you:name} everyone promises that, and I have mended too many broken tools to believe it.",
                typeof(PlainDealingModusMentis))),

        new PlayerOption("plain_say_need", "say you have work that cannot wait",
            "I have work that cannot wait, and nowhere else to do it.",
            "I tell {npc:name} I have work that cannot wait and nowhere else to do it.",
            End("plain_need_end", 2,
                "Then do it, and do it properly. I will be watching.",
                "I tell {you:name} to do it, and to do it properly, because I will be watching.",
                "Your hurry is not my concern. Find somewhere else.",
                "I tell {you:name} their hurry is not my concern.",
                typeof(BearingModusMentis))));

    // ── B — name the trade ───────────────────────────────────────────────────

    private static NpcLineNode NameTheTrade() => new(
        nodeId:          "name_trade",
        replica:         "You say you know the work. Plenty say so until they are standing at the bench.",
        replicaIndirect: "I tell {you:name} plenty say they know the work until they are standing at the bench.",
        replicaHeard:    "{npc:name} says plenty claim to know the work until they are standing at the bench.",

        new PlayerOption("trade_offer_show", "offer to show them before they decide",
            "Watch me at it for an hour, then decide.",
            "I ask {npc:name} to watch me at it for an hour before deciding.",
            End("trade_show_end", 2,
                "That is fair. If your hands are good, the workshop is open to you.",
                "I tell {you:name} that is fair, and that if their hands are good the workshop is open to them.",
                "I have no hour to spare on watching strangers.",
                "I tell {you:name} I have no hour to spare on watching strangers.",
                typeof(RoteModusMentis))),

        new PlayerOption("trade_name_master", "say where you learned it",
            "I learned it from a master who would not have let me near the work if I were careless.",
            "I tell {npc:name} I learned the work from a master who would not have let me near it if I were careless.",
            AskWhichMaster()));

    private static NpcLineNode AskWhichMaster() => new(
        nodeId:          "which_master",
        replica:         "And did that master teach you to clean up after yourself?",
        replicaIndirect: "I ask {you:name} whether that master taught them to clean up after themselves.",
        replicaHeard:    "{npc:name} asks whether that master taught me to clean up after myself.",

        new PlayerOption("master_say_first", "say that was the first thing they taught",
            "It was the first thing, and the last thing every day.",
            "I tell {npc:name} it was the first thing they taught me, and the last thing every day.",
            End("master_first_end", 3,
                "Then we were taught alike. Use what you need.",
                "I tell {you:name} we were taught alike, and to use what they need.",
                "A good answer. Too good. I will think on it, and the answer is no for today.",
                "I tell {you:name} the answer is too good, and no for today.",
                typeof(LoyaltyModusMentis))),

        new PlayerOption("master_laugh", "admit they had to say it more than once",
            "They had to say it more than once. I learned in the end.",
            "I admit to {npc:name} the master had to say it more than once, though I learned in the end.",
            End("master_laugh_end", 3,
                "Honest, at least. Come in, and remember it here.",
                "I tell {you:name} that is honest, and to come in and remember it here.",
                "Then learn it again somewhere else first.",
                "I tell {you:name} to learn it again somewhere else first.",
                typeof(PlainDealingModusMentis))));

    // ── C — offer something for it ───────────────────────────────────────────

    private static NpcLineNode OfferSomething() => new(
        nodeId:          "offer_something",
        replica:         "And what do I have out of it, besides the wear on my tools?",
        replicaIndirect: "I ask {you:name} what I have out of it besides the wear on my tools.",
        replicaHeard:    "{npc:name} asks what they have out of it besides the wear on their tools.",

        new PlayerOption("offer_labour", "offer a day's labour in exchange",
            "A day of my labour, at whatever you need doing.",
            "I offer {npc:name} a day of my labour at whatever they need doing.",
            End("offer_labour_end", 2,
                "There is always wood to carry. Done, and the workshop is yours to use.",
                "I tell {you:name} there is always wood to carry, and that the workshop is theirs to use.",
                "I have hands enough. Keep your labour.",
                "I tell {you:name} I have hands enough.",
                typeof(EnduranceModusMentis))),

        new PlayerOption("offer_fuel", "offer to bring your own stock and fuel",
            "I bring my own stock and my own fuel. You lose nothing but the room.",
            "I tell {npc:name} I bring my own stock and fuel, so they lose nothing but the room.",
            End("offer_fuel_end", 2,
                "Then it costs me nothing. Go on.",
                "I tell {you:name} it costs me nothing, and to go on.",
                "The room is what I am short of. No.",
                "I tell {you:name} the room is what I am short of.",
                typeof(PatienceModusMentis))));

    // ── Entry ────────────────────────────────────────────────────────────────

    private static readonly NpcLineNode Opening = new(
        nodeId:          "opening",
        replica:         "{you:name}. You are looking at my workshop as if you want something from it.",
        replicaIndirect: "I tell {you:name} they are looking at my workshop as if they want something from it.",
        replicaHeard:    "{npc:name} says I am looking at their workshop as if I want something from it.",

        new PlayerOption("ask_plainly", "ask plainly for the use of the workshop",
            "I would like the use of your workshop and your tools, {npc:name}.",
            "I ask {npc:name} for the use of their workshop and their tools.",
            AskPlainly()),

        new PlayerOption("name_trade", "say you know the trade",
            "I know this trade. I would only need the equipment.",
            "I tell {npc:name} I know the trade and would only need the equipment.",
            NameTheTrade()),

        new PlayerOption("offer_something", "offer something in return",
            "I would pay for the use of it, one way or another.",
            "I tell {npc:name} I would pay for the use of it, one way or another.",
            OfferSomething()));

    public override NpcLineNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => !npc.AffinityTable.IsEnemy(partyMemberId);
}
