using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Proposes to buy a category of goods from an NPC merchant (the NPC's <see cref="NpcEntity.SellTag"/>).
/// On success it opens the propose-to-buy dialogue; only succeeding THAT dialogue opens the
/// buy menu. Gated to acquaintances and above — you must have met the NPC first.
/// </summary>
public class ProposeToBuyVerb : DialogueVerb
{
    public override string VerbId            => "propose_to_buy";
    public override string DisplayName       => "Propose to buy";
    public override int    BaseDifficulty    => 1;   // the action only meets the NPC; the dialogue carries the real stakes
    protected override string DialogueTreeId => "propose_to_buy";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (SleeperGate.IsAsleep(scene, pov, target)) return false;  // wake them first
        if (sceneNpc.Entity is not NpcEntity npc) return false;
        if (!npc.CanSpeak || !npc.IsAlive) return false;
        if (npc.SellTag is null) return false;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return false;

        return TradeGate.CanTrade(npc, actor);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var npc = (target as SceneNpc)?.Entity as NpcEntity;
        string goods = npc?.SellTag?.Label() ?? "goods";
        return $"meet {NpcPronoun(target)} to propose to buy {goods}";
    }

    // Read out of context in the routines menu, so the pronoun is replaced by the name.
    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
    {
        var npc = (target as SceneNpc)?.Entity as NpcEntity;
        string goods = npc?.SellTag?.Label() ?? "goods";
        return $"meet {NpcName(target)} to buy {goods}";
    }

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc)
            return System.Array.Empty<Outcome>();
        return new[] { new DialogueTriggerOutcome(npc, DialogueTreeRegistry.Instance.Get("propose_to_buy").TreeId) };
    }
}
