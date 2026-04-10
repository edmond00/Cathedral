using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Corpse;

/// <summary>
/// Builds <see cref="CorpseSpot"/>s for named NPCs based on their species.
/// Each species has a fixed template of 2–3 <see cref="CorpseBodyPartPoI"/>s with harvestable items.
/// Human NPCs additionally receive a dynamic wearing <see cref="PointOfInterest"/> reflecting their
/// actual equipped items.
/// </summary>
public static class CorpseRegistry
{
    // ── Internal template record ──────────────────────────────────────────────

    private record BodyPartTemplate(
        string DisplayName,
        string Description,
        string[] KeywordStrings,
        Func<List<ItemElement>> ItemFactory);

    // ── Species → body-part templates ─────────────────────────────────────────

    private static readonly Dictionary<Type, BodyPartTemplate[]> _templates = new()
    {
        [typeof(HumanSpecies)] = new[]
        {
            new BodyPartTemplate(
                "Head",
                "the pale, slack face of the corpse",
                new[] { "the pale <face>, slack in death", "the closed <eyes> of a dead person" },
                () => new()),  // human loot comes from the Wearing PoI

            new BodyPartTemplate(
                "Torso",
                "the still chest and abdomen of the body",
                new[] { "the still <chest> of the corpse", "the cooling <torso> of the body" },
                () => new()),

            new BodyPartTemplate(
                "Limbs",
                "the cold stiff arms and legs of the corpse",
                new[] { "the stiff cold <arm>s of the dead", "the limp <leg>s of the corpse" },
                () => new()),
        },

        [typeof(WolfSpecies)] = new[]
        {
            new BodyPartTemplate(
                "Muzzle",
                "the snarling muzzle of the dead wolf",
                new[] { "the limp grey <muzzle> of the wolf", "the bared <fang>s of the dead beast" },
                () => new() { new ItemElement(new AnimalFang()), new ItemElement(new AnimalFang()) }),

            new BodyPartTemplate(
                "Body",
                "the matted pelt stretched over the wolf's ribs",
                new[] { "the coarse grey <pelt> of the wolf carcass", "the still warm <hide>" },
                () => new() { new ItemElement(new AnimalHide()) }),

            new BodyPartTemplate(
                "Forepaws",
                "the dead wolf's forepaws, claws still extended",
                new[] { "the splayed <paw>s of the wolf", "the curved dark <claw>s catching the light" },
                () => new() { new ItemElement(new AnimalClaw()), new ItemElement(new AnimalClaw()) }),
        },

        [typeof(BearSpecies)] = new[]
        {
            new BodyPartTemplate(
                "Head",
                "the massive skull of the bear, jaws agape",
                new[] { "the great open <jaw> of the bear", "the long yellow <fang>s of the dead beast" },
                () => new() { new ItemElement(new AnimalFang()), new ItemElement(new AnimalFang()) }),

            new BodyPartTemplate(
                "Body",
                "the thick bear pelt, almost a finger deep",
                new[] { "the dense shaggy <pelt> of the bear", "the heavy <hide> still warm" },
                () => new() { new ItemElement(new AnimalHide()), new ItemElement(new AnimalHide()) }),

            new BodyPartTemplate(
                "Forepaws",
                "the bear's great forepaws, each claw like a knife",
                new[] { "the enormous <paw>s of the bear", "the long black <claw>s, still sharp" },
                () => new() { new ItemElement(new AnimalClaw()), new ItemElement(new AnimalClaw()), new ItemElement(new AnimalClaw()) }),
        },

        [typeof(BoarSpecies)] = new[]
        {
            new BodyPartTemplate(
                "Head",
                "the boar's broad head, tusks intact",
                new[] { "the broad <snout> of the dead boar", "the curved ivory <tusk>s from the jaw" },
                () => new() { new ItemElement(new AnimalFang()), new ItemElement(new AnimalFang()) }),

            new BodyPartTemplate(
                "Body",
                "the coarse-bristled barrel body of the boar",
                new[] { "the bristled <hide> of the boar, rough as brushwood", "the broad <carcass> of the beast" },
                () => new() { new ItemElement(new AnimalHide()) }),
        },

        [typeof(FoxSpecies)] = new[]
        {
            new BodyPartTemplate(
                "Head",
                "the sharp-nosed head of the dead fox",
                new[] { "the slender <muzzle> of the fox", "the needle-fine <fang>s of a small predator" },
                () => new() { new ItemElement(new AnimalFang()) }),

            new BodyPartTemplate(
                "Body",
                "the sleek russet body of the fox",
                new[] { "the russet <pelt> of the fox, still lustrous", "the slim <body> of the dead animal" },
                () => new() { new ItemElement(new AnimalHide()) }),
        },

        [typeof(CatSpecies)] = new[]
        {
            new BodyPartTemplate(
                "Head",
                "the small delicate head of the dead cat",
                new[] { "the narrow <muzzle> of the dead cat", "the fine curved <fang>s" },
                () => new() { new ItemElement(new AnimalFang()) }),

            new BodyPartTemplate(
                "Paws",
                "the small paws, claws extended in death",
                new[] { "the small <paw>s of the cat", "the tiny hooked <claw>s" },
                () => new() { new ItemElement(new AnimalClaw()) }),
        },

        [typeof(DogSpecies)] = new[]
        {
            new BodyPartTemplate(
                "Head",
                "the lolling head of the dead dog",
                new[] { "the slack <muzzle> of the dead stray", "the yellow <fang>s of a large dog" },
                () => new() { new ItemElement(new AnimalFang()) }),

            new BodyPartTemplate(
                "Body",
                "the coarse-furred body of the stray",
                new[] { "the matted <fur> of the dead stray", "the <carcass> of the dog" },
                () => new() { new ItemElement(new AnimalHide()) }),
        },
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="CorpseSpot"/> for a named NPC using its species template.
    /// Human NPCs receive an additional wearing <see cref="PointOfInterest"/> from equipped items.
    /// </summary>
    public static CorpseSpot CreateForNamedNpc(NpcEntity entity, Area area)
    {
        var speciesType = entity.Archetype.Species.GetType();
        var templates   = _templates.TryGetValue(speciesType, out var t) ? t : Array.Empty<BodyPartTemplate>();

        var bodyParts = new List<PointOfInterest>();
        foreach (var tmpl in templates)
            bodyParts.Add(BuildBodyPartPoI(tmpl));

        // Human NPCs get a Wearing PoI reflecting actual equipped items
        if (entity.Archetype.Species is HumanSpecies)
        {
            var wearingPoi = BuildWearingPoI(entity);
            if (wearingPoi != null)
                bodyParts.Add(wearingPoi);
        }

        var name = $"{entity.DisplayName}'s Remains";
        return new CorpseSpot(
            area, entity, name,
            descriptions: new() { $"The body of {entity.DisplayName}, cooling on the ground" },
            keywords: new()
            {
                KeywordInContext.Parse($"the <body> of {entity.DisplayName.ToLowerInvariant()}, sprawled in the dirt"),
                KeywordInContext.Parse("the dead <remains>, already attracting flies"),
            },
            bodyParts);
    }

    /// <summary>
    /// Builds a <see cref="CorpseSpot"/> for a shallow NPC using caller-supplied PoIs.
    /// Called by <see cref="ShallowNpcArchetype.CreateCorpse"/>.
    /// </summary>
    public static CorpseSpot CreateForShallowNpc(
        ShallowNpcEntity entity,
        Area area,
        string displayName,
        List<string> descriptions,
        List<KeywordInContext> keywords,
        List<PointOfInterest> bodyParts)
    {
        return new CorpseSpot(area, entity, displayName, descriptions, keywords, bodyParts);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CorpseBodyPartPoI BuildBodyPartPoI(BodyPartTemplate tmpl)
    {
        var keywords = tmpl.KeywordStrings.Select(KeywordInContext.Parse).ToList();
        return new CorpseBodyPartPoI(
            tmpl.DisplayName,
            new() { tmpl.Description },
            keywords,
            tmpl.ItemFactory());
    }

    /// <summary>Builds a Wearing PoI from the NPC combatant's equipped items; null if empty.</summary>
    private static PointOfInterest? BuildWearingPoI(NpcEntity entity)
    {
        var items = entity.Combatant.GetAllItems();
        if (items.Count == 0) return null;

        var itemElements = items.Select(i => new ItemElement(i)).ToList();
        return new PointOfInterest(
            "Wearing",
            new() { $"The clothing and belongings {entity.DisplayName} was carrying" },
            new() { KeywordInContext.Parse("the <clothing> and belongings of the dead") },
            itemElements);
    }
}
