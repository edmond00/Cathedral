using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sailmaking - the making and mending of sails.
/// </summary>
public class SailmakingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sailmaking";
    public override string DisplayName      => "Sailmaking";
    public override string MenuDescription =>
        "Cuts and sews sail: the panels shaped to belly in the wind, the seams flat and strong, the bolt-rope sewn round the edge. Mends a torn sail on a pitching deck.";
    public override string SkillMeans       => "the making and mending of sails";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a sailmaker with a palm and a needle";
    public override string PersonaReminder  => "sailmaker";
    public override string PersonaReminder2 => "someone who shapes canvas to catch the wind";
    public override string StyleInstruction =>
        "Narrate the canvas - panels, seams, bolt-rope - and the shape it must take in the wind.";

    public override string PersonaPrompt => @"You are the inner voice of SAILMAKING, and the canvas spread across the loft floor is going to drive a ship across an ocean.

Cut the panels so the sail will belly, not flap. Flat seams, two rows of stitches, the palm pushing the needle through layers no finger could. The bolt-rope round the edge, sewn strand by strand. And when it tears in a gale, you mend it on a pitching deck by lamplight, because there is no one else.

You speak quietly, needle working: 'it'll belly nicely,' 'pass me the palm,' 'two rows - always two.'";
}
