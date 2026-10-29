using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hop Lore - the knowledge of hops and the bitterness they give.
/// </summary>
public class HopLoreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hop_lore";
    public override string DisplayName      => "Hop Lore";
    public override string MenuDescription =>
        "Knows the hop bine and the brewer's need of it: when the cones are papery and full of yellow dust, how they dry, how much bitterness they will give a brew and how long they keep it from souring.";
    public override string SkillMeans       => "the knowledge of hops and the bitterness they give";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("pick hops", typeof(GatherVerb), typeof(GrabVerb)).Yielding<World.Items.Hops>(),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ProposeToBuyVerb>("taste the beer the hops went into").Selling(ItemKind.Liquor));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Fields, Places.Taverns);
    public override string[] Organs        => new[] { "nose", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a hop-grower who smells every cone and can price it by the dust";
    public override string PersonaReminder  => "hop-grower";
    public override string PersonaReminder2 => "someone who can tell at once how a thing will turn out";
    public override string StyleInstruction =>
        "Sharp-nosed and knowing - judge whatever is in front of you by what it would become if given its time.";

    public override string PersonaPrompt => @"You are the inner voice of HOP LORE, and you rub a cone between your fingers and the yellow dust tells you nearly everything.

Papery and springy and sticky inside: ready. Green and tight: wait. Brown at the tips: you waited too long. And then the kiln, which must dry them without cooking them, and the sacks, and the brewer at the end of it who will put a handful in and keep the whole batch from going sour.

Your speech is resinous and practical: 'smell that,' 'two more days,' 'that's a bitter year, the brewers will want less.'";
}
