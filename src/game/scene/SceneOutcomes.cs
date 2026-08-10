using System;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Dialogue.Affinity;

using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Scene;

// ── Scene-specific Outcome concrete types ───────────────────────────────
// These need Scene / PoV / NPC types, so they live in the Scene namespace.

/// <summary>Picks up an item from a PoI in the scene and adds it to the inventory.</summary>
public sealed class ItemAcquisitionOutcome : Outcome
{
    private readonly ItemElement _itemElement;

    public ItemAcquisitionOutcome(ItemElement itemElement)
        : base($"Item received: {itemElement.Item.DisplayName}", OutcomeSeverity.Positive,
               $"picked up {itemElement.Item.WithArticle()}")
    {
        _itemElement = itemElement;
    }

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null || ctx.PoV == null) return;
        // Shared pickup: removes from the holding PoI, adds to inventory, and stamps depletion.
        ItemPickup.Pick(ctx.Scene, ctx.PoV, ctx.Actor!, _itemElement);
    }
}

/// <summary>Harvests an item from a corpse (cut verb).</summary>
public sealed class CorpseItemAcquisitionOutcome : Outcome
{
    private readonly ItemElement _itemElement;

    public CorpseItemAcquisitionOutcome(ItemElement itemElement)
        : base($"Item received: {itemElement.Item.DisplayName}", OutcomeSeverity.Positive,
               $"harvested {itemElement.Item.WithArticle()}")
    {
        _itemElement = itemElement;
    }

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null || ctx.PoV == null) return;
        // Shared pickup (corpses included): proper inventory placement + full-inventory handling.
        ItemPickup.Pick(ctx.Scene, ctx.PoV, ctx.Actor!, _itemElement, includeCorpse: true);
    }
}

/// <summary>Moves the PoV to a new area.</summary>
public sealed class AreaMoveOutcome : Outcome
{
    private readonly Area _destination;

    public AreaMoveOutcome(Area destination)
        : base($"Moved to: {destination.DisplayName}", OutcomeSeverity.Neutral,
               $"made my way to {destination.DisplayName}")
    {
        _destination = destination;
    }

    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Movement;

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.PoV == null) return;
        ctx.PoV.Where = _destination;
        ctx.PoV.Focus = null;
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
public sealed class TimeShiftOutcome : Outcome
{
    private readonly TimePeriod _destination;

    public TimeShiftOutcome(TimePeriod destination)
        : base($"Time passes: {destination.Label()}", OutcomeSeverity.Neutral,
               $"waited until {destination.Label().ToLowerInvariant()}")
    {
        _destination = destination;
    }

    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.TimeShift;

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.PoV == null) return;
        ctx.PoV.When  = _destination;
        ctx.PoV.Focus = null;
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
public sealed class NoticeOutcome : Outcome
{
    public NoticeOutcome(string text, string verbatim)
        : base(text, OutcomeSeverity.Neutral, verbatim) { }
}

/// <summary>
/// Takes a tiny creature out of the scene — caught in a hand or crushed underfoot.
///
/// <para>Deliberately not <see cref="NpcSlaynOutcome"/>, which spawns a corpse spot in the area. A
/// beetle does not leave a body worth walking over to, and a butterfly you have caught is in your
/// hand rather than on the ground. Both cases end the same way: the creature stops being alive, so
/// <c>Scene.GetNpcsAt</c> drops it and every verb on it goes with it.</para>
/// </summary>
public sealed class TinyCreatureRemovedOutcome : Outcome
{
    private readonly SceneNpc _npc;

    public TinyCreatureRemovedOutcome(SceneNpc npc, bool caught)
        : base(caught ? $"Caught: {npc.Entity.DisplayName}" : $"Crushed: {npc.Entity.DisplayName}",
               caught ? OutcomeSeverity.Positive : OutcomeSeverity.Neutral,
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

    public override void Apply(OutcomeContext ctx)
    {
        if (_npc.Entity is ShallowNpcEntity shallow) shallow.IsAlive = false;
        if (ctx.PoV != null) ctx.PoV.Focus = null;
    }
}


/// <summary>
/// Swaps a point of interest for another in place — the wreck a broken thing becomes.
///
/// <para>Replacement rather than mutation, so the wreckage can carry its own name, its own prose and
/// its own salvage items without the original having to anticipate any of it. The swap happens in
/// every area holding the original, because a connector or a shared fixture can be in two.</para>
/// </summary>
public sealed class PoiReplacementOutcome : Outcome
{
    private readonly PointOfInterest _original;
    private readonly PointOfInterest _replacement;

    public PoiReplacementOutcome(PointOfInterest original, PointOfInterest replacement)
        : base($"Broken: {original.DisplayName}", OutcomeSeverity.Neutral,
               $"broke {original.DisplayName.ToLowerInvariant()} apart")
    {
        _original    = original;
        _replacement = replacement;
    }

    /// <summary>Breaking: a rebuilt scene has the furniture whole again, so no routine may assume otherwise.</summary>
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null) return;

        foreach (var area in ctx.Scene.AllAreas)
        {
            int index = area.PointsOfInterest.IndexOf(_original);
            if (index >= 0) area.PointsOfInterest[index] = _replacement;
        }

        // The wreck inherits the original's identity so its description seed, and any depletion
        // already recorded against it, stay put.
        _replacement.StableKey = _original.StableKey;
        _replacement.Register(ctx.Scene);
        foreach (var item in _replacement.Items) item.Register(ctx.Scene);

        if (ctx.PoV != null) ctx.PoV.Focus = null;
    }
}

/// <summary>
/// Marks a sleeper as woken for the rest of this visit, so every ordinary conversation opens back up.
///
/// <para>Not persisted, and deliberately so: scenes rebuild on every arrival, and somebody you got
/// out of bed last week is asleep again tonight.</para>
/// </summary>
public sealed class SleeperRousedOutcome : Outcome
{
    private readonly SceneNpc _npc;

    public SleeperRousedOutcome(SceneNpc npc)
        : base($"Woken: {npc.Entity.DisplayName}", OutcomeSeverity.Neutral,
               $"woke {npc.Entity.DisplayName}")
    {
        _npc = npc;
    }

    public override void Apply(OutcomeContext ctx) => _npc.Roused = true;
}

/// <summary>
/// Takes an NPC out of the location and puts them in the party.
///
/// <para>The body joins as it is. An <c>NpcEntity</c> wraps an <c>EnemyCombatant</c>, which is a
/// <c>PartyMember</c> like any other, so recruitment is a list insertion — no copying of organs,
/// skills, wounds or inventory, and therefore no copy to drift out of step with the original.</para>
///
/// <para>They also leave the ctx.Scene: dead to <c>GetNpcsAt</c>, which is what every verb gate and the
/// NPC placement both read, so the person you recruited is not still standing in the square. The
/// flag is not persisted, so a <i>persistent</i> NPC would reappear on the next visit — a real gap,
/// and the reason this records the departure in the location state as well.</para>
/// </summary>
public sealed class RecruitedOutcome : Outcome
{
    private readonly SceneNpc _npc;

    public RecruitedOutcome(SceneNpc npc)
        : base($"Joined you: {npc.Entity.DisplayName}", OutcomeSeverity.Positive,
               $"took {npc.Entity.DisplayName} along with me")
    {
        _npc = npc;
    }

    /// <summary>Breaking: a rebuilt scene would put them back where they were.</summary>
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Actor! is not Protagonist proto) return;
        if (_npc.Entity is not NpcEntity npc) return;

        // The ceiling is checked in the verb gate too, so the action is not offered when the party is
        // full. Checked again here because a companion can be picked up between the offer and the
        // roll, and quietly exceeding the cap would be worse than declining.
        int max = Verbs.TameVerb.MaxCompanions(proto);
        if (proto.CompanionParty.Count >= max) return;

        proto.CompanionParty.Add(npc.Combatant);

        // One door for every way out of the world: gone from GetNpcsAt (and so from every verb gate),
        // out of the ctx.Scene's NPC list and schedules, and recorded as departed so the next build of
        // this location does not stand them back where they were.
        ctx.Scene?.RemoveNpcFromPlay(_npc);
        if (ctx.PoV != null) ctx.PoV.Focus = null;
    }
}

/// <summary>Unlocks a door and immediately passes through it.</summary>
public sealed class DoorUnlockOutcome : Outcome
{
    private readonly DoorPointOfInterest _door;
    private readonly Area                _destination;

    public DoorUnlockOutcome(DoorPointOfInterest door, Area destination)
        : base($"Door unlocked — entered {destination.DisplayName}", OutcomeSeverity.Neutral,
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

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null || ctx.PoV == null) return;
        _door.DoorState  = DoorState.Unlocked;
        // ForcedOpen also defeats the night rule for the rest of this visit. Without it a player who
        // forced an entry door after dark would be shut out again the moment they stepped back
        // outside, since EffectiveState re-shuts every entry door at Night.
        _door.ForcedOpen = true;
        ctx.PoV.Where        = _destination;
        ctx.PoV.Focus        = null;
        ctx.Scene.StateChanges.Capture(_door);
    }
}

/// <summary>Kills an NPC without combat and spawns a corpse.</summary>
public sealed class NpcSlaynOutcome : Outcome
{
    private readonly SceneNpc _sceneNpc;

    public NpcSlaynOutcome(SceneNpc sceneNpc)
        : base($"Slain: {sceneNpc.DisplayName}", OutcomeSeverity.Negative,
               $"killed {sceneNpc.DisplayName}")
    {
        _sceneNpc = sceneNpc;
    }

    // Removes an actor from the scene — later steps may only have been possible because of it.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null || ctx.PoV == null) return;

        // The body is made from the entity, so it survives the removal that follows.
        var remainsList = _sceneNpc.Entity.GenerateCorpse();
        ctx.Scene.RemoveNpcFromPlay(_sceneNpc);

        foreach (var remains in remainsList)
            ctx.Scene.AddPointOfInterestToArea(ctx.PoV.Where, remains);
        ctx.PoV.Focus = null;
    }
}

/// <summary>Queues a fight with a full NPC (sets scene.PendingFightRequest).</summary>
public sealed class FightTriggerOutcome : Outcome
{
    /// <summary>Who the fight is with. Read by the fight mode this outcome opens.</summary>
    public NpcEntity Target { get; }

    /// <summary>Why the fight started, shown when the fight screen opens.</summary>
    public string CombatContext { get; }

    /// <summary>True when they swung first, which gives them the opening turn.</summary>
    public bool EnemyInitiative { get; init; }

    /// <summary>Name-faked label for this narrator's point of view, if one was stamped.</summary>
    public string? ContextLabel { get; set; }

    public FightTriggerOutcome(NpcEntity npc, string combatContext = "")
        : base($"Combat begins: {npc.DisplayName}", OutcomeSeverity.Negative,
               $"provoked {npc.DisplayName} into a fight")
    {
        Target        = npc;
        CombatContext = string.IsNullOrEmpty(combatContext) ? $"combat with {npc.DisplayName}"
                                                            : combatContext;
    }

    // A fight is a phase a routine cannot contain, and it reshapes the scene while it runs.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null) return;
        ctx.Scene.PendingFightRequest = new FightRequest(Target);
    }
}

/// <summary>Queues a dialogue session with an NPC (sets scene.PendingDialogueRequest).</summary>
public sealed class DialogueTriggerOutcome : Outcome
{
    /// <summary>Who is being spoken to. Read by the dialogue mode this outcome opens.</summary>
    public NpcEntity Target { get; }

    /// <summary>Which tree to run. Null when <see cref="Tree"/> carries a built one instead.</summary>
    public string? TreeId { get; }

    /// <summary>A pre-built tree, for conversations the scene composes rather than looks up.</summary>
    public DialogueTree? Tree { get; init; }

    /// <summary>Name-faked label for this narrator's point of view, if one was stamped.</summary>
    public string? ContextLabel { get; set; }

    public DialogueTriggerOutcome(NpcEntity npc, string? treeId = null, DialogueTree? tree = null)
        : base($"Conversation: {npc.DisplayName}", OutcomeSeverity.Neutral,
               $"began speaking with {npc.DisplayName}")
    {
        Target = npc;
        TreeId = treeId;
        Tree   = tree;
    }

    // Deliberately None. A dialogue leaves the world in a state that persists to replay time —
    // affinity, jobs and trades are stored against the NPC's stable id — so a conversation that is
    // itself unrecordable (introducing yourself, a one-off tree) can be skipped without invalidating
    // the steps around it. Recordable dialogue verbs still terminate their own chain through
    // RoutineTriggeredPhase; that is a separate question from whether a skip is safe.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.None;

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null) return;
        ctx.Scene.PendingDialogueRequest = new DialogueRequest(Target, TreeId);
    }
}

/// <summary>Changes affinity toward the protagonist after appeasement.</summary>
public sealed class AffinityChangeOutcome : Outcome
{
    private readonly NpcEntity Target;

    public AffinityChangeOutcome(NpcEntity npc)
        : base($"Appeasement: {npc.DisplayName} — hostile→suspicious", OutcomeSeverity.Positive,
               $"calmed {npc.DisplayName}")
    {
        Target = npc;
    }

    public override void Apply(OutcomeContext ctx)
    {
        Target.AffinityTable.ClearEnemy(ctx.Actor!.AffinityKey);
        Target.AffinityTable.SetLevel(ctx.Actor!.AffinityKey, Cathedral.Game.Dialogue.Affinity.AffinityLevel.Suspicious);
    }
}

/// <summary>Internal: records an element in scene.StateChanges. No UI chip.</summary>
public sealed class StateCaptureOutcome : Outcome
{
    private readonly Element _element;
    public override bool ShowInUI => false;

    public StateCaptureOutcome(Element element)
        : base(string.Empty, OutcomeSeverity.Neutral, verbatim: string.Empty)
    {
        _element = element;
    }

    public override void Apply(OutcomeContext ctx)
        => ctx.Scene?.StateChanges.Capture(_element);
}

/// <summary>
/// Internal: queues a reminescence phase transition.
/// Does not appear as a UI chip — phase management is handled by NarrativeController.
/// </summary>
public sealed class ReminescenceTransitionOutcome : Outcome
{
    private readonly string _fromId;
    private readonly string _nextId;
    private readonly string _fragmentName;

    public override bool ShowInUI => false;

    public ReminescenceTransitionOutcome(string fromId, string nextId, string fragmentName)
        : base(string.Empty, OutcomeSeverity.Neutral, verbatim: string.Empty)
    {
        _fromId       = fromId;
        _nextId       = nextId;
        _fragmentName = fragmentName;
    }

    // Leaves exploration entirely. Never reached while recording (those phases arm no recorder),
    // declared so the rule holds if that ever changes.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null) return;
        ctx.Scene.PendingReminescenceTransition = new ReminescenceTransitionRequest(_fromId, _nextId, _fragmentName);
    }
}

/// <summary>
/// Internal: signals successful completion of the Get-Up phase.
/// Does not appear as a UI chip — consumed by NarrativeController on the next Continue click.
/// </summary>
public sealed class GetUpTransitionOutcome : Outcome
{
    public override bool ShowInUI => false;

    public GetUpTransitionOutcome() : base(string.Empty, OutcomeSeverity.Positive, verbatim: string.Empty) { }

    // Leaves exploration entirely — see ReminescenceTransitionOutcome.
    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.Breaking;

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null) return;
        ctx.Scene.PendingGetUpTransition = true;
    }
}
