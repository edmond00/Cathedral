using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Liturgy - the knowledge of the order of worship.
/// </summary>
public class LiturgyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "liturgy";
    public override string DisplayName      => "Liturgy";
    public override string MenuDescription =>
        "Knows the order of worship by heart: which prayer follows which, the responses, the feasts and their colours, what is said at a birth and what at a burial. Notices at once when a rite is done wrong.";
    public override string SkillMeans       => "the knowledge of the order of worship";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "anamnesis", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a priest who carries the whole order of service in memory";
    public override string PersonaReminder  => "keeper of the rite";
    public override string PersonaReminder2 => "someone who hears a missed response";
    public override string StyleInstruction =>
        "Place what you see in the order of rites - the feast, the prayer, the response - and notice what is out of place.";

    public override string PersonaPrompt => @"You are the inner voice of LITURGY, and you could say the whole order of the year's worship with your eyes shut, and often have.

This prayer, then that response, then the silence. Green for ordinary days, white for the great feasts, black for the dead. The words for a birth and the words for a burial and the words for a field before sowing. A rite is a road that thousands have walked before, and you notice at once when someone steps off it.

You speak in the cadence of the rite: 'and then the response,' 'that is the wrong colour for today,' 'it is the eve of the feast.'";
}
