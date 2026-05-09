using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// Builders for farm animal-pen areas (Sheep Pen, Pigsty, Dairy Shed, Chicken Coop).
/// Each method returns a complete <see cref="Area"/> with PoIs already attached.
/// </summary>
public static class AnimalPenSubfactory
{
    // ── Sheep Pen ────────────────────────────────────────────────────────────

    public static Area BuildSheepPen()
    {
        var pen = new Area(
            displayName: "Sheep Pen",
            contextDescription: "in the sheep pen",
            transitionDescription: "step into the sheep pen",
            descriptions: new() { "A timber-fenced sheep pen, sheep crowding around the trough" },
            moods: new[] { "dusty", "warm", "lanolin-scented", "milling", "cluttered" }
        );

        pen.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Trough",
            descriptions: new() { "A long wooden trough heaped with hay and trampled scraps" },
            items: new()
            {
                new ItemElement(new Hay()),
                new ItemElement(new Hay()),
            },
            moods: new[] { "long", "trampled", "dusty" }
        ));

        pen.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Shearing Post",
            descriptions: new() { "A worn post for tying sheep, wool-tufts caught in its splinters" },
            items: new()
            {
                new ItemElement(new Wool()),
                new ItemElement(new Shears()),
            },
            moods: new[] { "polished", "wool-tufted", "low" }
        ));

        return pen;
    }

    // ── Pigsty ───────────────────────────────────────────────────────────────

    public static Area BuildPigsty()
    {
        var pigsty = new Area(
            displayName: "Pigsty",
            contextDescription: "by the pigsty",
            transitionDescription: "approach the pigsty",
            descriptions: new() { "A walled mud pen with grunting pigs and a heavy smell of mire" },
            moods: new[] { "muddy", "smelly", "wet", "loud", "rank", "grunting" }
        );

        pigsty.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Trough",
            descriptions: new() { "A trough heaped with kitchen scraps and damp slop" },
            moods: new[] { "low", "dirty", "mucky" }
        ));

        return pigsty;
    }

    // ── Dairy Shed ───────────────────────────────────────────────────────────

    public static Area BuildDairyShed()
    {
        var shed = new Area(
            displayName: "Dairy Shed",
            contextDescription: "in the dairy shed",
            transitionDescription: "step into the dairy shed",
            descriptions: new() { "A cool stone-floored shed where milk is churned and cheese is pressed" },
            moods: new[] { "cool", "stone-floored", "milk-scented", "tidy", "white" }
        );

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Churn",
            descriptions: new() { "A tall wooden churn with its dasher leaning against the side" },
            items: new()
            {
                new ItemElement(new Butter()),
            },
            moods: new[] { "tall", "wooden", "creaking" }
        ));

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Mold Rack",
            descriptions: new() { "A timber rack of cheese molds in various stages of pressing" },
            items: new()
            {
                new ItemElement(new Cheese()),
            },
            moods: new[] { "rowed", "white", "stacked" }
        ));

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Pail",
            descriptions: new() { "A wooden pail of fresh milk, foam still rising at its rim" },
            items: new()
            {
                new ItemElement(new Milk()),
            },
            moods: new[] { "wooden", "foaming", "warm" }
        ));

        return shed;
    }

    // ── Chicken Coop ─────────────────────────────────────────────────────────

    public static Area BuildChickenCoop()
    {
        var coop = new Area(
            displayName: "Chicken Coop",
            contextDescription: "inside the chicken coop",
            transitionDescription: "step into the chicken coop",
            descriptions: new() { "A low timber coop crowded with hens, smelling of damp feathers and droppings" },
            moods: new[] { "low", "smelly", "dim", "crowded", "warm", "clucking", "feathery" }
        );

        coop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Nest Box",
            descriptions: new() { "A row of straw-lined boxes where the hens lay" },
            items: new()
            {
                new ItemElement(new Egg()),
                new ItemElement(new Egg()),
                new ItemElement(new Feather()),
                new ItemElement(new Feather()),
                new ItemElement(new Feather()),
                new ItemElement(new Straw()),
            },
            moods: new[] { "warm", "straw-lined", "dim", "fragrant" }
        ));

        return coop;
    }
}
