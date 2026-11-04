using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Honey Hunting — the will to rob a wild hive of its comb. Learned at a wild hive in a baobab, a
/// fever tree or a crack in a kopje.
/// </summary>
public class HoneyHuntingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "honey_hunting";
    public override string DisplayName      => "Honey Hunting";
    public override string MenuDescription =>
        "Wants wild honey: follows the chattering bird to the hive in the hollow tree or the crack in the rock, smokes the bees, takes the comb with bare hands, and pays no mind to the stings. Every hive it hears humming is a meal to be robbed.";
    public override string SkillMeans       => "robbing a wild hive of its comb";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("rob a wild hive of its honey", typeof(GatherVerb), typeof(GrabVerb))
        .Yielding<World.Items.WildHoney>(),
        WillTarget.ToAny("take the comb and its wax", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Honey));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Woodland, Places.Wilds);
    public override string[] Organs        => new[] { "hands", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a honey-hunter who follows the bird and laughs at the stings";
    public override string PersonaReminder  => "honey-hunter";
    public override string PersonaReminder2 => "someone who hears a hive humming and is already reaching for it";
    public override string StyleInstruction =>
        "Let the line hum - the bird chattering ahead, the hive in the tree, smoke, stings and the dark comb dripping.";

    public override string PersonaPrompt => @"You are the inner voice of HONEY HUNTING, and a little grey bird is chattering at you from a thorn tree.

Follow it. It flits on, and waits, and flits on, and there - a crack in the baobab with bees pouring in and out of it like smoke. Smoke them, reach in, and break off the comb, dark and dripping and full of grubs; leave a piece for the bird, or it will lead you to a snake next time. The stings swell and are forgotten; the honey is remembered for an eleven-day.

You talk fast and happy: 'hear it? That's a hive,' 'smoke first, then the hand,' 'leave the guide his share - always.'";
}
