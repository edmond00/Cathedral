using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Strengthen Relationship" — available once the party member is no longer a Stranger.
/// A friendly catch-up: success nudges affinity up one step (max CloseFriend), failure down one
/// (min AnnoyingAcquaintance).
///
/// <para>
/// This is by far the largest tree, and deliberately so: it is the one conversation the player can
/// have with the same person over and over, so it needs somewhere to go. The greeting opens onto
/// twelve subjects of ordinary talk — the weather, the harvest, the beasts, the folk hereabouts —
/// and each subject runs two to four replies deep before the check.
/// </para>
///
/// <para>
/// The topics are not evenly weighted. Six run rich (four ways to follow them), four run short, and
/// two — health and the wild country — run deepest, because they are where someone actually opens
/// up. What the NPC says at each subject comes from <c>{npc:opinion_&lt;topic&gt;}</c>, so the same
/// authored branch is a different conversation with a smith, a shepherd and a hermit.
/// </para>
///
/// <para>
/// The tree is split across partial files by subject family — <c>.Land</c>, <c>.Hearth</c>,
/// <c>.Folk</c> — and every node is built by a static <b>method</b>, never a static field: a field
/// graph this size would silently depend on textual initialisation order, which partial files do
/// not guarantee.
/// </para>
///
/// <para>
/// Every replica is the spoken line, plainly — see "Authoring the neutral text" on
/// <see cref="DialogueTree"/>. Small talk is where authored charm is most tempting and does the most
/// damage: this is the one tree a player hears dozens of times, so a turn of phrase written in here
/// becomes a tic the persona then embroiders on top of.
/// </para>
/// </summary>
public partial class StrengthenRelationshipTree : DialogueTree
{
    public override string TreeId           => "strengthen_relationship";
    public override string DisplayName      => "Strengthen Relationship";
    public override string Description      => "deepening the bond with someone you already know";
    public override string AssociatedVerbId => "strengthen_relationship";

    /// <summary>What succeeding at this conversation teaches: an acquaintance turned into something warmer.</summary>
    public override string? GrantedModusMentisId => "friendship";

    // Small talk is repeatable and self-contained: a routine can bake in the trigger so replaying it
    // starts the chat directly (its success is rolled live each time).
    public override DialogueRoutineBehavior RoutineBehavior => DialogueRoutineBehavior.IncludeTrigger;

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[]
    {
        new AffinityIncrementOutcome(+1, AffinityLevel.AnnoyingAcquaintance, AffinityLevel.CloseFriend),
    };

    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[]
    {
        new AffinityIncrementOutcome(-1, AffinityLevel.AnnoyingAcquaintance, AffinityLevel.CloseFriend),
    };

    // ── Authoring helper ───────────────────────────────────────────────────────

    /// <summary>
    /// A branch end. <paramref name="depth"/> is how many player replies reached it, which sets the
    /// difficulty (see <see cref="BranchDifficulty"/>) — the pool grows with depth, so the target
    /// must too.
    /// </summary>
    private static ResolutionNode End(string id, int depth,
                                      string success, string successIndirect,
                                      string failure, string failureIndirect) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect);

    // ── Entry ──────────────────────────────────────────────────────────────────

    private static readonly NpcLineNode Greeting = new(
        nodeId:  "greeting",
        replica:         "{you:name}. Good to see you again.",
        replicaIndirect: "I greet {you:name} and say it is good to see them again.",
        replicaHeard:    "{npc:name} greets me and says it is good to see me again.",

        // ── the land and the year (.Land) ───────────────────────────────────
        new PlayerOption("talk_weather", "turn the talk to the weather",
            "The weather has been fine, has it not?",
            "I tell {npc:name} the weather has been fine.",
            WeatherTopic()),

        new PlayerOption("talk_seasons", "remark on how far the year has turned",
            "The year is getting on.",
            "I tell {npc:name} the year is getting on.",
            SeasonsTopic()),

        new PlayerOption("talk_harvest", "ask how the crop is standing",
            "How is the crop looking this year?",
            "I ask {npc:name} how the crop is looking this year.",
            HarvestTopic()),

        new PlayerOption("talk_water", "mention the river and the rain",
            "The water has been strange this month.",
            "I tell {npc:name} the water has been strange this month.",
            WaterTopic()),

        // ── the household (.Hearth) ─────────────────────────────────────────
        new PlayerOption("talk_food", "turn the talk to food and drink",
            "What have you been eating lately?",
            "I ask {npc:name} what they have been eating lately.",
            FoodTopic()),

        new PlayerOption("talk_kin", "ask after their household",
            "And your household? Is everyone well?",
            "I ask {npc:name} whether everyone in their household is well.",
            KinTopic()),

        new PlayerOption("talk_rest", "ask what they do when the work is done",
            "What do you do when the day's work is done?",
            "I ask {npc:name} what they do when the day's work is done.",
            RestTopic()),

        new PlayerOption("talk_health", "ask how they have been keeping",
            "How have you been keeping, {npc:name}?",
            "I ask {npc:name} how they have been keeping.",
            HealthTopic()),

        // ── work, beasts and neighbours (.Folk) ─────────────────────────────
        new PlayerOption("talk_work", "ask about their work",
            "How has the work been going?",
            "I ask {npc:name} how the work has been going.",
            WorkTopic()),

        new PlayerOption("talk_beasts", "turn the talk to animals",
            "How are the animals here?",
            "I ask {npc:name} how the animals here are.",
            BeastsTopic()),

        new PlayerOption("talk_wilds", "ask about the country beyond the fields",
            "What is the country like past the last field?",
            "I ask {npc:name} what the country is like past the last field.",
            WildsTopic()),

        new PlayerOption("talk_neighbours", "ask what the folk hereabouts have been up to",
            "And the people here? Is anything happening?",
            "I ask {npc:name} whether anything is happening among the people here.",
            NeighboursTopic()));

    public override NpcLineNode EntryNode => Greeting;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => !npc.AffinityTable.IsStranger(partyMemberId);
}
