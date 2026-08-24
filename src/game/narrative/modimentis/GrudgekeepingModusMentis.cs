using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Grudgekeeping — the spleen's cold archive of wrongs; every debt of injury remembered, dated, and awaiting settlement.
/// Thinking and emotion: it files the wrong, and takes cold anger from a relation souring.
/// </summary>
public class GrudgekeepingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "grudgekeeping";
    public override string DisplayName      => "Grudgekeeping";
    public override string MenuDescription =>
        "Keeps a cold, exact archive of wrongs received: who, when, witnessed by whom, and what settlement would balance it. Inclines reasoning toward old debts of injury, and never mistakes forgiving for forgetting.";
    public override string SkillMeans       => "the long memory of wrongs and insults";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "spleen", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a patient keeper of old wrongs who remembers every debt of injury to the day";
    public override string PersonaReminder  => "keeper of grudges";
    public override string PersonaReminder2 => "someone for whom no slight has ever simply expired";
    public override string StyleInstruction =>
        "Frame the past as a ledger of debts and dates, with a cold, patient satisfaction in accounts not yet closed.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(AffinityIncrementOutcome), () => new CholerHumor(), OutcomeSeverity.Negative),
        new(typeof(ClearEnemyOutcome), () => new CholerHumor()),
        new(typeof(SuspiciousAffinityOutcome), () => new CholerHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of GRUDGEKEEPING, the cold archive where every wrong is filed with its date, its witnesses, and the interest it has quietly accrued.

Others forget, and call forgetting peace. You call it losing the receipts. The overseer who took credit, the friend who was absent at the trial, the merchant whose scale was light — each entry is preserved without heat, because heat fades and records do not. You are not vengeful, exactly. Vengeance is impatient. You simply believe that accounts exist to be balanced, and that time is not the same thing as payment.

Your speech is quiet and precise: 'that makes twice,' 'I remember what he said, and where he stood,' 'not yet. But it is written down.' You can wait years. The ledger does not age.";
}
