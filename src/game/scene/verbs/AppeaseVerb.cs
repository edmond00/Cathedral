using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Attempts to appease a hostile creature or NPC through non-verbal means
/// (offerings, gestures, body language). Does not require the NPC to be able to speak.
///
/// On success (dice roll): clears enemy flag + sets Suspicious affinity.
/// On failure: the under-threat failure pipeline applies (LLM decides if fight triggers).
/// </summary>
public class AppeaseVerb : Verb
{
    public override string VerbId         => "appease";
    public override string DisplayName    => "Appease";
    public override int    BaseDifficulty => 3;

    /// <summary>Appeasing is a legal, non-violent action.</summary>
    public override bool IsLegal => true;

    /// <summary>Can be attempted even when the enemy is right there.</summary>
    public override bool CanBeUsedUnderThreat => true;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (sceneNpc.Entity is not NpcEntity npc) return false;
        if (!npc.IsAlive) return false;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return false;

        var protagonistId = actor?.AffinityKey ?? "Protagonist";
        var isEnemy   = npc.AffinityTable.IsEnemy(protagonistId);
        var affinity  = npc.AffinityTable.GetLevel(protagonistId);
        var isNegative = affinity == AffinityLevel.AnnoyingAcquaintance;

        return isEnemy || isNegative;
    }

    // Not a dialogue trigger (non-verbal, resolved by the dice roll directly), so no "meet" lead;
    // the NPC is named once in the prompt's attention line and referred back to by pronoun.
    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"try to appease {NpcPronoun(target)}";

    // Read out of context in the routines menu, so the pronoun is replaced by the name.
    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbView? view = null)
        => $"try to appease {NpcName(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc)
            return System.Array.Empty<OutcomeReport>();
        return new[] { new AffinityChangeOutcome(npc) };
    }
}
