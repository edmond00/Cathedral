using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Runtime;
using Cathedral.Game.Dialogue.Tree.Trees;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Work;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Static, headless report on the shape of every dialogue tree — the counterpart to
/// <c>--dialogue-view</c> for anyone (or anything) that cannot open a window. Run it with
/// <c>--dialogue-audit</c>.
///
/// <para>
/// It answers the three questions that actually go wrong when authoring a tree: <b>how much</b>
/// content is in it (player replies, NPC lines, branch ends), <b>how long</b> its branches run
/// (the number of player replies from the greeting to the dice check), and whether every
/// <c>{scope:field}</c> token in it is one <see cref="DialogueTemplate"/> can expand — a typo'd
/// token is invisible until it reaches a player as literal braces.
/// </para>
///
/// <para>
/// Design targets, enforced as warnings rather than errors so a deliberate exception is possible:
/// branches run 2–4 player replies, small talk carries the bulk of the content, and every NPC line
/// offers at least one reply (a bare NPC line would strand the conversation).
/// </para>
/// </summary>
public static class DialogueTreeAudit
{
    private const int MinBranchLength = 2;
    private const int MaxBranchLength = 4;

    private static readonly Regex TokenPattern = new(@"\{([a-zA-Z]+):([a-zA-Z_]+)\}", RegexOptions.Compiled);

    public static string BuildReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("── Dialogue tree audit ───────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine($"{"tree",-26}{"replies",8}{"npc",6}{"ends",6}{"branches",10}{"len min/avg/max",18}");
        sb.AppendLine(new string('─', 74));

        var warnings = new List<string>();
        var trees = DialogueTreeRegistry.Instance.All
            .OrderByDescending(t => Measure(t).PlayerOptions)
            .ToList();

        // The caught-red-handed trees are built per crime and never registered, so the registry walk
        // would miss them entirely. One variant is representative — the three differ only in the
        // witness's opening line.
        trees.Add(CaughtRedHandedTreeFactory.Create(CriminalAffinityType.Thief, witnessIsBrave: true));

        foreach (var tree in trees)
        {
            var m = Measure(tree);
            sb.AppendLine(
                $"{tree.TreeId,-26}{m.PlayerOptions,8}{m.NpcNodes,6}{m.Resolutions,6}{m.BranchLengths.Count,10}" +
                $"{$"{m.MinLength} / {m.AverageLength:0.00} / {m.MaxLength}",18}");
            warnings.AddRange(m.Warnings.Select(w => $"  {tree.TreeId}: {w}"));
        }

        warnings.AddRange(CheckTokensExpand(trees));

        sb.AppendLine();
        if (warnings.Count == 0)
        {
            sb.AppendLine("No warnings — every branch is 2–4 replies, and every token expands for every archetype.");
        }
        else
        {
            sb.AppendLine($"Warnings ({warnings.Count}):");
            foreach (var w in warnings) sb.AppendLine(w);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Expands every token the trees use against a real spawned NPC of every speaking archetype.
    /// Knowing a token is <i>spelled</i> right (the per-tree check) is not the same as knowing it
    /// <i>resolves</i>: a resolver that reaches through a null — no trade catalogue, no pending job
    /// offer, an archetype that overrode nothing — comes back empty, and an empty expansion reaches
    /// the player as a hole in the middle of a sentence.
    /// </summary>
    private static IEnumerable<string> CheckTokensExpand(IEnumerable<DialogueTree> trees)
    {
        var tokens = new SortedSet<string>();
        foreach (var tree in trees)
            CollectTreeTokens(tree, tokens);

        var pc = new Protagonist();

        foreach (var archetype in SpeakingArchetypes())
        {
            // A fixed seed keeps the report stable run to run; nothing here depends on the roll.
            var npc = archetype.Spawn(new Random(1));
            var ctx = new DialogueContext(pc, npc, world: null, locationId: 0, DialogueNameTable.Build(pc, npc));

            // The job tokens only mean anything once the REQUEST_JOB verb has chosen a post, which is
            // exactly the state the request-job tree runs in — so reproduce it rather than reporting a
            // false hole for every archetype.
            npc.PendingJobOffer = JobRegistry.Instance
                .SampleJobs(npc.NpcId, archetype.ArchetypeId, 1)
                .FirstOrDefault();

            foreach (string token in tokens)
            {
                string expanded = DialogueTemplate.Expand($"{{{token}}}", ctx);
                if (string.IsNullOrWhiteSpace(expanded))
                    yield return $"  {archetype.ArchetypeId}: {{{token}}} expands to nothing";
                else if (expanded == $"{{{token}}}")
                    yield return $"  {archetype.ArchetypeId}: {{{token}}} was left unexpanded";
            }
        }
    }

    /// <summary>Every token appearing anywhere in a tree, as bare <c>scope:field</c> keys.</summary>
    private static void CollectTreeTokens(DialogueTree tree, SortedSet<string> into)
    {
        var seen = new HashSet<string>();
        Walk(tree.EntryNode);

        void Walk(DialogueNode node)
        {
            if (!seen.Add(node.NodeId)) return;
            switch (node)
            {
                case ResolutionNode res:
                    Add(res.SuccessReplica);
                    Add(res.FailureReplica);
                    break;
                case NpcLineNode npc:
                    Add(npc.Replica);
                    foreach (var opt in npc.Options)
                    {
                        Add(opt.Replica);
                        Add(opt.Intent);
                        Walk(opt.Next);
                    }
                    break;
            }
        }

        void Add(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (Match match in TokenPattern.Matches(text!))
                into.Add($"{match.Groups[1].Value}:{match.Groups[2].Value}");
        }
    }

    /// <summary>
    /// One instance of every archetype whose NPCs can hold a conversation. Listed explicitly: the
    /// archetypes are plain classes with no registry, and an audit that silently skipped a new one
    /// would be worse than no audit.
    /// </summary>
    private static IEnumerable<NamedNpcArchetype> SpeakingArchetypes() => new NamedNpcArchetype[]
    {
        // Workshop crafts
        new BlacksmithArchetype(), new BakerArchetype(),   new BrewerArchetype(),
        new CarpenterArchetype(),  new CooperArchetype(),  new MillerArchetype(),
        new WeaverArchetype(),     new ApprenticeArchetype(),
        // Field and farm
        new ReeveArchetype(),      new HaywardArchetype(), new PlowmanArchetype(),
        new ReaperArchetype(),     new BondmanArchetype(), new ShepherdArchetype(),
        new SwineherdArchetype(),  new DairymaidArchetype(), new PoultryKeeperArchetype(),
        new FarmerArchetype(),     new FarmhandArchetype(),
        // Out past the fields
        new WoodcutterArchetype(), new CharcoalBurnerArchetype(),
        new FishermanArchetype(),  new MinerArchetype(),
        new DruidArchetype(),      new HermitArchetype(), new SavageArchetype(),
    };

    /// <summary>Per-tree measurements. Branch length counts <b>player replies</b>, not nodes.</summary>
    private sealed class Measurement
    {
        public int PlayerOptions;
        public int NpcNodes;
        public int Resolutions;
        public List<int> BranchLengths = new();
        public List<string> Warnings   = new();

        public int    MinLength     => BranchLengths.Count == 0 ? 0 : BranchLengths.Min();
        public int    MaxLength     => BranchLengths.Count == 0 ? 0 : BranchLengths.Max();
        public double AverageLength => BranchLengths.Count == 0 ? 0 : BranchLengths.Average();
    }

    private static Measurement Measure(DialogueTree tree)
    {
        var m         = new Measurement();
        var optSeen   = new HashSet<string>();
        var badTokens = new SortedSet<string>();

        // NodeId → the distinct node objects wearing it. Ids are documented as unique within a tree,
        // and nothing enforces it: two nodes sharing one would merge in the debug viewer and hide
        // half the tree, while the counts here would quietly under-report.
        var nodesById = new Dictionary<string, HashSet<DialogueNode>>();

        // Every depth each resolution is actually reached at, to cross-check its authored difficulty
        // against the ladders in BranchDifficulty. A node whose difficulty matches neither ladder at
        // its real depth is almost always an authoring slip — the depth was counted wrong.
        var resolutionDepths = new Dictionary<ResolutionNode, SortedSet<int>>();

        Walk(tree.EntryNode, depth: 0, path: new HashSet<DialogueNode>());
        CollectTokens(tree.Description);

        foreach (var token in badTokens)
            m.Warnings.Add($"unknown template token {{{token}}}");

        foreach (var length in m.BranchLengths.Distinct().OrderBy(l => l))
            if (length < MinBranchLength || length > MaxBranchLength)
                m.Warnings.Add($"{m.BranchLengths.Count(l => l == length)} branch(es) of {length} " +
                               $"player replies (target {MinBranchLength}–{MaxBranchLength})");

        foreach (var (nodeId, sharing) in nodesById.OrderBy(p => p.Key))
            if (sharing.Count > 1)
                m.Warnings.Add($"{sharing.Count} different nodes all use the id '{nodeId}'");

        foreach (var (res, depths) in resolutionDepths.OrderBy(p => p.Key.NodeId))
        {
            // A forced node never rolls, so its difficulty is meaningless — skip it.
            if (res.Mode != ResolutionMode.DiceCheck) continue;

            if (depths.All(d => res.Difficulty != BranchDifficulty.Easy(d)
                             && res.Difficulty != BranchDifficulty.Hard(d)))
                m.Warnings.Add(
                    $"resolution '{res.NodeId}' needs {res.Difficulty} six(es) at depth " +
                    $"{string.Join("/", depths)} — neither ladder gives that; the authored depth is likely wrong");
        }

        return m;

        void Walk(DialogueNode node, int depth, HashSet<DialogueNode> path)
        {
            // A shared node reachable by two routes is legal; a node reachable from itself is not,
            // and would spin here forever.
            if (!path.Add(node))
            {
                m.Warnings.Add($"cycle through node '{node.NodeId}'");
                return;
            }

            if (!nodesById.TryGetValue(node.NodeId, out var sharing))
                nodesById[node.NodeId] = sharing = new HashSet<DialogueNode>();
            bool firstVisit = sharing.Add(node) && sharing.Count == 1;

            switch (node)
            {
                case ResolutionNode res:
                    if (firstVisit) m.Resolutions++;
                    CollectTokens(res.SuccessReplica);
                    CollectTokens(res.FailureReplica);
                    m.BranchLengths.Add(depth);
                    if (!resolutionDepths.TryGetValue(res, out var depths))
                        resolutionDepths[res] = depths = new SortedSet<int>();
                    depths.Add(depth);
                    break;

                case NpcLineNode npc:
                    if (firstVisit) m.NpcNodes++;
                    CollectTokens(npc.Replica);
                    if (npc.Options.Count == 0)
                        m.Warnings.Add($"NPC line '{npc.NodeId}' offers no reply — the conversation dead-ends there");

                    var optionIds = new HashSet<string>();
                    foreach (var opt in npc.Options)
                    {
                        if (!optionIds.Add(opt.OptionId))
                            m.Warnings.Add($"NPC line '{npc.NodeId}' offers two replies both called '{opt.OptionId}'");
                        if (optSeen.Add($"{npc.NodeId}::{opt.OptionId}")) m.PlayerOptions++;
                        CollectTokens(opt.Replica);
                        CollectTokens(opt.Intent);
                        Walk(opt.Next, depth + 1, path);
                    }
                    break;
            }

            path.Remove(node);
        }

        void CollectTokens(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (Match match in TokenPattern.Matches(text!))
            {
                string key = $"{match.Groups[1].Value}:{match.Groups[2].Value}";
                if (!DialogueTemplate.IsKnownField(key)) badTokens.Add(key);
            }
        }
    }
}
