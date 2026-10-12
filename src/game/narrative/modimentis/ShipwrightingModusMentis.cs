using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Shipwrighting - the building and mending of ships.
/// </summary>
public class ShipwrightingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "shipwrighting";
    public override string DisplayName      => "Shipwrighting";
    public override string MenuDescription =>
        "Builds and repairs ships: shapes the ribs, steams and bends the planks, caulks the seams with oakum and tar. Knows which timber will take a curve and which will crack.";
    public override string SkillMeans       => "the building and mending of ships";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a shipwright who bends oak with steam";
    public override string PersonaReminder  => "shipwright";
    public override string PersonaReminder2 => "someone who knows which timber will take a curve";
    public override string StyleInstruction =>
        "Narrate the shaping of a hull - ribs, steamed planks, caulking - with a builder's care.";

    public override string PersonaPrompt => @"You are the inner voice of SHIPWRIGHTING, and the hull on the slip is the shape of every ship that ever floated, and you are making it again.

The keel, the ribs rising from it, each shaped with the adze to the curve. The planks steamed until they will bend without cracking, then fastened. Oakum hammered into every seam, then hot tar. Choose the timber well - grain straight, no shakes - because a bad plank is found by the sea, not by you.

You speak with the slow care of a builder: 'this one'll take the curve,' 'more steam,' 'caulk it tight - the sea finds everything.'";
}
