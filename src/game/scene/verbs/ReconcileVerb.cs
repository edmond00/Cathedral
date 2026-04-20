using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Attempts to reconcile with a hostile or insufferable NPC through dialogue.
/// Available when the NPC is an enemy of the protagonist (enemy affinity flag)
/// or regards the protagonist as an AnnoyingAcquaintance.
///
/// On execution: opens the reconcile dialogue tree.
/// Success: clears enemy flag + sets Suspicious affinity.
/// Failure: stays enemy (and may demand a fight if IsBrave).
/// </summary>
public class ReconcileVerb : Verb
{
    public override string VerbId         => "reconcile";
    public override string DisplayName    => "Reconcile";
    public override int    BaseDifficulty => 3;

    /// <summary>Reconciliation is a legal, non-violent action.</summary>
    public override bool IsLegal => true;

    /// <summary>Can be attempted even when the enemy is right there.</summary>
    public override bool CanBeUsedUnderThreat => true;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (sceneNpc.Entity is not NpcEntity npc) return false;
        if (!npc.CanSpeak) return false;
        if (!npc.IsAlive) return false;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return false;

        var protagonistId = actor?.DisplayName ?? "Protagonist";
        var isEnemy   = npc.AffinityTable.IsEnemy(protagonistId);
        var affinity  = npc.AffinityTable.GetLevel(protagonistId);
        var isAnnoying = affinity == AffinityLevel.AnnoyingAcquaintance;

        return isEnemy || isAnnoying;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"try to reconcile with {target.DisplayName}";

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc) return;
        scene.PendingDialogueRequest = new DialogueRequest(npc, DialogueTreeRegistry.Instance.Get("reconcile").TreeId);
    }
}
