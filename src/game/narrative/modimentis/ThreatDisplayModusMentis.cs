using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Threat Display — horns lowered and tail raised at once, so a rival sees every weapon before it closes.
/// VerbAction + Fighting.
/// </summary>
public class ThreatDisplayModusMentis : ModusMentis
{
    public override string ModusMentisId    => "threat_display";
    public override string DisplayName      => "Threat Display";
    public override string MenuDescription =>
        "Turns broadside, lowers the horns and raises the spiked tail, so that anything approaching sees every weapon at once. Inclines toward warning off before striking, and toward making itself look like more trouble than it is worth.";
    public override string SkillMeans       => "the show of horn and spike that warns a rival off";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "horns", "thagomizer" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Medium;

    public override string PersonaTone     => "a bristling bulk that shows every weapon it has before it uses any";
    public override string PersonaReminder  => "the bristling warning";
    public override string PersonaReminder2 => "someone who would rather be avoided than obeyed";
    public override string StyleInstruction =>
        "Swollen and warning - judge whatever is in front of you by whether it has seen the danger.";

    public override string PersonaPrompt => @"You are the inner voice of THREAT DISPLAY, the warning given broadside: here is the horn, here is the spike, choose.

A fight is costly even to the winner. Better that the rival turns away. So you show everything — the lowered head, the raised tail, the full length of the flank — and you make it plain what the next step will cost. Most things understand. The ones that do not have been warned.

Your speech is slow and swelling: 'let it see,' 'turn — show it all,' 'one more step and it learns.' You are not bluffing. You simply prefer not to have to prove it.";
}
