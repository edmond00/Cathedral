using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks speaking verbs (Meet / Talk) when the target NPC regards the protagonist
/// as an annoying acquaintance — the lowest non-stranger affinity tier.
/// An NPC who finds you insufferable will not engage in conversation.
/// </summary>
public class NegativeAffinityDialogueRule : IActionRule
{
    private static readonly System.Collections.Generic.HashSet<string> SpeakingVerbIds =
        new() { "meet_stranger", "strengthen_relationship" };

    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        if (ctx.Action.PreselectedOutcome is not VerbOutcome vo) return ActionRuleResult.Pass();
        if (!SpeakingVerbIds.Contains(vo.VerbView.Verb.VerbId))  return ActionRuleResult.Pass();
        if (vo.Target is not SceneNpc sceneNpc)                   return ActionRuleResult.Pass();
        if (sceneNpc.Entity is not NpcEntity npc)                 return ActionRuleResult.Pass();

        var affinity = npc.AffinityTable.GetLevel(ctx.Protagonist.DisplayName);
        if (affinity != AffinityLevel.AnnoyingAcquaintance)      return ActionRuleResult.Pass();

        return ActionRuleResult.Fail(
            $"{npc.DisplayName} finds you insufferable and refuses to engage with you.");
    }
}
