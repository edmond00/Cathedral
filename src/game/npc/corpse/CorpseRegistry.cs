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
/// unchanged. Two identical parts still collapse to one goal ("cut the meat"); cutting removes one
/// instance and the goal returns while any remain.</para>
///
/// <para><b>A body yields four to eight parts</b>, and both ends of that are deliberate. Below four
/// a carcass is one cut and a shrug — the whole approach (a kill, a knife, a noetic point per
/// attempt) costs more than the body is worth. Above eight the list stops being a choice: the goals
/// run past what a phase can offer and the pack cannot hold them anyway, so the surplus reads as
/// litter rather than as plenty. Duplicates count toward the eight but not toward the goals, so a
/// pig at <c>Meat x3</c> offers one "cut the meat" that can be taken three times.</para>
///
/// <para>What varies between species is <b>which</b> parts and <b>how many</b>, never the item's
/// name — see <c>BodyPartItem</c> for why that rule is worth keeping. A bear is a bear because it
/// gives three cuts, two claws and a skull; it is not a bear because its meat is called bear meat.
/// Size is the whole scale: a hare gives five parts and a bear eight.</para>
/// </summary>
public static class CorpseRegistry
{
    // ── Species → what the body yields ────────────────────────────────────────

    private record CorpseTemplate(string Description, Func<List<ItemElement>> PartFactory);

    private static readonly Dictionary<Type, CorpseTemplate> _templates = new()
    {
        // A human is butchered like anything else, and the game does not soften it: the same Meat a
        // pig gives, plus what a body is actually robbed of — the hair off the head, the skull out
        // from under it. What they were carrying is a separate PoI beside this one, because that is
        // grabbed and this is cut.
        [typeof(HumanSpecies)] = new(
            "the body lies where it fell, pale and slack, the limbs already going stiff",
            () => new()
            {
                new ItemElement(new Meat()), new ItemElement(new Meat()),
                new ItemElement(new Liver()),
                new ItemElement(new Heart()),
                new ItemElement(new Brain()),
                new ItemElement(new Bone()),
                new ItemElement(new Skull()),
                new ItemElement(new Hair()),
            }),

        [typeof(WolfSpecies)] = new(
            "the wolf lies dead, muzzle still drawn back, the matted pelt stretched over its ribs",
            () => new()
            {
                new ItemElement(new Pelt()),
                new ItemElement(new Meat()), new ItemElement(new Meat()),
                new ItemElement(new Liver()),
                new ItemElement(new Fang()), new ItemElement(new Fang()),
                new ItemElement(new Claw()),
                new ItemElement(new Sinew()),
            }),

        // The biggest thing in the game, and the only carcass that gives a skull as well as a hide.
        [typeof(BearSpecies)] = new(
            "the bear is a hill of dead muscle, jaws agape, each claw like a knife",
            () => new()
            {
                new ItemElement(new Hide()),
                new ItemElement(new Meat()), new ItemElement(new Meat()),
                new ItemElement(new Suet()),
                new ItemElement(new Fang()),
                new ItemElement(new Claw()), new ItemElement(new Claw()),
                new ItemElement(new Skull()),
            }),

        [typeof(BoarSpecies)] = new(
            "the boar lies on its side, tusks intact, the coarse-bristled barrel of it still warm",
            () => new()
            {
                new ItemElement(new Hide()),
                new ItemElement(new Meat()), new ItemElement(new Meat()), new ItemElement(new Meat()),
                new ItemElement(new Liver()),
                new ItemElement(new Tusk()), new ItemElement(new Tusk()),
                new ItemElement(new Suet()),
            }),

        [typeof(FoxSpecies)] = new(
            "the fox is a small sleek russet weight, sharp nose down in the dirt",
            () => new()
            {
                new ItemElement(new Pelt()),
                new ItemElement(new Meat()),
                new ItemElement(new Liver()),
                new ItemElement(new Fang()),
                new ItemElement(new Claw()),
                new ItemElement(new Bone()),
            }),

        [typeof(CatSpecies)] = new(
            "the cat lies curled and still, claws extended in death",
            () => new()
            {
                new ItemElement(new Pelt()),
                new ItemElement(new Meat()),
                new ItemElement(new Heart()),
                new ItemElement(new Fang()),
                new ItemElement(new Claw()), new ItemElement(new Claw()),
            }),

        [typeof(DogSpecies)] = new(
            "the dog lies with its head lolling, the coarse-furred body slack",
            () => new()
            {
                new ItemElement(new Pelt()),
                new ItemElement(new Meat()), new ItemElement(new Meat()),
                new ItemElement(new Liver()),
                new ItemElement(new Fang()),
                new ItemElement(new Bone()),
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
    /// The remains of a fallen <b>companion</b>: the same body and belongings an NPC of that species
    /// would leave, built from the <see cref="PartyMember"/> directly.
    ///
    /// <para>It needs its own entry point because a companion is not an <see cref="NpcEntity"/> —
    /// recruiting moves the <see cref="EnemyCombatant"/> into the party and drops the wrapper, so
    /// there is no <c>Archetype</c> to read the species off and no <c>Combatant</c> property to
    /// reach the pack through. Both come straight off the member instead, and the resulting
    /// <see cref="CorpsePointOfInterest"/> carries no entity at all.</para>
    ///
    /// <para>Everything downstream is identical — the body is <c>cut</c>, the belongings are
    /// <c>grab</c>bed — because those verbs gate on the PoI's <em>type</em>, not on who it was.</para>
    /// </summary>
    public static List<PointOfInterest> CreateForCompanion(PartyMember member)
    {
        var template = _templates.GetValueOrDefault(member.Species.GetType());

        var remains = new List<PointOfInterest>
        {
            new CorpsePointOfInterest(
                npcEntity:    null,
                displayName:  $"{member.DisplayName}'s Remains",
                descriptions: new() { template?.Description ?? $"the body of {member.DisplayName}, cooling on the ground" },
                parts:        template?.PartFactory() ?? new(),
                moods:        CorpseMoods),
        };

        // Same rule as an NPC's: only a human leaves a separate pile of gear, and never an empty one.
        if (member.Species is HumanSpecies)
        {
            var items = member.GetAllItems();
            if (items.Count > 0)
                remains.Add(new PointOfInterest(
                    displayName:    $"{member.DisplayName}'s Belongings",
                    referenceLemma: "belongings",
                    descriptions:   new() { $"the clothes and gear {member.DisplayName} will not be needing" },
                    items:          items.Select(i => new ItemElement(i)).ToList(),
                    moods:          new[] { "disarranged", "still-warm", "unclaimed" }));
        }

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
