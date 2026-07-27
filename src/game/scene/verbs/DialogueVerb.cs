using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Base for the verbs that trigger a dialogue tree (meet, talk, propose-to-buy/sell, request-job, …).
/// Centralises the routine-recording hooks: whether such a verb is recordable — and what happens when
/// its routine is replayed — is driven entirely by the associated tree's
/// <see cref="DialogueTree.RoutineBehavior"/>:
/// <list type="bullet">
/// <item><b>Interrupt</b> — not recordable; recording stops before the dialogue (the base default).</item>
/// <item><b>IncludeTrigger / IncludeSuccess</b> — recordable; the step triggers the
///   <see cref="RoutinePhaseKind.Dialogue"/> phase, and <c>RoutineReplayEngine</c> decides at replay
///   time whether to reopen the dialogue or jump straight to its baked-in success phase.</item>
/// </list>
/// Concrete verbs keep their own <c>IsPossible</c> / <c>Verbatim</c> / <c>SuccessReports</c> and only
/// supply <see cref="DialogueTreeId"/>.
/// </summary>
public abstract class DialogueVerb : Verb
{
    /// <summary>The id of the dialogue tree this verb triggers (e.g. "meet_stranger").</summary>
    protected abstract string DialogueTreeId { get; }

    /// <summary>The routine behavior declared by the triggered tree.</summary>
    private DialogueRoutineBehavior Behavior =>
        DialogueTreeRegistry.Instance.Get(DialogueTreeId).RoutineBehavior;

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => Behavior != DialogueRoutineBehavior.Interrupt && ResolveNpc(target) != null;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
    {
        var npc = ResolveNpc(target);
        return npc == null
            ? null
            : new RoutineTargetRef(RoutineTargetKind.Npc, npc.DisplayName, npc.DisplayName);
    }

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => Behavior == DialogueRoutineBehavior.Interrupt
            ? RoutinePhaseKind.None
            : RoutinePhaseKind.Dialogue;

    /// <summary>The live, speakable NPC behind a target, or null when the target is not one.</summary>
    private static NpcEntity? ResolveNpc(Element target)
        => target is SceneNpc sceneNpc && sceneNpc.Entity is NpcEntity npc && npc.IsAlive ? npc : null;
}
