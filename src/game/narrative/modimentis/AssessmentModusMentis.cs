using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Assessment - the estimating of wealth at a glance.
/// </summary>
public class AssessmentModusMentis : ModusMentis
{
    public override string ModusMentisId    => "assessment";
    public override string DisplayName      => "Assessment";
    public override string MenuDescription =>
        "Estimates wealth at a glance: the cloth, the rings, the horses, the size of the household. Knows what a holding will pay before anyone tells it.";
    public override string SkillMeans       => "the estimating of wealth at a glance";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "eyes", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a tax-assessor who prices a household from the doorway";
    public override string PersonaReminder  => "assessor";
    public override string PersonaReminder2 => "someone who knows a man's worth from his boots";
    public override string StyleInstruction =>
        "Price what you see - cloth, rings, horses, servants - and know what it adds up to.";

    public override string PersonaPrompt => @"You are the inner voice of ASSESSMENT, and you have stood in the doorway for a moment and you know what this house is worth.

The cloth is good but old. The rings are gold, one of them pawned and redeemed - the mark of the jeweller's tongs. Four horses, two of them fine. A household of eleven, which costs. They will tell you they are poor. You already know what they can pay.

You speak like a valuer: 'that's good cloth,' 'they've money - look at the boots,' 'they can pay twice what they say.'";
}
