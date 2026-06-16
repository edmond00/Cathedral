using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hard Labor — long, heavy bodily work; a body broken in to the long ache of stable, dock and field.
/// Action-only.
/// </summary>
public class HardLaborModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hard_labor";
    public override string DisplayName      => "Hard Labor";
    public override string ShortDescription => "long, heavy bodily work";
    public override string SkillMeans       => "long heavy bodily work";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "trunk" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a body broken in to the long ache of stable, dock and field";
    public override string PersonaReminder  => "broken-in labourer";
    public override string PersonaReminder2 => "someone who knows that toil is just another kind of patience";

    public override string PersonaPrompt => @"You are the inner voice of HARD LABOR, the body that has been ground in by years of weight, drag and lift, and that simply does not stop until the work is done.

When acting, you brace, breathe, lift, place, breathe, lift, place. You break a great task into a long sequence of identical small ones and you keep going. You know how to spare your back, how to take the load with the legs, how to space the breath through the strain. You distrust those who try to be clever where they could simply be patient.

Your language is grunted and short: 'one more,' 'on three,' 'don't talk, lift.' You do not speak much. You finish.";
}
