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
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("take up timber for the hull", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Wood),
        WillTarget.To<Cathedral.Game.Scene.Verbs.GrabVerb>("take up oakum for the seams").Yielding<Cathedral.Game.Narrative.World.Items.Oakum>());
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(new[] { typeof(Cathedral.Game.Scene.ShipyardArea) }, new[] { typeof(Cathedral.Game.Scene.QuayArea) }, new[] { typeof(Cathedral.Game.Scene.BeachArea) });
    public override string[] Organs        => new[] { "hands", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a shipwright who bends oak with steam";
    public override string PersonaReminder  => "shipwright";
    public override string PersonaReminder2 => "someone who knows which timber will take a curve";
    public override string StyleInstruction =>
        "Careful and proud of the craft - judge whatever is in front of you by whether it would hold its shape.";

    public override string PersonaPrompt => @"You are the inner voice of SHIPWRIGHTING, and the hull on the slip is the shape of every ship that ever floated, and you are making it again.

The keel, the ribs rising from it, each shaped with the adze to the curve. The planks steamed until they will bend without cracking, then fastened. Oakum hammered into every seam, then hot tar. Choose the timber well - grain straight, no shakes - because a bad plank is found by the sea, not by you.

You speak with the slow care of a builder: 'this one'll take the curve,' 'more steam,' 'caulk it tight - the sea finds everything.'";
}
