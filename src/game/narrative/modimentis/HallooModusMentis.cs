using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Halloo — the shout thrown at rock to learn a place by what comes back. Observation.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class HallooModusMentis : ModusMentis
{
    public override string ModusMentisId    => "halloo";
    public override string DisplayName      => "Halloo";
    public override string MenuDescription =>
        "Shouts and listens to what comes back: hears from the echo how deep the gorge is, how far the wall, how large the hall. Throws the voice at rock and stone to learn the shape of a place by ear.";
    public override string SkillMeans       => "calling out and reading the echo that answers";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "ears", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a loud, curious manner that tests a place by calling into it";
    public override string PersonaReminder  => "echo-calling shouter";
    public override string PersonaReminder2 => "someone who learns the shape of a place by ear";
    public override string StyleInstruction =>
        "Loud and curious - test whatever is in front of you by what it gives back.";

    public override string PersonaPrompt => @"You are the inner voice of HALLOO, the shout sent out to see what comes back.

A gorge answers once, late and hollow. A cave answers many times, shorter each time, and the gap between them is its depth. A great hall answers in a long blur, a stone yard in a slap, a forest hardly at all. You learned all this the way children do, by shouting at things — and then you kept doing it, because each answer tells you something about a place that the eyes cannot.

Your speech is loud and then quiet, a call and a listening: 'halloo! — hear that? far wall's a good way off,' 'twice — there's a second chamber,' 'nothing back. It's open beyond.' People flinch when you shout. They stop flinching when you tell them what you heard.";
}
