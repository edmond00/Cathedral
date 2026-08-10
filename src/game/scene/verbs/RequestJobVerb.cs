using System.Collections.Generic;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Work;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Asks a master or reeve for work. Like GRAB across items, this expands into one action per job the
/// NPC offers — "meet them to request to work as a bellows-hand", "…as a quench-hand", etc. Each NPC
/// deterministically samples ~3 jobs from its archetype's pool (stable per NPC id). On success it
/// opens the request-job dialogue; only succeeding THAT dialogue opens the work menu — the action
/// itself is just the meeting, hence base difficulty 1. Gated to acquaintances and above — you must
/// have met the NPC first.
/// </summary>
public class RequestJobVerb : DialogueVerb
{
    private const int JobsOffered = 3;

    public override string VerbId            => "request_job";
    public override string DisplayName       => "Request job";
    public override int    BaseDifficulty    => 1;   // the action only meets the NPC; the dialogue carries the real stakes
    protected override string DialogueTreeId => "request_job";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Eligible(scene, pov, target, actor) is not null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"meet {NpcPronoun(target)} to ask for work";

    // Read out of context in the routines menu: name the NPC, and name the job that was actually
    // requested (the view's variant) rather than the generic "for work".
    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => view?.Variant is Job job
            ? $"meet {NpcName(target)} to ask to work as {job.WithArticle()}"
            : $"meet {NpcName(target)} to ask for work";

    public override IEnumerable<VerbAction> ExpandViews(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        var npc = Eligible(scene, pov, target, actor);
        if (npc is null) yield break;

        foreach (var job in JobRegistry.Instance.SampleJobs(npc.NpcId, npc.Archetype.ArchetypeId, JobsOffered))
            yield return new VerbAction(this, $"meet {NpcPronoun(target)} to request to work as {job.WithArticle()}", target, variant: job);
    }

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target, VerbAction view)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc || view.Variant is not Job job)
            return System.Array.Empty<Outcome>();

        npc.PendingJobOffer = job;   // the dialogue's terminal outcome promotes this to JobRequest
        return new[] { new DialogueTriggerOutcome(npc, DialogueTreeRegistry.Instance.Get("request_job").TreeId) };
    }

    // Routine recording captures which of the offered jobs was requested, so a replayed routine
    // reopens the work menu for the same job.
    public override string? RoutineVariantKey(VerbAction view) => (view.Variant as Job)?.Id;

    public override object? ResolveRoutineVariant(string variantKey) => JobRegistry.Instance.GetById(variantKey);

    /// <summary>Returns the eligible job-giving NPC at the target, or null when work cannot be requested.</summary>
    private static NpcEntity? Eligible(Scene scene, PoV pov, Element target, PartyMember? actor)
    {
        if (target is not SceneNpc sceneNpc) return null;
        if (SleeperGate.IsAsleep(scene, pov, target)) return null;   // nobody hires in their sleep
        if (sceneNpc.Entity is not NpcEntity npc) return null;
        if (!npc.CanSpeak || !npc.IsAlive) return null;
        if (!JobRegistry.Instance.HasJobs(npc.Archetype.ArchetypeId)) return null;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return null;
        return TradeGate.CanTrade(npc, actor) ? npc : null;
    }
}
