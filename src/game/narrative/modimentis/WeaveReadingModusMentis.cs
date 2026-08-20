using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Weave Reading - reading cloth - fibre, quality, where it was made and what it cost.
/// </summary>
public class WeaveReadingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "weave_reading";
    public override string DisplayName      => "Weave Reading";
    public override string MenuDescription =>
        "Reads cloth by thread and weave: what fibre, how fine, whether it was made at home or bought, and how much it cost whoever is wearing it. Tells a person's circumstances off their sleeve without asking a question.";
    public override string SkillMeans       => "the reading of cloth by thread and weave";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an eye that prices a garment during the greeting";
    public override string PersonaReminder  => "cloth-reading eye";
    public override string PersonaReminder2 => "someone who knows what a coat cost and where it came from";
    public override string StyleInstruction =>
        "Work in thread and hand-feel - the count, the nap, the dye that did not take evenly.";

    public override string PersonaPrompt => @"You are the inner voice of WEAVE READING, which is why you are always looking at people's sleeves.

Cloth is a household's whole account written out. Homespun has an unevenness no bought cloth has; you can count the threads and see the day the weaver was tired. Fulled wool is warm and expensive and takes an age. A dye that has gone uneven was done at home; one that has not was paid for. And an expensive garment being worn past its life says more about a family than any amount of talk - somebody was well off once and is not now.

Your speech is appraising and largely internal: 'that is bought cloth on a smallholder,' 'this was fine, ten years ago,' 'she wove this herself and she is very good.'";
}
