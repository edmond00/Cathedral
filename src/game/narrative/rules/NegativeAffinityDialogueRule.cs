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
    /// <summary>
    /// The verbs a hostile NPC will still entertain. An allowlist rather than a blocklist, because
    /// the blocklist it replaces had to be extended by hand for every new conversation and silently
    /// let the unlisted ones through — an enemy would happily be begged from or asked to introduce
    /// you to their master.
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<string> ToleratedByEnemies =
        new() { "reconcile", "appease" };

    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        var vo = ctx.Action.PreselectedOutcome;
        // Applies to any conversation, plus `appease`, which is not one but is the other way out of
        // hostility.
        bool speaking = vo.Verb is Cathedral.Game.Scene.Verbs.DialogueVerb
                     || vo.Verb.VerbId == "appease";
        if (!speaking) return ActionRuleResult.Pass();
        if (vo.Target is not SceneNpc sceneNpc)                  return ActionRuleResult.Pass();
        if (sceneNpc.Entity is not NpcEntity npc)                return ActionRuleResult.Pass();

        var verbId = vo.Verb.VerbId;

        // ── Enemy: only reconcile / appease are allowed ───────────────────────
        if (npc.AffinityTable.IsEnemy(ctx.Actor.AffinityKey))
        {
            if (ToleratedByEnemies.Contains(verbId)) return ActionRuleResult.Pass();
            return ActionRuleResult.Fail(
                $"{npc.DisplayName} is hostile and will not listen to you.");
        }

        // ── AnnoyingAcquaintance: only reconcile is allowed ───────────────────
        var affinity = npc.AffinityTable.GetLevel(ctx.Actor.AffinityKey);
        if (affinity == AffinityLevel.AnnoyingAcquaintance)
        {
            if (verbId == "reconcile") return ActionRuleResult.Pass();
            return ActionRuleResult.Fail(
                $"{npc.DisplayName} finds you insufferable and refuses to engage with you.");
        }

        return ActionRuleResult.Pass();
    }
}
