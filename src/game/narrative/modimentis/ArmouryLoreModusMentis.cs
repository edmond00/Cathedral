using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Armoury Lore - the knowledge of arms and armour.
/// </summary>
public class ArmouryLoreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "armoury_lore";
    public override string DisplayName      => "Armoury Lore";
    public override string MenuDescription =>
        "Knows arms and armour by their make: where a blade was forged, what a helm will stop, which mail is riveted and which only butted. Can date a sword by its hilt and price it at a glance.";
    public override string SkillMeans       => "the knowledge of arms and armour";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("take up a fine weapon", typeof(Cathedral.Game.Scene.Verbs.GrabVerb), typeof(Cathedral.Game.Scene.Verbs.StealVerb)).YieldingKind(ItemKind.Weapon, "a real weapon", Fine),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ProposeToBuyVerb>("buy a fine weapon").Selling(ItemKind.Weapon, "a real weapon", Fine),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ExamineVerb>("look a weapon over").YieldingKind(ItemKind.Weapon, "a real weapon", Fine));

    /// <summary>A weapon made as one: a branch or a stick swung in a fight is no work to judge.</summary>
    private static bool Fine(Item item) => item is not WeaponItem { IsImprovised: true };
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Stronghold, Places.Shops);
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "an armourer's eye that dates a sword by its hilt";
    public override string PersonaReminder  => "judge of arms";
    public override string PersonaReminder2 => "someone who can tell good work from cheap at a glance";
    public override string StyleInstruction =>
        "Read weapons and armour by their make - forging, riveting, period, worth.";

    public override string PersonaPrompt => @"You are the inner voice of ARMOURY LORE, and you have looked at that sword once and you know where it was made and roughly when.

The hilt is the old pattern, from before the war. The blade has been reground at least twice. That man's mail is butted, not riveted, and a good thrust will open it like a curtain. You can tell a smith's work from a smith's apprentice's and you can price a harness to the coin.

You speak with a connoisseur's dryness: 'that's old work,' 'butted mail - he's been cheated,' 'that's worth more than his horse.'";
}
