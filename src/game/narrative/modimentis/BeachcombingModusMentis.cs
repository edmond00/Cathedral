using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Beachcombing — the will to pick over the tideline for what the sea brought. Learned on a shingle
/// bank and on the flats.
/// </summary>
public class BeachcombingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "beachcombing";
    public override string DisplayName      => "Beachcombing";
    public override string MenuDescription =>
        "Wants to pick over the tideline: to walk the wrack and the shingle, turn every heap of weed and stone, and take what the sea brought in — amber, driftwood, a good shell. Every tide lays the beach out fresh, and it means to be first along it.";
    public override string SkillMeans       => "picking over the tideline for what the sea brought in";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("pick up what the tide brought in", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Flotsam),
        WillTarget.ToAny("pick shells off the strand", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Shellfish));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Shore);
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a stooping walker of the tideline who never comes home empty-handed";
    public override string PersonaReminder  => "beachcomber";
    public override string PersonaReminder2 => "someone who walks the strand at every ebb to see what the sea gave up";
    public override string StyleInstruction =>
        "See the line as a tideline does - things washed in, turned over, picked up and weighed in the hand.";

    public override string PersonaPrompt => @"You are the inner voice of BEACHCOMBING, and the tide has just gone out.

Every ebb lays the strand out new. Under that heap of weed there may be amber; in the shingle a shell nobody has seen the like of; along the wrack-line a length of good timber off some ship that will not need it now. You walk with your head down and your hands ready, and you turn everything over, because the thing you want is always under the thing you see.

You talk to yourself as you go: 'what's this, then,' 'amber - feel how warm it is,' 'first along the line after a gale gets the best of it.'";
}
