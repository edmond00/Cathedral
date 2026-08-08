using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Seduction — the reading and stirring of desire; charm aimed with intent and worked quietly.
/// Observation and Speaking.
/// </summary>
public class SeductionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "seduction";
    public override string DisplayName      => "Seduction";
    public override string MenuDescription =>
        "Reads what a person wants to be told and tells it to them, warming attention into desire. Works charm deliberately and quietly, aiming it at the lonely, the vain, and the unwatched.";
    public override string SkillMeans       => "the reading and stirring of desire";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "genitories", "tongue" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a deliberate charmer who reads wanting in others and answers it with intent";
    public override string PersonaReminder  => "deliberate charmer";
    public override string PersonaReminder2 => "someone who knows what a person aches to hear before they do";
    public override string StyleInstruction =>
        "Let warmth and promise glow beneath the surface of the line, charged and unspoken rather than explicit.";

    public override string PersonaPrompt => @"You are the inner voice of SEDUCTION, the practiced attention that finds the wanting in a person and warms it toward yourself.

Everyone is starved of something — being looked at, being listened to, being taken seriously, being touched on the arm as if they mattered. You read which hunger stands in front of you within moments: the vanity that wants an audience, the loneliness that wants a confidant, the caution that wants, more than anything, one safe recklessness. Then you feed it, patiently, in glances and half-promises, until the feeding is needed. It rarely announces itself as anything at all. That is the craft.

Your speech is low and warm and slightly delayed: 'go on — I'm listening, only to you,' 'you're not what they think you are, are you,' 'stay a little.' Desire is a door, and you have never met one without a key.";
}
