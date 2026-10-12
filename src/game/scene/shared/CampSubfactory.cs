using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// Builders for sparse wilderness camp PoIs added to a host area when a
/// woodcutter, miner, or fisherman is in residence. Returns a list of PoIs
/// that the caller should attach to the appropriate area.
/// </summary>
public static class CampSubfactory
{
    /// <summary>Forest woodcutter / charcoal-burner camp: bedroll, fire pit, sack.</summary>
    public static List<PointOfInterest> BuildForestCamp()
    {
        return new List<PointOfInterest>
        {
            new BedrollPointOfInterest(
            displayName: "Bedroll",
                descriptions: new() { "A rolled bedroll of coarse cloth, half-unrolled by a tree-root" },
                items: new()
                {
                    new ItemElement(new Cloth()),
                },
                moods: new[] { "rolled", "dirty", "low" }
            ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "bushcraft", ["smell"] = "petrichor" } },
            new FirePointOfInterest(
            displayName: "Fire Pit",
                descriptions: new() { "A blackened ring of stones with the cooled remains of a small fire" },
                items: new()
                {
                    new ItemElement(new Coal()),
                    new ItemElement(new Twig()),
                },
                moods: new[] { "blackened", "circular", "cold" }
            ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "firecraft", ["listen"] = "forge_ear", ["smell"] = "smoke_reading", ["contemplate"] = "journeyman_eye" } },
            new SackPointOfInterest(
            displayName: "Sack",
                descriptions: new() { "A heavy sack leaning against a stump, holding the day's gathered wood" },
                items: new()
                {
                    new ItemElement(new Log()),
                    new ItemElement(new Bark()),
                },
                moods: new[] { "leaning", "heavy", "rough" }
            ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "bushcraft" } },
        };
    }

    /// <summary>Cave miner camp: bedroll, lantern hook, ore pile.</summary>
    public static List<PointOfInterest> BuildMineCamp()
    {
        return new List<PointOfInterest>
        {
            new BedrollPointOfInterest(
            displayName: "Bedroll",
                descriptions: new() { "A bedroll spread on the cave floor near the entrance light" },
                items: new()
                {
                    new ItemElement(new Cloth()),
                },
                moods: new[] { "rolled", "dirt-darkened", "low" }
            ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "bushcraft", ["smell"] = "petrichor" } },
            new LanternPointOfInterest(
            displayName: "Lantern Hook",
                descriptions: new() { "An iron hook driven into the rock, a lantern hanging from it" },
                items: new()
                {
                    new ItemElement(new Lantern()),
                },
                moods: new[] { "iron", "hanging", "soot-blackened" }
            ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "firecraft" } },
            new OrePointOfInterest(
            displayName: "Ore Pile",
                descriptions: new() { "A small heap of iron ore staged for hauling out to the village" },
                items: new()
                {
                    new ItemElement(new IronOre()),
                    new ItemElement(new IronOre()),
                },
                moods: new[] { "heaped", "heavy", "dark" }
            ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } },
        };
    }

    /// <summary>Coast fisherman camp: bedroll, drying frame, net pile.</summary>
    public static List<PointOfInterest> BuildCoastCamp()
    {
        return new List<PointOfInterest>
        {
            new BedrollPointOfInterest(
            displayName: "Bedroll",
                descriptions: new() { "A salt-stiff bedroll laid above the tide-line, cloth still damp" },
                items: new()
                {
                    new ItemElement(new Cloth()),
                },
                moods: new[] { "salt-stiff", "low", "rolled" }
            ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "bushcraft", ["smell"] = "petrichor" } },
            new FramePointOfInterest(
            displayName: "Drying Frame",
                descriptions: new() { "A wooden frame strung with split fish drying in the wind" },
                items: new()
                {
                    new ItemElement(new Herring()),
                    new ItemElement(new Herring()),
                },
                moods: new[] { "wind-rocked", "fragrant", "tall" }
            ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "anglery", ["smell"] = "brine_sense" } },
            new NetPointOfInterest(
            displayName: "Net Pile",
                descriptions: new() { "A bundle of mended net heaped at the edge of the camp" },
                items: new()
                {
                    new ItemElement(new Net()),
                },
                moods: new[] { "tangled", "rope-coarse", "heavy" }
            ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "knotwork" } },
        };
    }

    // ── Stores ───────────────────────────────────────────────────────────────
    // Where a wilderness worker keeps what they have cut, burned, caught or dug, and so where they
    // trade it: the goods are not carried about. A small lean-to beside the camp, entered from it.

    /// <summary>The woodcutter's and charcoal burner's shed: stacked cordwood and sacked charcoal.</summary>
    public static Area BuildWoodshed()
    {
        var shed = new ShedArea("Woodshed", "in the woodshed", "duck into the woodshed",
            new() { "A lean-to of split poles roofed with bark, stacked to the eaves with cut wood" },
            new[] { "resinous", "dim", "stacked" });
        shed.PointsOfInterest.Add(new FirewoodPointOfInterest(
            displayName: "Cordwood Stack",
            descriptions: new() { "Cordwood split and stacked bark-up to season, a cord at a time" },
            items: new() { new ItemElement(new Cordwood()), new ItemElement(new Cordwood()), new ItemElement(new Log()) },
            moods: new[] { "stacked", "seasoning", "resinous" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["smell"] = "petrichor" } });
        shed.PointsOfInterest.Add(new SackPointOfInterest(
            displayName: "Charcoal Sacks",
            descriptions: new() { "Sacks of charcoal tied at the neck, black dust on everything near them" },
            items: new() { new ItemElement(new Coal()), new ItemElement(new Coal()) },
            moods: new[] { "black", "dusty", "light" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft" } });
        return shed;
    }

    /// <summary>The fisherman's net shed: salted catch in a barrel and spare gear on pegs.</summary>
    public static Area BuildNetShed()
    {
        var shed = new ShedArea("Net Shed", "in the net shed", "duck into the net shed",
            new() { "A tarred shed above the tide-line, nets hung from the rafters and a salting barrel by the door" },
            new[] { "tarry", "salt", "cramped" });
        shed.PointsOfInterest.Add(new BarrelPointOfInterest(
            displayName: "Salting Barrel",
            descriptions: new() { "A barrel of fish packed in salt, layer on layer" },
            items: new() { new ItemElement(new SaltFish()), new ItemElement(new SaltFish()), new ItemElement(new Herring()) },
            moods: new[] { "briny", "packed", "heavy" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "anglery", ["smell"] = "brine_sense" } });
        shed.PointsOfInterest.Add(new NetPointOfInterest(
            displayName: "Hung Nets",
            descriptions: new() { "Spare nets hung from the rafters to dry, floats knocking together" },
            items: new() { new ItemElement(new Net()) },
            moods: new[] { "hanging", "tarry", "knotted" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "knotwork" } });
        return shed;
    }

    /// <summary>The cave miner's ore store: sacked ore and a lamp, out of the wet.</summary>
    public static Area BuildOreStore()
    {
        var shed = new ShedArea("Ore Store", "in the ore store", "duck into the ore store",
            new() { "A dry side-hollow walled off with stacked stones, where the sacked ore is kept out of the wet" },
            new[] { "dry", "dusty", "close" });
        shed.PointsOfInterest.Add(new SackPointOfInterest(
            displayName: "Ore Sacks",
            descriptions: new() { "Sacks of broken ore, heavy as stones, tied and stacked" },
            items: new() { new ItemElement(new IronOre()), new ItemElement(new IronOre()), new ItemElement(new Coal()) },
            moods: new[] { "heavy", "stacked", "gritty" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "veinsight" } });
        shed.PointsOfInterest.Add(new LanternPointOfInterest(
            displayName: "Spare Lamp",
            descriptions: new() { "A spare lamp hung on a peg driven into a crack" },
            items: new() { new ItemElement(new Lantern()) },
            moods: new[] { "dim", "hanging" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "delving" } });
        return shed;
    }
}
