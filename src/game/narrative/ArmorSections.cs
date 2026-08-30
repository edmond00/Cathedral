using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Which body section each garment slot protects — the bridge between the wardrobe
/// (<see cref="WearSlot"/>) and the anatomy (body-part ids from the anatomy factories).
///
/// Several slots may guard the same section and their dice accumulate: legwear and footwear both
/// cover the lower limbs, and a trunk can be layered three deep. Left and right are deliberately
/// not distinguished — a glove on the right hand guards both arms and both hands — so the mapping
/// is slot → section rather than slot → limb.
///
/// <see cref="WearSlot.BeltGear"/> is deliberately absent: a belt guards nothing.
///
/// The section ids are human body-part ids. Beasts have a different anatomy (<c>muzzle</c>,
/// <c>limbs</c>) and wear nothing, so a lookup against a beast simply finds no matching body part
/// and yields no armour — callers must check the defender actually has the section rather than
/// assuming these ids exist.
/// </summary>
public static class ArmorSections
{
    /// <summary>Slot → the body-part id it protects. Slots absent from this map protect nothing.</summary>
    public static readonly IReadOnlyDictionary<WearSlot, string> SlotToSection =
        new Dictionary<WearSlot, string>
        {
            [WearSlot.Outerwear] = "trunk",
            [WearSlot.Bodywear]  = "trunk",
            [WearSlot.Underwear] = "trunk",

            [WearSlot.Neckwear]  = "visage",
            [WearSlot.Eyewear]   = "visage",

            [WearSlot.Headgear]  = "encephalon",

            [WearSlot.Handwear]  = "upper_limbs",

            [WearSlot.Legwear]   = "lower_limbs",
            [WearSlot.Footwear]  = "lower_limbs",
        };

    /// <summary>Every protectable section, in a stable order for reports.</summary>
    public static IReadOnlyList<string> AllSections { get; } =
        new[] { "encephalon", "visage", "trunk", "upper_limbs", "lower_limbs" };

    /// <summary>The slots whose garments contribute armour to <paramref name="sectionId"/>.</summary>
    public static IReadOnlyList<WearSlot> SlotsFor(string sectionId) =>
        SlotToSection.Where(kv => kv.Value == sectionId).Select(kv => kv.Key).ToList();

    /// <summary>The section a slot guards, or null when it guards nothing.</summary>
    public static string? SectionOf(WearSlot slot) =>
        SlotToSection.TryGetValue(slot, out var section) ? section : null;
}
