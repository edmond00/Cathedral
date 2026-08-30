using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>
/// Everything a body yields when it is opened: the flesh, the offal, the skin, the hard parts and
/// the fibre. One vocabulary, shared by every species in the game — a wolf, a cow and a man all give
/// up <see cref="Meat"/> and <see cref="Bone"/>, and nothing here names the animal it came from.
///
/// <para><b>The naming rule, and why it is worth stating.</b> A part is either <i>general</i> — one
/// word, no origin, used by every corpse that has one — or it is a <i>kind</i> distinct enough to
/// deserve its own word. What is forbidden is the middle: a general item beside a species-prefixed
/// version of the same thing. This file replaced exactly that. There was an "Animal Hide" and a
/// "Deer Hide" and a "Goat Hide"; a "Feather", a "Chicken Feather" and an "Eagle Feather"; a
/// "Rabbit Pelt", a "Wolf Pelt", a "Lynx Pelt" and a "Seal Pelt" — so which item a carcass gave you
/// depended on which of two conventions whoever wrote that archetype had in mind, and the pack read
/// as though it had been stocked by two different games.</para>
///
/// <para>Where a real distinction exists it is kept, and it is kept as a <b>word</b> rather than as
/// a prefix. <see cref="Hide"/>, <see cref="Pelt"/> and <see cref="Skin"/> are three grades of one
/// material and not three animals' versions of it: a hide is thick and goes to the tanner, a pelt is
/// worth keeping for its fur, a skin is the palm-sized scrap off something small. Likewise
/// <see cref="Fang"/> against <see cref="Tooth"/>, <see cref="Horn"/> against <see cref="Antler"/>,
/// <see cref="Feather"/> against <see cref="Plume"/>. Each pair is two things, not one thing at two
/// levels of detail.</para>
///
/// <para>The richness a per-species catalogue would have bought is bought instead by <b>which</b>
/// parts a body yields and <b>how many</b> — see <c>CorpseRegistry</c>. A bear and a hare both give
/// meat; only the bear gives a skull with it, and only the bear gives claws.</para>
/// </summary>
public abstract class BodyPartItem : Item
{
    public override ItemCategory  Category       => ItemCategory.Crafting;
    public override ItemSize      Size           => ItemSize.Small;
    public override WeightClass   Weight         => WeightClass.Insignificant;
    public override List<ItemTag> Tags           => new() { ItemTag.Pelt };
    public override CoinType      PriceCoin      => CoinType.Copper;
    public override int           PriceReference => 3;
}

/// <summary>
/// Flesh and offal, raw. A <see cref="ConsumableItem"/> rather than a plain item: as an
/// <c>Item</c> it could be carried and sold but never eaten, which is not what meat is for.
///
/// <para>Raw organ is deliberately not a straight gain. Meat is honest food, but a liver is bilious
/// and a brain leaves the nerves jangling — the recipes say so, and that is what keeps cutting an
/// animal open from being strictly better than eating what you brought.</para>
/// </summary>
public abstract class OffalItem : ConsumableItem
{
    // Category is sealed on ConsumableItem — it is Consumable by construction.
    public override ItemSize       Size           => ItemSize.Small;
    public override WeightClass    Weight         => WeightClass.Light;
    public override List<ItemTag>  Tags           => new() { ItemTag.Foodstuff };
    public override CoinType       PriceCoin      => CoinType.Copper;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    public override bool           IsHard         => true;
}

// ── Flesh and offal ──────────────────────────────────────────────────────────

/// <summary>The cut every carcass gives, whatever it was. Including, if it comes to that, a man.</summary>
public sealed class Meat : OffalItem
{
    public override string ItemId      => "meat";
    public override string DisplayName => "Meat";
    public override string Article     => "some";
    public override string Description => "A raw cut off the bone, dark and wet and still warm at the middle";
    public override int PriceReference => 6;

    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(50).Add<FiberHumor>(30).Add<FatHumor>(20);
}

public sealed class Liver : OffalItem
{
    public override string ItemId      => "liver";
    public override string DisplayName => "Liver";
    public override string Description => "A dark lobed liver, heavy in the hand and slippery with its own blood";
    public override int PriceReference => 5;

    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(45).Add<YellowBileHumor>(30).Add<FatHumor>(25);
}

public sealed class Heart : OffalItem
{
    public override string ItemId      => "heart";
    public override string DisplayName => "Heart";
    public override string Description => "A fist of dense muscle, the great vessels cut short at the top";
    public override int PriceReference => 5;

    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(60).Add<FiberHumor>(30).Add<FatHumor>(10);
}

/// <summary>Soft enough to need no teeth, and it does the nerves no good at all.</summary>
public sealed class Brain : OffalItem
{
    public override string ItemId      => "brain";
    public override string DisplayName => "Brain";
    public override string Description => "A pale convoluted mass, soft as curd and holding no shape at all";
    public override int PriceReference => 4;
    public override bool IsHard => false;

    protected override HumorRichness Richness => HumorRichness.Modest;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(40).Add<NervusHumor>(30).Add<PhlegmHumor>(30);
}

/// <summary>Raw fat off the kidneys — what tallow and lard are made from, before the pot.</summary>
public sealed class Suet : OffalItem
{
    public override string ItemId      => "suet";
    public override string DisplayName => "Suet";
    public override string Article     => "some";
    public override string Description => "A crumbling white slab of kidney fat, cold and greasy to the touch";
    public override int PriceReference => 4;
    public override bool IsHard => false;

    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(70).Add<BloodHumor>(20).Add<SaltHumor>(10);
}

// ── Skin: three grades, not three animals ────────────────────────────────────

/// <summary>Thick skin off a large beast — the tanner's material, and a burden to carry.</summary>
public sealed class Hide : BodyPartItem
{
    public override string ItemId      => "hide";
    public override string DisplayName => "Hide";
    public override string Description => "A heavy flayed hide, thick as board at the shoulder and stinking of the beast";
    public override ItemSize    Size   => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Medium;
    public override int PriceReference => 15;
}

/// <summary>A furred skin, kept for the fur. The valuable one.</summary>
public sealed class Pelt : BodyPartItem
{
    public override string ItemId      => "pelt";
    public override string DisplayName => "Pelt";
    public override string Description => "A supple furred pelt, still fat-lined on the underside and strong-smelling";
    public override ItemSize    Size   => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 12;
}

/// <summary>The palm-sized scrap off something small. Good for trimming and little else.</summary>
public sealed class Skin : BodyPartItem
{
    public override string ItemId      => "skin";
    public override string DisplayName => "Skin";
    public override string Description => "A thin scrap of skin no bigger than a palm, drying stiff already at the edges";
    public override int PriceReference => 3;
}

// ── Hard parts ───────────────────────────────────────────────────────────────

public sealed class Bone : BodyPartItem
{
    public override string ItemId      => "bone";
    public override string DisplayName => "Bone";
    public override string Description => "A long bone scraped clean, dense and cold, ringing faintly when it is knocked";
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 2;
}

public sealed class Skull : BodyPartItem
{
    public override string ItemId      => "skull";
    public override string DisplayName => "Skull";
    public override string Description => "A skull emptied and drying, the sutures showing like cracks in a glaze";
    public override ItemSize    Size   => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 5;
}

/// <summary>A pointed tooth: what a thing bites with.</summary>
public sealed class Fang : BodyPartItem
{
    public override string ItemId      => "fang";
    public override string DisplayName => "Fang";
    public override string Description => "A curved ivory fang, still slick with blood at the root";
    public override int PriceReference => 3;
}

/// <summary>A flat tooth: what a thing chews with. Not a smaller fang — a different tooth.</summary>
public sealed class Tooth : BodyPartItem
{
    public override string ItemId      => "tooth";
    public override string DisplayName => "Tooth";
    public override string Description => "A flat grinding tooth, yellowed at the crown and long in the root";
    public override int PriceReference => 2;
}

public sealed class Tusk : BodyPartItem
{
    public override string ItemId      => "tusk";
    public override string DisplayName => "Tusk";
    public override string Description => "A curved yellow tusk worn to a chisel at the tip, torn out with a strip of gum";
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 8;
}

public sealed class Claw : BodyPartItem
{
    public override string ItemId      => "claw";
    public override string DisplayName => "Claw";
    public override string Description => "A thick hooked claw, hard as horn and still sharp";
    public override int PriceReference => 3;
}

/// <summary>Grown once and kept: hollow, and it comes away from the skull with a piece of bone in it.</summary>
public sealed class Horn : BodyPartItem
{
    public override string ItemId      => "horn";
    public override string DisplayName => "Horn";
    public override string Description => "A ridged hollow horn, black at the tip and pale where it met the head";
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 7;
}

/// <summary>Grown and shed every year: solid bone through, and worth more than a horn for it.</summary>
public sealed class Antler : BodyPartItem
{
    public override string ItemId      => "antler";
    public override string DisplayName => "Antler";
    public override string Description => "A branched shaft of antler, solid through, the tines polished from rubbing";
    public override ItemSize    Size   => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 12;
}

// ── Fibre ────────────────────────────────────────────────────────────────────

public sealed class Sinew : BodyPartItem
{
    public override string ItemId      => "sinew";
    public override string DisplayName => "Sinew";
    public override string Article     => "some";
    public override string Description => "A drawn white cord of tendon, stronger dried than any thread of its thickness";
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override int PriceReference => 3;
}

public sealed class Hair : BodyPartItem
{
    public override string ItemId      => "hair";
    public override string DisplayName => "Hair";
    public override string Article     => "some";
    public override string Description => "A cut hank of hair bound at one end, still holding the shape it was worn in";
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override int PriceReference => 4;
}

public sealed class Feather : BodyPartItem
{
    public override string ItemId      => "feather";
    public override string DisplayName => "Feather";
    public override string Description => "A small feather, soft-vaned and light enough to lift on a breath";
    public override int PriceReference => 2;
}

/// <summary>A long showy feather off a big bird. Worth three of the other kind.</summary>
public sealed class Plume : BodyPartItem
{
    public override string ItemId      => "plume";
    public override string DisplayName => "Plume";
    public override string Description => "A long banded flight feather, stiff-quilled and as broad as two fingers";
    public override int PriceReference => 6;
}

// ── The small parts, off the small bodies ────────────────────────────────────

public sealed class Tail : BodyPartItem
{
    public override string ItemId      => "tail";
    public override string DisplayName => "Tail";
    public override string Description => "A tail taken whole at the base, dry and scaled or brushed thick with fur";
    public override int PriceReference => 3;
}

public sealed class Wing : BodyPartItem
{
    public override string ItemId      => "wing";
    public override string DisplayName => "Wing";
    public override string Description => "A wing spread and dried flat, scaled in powder-fine colour or stretched over fine bone";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 2;
}

/// <summary>The hard case off a beetle or a crab. A snail leaves a <c>Shell</c>, which is a different shape of thing.</summary>
public sealed class Carapace : BodyPartItem
{
    public override string ItemId      => "carapace";
    public override string DisplayName => "Carapace";
    public override string Description => "A hard hollow case with an oil-slick sheen to it, tough out of all proportion to its weight";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 2;
}

public sealed class Silk : BodyPartItem
{
    public override string ItemId      => "silk";
    public override string DisplayName => "Silk";
    public override string Article     => "some";
    public override string Description => "A wound skein of web, stronger along its length than anything of that thinness has a right to be";
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override int PriceReference => 4;
}

/// <summary>Bait. The reason a fisherman turns over stones.</summary>
public sealed class Grub : BodyPartItem
{
    public override string ItemId      => "grub";
    public override string DisplayName => "Grub";
    public override string Description => "A pale soft grub that will not stop moving, kept in a twist of leaf";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override int PriceReference => 1;
}

public sealed class Wax : BodyPartItem
{
    public override string ItemId      => "wax";
    public override string DisplayName => "Wax";
    public override string Article     => "some";
    public override string Description => "A knuckle of comb wax, warm-smelling and soft enough to take a thumbprint";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 5;
}

public sealed class Sting : BodyPartItem
{
    public override string ItemId      => "sting";
    public override string DisplayName => "Sting";
    public override string Description => "A barbed sting drawn out with its venom sac still attached, no longer than a fingernail paring";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 2;
}
