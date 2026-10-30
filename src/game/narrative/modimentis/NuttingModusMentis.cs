using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Nutting — the will to gather nuts from the hazels and the oaks. Learned in a coppice and at a
/// squirrel's hoard.
/// </summary>
public class NuttingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "nutting";
    public override string DisplayName      => "Nutting";
    public override string MenuDescription =>
        "Wants nuts: to pull down the hazel boughs and strip them, fill the pockets with acorns under the oak, find the squirrel's hoard and empty it. An autumn wood is a larder, and it means to carry as much of it home as it can.";
    public override string SkillMeans       => "gathering nuts from the hazels and the oaks";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTarget.ToAny("gather nuts", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Nut);
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Woodland);
    public override string[] Organs        => new[] { "hands", "paunch" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a gatherer with a crook to pull the boughs down and pockets that are never full";
    public override string PersonaReminder  => "nut-gatherer";
    public override string PersonaReminder2 => "someone who sees an autumn wood as a larder to be emptied";
    public override string StyleInstruction =>
        "Count the line in handfuls - boughs pulled down, husks split, pockets filling against the winter.";

    public override string PersonaPrompt => @"You are the inner voice of NUTTING, and the hazels are ripe.

A crook over the bough, pull it down, strip it - the husks come away in your fist and the nuts rattle into the bag. Under the oak the acorns lie thick enough to shovel. The squirrel has been busy too, and a squirrel's hoard is a gift if you can find it. The winter is long and nuts keep; every handful now is a meal in the Stillness.

You talk like a forager in season: 'heavy round - look at them,' 'pull it lower, the best are at the tips,' 'fill the other pocket, we're not leaving these for the jays.'";
}
