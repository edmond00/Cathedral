using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.Items;

// Variants of the weapons already in the game: other weights, levels and prices in the same categories,
// so that a fighter has a choice in every hand and a smith a reason to make more than one kind.

public sealed class Seax : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Blade });
    public override string ItemId      => "seax";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms, ItemTag.Pelt };
    public override string DisplayName => "Seax";
    public override string Description => "A broad single-edged knife-sword with a bone grip, the back thick and straight";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 4;
    public override string WeaponCategory => "short_blade";
}

public sealed class RondelDagger : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Blade });
    public override string ItemId      => "rondel_dagger";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms };
    public override string DisplayName => "Rondel Dagger";
    public override string Description => "A stiff thrusting dagger between two iron discs, made to find the gaps in mail";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 5;
    public override string WeaponCategory => "short_blade";
}

public sealed class Stiletto : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Blade });
    public override string ItemId      => "stiletto";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms };
    public override string DisplayName => "Stiletto";
    public override string Description => "A slender triangular blade with no edge at all, only a point";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 4;
    public override string WeaponCategory => "short_blade";
}

public sealed class Falchion : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Blade });
    public override string ItemId      => "falchion";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms };
    public override string DisplayName => "Falchion";
    public override string Description => "A heavy curved chopping sword, broad toward the point like a cleaver";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 5;
    public override string WeaponCategory => "saber";
}

public sealed class Scimitar : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Blade });
    public override string ItemId      => "scimitar";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms };
    public override string DisplayName => "Scimitar";
    public override string Description => "A light deeply curved sword from the south, made to draw across rather than to chop";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 5;
    public override string WeaponCategory => "saber";
}

public sealed class Mace : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Bludgeon });
    public override string ItemId      => "mace";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms };
    public override string DisplayName => "Mace";
    public override string Description => "An iron-headed mace on an ash haft, the head studded with blunt knobs";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 5;
    public override string WeaponCategory => "blunt";
}

public sealed class FlangedMace : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Bludgeon });
    public override string ItemId      => "flanged_mace";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms };
    public override string DisplayName => "Flanged Mace";
    public override string Description => "A mace of six iron flanges on a long haft, made to dent plate";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Heavy;
    public override int     Level      => 6;
    public override string WeaponCategory => "blunt";
}

public sealed class MorningStar : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Bludgeon });
    public override string ItemId      => "morning_star";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms };
    public override string DisplayName => "Morning Star";
    public override string Description => "A spiked iron ball on a stout club, ugly and very effective";
    public override WeaponSize Bulk      => WeaponSize.Large;
    public override WeightClass   Weight     => WeightClass.Heavy;
    public override int     Level      => 6;
    public override string WeaponCategory => "blunt";
}

public sealed class HorsemansPick : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Bludgeon });
    public override string ItemId      => "horsemans_pick";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms };
    public override string DisplayName => "Horseman's Pick";
    public override string Description => "A short-hafted war pick light enough for one hand in the saddle";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 4;
    public override string WeaponCategory => "pickaxe";
}

public sealed class BeardedAxe : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Axe });
    public override string ItemId      => "bearded_axe";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Arms, ItemTag.Timber };
    public override string DisplayName => "Bearded Axe";
    public override string Description => "A light axe whose long-hooked blade can drag a shield down";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 4;
    public override string WeaponCategory => "axe";
}

public sealed class Javelin : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Spear });
    public override string ItemId      => "javelin";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork };
    public override string DisplayName => "Javelin";
    public override string Description => "A light ash spear with a narrow iron head, balanced for throwing";
    public override WeaponSize Bulk      => WeaponSize.Large;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 4;
    public override string WeaponCategory => "spear";
}

public sealed class LongPike : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Spear });
    public override string ItemId      => "long_pike";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork, ItemTag.Ironwork };
    public override string DisplayName => "Long Pike";
    public override string Description => "A very long ash shaft with a small square iron head, made to stand against horses";
    public override WeaponSize Bulk      => WeaponSize.Large;
    public override WeightClass   Weight     => WeightClass.Heavy;
    public override int     Level      => 6;
    public override string WeaponCategory => "spear";
}

public sealed class Glaive : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Spear });
    public override string ItemId      => "glaive";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork, ItemTag.Ironwork };
    public override string DisplayName => "Glaive";
    public override string Description => "A single-edged blade on a long pole, curved like a scythe set straight";
    public override WeaponSize Bulk      => WeaponSize.Large;
    public override WeightClass   Weight     => WeightClass.Heavy;
    public override int     Level      => 6;
    public override string WeaponCategory => "spear";
}

public sealed class DaneAxe : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Axe });
    public override string ItemId      => "dane_axe";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork, ItemTag.Ironwork };
    public override string DisplayName => "Dane Axe";
    public override string Description => "A broad thin axe head on a shaft as tall as a man, swung with both hands";
    public override WeaponSize Bulk      => WeaponSize.Large;
    public override WeightClass   Weight     => WeightClass.Heavy;
    public override int     Level      => 6;
    public override string WeaponCategory => "axe";
}

public sealed class ThrowingAxe : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Axe });
    public override string ItemId      => "throwing_axe";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork, ItemTag.Ironwork };
    public override string DisplayName => "Throwing Axe";
    public override string Description => "A small axe with a short haft and a forward-heavy head, for throwing";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 4;
    public override string WeaponCategory => "axe";
}

public sealed class ShortBow : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Bow });
    public override string ItemId      => "short_bow";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork };
    public override string DisplayName => "Short Bow";
    public override string Description => "A small bow of ash for shooting from cover or from the saddle";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 4;
    public override string WeaponCategory => "bow";
}

public sealed class CompositeBow : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Bow });
    public override string ItemId      => "composite_bow";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork };
    public override string DisplayName => "Composite Bow";
    public override string Description => "A recurved bow of horn, wood and sinew, small but very strong";
    public override WeaponSize Bulk      => WeaponSize.Large;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 5;
    public override string WeaponCategory => "bow";
}

public sealed class HandCrossbow : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Bow });
    public override string ItemId      => "hand_crossbow";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork, ItemTag.Ironwork };
    public override string DisplayName => "Hand Crossbow";
    public override string Description => "A small crossbow spanned by hand, for a quick bolt at close range";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 4;
    public override string WeaponCategory => "crossbow";
}

public sealed class Buckler : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Shield });
    public override string ItemId      => "buckler";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork, ItemTag.Ironwork };
    public override string DisplayName => "Buckler";
    public override string Description => "A small round fist-shield with an iron boss, for turning blows aside";
    public override WeaponSize Bulk      => WeaponSize.Medium;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 4;
    public override string WeaponCategory => "shield";
}

public sealed class KiteShield : WeaponItem
{
    public override IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Shield });
    public override string ItemId      => "kite_shield";
    public override List<ItemTag> Tags => new() { ItemTag.Arms, ItemTag.Woodwork };
    public override string DisplayName => "Kite Shield";
    public override string Description => "A tall shield tapering to a point, covering a rider from shoulder to knee";
    public override WeaponSize Bulk      => WeaponSize.Large;
    public override WeightClass   Weight     => WeightClass.Heavy;
    public override int     Level      => 5;
    public override string WeaponCategory => "shield";
}
