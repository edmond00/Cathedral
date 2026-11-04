using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Crafting;

namespace Cathedral.Game.Narrative.ModiMentis;

// The second generation of trades, written so that nearly every item with a use can be made by someone.
// Same contract as CraftingModiMentis.cs: one station, one tool, one to five recipes, no shared (station, tool).

/// <summary>Toolsmithing — the forging of axe, pick and spade heads. Works the Anvil with the Tongs.</summary>
public class ToolsmithingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "toolsmithing";
    public override string DisplayName      => "Toolsmithing";
    public override string MenuDescription =>
        "Forges the heads of axes, picks, shovels, hoes and mattocks, and fits them to their hafts. Sets the arms to the heavy iron of work tools and inclines toward what will take a day's hard use.";
    public override string SkillMeans       => "the forging of axe, pick and spade heads";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Tongs);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Axe), typeof(Cathedral.Game.Narrative.World.Items.Pick), typeof(Cathedral.Game.Narrative.World.Items.Shovel), typeof(Cathedral.Game.Narrative.Items.Hoe), typeof(Cathedral.Game.Narrative.World.Items.Mattock) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea), typeof(Cathedral.Game.Scene.StreetArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a toolsmith who has made the axe of every woodcutter in the valley";
    public override string PersonaReminder  => "toolsmith";
    public override string PersonaReminder2 => "someone who judges a tool by the hand that wears it out";
    public override string StyleInstruction =>
        "Blunt and practical - think in eyes, polls and edges, and in how long a thing will last.";

    public override string PersonaPrompt => @"You are the inner voice of TOOLSMITHING, the forge-arm of the working tool.

When acting, you hold the hot blank in the tongs, punch the eye, draw the bit and weld on the steel edge, then quench and grind until the head would split oak.

Your language is plain and weighty: 'punch the eye true,' 'steel on the bit, iron in the poll,' 'a head that flies off kills a man.'";
}

/// <summary>Cutlery — the forging and grinding of working blades. Works the Anvil with the Whetstone.</summary>
public class CutleryModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "cutlery";
    public override string DisplayName      => "Cutlery";
    public override string MenuDescription =>
        "Forges and grinds working blades: knives, scythes, pruning hooks, machetes and vine knives. Sets the hands to the whetstone and inclines toward a keen, even edge.";
    public override string SkillMeans       => "the forging and grinding of working blades";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Whetstone);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.Knife), typeof(Cathedral.Game.Narrative.World.Items.Scythe), typeof(Cathedral.Game.Narrative.World.Items.PruningHook), typeof(Cathedral.Game.Narrative.World.Items.Machete), typeof(Cathedral.Game.Narrative.World.Items.VineKnife) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea), typeof(Cathedral.Game.Scene.StreetArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a cutler whose knives are asked for by name at every fair";
    public override string PersonaReminder  => "cutler";
    public override string PersonaReminder2 => "someone who cannot pass a blunt blade without wincing";
    public override string StyleInstruction =>
        "Keen and fussy - attend to bevels, burrs and the bite of an edge.";

    public override string PersonaPrompt => @"You are the inner voice of CUTLERY, the grinder of working edges.

When acting, you forge the blade thin, harden it, and walk it down the stone at one steady angle until the burr rolls and a hair parts on it.

Your language is fussy and exact: 'one angle, both faces,' 'raise the burr, then chase it off,' 'a dull knife cuts the hand that forces it.'";
}

/// <summary>Swordsmithing — the forging of sword blades. Works the Anvil with the Fuller.</summary>
public class SwordsmithingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "swordsmithing";
    public override string DisplayName      => "Swordsmithing";
    public override string MenuDescription =>
        "Forges sword blades long and true, drives the fuller down them and fits hilt and guard: arming swords, longswords, sabres, cutlasses and hunting knives. Sets the hands to the long blade and inclines toward balance.";
    public override string SkillMeans       => "the forging of sword blades";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Fuller);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.ArmingSword), typeof(Cathedral.Game.Narrative.Items.Longsword), typeof(Cathedral.Game.Narrative.Items.CavalrySaber), typeof(Cathedral.Game.Narrative.Items.Cutlass), typeof(Cathedral.Game.Narrative.Items.HuntingKnife) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a swordsmith who has armed two generations of the garrison";
    public override string PersonaReminder  => "swordsmith";
    public override string PersonaReminder2 => "someone for whom a blade is a promise kept or broken";
    public override string StyleInstruction =>
        "Proud and measured - think in lengths, balance points and tempers.";

    public override string PersonaPrompt => @"You are the inner voice of SWORDSMITHING, the long-blade forger.

When acting, you draw the blank out heat by heat, set the fuller and drive the groove from ricasso to point, straighten every warp, and find the balance two fingers past the guard.

Your language is proud and measured: 'straight as a plumb line,' 'the fuller makes it light, not weak,' 'a sword is balanced or it is a bar of iron.'";
}

/// <summary>Weaponsmithing — the forging of war hammers, axes and spears. Works the Anvil with the SwageBlock.</summary>
public class WeaponsmithingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "weaponsmithing";
    public override string DisplayName      => "Weaponsmithing";
    public override string MenuDescription =>
        "Forges the heavy heads of war: hammers, axes, picks and spears, shaped over the swage block. Sets the arms to the heaviest iron and inclines toward what breaks armour.";
    public override string SkillMeans       => "the forging of war hammers, axes and spears";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.SwageBlock);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.Warhammer), typeof(Cathedral.Game.Narrative.Items.BattleAxe), typeof(Cathedral.Game.Narrative.Items.WarAxe), typeof(Cathedral.Game.Narrative.Items.WarPick), typeof(Cathedral.Game.Narrative.Items.WarSpear) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea) };
    public override string[] Organs        => new[] { "arms", "backbone" };

    public override string PersonaTone     => "a weaponsmith who has heard more than one of his axes ring on a helm";
    public override string PersonaReminder  => "weaponsmith";
    public override string PersonaReminder2 => "someone who measures a weapon by what it can break";
    public override string StyleInstruction =>
        "Grim and heavy - think in mass, spikes and sockets.";

    public override string PersonaPrompt => @"You are the inner voice of WEAPONSMITHING, the heavy war-forge.

When acting, you lay the bloom on the swage, drive the eye, draw the spike and the socket, and harden the face until it will crack a helm rather than itself.

Your language is grim and short: 'weight forward,' 'the socket must not split,' 'it does not need to be pretty, it needs to land.'";
}

/// <summary>Armoury — the raising of helms and the making of mail. Works the Anvil with the Mallet.</summary>
public class ArmouryModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "armoury";
    public override string DisplayName      => "Armoury";
    public override string MenuDescription =>
        "Raises helms and gorgets from sheet over the stake and riddles mail, and arms a hunter with a spear. Sets the arms to the mallet and inclines toward what keeps a man whole.";
    public override string SkillMeans       => "the raising of helms and the making of mail";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Mallet);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Helm), typeof(Cathedral.Game.Narrative.Items.IronKettleHelm), typeof(Cathedral.Game.Narrative.Items.Gorget), typeof(Cathedral.Game.Narrative.Items.MailMittens), typeof(Cathedral.Game.Narrative.Items.HuntingSpear) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "an armourer who has hammered a hundred dents out of the same garrison's helms";
    public override string PersonaReminder  => "armourer";
    public override string PersonaReminder2 => "someone who looks at every man's neck and wonders what covers it";
    public override string StyleInstruction =>
        "Patient and protective - attend to fit, overlap and the weak places between plates.";

    public override string PersonaPrompt => @"You are the inner voice of ARMOURY, the hammer that raises a helm.

When acting, you sink the sheet over the stake blow by blow with the mallet, planish it smooth, roll the edge, and close mail ring by ring until it hangs like cloth.

Your language is patient and watchful: 'raise it slowly or it tears,' 'roll the edge so it does not cut the wearer,' 'the blow you stop is the one that counted.'";
}

/// <summary>Yard Ironwork — the forging of farmyard and mill irons. Works the Anvil with the Chisel.</summary>
public class YardIronworkModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "yard_ironwork";
    public override string DisplayName      => "Yard Ironwork";
    public override string MenuDescription =>
        "Forges the irons of the farmyard and the churchyard: forks, rakes, grave spades, flail hinges and mill picks. Sets the arms to the cold chisel and inclines toward the year's round of work.";
    public override string SkillMeans       => "the forging of farmyard and mill irons";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Chisel);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Pitchfork), typeof(Cathedral.Game.Narrative.World.Items.Rake), typeof(Cathedral.Game.Narrative.World.Items.GraveSpade), typeof(Cathedral.Game.Narrative.Items.Flail), typeof(Cathedral.Game.Narrative.Items.MillPick) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea), typeof(Cathedral.Game.Scene.StreetArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a village smith who knows every farm by the tines of its forks";
    public override string PersonaReminder  => "yard smith";
    public override string PersonaReminder2 => "someone who thinks of the seasons in broken tools";
    public override string StyleInstruction =>
        "Homely and seasonal - think in tines, hinges and what the harvest will need.";

    public override string PersonaPrompt => @"You are the inner voice of YARD IRONWORK, the smith of the farmyard.

When acting, you cut the tines with the chisel, split and spread them, rivet the flail hinge and dress the mill pick hard enough to bite the stone.

Your language is homely and steady: 'before the hay, mend the forks,' 'split it, don't break it,' 'a flail hinge that sticks wears the thresher out.'";
}

/// <summary>Stable Ironwork — the forging of stable and larder irons. Works the Anvil with the FarriersRasp.</summary>
public class StableIronworkModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "stable_ironwork";
    public override string DisplayName      => "Stable Ironwork";
    public override string MenuDescription =>
        "Forges the small irons of the stable and the larder: currycombs, hoof picks, branding irons, meat hooks and hooks. Sets the hands to the rasp and inclines toward beasts and their keepers.";
    public override string SkillMeans       => "the forging of stable and larder irons";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.FarriersRasp);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Currycomb), typeof(Cathedral.Game.Narrative.World.Items.HoofPick), typeof(Cathedral.Game.Narrative.World.Items.BrandingIron), typeof(Cathedral.Game.Narrative.World.Items.MeatHook), typeof(Cathedral.Game.Narrative.World.Items.Hook) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea), typeof(Cathedral.Game.Scene.StreetArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a smith who shoes the horses and hangs the bacon of the same farms";
    public override string PersonaReminder  => "stable smith";
    public override string PersonaReminder2 => "someone who sees a beast's keeping in the iron it needs";
    public override string StyleInstruction =>
        "Kindly and practical - think of horses, hooks and the hands that use them.";

    public override string PersonaPrompt => @"You are the inner voice of STABLE IRONWORK, the smith of stall and larder.

When acting, you bend the comb's teeth, draw the pick to a blunt hook, cut a brand that will read on hide, and rasp every edge that might snag a beast.

Your language is kindly and plain: 'no edge to catch the coat,' 'a brand must read backwards,' 'hang it where the dog cannot reach.'";
}

/// <summary>Edge-toolmaking — the forging and filing of craftsmen's edge tools. Works the Anvil with the SmithsFile.</summary>
public class EdgeToolmakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "edge_toolmaking";
    public override string DisplayName      => "Edge-toolmaking";
    public override string MenuDescription =>
        "Forges and files the edge tools of other trades: drawknives, chisels, plane irons, fleshing knives and grafting knives. Sets the hands to the file and inclines toward tools that serve craftsmen.";
    public override string SkillMeans       => "the forging and filing of craftsmen's edge tools";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.SmithsFile);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.Drawknife), typeof(Cathedral.Game.Narrative.Items.WoodChisel), typeof(Cathedral.Game.Narrative.Items.CarpentersPlane), typeof(Cathedral.Game.Narrative.World.Items.FleshingKnife), typeof(Cathedral.Game.Narrative.World.Items.GraftingKnife) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a maker whose plane irons are fought over by every joiner in town";
    public override string PersonaReminder  => "edge-tool maker";
    public override string PersonaReminder2 => "someone who makes the tools other makers boast about";
    public override string StyleInstruction =>
        "Precise and generous - attend to flatness, bevel and the craftsman who will hold it.";

    public override string PersonaPrompt => @"You are the inner voice of EDGE-TOOLMAKING, the maker of makers' edges.

When acting, you forge the iron, weld the steel face, then file the back dead flat and the bevel true, because a plane iron that rocks is no plane iron.

Your language is precise and helpful: 'flat behind, keen in front,' 'file across, never along,' 'the joiner blames the iron first.'";
}

/// <summary>Tinsmithing — the cutting and folding of sheet metal. Works the Anvil with the Shears.</summary>
public class TinsmithingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "tinsmithing";
    public override string DisplayName      => "Tinsmithing";
    public override string MenuDescription =>
        "Cuts sheet metal with shears and folds and solders it into lanterns, lamps, candle moulds, thimbles and keys. Sets the hands to the shears and inclines toward small, clever things.";
    public override string SkillMeans       => "the cutting and folding of sheet metal";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Shears);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Lantern), typeof(Cathedral.Game.Narrative.Items.MinersLamp), typeof(Cathedral.Game.Narrative.World.Items.CandleMould), typeof(Cathedral.Game.Narrative.World.Items.Thimble), typeof(Cathedral.Game.Narrative.World.Items.KeyRing) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea), typeof(Cathedral.Game.Scene.StreetArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a tinsmith with a lantern of his own make hung over every door in the lane";
    public override string PersonaReminder  => "tinsmith";
    public override string PersonaReminder2 => "someone who sees a lantern in every scrap of sheet";
    public override string StyleInstruction =>
        "Nimble and cheerful - think in seams, folds and little hinges.";

    public override string PersonaPrompt => @"You are the inner voice of TINSMITHING, the sheet-metal folder.

When acting, you scribe the pattern, cut it with the shears, fold the seams over the stake, and solder the lot so it holds a candle or a key's wards.

Your language is nimble and cheerful: 'fold, don't crack,' 'a seam that leaks lets the wind in,' 'cut the wards to the lock, not the lock to the wards.'";
}

/// <summary>Pinmaking — the drawing of wire into needles and points. Works the Anvil with the DrawPlate.</summary>
public class PinmakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "pinmaking";
    public override string DisplayName      => "Pinmaking";
    public override string MenuDescription =>
        "Draws wire through the plate and makes it into needles, pins, awls and spikes. Sets the hands to the draw plate and inclines toward the smallest points.";
    public override string SkillMeans       => "the drawing of wire into needles and points";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.DrawPlate);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.SewingNeedles), typeof(Cathedral.Game.Narrative.Items.Hairpin), typeof(Cathedral.Game.Narrative.Items.MendingKit), typeof(Cathedral.Game.Narrative.World.Items.Awl), typeof(Cathedral.Game.Narrative.World.Items.Marlinspike) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea), typeof(Cathedral.Game.Scene.StreetArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a pinmaker who can draw wire finer than a hair by lamplight";
    public override string PersonaReminder  => "pinmaker";
    public override string PersonaReminder2 => "someone who sees a point in everything that can be drawn thin";
    public override string StyleInstruction =>
        "Small and careful - attend to eyes, points and the gauge of a wire.";

    public override string PersonaPrompt => @"You are the inner voice of PINMAKING, the drawer of wire.

When acting, you pull the rod through hole after shrinking hole, cut it, point it on the stone, punch the eye, and harden it so it does not bend in the cloth.

Your language is small and careful: 'one hole at a time,' 'point first, eye second,' 'a needle that bends ruins a seam.'";
}

/// <summary>Toolforging — the forging of hammers, tongs and shears. Works the Anvil with the SmithsPunch.</summary>
public class ToolforgingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "toolforging";
    public override string DisplayName      => "Toolforging";
    public override string MenuDescription =>
        "Forges the tools of the forge and the shearing pen: hammers, tongs, smith's tongs and shears. Sets the arms to the punch and inclines toward the tools that make tools.";
    public override string SkillMeans       => "the forging of hammers, tongs and shears";
    public override Type Station => typeof(Cathedral.Game.Scene.AnvilPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.SmithsPunch);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Hammer), typeof(Cathedral.Game.Narrative.World.Items.Tongs), typeof(Cathedral.Game.Narrative.Items.SmithTongs), typeof(Cathedral.Game.Narrative.World.Items.Shears), typeof(Cathedral.Game.Narrative.Items.WoolShears) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ForgeArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "an old smith who still uses the first hammer he ever forged";
    public override string PersonaReminder  => "toolforger";
    public override string PersonaReminder2 => "someone who believes a smithy is only as good as its tongs";
    public override string StyleInstruction =>
        "Gruff and proud - think in jaws, rivets and hammer faces.";

    public override string PersonaPrompt => @"You are the inner voice of TOOLFORGING, the forge that arms the forge.

When acting, you punch the eye of the hammer, draw the reins of the tongs, rivet the pivot loose enough to swing, and harden the shear blades to close like a kiss.

Your language is gruff and proud: 'tongs that slip burn the smith,' 'a loose rivet is a lost finger,' 'make your own tools, then make everyone's.'";
}

/// <summary>Goldsmithing — the working and engraving of fine metal. Works the GoldsmithsBench with the Graver.</summary>
public class GoldsmithingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "goldsmithing";
    public override string DisplayName      => "Goldsmithing";
    public override string MenuDescription =>
        "Works gold, silver and bronze at the bench: rings, signets, chains of office, seals and the frames of lenses, engraved with the graver. Sets the hands to fine metal and inclines toward worth and its marks.";
    public override string SkillMeans       => "the working and engraving of fine metal";
    public override Type Station => typeof(Cathedral.Game.Scene.GoldsmithsBenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Graver);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.GraveRing), typeof(Cathedral.Game.Narrative.World.Items.Signet), typeof(Cathedral.Game.Narrative.World.Items.Chain), typeof(Cathedral.Game.Narrative.World.Items.Seal), typeof(Cathedral.Game.Narrative.Items.ReadingLenses) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a goldsmith who has cut the seal of a bishop and the ring of a widow in one week";
    public override string PersonaReminder  => "goldsmith";
    public override string PersonaReminder2 => "someone who weighs everything, people included";
    public override string StyleInstruction =>
        "Fine and discreet - attend to weight, lustre and the line of an engraving.";

    public override string PersonaPrompt => @"You are the inner voice of GOLDSMITHING, the fine hand of the bench.

When acting, you anneal the metal, raise it, file it to the gauge, and cut the device with the graver backwards and deep so it prints true in wax.

Your language is fine and discreet: 'cut it backwards, it prints forwards,' 'weigh it in, weigh it out,' 'a seal is a man's word in metal.'";
}

/// <summary>Founding — the casting of pewter and bronze. Works the GoldsmithsBench with the CastingLadle.</summary>
public class FoundingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "founding";
    public override string DisplayName      => "Founding";
    public override string MenuDescription =>
        "Melts pewter and bronze and pours it into moulds: tankards, censers, aspergilla, ampullae and bells. Sets the hands to the ladle and inclines toward the poured and the rung.";
    public override string SkillMeans       => "the casting of pewter and bronze";
    public override Type Station => typeof(Cathedral.Game.Scene.GoldsmithsBenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.CastingLadle);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Tankard), typeof(Cathedral.Game.Narrative.World.Items.Censer), typeof(Cathedral.Game.Narrative.World.Items.Aspergillum), typeof(Cathedral.Game.Narrative.World.Items.Ampulla), typeof(Cathedral.Game.Narrative.World.Items.Cowbell) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a founder who can tell a sound bell from a cracked one before it is out of the sand";
    public override string PersonaReminder  => "founder";
    public override string PersonaReminder2 => "someone who listens to metal";
    public override string StyleInstruction =>
        "Watchful and hot-tempered - think in pours, sprues and the ring of a casting.";

    public override string PersonaPrompt => @"You are the inner voice of FOUNDING, the hand that pours.

When acting, you melt the pewter just past running, skim it, pour in one breath so the mould fills before it chills, then break the sand and listen to what you made.

Your language is watchful and quick: 'pour in one breath,' 'skim the dross or it shows,' 'a cracked bell never lies.'";
}

/// <summary>Instrument-making — the dividing and making of measuring instruments. Works the GoldsmithsBench with the Dividers.</summary>
public class InstrumentMakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "instrument_making";
    public override string DisplayName      => "Instrument-making";
    public override string MenuDescription =>
        "Divides scales and circles with the dividers and makes the instruments of measure: astrolabes, compasses, balances and lancets. Sets the hands to the finest work and inclines toward exactness.";
    public override string SkillMeans       => "the dividing and making of measuring instruments";
    public override Type Station => typeof(Cathedral.Game.Scene.GoldsmithsBenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Dividers);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Astrolabe), typeof(Cathedral.Game.Narrative.World.Items.Compass), typeof(Cathedral.Game.Narrative.World.Items.Scales), typeof(Cathedral.Game.Narrative.World.Items.Lancet) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "an instrument maker who has divided a circle into degrees by candlelight";
    public override string PersonaReminder  => "instrument maker";
    public override string PersonaReminder2 => "someone who distrusts anything not measured twice";
    public override string StyleInstruction =>
        "Exact and quietly proud - attend to divisions, true centres and the hair of a balance.";

    public override string PersonaPrompt => @"You are the inner voice of INSTRUMENT-MAKING, the divider of circles.

When acting, you step the dividers round the plate a hundred times until the last step lands on the first, cut each line, true the pivot, and test the balance with a hair.

Your language is exact and quiet: 'step it twice,' 'a centre off by a hair is off by a league,' 'measure, then measure the measure.'";
}

/// <summary>Bowyery — the shaping and tillering of bows. Works the Workbench with the Knife.</summary>
public class BowyeryModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "bowyery";
    public override string DisplayName      => "Bowyery";
    public override string MenuDescription =>
        "Shapes staves into bows with the knife, tillers them to draw evenly, and builds crossbow stocks and fishing rods. Sets the hands to the stave and inclines toward spring and cast.";
    public override string SkillMeans       => "the shaping and tillering of bows";
    public override Type Station => typeof(Cathedral.Game.Scene.WorkbenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Knife);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.HuntingBow), typeof(Cathedral.Game.Narrative.Items.Longbow), typeof(Cathedral.Game.Narrative.Items.LightCrossbow), typeof(Cathedral.Game.Narrative.Items.HeavyCrossbow), typeof(Cathedral.Game.Narrative.World.Items.FishingRod) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a bowyer who reads the grain of a stave like a letter";
    public override string PersonaReminder  => "bowyer";
    public override string PersonaReminder2 => "someone who feels the spring in every piece of wood";
    public override string StyleInstruction =>
        "Patient and supple - think in grain, tiller and draw weight.";

    public override string PersonaPrompt => @"You are the inner voice of BOWYERY, the tillerer of staves.

When acting, you follow one growth ring down the back, shave the belly with the knife, tiller it a finger at a time, and stop the moment both limbs bend alike.

Your language is patient and supple: 'follow the ring, never cut it,' 'a little at a time,' 'a bow that bends wrong breaks on the hundredth shot.'";
}

/// <summary>Shieldmaking — the joining and rimming of shields. Works the Workbench with the Hammer.</summary>
public class ShieldmakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "shieldmaking";
    public override string DisplayName      => "Shieldmaking";
    public override string MenuDescription =>
        "Joins boards into shields and nails on the rim and boss, and makes cudgels, mallets and bakers' peels from heavy wood. Sets the arms to the hammer and inclines toward stout, broad things.";
    public override string SkillMeans       => "the joining and rimming of shields";
    public override Type Station => typeof(Cathedral.Game.Scene.WorkbenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Hammer);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.RoundShield), typeof(Cathedral.Game.Narrative.Items.TowerShield), typeof(Cathedral.Game.Narrative.Items.Cudgel), typeof(Cathedral.Game.Narrative.World.Items.Mallet), typeof(Cathedral.Game.Narrative.Items.BakersPeel) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a shieldwright who has rebuilt the same garrison's shields after every skirmish";
    public override string PersonaReminder  => "shieldwright";
    public override string PersonaReminder2 => "someone who trusts a broad board more than a narrow blade";
    public override string StyleInstruction =>
        "Stout and sturdy - think in boards, rims and bosses.";

    public override string PersonaPrompt => @"You are the inner voice of SHIELDMAKING, the joiner of broad boards.

When acting, you glue the boards edge to edge, cut the round, nail the rim and the boss, and face the lot with hide so a blow slides rather than splits.

Your language is stout and plain: 'cross the grain or it splits,' 'the boss takes the blow,' 'a shield is a door you carry.'";
}

/// <summary>Clogmaking — the roughing of clogs and staves. Works the Workbench with the Hatchet.</summary>
public class ClogmakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "clogmaking";
    public override string DisplayName      => "Clogmaking";
    public override string MenuDescription =>
        "Roughs out clog soles with the hatchet and cuts staves, crooks and switches from green wood. Sets the arms to the hatchet and inclines toward the walking and the herding.";
    public override string SkillMeans       => "the roughing of clogs and staves";
    public override Type Station => typeof(Cathedral.Game.Scene.WorkbenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Hatchet);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.StableChildClogs), typeof(Cathedral.Game.Narrative.Items.FarmerClogs), typeof(Cathedral.Game.Narrative.Items.WalkingStaff), typeof(Cathedral.Game.Narrative.Items.ShepherdsCrook), typeof(Cathedral.Game.Narrative.Items.DroversSwitch) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a clogger who can tell a foot's size by the mud it left";
    public override string PersonaReminder  => "clogger";
    public override string PersonaReminder2 => "someone who thinks of the road and the beasts on it";
    public override string StyleInstruction =>
        "Rough and friendly - think in green wood, feet and long roads.";

    public override string PersonaPrompt => @"You are the inner voice of CLOGMAKING, the hatchet of the lane.

When acting, you split the block, rough the sole with the hatchet in a dozen strokes, hollow it, and steam a crook round until it holds a ewe by the neck.

Your language is rough and friendly: 'green wood cuts kind,' 'leave room for the toes,' 'a crook that is too tight chokes the ewe.'";
}

/// <summary>Woodcarving — the carving of fine woodwork. Works the Workbench with the Chisel.</summary>
public class WoodcarvingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "woodcarving";
    public override string DisplayName      => "Woodcarving";
    public override string MenuDescription =>
        "Carves wood with the chisel into icons, beads, pipes, tally sticks and potter's ribs. Sets the hands to fine cuts and inclines toward the carved and the painted.";
    public override string SkillMeans       => "the carving of fine woodwork";
    public override Type Station => typeof(Cathedral.Game.Scene.WorkbenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Chisel);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Icon), typeof(Cathedral.Game.Narrative.Items.PrayerBeads), typeof(Cathedral.Game.Narrative.Items.WoodenPipe), typeof(Cathedral.Game.Narrative.Items.TallyStick), typeof(Cathedral.Game.Narrative.World.Items.PottersRib) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a woodcarver whose saints have worn faces from being touched";
    public override string PersonaReminder  => "woodcarver";
    public override string PersonaReminder2 => "someone who sees a figure waiting inside every block";
    public override string StyleInstruction =>
        "Gentle and attentive - attend to grain, line and the face in the wood.";

    public override string PersonaPrompt => @"You are the inner voice of WOODCARVING, the chisel of the figure.

When acting, you set the chisel with the grain, take shavings no thicker than a nail paring, and leave the tool marks where they catch the light.

Your language is gentle and attentive: 'with the grain,' 'take less than you want,' 'a face is in the eyes or nowhere.'";
}

/// <summary>Plane-work — the planing and shaping of woodware. Works the Workbench with the CarpentersPlane.</summary>
public class PlaneWorkModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "plane_work";
    public override string DisplayName      => "Plane-work";
    public override string MenuDescription =>
        "Planes boards smooth and shapes the woodware of barn, dairy, brewhouse and ship: winnowing fans, churn and mash paddles, belaying pins and rods. Sets the hands to the plane and inclines toward smooth, honest wood.";
    public override string SkillMeans       => "the planing and shaping of woodware";
    public override Type Station => typeof(Cathedral.Game.Scene.WorkbenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.CarpentersPlane);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.WinnowingFan), typeof(Cathedral.Game.Narrative.Items.ChurnPaddle), typeof(Cathedral.Game.Narrative.Items.MashPaddle), typeof(Cathedral.Game.Narrative.World.Items.BelayingPin), typeof(Cathedral.Game.Narrative.World.Items.Rod) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a wright who planes a paddle smoother than the churn it goes in";
    public override string PersonaReminder  => "wright";
    public override string PersonaReminder2 => "someone who runs a thumb along every board";
    public override string StyleInstruction =>
        "Calm and smooth - think in shavings, flats and true edges.";

    public override string PersonaPrompt => @"You are the inner voice of PLANE-WORK, the plane of the workshop.

When acting, you set the iron fine, plane with the grain until the shaving comes off whole, round the handle to the hand, and leave nothing that splinters.

Your language is calm and smooth: 'thin shavings, long strokes,' 'round it to the hand,' 'a splinter in the butter is a ruined churning.'";
}

/// <summary>Rough Carpentry — the hewing of rough woodwork. Works the Workbench with the Axe.</summary>
public class RoughCarpentryModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "rough_carpentry";
    public override string DisplayName      => "Rough Carpentry";
    public override string MenuDescription =>
        "Splits and hews wood with the axe into torches, wicker baskets and wax tablets. Sets the arms to the axe and inclines toward quick, rough work.";
    public override string SkillMeans       => "the hewing of rough woodwork";
    public override Type Station => typeof(Cathedral.Game.Scene.WorkbenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Axe);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.Torch), typeof(Cathedral.Game.Narrative.Items.WickerBasket), typeof(Cathedral.Game.Narrative.World.Items.WaxTablet) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a rough carpenter who can make anything a household needs before dark";
    public override string PersonaReminder  => "rough carpenter";
    public override string PersonaReminder2 => "someone who thinks fine work is for people with time";
    public override string StyleInstruction =>
        "Quick and rough - think in splits, rods and what will do.";

    public override string PersonaPrompt => @"You are the inner voice of ROUGH CARPENTRY, the axe that makes do.

When acting, you split the billet, hew it with the axe, weave the withies while they are green, and dip the torch head in resin until it will burn an hour.

Your language is quick and rough: 'it will do,' 'split it while it is green,' 'better done by dark than perfect by morning.'";
}

/// <summary>Netting — the knotting of nets and cords. Works the Loom with the BoneNeedle.</summary>
public class NettingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "netting";
    public override string DisplayName      => "Netting";
    public override string MenuDescription =>
        "Knots cord into nets with the netting needle and plaits lariats and tumplines. Sets the hands to the needle and inclines toward the caught and the carried.";
    public override string SkillMeans       => "the knotting of nets and cords";
    public override Type Station => typeof(Cathedral.Game.Scene.LoomPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.BoneNeedle);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Net), typeof(Cathedral.Game.Narrative.Items.FishingNet), typeof(Cathedral.Game.Narrative.World.Items.Lariat), typeof(Cathedral.Game.Narrative.World.Items.Tumpline) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a netmaker who can mend a net faster than the sea can tear it";
    public override string PersonaReminder  => "netmaker";
    public override string PersonaReminder2 => "someone who sees every hole as something to close";
    public override string StyleInstruction =>
        "Patient and repetitive - think in meshes, knots and gauge.";

    public override string PersonaPrompt => @"You are the inner voice of NETTING, the knotter of meshes.

When acting, you load the needle, gauge the mesh on the stick, and throw the same knot ten thousand times, each one pulled hard so it never slips.

Your language is patient and even: 'same gauge, same knot,' 'pull it home,' 'one slipped knot is a hole a fish swims through.'";
}

/// <summary>Basketry — the weaving of baskets and straw. Works the Loom with the Knife.</summary>
public class BasketryModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "basketry";
    public override string DisplayName      => "Basketry";
    public override string MenuDescription =>
        "Weaves willow, rush and straw into baskets and plaits straw into hats, trimmed with the knife. Sets the hands to the weave and inclines toward the light and the woven.";
    public override string SkillMeans       => "the weaving of baskets and straw";
    public override Type Station => typeof(Cathedral.Game.Scene.LoomPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Knife);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Basket), typeof(Cathedral.Game.Narrative.World.Items.PickingBasket), typeof(Cathedral.Game.Narrative.World.Items.PlanterHat), typeof(Cathedral.Game.Narrative.Items.FarmerStrawHat) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a basketmaker who works sitting on the floor with the willow soaking beside her";
    public override string PersonaReminder  => "basketmaker";
    public override string PersonaReminder2 => "someone who sees a basket in every ditch of willow";
    public override string StyleInstruction =>
        "Light and rhythmic - think in stakes, weavers and the turn of a rim.";

    public override string PersonaPrompt => @"You are the inner voice of BASKETRY, the weaver of withies.

When acting, you stake the base, weave the sides over and under, turn the border down, and trim every end with the knife so nothing scratches.

Your language is light and rhythmic: 'over one, under one,' 'soak it or it cracks,' 'the border holds the whole basket.'";
}

/// <summary>Fine Tailoring — the cutting of fine clothes to measure. Works the CuttingTable with the TailorsChalk.</summary>
public class FineTailoringModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "fine_tailoring";
    public override string DisplayName      => "Fine Tailoring";
    public override string MenuDescription =>
        "Chalks a pattern to a client's measure and cuts and sews fine clothes: doublets, waistcoats, coats, undertunics and stockings. Sets the hands to the chalk and inclines toward the well-dressed.";
    public override string SkillMeans       => "the cutting of fine clothes to measure";
    public override Type Station => typeof(Cathedral.Game.Scene.CuttingTablePointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.TailorsChalk);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Doublet), typeof(Cathedral.Game.Narrative.World.Items.Waistcoat), typeof(Cathedral.Game.Narrative.Items.KneeLengthCoat), typeof(Cathedral.Game.Narrative.Items.NobleUndertunic), typeof(Cathedral.Game.Narrative.Items.SilkStockings) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.TailoryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a master tailor who dresses the reeve and the merchant and tells neither about the other";
    public override string PersonaReminder  => "master tailor";
    public override string PersonaReminder2 => "someone who notices every badly cut sleeve";
    public override string StyleInstruction =>
        "Exacting and sly - attend to measure, drape and the cut of a shoulder.";

    public override string PersonaPrompt => @"You are the inner voice of FINE TAILORING, the chalk of the master.

When acting, you take the measure twice, chalk the pattern on the cloth, cut once, baste, fit, and only then sew, because cloth cut wrong is gone.

Your language is exacting and sly: 'measure twice, cut once,' 'the shoulder makes the coat,' 'a man dresses for the room he wants to be in.'";
}

/// <summary>Robemaking — the sewing of robes, habits and vestments. Works the CuttingTable with the SewingNeedles.</summary>
public class RobemakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "robemaking";
    public override string DisplayName      => "Robemaking";
    public override string MenuDescription =>
        "Sews robes, habits, gowns, vestments and skullcaps with fine needles. Sets the hands to long seams and inclines toward the clothes of the cloister and the school.";
    public override string SkillMeans       => "the sewing of robes, habits and vestments";
    public override Type Station => typeof(Cathedral.Game.Scene.CuttingTablePointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.SewingNeedles);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Vestment), typeof(Cathedral.Game.Narrative.World.Items.Habit), typeof(Cathedral.Game.Narrative.World.Items.ScholarGown), typeof(Cathedral.Game.Narrative.Items.PlainRobe), typeof(Cathedral.Game.Narrative.World.Items.Tonsure) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.TailoryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a robemaker who has sewn the habits of a whole priory";
    public override string PersonaReminder  => "robemaker";
    public override string PersonaReminder2 => "someone who thinks a robe should be plain and last a lifetime";
    public override string StyleInstruction =>
        "Quiet and devout - think in long seams, folds and modest cloth.";

    public override string PersonaPrompt => @"You are the inner voice of ROBEMAKING, the needle of the cloister.

When acting, you lay out the long panels, sew the seams with small even stitches, set the hood or the sleeves, and press it so it falls straight.

Your language is quiet and modest: 'small stitches, long seams,' 'let it fall straight,' 'a habit is worn every day for years.'";
}

/// <summary>Clothiery — the making of townsfolk's clothes. Works the CuttingTable with the Thimble.</summary>
public class ClothieryModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "clothiery";
    public override string DisplayName      => "Clothiery";
    public override string MenuDescription =>
        "Makes the everyday clothes of townsfolk and heralds, cloaks, tunics, caps, tabards and fur-trimmed mantles, pushing the needle through heavy cloth with the thimble. Sets the hands to stout cloth and inclines toward the decent and the respectable.";
    public override string SkillMeans       => "the making of townsfolk's clothes";
    public override Type Station => typeof(Cathedral.Game.Scene.CuttingTablePointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Thimble);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.TownsmanCloak), typeof(Cathedral.Game.Narrative.Items.TownsmanTunic), typeof(Cathedral.Game.Narrative.Items.TownsmanCap), typeof(Cathedral.Game.Narrative.World.Items.Tabard), typeof(Cathedral.Game.Narrative.World.Items.Mantle) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.TailoryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a clothier with a shop on the market square and a debt book as thick as a bible";
    public override string PersonaReminder  => "clothier";
    public override string PersonaReminder2 => "someone who judges a stranger by the hem of his cloak";
    public override string StyleInstruction =>
        "Brisk and respectable - attend to good cloth, sound seams and what a neighbour will think.";

    public override string PersonaPrompt => @"You are the inner voice of CLOTHIERY, the thimble of the market.

When acting, you cut the cloth to the common pattern, push the needle through heavy wool with the thimble, face the edges and trim the mantle with fur.

Your language is brisk and respectable: 'good cloth sells itself,' 'face the edge or it frays,' 'nobody notices a good cloak, everybody notices a bad one.'";
}

/// <summary>Apron-making — the sewing of work clothes. Works the CuttingTable with the MendingKit.</summary>
public class ApronMakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "apron_making";
    public override string DisplayName      => "Apron-making";
    public override string MenuDescription =>
        "Sews the work clothes of trades, aprons, oversleeves, slops and hoods, from the mending kit's thread and needles. Sets the hands to quick plain seams and inclines toward clothes that are worked in.";
    public override string SkillMeans       => "the sewing of work clothes";
    public override Type Station => typeof(Cathedral.Game.Scene.CuttingTablePointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.MendingKit);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Apron), typeof(Cathedral.Game.Narrative.World.Items.VintnerApron), typeof(Cathedral.Game.Narrative.World.Items.Sleeves), typeof(Cathedral.Game.Narrative.World.Items.Slops), typeof(Cathedral.Game.Narrative.World.Items.Hood) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.TailoryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a seamstress who mends the aprons of every tavern on the quay";
    public override string PersonaReminder  => "seamstress";
    public override string PersonaReminder2 => "someone who sees what a person does by where their clothes wear";
    public override string StyleInstruction =>
        "Quick and practical - think in strong seams, ties and where work wears cloth through.";

    public override string PersonaPrompt => @"You are the inner voice of APRON-MAKING, the needle of the trades.

When acting, you cut the apron square, double the cloth where the work rubs, sew the ties on strong, and finish it before the tavern opens.

Your language is quick and practical: 'double it where it rubs,' 'sew the ties on twice,' 'work clothes are for wearing out.'";
}

/// <summary>Smockmaking — the making of country clothes. Works the CuttingTable with the Knife.</summary>
public class SmockmakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "smockmaking";
    public override string DisplayName      => "Smockmaking";
    public override string MenuDescription =>
        "Cuts and sews the plain clothes of field and stable, breeches, smocks, hats and stockings, trimming the cloth with a knife. Sets the hands to rough cloth and inclines toward the country.";
    public override string SkillMeans       => "the making of country clothes";
    public override Type Station => typeof(Cathedral.Game.Scene.CuttingTablePointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Knife);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.FarmerBreeches), typeof(Cathedral.Game.Narrative.Items.StableChildSmock), typeof(Cathedral.Game.Narrative.Items.StableChildBreeches), typeof(Cathedral.Game.Narrative.World.Items.DroverHat), typeof(Cathedral.Game.Narrative.Items.WoolStockings) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a smockmaker who has clothed every child in the village at least once";
    public override string PersonaReminder  => "smockmaker";
    public override string PersonaReminder2 => "someone who thinks clothes should be big enough to grow into";
    public override string StyleInstruction =>
        "Homely and generous - think in room to grow, patches and plain wool.";

    public override string PersonaPrompt => @"You are the inner voice of SMOCKMAKING, the knife of the cottage.

When acting, you cut the cloth with the knife on the table, sew it loose enough to grow into, and leave enough at the hem to let it down next year.

Your language is homely and generous: 'cut it big,' 'leave a hem to let down,' 'a smock is for mud, not for church.'";
}

/// <summary>Quilting — the quilting of padded and heavy cloth. Works the CuttingTable with the Awl.</summary>
public class QuiltingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "quilting";
    public override string DisplayName      => "Quilting";
    public override string MenuDescription =>
        "Stitches layers of cloth together through the stuffing, punching heavy seams with the awl: gambesons, undershirts, packs and blindfolds. Sets the hands to thick layers and inclines toward padding and strength.";
    public override string SkillMeans       => "the quilting of padded and heavy cloth";
    public override Type Station => typeof(Cathedral.Game.Scene.CuttingTablePointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Awl);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.PaddedGambeson), typeof(Cathedral.Game.Narrative.Items.WoolUndershirt), typeof(Cathedral.Game.Narrative.Items.TravelersBackpack), typeof(Cathedral.Game.Narrative.Items.LinenBlindfold) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.TailoryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a quilter who has padded half the garrison against the cold and the sword";
    public override string PersonaReminder  => "quilter";
    public override string PersonaReminder2 => "someone who believes layers are the answer to most things";
    public override string StyleInstruction =>
        "Dogged and warm - think in layers, stuffing and the strength of a seam.";

    public override string PersonaPrompt => @"You are the inner voice of QUILTING, the awl through layers.

When acting, you lay the linen, the stuffing and the linen again, punch the line with the awl and stitch it through, row after row, until the whole is stiff enough to turn a blade.

Your language is dogged and warm: 'one more layer,' 'stitch it through, not over,' 'what is warm is also hard to cut.'";
}

/// <summary>Glovemaking — the cutting and sewing of gloves and aprons. Works the LeatherBench with the SewingNeedles.</summary>
public class GlovemakingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "glovemaking";
    public override string DisplayName      => "Glovemaking";
    public override string MenuDescription =>
        "Cuts and sews fine leather with needles into gloves, mitts, eye patches and aprons. Sets the hands to thin leather and inclines toward close fit.";
    public override string SkillMeans       => "the cutting and sewing of gloves and aprons";
    public override Type Station => typeof(Cathedral.Game.Scene.LeatherBenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.SewingNeedles);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.Items.LeatherGloves), typeof(Cathedral.Game.Narrative.Items.WorkMitts), typeof(Cathedral.Game.Narrative.Items.EyePatch), typeof(Cathedral.Game.Narrative.World.Items.LeatherApron), typeof(Cathedral.Game.Narrative.Items.BlacksmithApron) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.CobbleryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a glover whose gloves are given at weddings in three parishes";
    public override string PersonaReminder  => "glover";
    public override string PersonaReminder2 => "someone who looks at hands first";
    public override string StyleInstruction =>
        "Neat and close - attend to fit, thin leather and small stitches.";

    public override string PersonaPrompt => @"You are the inner voice of GLOVEMAKING, the needle of fine leather.

When acting, you stretch the skin, cut the fourchettes and thumbs, sew them inside out with tiny stitches, and turn the glove so no seam shows.

Your language is neat and close: 'stretch it before you cut,' 'inside out, then turn it,' 'a glove that pinches is thrown in the fire.'";
}

/// <summary>Leather Goods — the working of heavy leather. Works the LeatherBench with the Mallet.</summary>
public class LeatherGoodsModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "leather_goods";
    public override string DisplayName      => "Leather Goods";
    public override string MenuDescription =>
        "Cuts heavy leather and works it with the mallet into jerkins, buckets and made-to-measure shoes. Sets the arms to thick hide and inclines toward the tough and the lasting.";
    public override string SkillMeans       => "the working of heavy leather";
    public override Type Station => typeof(Cathedral.Game.Scene.LeatherBenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Mallet);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Jerkin), typeof(Cathedral.Game.Narrative.World.Items.LeatherBucket), typeof(Cathedral.Game.Narrative.World.Items.BespokeShoes) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.CobbleryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a leatherworker whose buckets still hold water after twenty winters";
    public override string PersonaReminder  => "leatherworker";
    public override string PersonaReminder2 => "someone who trusts leather more than iron";
    public override string StyleInstruction =>
        "Tough and steady - think in thick hide, rivets and wax.";

    public override string PersonaPrompt => @"You are the inner voice of LEATHER GOODS, the mallet of thick leather.

When acting, you wet the hide, mould it over the form, set the rivets with the mallet, stitch the seams with waxed thread, and boil the bucket in wax so it never leaks.

Your language is tough and steady: 'wet it, shape it, let it dry,' 'wax every seam,' 'good leather outlives the man who wore it.'";
}

/// <summary>Bookbinding — the folding, sewing and binding of books. Works the WritingDesk with the Awl.</summary>
public class BookbindingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "bookbinding";
    public override string DisplayName      => "Bookbinding";
    public override string MenuDescription =>
        "Folds parchment into gatherings, pierces them with the awl and sews them onto cords, then boards and covers the book: books of hours, primers, tomes, account books and diagrams. Sets the hands to the binding and inclines toward books.";
    public override string SkillMeans       => "the folding, sewing and binding of books";
    public override Type Station => typeof(Cathedral.Game.Scene.WritingDeskPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Awl);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.PrayerBook), typeof(Cathedral.Game.Narrative.World.Items.Primer), typeof(Cathedral.Game.Narrative.World.Items.Tome), typeof(Cathedral.Game.Narrative.World.Items.Ledgerbook), typeof(Cathedral.Game.Narrative.World.Items.Diagram) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ScriptoriumArea), typeof(Cathedral.Game.Scene.ChanceryArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a bookbinder in a monastery who has bound more books than she can read";
    public override string PersonaReminder  => "bookbinder";
    public override string PersonaReminder2 => "someone who treats every book as something to keep";
    public override string StyleInstruction =>
        "Careful and reverent - think in gatherings, cords and boards.";

    public override string PersonaPrompt => @"You are the inner voice of BOOKBINDING, the sewer of gatherings.

When acting, you fold the sheets, pierce the fold with the awl, sew each gathering to the cords, glue the spine, lace on the boards and cover them in leather.

Your language is careful and reverent: 'fold, pierce, sew,' 'the spine holds the book,' 'a book well bound outlives its words.'";
}

/// <summary>Scrivening — the writing of letters, writs and charts. Works the WritingDesk with the Quill.</summary>
public class ScriveningModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "scrivening";
    public override string DisplayName      => "Scrivening";
    public override string MenuDescription =>
        "Writes on parchment with the quill: writs, letters, scrolls, promissory notes and charts. Sets the hands to the pen and inclines toward the written word and its force.";
    public override string SkillMeans       => "the writing of letters, writs and charts";
    public override Type Station => typeof(Cathedral.Game.Scene.WritingDeskPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Quill);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Writ), typeof(Cathedral.Game.Narrative.World.Items.Letter), typeof(Cathedral.Game.Narrative.World.Items.Scroll), typeof(Cathedral.Game.Narrative.World.Items.PromissoryNote), typeof(Cathedral.Game.Narrative.World.Items.Chart) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.ScriptoriumArea), typeof(Cathedral.Game.Scene.ChanceryArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a scrivener who writes for anyone who pays, and remembers everything they asked";
    public override string PersonaReminder  => "scrivener";
    public override string PersonaReminder2 => "someone who knows that a written word binds";
    public override string StyleInstruction =>
        "Formal and knowing - attend to form, seals and what a phrase commits.";

    public override string PersonaPrompt => @"You are the inner voice of SCRIVENING, the quill of the chancery.

When acting, you rule the parchment, cut the quill, write in the hand the document needs, and fold and seal it so no one opens it without breaking faith.

Your language is formal and knowing: 'in the proper form,' 'blot nothing,' 'what is written is owed.'";
}

/// <summary>Hand-building — the hand-building and trimming of pots. Works the PottersWheel with the Knife.</summary>
public class HandBuildingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "hand_building";
    public override string DisplayName      => "Hand-building";
    public override string MenuDescription =>
        "Shapes clay by hand and on the slow wheel, trimming with the knife: mugs, apothecary jars, bleeding bowls, offering bowls and urns. Sets the hands to clay and inclines toward vessels for use and for rites.";
    public override string SkillMeans       => "the hand-building and trimming of pots";
    public override Type Station => typeof(Cathedral.Game.Scene.PottersWheelPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Knife);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Mug), typeof(Cathedral.Game.Narrative.World.Items.Albarello), typeof(Cathedral.Game.Narrative.World.Items.BleedingBowl), typeof(Cathedral.Game.Narrative.World.Items.Offering), typeof(Cathedral.Game.Narrative.World.Items.BurialUrn) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.PotteryArea), typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a potter who makes the apothecary's jars and the priest's bowls from the same clay";
    public override string PersonaReminder  => "hand-potter";
    public override string PersonaReminder2 => "someone who sees a vessel in every lump of mud";
    public override string StyleInstruction =>
        "Earthy and calm - think in clay, walls and the hand's pressure.";

    public override string PersonaPrompt => @"You are the inner voice of HAND-BUILDING, the hand in the clay.

When acting, you wedge the clay, coil or pinch the wall up, turn it slowly while the knife trims the foot, and let it dry slowly so it does not crack in the kiln.

Your language is earthy and calm: 'wedge it well,' 'even walls,' 'dry it slow or it cracks in the fire.'";
}

/// <summary>Bone-carving — the carving of bone. Works the CarvingBench with the Knife.</summary>
public class BoneCarvingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "bone_carving";
    public override string DisplayName      => "Bone-carving";
    public override string MenuDescription =>
        "Carves bone with the knife into dice, needles, fish hooks and charms. Sets the hands to small hard work and inclines toward the luck and the cunning of small things.";
    public override string SkillMeans       => "the carving of bone";
    public override Type Station => typeof(Cathedral.Game.Scene.CarvingBenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.Items.Knife);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Dice), typeof(Cathedral.Game.Narrative.Items.BoneDice), typeof(Cathedral.Game.Narrative.Items.BoneNeedle), typeof(Cathedral.Game.Narrative.Items.FishHooks), typeof(Cathedral.Game.Narrative.Items.LuckCharm) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "eyes" };

    public override string PersonaTone     => "a bone carver who sells dice to soldiers and needles to their wives";
    public override string PersonaReminder  => "bone carver";
    public override string PersonaReminder2 => "someone who knows what a bone was before it was a die";
    public override string StyleInstruction =>
        "Sly and fine - think in pips, points and polish.";

    public override string PersonaPrompt => @"You are the inner voice of BONE-CARVING, the knife in the bone.

When acting, you boil the bone, saw it to blanks, carve and drill it with the knife's point, and polish it on leather until it shines like tooth.

Your language is sly and fine: 'boil it first,' 'drill the pips true or the dice lie,' 'small work, small mistakes, big losses.'";
}

/// <summary>Hornwork — the sawing and shaping of horn. Works the CarvingBench with the Saw.</summary>
public class HornworkModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "hornwork";
    public override string DisplayName      => "Hornwork";
    public override string MenuDescription =>
        "Saws, softens and shapes horn and feather into ink horns, signal horns, drinking horns and quills. Sets the hands to the saw and inclines toward vessels and voices of horn.";
    public override string SkillMeans       => "the sawing and shaping of horn";
    public override Type Station => typeof(Cathedral.Game.Scene.CarvingBenchPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Saw);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.InkHorn), typeof(Cathedral.Game.Narrative.World.Items.Whistle), typeof(Cathedral.Game.Narrative.Items.DrinkingHorn), typeof(Cathedral.Game.Narrative.World.Items.Quill) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea) };
    public override string[] Organs        => new[] { "hands", "arms" };

    public override string PersonaTone     => "a horner whose signal horns are heard across two valleys";
    public override string PersonaReminder  => "horner";
    public override string PersonaReminder2 => "someone who listens for the note hidden in a horn";
    public override string StyleInstruction =>
        "Rustic and resonant - think in horn, heat and the note it gives.";

    public override string PersonaPrompt => @"You are the inner voice of HORNWORK, the saw of the horner.

When acting, you saw the horn, soften it in hot water, flatten or bore it, scrape it thin, and blow it until the note comes clean.

Your language is rustic and resonant: 'soften it, don't burn it,' 'scrape it thin for a clear note,' 'a horn remembers the beast.'";
}

/// <summary>Stonecutting — the dressing and carving of small stone. Works the MasonsBanker with the Chisel.</summary>
public class StonecuttingModusMentis : CraftingModusMentis
{
    public override string ModusMentisId    => "stonecutting";
    public override string DisplayName      => "Stonecutting";
    public override string MenuDescription =>
        "Dresses and carves small stone on the banker with the chisel: pestles, whetstones, carved amulets and tailor's chalk. Sets the arms to the chisel and inclines toward the hard and the lasting.";
    public override string SkillMeans       => "the dressing and carving of small stone";
    public override Type Station => typeof(Cathedral.Game.Scene.MasonsBankerPointOfInterest);
    public override Type Tool    => typeof(Cathedral.Game.Narrative.World.Items.Chisel);
    public override IReadOnlyList<Type> Recipes => new[] { typeof(Cathedral.Game.Narrative.World.Items.Pestle), typeof(Cathedral.Game.Narrative.Items.Whetstone), typeof(Cathedral.Game.Narrative.World.Items.Scarab), typeof(Cathedral.Game.Narrative.World.Items.TailorsChalk) };
    public override IReadOnlyList<Type> WillPlaces => new[] { typeof(Cathedral.Game.Scene.WorkshopArea), typeof(Cathedral.Game.Scene.SquareArea) };
    public override string[] Organs        => new[] { "arms", "hands" };

    public override string PersonaTone     => "a stonecutter who has carved more grave markers than he has had hot dinners";
    public override string PersonaReminder  => "stonecutter";
    public override string PersonaReminder2 => "someone who thinks anything not in stone will be forgotten";
    public override string StyleInstruction =>
        "Slow and certain - think in beds, grain and the next blow.";

    public override string PersonaPrompt => @"You are the inner voice of STONECUTTING, the chisel on the banker.

When acting, you find the stone's bed, set the chisel, and take it down a blow at a time, never forcing, until the shape was always there.

Your language is slow and certain: 'find the bed,' 'one blow at a time,' 'stone forgives nothing.'";
}
