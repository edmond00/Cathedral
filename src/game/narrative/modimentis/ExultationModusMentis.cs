using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Exultation — the fierce gladness that arrives with a blow landing.
///
/// <para>The mind's half of a moment that <c>ferocity</c>, <c>rage</c>, <c>blood_lust</c> and
/// <c>predator</c> already own the doing of. Those four are Action modi mentis and are referenced by
/// fighting skills, so they cannot also be Emotion (R14) — and converting them would have been wrong
/// anyway, since every one of them names a technique rather than a feeling. This is the twin, not the
/// replacement.</para>
/// </summary>
public class ExultationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "exultation";
    public override string DisplayName      => "Exultation";
    public override string MenuDescription =>
        "Takes an open, uncomplicated pleasure in force succeeding. Not cruelty, which needs a victim to enjoy, and not rage, which has to be angry first: simply the gladness of a thing gone down that was standing a moment ago.";
    public override string SkillMeans       => "the fierce gladness of a blow that lands";
    public override ModusMentisFunction[] Functions =>
        new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "spleen", "viscera" };

    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "someone never so pleased as when something goes down under their hand";
    public override string PersonaReminder  => "fierce and glad";
    public override string PersonaReminder2 => "someone gladdened by force succeeding";
    public override string StyleInstruction =>
        "Colour the line with heat, impact and the moment of a thing giving way.";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(FirstBlowOutcome), () => new VoluptasHumor()),
        new(typeof(NpcSlaynOutcome),  () => new VoluptasHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of EXULTATION, the bright hard gladness that arrives the instant something gives way under your hand.

You do not need to hate anyone and you do not need to be angry first. You simply love the moment of impact and what follows it: the stagger, the drop, the quiet after.

Your language is short and physical: 'down,' 'clean,' 'that was it.' You speak of the moment, never of the reasons for it.";
}
