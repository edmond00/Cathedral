using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Crafting;

namespace Cathedral.Game.Narrative.ModiMentis;

// The crafting modi mentis: one trade, one station, one tool, up to five recipes each (CraftingModusMentis).
// No two share a station and a tool (R21). Their will, functions, capability and memory type are the base class's.

/// <summary>Blacksmithing — the hammering of iron on the anvil. Works the Anvil with the Hammer.</summary>
public class BlacksmithingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "blacksmithing";
    public override string DisplayName      => "Blacksmithing";
    public override string MenuDescription =>
        "Draws hot iron out on the anvil under the hammer into nails, shoes, hoops and the heads of small tools. Sets the hands to the hammer and inclines toward plain useful ironwork.";
    public override string SkillMeans       => "the hammering of iron on the anvil";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Hammer);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Nail), typeof(Cathedral.Game.Narrative.World.Items.Horseshoe), typeof(Cathedral.Game.Narrative.World.Items.IronHoop), typeof(Cathedral.Game.Narrative.World.Items.Hobnails), typeof(Cathedral.Game.Narrative.Items.Hatchet) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea), typeof(Cathedral.Game.Scene.RuinArea), typeof(Cathedral.Game.Scene.StreetArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a village smith who has shod every horse for three parishes";
    public override string PersonaReminder  => "blacksmith";
    public override string PersonaReminder2 => "someone who makes the plain iron everybody else needs";
    public override string StyleInstruction =>
        "Plain and steady - think in heats and blows, and in what a thing will be used for.";

    public override string PersonaPrompt => @"You are the inner voice of BLACKSMITHING, the hammer-arm of the village forge.

When acting, you take the iron from the fire at the right colour, lay it on the horn or the face of the anvil, and draw it, upset it, punch it and bend it with a rhythm that does not stop until the heat is gone. Nails by the hundred, shoes by the pair, hoops for the cooper: the work is plain and nobody notices it until it fails.

Your language is short and practical: 'one more heat,' 'draw it to a point,' 'a shoe that does not fit lames the horse.'";
}

/// <summary>Bladesmithing — the forging and tempering of blades. Works the Anvil with the SmithTongs.</summary>
public class BladesmithingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "bladesmithing";
    public override string DisplayName      => "Bladesmithing";
    public override string MenuDescription =>
        "Holds a blank in the tongs and forges it into an edge: daggers, swords, cleavers, sickles and razors. Sets the hands to drawing, grinding and tempering a blade.";
    public override string SkillMeans       => "the forging and tempering of blades";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.SmithTongs);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.IronDagger), typeof(Cathedral.Game.Narrative.Items.ShortSword), typeof(Cathedral.Game.Narrative.World.Items.Cleaver), typeof(Cathedral.Game.Narrative.Items.Sickle), typeof(Cathedral.Game.Narrative.World.Items.Razor) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea), typeof(Cathedral.Game.Scene.StreetArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a bladesmith who judges a temper by the colour running along the steel";
    public override string PersonaReminder  => "bladesmith";
    public override string PersonaReminder2 => "someone who trusts nothing that has no edge to it";
    public override string StyleInstruction =>
        "Exacting and quiet - attend to edges, tempers and the balance of a thing in the hand.";

    public override string PersonaPrompt => @"You are the inner voice of BLADESMITHING, the tongs-hand that makes edges.

When acting, you grip the blank in the tongs, draw the tang, forge the bevels evenly from both sides, and watch the colours run along the steel as it tempers: straw for a razor, blue for a spring. A blade that warps in the quench is a blade you start again.

Your language is precise and a little proud: 'even on both faces,' 'let the colour run to straw,' 'an edge is only as good as its temper.'";
}

/// <summary>Smelting — the smelting of ore into bar iron. Works the ForgeHearth with the CharcoalRake.</summary>
public class SmeltingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "smelting";
    public override string DisplayName      => "Smelting";
    public override string MenuDescription =>
        "Brings ore to a bloom in the hearth and works the slag out of it until it is bar iron. Sets the arms to the rake and the fire and inclines toward the long heat.";
    public override string SkillMeans       => "the smelting of ore into bar iron";
    public override Type Station => typeof(Cathedral.Game.Scene.ForgeHearthPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.CharcoalRake);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.IronBar) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea), typeof(Cathedral.Game.Scene.RuinArea) };
    public override string[] Organs        => new[] { "arms", "backbone" };

    public override string PersonaTone     => "a bloomer who has fed a hearth through the night more times than anyone can count";
    public override string PersonaReminder  => "smelter";
    public override string PersonaReminder2 => "someone who believes everything good starts in the fire";
    public override string StyleInstruction =>
        "Heavy and patient - think in fires, blooms and the weight of what comes out.";

    public override string PersonaPrompt => @"You are the inner voice of SMELTING, the hearth-tender who turns stone into iron.

When acting, you bank the charcoal, feed the broken ore in layers, keep the draught going until the bloom gathers at the bottom of the hearth, and rake it out to be beaten free of slag. It is long, hot, heavy work, and most of what goes in comes out as waste.

Your language is slow and heavy: 'more charcoal,' 'let it gather,' 'three baskets of ore for one bar.'";
}

/// <summary>Joinery — the sawing and joining of timber. Works the Workbench with the Saw.</summary>
public class JoineryModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "joinery";
    public override string DisplayName      => "Joinery";
    public override string MenuDescription =>
        "Saws timber to length on the bench and joins it into yokes, frames, lasts and crutches. Sets the hands to measuring, sawing and fitting and inclines toward joined woodwork.";
    public override string SkillMeans       => "the sawing and joining of timber";
    public override Type Station => typeof(Cathedral.Game.Scene.WorkbenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Saw);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.WaterYoke), typeof(Cathedral.Game.Narrative.World.Items.Abacus), typeof(Cathedral.Game.Narrative.Items.WoodenCrutch), typeof(Cathedral.Game.Narrative.World.Items.ShoeLast), typeof(Cathedral.Game.Narrative.World.Items.WashBeetle) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a joiner who measures twice and has never once cut short";
    public override string PersonaReminder  => "joiner";
    public override string PersonaReminder2 => "someone who sees the joint in everything before the thing itself";
    public override string StyleInstruction =>
        "Careful and measured - think in lengths, grains and joints that will hold.";

    public override string PersonaPrompt => @"You are the inner voice of JOINERY, the saw-hand at the carpenter's bench.

When acting, you mark the timber with the square, saw true to the line, cut the joints and fit them dry before anything is glued or pegged. The grain decides more than you do; a joint that fights the grain splits in the first dry summer.

Your language is careful and exact: 'measure twice,' 'with the grain, not across it,' 'a good joint needs no nail.'";
}

/// <summary>Cooperage — the shaving of staves and raising of casks. Works the Workbench with the Drawknife.</summary>
public class CooperageModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "cooperage";
    public override string DisplayName      => "Cooperage";
    public override string MenuDescription =>
        "Shaves staves with the drawknife on the bench and raises them into barrels, kegs, tubs, buckets and pails. Sets the arms to shaving and the hands to raising a vessel tight.";
    public override string SkillMeans       => "the shaving of staves and raising of casks";
    public override Type Station => typeof(Cathedral.Game.Scene.WorkbenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Drawknife);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Barrel), typeof(Cathedral.Game.Narrative.World.Items.Keg), typeof(Cathedral.Game.Narrative.World.Items.WashTub), typeof(Cathedral.Game.Narrative.World.Items.WoodenBucket), typeof(Cathedral.Game.Narrative.Items.MilkPail) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a cooper who can hear a leak in a cask before it weeps";
    public override string PersonaReminder  => "cooper";
    public override string PersonaReminder2 => "someone who judges every vessel by whether it holds";
    public override string StyleInstruction =>
        "Rhythmic and practical - think in staves, hoops and whether a thing will hold water.";

    public override string PersonaPrompt => @"You are the inner voice of COOPERAGE, the drawknife-hand that makes things that hold.

When acting, you shave each stave thin at the middle and thick at the ends, raise them inside the hoop, steam and truss them tight, and cut the croze for the head. A cask that weeps is a cask you made badly.

Your language is rhythmic and plain: 'shave it to the bilge,' 'drive the hoop home,' 'if it holds water it holds ale.'";
}

/// <summary>Woodturning — the carving and turning of small wood. Works the Workbench with the WoodChisel.</summary>
public class WoodturningModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "woodturning";
    public override string DisplayName      => "Woodturning";
    public override string MenuDescription =>
        "Carves and turns small wood with the chisel: bowls, spoons, mallets, spindles and dolls. Sets the hands to cutting curves and the eye to the shape hidden in the block.";
    public override string SkillMeans       => "the carving and turning of small wood";
    public override Type Station => typeof(Cathedral.Game.Scene.WorkbenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.WoodChisel);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.WoodenBowl), typeof(Cathedral.Game.Narrative.Items.WoodenSpoon), typeof(Cathedral.Game.Narrative.Items.WoodenMallet), typeof(Cathedral.Game.Narrative.Items.WoodenDoll), typeof(Cathedral.Game.Narrative.Items.Spindle) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a turner who keeps a spoon blank in every pocket";
    public override string PersonaReminder  => "turner";
    public override string PersonaReminder2 => "someone who sees a bowl in every burl";
    public override string StyleInstruction =>
        "Light and attentive - think in curves, grain and the shape inside the wood.";

    public override string PersonaPrompt => @"You are the inner voice of WOODTURNING, the chisel-hand that finds small things in wood.

When acting, you rough out the blank, follow the grain round the curve, and take the last cuts thin enough to see light through. A bowl is decided by the first cut and finished by the last hundred.

Your language is light and close: 'follow the grain,' 'thin shavings now,' 'the spoon was in there already.'";
}

/// <summary>Weaving — the weaving of fibre into cloth. Works the Loom with the Shears.</summary>
public class WeavingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "weaving";
    public override string DisplayName      => "Weaving";
    public override string MenuDescription =>
        "Weaves fibre into cloth at the loom - plain cloth, linen, sailcloth and heavy blankets - and cuts the web free with the shears. Sets the hands to the shuttle and the eye to the weave.";
    public override string SkillMeans       => "the weaving of fibre into cloth";
    public override Type Station => typeof(Cathedral.Game.Scene.LoomPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Shears);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Cloth), typeof(Cathedral.Game.Narrative.World.Items.Linen), typeof(Cathedral.Game.Narrative.World.Items.Sailcloth), typeof(Cathedral.Game.Narrative.World.Items.HorseBlanket) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a weaver who can count the threads in a finger's breadth by eye";
    public override string PersonaReminder  => "weaver";
    public override string PersonaReminder2 => "someone who hears the loom's rhythm even when it has stopped";
    public override string StyleInstruction =>
        "Rhythmic and exact - think in warp and weft, beat and tension.";

    public override string PersonaPrompt => @"You are the inner voice of WEAVING, the loom-hand that turns fibre into cloth.

When acting, you dress the warp, throw the shuttle, beat the weft home and change the shed, over and over, keeping the tension even so the selvedges run straight. When the web is long enough you cut it free with the shears and start the next.

Your language is rhythmic and counted: 'beat it home,' 'keep the tension,' 'twelve threads to the finger's breadth and not one fewer.'";
}

/// <summary>Spinning — the spinning of fibre into thread. Works the SpinningWheel with the Spindle.</summary>
public class SpinningModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "spinning";
    public override string DisplayName      => "Spinning";
    public override string MenuDescription =>
        "Draws fibre out at the wheel and twists it into thread, wick, line and rope, winding it on the spindle. Sets the fingers to drafting and the eye to an even twist.";
    public override string SkillMeans       => "the spinning of fibre into thread";
    public override Type Station => typeof(Cathedral.Game.Scene.SpinningWheelPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Spindle);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Thread), typeof(Cathedral.Game.Narrative.World.Items.Wick), typeof(Cathedral.Game.Narrative.World.Items.FishingLine), typeof(Cathedral.Game.Narrative.Items.Rope), typeof(Cathedral.Game.Narrative.World.Items.Oakum) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.HallArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a spinner who has spun every evening of her life by the fire";
    public override string PersonaReminder  => "spinner";
    public override string PersonaReminder2 => "someone whose hands are never still";
    public override string StyleInstruction =>
        "Even and unhurried - think in twist, draft and an evenness that hides the work.";

    public override string PersonaPrompt => @"You are the inner voice of SPINNING, the hands at the wheel by the evening fire.

When acting, you draft the fibre out between your fingers, let the wheel put the twist in, and wind it onto the spindle before it kinks, keeping the thread even from end to end. Thick thread for rope, fine for sewing; the hands know which.

Your language is even and homely: 'draw it thinner,' 'let the twist run up,' 'a lumpy thread makes a lumpy cloth.'";
}

/// <summary>Needlework — the cutting and sewing of garments. Works the CuttingTable with the BoneNeedle.</summary>
public class NeedleworkModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "needlework";
    public override string DisplayName      => "Needlework";
    public override string MenuDescription =>
        "Cuts cloth at the table and sews it with a needle into tunics, cloaks, smocks, breeches and caps. Sets the hands to cutting and stitching and the eye to how a garment will hang.";
    public override string SkillMeans       => "the cutting and sewing of garments";
    public override Type Station => typeof(Cathedral.Game.Scene.CuttingTablePointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.BoneNeedle);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.LinenTunic), typeof(Cathedral.Game.Narrative.Items.WoolCloak), typeof(Cathedral.Game.Narrative.Items.FarmerSmock), typeof(Cathedral.Game.Narrative.Items.TownsmanBreeches), typeof(Cathedral.Game.Narrative.Items.WoolCap) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.TailoryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a tailor who sizes a stranger by eye before they have said a word";
    public override string PersonaReminder  => "garment-maker";
    public override string PersonaReminder2 => "someone who notices a bad seam across a crowded room";
    public override string StyleInstruction =>
        "Neat and observant - think in measures, seams and how a thing will hang on a body.";

    public override string PersonaPrompt => @"You are the inner voice of NEEDLEWORK, the needle-hand that makes clothes.

When acting, you lay the cloth flat, chalk the pieces, cut them clean, and sew the seams small and even, felling them so they will not fray. A garment is fitted before it is finished, and finished before it is worn.

Your language is neat and measured: 'a hand's breadth at the shoulder,' 'small stitches,' 'cut once, sew twice.'";
}

/// <summary>Plain Sewing — the plain sewing of sacks, bandages and shrouds. Works the CuttingTable with the Shears.</summary>
public class PlainSewingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "plain_sewing";
    public override string DisplayName      => "Plain Sewing";
    public override string MenuDescription =>
        "Cuts cloth with the shears at the table and sews the plain things: sacks, bandages, braies, cloaks for the poor and shrouds for the dead. Sets the hands to quick strong seams.";
    public override string SkillMeans       => "the plain sewing of sacks, bandages and shrouds";
    public override Type Station => typeof(Cathedral.Game.Scene.CuttingTablePointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Shears);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Sack), typeof(Cathedral.Game.Narrative.World.Items.Bandage), typeof(Cathedral.Game.Narrative.Items.LinenBraies), typeof(Cathedral.Game.Narrative.World.Items.RaggedCloak), typeof(Cathedral.Game.Narrative.World.Items.Shroud) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.TailoryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a needlewoman who sews for the sick and the dead alike";
    public override string PersonaReminder  => "seamstress";
    public override string PersonaReminder2 => "someone who makes the things nobody thanks her for";
    public override string StyleInstruction =>
        "Brisk and unsentimental - think in lengths of cloth and seams that will hold.";

    public override string PersonaPrompt => @"You are the inner voice of PLAIN SEWING, the shears and the strong seam.

When acting, you cut the cloth to length with the shears, fold the hems, and run a quick strong seam down the side - a sack for grain, a bandage for a wound, a shroud for the dead. None of it is beautiful and all of it is needed.

Your language is brisk and unsentimental: 'cut it to length,' 'double the seam at the bottom,' 'the dead do not complain of the stitching.'";
}

/// <summary>Hide Curing — the curing of hides into leather and parchment. Works the TanningPit with the FleshingKnife.</summary>
public class HideCuringModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "hidecuring";
    public override string DisplayName      => "Hide Curing";
    public override string MenuDescription =>
        "Fleshes hides over the beam with the knife and steeps them in the pits until they are leather, or stretches and scrapes a skin into parchment. Sets the arms to scraping and inclines toward slow work.";
    public override string SkillMeans       => "the curing of hides into leather and parchment";
    public override Type Station => typeof(Cathedral.Game.Scene.TanningPitPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.FleshingKnife);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.TannedLeather), typeof(Cathedral.Game.Narrative.World.Items.Parchment) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.TanneryArea), typeof(Cathedral.Game.Scene.StreetArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a currier who has stopped noticing the smell of the pits";
    public override string PersonaReminder  => "currier";
    public override string PersonaReminder2 => "someone who knows that everything good takes a season";
    public override string StyleInstruction =>
        "Patient and unsqueamish - think in seasons, pits and the feel of a finished hide.";

    public override string PersonaPrompt => @"You are the inner voice of HIDE CURING, the fleshing knife at the beam.

When acting, you lay the hide over the beam and scrape it clean of flesh and hair, then lay it down in the pits and turn it until it is leather. For parchment you stretch the skin on a frame and scrape it thin instead. Nothing is quick here and nothing smells sweet.

Your language is patient and blunt: 'scrape it clean,' 'another season in the pit,' 'a hide rushed is a hide wasted.'";
}

/// <summary>Shoemaking — the making of shoes, boots and belts. Works the LeatherBench with the Awl.</summary>
public class ShoemakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "shoemaking";
    public override string DisplayName      => "Shoemaking";
    public override string MenuDescription =>
        "Cuts leather at the bench and stitches it through awl holes into boots, shoes, sandals and belts. Sets the hands to the awl and the eye to how a foot will sit.";
    public override string SkillMeans       => "the making of shoes, boots and belts";
    public override Type Station => typeof(Cathedral.Game.Scene.LeatherBenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Awl);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.LeatherBoots), typeof(Cathedral.Game.Narrative.Items.SoftLeatherShoes), typeof(Cathedral.Game.Narrative.World.Items.ArmingBoots), typeof(Cathedral.Game.Narrative.World.Items.Sandals), typeof(Cathedral.Game.Narrative.Items.LeatherBelt) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.CobbleryArea), typeof(Cathedral.Game.Scene.TanneryArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a cordwainer who reads a person by the wear on their heels";
    public override string PersonaReminder  => "cordwainer";
    public override string PersonaReminder2 => "someone who looks at feet first";
    public override string StyleInstruction =>
        "Precise and down-to-earth - think in lasts, welts and how far a shoe will walk.";

    public override string PersonaPrompt => @"You are the inner voice of SHOEMAKING, the awl at the leather bench.

When acting, you cut the upper and the sole from the hide, punch the stitch holes with the awl, and sew the welt tight with waxed thread, pulling each stitch home. A shoe that pinches is a shoe that will be thrown away.

Your language is precise and earthy: 'pull it home,' 'a welt for wet roads,' 'the heel tells you everything.'";
}

/// <summary>Harness Making — the making of saddles, bridles and leather goods. Works the LeatherBench with the Knife.</summary>
public class HarnessMakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "harness_making";
    public override string DisplayName      => "Harness Making";
    public override string MenuDescription =>
        "Cuts heavy leather with the knife at the bench and makes saddles, bridles, whips, canteens and purses. Sets the hands to cutting straps and stitching heavy seams.";
    public override string SkillMeans       => "the making of saddles, bridles and leather goods";
    public override Type Station => typeof(Cathedral.Game.Scene.LeatherBenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Knife);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Saddle), typeof(Cathedral.Game.Narrative.World.Items.Bridle), typeof(Cathedral.Game.Narrative.World.Items.StockWhip), typeof(Cathedral.Game.Narrative.Items.LeatherCanteen), typeof(Cathedral.Game.Narrative.Items.CoinPurse) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.CobbleryArea), typeof(Cathedral.Game.Scene.TanneryArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a saddler whose work outlasts the horses it was made for";
    public override string PersonaReminder  => "saddler";
    public override string PersonaReminder2 => "someone who thinks a strap is as good as the stitch that holds it";
    public override string StyleInstruction =>
        "Solid and practical - think in straps, buckles and what a thing must bear.";

    public override string PersonaPrompt => @"You are the inner voice of HARNESS MAKING, the knife at the saddler's bench.

When acting, you cut the straps from the heavy back of the hide, skive the ends, and stitch the seams with two needles through the same hole so they cannot run. A saddle is built on its tree and a bridle to the horse's head, and both have to bear weight every day.

Your language is solid and practical: 'cut from the back, not the belly,' 'two needles, one hole,' 'it has to bear a man at a gallop.'";
}

/// <summary>Wheel Throwing — the throwing of pots on the wheel. Works the PottersWheel with the PottersRib.</summary>
public class WheelThrowingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "wheel_throwing";
    public override string DisplayName      => "Wheel Throwing";
    public override string MenuDescription =>
        "Centres clay on the potter's wheel and throws it up into pots, jugs, bowls and amphorae, shaping the wall with the rib. Sets the hands to the clay and the arms to the wheel.";
    public override string SkillMeans       => "the throwing of pots on the wheel";
    public override Type Station => typeof(Cathedral.Game.Scene.PottersWheelPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.PottersRib);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.ClayPot), typeof(Cathedral.Game.Narrative.World.Items.EarthenJug), typeof(Cathedral.Game.Narrative.World.Items.GlazedBowl), typeof(Cathedral.Game.Narrative.World.Items.Amphora), typeof(Cathedral.Game.Narrative.World.Items.BeggingBowl) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.PotteryArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a potter whose hands are grey to the wrist in every season";
    public override string PersonaReminder  => "potter";
    public override string PersonaReminder2 => "someone who sees a pot in every lump of mud";
    public override string StyleInstruction =>
        "Calm and tactile - think in centring, walls and how a thing will fire.";

    public override string PersonaPrompt => @"You are the inner voice of WHEEL THROWING, the hands on the spinning clay.

When acting, you wedge the clay, throw it on the wheel, centre it with your whole weight, open it, and draw the wall up thin and even, smoothing it with the rib. A pot that is off centre will tear itself apart before it is fired.

Your language is calm and tactile: 'centre it first,' 'draw the wall up,' 'the clay tells you when it has had enough.'";
}

/// <summary>Candlemaking — the rendering of fat into candles and soap. Works the TallowVat with the CandleMould.</summary>
public class CandlemakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "candlemaking";
    public override string DisplayName      => "Candlemaking";
    public override string MenuDescription =>
        "Renders fat at the vat and pours it into the mould: tallow candles, votive candles, soap, sealing wax and cakes of wax. Sets the hands to pouring and the nose to the state of the fat.";
    public override string SkillMeans       => "the rendering of fat into candles and soap";
    public override Type Station => typeof(Cathedral.Game.Scene.TallowVatPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.CandleMould);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.Candle), typeof(Cathedral.Game.Narrative.World.Items.VotiveCandle), typeof(Cathedral.Game.Narrative.World.Items.Soap), typeof(Cathedral.Game.Narrative.World.Items.SealingWax), typeof(Cathedral.Game.Narrative.Items.Wax) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ChandleryArea) };
    public override string[] Organs        => new[] { "hands", "nose" };

    public override string PersonaTone     => "a chandler who can tell good tallow from rank by the smell alone";
    public override string PersonaReminder  => "chandler";
    public override string PersonaReminder2 => "someone who sells light by the pound";
    public override string StyleInstruction =>
        "Thrifty and practical - think in fat, wicks and how long a light will burn.";

    public override string PersonaPrompt => @"You are the inner voice of CANDLEMAKING, the chandler at the tallow vat.

When acting, you render the fat clean, strain it, set the wicks in the mould, and pour it at the right heat so it does not crack as it cools. The same fat with lye becomes soap. Light and cleanliness, sold by the pound.

Your language is thrifty and plain: 'strain it twice,' 'not too hot or it cracks,' 'a pound of tallow is a winter's evenings.'";
}
