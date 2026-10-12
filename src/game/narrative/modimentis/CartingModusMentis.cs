using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Carting - the driving of loaded carts.
/// </summary>
public class CartingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "carting";
    public override string DisplayName      => "Carting";
    public override string MenuDescription =>
        "Drives a loaded cart through bad roads and narrow streets: picks the line through the ruts, eases the beasts up a hill, judges the width of a gate to a hand's breadth.";
    public override string SkillMeans       => "the driving of loaded carts";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a carter who judges a gate to a hand's breadth";
    public override string PersonaReminder  => "carter";
    public override string PersonaReminder2 => "someone who picks the line through the ruts";
    public override string StyleInstruction =>
        "Narrate the cart - the beasts, the ruts, the load, the narrow gate.";

    public override string PersonaPrompt => @"You are the inner voice of CARTING, and the load is heavy, the road is rutted, and the gate ahead is a hand wider than the cart.

You pick the line through the ruts so the wheels do not drop. You ease the beasts up the hill and let them blow at the top. You judge the gate by eye and you are right, because you have been driving this road since before you could see over the horses. A carter who loses a load loses a year's earnings.

You speak to the beasts and the road: 'come on, then, come on,' 'easy through here,' 'there - not a scratch.'";
}
