using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Corpse;

/// <summary>
/// Builds the remains a slain NPC leaves behind: a <see cref="CorpsePointOfInterest"/> holding every
/// harvestable part, plus — for a human — a second, plain point of interest holding what they were
/// carrying.
///
/// <para>The split is what keeps the verbs apart without either having to reason about individual
/// items: everything in the corpse PoI is <c>cut</c>, everything in the belongings PoI is
/// <c>grab</c>/<c>steal</c>. Both are ordinary area PoIs, so the narration folds their items into the
/// observation's action list and one keyword offers every part at once.</para>
///
/// <para>Parts are listed flat per species rather than grouped by body part. The grouping used to be
/// real — a wolf's fangs sat in its muzzle, its hide in its body — but each group cost a narration
/// phase to reach, and the distinct actions a player sees come from the <i>item</i> names, which are
/// unchanged. Two identical parts still collapse to one goal ("cut the pork meat"); cutting removes
/// one instance and the goal returns while any remain.</para>
/// </summary>
public static class CorpseRegistry
{
    // ── Species → what the body yields ────────────────────────────────────────

    private record CorpseTemplate(string Description, Func<List<ItemElement>> PartFactory);

    private static readonly Dictionary<Type, CorpseTemplate> _templates = new()
    {
        // A human body yields nothing harvestable — what it leaves is what it carried.
        [typeof(HumanSpecies)] = new(
            "the body lies where it fell, pale and slack, the limbs already going stiff",
            () => new()),

        [typeof(WolfSpecies)] = new(
            "the wolf lies dead, muzzle still drawn back, the matted pelt stretched over its ribs",
            () => new()
            {
                new ItemElement(new AnimalFang()), new ItemElement(new AnimalFang()),
                new ItemElement(new AnimalHide()),
                new ItemElement(new AnimalClaw()), new ItemElement(new AnimalClaw()),
            }),

        [typeof(BearSpecies)] = new(
            "the bear is a hill of dead muscle, jaws agape, each claw like a knife",
            () => new()
            {
                new ItemElement(new AnimalFang()), new ItemElement(new AnimalFang()),
                new ItemElement(new AnimalHide()), new ItemElement(new AnimalHide()),
                new ItemElement(new AnimalClaw()), new ItemElement(new AnimalClaw()), new ItemElement(new AnimalClaw()),
            }),

        [typeof(BoarSpecies)] = new(
            "the boar lies on its side, tusks intact, the coarse-bristled barrel of it still warm",
            () => new()
            {
                new ItemElement(new AnimalFang()), new ItemElement(new AnimalFang()),
                new ItemElement(new AnimalHide()),
            }),

        [typeof(FoxSpecies)] = new(
            "the fox is a small sleek russet weight, sharp nose down in the dirt",
            () => new()
            {
                new ItemElement(new AnimalFang()),
                new ItemElement(new AnimalHide()),
            }),

        [typeof(CatSpecies)] = new(
            "the cat lies curled and still, claws extended in death",
            () => new()
            {
                new ItemElement(new AnimalFang()),
                new ItemElement(new AnimalClaw()),
            }),

        [typeof(DogSpecies)] = new(
            "the dog lies with its head lolling, the coarse-furred body slack",
            () => new()
            {
                new ItemElement(new AnimalFang()),
                new ItemElement(new AnimalHide()),
            }),
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// The remains of a named NPC: the body, and their belongings when they carried any. Returned in
    /// the order they should be observed — the body first, since that is what just happened.
    /// </summary>
    public static List<PointOfInterest> CreateForNamedNpc(NpcEntity entity)
    {
        var species  = entity.Archetype.Species.GetType();
        var template = _templates.GetValueOrDefault(species);

        var remains = new List<PointOfInterest>
        {
            new CorpsePointOfInterest(
                entity,
                displayName:  $"{entity.DisplayName}'s Remains",
                descriptions: new() { template?.Description ?? $"the body of {entity.DisplayName}, cooling on the ground" },
                parts:        template?.PartFactory() ?? new(),
                moods:        CorpseMoods),
        };

        var belongings = BuildBelongings(entity);
        if (belongings != null) remains.Add(belongings);

        return remains;
    }

    /// <summary>
    /// The remains of a shallow NPC — one body, never any belongings. Called by
    /// <see cref="ShallowNpcArchetype.CreateCorpse"/>, which supplies what the species leaves.
    /// </summary>
    public static List<PointOfInterest> CreateForShallowNpc(
        ShallowNpcEntity entity,
        string displayName,
        List<string> descriptions,
        List<ItemElement> parts)
    {
        return new List<PointOfInterest>
        {
            new CorpsePointOfInterest(entity, displayName, descriptions, parts, CorpseMoods),
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Shared mood vocabulary, so a body reads as one whatever killed it.</summary>
    private static readonly string[] CorpseMoods = { "still", "cooling", "bloodied", "lifeless" };

    /// <summary>
    /// What a human was wearing and carrying, as a plain PoI so the pickup verbs apply. Null when
    /// they carried nothing — an empty PoI would be an observation offering only IGNORE.
    /// </summary>
    private static PointOfInterest? BuildBelongings(NpcEntity entity)
    {
        if (entity.Archetype.Species is not HumanSpecies) return null;

        var items = entity.Combatant.GetAllItems();
        if (items.Count == 0) return null;

        return new PointOfInterest(
            displayName:    $"{entity.DisplayName}'s Belongings",
            referenceLemma: "belongings",
            descriptions:   new() { $"the clothes and gear {entity.DisplayName} will not be needing" },
            items:          items.Select(i => new ItemElement(i)).ToList(),
            moods:          new[] { "disarranged", "still-warm", "unclaimed" });
    }
}
