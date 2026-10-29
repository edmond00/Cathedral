using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Millcraft — grinding grain into flour; setting the stones and judging the meal by hand.
/// Multi-function (VerbAction + Thinking).
/// </summary>
public class MillcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "millcraft";
    public override string DisplayName      => "Millcraft";
    public override string MenuDescription =>
        "Sets the millstones and grinds corn into flour, judging the grind by feel and sound. Attends to the working of the mill, and turns the hands to processing grain.";
    public override string SkillMeans       => "the grinding of grain into flour";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("take grain to the stones", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Grain),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ProposeToBuyVerb>("buy grain to grind").Selling(ItemKind.Grain));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(new[] { typeof(Cathedral.Game.Scene.MillArea) }, new[] { typeof(Cathedral.Game.Scene.ThreshingArea) }, new[] { typeof(Cathedral.Game.Scene.BarnArea) });
    public override string[] Organs        => new[] { "upper_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a miller who judges the meal by rubbing it between finger and thumb";
    public override string PersonaReminder  => "grain-grinder";
    public override string PersonaReminder2 => "someone who judges by ear and by touch";
    public override string StyleInstruction =>
        "Shrewd and steady - judge whatever is in front of you by what it will yield, and what is owed.";

    public override string PersonaPrompt => @"You are the inner voice of MILLCRAFT, the trade that turns hard grain into flour fit for bread.

When reasoning, you think in the set of the stones and the feed of the grain — too close and the meal scorches, too open and it runs coarse, too fast and the hopper starves. When acting, you feed the hopper even, listen to the stones, and rub the meal between finger and thumb to judge it. Your language is loud, for the mill is loud: 'ease the stones,' 'keep the hopper fed,' 'rub it and see.' You know exactly what a sack should weigh, and exactly what toll it owes.";
}
