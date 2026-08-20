using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// Headless audit of what there is to <i>do</i> in a location, as opposed to what there is in it.
///
/// <para>Every observable object in a scene expands into a list of actions by asking each registered
/// verb whether it applies (<c>SceneViewAdapter.RefreshVerbs</c>). An object no verb accepts is
/// prose and nothing else — it can be looked at once and never interacted with. Before the sensory
/// verbs existed that described most of the furniture in the game, and the number is invisible in
/// play because nothing is missing, exactly: the object is there, it reads well, and its action list
/// is simply empty.</para>
///
/// <para>So this counts. Per factory, across a sample of location ids, it reports the distribution of
/// verbs-per-observable against the design targets — <b>80% of observables reachable by at least two
/// verbs, 50% by at least three</b>, and about half observable by at least one sense — then warns
/// about the failures that are silent at runtime:</para>
///
/// <list type="bullet">
/// <item>an observable no verb accepts at any period;</item>
/// <item>a verb in the registry that no sampled scene ever offers — dead content;</item>
/// <item>a verb with no <c>GrantedModusMentisId</c>, so succeeding at it teaches nothing;</item>
/// <item>a modus mentis id, declared by a verb or by a target override, that does not resolve —
///   which grants nothing, silently, exactly as a mistyped trait id does;</item>
/// <item>a <c>ReferenceToolIds</c> entry no item matches, which makes the verb permanently
///   impossible rather than merely hard;</item>
/// <item>a location with fewer than two landmark areas.</item>
/// </list>
///
/// <para>Verb applicability is sampled at every time period, because presence is period-gated: an NPC
/// verb that only exists at night is still a verb the object has. Run after adding a verb, a
/// connector, or a batch of scene content.</para>
/// </summary>
public static partial class VerbAudit
{
    private const int SampleSize = 40;

    /// <summary>How many merged sleeping observations the sweep exercised. Reported so a zero here —
    /// which would mean the whole sleeping path went untested — cannot pass unnoticed.</summary>
    private static int SleepersProbed;

    /// <summary>The design targets from the verb-expansion brief, as fractions of all observables.</summary>
    private const double TargetTwoPlusVerbs  = 0.80;
    private const double TargetThreePlusVerbs = 0.50;
    private const double TargetSensory        = 0.50;

    /// <summary>The four sensory verbs, whose coverage is tracked separately.</summary>
    private static readonly HashSet<string> SensoryVerbIds =
        new() { "examine", "contemplate", "listen", "smell" };

    public static string BuildReport()
    {
        var sb       = new StringBuilder();
        var warnings = new List<string>();

        // Verbs seen offered anywhere, so the sweep can name the ones that were never offered at all.
        var everOffered = new HashSet<string>();

        sb.AppendLine("=== VERB AUDIT ===");
        sb.AppendLine($"Sampling {SampleSize} location ids per factory, at every time period.");
        sb.AppendLine();

        AuditVerbDeclarations(sb, warnings);

        sb.AppendLine("--- COVERAGE BY FACTORY ---");
        sb.AppendLine($"  {"factory",-10} {"observables",11} {"≥1",6} {"≥2",6} {"≥3",6} {"sensory",8}  {"mean",5}");

        foreach (var (label, build) in Factories())
            AuditFactory(sb, warnings, everOffered, label, build);

        sb.AppendLine();
        sb.AppendLine($"  targets: ≥2 verbs on {TargetTwoPlusVerbs:P0} of observables, " +
                      $"≥3 on {TargetThreePlusVerbs:P0}, a sense on {TargetSensory:P0}");

        AuditDeadVerbs(sb, warnings, everOffered);
        AuditUnbuiltKinds(sb, warnings);
        AuditVerbTargets(sb, warnings);
        AuditAnatomyReach(sb);

        sb.AppendLine();
        if (warnings.Count == 0)
        {
            sb.AppendLine("No warnings — every verb is reachable and teaches something, and every");
            sb.AppendLine("observable has an action on it.");
            return sb.ToString();
        }

        // Grouped by kind: one systemic fault across 40 ids × 9 factories buries everything else.
        sb.AppendLine($"--- {warnings.Count} WARNING(S), BY KIND ---");
        foreach (var group in warnings.GroupBy(Kind).OrderByDescending(g => g.Count()))
        {
            sb.AppendLine($"  [{group.Count(),4}] {group.Key}");
            foreach (var example in group.Distinct().Take(3))
                sb.AppendLine($"         e.g. {example}");
        }

        return sb.ToString();
    }

    private static IEnumerable<(string Label, Func<int, Scene> Build)> Factories()
    {
        yield return ("VILLAGE",  id => new Village.VillageSceneFactory().Build(id));
        yield return ("FARM",     id => new Farm.FarmSceneFactory().Build(id));
        yield return ("FIELD",    id => new Field.FieldSceneFactory().Build(id));
        yield return ("PLAIN",    id => new Plain.PlainSceneFactory().Build(id));
        yield return ("FOREST",   id => new Forest.ForestSceneFactory().Build(id));
        yield return ("CAVE",     id => new Cave.CaveSceneFactory().Build(id));
        yield return ("COAST",    id => new Coast.CoastSceneFactory().Build(id));
        yield return ("MOUNTAIN", id => new Mountain.MountainSceneFactory().Build(id));
        yield return ("PEAK",     id => new Peak.PeakSceneFactory().Build(id));
    }

    /// <summary>
    /// Whether this anatomy could ever be offered the verb at all — the capability half of
    /// <see cref="Verb.IsPossible"/>. A beast is not offered <c>unlock_door</c>, so a lesson it
    /// cannot hold there is no fault.
    /// </summary>
    private static bool CanBeAttemptedBy(Verb verb, AnatomyType anatomy)
    {
        var caps = AnatomyFactoryRegistry.GetFactory(anatomy).Capabilities;
        return (caps & verb.EffectiveCapabilities) == verb.EffectiveCapabilities;
    }

    /// <summary>
    /// Every type each verb was actually offered against — its target's, the area's, and the holder's
    /// where the target was an item the verb could take. Read by the unreachable-lesson check.
    /// </summary>
    private static readonly Dictionary<string, HashSet<Type>> ReachableTypes = new(StringComparer.Ordinal);

    private static HashSet<Type> Reachable(string verbId)
        => ReachableTypes.TryGetValue(verbId, out var set) ? set : ReachableTypes[verbId] = new HashSet<Type>();

    /// <summary>What each verb was ever seen to actually grant, as against merely offer.</summary>
    private static readonly Dictionary<string, HashSet<string>> GrantedMm = new(StringComparer.Ordinal);

    private static HashSet<string> Granted(string verbId)
        => GrantedMm.TryGetValue(verbId, out var set) ? set : GrantedMm[verbId] = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// A verb's own default that <b>never wins</b>. <c>Lessons</c> yields candidates in order and the
    /// first the body can hold is granted, so a branch matching everything the verb is ever offered
    /// on hides the default completely — the verb still declares it, both audits still report it as
    /// reachable, and no player ever learns it.
    ///
    /// <para>Reported separately from the reachability check because the fault is the opposite shape:
    /// there the lesson named content the verb never meets, here it names content the verb
    /// <i>always</i> meets. BREAK is only ever offered on breakables, so a branch testing for one
    /// leaves <c>brute_force</c> dead.</para>
    /// </summary>
    /// <summary>Every (verb, declared lesson) an object was seen to declare, with one object's name.</summary>
    private static readonly Dictionary<(string Verb, string Mm), string> DeclaredSeen = new();

    /// <summary>The subset of those that were ever actually granted.</summary>
    private static readonly HashSet<(string Verb, string Mm)> DeclaredWon = new();

    /// <summary>
    /// An object's own <see cref="IVerbModusMentisSource"/> declaration that <b>never wins</b>.
    ///
    /// <para>The same shape of fault as <see cref="CheckDefaultsCanWin"/> one layer up:
    /// <c>base.Lessons</c> yields the declaration ahead of the verb's default but behind every
    /// branch, so a branch testing for the very type that declares it silences the declaration.
    /// The object still promises the lesson and no player is ever taught it — the psalter declared
    /// <c>decipher</c> from behind EXAMINE's philosophy branch, and the toll board <c>tallycraft</c>
    /// from behind algebraic_analysis.</para>
    /// </summary>
    private static void CheckDeclarationsCanWin(List<string> warnings)
    {
        foreach (var (key, sample) in DeclaredSeen.OrderBy(kv => kv.Key.Verb, StringComparer.Ordinal))
        {
            if (DeclaredWon.Contains(key)) continue;
            // Verb and lesson unquoted, so the by-kind grouping keeps each pairing separate — the
            // whole value of this warning is WHICH declaration is dead.
            warnings.Add($"{key.Verb} → {key.Mm} is declared (e.g. by '{sample}') but a branch of that "
                       + "verb always matches first, so the declaration is never granted");
        }
    }

    private static void CheckDefaultsCanWin(List<string> warnings)
    {
        foreach (var verb in VerbRegistry.Instance.GetAll())
        {
            if (!GrantedMm.TryGetValue(verb.VerbId, out var won) || won.Count == 0) continue;

            var defaults = verb.GrantedModusMentisIds(null);
            if (defaults.Count == 0 || defaults.Any(won.Contains)) continue;

            warnings.Add($"verb '{verb.VerbId}' declares [{string.Join(", ", defaults)}] as its default "
                       + "but a branch always matches first, so the default is never granted");
        }
    }

    /// <summary>Prints what each verb can actually be turned on, so a lesson naming a type it never sees is visible.</summary>
    private static void AuditVerbTargets(StringBuilder sb, List<string> warnings)
    {
        CheckLessonsCanBeReached(warnings);
        CheckDefaultsCanWin(warnings);
        CheckDeclarationsCanWin(warnings);

        sb.AppendLine();
        sb.AppendLine("── what each verb was actually seen to grant ──");
        foreach (var (v, mms) in GrantedMm.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            sb.AppendLine($"     {v,-16} {string.Join(" ", mms.OrderBy(m => m, StringComparer.Ordinal))}");
        sb.AppendLine();
        sb.AppendLine("── target kinds each verb is offered on ──");
        foreach (var (verbId, types) in ReachableTypes.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var names = types.Select(t => t.Name.Replace("PointOfInterest", "").Replace("Area", "~"))
                             .Where(n => n.Length > 0)
                             .OrderBy(n => n, StringComparer.Ordinal)
                             .ToList();
            sb.AppendLine($"     {verbId,-18} {string.Join(" ", names)}");
        }
    }

    /// <summary>
    /// Content kinds that exist as a type and that <b>no factory ever builds</b> — the same question
    /// <c>--outcome-audit</c> asks of an <c>Outcome</c> nothing produces, now askable of the world's
    /// furniture because the kinds are types rather than strings.
    ///
    /// <para>This is what a hand-maintained list of assumed words was standing in for. A kind that
    /// exists in code and nowhere in the world is either content somebody meant to place and did
    /// not, or a type left behind by a lesson that was deleted — and both are worth a line.</para>
    /// </summary>
    private static void AuditUnbuiltKinds(StringBuilder sb, List<string> warnings)
    {
        var declared = typeof(PointOfInterest).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(PointOfInterest).IsAssignableFrom(t))
            .ToList();

        // A kind is "built" if the sweep saw one, or saw any subclass of it — a Grave counts as a
        // DiggableGround having been built.
        var unbuilt = declared
            .Where(t => !SeenKinds.Any(seen => t.IsAssignableFrom(seen)))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        sb.AppendLine();
        sb.AppendLine($"── content kinds: {declared.Count} declared, {declared.Count - unbuilt.Count} built by some factory ──");
        for (int i = 0; i < unbuilt.Count; i += 4)
            sb.AppendLine("     " + string.Join("  ", unbuilt.Skip(i).Take(4)));

        foreach (var name in unbuilt.Where(n => !NotPlacedByAFactory.ContainsKey(n)))
            warnings.Add($"content kind '{name}' exists as a type but no sampled factory ever builds one");
    }

    /// <summary>
    /// Kinds that legitimately no factory builds, with the reason. Two sorts: things the game spawns
    /// during play rather than placing at build, and things that belong to the phases this sweep does
    /// not cover. Everything not named here that goes unbuilt is content somebody meant to place.
    ///
    /// <para>Kept as a table rather than a bare list because the reason is the useful half — a name
    /// on a silent exemption list is indistinguishable from an oversight a year later.</para>
    /// </summary>
    private static readonly Dictionary<string, string> NotPlacedByAFactory = new()
    {
        ["CorpsePointOfInterest"]      = "spawned by a kill, never placed at build",
        ["SleepingNpcPointOfInterest"] = "created per night by SceneNpcPlacement, merging a sleeper with their bed",
        ["FragmentPointOfInterest"]    = "childhood reminescence only, which this sweep does not build",
        ["GetUpPointOfInterest"]       = "the get-up phase only, likewise",
        ["BellPointOfInterest"]        = "test location only — --verb-audit deliberately does not sweep it",
        ["MiddenPointOfInterest"]      = "test location only, likewise",
    };

    /// <summary>Every kind the sweep actually constructed, for the check above.</summary>
    private static readonly HashSet<Type> SeenKinds = new();


    /// <summary>Strips ids and quoted names so the same fault at 40 locations counts as one kind.</summary>
    private static string Kind(string warning)
    {
        var body = System.Text.RegularExpressions.Regex.Replace(warning, @"^[A-Z]+ \d+: ", "");
        body     = System.Text.RegularExpressions.Regex.Replace(body, @"'[^']*'", "'…'");
        return System.Text.RegularExpressions.Regex.Replace(body, @"\b\d+\b", "N");
    }

    // ── Declaration checks (registry only, no scenes needed) ──────────────────

    /// <summary>
    /// Verbs that declare no lesson because theirs is decided per execution rather than per verb, and
    /// granted by their own reports. Only <c>attack</c>: what a blow teaches is the modus mentis of
    /// the fighting skill the first blow drew, which is not knowable until the swing is resolved —
    /// see <c>FirstBlowOutcome</c>. Everything not named here that teaches nothing is the dead-content
    /// fault this audit exists to catch.
    /// </summary>
    private static readonly HashSet<string> TeachesPerBlow = new() { "attack" };

    /// <summary>
    /// Verbs whose lesson no <b>beast</b> body can hold, and for which no beast counterpart has been
    /// written yet. Listed rather than warned about, because the gap is known and its fix is content
    /// design rather than a bug: see <c>design/mm_expansion_proposal.md</c>.
    ///
    /// <para>They matter because a beast companion narrates after a Speak-About hand-off — it
    /// observes, thinks and acts like anybody else — so every one of these is a verb a wolf or a cat
    /// can be offered and learn nothing from. The human side of exactly this fault was ten verbs
    /// deep and had been shipping unnoticed, so the list is kept explicit and countable instead of
    /// being allowed back into silence.</para>
    ///
    /// <para><b>Empty this list, do not extend it.</b> A verb added here should be a verb somebody
    /// has decided a beast should learn nothing from, with the reason written down.</para>
    /// </summary>
    private static readonly HashSet<string> NoBeastCounterpart = new()
    {
        "appease", "contemplate", "crush", "follow_path", "get_up", "hide_and_wait",
        "move", "murder", "remember", "slip_into", "steal", "pickpocket", "sit_and_wait",
        "stalk", "swim_across", "voyage_toward",
    };


    /// <summary>
    /// Checks what every verb <i>declares</i>, independently of whether any scene offers it: that it
    /// teaches something, that what it teaches exists, and that the tools it demands exist. All three
    /// fail silently at runtime — an unresolvable lesson grants nothing and an unresolvable tool makes
    /// the verb unperformable — so they are worth catching before a scene is even built.
    /// </summary>
    private static void AuditVerbDeclarations(StringBuilder sb, List<string> warnings)
    {
        var verbs    = VerbRegistry.Instance.GetAll().OrderBy(v => v.VerbId).ToList();
        var itemIds  = ItemRegistry.Instance.All.Select(i => i.ItemId).ToHashSet();
        int teaching = 0, gated = 0;

        foreach (var verb in verbs)
        {
            // The whole candidate list, not the single default: a verb that teaches one lesson to a
            // beast and another to a person declares GrantedModusMentisIds and leaves the singular
            // accessor null, and reading only that reported ten working verbs as teaching nothing.
            var mmIds = verb.GrantedModusMentisIds(null);
            if (mmIds.Count == 0)
            {
                if (!TeachesPerBlow.Contains(verb.VerbId))
                    warnings.Add($"verb '{verb.VerbId}' teaches no modus mentis — succeeding at it grants nothing");
            }
            else
            {
                teaching++;
                foreach (var mmId in mmIds)
                    if (ModusMentisRegistry.Instance.GetModusMentis(mmId) == null)
                        warnings.Add($"verb '{verb.VerbId}' teaches '{mmId}', which no modus mentis answers to");
            }

            // A verb whose candidates no single anatomy can hold teaches that anatomy nothing at all,
            // silently — ModusMentisGrantOutcome refuses the lesson and writes a log line nobody
            // reads. This is the check that was missing when climb, cross, stairs, smell, dig, track
            // and catch all taught the protagonist nothing for as long as they have existed.
            foreach (var anatomy in System.Enum.GetValues<AnatomyType>())
            {
                if (mmIds.Count == 0) break;
                bool anyLearnable = mmIds
                    .Select(id => ModusMentisRegistry.Instance.GetModusMentis(id))
                    .Any(mm => mm != null && ModusMentisAnatomy.IsLearnableBy(mm, anatomy));

                if (anatomy == AnatomyType.Beast && NoBeastCounterpart.Contains(verb.VerbId)) continue;

                if (!anyLearnable && CanBeAttemptedBy(verb, anatomy))
                    warnings.Add($"verb '{verb.VerbId}' teaches {anatomy} nothing — "
                               + $"[{string.Join(", ", mmIds)}] names no source a {anatomy} body owns");
            }

            // A verb naming reference tools it does not require is a category that was changed on one
            // side only: the ids are read nowhere, so the verb quietly stops being gated and the
            // audit's tool-resolution check below stops covering it.
            if (!verb.RequiresTool)
            {
                if (verb.ReferenceToolIds.Count > 0)
                    warnings.Add($"verb '{verb.VerbId}' is {verb.ToolUse} yet names reference tools "
                               + $"({string.Join(", ", verb.ReferenceToolIds)}) — nothing reads them; "
                               + "it is Required that makes a tool obligatory");
                continue;
            }

            gated++;
            // The inverse, and the worse of the two: Required with nothing named refuses every
            // attempt ("I would need a tool for that") and can never be satisfied by anything.
            if (verb.ReferenceToolIds.Count == 0)
                warnings.Add($"verb '{verb.VerbId}' is Required but names no reference tool — "
                           + "RequiredToolRule refuses it and no item can ever answer");

            foreach (var toolId in verb.ReferenceToolIds.Where(t => !itemIds.Contains(t)))
                warnings.Add($"verb '{verb.VerbId}' requires tool '{toolId}', which no item answers to — the verb can never be performed");
        }

        int excluded = verbs.Count(v => v.ToolUse == ToolUsage.Excluded);
        int optional = verbs.Count(v => v.ToolUse == ToolUsage.Optional);

        sb.AppendLine("--- VERB DECLARATIONS ---");
        sb.AppendLine($"  {verbs.Count} verb(s) registered; {teaching} teach a modus mentis");
        sb.AppendLine($"  implements: {excluded} excluded, {optional} optional, {gated} required");
        sb.AppendLine();

        // The excluded list is worth printing in full rather than counted. It is the one declaration
        // here that is pure judgement — every other warning in this audit has a right answer the code
        // can check — so the only review it can get is a reader running an eye down it.
        sb.AppendLine("--- IMPLEMENTS: EXCLUDED ---");
        sb.AppendLine("  (no implement may be combined; only an item MADE FOR one of these gets in)");
        foreach (var line in Chunk(verbs.Where(v => v.ToolUse == ToolUsage.Excluded).Select(v => v.VerbId), 5))
            sb.AppendLine($"      {line}");
        sb.AppendLine();

        sb.AppendLine("--- IMPLEMENTS: REQUIRED ---");
        foreach (var verb in verbs.Where(v => v.ToolUse == ToolUsage.Required))
            sb.AppendLine($"      {verb.VerbId,-12} {string.Join(" / ", verb.ReferenceToolIds)}");
        sb.AppendLine();
    }

    /// <summary>Groups ids into fixed-width rows, for a list read rather than scanned.</summary>
    private static IEnumerable<string> Chunk(IEnumerable<string> ids, int perRow)
    {
        var row = new List<string>();
        foreach (var id in ids)
        {
            row.Add(id.PadRight(24));
            if (row.Count == perRow) { yield return string.Concat(row).TrimEnd(); row.Clear(); }
        }
        if (row.Count > 0) yield return string.Concat(row).TrimEnd();
    }

    // ── Per-factory coverage sweep ────────────────────────────────────────────

    private static void AuditFactory(
        StringBuilder sb, List<string> warnings, HashSet<string> everOffered,
        string label, Func<int, Scene> build)
    {
        // Verb counts across every observable of every sampled location, one entry per observable.
        var counts        = new List<int>();
        int sensoryCovered = 0;

        // A stand-in actor. Verbs that gate on the player — the dialogue verbs want somebody with a
        // speaking modus mentis — need one, and a default protagonist is the plainest possible player.
        var actor = new Protagonist();

        for (int id = 1; id <= SampleSize; id++)
        {
            Scene scene;
            try
            {
                scene = build(id);
            }
            catch (Exception ex)
            {
                warnings.Add($"{label} {id}: generation threw — {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            SweepAreaVerbs(scene, everOffered, actor);

            // A climb has to be worth making. Every area reachable only by scaling something is
            // there for what it offers at the top — the tops the old automatic pass produced were
            // documented as "deliberately bare: what it is for is the view", so one that ends up
            // with nothing on it is an empty room the player paid a roll to reach.
            foreach (var climb in scene.AllAreas
                         .SelectMany(a => a.PointsOfInterest)
                         .OfType<Building.ScalePointOfInterest>()
                         .Distinct())
            {
                int atTop = climb.TopArea.PointsOfInterest.Count(p => p != climb);
                if (atTop == 0)
                    warnings.Add($"{label} {id}: '{climb.TopArea.DisplayName}' is reachable only by "
                                 + $"climbing '{climb.DisplayName}' and has nothing on it — a roll paid for an empty room");
            }

            foreach (var area in scene.AllAreas)
            {

                foreach (var observable in Observables(area, scene))
                {
                    if (observable is PointOfInterest p) SeenKinds.Add(p.GetType());

                    // Exercise the lessons so a branch that throws is caught here rather than in play.
                    // Every period, because the hour changes which branch is reached.
                    foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
                    foreach (var verb in scene.Verbs)
                    {
                        var ctx = new LessonContext(scene, new PoV(area, period), actor, observable);
                        try { verb.Lessons(ctx).ToList(); } catch { }

                        // What the verb would ACTUALLY grant here — the first candidate a body can
                        // hold, not every candidate offered. Only where the verb is genuinely
                        // offered: asking what BREAK would teach about a tree it can never break
                        // records the default as reachable when it is not.
                        try
                        {
                            if (verb.IsPossible(scene, new PoV(area, period), observable, actor))
                            {
                                var won = verb.ResolveLesson(ctx);
                                if (won != null) Granted(verb.VerbId).Add(won.ModusMentisId);

                                // What this object DECLARES for this verb, against what it actually
                                // taught. base.Lessons yields the declaration before the verb default
                                // but after every branch, so a branch matching the object's own type
                                // silences the declaration completely.
                                var declared = (observable as IVerbModusMentisSource)?.ModusMentisFor(verb.VerbId);

                                // A declaration this body cannot HOLD is a different thing entirely
                                // and is correct by design — footprints declare the beast's
                                // spoor_reading, and a human is meant to fall past it.
                                var declaredMm = declared == null
                                    ? null
                                    : ModusMentisRegistry.Instance.GetModusMentis(declared);
                                if (declaredMm != null && ModusMentisAnatomy.IsLearnableBy(declaredMm, actor))
                                {
                                    DeclaredSeen[(verb.VerbId, declared!)] = observable.DisplayName;
                                    if (won?.ModusMentisId == declared)
                                        DeclaredWon.Add((verb.VerbId, declared!));
                                }
                            }
                        }
                        catch { }
                    }

                    var offered = OfferedVerbIds(scene, area, observable, actor);
                    foreach (var verbId in offered) everOffered.Add(verbId);

                    // What each verb is ever offered ON. A lesson names a type; if the verb's own
                    // gate never admits that type, the lesson cannot fire however much of the content
                    // the world contains — see AuditUnreachableLessons.
                    foreach (var verbId in offered)
                    {
                        Reachable(verbId).Add(observable.GetType());
                        Reachable(verbId).Add(area.GetType());
                        if (observable is PointOfInterest holder)
                            foreach (var item in holder.Items)
                                if (OfferedVerbIds(scene, area, item, actor).Contains(verbId))
                                    Reachable(verbId).Add(holder.GetType());
                    }

                    counts.Add(offered.Count);
                    if (offered.Overlaps(SensoryVerbIds)) sensoryCovered++;

                    if (offered.Count == 0)
                        warnings.Add($"{label} {id}: '{observable.DisplayName}' in '{area.DisplayName}' "
                                   + $"has no verb at any period — it is prose only{WhyNoVerb(scene, area, observable)}");

                    CheckTargetOverrides(warnings, label, id, observable);
                }
            }
        }

        if (counts.Count == 0)
        {
            sb.AppendLine($"  {label,-10} (nothing generated)");
            return;
        }

        double one   = counts.Count(c => c >= 1) / (double)counts.Count;
        double two   = counts.Count(c => c >= 2) / (double)counts.Count;
        double three = counts.Count(c => c >= 3) / (double)counts.Count;
        double sense = sensoryCovered / (double)counts.Count;

        sb.AppendLine($"  {label,-10} {counts.Count,11} " +
                      $"{Pct(one),6} {Pct(two, TargetTwoPlusVerbs),6} {Pct(three, TargetThreePlusVerbs),6} " +
                      $"{Pct(sense, TargetSensory),8}  {counts.Average(),5:F1}");
    }

    /// <summary>A percentage, marked with '*' when it falls short of a target it is measured against.</summary>
    private static string Pct(double value, double? target = null)
    {
        string text = $"{value * 100:F0}%";
        return target.HasValue && value < target.Value ? text + "*" : text;
    }

    /// <summary>
    /// Everything in an area a player can put a keyword on: its points of interest and the NPCs
    /// scheduled anywhere in it. Items are deliberately left out — they are never standalone
    /// observations, their verbs fold into the parent PoI.
    /// </summary>
    /// <summary>
    /// Why an observable ended up with no verb, when the answer is structural rather than a content
    /// gap. Empty for a plain point of interest with nothing to do to it — that is the finding this
    /// warning was written for, and it needs no elaboration.
    ///
    /// <para>An NPC is different: <c>attack</c> accepts any live person standing in front of you, so
    /// an NPC with <i>no</i> verb at <i>any</i> period is never a content gap — something has made
    /// them unreachable. The two ways that happens are the two ways <see cref="Observables"/> and
    /// <c>Scene.GetNpcsAt</c> can disagree about where somebody is, and saying which one it was is the
    /// difference between a warning you can act on and a name in a list.</para>
    /// </summary>
    private static string WhyNoVerb(Scene scene, Area area, Element observable)
    {
        if (observable is not SceneNpc npc) return "";

        if (!npc.IsAlive) return " — the NPC is not alive, so GetNpcsAt never returns them";

        if (!scene.NpcSchedules.TryGetValue(npc.Id, out var schedule))
            return " — the NPC has no schedule entry, so GetNpcsAt never returns them";

        var listedHere = schedule.ActivePeriods
            .Where(p => p.Area.Id == area.Id)
            .Select(p => p.Period.ToString())
            .ToList();
        var presentHere = Enum.GetValues<TimePeriod>()
            .Where(p => scene.GetNpcsAt(area, p).Exists(n => n.Id == npc.Id))
            .Select(p => p.ToString())
            .ToList();

        if (presentHere.Count == 0)
            return $" — scheduled here at [{string.Join(", ", listedHere)}] but GetNpcsAt places them "
                 + $"here at no period (schedule says: {ScheduleSummary(schedule)})";

        return $" — present here at [{string.Join(", ", presentHere)}] and still no verb applies";
    }

    /// <summary>Period → area, for reading a schedule back in a warning.</summary>
    private static string ScheduleSummary(Cathedral.Game.Narrative.NpcSchedule schedule)
        => string.Join("; ", Enum.GetValues<TimePeriod>()
            .Select(p => $"{p}={schedule.GetArea(p)?.DisplayName ?? "-"}"));

    private static IEnumerable<Element> Observables(Area area, Scene scene)
    {
        foreach (var poi in area.PointsOfInterest) yield return poi;

        foreach (var npc in scene.Npcs)
        {
            if (!scene.NpcSchedules.TryGetValue(npc.Id, out var schedule)) continue;
            if (schedule.ActivePeriods.Any(p => p.Area.Id == area.Id))
                yield return npc;
        }
    }

    /// <summary>
    /// Every verb that accepts <paramref name="target"/> from <paramref name="area"/> at any period.
    ///
    /// <para>Sampled across all six periods because presence, lock state and schedule all move with
    /// the clock: a verb that only exists at night is still a verb this object has. A default
    /// protagonist stands in for the actor — several verbs gate on one (the dialogue verbs need
    /// somebody with a speaking modus mentis to be speaking), and passing null there silently zeroed
    /// every conversation out of the count.</para>
    /// </summary>
    private static HashSet<string> OfferedVerbIds(Scene scene, Area area, Element target, Protagonist actor)
    {
        var offered = new HashSet<string>();
        SleepingNpcPointOfInterest? addedSleeper = null;

        // An item is never an observation of its own: SceneViewAdapter folds each item's verb views
        // into its holding PoI's action list. So the actions a PoI offers are its own plus those of
        // everything in it, and counting only its own said a well-stocked tree had one verb when the
        // player is looking at five.
        var probes = new List<Element> { target };
        if (target is PointOfInterest holder) probes.AddRange(holder.Items);

        foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
        {
            var pov = new PoV(area, period);

            // A sleeping person is not observed as an NPC: placement swaps them and their bed for a
            // single merged object while the sleep lasts, and murder / wake / pickpocket are offered
            // on that. Probing only the NPC form would report those three as reachable while the
            // shape the player actually meets went untested.
            var sleeperProbe = addedSleeper ??= SleeperProbe(scene, area, target, period);
            if (sleeperProbe != null) SleepersProbed++;
            var thisPeriod   = sleeperProbe == null ? probes : probes.Append<Element>(sleeperProbe).ToList();

            foreach (var probe in thisPeriod)
            foreach (var verb in scene.Verbs)
            {
                try
                {
                    if (verb.IsPossible(scene, pov, probe, actor)) offered.Add(verb.VerbId);
                }
                catch (Exception ex)
                {
                    // A verb that throws on an element it does not recognise would crash the real
                    // action menu the moment that element is observed, so this is a finding, not noise.
                    offered.Add($"!{verb.VerbId}");
                    Console.Error.WriteLine(
                        $"VerbAudit: '{verb.VerbId}'.IsPossible threw on '{probe.DisplayName}' — {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        if (addedSleeper != null) area.PointsOfInterest.Remove(addedSleeper);
        return offered;
    }

    /// <summary>
    /// Builds the merged sleeping observation for <paramref name="target"/> when that NPC would be
    /// asleep here at this hour, and puts it in the area so the verbs that gate on presence can see
    /// it. Mirrors what <c>SceneNpcPlacement</c> does at runtime; the caller takes it out again.
    /// </summary>
    private static SleepingNpcPointOfInterest? SleeperProbe(Scene scene, Area area, Element target, TimePeriod period)
    {
        if (target is not SceneNpc npc) return null;
        if (!npc.IsSleeping(scene, new PoV(area, period))) return null;

        var bed = Building.BuildingRooms.BedsIn(area).FirstOrDefault();
        if (bed == null) return null;

        var merged = new SleepingNpcPointOfInterest(npc, bed);
        area.PointsOfInterest.Add(merged);
        return merged;
    }

    /// <summary>
    /// Records the verbs offered on the areas themselves, which are observations too — every area
    /// bordering you is listed, carrying <c>move</c>. Not folded into the per-observable coverage
    /// figures (an area is a destination, not a thing in a room), but it has to be swept or the
    /// reach check reports the game's most-used verb as dead content.
    /// </summary>
    private static void SweepAreaVerbs(Scene scene, HashSet<string> everOffered, Protagonist actor)
    {
        foreach (var from in scene.AllAreas)
        foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
        {
            var pov = new PoV(from, period);
            foreach (var to in scene.AllAreas)
            foreach (var verb in scene.Verbs)
            {
                try
                {
                    if (verb.IsPossible(scene, pov, to, actor)) everOffered.Add(verb.VerbId);
                }
                catch { /* reported by the per-observable sweep */ }
            }
        }
    }

    /// <summary>
    /// Checks that a target's per-verb modus mentis overrides name verbs that exist and lessons that
    /// resolve. An override keyed to a misspelt verb id is never consulted, and one naming a
    /// non-existent modus mentis silently teaches nothing — both leave the object looking correct.
    /// </summary>
    private static void CheckTargetOverrides(List<string> warnings, string label, int id, Element observable)
    {
        // Both halves of IVerbModusMentisSource: an object's own declaration and a creature's,
        // the latter declared per archetype. Reading only the first left every person's and every
        // beast's lesson unchecked, which is where the mechanism is newest and least trodden.
        var overrides = observable switch
        {
            PointOfInterest { VerbModiMentis: { } poi } => poi,
            SceneNpc npc                                => npc.Entity.Archetype.VerbModiMentis,
            _                                           => null,
        };
        if (overrides == null) return;

        foreach (var (verbId, mmId) in overrides)
        {
            if (VerbRegistry.Instance.Get(verbId) == null)
                warnings.Add($"{label} {id}: '{observable.DisplayName}' overrides the lesson for verb '{verbId}', which is not a registered verb");

            if (ModusMentisRegistry.Instance.GetModusMentis(mmId) == null)
                warnings.Add($"{label} {id}: '{observable.DisplayName}' teaches '{mmId}' for '{verbId}', which no modus mentis answers to");

            // A lesson declared for a sense the object does not reward is never consulted: the verb
            // is not even offered. The declaration reads as content and does nothing — nine objects
            // declared what contemplating them teaches while rewarding only examine and listen.
            if (SensoryVerbIds.Contains(verbId)
                && observable is PointOfInterest poi && !poi.RewardsSense(verbId))
                warnings.Add($"{label} {id}: '{observable.DisplayName}' declares a lesson for '{verbId}' "
                           + "but its SensoryProfile does not reward that sense, so the verb is never offered on it");
        }
    }

    // ── Dead verbs ────────────────────────────────────────────────────────────

    /// <summary>
    /// Verbs the registry knows about that no sampled scene ever offered. Either nothing places their
    /// target yet, or their gate is unsatisfiable — both mean the verb is currently unreachable.
    /// Phase-scoped verbs are exempt: their scenes are built ad hoc and are not in the sweep.
    /// </summary>
    private static void AuditDeadVerbs(StringBuilder sb, List<string> warnings, HashSet<string> everOffered)
    {
        // Verbs the sweep cannot legitimately reach, and why. Excluded from the dead list because
        // they are not dead — reporting them sends the reader looking for scene content that is not
        // missing.
        var unreachable = new Dictionary<string, string>
        {
            // Phase-scoped: their scenes are built ad hoc and are not in this sweep.
            ["remember"] = "childhood phase only",
            ["get_up"]   = "get-up phase only",
            ["ignore"]   = "injected directly, never registry-discovered",
            ["cut"]      = "needs a corpse, which only exists after a kill",

            // Relationship-gated: the stand-in actor is a stranger to everyone, by construction.
            ["appease"]                 = "needs an enemy or an annoyed acquaintance",
            ["propose_to_join"]         = "needs close-acquaintance-or-better, and room in the party",
            ["tame"]                    = "needs a beast already appeased",
            ["reconcile"]               = "needs an enemy or an annoyed acquaintance",
            ["strengthen_relationship"] = "needs a non-stranger",
            ["gather_knowledge"]        = "needs a non-stranger",
            ["propose_to_buy"]          = "needs acquaintance-or-better with a seller",
            ["propose_to_sell"]         = "needs acquaintance-or-better with a buyer",
            ["request_job"]             = "needs acquaintance-or-better with an employer",
        };
        var phaseScoped = unreachable.Keys.ToHashSet();

        var dead = VerbRegistry.Instance.GetAll()
            .Select(v => v.VerbId)
            .Where(id => !everOffered.Contains(id) && !phaseScoped.Contains(id))
            .OrderBy(id => id)
            .ToList();

        sb.AppendLine();
        sb.AppendLine("--- REACH ---");
        sb.AppendLine($"  {everOffered.Count} verb(s) offered somewhere in the sample");
        sb.AppendLine($"  {SleepersProbed} merged sleeping observation(s) exercised");

        // Listed in full here rather than left to the by-kind warning summary, which shows three
        // examples per group. This set is small, it is the to-do list for scene content, and seeing
        // three of nineteen entries is worse than useless.
        var gated = unreachable.Keys
            .Where(id => !everOffered.Contains(id) && VerbRegistry.Instance.Get(id) != null)
            .OrderBy(id => id)
            .ToList();
        if (gated.Count > 0)
        {
            sb.AppendLine($"  {gated.Count} verb(s) not reached by this sweep, legitimately:");
            foreach (var id in gated)
                sb.AppendLine($"      {id,-24} {unreachable[id]}");
        }

        if (dead.Count > 0)
        {
            sb.AppendLine($"  {dead.Count} verb(s) NEVER offered — nothing places their target:");
            foreach (var id in dead)
                sb.AppendLine($"      {id}");
        }

        foreach (var id in dead)
            warnings.Add($"verb '{id}' is never offered by any sampled location — nothing places its target");
    }

    /// <summary>
    /// What each anatomy is allowed to attempt, and what its body rules out
    /// (<c>Verb.RequiredCapabilities</c>). Not a warning: a beast being barred from twenty-four verbs
    /// is the design, not a fault. It is here so the cost of that design stays visible — and so the
    /// next anatomy's poverty is one line to read rather than something to work out from the source.
    /// </summary>
    private static void AuditAnatomyReach(StringBuilder sb)
    {
        var verbs = VerbRegistry.Instance.GetAll().OrderBy(v => v.VerbId).ToList();

        sb.AppendLine();
        sb.AppendLine("--- ANATOMY ---");
        foreach (var anatomy in ModusMentisAnatomy.AllAnatomies)
        {
            // EffectiveCapabilities, not RequiredCapabilities: a Required verb implies handcraft
            // whether or not it says so, and reading the declared half here would report a beast as
            // able to SLAY — which is precisely the gap this audit exists to make visible.
            var caps    = AnatomyFactoryRegistry.GetFactory(anatomy).Capabilities;
            var blocked = verbs.Where(v => (caps & v.EffectiveCapabilities) != v.EffectiveCapabilities)
                               .Select(v => v.VerbId).ToList();

            sb.AppendLine($"  {anatomy,-6} can [{caps}] — {verbs.Count - blocked.Count}/{verbs.Count} verbs");
            if (blocked.Count > 0)
                sb.AppendLine($"      barred: {string.Join(", ", blocked)}");
        }
    }
}
