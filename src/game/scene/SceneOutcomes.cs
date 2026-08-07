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

/// <summary>Harvests an item from a corpse (cut verb).</summary>
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
        if (scene == null || pov == null) return;
        // Shared pickup (corpses included): proper inventory placement + full-inventory handling.
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

/// <summary>
/// Moves the PoV to a later time of day (waiting, resting, sleeping until dawn).
///
/// <para>Time of day is PoV state on the same footing as the current area: it is what
/// <see cref="Scene.GetNpcsAt"/> — and therefore every NPC verb's <c>IsPossible</c> — gates
/// presence on, so shifting it changes which actions exist just as walking somewhere else does.
/// The controller notices the change after reports apply and re-places NPCs for the new period; the
/// headless routine replay needs nothing extra, because its verb gates read <c>pov.When</c> directly.</para>
/// </summary>
public sealed class TimeShiftOutcome : OutcomeReport
{
    private readonly TimePeriod _destination;

    public TimeShiftOutcome(TimePeriod destination)
        : base($"Time passes: {destination.Label()}", OutcomeReportSeverity.Neutral,
               $"waited until {destination.Label().ToLowerInvariant()}")
    {
        _destination = destination;
    }

    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.TimeShift;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (pov == null) return;
        pov.When  = _destination;
        pov.Focus = null;
    }
}

/// <summary>
/// A pure notice: something the character saw happen, carrying no state change of its own.
///
/// <para>Produced by the verbs that spend time waiting for the world to move — hiding until somebody
/// comes or goes — where the <i>information</i> is the whole reward and the accompanying
/// <see cref="TimeShiftOutcome"/> carries the only actual effect. Without this the player would be
/// told the hour had changed and never told why they had been waiting.</para>
/// </summary>
public sealed class NoticeOutcome : OutcomeReport
{
    public NoticeOutcome(string text, string verbatim)
        : base(text, OutcomeReportSeverity.Neutral, verbatim) { }
}

/// <summary>
/// Takes a tiny creature out of the scene — caught in a hand or crushed underfoot.
///
/// <para>Deliberately not <see cref="NpcSlaynOutcome"/>, which spawns a corpse spot in the area. A
/// beetle does not leave a body worth walking over to, and a butterfly you have caught is in your
/// hand rather than on the ground. Both cases end the same way: the creature stops being alive, so
/// <c>Scene.GetNpcsAt</c> drops it and every verb on it goes with it.</para>
/// </summary>
public sealed class TinyCreatureRemovedOutcome : OutcomeReport
{
    private readonly SceneNpc _npc;

    public TinyCreatureRemovedOutcome(SceneNpc npc, bool caught)
        : base(caught ? $"Caught: {npc.Entity.DisplayName}" : $"Crushed: {npc.Entity.DisplayName}",
               caught ? OutcomeReportSeverity.Positive : OutcomeReportSeverity.Neutral,
               caught
                   ? $"caught the {npc.Entity.DisplayName.ToLowerInvariant()}"
                   : $"crushed the {npc.Entity.DisplayName.ToLowerInvariant()}")
    {
        _npc = npc;
    }

    /// <summary>
    /// Breaking: a replayed routine rebuilds the scene from scratch, and which insects it rolls is
    /// not the set this one removed.
    /// </summary>
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (_npc.Entity is ShallowNpcEntity shallow) shallow.IsAlive = false;
        if (pov != null) pov.Focus = null;
    }
}

/// <summary>
/// Records the landmarks picked out from a high place, so <c>GoTowardVerb</c> can head for them.
///
/// <para>Knowledge, not world state: it goes on the point of view, and it is per-visit. Declares no
/// <c>RoutineChainEffect</c> because nothing about the world moved — only what the character knows
/// about it.</para>
/// </summary>
public sealed class LandmarksRevealedOutcome : OutcomeReport
{
    private readonly System.Collections.Generic.IReadOnlyList<Area> _landmarks;

    public LandmarksRevealedOutcome(System.Collections.Generic.IReadOnlyList<Area> landmarks)
        : base(Describe(landmarks), OutcomeReportSeverity.Positive, Verbalise(landmarks))
    {
        _landmarks = landmarks;
    }

    private static string Describe(System.Collections.Generic.IReadOnlyList<Area> areas)
        => areas.Count == 0
            ? "Nothing worth walking to"
            : "Landmarks noted: " + string.Join(", ", areas.Select(a => a.DisplayName));

    private static string Verbalise(System.Collections.Generic.IReadOnlyList<Area> areas)
        => areas.Count == 0
            ? "found nothing out there worth the walk"
            : "picked out " + string.Join(" and ", areas.Select(a => a.DisplayName)) + " from up here";

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (pov == null) return;
        foreach (var area in _landmarks) pov.RevealedLandmarks.Add(area.Id);
    }
}

/// <summary>
/// Swaps a point of interest for another in place — the wreck a broken thing becomes.
///
/// <para>Replacement rather than mutation, so the wreckage can carry its own name, its own prose and
/// its own salvage items without the original having to anticipate any of it. The swap happens in
/// every area holding the original, because a connector or a shared fixture can be in two.</para>
/// </summary>
public sealed class PoiReplacementOutcome : OutcomeReport
{
    private readonly PointOfInterest _original;
    private readonly PointOfInterest _replacement;

    public PoiReplacementOutcome(PointOfInterest original, PointOfInterest replacement)
        : base($"Broken: {original.DisplayName}", OutcomeReportSeverity.Neutral,
               $"broke {original.DisplayName.ToLowerInvariant()} apart")
    {
        _original    = original;
        _replacement = replacement;
    }

    /// <summary>Breaking: a rebuilt scene has the furniture whole again, so no routine may assume otherwise.</summary>
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null) return;

        foreach (var area in scene.AllAreas)
        {
            int index = area.PointsOfInterest.IndexOf(_original);
            if (index >= 0) area.PointsOfInterest[index] = _replacement;
        }

        // The wreck inherits the original's identity so its description seed, and any depletion
        // already recorded against it, stay put.
        _replacement.StableKey = _original.StableKey;
        _replacement.Register(scene);
        foreach (var item in _replacement.Items) item.Register(scene);

        if (pov != null) pov.Focus = null;
    }
}

/// <summary>
/// Marks a sleeper as woken for the rest of this visit, so every ordinary conversation opens back up.
///
/// <para>Not persisted, and deliberately so: scenes rebuild on every arrival, and somebody you got
/// out of bed last week is asleep again tonight.</para>
/// </summary>
public sealed class SleeperRousedOutcome : OutcomeReport
{
    private readonly SceneNpc _npc;

    public SleeperRousedOutcome(SceneNpc npc)
        : base($"Woken: {npc.Entity.DisplayName}", OutcomeReportSeverity.Neutral,
               $"woke {npc.Entity.DisplayName}")
    {
        _npc = npc;
    }

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov) => _npc.Roused = true;
}

/// <summary>
/// Takes an NPC out of the location and puts them in the party.
///
/// <para>The body joins as it is. An <c>NpcEntity</c> wraps an <c>EnemyCombatant</c>, which is a
/// <c>PartyMember</c> like any other, so recruitment is a list insertion — no copying of organs,
/// skills, wounds or inventory, and therefore no copy to drift out of step with the original.</para>
///
/// <para>They also leave the scene: dead to <c>GetNpcsAt</c>, which is what every verb gate and the
/// NPC placement both read, so the person you recruited is not still standing in the square. The
/// flag is not persisted, so a <i>persistent</i> NPC would reappear on the next visit — a real gap,
/// and the reason this records the departure in the location state as well.</para>
/// </summary>
public sealed class RecruitedOutcome : OutcomeReport
{
    private readonly SceneNpc _npc;

    public RecruitedOutcome(SceneNpc npc)
        : base($"Joined you: {npc.Entity.DisplayName}", OutcomeReportSeverity.Positive,
               $"took {npc.Entity.DisplayName} along with me")
    {
        _npc = npc;
    }

    /// <summary>Breaking: a rebuilt scene would put them back where they were.</summary>
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (protagonist is not Protagonist proto) return;
        if (_npc.Entity is not NpcEntity npc) return;

        // The ceiling is checked in the verb gate too, so the action is not offered when the party is
        // full. Checked again here because a companion can be picked up between the offer and the
        // roll, and quietly exceeding the cap would be worse than declining.
        int max = Verbs.TameVerb.MaxCompanions(proto);
        if (proto.CompanionParty.Count >= max) return;

        proto.CompanionParty.Add(npc.Combatant);
        npc.IsAlive = false;                      // gone from GetNpcsAt, and so from every verb gate

        if (scene != null)
        {
            scene.Npcs.Remove(_npc);
            scene.NpcSchedules.Remove(_npc.Id);
        }
        if (pov != null) pov.Focus = null;
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
    // chain that skipped this step would assume a way through that replay does not have. Both halves
    // must be declared — Breaking alone left the door out of the movement prefix, so a routine
    // emitted after passing through one replayed from the wrong side of it.
    public override RoutineChainEffect RoutineChainEffect
        => RoutineChainEffect.Movement | RoutineChainEffect.Breaking;

    public override void Apply(PartyMember protagonist, Scene? scene, PoV? pov)
    {
        if (scene == null || pov == null) return;
        _door.DoorState  = DoorState.Unlocked;
        // ForcedOpen also defeats the night rule for the rest of this visit. Without it a player who
        // forced an entry door after dark would be shut out again the moment they stepped back
        // outside, since EffectiveState re-shuts every entry door at Night.
        _door.ForcedOpen = true;
        pov.Where        = _destination;
        pov.Focus        = null;
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
        foreach (var remains in _sceneNpc.Entity.GenerateCorpse())
            scene.AddPointOfInterestToArea(pov.Where, remains);
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
