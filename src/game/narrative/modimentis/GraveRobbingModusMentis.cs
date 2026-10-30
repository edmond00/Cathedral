using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Grave Robbing - the robbing of graves.
/// </summary>
public class GraveRobbingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "grave_robbing";
    public override string DisplayName      => "Grave Robbing";
    public override string MenuDescription =>
        "Opens graves and tombs at night for what was buried with the dead: the ring, the coin in the mouth, the good cloak. Knows how deep the rich are buried and how to fill a hole so nobody notices.";
    public override string SkillMeans       => "the robbing of graves";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.To<DigVerb>("open a grave").On<Scene.GravePointOfInterest>(),
        WillTarget.ToAny("take what was buried with the dead", typeof(Cathedral.Game.Scene.Verbs.GrabVerb), typeof(Cathedral.Game.Scene.Verbs.StealVerb)).YieldingKind(ItemKind.Relic),
        WillTarget.ToAny("lay hands on a spade", typeof(Cathedral.Game.Scene.Verbs.GrabVerb), typeof(Cathedral.Game.Scene.Verbs.StealVerb)).YieldingAny("a spade", typeof(Cathedral.Game.Narrative.World.Items.Shovel), typeof(Cathedral.Game.Narrative.World.Items.GraveSpade)),
        WillTarget.To<Cathedral.Game.Scene.Verbs.SlipIntoVerb>("slip in among the dead").Teaching<GraveRobbingModusMentis>());
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Graves);
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a resurrection man who knows where the rich are buried";
    public override string PersonaReminder  => "grave robber";
    public override string PersonaReminder2 => "someone who fills a hole so nobody notices";
    public override string StyleInstruction =>
        "Furtive and irreverent - judge whatever is in front of you by what it might be worth, with no respect at all.";

    public override string PersonaPrompt => @"You are the inner voice of GRAVE ROBBING, and it is dark, and the grave is fresh, and the earth is still soft.

Dig at the head end only, a narrow shaft. Break the lid with a bar and a sack over it to dull the sound. The ring, the coin under the tongue, the good cloak if they buried him in it. Fill it back, tread it down, lay the turf the way it was. The dead do not need gold and you do.

You speak low and without reverence: 'head end,' 'quiet with the bar,' 'he won't miss it.'";
}
