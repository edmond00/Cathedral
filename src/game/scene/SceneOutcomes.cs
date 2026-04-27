using System;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Dialogue.Affinity;

namespace Cathedral.Game.Scene;

// ── Scene-specific OutcomeReport concrete types ───────────────────────────────
// These need Scene / PoV / NPC types, so they live in the Scene namespace.

/// <summary>Picks up an item from a PoI in the scene and adds it to the inventory.</summary>
public sealed class ItemAcquisitionOutcome : OutcomeReport
{
    private readonly ItemElement _itemElement;

    public ItemAcquisitionOutcome(ItemElement itemElement)
        : base($"Item received: {itemElement.Item.DisplayName}", OutcomeReportSeverity.Positive)
    {
        _itemElement = itemElement;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null || pov == null) return;

        // Remove from the first PoI that holds it (area or current spot)
        var allPoIs = pov.InSpot != null
            ? pov.InSpot.PointsOfInterest.AsEnumerable()
            : pov.Where.PointsOfInterest.AsEnumerable();

        foreach (var poi in allPoIs)
            if (poi.Items.Remove(_itemElement)) break;

        protagonist.Inventory.Add(_itemElement.Item);
        scene.StateChanges.Capture(_itemElement);
    }
}

/// <summary>Picks up an item from a CorpseBodyPartPoI (cut verb).</summary>
public sealed class CorpseItemAcquisitionOutcome : OutcomeReport
{
    private readonly ItemElement _itemElement;

    public CorpseItemAcquisitionOutcome(ItemElement itemElement)
        : base($"Item received: {itemElement.Item.DisplayName}", OutcomeReportSeverity.Positive)
    {
        _itemElement = itemElement;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (pov?.InSpot == null) return;

        foreach (var poi in pov.InSpot.PointsOfInterest.OfType<CorpseBodyPartPoI>())
            if (poi.Items.Remove(_itemElement)) break;

        protagonist.Inventory.Add(_itemElement.Item);
    }
}

/// <summary>Moves the PoV to a new area.</summary>
public sealed class AreaMoveOutcome : OutcomeReport
{
    private readonly Area _destination;

    public AreaMoveOutcome(Area destination)
        : base($"Moved to: {destination.DisplayName}", OutcomeReportSeverity.Neutral)
    {
        _destination = destination;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (pov == null) return;
        pov.Where = _destination;
        pov.Focus = null;
    }
}

/// <summary>Enters a spot in the current area.</summary>
public sealed class SpotEnterOutcome : OutcomeReport
{
    private readonly Spot _spot;

    public SpotEnterOutcome(Spot spot)
        : base($"Examining: {spot.DisplayName}", OutcomeReportSeverity.Neutral)
    {
        _spot = spot;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (pov == null) return;
        pov.InSpot = _spot;
        pov.Focus  = null;
    }
}

/// <summary>Leaves the current spot.</summary>
public sealed class SpotLeaveOutcome : OutcomeReport
{
    public SpotLeaveOutcome()
        : base("Left the spot", OutcomeReportSeverity.Neutral) { }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (pov == null) return;
        pov.InSpot = null;
        pov.Focus  = null;
    }
}

/// <summary>Unlocks a door and immediately passes through it.</summary>
public sealed class DoorUnlockOutcome : OutcomeReport
{
    private readonly DoorPointOfInterest _door;
    private readonly Area                _destination;

    public DoorUnlockOutcome(DoorPointOfInterest door, Area destination)
        : base($"Door unlocked — entered {destination.DisplayName}", OutcomeReportSeverity.Neutral)
    {
        _door        = door;
        _destination = destination;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null || pov == null) return;
        _door.DoorState = DoorState.Unlocked;
        pov.Where       = _destination;
        pov.Focus       = null;
        scene.StateChanges.Capture(_door);
    }
}

/// <summary>Kills an NPC without combat and spawns a corpse.</summary>
public sealed class NpcSlaynOutcome : OutcomeReport
{
    private readonly SceneNpc _sceneNpc;

    public NpcSlaynOutcome(SceneNpc sceneNpc)
        : base($"Slain: {sceneNpc.DisplayName}", OutcomeReportSeverity.Negative)
    {
        _sceneNpc = sceneNpc;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null || pov == null) return;
        _sceneNpc.Entity.IsAlive = false;
        var corpse = _sceneNpc.Entity.GenerateCorpse(pov.Where);
        scene.AddSpotToArea(pov.Where, corpse);
        pov.Focus = null;
    }
}

/// <summary>Queues a fight with a full NPC (sets scene.PendingFightRequest).</summary>
public sealed class FightTriggerOutcome : OutcomeReport
{
    private readonly NpcEntity _npc;

    public FightTriggerOutcome(NpcEntity npc)
        : base($"Combat begins: {npc.DisplayName}", OutcomeReportSeverity.Negative)
    {
        _npc = npc;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null) return;
        scene.PendingFightRequest = new FightRequest(_npc);
    }
}

/// <summary>Queues a dialogue session with an NPC (sets scene.PendingDialogueRequest).</summary>
public sealed class DialogueTriggerOutcome : OutcomeReport
{
    private readonly NpcEntity _npc;
    private readonly string    _treeId;

    public DialogueTriggerOutcome(NpcEntity npc, string treeId)
        : base($"Conversation: {npc.DisplayName}", OutcomeReportSeverity.Neutral)
    {
        _npc    = npc;
        _treeId = treeId;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null) return;
        scene.PendingDialogueRequest = new DialogueRequest(_npc, _treeId);
    }
}

/// <summary>Changes affinity toward the protagonist after appeasement.</summary>
public sealed class AffinityChangeOutcome : OutcomeReport
{
    private readonly NpcEntity _npc;

    public AffinityChangeOutcome(NpcEntity npc)
        : base($"Appeasement: {npc.DisplayName} — hostile→suspicious", OutcomeReportSeverity.Positive)
    {
        _npc = npc;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        _npc.AffinityTable.ClearEnemy(protagonist.DisplayName);
        _npc.AffinityTable.SetLevel(protagonist.DisplayName, Cathedral.Game.Dialogue.Affinity.AffinityLevel.Suspicious);
    }
}

/// <summary>Internal: records an element in scene.StateChanges. No UI chip.</summary>
public sealed class StateCaptureOutcome : OutcomeReport
{
    private readonly Element _element;
    public override bool ShowInUI => false;

    public StateCaptureOutcome(Element element)
        : base(string.Empty, OutcomeReportSeverity.Neutral)
    {
        _element = element;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
        => scene?.StateChanges.Capture(_element);
}

/// <summary>
/// Internal: queues a reminescence phase transition.
/// Does not appear as a UI chip — phase management is handled by NarrativeController.
/// </summary>
public sealed class ReminescenceTransitionOutcome : OutcomeReport
{
    private readonly string _fromId;
    private readonly string _nextId;
    private readonly string _fragmentName;

    public override bool ShowInUI => false;

    public ReminescenceTransitionOutcome(string fromId, string nextId, string fragmentName)
        : base(string.Empty, OutcomeReportSeverity.Neutral)
    {
        _fromId       = fromId;
        _nextId       = nextId;
        _fragmentName = fragmentName;
    }

    public override void Apply(Protagonist protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null) return;
        scene.PendingReminescenceTransition = new ReminescenceTransitionRequest(_fromId, _nextId, _fragmentName);
    }
}
