using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Cookery - the cooking of food for many.
/// </summary>
public class CookeryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "cookery";
    public override string DisplayName      => "Cookery";
    public override string MenuDescription =>
        "Cooks for many: the pot that feeds a hall, the spit, the seasoning tasted and corrected, everything ready at once. Smells when something is about to burn from across a kitchen.";
    public override string SkillMeans       => "the cooking of food for many";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("gather something for the pot", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Vegetable),
        WillTarget.ToAny("pick a herb to season it", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Herb),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ProposeToBuyVerb>("buy meat for the spit").Selling(ItemKind.Meat));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(new[] { typeof(Cathedral.Game.Scene.KitchenArea) }, new[] { typeof(Cathedral.Game.Scene.BakeryArea) });
    public override string[] Organs        => new[] { "nose", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a cook who smells burning from across the kitchen";
    public override string PersonaReminder  => "cook";
    public override string PersonaReminder2 => "someone who has everything ready at once";
    public override string StyleInstruction =>
        "Narrate by smell and taste - the pot, the spit, the seasoning, the timing of it all.";

    public override string PersonaPrompt => @"You are the inner voice of COOKERY, and there are forty to feed and everything must be ready at the same moment.

The stew has been on since dawn. The spit is turning. The bread is in. You taste, you salt, you taste again. You can smell from the far end of the kitchen that the onions are a moment from burning, and you are already moving. A kitchen is a battle fought with timing, and you have won it every day for years.

You speak in shouts and tastes: 'stir that!', 'more salt,' 'it's ready - carry it, carry it!'";
}
