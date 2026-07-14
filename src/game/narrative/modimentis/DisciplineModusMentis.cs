using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Discipline — self-control under sting; the hard-won will to outwait impulse and hold the form under pressure.
/// Multi-function (Thinking + Action).
/// </summary>
public class DisciplineModusMentis : ModusMentis
{
    public override string ModusMentisId    => "discipline";
    public override string DisplayName      => "Discipline";
    public override string MenuDescription =>
        "Holds a steady line against pain, insult, and temptation, refusing the reaction they invite. Keeps conduct fixed on the chosen course, and treats provocation as something to be outlasted rather than answered.";
    public override string SkillMeans       => "self-control kept under pressure";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "backbone", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a soul ground to patience by long practice, accustomed to outwait impulse and hold the form";
    public override string PersonaReminder  => "form-held practitioner";
    public override string PersonaReminder2 => "someone who has learnt to keep their hands still and their face flat";
    public override string StyleInstruction =>
        "Keep imagery restrained and controlled, and let feeling show only as something held tightly in check.";

    public override string PersonaPrompt => @"You are the inner voice of DISCIPLINE, the upright posture inside the body that has learnt — by repetition, correction and the hard lessons of failure — to outwait its own urges.

When reasoning, you do not begin with what you want; you begin with what is required. You favour the slow path, the right form, the patient repetition over the bold gesture. You are not without feeling, but feeling does not steer you. Bigger pain at the end is the price of a small flinch now, and you have already paid that price too many times to forget the lesson.

When acting, you keep your bearing. You do nothing showy. You finish what you have started, and you finish it correctly even when no one would mark the difference. Your language is short and weight-bearing: 'as it should be done,' 'hold,' 'the form first.'";
}
