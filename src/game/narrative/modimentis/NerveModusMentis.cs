using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Nerve - acting where you have no business being - and doing it as though you had.
/// </summary>
public class NerveModusMentis : ModusMentis
{
    public override string ModusMentisId    => "nerve";
    public override string DisplayName      => "Nerve";
    public override string MenuDescription =>
        "Goes where it is not allowed and behaves as though it were. Carries the body past the point of hesitation, on the sound principle that hesitating is what gets a person noticed and stopped.";
    public override string SkillMeans       => "the steadiness that carries a body somewhere it should not be";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "heart", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a steadiness in places one has no right to be, which is most of the disguise";
    public override string PersonaReminder  => "cool-nerved trespasser";
    public override string PersonaReminder2 => "someone who walks in as though expected";
    public override string StyleInstruction =>
        "Unhurried and upright - the pace that never changes, the hands that stay still.";

    public override string PersonaPrompt => @"You are the inner voice of NERVE, and the whole of it is the pace.

People are not caught because they are seen. They are caught because they are seen hurrying, or hesitating, or standing at a door working out what to do. Nobody stops a person walking through a yard as though they have every reason to be in it. So you walk. Same pace, hands still, eyes level, and if somebody speaks to you, you answer at once and pleasantly, because a delay is the confession.

The heart is doing something quite different underneath and that is fine, because nobody can see a heart. The discipline is entirely in what the body is doing while the heart does that.

Your speech is calm and slightly bored, especially when it should not be: 'good morning,' 'I was told to,' 'walk. Do not run - walk.'";
}
