using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Shared gate for the conversations added on top of the original six: the target must be a live,
/// speaking, awake person who is standing here now — and, unless the verb opts out, somebody the
/// actor has actually met.
///
/// <para><b>The introduction is the gate.</b> <c>meet_stranger</c> is what turns a stranger into a
/// distant acquaintance, and every other conversation is meant to sit behind it — which is already
/// how trade (<see cref="TradeGate"/>), work and <c>strengthen_relationship</c> read. These four
/// were the exception: walking up to a total stranger and asking what they know was offered before
/// asking their name, so the introduction had no reason to exist. Opting out is a per-verb decision
/// (see <see cref="RequiresAcquaintance"/>), not a forgotten check.</para>
/// </summary>
public abstract class SocialDialogueVerb : DialogueVerb
{
    public override int BaseDifficulty => 1;   // the action only opens the conversation

    /// <summary>
    /// Whether the target must be someone the actor has already met. True for anything that asks
    /// something of a person; the two verbs whose whole subject is a stranger override it.
    /// </summary>
    protected virtual bool RequiresAcquaintance => true;

    /// <summary>The live, speaking, awake person behind a target who is present now, or null.</summary>
    protected NpcEntity? Available(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return null;
        if (SleeperGate.IsAsleep(scene, pov, target)) return null;
        if (sceneNpc.Entity is not NpcEntity npc) return null;
        if (!npc.IsAlive || !npc.CanSpeak) return null;
        if (RequiresAcquaintance && npc.AffinityTable.IsStranger(actor?.AffinityKey ?? "Protagonist"))
            return null;

        return scene.GetNpcsAt(pov.Where, pov.When).Any(n => n.Id == sceneNpc.Id) ? npc : null;
    }
}

/// <summary>
/// Asks a stranger for money. The lowest thing a conversation can be, and the only one available to
/// somebody with nothing.
/// </summary>
public class BegForCoinVerb : SocialDialogueVerb
{
    public override string VerbId      => "beg_for_coin";
    public override string DisplayName => "Beg";

    protected override string DialogueTreeId => "beg_for_coin";

    /// <summary>
    /// The one conversation that is <i>about</i> being nobody to the person you are addressing.
    /// Gating it behind an introduction would leave a destitute player with nothing to say to anyone.
    /// </summary>
    protected override bool RequiresAcquaintance => false;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Available(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} for a coin";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"beg a coin from {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Available(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "beg_for_coin") };
    }
}

/// <summary>
/// Goads somebody into swinging first.
///
/// <para>The fight it starts is with that person alone — no allies. That is what makes it different
/// from simply attacking: picking a fight in a village square normally brings every brave soul in
/// the section down on you, and a provocation is a way to get one person on their own.</para>
/// </summary>
public class ProvokeVerb : SocialDialogueVerb
{
    public override string VerbId      => "provoke";
    public override string DisplayName => "Provoke";

    protected override string DialogueTreeId => "provoke";

    /// <summary>
    /// Insulting somebody does not require having been introduced to them, and this is the only way
    /// to pick a fight with one person instead of the whole square — behind an introduction it would
    /// be unreachable against exactly the people worth using it on.
    /// </summary>
    protected override bool RequiresAcquaintance => false;

    // Left legal deliberately: words, not blows. What it leads to is the NPC's decision, at least
    // formally, and a witness to a provocation has watched an argument.

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Available(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"say something to {NpcPronoun(target)} that cannot be let pass";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"provoke {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Available(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "provoke") };
    }
}

/// <summary>
/// Asks somebody to come with you.
///
/// <para>Gated on a real relationship — close acquaintance or better — because it is the largest
/// thing you can ask of a person, and on room in the party, because being told yes and then having
/// nowhere to put them would be the worst reading of a success.</para>
/// </summary>
public class ProposeToJoinVerb : SocialDialogueVerb
{
    public override string VerbId      => "propose_to_join";
    public override string DisplayName => "Ask to Join You";

    protected override string DialogueTreeId => "propose_to_join";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        var npc = Available(scene, pov, target, actor);
        if (npc == null) return false;

        string key = actor?.AffinityKey ?? "Protagonist";
        if (npc.AffinityTable.IsEnemy(key)) return false;
        if (npc.AffinityTable.GetLevel(key) < AffinityLevel.CloseAcquaintance) return false;

        // The roster belongs to the protagonist: a companion asking someone to come along is asking
        // on their behalf, and there is nobody else to count against. No actor at all (content
        // tooling) leaves the ceiling untested.
        if (actor is not Protagonist proto) return actor == null;
        return proto.CompanionParty.Count < TameVerb.MaxCompanions(proto);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} to come away with me";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"ask {NpcName(target)} to travel with me";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Available(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "propose_to_join") };
    }
}

/// <summary>
/// Asks somebody what they know.
///
/// <para>Available to anybody you have been introduced to, because the interesting part is not who
/// you may ask but what they turn out to know — a shepherd on the lie of the land is worth more than
/// a reeve on it, and finding that out is the game. Not to a stranger, though: asking a person you
/// have not so much as named yourself to what they know about the district was offered ahead of
/// meeting them, which made <c>meet_stranger</c> a step with nothing behind it.</para>
/// </summary>
public class GatherKnowledgeVerb : SocialDialogueVerb
{
    public override string VerbId      => "gather_knowledge";
    public override string DisplayName => "Ask What They Know";

    protected override string DialogueTreeId => "gather_knowledge";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Available(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} what {NpcSubjectPronoun(target)} knows";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"ask {NpcName(target)} what they know";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Available(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "gather_knowledge") };
    }
}

/// <summary>
/// Takes what is in somebody's pockets while they are standing there.
///
/// <para>Not a conversation — it is <c>steal</c> aimed at a person instead of a shelf — but it lives
/// beside the social verbs because its whole difficulty is doing it while being looked at. Easier
/// against a sleeper, which is the one case where the target is not looking.</para>
/// </summary>
public class PickpocketVerb : Verb
{


    public override string VerbId         => "pickpocket";
    public override string DisplayName    => "Pickpocket";
    public override int    BaseDifficulty => 5;

    /// <summary>Fingers in someone's purse. No hands, no verb.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    /// <summary>Taking from somebody's pockets is a crime wherever they are standing.</summary>
    protected override bool IsIllegalFor(Scene scene, PoV pov, Element? target, PartyMember? actor) => true;

    /// <summary>What a success teaches: taking from a person without them knowing.</summary>
    /// <summary>
    /// What lifting a purse teaches. Not <c>petty_thief</c>, which STEAL keeps: taking from a shelf
    /// and taking from a body that is still wearing it are different trades, and only one of them is
    /// mostly about the crowd.
    /// </summary>
    public override string? GrantedModusMentisId(Element? target) => "cutpurse";

    /// <summary>
    /// A sleeper's pockets are a great deal easier than a waking man's — difficulty 2 against 5.
    /// The one case where the target is not looking at you is the one case this is nearly free.
    /// </summary>
    public override int DifficultyFor(Element? target)
        => target is SleepingNpcPointOfInterest ? 2 : BaseDifficulty;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        // A sleeper reaches this as the merged sleeping observation rather than as an NPC, because
        // placement swaps them and their bed for one object while the sleep lasts.
        if (target is SleepingNpcPointOfInterest merged)
            return pov.Where.PointsOfInterest.Contains(merged) && merged.Sleeper.IsAlive;

        if (target is not SceneNpc sceneNpc) return false;
        if (sceneNpc.Entity is not NpcEntity npc || !npc.IsAlive) return false;

        return scene.GetNpcsAt(pov.Where, pov.When).Any(n => n.Id == sceneNpc.Id);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => target is SleepingNpcPointOfInterest
            ? "go through their pockets while they sleep"
            : $"go through {NpcPossessive(target)} pockets";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"pick {SleeperGate.Name(target)}'s pocket";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var sceneNpc = target as SceneNpc ?? (target as SleepingNpcPointOfInterest)?.Sleeper;
        if (sceneNpc?.Entity is not NpcEntity npc)
            return System.Array.Empty<Outcome>();

        var reports = new List<Outcome>();

        // Coins, always — everybody carries a few, and a purse with nothing in it would make the
        // whole verb pointless whenever the archetype has no pocket items authored.
        int coins = 1 + System.Math.Abs(npc.NpcId.GetHashCode()) % 5;
        reports.Add(new CoinGrantOutcome(CoinType.Copper, coins));

        // And whatever this sort of person keeps loose: a baker's crust, a reeve's tally stick.
        if (npc.Archetype is NamedNpcArchetype named)
        {
            var pocket = named.PocketItems;
            if (pocket.Count > 0)
                reports.Add(new ItemGrantOutcome(pocket[System.Math.Abs(npc.NpcId.GetHashCode()) % pocket.Count]()));
        }

        return reports;
    }

    // Not recordable: whose pocket, and what is in it, is not stable across a rebuild.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
