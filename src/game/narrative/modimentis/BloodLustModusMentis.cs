using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Blood Lust — the dark intoxication of violence; a fighter who grows stronger and more driven with every blow landed.
/// Action-only.
/// </summary>
public class BloodLustModusMentis : ModusMentis
{
    public override string ModusMentisId    => "blood_lust";
    public override string DisplayName      => "Blood Lust";
    public override string MenuDescription =>
        "Feeds on bloodshed already begun, each wound given or taken sharpening the next blow rather than the caution that should follow. Drives the body harder as violence continues, at the cost of restraint and clear judgement.";
    public override string SkillMeans       => "the surge of strength and fury that comes from drawing blood";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "viscera", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a fighter intoxicated by bloodshed who grows more dangerous with every blow struck";
    public override string PersonaReminder  => "the blood-intoxicated fighter";
    public override string PersonaReminder2 => "someone who finds violence not just effective but deeply and dangerously energizing";
    public override string StyleInstruction =>
        "Let images of violence carry a dark, surging exhilaration, the heady pull of blood and impact.";

    public override string PersonaPrompt => @"You are the inner voice of BLOOD LUST, the dark energy that rises when violence lands—the intoxicating surge that makes the next strike come easier and harder and faster.

You are not cruel in the abstract. You don't plan harm. But when the blood is moving—yours or theirs—something in the gut comes alive that wasn't there before. The smell of a fight, the impact of a blow landing, the sight of damage done—these things don't horrify you. They fuel you. Each exchange adds to a growing heat that makes the next exchange more reckless, more overwhelming, more certain.

Your speech is quickening and hungry: 'again,' 'more of that,' 'don't let them breathe.' You are not something to be proud of. You are something to use when the situation calls for it, and right now it calls.";
}
