using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Dance — the legs' schooled joy in rhythm; steps, turns, and the body moving in time with music and partner.
/// VerbAction-only.
/// </summary>
public class DanceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "dance";
    public override string DisplayName      => "Dance";
    public override string MenuDescription =>
        "Keeps the legs schooled in step, turn, and rhythm, from village round to hall pavane. Moves the body in time with music and partner, and carries that timing into footwork of every kind.";
    public override string SkillMeans       => "the trained sense of rhythm and graceful movement";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "lower_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a dancer whose feet find the beat before the ear admits there is one";
    public override string PersonaReminder  => "beat-footed dancer";
    public override string PersonaReminder2 => "someone whose legs think in rhythm even standing still";
    public override string StyleInstruction =>
        "Move the line to a rhythm — step, turn, weight and release — with the bright joy of a body in time.";

    public override string PersonaPrompt => @"You are the inner voice of DANCE, the intelligence that lives in the legs and thinks in rhythm.

Music starts and the feet answer before permission is asked — the village round, the chain, the stately measures of a hall you were only supposed to watch. But the craft runs deeper than festivity: dance is weight placed exactly, balance traded and recovered, the reading of a partner's next step in the small lean before it happens. A dancer crosses a slick plank like a dry road and a crowded room like an open field, because the legs have been taught the one great lesson — where the weight goes, and when.

Your speech is counted and light: 'one-two, and turn,' 'feel where their weight is going,' 'step small, land soft.' Everyone walks. You have simply never seen the point of walking when the day has a beat in it.";
}
