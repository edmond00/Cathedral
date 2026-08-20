using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Fight;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Dialogue.Tree.Trees;
using Cathedral.Game.Narrative.Reminescence;
using Cathedral.Game.Narrative.Work;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Answers the question <c>--mm-grant-csv</c> raises but cannot settle: <b>how much of the modus
/// mentis catalogue can a player actually reach, playing normally?</b>
///
/// <para>A verb sweep can only see one of the five routes a lesson arrives by, so reading "62 of 183
/// modi mentis are taught by a verb" as the answer understates it badly — a job teaches three, a
/// conversation teaches its own, a fight teaches the skill it drew, and childhood deals the opening
/// hand. This gathers all five, per modus mentis, and says which routes reach it:</para>
///
/// <list type="bullet">
///   <item><b>childhood</b> — the skill types every reminescence fragment's outcome grants</item>
///   <item><b>fight</b> — the modus mentis unlocking any fighting skill on a medium this body owns,
///     which is what the in-fight learning check teaches (and what ATTACK's first blow draws)</item>
///   <item><b>action</b> — a verb's own grant and every per-target override, swept by
///     <see cref="Scene.VerbLessonSweep"/></item>
///   <item><b>dialogue</b> — a tree's own lesson, including GATHER KNOWLEDGE's topic grants, which
///     reach every archetype's <c>TradeModusMentisId</c></item>
///   <item><b>work</b> — the three modi mentis every job in <see cref="JobRegistry"/> pays in</item>
/// </list>
///
/// <para><b>Anatomy is the second axis and it is not optional.</b> The protagonist is human, so a
/// beast's lessons are out of reach however many routes name them — but a tamed companion narrates
/// and fights, so they are reachable in the run, by somebody else. Both columns are reported;
/// counting them as one is what makes an audit of this say 183 when the player can hold 120.</para>
/// </summary>
public static class MmReachAudit
{
    /// <summary>The five routes a lesson can arrive by, plus what named each one.</summary>
    private sealed class Reach
    {
        public readonly SortedSet<string> Childhood = new(StringComparer.Ordinal);
        public readonly SortedSet<string> Fight     = new(StringComparer.Ordinal);
        public readonly SortedSet<string> Action    = new(StringComparer.Ordinal);
        public readonly SortedSet<string> Dialogue  = new(StringComparer.Ordinal);
        public readonly SortedSet<string> Work      = new(StringComparer.Ordinal);

        public bool Any => Childhood.Count + Fight.Count + Action.Count + Dialogue.Count + Work.Count > 0;
        public int  Routes => (Childhood.Count > 0 ? 1 : 0) + (Fight.Count > 0 ? 1 : 0)
                            + (Action.Count    > 0 ? 1 : 0) + (Dialogue.Count > 0 ? 1 : 0)
                            + (Work.Count      > 0 ? 1 : 0);
    }

    public static string BuildCsv(out string summary)
    {
        var reach = new Dictionary<string, Reach>(StringComparer.Ordinal);
        Reach Of(string mmId)
        {
            if (!reach.TryGetValue(mmId, out var r)) reach[mmId] = r = new Reach();
            return r;
        }

        foreach (var (mm, by) in Childhood()) Of(mm).Childhood.Add(by);
        foreach (var (mm, by) in Fight())     Of(mm).Fight.Add(by);
        foreach (var (mm, by) in Work())      Of(mm).Work.Add(by);

        // What the circumstances of an act teach, which is an action-route grant that hangs off no
        // What a verb teaches from the context rather than from the target: free logic inside each
        // verb, so it cannot be enumerated by running it — each verb declares the ids it can return.
        foreach (var (mm, by, dialogue) in VerbsAndTrees())
            (dialogue ? Of(mm).Dialogue : Of(mm).Action).Add(by);

        return Render(reach, out summary);
    }

    // ── the five routes ────────────────────────────────────────────────────────

    /// <summary>Every skill a reminescence fragment's outcome deals.</summary>
    private static IEnumerable<(string Mm, string By)> Childhood()
    {
        var catalogue = new Dictionary<string, ReminescenceData>(StringComparer.OrdinalIgnoreCase);
        ReminescenceCatalog.Build(catalogue);

        foreach (var reminescence in catalogue.Values)
        foreach (var fragment in reminescence.Fragments)
        foreach (var type in fragment.Outcome.SkillTypes)
        {
            if (Activator.CreateInstance(type) is ModusMentis mm)
                yield return (mm.ModusMentisId, reminescence.Id + "/" + fragment.Name);
        }
    }

    /// <summary>
    /// The modus mentis unlocking any fighting skill on a medium the protagonist's body owns —
    /// what <c>Fighter.GetLearnableSkills</c> offers, and so what the learning check can teach.
    ///
    /// <para>Read off the medium registries rather than off <c>GetAttackSkills</c>: the check teaches
    /// guards and buffs too, and a skill list is walked one unknown at a time, so a long enough run
    /// reaches every entry of every medium the body has.</para>
    /// </summary>
    private static IEnumerable<(string Mm, string By)> Fight()
    {
        var body     = new Protagonist();
        var registry = FightingSkillRegistry.Instance;

        foreach (var cat in OrganMediumRegistry.GetAll())
        {
            if (body.GetOrganById(cat.OrganId) == null) continue;       // this body lacks the organ
            foreach (var mm in Unlocking(registry, cat.SkillIds)) yield return (mm.Mm, "organ:" + cat.OrganId + "/" + mm.By);
        }

        foreach (var cat in BodyPartMediumRegistry.GetAll())
        {
            if (body.GetBodyPartById(cat.BodyPartId) == null) continue;
            foreach (var mm in Unlocking(registry, cat.SkillIds)) yield return (mm.Mm, "bodypart:" + cat.BodyPartId + "/" + mm.By);
        }

        // A weapon medium needs a weapon of that category to exist and be obtainable; the catalogue
        // is the honest answer to that, since anything in it can be found, bought or taken.
        var obtainable = new HashSet<string>(
            ItemRegistry.Instance.All.OfType<IWeaponItem>().Select(w => w.WeaponCategory),
            StringComparer.Ordinal);

        foreach (var cat in WeaponMediumRegistry.GetAll())
        {
            if (!obtainable.Contains(cat.CategoryId)) continue;
            foreach (var mm in Unlocking(registry, cat.SkillIds)) yield return (mm.Mm, "weapon:" + cat.CategoryId + "/" + mm.By);
        }
    }

    private static IEnumerable<(string Mm, string By)> Unlocking(FightingSkillRegistry registry, IEnumerable<string> skillIds)
    {
        foreach (var id in skillIds)
        {
            var skill = registry.GetById(id);
            if (skill != null) yield return (skill.RequiredModusMentisId, skill.SkillId);
        }
    }

    /// <summary>The three modi mentis every job pays in.</summary>
    private static IEnumerable<(string Mm, string By)> Work()
    {
        foreach (var job in JobRegistry.Instance.All)
        foreach (var mm in job.ModusMentisIds)
            yield return (mm, job.Id);
    }

    /// <summary>
    /// A verb's own grant, every per-target override the world places, and every tree's lesson.
    /// The sweep half comes from <see cref="Scene.VerbLessonSweep"/>, which is also what keeps this
    /// audit and the <c>mm-grants</c> skill from disagreeing about a verb's declared lessons.
    /// </summary>
    private static IEnumerable<(string Mm, string By, bool Dialogue)> VerbsAndTrees()
    {
        foreach (var (mm, verb, source) in Scene.VerbLessonSweep.Grants())
        {
            if (mm.StartsWith("(", StringComparison.Ordinal)) continue;   // an audit note, not an id

            // `anatomy-alternate` is counted like the rest: it is the same verb, reached the same
            // way, by the other body. Whether *this* body can hold it is the anatomy column's
            // question, asked separately below, so filtering here would answer it twice.
            yield return (mm, verb, Dialogue: false);
        }

        // A tree's own lesson. Read straight from the trees rather than from the scene sweep, which
        // walks objects and has no business knowing about conversations.
        foreach (var tree in AllTrees())
        {
            string? id = null;
            try { id = tree.GrantedModusMentisId; } catch { }
            if (id != null) yield return (id, "tree:" + tree.TreeId, true);
        }

        // GATHER KNOWLEDGE's trade topic teaches whatever the person's own trade is, so the reachable
        // set is every archetype's declaration — the one grant in the game whose value is a table
        // lookup on who was asked.
        foreach (var archetype in NamedArchetypes())
        {
            string? trade = null;
            try { trade = archetype.TradeModusMentisId; } catch { }
            if (!string.IsNullOrEmpty(trade))
                yield return (trade, "gather_knowledge/trade:" + archetype.ArchetypeId, true);
        }
        yield return ("peasantry", "gather_knowledge/trade:(archetype with none)", true);

        // Every tree's per-branch lessons. AdditionalGrantedModusMentisIds is keyed on who was
        // spoken to and which resolution was reached, so it is sampled rather than declared: one
        // NPC with authority and one without, against every resolution the tree contains.
        foreach (var (mm, by) in TreeBranchGrants()) yield return (mm, by, true);
    }


    /// <summary>
    /// The lessons each tree's branches teach, sampled. There is no static declaration to read:
    /// <see cref="DialogueTree.AdditionalGrantedModusMentisIds"/> takes the person and the branch,
    /// which is the whole point of it — asking a reeve about the neighbours is not the lesson that
    /// asking a farmhand is. So the audit walks every resolution in every tree against two sampled
    /// speakers and records what comes back.
    /// </summary>
    internal static IEnumerable<(string Mm, string By)> TreeBranchGrants()
    {
        var speakers = SampleSpeakers().ToList();
        if (speakers.Count == 0) yield break;

        foreach (var tree in AllTrees())
        foreach (var resolution in Resolutions(tree))
        foreach (var npc in speakers)
        {
            List<string> ids;
            try { ids = tree.LessonsFor(npc, resolution).ToList(); }
            catch { continue; }

            foreach (var id in ids)
                yield return (id, tree.TreeId + "/branch");
        }
    }


    /// <summary>
    /// Every tree a player can reach. The registry is not the whole answer: the caught-red-handed
    /// trees are built per criminal type by a factory and never registered, so reading the registry
    /// alone reports their lessons as unreachable when they are the ones a thief meets most.
    /// </summary>
    private static IEnumerable<DialogueTree> AllTrees()
    {
        foreach (var tree in DialogueTreeRegistry.Instance.All) yield return tree;

        foreach (Dialogue.Affinity.CriminalAffinityType crime
                 in Enum.GetValues<Dialogue.Affinity.CriminalAffinityType>())
        {
            DialogueTree? tree = null;
            try { tree = Dialogue.Tree.Trees.CaughtRedHandedTreeFactory.Create(crime); } catch { }
            if (tree != null) yield return tree;
        }
    }

    /// <summary>Two speakers: one who holds authority and one who does not, since trees key on it.</summary>
    private static IEnumerable<NpcEntity> SampleSpeakers()
    {
        foreach (var archetype in NamedArchetypes()
                     .Where(a => a.Species.AnatomyType == AnatomyType.Human)
                     .GroupBy(a => a.AuthorityLevel > 0)
                     .Select(g => g.First()))
        {
            NpcEntity? npc = null;
            try { npc = archetype.Spawn(new Random(7), "audit"); } catch { }
            if (npc != null) yield return npc;
        }
    }

    /// <summary>Every resolution node in a tree, found by walking it from the root.</summary>
    private static IEnumerable<ResolutionNode> Resolutions(DialogueTree tree)
    {
        var seen  = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<DialogueNode>();
        try { stack.Push(tree.EntryNode); } catch { yield break; }

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node == null || !seen.Add(node.NodeId)) continue;
            if (node is ResolutionNode r) { yield return r; continue; }

            foreach (var child in Children(node)) stack.Push(child);
        }
    }

    private static IEnumerable<DialogueNode> Children(DialogueNode node)
    {
        if (node is NpcLineNode line)
            foreach (var option in line.Options)
                if (option.Next != null) yield return option.Next;
    }

    /// <summary>Every speaking archetype, by reflection, so a new one is covered the day it is written.</summary>
    private static IEnumerable<NamedNpcArchetype> NamedArchetypes()
        => typeof(NamedNpcArchetype).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(NamedNpcArchetype).IsAssignableFrom(t)
                        && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => Activator.CreateInstance(t) as NamedNpcArchetype)
            .Where(a => a != null)!
            .Cast<NamedNpcArchetype>();

    // ── rendering ──────────────────────────────────────────────────────────────

    private static string Render(Dictionary<string, Reach> reach, out string summary)
    {
        var all = ModusMentisRegistry.Instance.GetAllModiMentis()
                                     .OrderBy(m => m.ModusMentisId, StringComparer.Ordinal)
                                     .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", new[]
        {
            "MM", "MM_NAME", "REACHABLE", "ROUTE_COUNT",
            "CHILDHOOD", "FIGHT", "ACTION", "DIALOGUE", "WORK",
            "HUMAN_CAN_LEARN", "BEAST_CAN_LEARN", "WHO_CAN_REACH_IT",
            "MM_FUNCTIONS", "MM_ORGANS", "MM_MEMORY", "MM_MORAL", "MM_REQUIRES",
            "VIA_CHILDHOOD", "VIA_FIGHT", "VIA_ACTION", "VIA_DIALOGUE", "VIA_WORK",
        }));

        int humanReachable = 0, beastOnly = 0, unreachable = 0;
        var byRoute = new Dictionary<string, int>(StringComparer.Ordinal)
            { ["childhood"] = 0, ["fight"] = 0, ["action"] = 0, ["dialogue"] = 0, ["work"] = 0 };

        foreach (var mm in all)
        {
            var r = reach.GetValueOrDefault(mm.ModusMentisId) ?? new Reach();

            bool human = Learnable(mm, AnatomyType.Human);
            bool beast = Learnable(mm, AnatomyType.Beast);

            // Reachable means a route names it AND some body in the party can hold the lesson. A
            // grant to a body that cannot learn it is refused, not capped — see ModusMentisAnatomy.
            string who = !r.Any        ? "nobody (no route names it)"
                       : human && beast ? "protagonist or beast companion"
                       : human          ? "protagonist"
                       : beast          ? "beast companion only"
                                        : "nobody (no anatomy can hold it)";

            if      (r.Any && human) humanReachable++;
            else if (r.Any && beast) beastOnly++;
            else                     unreachable++;

            if (r.Childhood.Count > 0) byRoute["childhood"]++;
            if (r.Fight.Count     > 0) byRoute["fight"]++;
            if (r.Action.Count    > 0) byRoute["action"]++;
            if (r.Dialogue.Count  > 0) byRoute["dialogue"]++;
            if (r.Work.Count      > 0) byRoute["work"]++;

            sb.AppendLine(string.Join(",", new[]
            {
                Csv(mm.ModusMentisId), Csv(mm.DisplayName),
                Csv(r.Any && (human || beast) ? "yes" : "no"),
                Csv(r.Routes.ToString()),
                Yes(r.Childhood), Yes(r.Fight), Yes(r.Action), Yes(r.Dialogue), Yes(r.Work),
                Csv(human ? "yes" : "no"), Csv(beast ? "yes" : "no"), Csv(who),
                Csv(string.Join("; ", mm.Functions)), Csv(string.Join("; ", mm.Organs)),
                Csv(mm.MemoryType.ToString()), Csv(mm.MoralLevel.ToString()),
                Csv(mm.RequiredCapabilities.ToString()),
                Csv(Sample(r.Childhood)), Csv(Sample(r.Fight)), Csv(Sample(r.Action)),
                Csv(Sample(r.Dialogue)), Csv(Sample(r.Work)),
            }));
        }

        var s = new StringBuilder();
        s.AppendLine("=== MODUS MENTIS REACHABILITY ===");
        s.AppendLine($"  catalogue                              {all.Count}");
        s.AppendLine($"  reachable by the protagonist           {humanReachable}");
        s.AppendLine($"  reachable only by a beast companion    {beastOnly}");
        s.AppendLine($"  reachable by nobody in normal play     {unreachable}");
        s.AppendLine();
        s.AppendLine("  by route (a modus mentis may have several):");
        foreach (var (route, count) in byRoute.OrderByDescending(kv => kv.Value))
            s.AppendLine($"     {route,-12} {count,4}");
        summary = s.ToString();

        return sb.ToString();
    }

    private static bool Learnable(ModusMentis mm, AnatomyType anatomy)
    {
        try { return ModusMentisAnatomy.IsLearnableBy(mm, anatomy); } catch { return false; }
    }

    private static string Yes(SortedSet<string> route) => route.Count > 0 ? "yes" : "";

    /// <summary>What named this route, capped — the full list runs to dozens for a common lesson.</summary>
    private static string Sample(SortedSet<string> route)
        => route.Count == 0 ? ""
         : string.Join("; ", route.Take(5)) + (route.Count > 5 ? $" (+{route.Count - 5})" : "");

    private static string Csv(string? value)
    {
        value ??= "";
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
