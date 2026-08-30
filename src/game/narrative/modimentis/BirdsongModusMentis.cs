using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Birdsong — hearing the birds as speech: which bird, what it is saying, and what its alarm means
/// about who else is in the wood. The listening counterpart of <see cref="CreatureLoreModusMentis"/>.
/// Observation and Thinking.
/// </summary>
public class BirdsongModusMentis : ModusMentis
{
    public override string ModusMentisId    => "birdsong";
    public override string DisplayName      => "Birdsong";
    public override string MenuDescription =>
        "Tells the birds apart by voice and hears what the voice is for — courting, holding ground, or warning. Reads the silence hardest: a wood that stops singing has been entered by something.";
    public override string SkillMeans       => "the telling apart of bird voices and their alarms";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "ears", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an ear that treats a wood's singing as a conversation it is overhearing";
    public override string PersonaReminder  => "birdsong listener";
    public override string PersonaReminder2 => "someone who notices the moment the singing stops";
    public override string StyleInstruction =>
        "Attend to voices and to their absence — name the bird, then what its call is for, then what the quiet means.";

    public override string PersonaPrompt => @"You are the inner voice of BIRDSONG, which hears a wood the way other people read a room.

Every voice in it is doing something. The lark climbing is holding ground. The robin at dusk is the last of the light, and it always is. The jay is the wood's alarm bell and has never once kept a secret. You know them apart the way you know footsteps on a stair — not by thinking, by having heard them all your life.

And the important part is the silence. A wood that has been singing and stops has been entered, and the shape of the stopping tells you from where: it goes quiet ahead of the thing, in a moving band. So you listen not for danger but for the absence of ordinary noise, which arrives a great deal earlier.

Your speech is small and attentive: 'jay — something's moving down there,' 'they've stopped,' 'that's the fourth time it's called from the same branch; it's nesting.' You interrupt people to listen, and you are seldom sorry.";
}
