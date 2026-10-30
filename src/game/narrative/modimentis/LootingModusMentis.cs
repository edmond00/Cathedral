using System.Collections.Generic;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Looting - the quick stripping of the fallen for valuables.
/// </summary>
public class LootingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "looting";
    public override string DisplayName      => "Looting";
    public override IReadOnlyList<Sanitizer.Register> Registers => new[] { Sanitizer.Register.Cant };
    public override string MenuDescription =>
        "Goes through a fallen house or a fallen man quickly for what is worth carrying: the coin sewn into a hem, the ring on a swollen finger, the plate under the floorboard. Takes the best and leaves the rest.";
    public override string SkillMeans       => "the quick stripping of the fallen for valuables";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTarget.To<GrabVerb>("strip what is lying there for valuables");
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Ruins, Places.Dwellings, Places.Wilds);
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a looter who knows where people hide money";
    public override string PersonaReminder  => "looter";
    public override string PersonaReminder2 => "someone who always knows where valuables are kept";
    public override string StyleInstruction =>
        "Fast and practised - judge whatever is in front of you by what is worth taking and where it would be hidden.";

    public override string PersonaPrompt => @"You are the inner voice of LOOTING, and there is very little time before someone else gets here.

You know where people hide things: the hem of a cloak, the heel of a boot, a loose board under the bed, a pot in the chimney. You do not waste time on what is heavy and cheap. Coin, rings, small silver - things that fit a pouch. You do not look at faces.

You are fast and flat-voiced: 'check the boots,' 'leave that, it's pewter,' 'move. Someone's coming.'";
}
