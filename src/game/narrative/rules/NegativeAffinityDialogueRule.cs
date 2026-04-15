using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks speaking verbs when the target NPC is hostile (enemy) or finds the protagonist
/// insufferable (AnnoyingAcquaintance affinity).
///
/// Enemy NPCs will only accept <c>reconcile</c> or <c>appease</c> attempts.
/// AnnoyingAcquaintance NPCs will only accept <c>reconcile</c>.
/// All other speaking verbs are blocked until the relationship is repaired.
/// </summary>
public class NegativeAffinityDialogueRule : IActionRule
{
    private static readonly System.Collections.Generic.HashSet<string> SpeakingVerbIds =
        new() { "meet_stranger", "strengthen_relationship", "reconcile", "appease" };

    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        if (ctx.Action.PreselectedOutcome is not VerbOutcome vo) return ActionRuleResult.Pass();
        if (!SpeakingVerbIds.Contains(vo.VerbView.Verb.VerbId))  return ActionRuleResult.Pass();
        if (vo.Target is not SceneNpc sceneNpc)                   return ActionRuleResult.Pass();
        if (sceneNpc.Entity is not NpcEntity npc)                 return ActionRuleResult.Pass();

        var verbId = vo.VerbView.Verb.VerbId;

        // ── Enemy: only reconcile / appease are allowed ───────────────────────
        if (npc.AffinityTable.IsEnemy(ctx.Protagonist.DisplayName))
        {
            if (verbId is "reconcile" or "appease") return ActionRuleResult.Pass();
            return ActionRuleResult.Fail(
                $"{npc.DisplayName} is hostile and will not listen to you.");
        }

        // ── AnnoyingAcquaintance: only reconcile is allowed ───────────────────
        var affinity = npc.AffinityTable.GetLevel(ctx.Protagonist.DisplayName);
        if (affinity == AffinityLevel.AnnoyingAcquaintance)
        {
            if (verbId == "reconcile") return ActionRuleResult.Pass();
            return ActionRuleResult.Fail(
                $"{npc.DisplayName} finds you insufferable and refuses to engage with you.");
        }

        return ActionRuleResult.Pass();
    }
}
