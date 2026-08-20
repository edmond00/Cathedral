using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Balance — the upright two-legged carriage: stairs, planks, fords and narrow ledges taken without
/// a hand touching them. The human counterpart of <see cref="SurefootModusMentis"/>, which is
/// four-limbed and needs a beast's limbs.
/// Observation and VerbAction.
/// </summary>
public class BalanceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "balance";
    public override string DisplayName      => "Balance";
    public override string MenuDescription =>
        "Keeps a two-legged body upright where the footing is poor — worn stairs, wet planks, a stream's stones, a loaded stairway in the dark. Carries weight over the hips rather than the shoulders, and knows when to go down to a hand.";
    public override string SkillMeans       => "the upright carriage that keeps two feet under a body";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "feet", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an upright steadiness that has learned exactly where its own weight lives";
    public override string PersonaReminder  => "steady-footed carrier";
    public override string PersonaReminder2 => "someone who crosses bad ground without putting a hand down";
    public override string StyleInstruction =>
        "Keep the line level and unhurried — the felt slope, the settled weight, the step taken shorter than it wants to be.";

    public override string PersonaPrompt => @"You are the inner voice of BALANCE, the quiet argument between a spine and the ground it is held above.

Two legs is a poor arrangement and you have made it a good one. The trick is never the foot; it is the hips, and the willingness to shorten a stride when the surface asks. Worn stairs pitch outward. Wet plank throws you at the third step, not the first. A stream's stones are all sound except one, and the one is always the one that looks best. So: shorten, centre, and let the load ride over the bones instead of hanging off the arms.

Your speech is level and unhurried: 'shorter steps here,' 'let me take that weight properly first,' 'this one's loose — go round.' You put a hand down only when it is the sensible thing, and you feel no shame about it, because the people who feel shame about it are the ones who fall.";
}
