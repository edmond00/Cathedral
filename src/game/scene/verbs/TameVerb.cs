using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Takes a beast that has already been gentled down and makes it yours.
///
/// <para>Second half of a two-step. <c>appease</c> gets a hostile animal to stop being hostile; this
/// is what that is <i>for</i>. Requiring the appeased state rather than checking hostility directly
/// means an animal has to be worked on twice, at two separate rolls, which is about right for
/// something that then travels with you.</para>
///
/// <para>Beasts only. A person who joins you does so through a conversation
/// (<c>propose_to_join</c>), not by being handled.</para>
/// </summary>
public class TameVerb : Verb
{
    public override string VerbId         => "tame";
    public override string DisplayName    => "Tame";
    public override int    BaseDifficulty => 4;

    /// <summary>Handling an animal into accepting a handler. No hands, no verb.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    /// <summary>Working with an animal at close quarters is worth doing under threat.</summary>
    public override bool CanBeUsedUnderThreat => true;

    /// <summary>What a success teaches: handling an animal until it accepts being handled.</summary>
    public override string? GrantedModusMentisId(Element? target) => "husbandry";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (sceneNpc.Entity is not NpcEntity npc || !npc.IsAlive) return false;

        // Beasts only, and only ones already talked down. A creature still set on killing you is not
        // a candidate for coming along.
        if (npc.CanSpeak) return false;
        if (!npc.AffinityTable.IsAppeased(actor?.AffinityKey ?? "Protagonist")) return false;

        // The roster has a ceiling, and it is far better to not offer the action than to run the
        // whole thing and then refuse the animal at the door. The roster is the protagonist's — a
        // companion taming on their behalf is checked against the same one.
        if (actor is Protagonist proto && proto.CompanionParty.Count >= MaxCompanions(proto)) return false;

        return scene.GetNpcsAt(pov.Where, pov.When).Any(n => n.Id == sceneNpc.Id);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"win {NpcPronoun(target)} over and keep {NpcPronoun(target)} with me";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbView? view = null)
        => $"tame {NpcName(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => target is SceneNpc sceneNpc && sceneNpc.Entity is NpcEntity
            ? new OutcomeReport[] { new RecruitedOutcome(sceneNpc) }
            : System.Array.Empty<OutcomeReport>();

    /// <summary>A half-tamed animal that changes its mind is at your throat, not across the clearing.</summary>
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, null, new CutWound(), new ContusionWound(),
    };

    /// <summary>How many companions this character can keep — the heart-derived <c>max_companions</c>.</summary>
    internal static int MaxCompanions(PartyMember actor)
        => actor.DerivedStats.FirstOrDefault(s => s.Name == "max_companions")?.GetValue(actor) ?? 0;

    // Not recordable: the animal is gone from the scene afterwards, so a replay has nothing to tame.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
