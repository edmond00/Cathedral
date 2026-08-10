using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that flags the NPC as having agreed to hire the player. Promotes the
/// <see cref="NpcEntity.PendingJobOffer"/> (chosen when the REQUEST_JOB verb succeeded) to
/// <see cref="NpcEntity.JobRequest"/> so the game controller can open the work menu immediately
/// after the dialogue session ends (mirrors <see cref="OpenTradeMenuOutcome"/>).
/// </summary>
public class OpenJobMenuOutcome : Outcome
{
    public OpenJobMenuOutcome()
        : base("NPC agrees to take you on for the work", OutcomeSeverity.Positive, "") { }


    
    // Ordinary Outcome, like every other consequence. The chip text is settled in Apply because a
    // conversation's effect only knows its own wording once it has seen this NPC's before/after
    // state; ShowInUI stays false when nothing actually changed, which is what returning null used
    // to mean. Trees hand out a fresh set per access precisely so this per-conversation state is safe.
    public override bool ShowInUI => Reported;

    public override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        npc.JobRequest = npc.PendingJobOffer;

        // The offer is chosen by RequestJobVerb.SuccessReports before the dialogue opens. If it is
        // missing the check succeeded but the work menu will silently not open, which reads in-game
        // as the NPC refusing — say so rather than leaving it invisible.
        if (npc.JobRequest == null)
        {
            Console.Error.WriteLine(
                $"OpenJobMenuOutcome: {npc.DisplayName} agreed to hire but has no PendingJobOffer — " +
                "the work menu will not open.");
            return;
        }

        Report($"{npc.DisplayName} takes you on as {npc.JobRequest.WithArticle()}");
    }
}
