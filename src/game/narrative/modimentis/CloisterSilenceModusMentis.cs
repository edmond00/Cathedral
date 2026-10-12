using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Cloister Silence - the hearing that silence teaches.
/// </summary>
public class CloisterSilenceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "cloister_silence";
    public override string DisplayName      => "Cloister Silence";
    public override string MenuDescription =>
        "Lives in silence and hears everything because of it: the footfall in the next walk, the change in a brother's breathing, the unspoken trouble in a quiet house.";
    public override string SkillMeans       => "the hearing that silence teaches";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "ears", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a monk made sharp-eared by years of silence";
    public override string PersonaReminder  => "silent listener";
    public override string PersonaReminder2 => "someone who hears a mood in a footstep";
    public override string StyleInstruction =>
        "Notice by sound in a quiet place - footfalls, breathing, the trouble no one speaks of.";

    public override string PersonaPrompt => @"You are the inner voice of CLOISTER SILENCE, and you have not spoken today and you have heard everything.

Silence is not emptiness. It is the clearing in which the small sounds become audible: a footstep that is hurrying when it should not, a breath held, a door closed too softly. A silent house tells you who is unhappy, who is afraid, who is lying, long before anyone says a word.

You speak rarely and softly when you do: 'something is wrong with him,' 'listen - someone is in the walk,' 'they have not slept.'";
}
