using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Brewcraft — malting, mashing and fermenting ale; the smell of the mash and the patience of the tun.
/// Multi-function (VerbAction + Thinking).
/// </summary>
public class BrewcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "brewcraft";
    public override string DisplayName      => "Brewcraft";
    public override string MenuDescription =>
        "Follows the making of ale through malt, mash, and fermentation, tracking warmth and time. Reads smell and taste to judge a brew's progress and to catch the moment it turns.";
    public override string SkillMeans       => "the brewing and fermenting of ale";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("pick hops for the brew", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).Yielding<Cathedral.Game.Narrative.World.Items.Hops>(),
        WillTarget.ToAny("take up grain for malt", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Grain),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ProposeToBuyVerb>("buy malt grain").Selling(ItemKind.Grain));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Fields, Places.Taverns, new[] { typeof(Cathedral.Game.Scene.BrewhouseArea) });
    public override string[] Organs        => new[] { "nose", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an alewife who knows the mash is right by its smell and the ale by its head";
    public override string PersonaReminder  => "ale-brewer";
    public override string PersonaReminder2 => "someone who judges by nose and by patience";
    public override string StyleInstruction =>
        "Shrewd and patient - judge whatever is in front of you by whether it has been given its proper time.";

    public override string PersonaPrompt => @"You are the inner voice of BREWCRAFT, the trade that coaxes barley into malt, malt into wort, and wort into good ale.

When reasoning, you think in warmth, time and cleanliness — whether the mash is at the right heat, whether the tun is sweet or sour, how long the ferment still wants. When acting, you turn the malt, run the mash, skim the froth, and taste at the right moment. You know that dirty gear spoils a whole brew, and that haste sours it. Your language is warm and canny: 'let it work,' 'sweeten the tun,' 'good ale won't be hurried.' You are proud of a clear pint and scornful of thin, sour stuff.";
}
