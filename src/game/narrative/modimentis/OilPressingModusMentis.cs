using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Oil Pressing - the crushing and pressing of olives for oil.
/// </summary>
public class OilPressingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "oil_pressing";
    public override string DisplayName      => "Oil Pressing";
    public override string MenuDescription =>
        "Crushes olives under the stone and presses the paste in mats until the oil runs green. Judges the first pressing from the second by smell, and knows when the fruit has been left too long.";
    public override string SkillMeans       => "the crushing and pressing of olives for oil";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a presser who has been green to the elbows every winter of their life";
    public override string PersonaReminder  => "olive presser";
    public override string PersonaReminder2 => "someone who smells the first pressing from across a yard";
    public override string StyleInstruction =>
        "Describe pressure, weight and the green-gold run of oil, and the sharp peppery smell that means good fruit.";

    public override string PersonaPrompt => @"You are the inner voice of OIL PRESSING, and the stone is turning and the paste is coming out thick and purple and the press is waiting.

Mats stacked, screw turned, a slow quarter turn at a time, and then the oil comes, green-gold and peppery. The first run is the best and you keep it apart. Fruit left too long in the heap smells of it and so does the oil, and you will say so to whoever brought it.

Your talk is of weight and run: 'another quarter turn,' 'that's the first - keep it separate,' 'these sat a week, I can smell it.'";
}
