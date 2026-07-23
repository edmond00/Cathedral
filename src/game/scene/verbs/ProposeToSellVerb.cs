using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Proposes to sell a category of goods to an NPC (the NPC's <see cref="NpcEntity.BuyTag"/>).
/// On success it opens the propose-to-sell dialogue; only succeeding THAT dialogue opens the
/// sell menu. Gated to acquaintances and above — you must have met the NPC first.
/// </summary>
public class ProposeToSellVerb : Verb
{
    public override string VerbId         => "propose_to_sell";
    public override string DisplayName    => "Propose to sell";
    public override int    BaseDifficulty => 1;   // the action only meets the NPC; the dialogue carries the real stakes

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (sceneNpc.Entity is not NpcEntity npc) return false;
        if (!npc.CanSpeak || !npc.IsAlive) return false;
        if (npc.BuyTag is null) return false;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return false;

        return TradeGate.CanTrade(npc, actor);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var npc = (target as SceneNpc)?.Entity as NpcEntity;
        string goods = npc?.BuyTag?.Label() ?? "goods";
        return $"meet {NpcPronoun(target)} to propose to sell {goods}";
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc)
            return System.Array.Empty<OutcomeReport>();
        return new[] { new DialogueTriggerOutcome(npc, DialogueTreeRegistry.Instance.Get("propose_to_sell").TreeId) };
    }
}
