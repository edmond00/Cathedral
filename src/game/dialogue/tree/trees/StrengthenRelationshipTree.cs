using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

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
/// </summary>
public partial class StrengthenRelationshipTree : DialogueTree
{
    public override string TreeId           => "strengthen_relationship";
    public override string DisplayName      => "Strengthen Relationship";
    public override string Description      => "deepening the bond with someone you already know";
    public override string AssociatedVerbId => "strengthen_relationship";

    // Small talk is repeatable and self-contained: a routine can bake in the trigger so replaying it
    // starts the chat directly (its success is rolled live each time).
    public override DialogueRoutineBehavior RoutineBehavior => DialogueRoutineBehavior.IncludeTrigger;

    public override IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; } = new IDialogueOutcome[]
    {
        new AffinityIncrementOutcome(+1, AffinityLevel.AnnoyingAcquaintance, AffinityLevel.CloseFriend),
    };

    public override IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; } = new IDialogueOutcome[]
    {
        new AffinityIncrementOutcome(-1, AffinityLevel.AnnoyingAcquaintance, AffinityLevel.CloseFriend),
    };

    // ── Authoring helper ───────────────────────────────────────────────────────

    /// <summary>
    /// A branch end. <paramref name="depth"/> is how many player replies reached it, which sets the
    /// difficulty (see <see cref="BranchDifficulty"/>) — the pool grows with depth, so the target
    /// must too.
    /// </summary>
    private static ResolutionNode End(string id, int depth, string success, string failure) => new(
        nodeId:         id,
        difficulty:     BranchDifficulty.Easy(depth),
        successReplica: success,
        failureReplica: failure);

    // ── Entry ──────────────────────────────────────────────────────────────────

    private static readonly NpcLineNode Greeting = new(
        nodeId:  "greeting",
        replica: "Ah, {you:name}! Good to see you again.",

        // ── the land and the year (.Land) ───────────────────────────────────
        new PlayerOption("talk_weather", "turn the talk to the weather",
            "Fine weather we're having, isn't it?", WeatherTopic()),

        new PlayerOption("talk_seasons", "remark on how far the year has turned",
            "The year's getting on. It always catches me out.", SeasonsTopic()),

        new PlayerOption("talk_harvest", "ask how the crop is standing",
            "How's the crop looking this year?", HarvestTopic()),

        new PlayerOption("talk_water", "mention the river and the rain",
            "The water's been strange this month, hasn't it.", WaterTopic()),

        // ── the household (.Hearth) ─────────────────────────────────────────
        new PlayerOption("talk_food", "turn the talk to food and drink",
            "What's been on your table lately?", FoodTopic()),

        new PlayerOption("talk_kin", "ask after their household",
            "And your household? Everyone well?", KinTopic()),

        new PlayerOption("talk_rest", "ask what they do when the work is done",
            "What do you do with yourself when the day's done?", RestTopic()),

        new PlayerOption("talk_health", "ask how they have been keeping",
            "How have you been keeping, {npc:name}?", HealthTopic()),

        // ── work, beasts and neighbours (.Folk) ─────────────────────────────
        new PlayerOption("talk_work", "ask about their work",
            "How's the work been treating you?", WorkTopic()),

        new PlayerOption("talk_beasts", "turn the talk to animals",
            "The beasts hereabouts — how do they fare?", BeastsTopic()),

        new PlayerOption("talk_wilds", "ask about the country beyond the fields",
            "What's it like out past the last field?", WildsTopic()),

        new PlayerOption("talk_neighbours", "ask what the folk hereabouts have been up to",
            "And the folk hereabouts? Anything stirring?", NeighboursTopic()));

    public override NpcLineNode EntryNode => Greeting;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => !npc.AffinityTable.IsStranger(partyMemberId);
}
