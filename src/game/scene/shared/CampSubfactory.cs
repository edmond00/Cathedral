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
            new PointOfInterest(
                displayName: "Bedroll",
                descriptions: new() { "A rolled bedroll of coarse cloth, half-unrolled by a tree-root" },
                items: new()
                {
                    new ItemElement(new Cloth()),
                },
                moods: new[] { "rolled", "dirty", "low" }
            ),
            new PointOfInterest(
                displayName: "Fire Pit",
                descriptions: new() { "A blackened ring of stones with the cooled remains of a small fire" },
                items: new()
                {
                    new ItemElement(new Coal()),
                    new ItemElement(new Twig()),
                },
                moods: new[] { "blackened", "circular", "cold" }
            ),
            new PointOfInterest(
                displayName: "Sack",
                descriptions: new() { "A heavy sack leaning against a stump, holding the day's gathered wood" },
                items: new()
                {
                    new ItemElement(new Log()),
                    new ItemElement(new Bark()),
                },
                moods: new[] { "leaning", "heavy", "rough" }
            ),
        };
    }

    /// <summary>Cave miner camp: bedroll, lantern hook, ore pile.</summary>
    public static List<PointOfInterest> BuildMineCamp()
    {
        return new List<PointOfInterest>
        {
            new PointOfInterest(
                displayName: "Bedroll",
                descriptions: new() { "A bedroll spread on the cave floor near the entrance light" },
                items: new()
                {
                    new ItemElement(new Cloth()),
                },
                moods: new[] { "rolled", "dirt-darkened", "low" }
            ),
            new PointOfInterest(
                displayName: "Lantern Hook",
                descriptions: new() { "An iron hook driven into the rock, a lantern hanging from it" },
                items: new()
                {
                    new ItemElement(new Lantern()),
                },
                moods: new[] { "iron", "hanging", "soot-blackened" }
            ),
            new PointOfInterest(
                displayName: "Ore Pile",
                descriptions: new() { "A small heap of iron ore staged for hauling out to the village" },
                items: new()
                {
                    new ItemElement(new IronOre()),
                    new ItemElement(new IronOre()),
                },
                moods: new[] { "heaped", "heavy", "dark" }
            ),
        };
    }

    /// <summary>Coast fisherman camp: bedroll, drying frame, net pile.</summary>
    public static List<PointOfInterest> BuildCoastCamp()
    {
        return new List<PointOfInterest>
        {
            new PointOfInterest(
                displayName: "Bedroll",
                descriptions: new() { "A salt-stiff bedroll laid above the tide-line, cloth still damp" },
                items: new()
                {
                    new ItemElement(new Cloth()),
                },
                moods: new[] { "salt-stiff", "low", "rolled" }
            ),
            new PointOfInterest(
                displayName: "Drying Frame",
                descriptions: new() { "A wooden frame strung with split fish drying in the wind" },
                items: new()
                {
                    new ItemElement(new Herring()),
                    new ItemElement(new Herring()),
                },
                moods: new[] { "wind-rocked", "fragrant", "tall" }
            ),
            new PointOfInterest(
                displayName: "Net Pile",
                descriptions: new() { "A bundle of mended net heaped at the edge of the camp" },
                items: new()
                {
                    new ItemElement(new Net()),
                },
                moods: new[] { "tangled", "rope-coarse", "heavy" }
            ),
        };
    }
}
