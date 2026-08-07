using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Asks somebody to take you to somebody else.
///
/// <para>The way past the two things that make an important person hard to reach: not knowing where
/// they are, and being nobody to them. Succeeding does both — you are moved to wherever they are and
/// you arrive already introduced, which is the whole social function of an introduction.</para>
///
/// <para>Who can introduce whom is a standing relationship, not a proximity: an apprentice presents
/// you to their own master, a labourer to the reeve who sets their work. See
/// <c>NamedNpcArchetype.CanIntroduceToArchetypes</c>. That means finding the right go-between is
/// itself part of the problem, which is the point.</para>
///
/// <para>Expands into one action per eligible third party present in the location — the
/// <c>RequestJobVerb</c> pattern — carrying the target in <c>VerbView.Variant</c>.</para>
/// </summary>
public class IntroduceMeVerb : DialogueVerb
{
    public override string VerbId         => "introduce_me";
    public override string DisplayName    => "Ask for an Introduction";
    public override int    BaseDifficulty => 1;   // the action only opens the conversation

    protected override string DialogueTreeId => "introduce_me";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Candidates(scene, pov, target, actor).Count > 0;

    public override IEnumerable<VerbView> ExpandViews(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        foreach (var third in Candidates(scene, pov, target, actor))
            yield return new VerbView(this,
                $"ask {NpcPronoun(target)} to present me to the {third.Archetype.RoleNoun}",
                target, variant: third);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} to present me to someone";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbView? view = null)
        => view?.Variant is NpcEntity third
            ? $"ask {NpcName(target)} to present me to {third.DisplayName}"
            : $"ask {NpcName(target)} for an introduction";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target, VerbView view)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity speaker) return System.Array.Empty<OutcomeReport>();
        if (view.Variant is not NpcEntity third) return System.Array.Empty<OutcomeReport>();

        // Hand the subject to the dialogue adapter, which builds the {third:*} context from it.
        speaker.PendingIntroductionTarget = third;
        return new OutcomeReport[] { new DialogueTriggerOutcome(speaker, "introduce_me") };
    }

    /// <summary>
    /// The people this speaker could present the player to: someone whose archetype they have
    /// standing with, who is somewhere in this location today, alive, and not already known to the
    /// player. Asking to be introduced to somebody you have already met is not an action worth
    /// offering.
    /// </summary>
    private static List<NpcEntity> Candidates(Scene scene, PoV pov, Element target, PartyMember? actor)
    {
        var empty = new List<NpcEntity>();

        if (target is not SceneNpc sceneNpc) return empty;
        if (SleeperGate.IsAsleep(scene, pov, target)) return empty;
        if (sceneNpc.Entity is not NpcEntity speaker || !speaker.IsAlive || !speaker.CanSpeak) return empty;
        if (speaker.Archetype is not NamedNpcArchetype named) return empty;
        if (named.CanIntroduceToArchetypes.Count == 0) return empty;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Any(n => n.Id == sceneNpc.Id)) return empty;

        string key = actor?.AffinityKey ?? "Protagonist";

        return scene.Npcs
            .Where(n => n.IsAlive && n.Id != sceneNpc.Id)
            .Select(n => n.Entity)
            .OfType<NpcEntity>()
            .Where(third => named.CanIntroduceToArchetypes.Contains(third.Archetype.ArchetypeId)
                         && third.AffinityTable.IsStranger(key))
            .Distinct()
            .ToList();
    }

    public override string? RoutineVariantKey(VerbView view) => (view.Variant as NpcEntity)?.NpcId;
}
