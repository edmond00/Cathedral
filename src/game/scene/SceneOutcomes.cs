using System;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
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
        : base($"Item received: {itemElement.Item.DisplayName}", OutcomeReportSeverity.Positive,
               $"picked up {itemElement.Item.WithArticle()}")
    {
        _itemElement = itemElement;
    }

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null || pov == null) return;
        // Shared pickup: removes from the holding PoI, adds to inventory, and stamps depletion.
        ItemPickup.Pick(scene, pov, protagonist, _itemElement);
    }
}

/// <summary>Picks up an item from a CorpseBodyPartPoI (cut verb).</summary>
public sealed class CorpseItemAcquisitionOutcome : OutcomeReport
{
    private readonly ItemElement _itemElement;

    public CorpseItemAcquisitionOutcome(ItemElement itemElement)
        : base($"Item received: {itemElement.Item.DisplayName}", OutcomeReportSeverity.Positive,
               $"harvested {itemElement.Item.WithArticle()}")
    {
        _itemElement = itemElement;
    }

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null || pov?.InSpot == null) return;
        // Shared pickup (corpse PoIs included): proper inventory placement + full-inventory handling.
        ItemPickup.Pick(scene, pov, protagonist, _itemElement, includeCorpse: true);
    }
}

/// <summary>Moves the PoV to a new area.</summary>
public sealed class AreaMoveOutcome : OutcomeReport
{
    private readonly Area _destination;

    public AreaMoveOutcome(Area destination)
        : base($"Moved to: {destination.DisplayName}", OutcomeReportSeverity.Neutral,
               $"made my way to {destination.DisplayName}")
    {
        _destination = destination;
    }

    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Movement;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
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
        : base($"Examining: {spot.DisplayName}", OutcomeReportSeverity.Neutral,
               $"went to examine {spot.DisplayName}")
    {
        _spot = spot;
    }

    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Movement;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
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
        : base("Left the spot", OutcomeReportSeverity.Neutral, "stepped back") { }

    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Movement;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
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
        : base($"Door unlocked — entered {destination.DisplayName}", OutcomeReportSeverity.Neutral,
               $"unlocked the way into {destination.DisplayName}")
    {
        _door        = door;
        _destination = destination;
    }

    // Moves AND leaves the door unlocked: the scene rebuild replay starts from re-locks it, so a
    // chain that skipped this step would assume a way through that replay does not have.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
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
        : base($"Slain: {sceneNpc.DisplayName}", OutcomeReportSeverity.Negative,
               $"killed {sceneNpc.DisplayName}")
    {
        _sceneNpc = sceneNpc;
    }

    // Removes an actor from the scene — later steps may only have been possible because of it.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
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
        : base($"Combat begins: {npc.DisplayName}", OutcomeReportSeverity.Negative,
               $"provoked {npc.DisplayName} into a fight")
    {
        _npc = npc;
    }

    // A fight is a phase a routine cannot contain, and it reshapes the scene while it runs.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
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
        : base($"Conversation: {npc.DisplayName}", OutcomeReportSeverity.Neutral,
               $"began speaking with {npc.DisplayName}")
    {
        _npc    = npc;
        _treeId = treeId;
    }

    // Deliberately None. A dialogue leaves the world in a state that persists to replay time —
    // affinity, jobs and trades are stored against the NPC's stable id — so a conversation that is
    // itself unrecordable (introducing yourself, a one-off tree) can be skipped without invalidating
    // the steps around it. Recordable dialogue verbs still terminate their own chain through
    // RoutineTriggeredPhase; that is a separate question from whether a skip is safe.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.None;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
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
        : base($"Appeasement: {npc.DisplayName} — hostile→suspicious", OutcomeReportSeverity.Positive,
               $"calmed {npc.DisplayName}")
    {
        _npc = npc;
    }

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        _npc.AffinityTable.ClearEnemy(protagonist.AffinityKey);
        _npc.AffinityTable.SetLevel(protagonist.AffinityKey, Cathedral.Game.Dialogue.Affinity.AffinityLevel.Suspicious);
    }
}

/// <summary>Internal: records an element in scene.StateChanges. No UI chip.</summary>
public sealed class StateCaptureOutcome : OutcomeReport
{
    private readonly Element _element;
    public override bool ShowInUI => false;

    public StateCaptureOutcome(Element element)
        : base(string.Empty, OutcomeReportSeverity.Neutral, verbatim: string.Empty)
    {
        _element = element;
    }

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
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
        : base(string.Empty, OutcomeReportSeverity.Neutral, verbatim: string.Empty)
    {
        _fromId       = fromId;
        _nextId       = nextId;
        _fragmentName = fragmentName;
    }

    // Leaves exploration entirely. Never reached while recording (those phases arm no recorder),
    // declared so the rule holds if that ever changes.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null) return;
        scene.PendingReminescenceTransition = new ReminescenceTransitionRequest(_fromId, _nextId, _fragmentName);
    }
}

/// <summary>
/// Internal: signals successful completion of the Get-Up phase.
/// Does not appear as a UI chip — consumed by NarrativeController on the next Continue click.
/// </summary>
public sealed class GetUpTransitionOutcome : OutcomeReport
{
    public override bool ShowInUI => false;

    public GetUpTransitionOutcome() : base(string.Empty, OutcomeReportSeverity.Positive, verbatim: string.Empty) { }

    // Leaves exploration entirely — see ReminescenceTransitionOutcome.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null) return;
        scene.PendingGetUpTransition = true;
    }
}
