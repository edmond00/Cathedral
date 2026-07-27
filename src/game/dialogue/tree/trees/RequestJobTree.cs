using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Request job" — the player asks a master or reeve to take them on for work.
/// Success opens the work menu after the dialogue; failure turns the player away for now.
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

    private static ResolutionNode Decide(string id, string success, string failure) => new(
        nodeId:         id,
        difficulty:     2,
        successReplica: success,
        failureReplica: failure);

    private static readonly ResolutionNode DecidePlain = Decide(
        "decide_plain",
        "Well, you've the look of a worker. Come — there's labour enough for willing hands.",
        "I've naught for you today. Try your luck elsewhere.");

    private static readonly ResolutionNode DecideWilling = Decide(
        "decide_willing",
        "Bold words. Let's see if your back matches your tongue — there's work to be had.",
        "Willing or no, I've nothing for you just now.");

    private static readonly NpcLineNode Opening = new(
        nodeId:  "opening",
        replica: "Aye, {you:name}? What brings you to me?",
        new PlayerOption("ask_plainly", "ask plainly for work and its pay",
            "I'm after work, {npc:name}. What have you, and what does it pay?", DecidePlain),
        new PlayerOption("show_willing", "show you are willing and able",
            "I'm strong and willing — put me to any task you like.", DecideWilling));

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
