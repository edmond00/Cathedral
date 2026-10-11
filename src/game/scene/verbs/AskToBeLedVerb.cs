using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Asks a trader met away from their premises to take the player there — the smith met in the street
/// asked to lead the way to the forge. Opens <c>ask_to_be_led</c>; succeeding it walks the player to
/// the premises at the first hour the trader keeps them, with the trader in focus
/// (<see cref="TradeGate.NextOnPremises"/>, <c>NarrativeController.WalkToPremises</c>).
///
/// <para>Trade happens only on premises, so a trader met anywhere else could be talked to and not
/// dealt with; this is the bridge. Offered only where it means something: on somebody who trades,
/// whose day brings them to their premises at some hour, and who is not standing on them now.</para>
///
/// <para>Behind the introduction, as asking a favour of somebody is: the player must already know
/// them. Asking to buy needs no introduction, but asking to be walked across town does.</para>
/// </summary>
public class AskToBeLedVerb : SocialDialogueVerb
{
    public override string VerbId      => "ask_to_be_led";
    public override string DisplayName => "Ask to be led";

    protected override string DialogueTreeId => "ask_to_be_led";

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
    {
        if (Available(scene, pov, target, actor) is not { } npc) return null;
        if (!npc.Trades || npc.TradesIn(pov.Where)) return null;
        if (npc.AffinityTable.IsEnemy(actor?.AffinityKey ?? "Protagonist")) return null;
        return TradeGate.NextOnPremises(scene, (SceneNpc)target, pov.When) != null ? npc : null;
    }

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var npc = (target as SceneNpc)?.Entity as NpcEntity;
        return $"ask {NpcPronoun(target)} to take me to {npc?.Archetype.Workplace ?? "where they trade"}";
    }

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "ask_to_be_led") };
    }
}
