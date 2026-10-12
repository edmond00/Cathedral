using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.ModiMentis;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Ask to Be Led" — asking a trader met away from their premises to take you there.
///
/// <para>A trader's goods are kept where they work, and trade happens nowhere else (<c>TradeGate</c>),
/// so the smith met in the street can be talked to and not dealt with. This is asking to be walked to
/// the forge. Succeeding takes the player there at the first hour the trader keeps it, the trader in
/// focus — see <see cref="LedToPremisesOutcome"/>.</para>
///
/// <para>Behind the introduction: a favour is asked of somebody you know. The
/// <see cref="BranchDifficulty.Easy"/> ladder — it is a small favour, and a trader wants custom. A
/// refusal costs nothing.</para>
/// </summary>
public class AskToBeLedTree : DialogueTree
{
    public override string TreeId           => "ask_to_be_led";
    public override string DisplayName      => "Ask to Be Led";
    public override string Description      => "asking them to take you to where they keep their goods";
    public override string NpcDescription   => "being asked to take this person to where you keep your goods";
    public override string AssociatedVerbId => "ask_to_be_led";

    /// <summary>What succeeding teaches: finding one's way by asking it of those who know.</summary>
    public override string? GrantedModusMentisId => "wayfaring";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new LedToPremisesOutcome() };
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[] { new AffinityIncrementOutcome(0) };

    /// <summary>A trader who keeps premises somewhere, asked by somebody they know and do not count an enemy.</summary>
    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => npc.Trades && npc.TradeAreaIds.Count > 0
        && !npc.AffinityTable.IsStranger(partyMemberId)
        && !npc.AffinityTable.IsEnemy(partyMemberId);

    private static ResolutionNode End(string id, int depth, string success, string successIndirect,
                                      string failure, string failureIndirect, params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    private static readonly NpcLineNode ForNow = new(
        nodeId:          "now_pressed",
        replica:         "Now? I have my own errands. What is the hurry?",
        replicaIndirect: "I ask them what the hurry is, since I have errands of my own.",
        replicaHeard:    "They ask me what the hurry is, since they have errands of their own.",

            new PlayerOption("now_need", "say I need it today",
                "I need {npc:sells} today, not tomorrow.",
                "I tell them I need {npc:sells} today.",
                End("now_need_end", 2,
                    "Today, is it. Then come along, and keep up.",
                    "I tell them to come along and keep up.",
                    "Everyone needs everything today. Come by yourself when you find the way.",
                    "I tell them to find the way by themselves.",
                    typeof(PatienceModusMentis), typeof(WayfaringModusMentis))),

            new PlayerOption("now_carry", "offer to carry something on the way",
                "I will carry that for you while we walk.",
                "I offer to carry their load on the way.",
                End("now_carry_end", 2,
                    "Will you now. Here, take this end, and mind the step.",
                    "I hand them one end of my load.",
                    "I carry my own things. Nobody touches them but me.",
                    "I tell them nobody carries my things but me.",
                    typeof(CourtesyModusMentis), typeof(DeferenceModusMentis))));

    private static readonly NpcLineNode ForPraise = new(
        nodeId:          "praise_pressed",
        replica:         "The best hereabouts? Who told you that?",
        replicaIndirect: "I ask them who told them that.",
        replicaHeard:    "They ask me who told me that.",

            new PlayerOption("praise_neighbour", "say the neighbours speak of it",
                "Your neighbours speak of nobody else.",
                "I tell them their neighbours speak of nobody else.",
                End("praise_neighbour_end", 2,
                    "Do they. Well. It does no harm to show you, then. This way.",
                    "I agree to show them the way.",
                    "My neighbours speak of nothing but their own troubles. Try another.",
                    "I tell them my neighbours speak of nothing but their troubles.",
                    typeof(FlatteryModusMentis), typeof(StreetwiseModusMentis))),

            new PlayerOption("praise_look", "say it shows in how they carry themselves",
                "Nobody told me. It shows in the way you carry yourself.",
                "I tell them it shows in the way they carry themselves.",
                End("praise_look_end", 2,
                    "Ha. A good eye. Come and see whether it was right.",
                    "I tell them to come and see.",
                    "Sweet words cost nothing. My wares cost coin.",
                    "I tell them sweet words cost nothing.",
                    typeof(AppraisalModusMentis), typeof(BanterModusMentis))));

    private static readonly NpcLineNode ForCoin = new(
        nodeId:          "coin_pressed",
        replica:         "Coin talks. How much of it have you?",
        replicaIndirect: "I ask them how much coin they have.",
        replicaHeard:    "They ask me how much coin I have.",

            new PlayerOption("coin_show", "show the purse",
                "Enough. Look.",
                "I show them my purse.",
                End("coin_show_end", 2,
                    "That will do. Follow me, and keep that out of sight in the street.",
                    "I tell them to follow me and keep the purse hidden.",
                    "That? That buys a nail. Come back when it is heavier.",
                    "I tell them the purse is too light.",
                    typeof(BargainingModusMentis), typeof(EnterpriseModusMentis))),

            new PlayerOption("coin_depends", "say it depends on the goods",
                "Enough, if what you have is worth it.",
                "I tell them it depends whether the goods are worth it.",
                End("coin_depends_end", 2,
                    "Fair. Come and judge for yourself, then.",
                    "I tell them to come and judge for themselves.",
                    "Everything I make is worth it. If you doubt it, buy elsewhere.",
                    "I tell them to buy elsewhere if they doubt me.",
                    typeof(BargainingModusMentis), typeof(CuriosityModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "My goods are at {npc:workplace}, not in my pockets. You want to see them?",
        replicaIndirect: "I tell them my goods are at {npc:workplace}, not in my pockets.",
        replicaHeard:    "They tell me their goods are at {npc:workplace}, not in their pockets.",

        new PlayerOption("for_now", "ask them to take me there now",
            "Will you take me there now?",
            "I ask them to take me there now.",
            ForNow),

        new PlayerOption("for_praise", "say I heard theirs is the best work hereabouts",
            "I have heard yours is the best work hereabouts.",
            "I tell them I have heard theirs is the best work hereabouts.",
            ForPraise),

        new PlayerOption("for_coin", "say I have coin to spend with them",
            "I have coin to spend, and I would spend it with you.",
            "I tell them I have coin to spend with them.",
            ForCoin));

    public override NpcLineNode EntryNode => Approach;
}
