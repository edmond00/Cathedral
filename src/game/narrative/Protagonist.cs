using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Debug;
// using Cathedral.Game.Narrative.Nodes.Debug;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The player-controlled protagonist.  Extends <see cref="PartyMember"/> with features
/// that are exclusive to the protagonist: journal, companion party list, and location tracking.
///
/// Shared state (body, organs, modiMentis, inventory, …) lives in <see cref="PartyMember"/>.
/// </summary>
public class Protagonist : PartyMember
{
    // ── Protagonist-only data ────────────────────────────────────

    /// <summary>Journal entries written throughout the journey.</summary>
    public List<string> JournalEntries { get; set; } = new();

    /// <summary>Named companions travelling with the protagonist.</summary>
    public List<Companion> CompanionParty { get; set; } = new();

    /// <summary>Current location on the world sphere (used as RNG seed).</summary>
    public int CurrentLocationId { get; set; }

    // ── PartyMember abstract ─────────────────────────────────────
    public override string DisplayName => "Protagonist";

    // ── Constructor ──────────────────────────────────────────────
    public Protagonist() : base(SpeciesRegistry.Human)
    {
        InitializeTestEquipment(); // TEMP TEST ITEMS — remove when real acquiring works
    }

    // ── Test equipment (temporary) ───────────────────────────────

    /// <summary>
    /// Pre-equips a selection of debug items so the Inventory tab has visible content
    /// from the first launch. Remove this method and its call when real item acquisition
    /// is exercised through gameplay.
    /// </summary>
    private void InitializeTestEquipment()
    {
        // --- Build a flask and fill it with spring water ---
        var flask = new DebugInventoryNode.GlassFlask();
        flask.TryAdd(new DebugInventoryNode.SpringWater());

        // --- Build a pouch with red wine bottle inside, then put pouch in the backpack ---
        var pouch = new DebugInventoryNode.LeatherPouch();
        var wineBottle = new DebugInventoryNode.GlassFlask();
        wineBottle.TryAdd(new DebugInventoryNode.RedWine());
        pouch.TryAdd(wineBottle);

        var backpack = new DebugInventoryNode.TestBackpack();
        backpack.TryAdd(pouch);

        // --- Equip items directly at their preferred anchors ---
        Equip(EquipmentAnchor.Headgear,  new DebugInventoryNode.LeatherCap());
        Equip(EquipmentAnchor.Outerwear, new DebugInventoryNode.WoolenCloak());
        Equip(EquipmentAnchor.Bodywear,  new DebugInventoryNode.LinenShirt());
        // Footwear holds both socks (Small=3) and boots (Small=3) → uses full 6-slot capacity
        Equip(EquipmentAnchor.Footwear,  new DebugInventoryNode.WoolSocks());
        Equip(EquipmentAnchor.Footwear,  new DebugInventoryNode.LeatherBoots());
        Equip(EquipmentAnchor.RightHold, new DebugInventoryNode.IronDagger());
        Equip(EquipmentAnchor.LeftHold,  flask);
        Equip(EquipmentAnchor.BeltGear,  backpack);
    }
}
