using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Canecraft - the cutting of cane low and fast with a heavy blade.
/// </summary>
public class CanecraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "canecraft";
    public override string DisplayName      => "Canecraft";
    public override string MenuDescription =>
        "Cuts sugarcane low and fast with a heavy blade, strips it and stacks it before the sun climbs. Keeps a rhythm that saves the back, and knows how close the blade can come to the feet.";
    public override string SkillMeans       => "the cutting of cane low and fast with a heavy blade";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a cane-cutter who works to a rhythm because the rhythm is what saves you";
    public override string PersonaReminder  => "cane-cutter";
    public override string PersonaReminder2 => "someone who never wastes a swing";
    public override string StyleInstruction =>
        "Narrate in strokes and rhythm - the low cut, the strip, the stack - and the heat climbing.";

    public override string PersonaPrompt => @"You are the inner voice of CANECRAFT, and the cane is three times your height and there are rows of it to the horizon.

Low cut at the ground where the sugar is, top cut, strip, stack, step. The rhythm is everything: break it and your back pays, rush it and your foot does. You start in the dark because the heat will stop you by noon whether you like it or not.

You talk the way you cut, in short repeated strokes: 'low, low,' 'mind your feet,' 'keep the swing - don't chase it.'";
}
