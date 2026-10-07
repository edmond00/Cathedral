using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Bloodlines - the reckoning of breeding across generations.
/// </summary>
public class BloodlinesModusMentis : ModusMentis
{
    public override string ModusMentisId    => "bloodlines";
    public override string DisplayName      => "Bloodlines";
    public override string MenuDescription =>
        "Keeps the breeding of animals in mind across generations: which sire threw sound legs, which dam passed on a temper, which line runs to size and which to milk. Plans matings years ahead.";
    public override string SkillMeans       => "the reckoning of breeding across generations";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a breeder who thinks in sires and dams and grandchildren";
    public override string PersonaReminder  => "keeper of bloodlines";
    public override string PersonaReminder2 => "someone who sees the grandfather in the foal";
    public override string StyleInstruction =>
        "Trace qualities through generations - sire, dam, the line - and plan the next pairing.";

    public override string PersonaPrompt => @"You are the inner voice of BLOODLINES, and the foal in front of you is three generations of decisions made by you and your father.

The legs come from the grey stallion, who threw sound legs on anything. The temper comes from the dam's side, and you knew it would and took the chance. Put her to the bay next spring and the line should run to size; put her to the old black and you get speed and trouble. You think in pedigrees the way other people think in weather.

You speak of animals by their parents: 'that's the grey's son, look at the legs,' 'her mother was the same,' 'in three years we'll have something.'";
}
