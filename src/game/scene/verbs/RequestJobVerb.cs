using System.Collections.Generic;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Work;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Asks a master or reeve for work. Like GRAB across items, this expands into one action per job the
/// NPC offers — "request to work as a bellows-hand", "…as a quench-hand", etc. Each NPC deterministically
/// samples ~3 jobs from its archetype's pool (stable per NPC id). On success it opens the request-job
/// dialogue; only succeeding THAT dialogue opens the work menu. Requesting work from a master is harder
/// than from a reeve. Gated to acquaintances and above — you must have met the NPC first.
/// </summary>
public class RequestJobVerb : Verb
{
    private const int JobsOffered = 3;

    public override string VerbId         => "request_job";
    public override string DisplayName    => "Request job";
    public override int    BaseDifficulty => 3;   // reeve baseline

    /// <summary>Requesting work from a village master is harder than from a reeve/farmer/hayward.</summary>
    public override int DifficultyFor(Element? target)
    {
        var npc = (target as SceneNpc)?.Entity as NpcEntity;
        return npc?.Archetype is CraftsmanArchetype ? 5 : BaseDifficulty;
    }

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
        => Eligible(scene, pov, target, actor) is not null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {target.DisplayName} for work";

    public override IEnumerable<VerbView> ExpandViews(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        var npc = Eligible(scene, pov, target, actor);
        if (npc is null) yield break;

        foreach (var job in JobRegistry.Instance.SampleJobs(npc.NpcId, npc.Archetype.ArchetypeId, JobsOffered))
            yield return new VerbView(this, $"request to work as {job.WithArticle()}", target, variant: job);
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target, VerbView view)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc || view.Variant is not Job job)
            return System.Array.Empty<OutcomeReport>();

        npc.PendingJobOffer = job;   // the dialogue's terminal outcome promotes this to JobRequest
        return new[] { new DialogueTriggerOutcome(npc, DialogueTreeRegistry.Instance.Get("request_job").TreeId) };
    }

    /// <summary>Returns the eligible job-giving NPC at the target, or null when work cannot be requested.</summary>
    private static NpcEntity? Eligible(Scene scene, PoV pov, Element target, Protagonist? actor)
    {
        if (target is not SceneNpc sceneNpc) return null;
        if (sceneNpc.Entity is not NpcEntity npc) return null;
        if (!npc.CanSpeak || !npc.IsAlive) return null;
        if (!JobRegistry.Instance.HasJobs(npc.Archetype.ArchetypeId)) return null;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return null;
        return TradeGate.CanTrade(npc, actor) ? npc : null;
    }
}
