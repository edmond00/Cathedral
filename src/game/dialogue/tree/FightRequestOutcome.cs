using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that flags the NPC as demanding a fight.
/// <see cref="NpcEntity.FightRequestedByDialogue"/> is set to true so the game controller
/// can transition into fight mode immediately after the dialogue session ends.
/// </summary>
public class FightRequestOutcome : Outcome
{
    /// <summary>
    /// Whether the fight is between the two of them alone. False by default — a fight demanded after
    /// a failed reconciliation or a caught theft brings the NPC's friends — and true for a
    /// provocation, where getting somebody on their own is the whole point.
    /// </summary>
    private readonly bool _personal;

    public FightRequestOutcome(bool personal = false)
        : base(personal ? "NPC is goaded into a fight, alone" : "NPC demands a fight",
               OutcomeSeverity.Negative, "") => _personal = personal;


    
    // Ordinary Outcome, like every other consequence. The chip text is settled in Apply because a
    // conversation's effect only knows its own wording once it has seen this NPC's before/after
    // state; ShowInUI stays false when nothing actually changed, which is what returning null used
    // to mean. Trees hand out a fresh set per access precisely so this per-conversation state is safe.
    public override bool ShowInUI => Reported;

    public override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        npc.FightRequestedByDialogue = true;
        npc.FightIsPersonal          = _personal;
        Report($"{npc.DisplayName} demands a fight");
    }
}
