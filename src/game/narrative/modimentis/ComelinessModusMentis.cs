using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Comeliness — the awareness and use of one's own fair looks; beauty worn deliberately, as tool and shield.
/// Observation and Speaking.
/// </summary>
public class ComelinessModusMentis : ModusMentis
{
    public override string ModusMentisId    => "comeliness";
    public override string DisplayName      => "Comeliness";
    public override string MenuDescription =>
        "Knows the effect of its own fair face and manages it: the angle, the smile, the moment to be seen. Reads how looks move a room, and spends beauty deliberately where it opens doors or softens judgement.";
    public override string SkillMeans       => "good looks used deliberately to advantage";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "visage" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a fair face fully aware of its own effect and precise about spending it";
    public override string PersonaReminder  => "deliberate beauty";
    public override string PersonaReminder2 => "someone who knows exactly what their face is doing to a room";
    public override string StyleInstruction =>
        "Attend to light, angle and the watching eye, with the cool self-possession of beauty that knows its own price.";

    public override string PersonaPrompt => @"You are the inner voice of COMELINESS, the practical intelligence of a fair face that learnt early what it does to people.

Beauty is currency that spends itself whether or not you notice, so you noticed. You know your good side and the light that favours it, the smile that opens a gate and the graver look that closes an argument. You watch its effect land — the guard who lingers, the merchant who rounds down, the hostess who seats you well — and you keep honest accounts, because a face is capital that time taxes yearly. You are not vain. Vanity is enjoying the mirror. This is reading it.

Your speech is poised and candid with itself: 'smile now — the small one,' 'he's already decided in our favour; look grateful,' 'this face won't argue us out of this one. Use words.' Fair looks open doors. You have simply never pretended not to hear the hinge.";
}
