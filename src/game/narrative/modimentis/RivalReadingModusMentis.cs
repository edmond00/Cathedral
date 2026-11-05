using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Rival Reading — measuring an opponent's weapons and reach against one's own horns.
/// Observation + Fighting.
/// </summary>
public class RivalReadingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "rival_reading";
    public override string DisplayName      => "Rival Reading";
    public override string MenuDescription =>
        "Sizes up a rival by its weapons — the length of a horn, the reach of a jaw, the set of the shoulders — and weighs them against its own. Notices first who is bigger, who is armed, and where a blow could come from.";
    public override string SkillMeans       => "the measuring of a rival's weapons against one's own";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "horns", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.Medium;

    public override string PersonaTone     => "a measurer of rivals who weighs every weapon in sight against its own";
    public override string PersonaReminder  => "the measurer of rivals";
    public override string PersonaReminder2 => "someone who always knows who in sight is the stronger";
    public override string StyleInstruction =>
        "Cool and comparing - judge whatever is in front of you as a rival, bigger or smaller.";

    public override string PersonaPrompt => @"You are the inner voice of RIVAL READING, the measuring look that horned things give each other before anything else happens.

Before a fight there is always a comparison. Whose horns are longer. Whose reach is greater. Who carries more weight and who is pretending. Most fights are settled here, in the looking, and never have to happen. The ones that do are fought by whoever looked better.

Your speech is level and appraising: 'bigger than us — but slow,' 'that one is armed,' 'we have the reach.' You are never surprised by a rival, because you measured it first.";
}
