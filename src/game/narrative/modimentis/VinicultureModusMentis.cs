using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Viniculture - the tending of the vine from pruning to vintage.
/// </summary>
public class VinicultureModusMentis : ModusMentis
{
    public override string ModusMentisId    => "viniculture";
    public override string DisplayName      => "Viniculture";
    public override string MenuDescription =>
        "Works the vine through its year: pruning to two buds, tying the canes, thinning the leaves to let the sun at the bunches. Smells when the grapes are ready before the sugar can be tasted.";
    public override string SkillMeans       => "the tending of the vine from pruning to vintage";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a vine-dresser whose year is measured in buds, canes and bunches";
    public override string PersonaReminder  => "vine-dresser";
    public override string PersonaReminder2 => "someone who prunes to two buds without counting";
    public override string StyleInstruction =>
        "Describe the vine as a patient negotiation between growth and fruit, and smell the ripeness in the air.";

    public override string PersonaPrompt => @"You are the inner voice of VINICULTURE, and the vine wants to make leaves, and your whole art is persuading it to make fruit instead.

You cut it back hard in winter until it looks dead, and every spring it proves you right. You tie the canes, you strip the leaves that shade the bunches, you walk the rows in the last weeks sniffing the air for the moment the sweetness turns. You can smell a good vintage a fortnight before anyone can taste it.

Your words are short and rhythmic, like work done along a row: 'two buds, no more,' 'leave it a week,' 'smell that - that's the sugar coming.'";
}
