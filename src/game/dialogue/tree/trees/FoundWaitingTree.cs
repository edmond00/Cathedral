using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.ModiMentis;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// The householder comes in at nightfall and finds the player sitting in their house — the one
/// trespass that is <i>only</i> presence. Nothing was taken, nothing forced: the player waited where
/// they had no business waiting, and the hour came round on them.
///
/// <para>Shaped differently from <see cref="CaughtRedHandedTreeFactory"/>, because the question is
/// different. There the witness wants to know why you did it; here the householder wants to know
/// why you are still here, and whether to keep a stranger under their roof until morning:</para>
///   shelter → ask to be kept the night (the deepest route)
///   leave   → claim somebody let you in
///   pardon  → beg their pardon and put yourself at their mercy
///   go      → say you will go — no roll, the door
///
/// <para><b>What is at stake is the house, not a fight.</b> Success makes the whole building
/// tolerated for the rest of the visit (<see cref="TrespassToleratedOutcome"/>); failure is being
/// shown the door, which ends the visit (<see cref="ShownTheDoorOutcome"/>). Every branch end is on
/// the <see cref="BranchDifficulty.Grave"/> ladder, one six over Hard: asking to be kept is a larger
/// thing than asking to be let off.</para>
///
/// <para>Opened only by <see cref="Scene.ConfrontationKind.FoundWaiting"/>, whichever way that
/// confrontation turns into a conversation. Never registered — built per confrontation, like the
/// caught-red-handed trees, so its outcome instances are fresh each time.</para>
/// </summary>
public sealed class FoundWaitingTree : DialogueTree
{
    public const string Id = "found_waiting";

    private readonly NpcLineNode _entry;

    public override string TreeId           => Id;
    public override string DisplayName      => "Found Waiting";
    public override string Description      => "being found in somebody's house at night by the householder";
    public override string NpcDescription   => "having come home at nightfall to find a stranger sitting in your house, and confronting them";
    public override string AssociatedVerbId => "";   // triggered programmatically, not by a verb
    public override NpcLineNode EntryNode   => _entry;

    public override IReadOnlyList<Outcome> SuccessOutcomes { get; }
    public override IReadOnlyList<Outcome> FailureOutcomes { get; }

    // The tree is triggered programmatically — IsAvailable is never checked.
    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => false;

    public FoundWaitingTree()
    {
        SuccessOutcomes = new Outcome[] { new TrespassToleratedOutcome() };
        FailureOutcomes = new Outcome[] { new ShownTheDoorOutcome() };

        ResolutionNode End(string id, int depth,
                           string success, string successIndirect,
                           string failure, string failureIndirect,
                           params System.Type[] lessons) => new(
            nodeId:                 id,
            difficulty:             BranchDifficulty.Grave(depth),
            successReplica:         success,
            successReplicaIndirect: successIndirect,
            failureReplica:         failure,
            failureReplicaIndirect: failureIndirect,
            mode:                   ResolutionMode.DiceCheck,
            topic:                  null,
            lessons:                lessons);

        // ══════════════════════════════════════════════════════════════
        //  A — ask to be kept the night (deepest)
        // ══════════════════════════════════════════════════════════════

        var shelterNeedResult = End("shelter_need", 2,
            "Nowhere else. I have been that myself. Stay by the hearth, and be gone at first light.",
            "I tell {you:name} I have had nowhere else myself, and that they may stay until first light.",
            "Nowhere else is not my trouble. Out, before I wake the house.",
            "I tell {you:name} that having nowhere else is not my trouble, and to get out.",
                typeof(HumilityModusMentis), typeof(BeggaryModusMentis));

        var shelterWorkResult = End("shelter_work", 2,
            "Work for it, then. There is wood to split at dawn, and I will hold you to it.",
            "I tell {you:name} there is wood to split at dawn, and that I will hold them to it.",
            "I have hands enough in this house. Go and find work somewhere else.",
            "I tell {you:name} I have hands enough, and to find work somewhere else.",
                typeof(OathmakingModusMentis));

        var roadPilgrimResult = End("road_pilgrim", 3,
            "A pilgrim. Then I will not be the one who turned you from the road. Sleep, and go on in the morning.",
            "I tell {you:name} I will not be the one who turned a pilgrim from the road, and that they may sleep.",
            "Every vagrant is a pilgrim after dark. Take your holy road out of my door.",
            "I tell {you:name} every vagrant is a pilgrim after dark, and to take their road out of my door.",
                typeof(PilgrimageModusMentis), typeof(PietyModusMentis));

        var roadHonestResult = End("road_honest", 3,
            "At least you do not pretend. Stay the night. Tomorrow you can not know where you are going somewhere else.",
            "I tell {you:name} that at least they do not pretend, and that they may stay the night.",
            "A stranger with no road is the last thing I want under my roof. Go.",
            "I tell {you:name} a stranger with no road is the last thing I want under my roof.",
                typeof(PlainDealingModusMentis), typeof(WayfaringModusMentis));

        var shelterRoad = new NpcLineNode(
            nodeId:          "shelter_road",
            replica:         "Far, by the look of you. And where are you bound, that it brings you through my house?",
            replicaIndirect: "I tell {you:name} they look to have come far, and ask where they are bound.",
            replicaHeard:    "{npc:name} says I look to have come far, and asks where I am bound.",
            new PlayerOption("road_pilgrim", "name a holy place you are bound for",
                "There is a shrine I mean to reach before the season turns. I only need the night.",
                "I tell {npc:name} I mean to reach a shrine before the season turns, and only need the night.",
                roadPilgrimResult),
            new PlayerOption("road_honest", "admit you do not know",
                "I do not know. Wherever the road goes next. Tonight it went here.",
                "I tell {npc:name} I do not know where I am bound, and that tonight the road went here.",
                roadHonestResult));

        var shelterWhy = new NpcLineNode(
            nodeId:          "shelter_why",
            replica:         "My house is not an inn. Why should I keep a stranger under my roof?",
            replicaIndirect: "I tell {you:name} my house is not an inn, and ask why I should keep a stranger under my roof.",
            replicaHeard:    "{npc:name} says their house is not an inn, and asks why they should keep a stranger.",
            new PlayerOption("shelter_need", "say you had nowhere else to go",
                "Because I had nowhere else, and the night was coming down.",
                "I tell {npc:name} I had nowhere else, and that the night was coming down.",
                shelterNeedResult),
            new PlayerOption("shelter_road", "tell them where you have come from",
                "I have been on the road since before dawn. Let me tell you where from.",
                "I tell {npc:name} I have been on the road since before dawn.",
                shelterRoad),
            new PlayerOption("shelter_work", "offer to work for your keep at first light",
                "Keep me tonight and I will work for it in the morning, whatever you need done.",
                "I offer {npc:name} to work for my keep in the morning, whatever they need done.",
                shelterWorkResult));

        // ══════════════════════════════════════════════════════════════
        //  B — claim somebody let you in
        // ══════════════════════════════════════════════════════════════

        var leaveNameResult = End("leave_name", 2,
            "Them. They have always been too soft with strangers. Stay, then, and I will have a word with them.",
            "I grumble that they were always too soft with strangers, and tell {you:name} they may stay.",
            "Nobody in this house would say so. You are lying in my own hall. Out.",
            "I tell {you:name} nobody in this house would say so, and to get out.",
                typeof(MythomaniaModusMentis));

        var leaveDoorResult = End("leave_door", 2,
            "Open. Then that is my fault as much as yours. Stay till morning, and I will bar it after you.",
            "I allow that the open door is my fault as much as theirs, and tell {you:name} they may stay till morning.",
            "A door left open is not an invitation. Go out the way you came.",
            "I tell {you:name} an open door is no invitation, and to go out the way they came.",
                typeof(DramaturgyModusMentis));

        var leaveWho = new NpcLineNode(
            nodeId:          "leave_who",
            replica:         "Nobody lets strangers in here but me. Who was it?",
            replicaIndirect: "I tell {you:name} nobody lets strangers in here but me, and ask who it was.",
            replicaHeard:    "{npc:name} says nobody lets strangers in there but them, and asks who it was.",
            new PlayerOption("leave_name", "say one of the household told you to wait",
                "One of your household told me to wait here for you. I took them at their word.",
                "I tell {npc:name} one of their household told me to wait here for them.",
                leaveNameResult),
            new PlayerOption("leave_door", "say the door stood open and nobody answered",
                "The door stood open and nobody answered. I thought it was meant.",
                "I tell {npc:name} the door stood open, nobody answered, and I thought it was meant.",
                leaveDoorResult));

        // ══════════════════════════════════════════════════════════════
        //  C — beg their pardon
        // ══════════════════════════════════════════════════════════════

        var pardonFlatterResult = End("pardon_flatter", 2,
            "You have a tongue on you. Well. It is a good house. You may as well sleep in it.",
            "I tell {you:name} they have a tongue on them, and that they may as well sleep in my house.",
            "Sweet words at midnight from somebody sitting in my house. Out.",
            "I tell {you:name} I want no sweet words at midnight, and to get out.",
                typeof(FlatteryModusMentis), typeof(CourtesyModusMentis));

        var pardonDeferResult = End("pardon_defer", 2,
            "Then I will be merciful, since you ask it so plainly. Stay the night.",
            "I tell {you:name} I will be merciful since they ask it plainly, and that they may stay the night.",
            "My mercy runs to the door. Go through it.",
            "I tell {you:name} my mercy runs as far as the door, and to go through it.",
                typeof(DeferenceModusMentis));

        var pardon = new NpcLineNode(
            nodeId:          "pardon",
            replica:         "Pardon is cheap at this hour. What is it you want of me?",
            replicaIndirect: "I tell {you:name} pardon is cheap at this hour, and ask what they want of me.",
            replicaHeard:    "{npc:name} says pardon is cheap at this hour, and asks what I want of them.",
            new PlayerOption("pardon_flatter", "praise the house and the one who keeps it",
                "Only to rest a little longer in the best-kept house I have seen on the road.",
                "I tell {npc:name} I only want to rest in the best-kept house I have seen on the road.",
                pardonFlatterResult),
            new PlayerOption("pardon_defer", "put yourself entirely at their mercy",
                "Nothing I have a right to. Whatever you decide, I will abide by it.",
                "I tell {npc:name} I want nothing I have a right to, and will abide by whatever they decide.",
                pardonDeferResult));

        // ══════════════════════════════════════════════════════════════
        //  Entry: the householder finds the player
        // ══════════════════════════════════════════════════════════════

        _entry = new NpcLineNode(
            nodeId:          "found_waiting",
            replica:         "It is the dead of night and you are sitting in my house. What are you doing here?",
            replicaIndirect: "I tell {you:name} it is the dead of night and they are sitting in my house, and ask what they are doing here.",
            replicaHeard:    "{npc:name} says it is the dead of night and I am sitting in their house, and asks what I am doing there.",
            new PlayerOption("shelter", "ask to be kept the night",
                "I need a roof for the night. I ask you for this one.",
                "I ask {npc:name} for a roof for the night, theirs.",
                shelterWhy),
            new PlayerOption("leave", "say somebody let you in",
                "I was let in. I did not think I was unwelcome.",
                "I tell {npc:name} I was let in and did not think I was unwelcome.",
                leaveWho),
            new PlayerOption("pardon", "beg their pardon",
                "I beg your pardon. I should not have stayed so long.",
                "I beg {npc:name}'s pardon, and say I should not have stayed so long.",
                pardon),
            new PlayerOption("go", "say you will go",
                "I am going. I will not trouble you further.",
                "I tell {npc:name} I am going and will not trouble them further.",
                new NpcLineNode(
                    nodeId:          "go",
                    replica:         "Going. At this hour, through my house. Which way did you come in?",
                    replicaIndirect: "I ask {you:name} which way they came in, if they mean to go through my house at this hour.",
                    replicaHeard:    "{npc:name} asks which way I came in, if I mean to go through their house at this hour.",
                    new PlayerOption("go_quietly", "show them, and leave without another word",
                        "The way I came. I will show you, and you can bar it behind me.",
                        "I tell {npc:name} I will go the way I came and they can bar it behind me.",
                        new ResolutionNode(
                            nodeId:                 "go_quietly",
                            difficulty:             1,
                            successReplica:         "Then go, and do not come back tonight.",
                            successReplicaIndirect: "I tell {you:name} to go, and not to come back tonight.",
                            failureReplica:         "Then go, and do not come back tonight.",
                            failureReplicaIndirect: "I tell {you:name} to go, and not to come back tonight.",
                            mode:                   ResolutionMode.ForceFailure)))));
    }
}
