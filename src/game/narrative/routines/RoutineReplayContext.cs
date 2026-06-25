using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// Mutable state threaded through a routine replay (virtual or full).
///
/// Virtual replay (<see cref="DryRun"/> = true) validates that every step COULD execute without
/// touching real game state: constraint checks consult the <see cref="VirtualLedger"/> so that a
/// later step sees the would-be consumption of an earlier step. Full replay (<see cref="DryRun"/>
/// = false) mutates the real acting member / scene through each constraint's
/// <see cref="RoutineConstraint.Consume"/> and the verb's success reports.
/// </summary>
public class RoutineReplayContext
{
    /// <summary>The freshly built scene used for this replay (disposable; not the narration scene).</summary>
    public Scene.Scene Scene { get; }

    /// <summary>Working point of view, advanced as movement steps apply.</summary>
    public PoV Pov { get; }

    /// <summary>The protagonist owning the routine (party root for acting-member resolution).</summary>
    public Protagonist Protagonist { get; }

    /// <summary>The acting member for the current step (bound from the acting-member constraint).</summary>
    public PartyMember ActingMember { get; set; }

    /// <summary>True for virtual replay: no real state is mutated.</summary>
    public bool DryRun { get; }

    /// <summary>Item-id → count already (virtually) consumed during this replay.</summary>
    public Dictionary<string, int> VirtualLedger { get; } = new();

    public RoutineReplayContext(Scene.Scene scene, PoV pov, Protagonist protagonist, bool dryRun)
    {
        Scene        = scene;
        Pov          = pov;
        Protagonist  = protagonist;
        ActingMember = protagonist;
        DryRun       = dryRun;
    }

    /// <summary>
    /// Resolves a party member by their <see cref="PartyMember.DisplayName"/> within the party
    /// (protagonist + companion roster). Returns null when the member is no longer present.
    /// </summary>
    public PartyMember? ResolveMember(string memberKey)
    {
        if (memberKey == Protagonist.DisplayName) return Protagonist;
        return Protagonist.CompanionParty.FirstOrDefault(c => c.DisplayName == memberKey);
    }

    /// <summary>Count of an item currently held by the acting member, minus virtual-ledger spend.</summary>
    public int AvailableItemCount(string itemId)
    {
        int held = ActingMember.GetAllItems().Count(i => i.ItemId == itemId);
        VirtualLedger.TryGetValue(itemId, out int spent);
        return held - spent;
    }
}
