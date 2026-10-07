using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cathedral.Glyph;
using Cathedral.Glyph.Microworld;
using Cathedral.Game.History.Lore;
using Cathedral.Game.History.Worlds;

namespace Cathedral.Game.History;

/// <summary>
/// <c>--history-audit</c>: checks the empire's hardcoded history against its own rules, then
/// generates the history of every lore world and a sample of the others, headless, and checks each for
/// the incoherences a generator produces without complaint.
///
/// <para><b>Why it exists.</b> A history is thousands of events, and every way of getting one wrong is
/// silent: a ruler crowned after his death, a port on a landlocked region, a realm that holds nothing,
/// a Register numeral with a gap below it, a lore beat that never sprouted because its seed was sown in
/// the past, a history that comes out differently on the second run (which Continue would refuse, and
/// nobody would know why). Every one is a comparison away from being caught here.</para>
/// </summary>
public static class HistoryAudit
{
    /// <summary>Generic moons per imperial status, taken in sky order.</summary>
    private const int GenericSamplesPerStatus = 4;

    /// <summary>Unnamed members of each lore group (Oox's, Fogun's) to include.</summary>
    private const int UnnamedGroupSamples = 2;

    /// <summary>Generation slower than this is reported as a fault: it runs on every New and Continue.</summary>
    private const long SlowMilliseconds = 2000;

    /// <summary>Fewest realms a world of twenty regions or more may stand divided into at the present.</summary>
    private const int MinStandingRealms = 3;

    public static string BuildReport()
    {
        var sb = new StringBuilder();
        var faults = new List<string>();
        sb.AppendLine("HISTORY AUDIT");
        sb.AppendLine("=============");
        sb.AppendLine();

        var lore = EmpireLore.Instance;
        CheckEmpire(sb, faults, lore);
        string empireFingerprint = Fingerprint(lore);

        sb.AppendLine("Building the sphere...");
        var (positions, triangles) = IcosphereGeometry.Build(Config.GlyphSphere.SphereSubdivisions, Config.GlyphSphere.SphereRadius);
        var adjacency = IcosphereGeometry.Adjacency(positions.Count, triangles);
        sb.AppendLine($"  {positions.Count} vertices.");
        sb.AppendLine();

        sb.AppendLine("WORLDS");
        sb.AppendLine("------");
        sb.AppendLine("  moon  world            status     variant       regions  events  figures  realms(now)  faiths(now/hid)  orgs(now/hid)  places  wars   ms  hash");
        WorldHistory? showcase = null;
        var afterEmpire = new List<WorldHistory>();
        foreach (var (ordinal, expectedName, expectedVariant) in Samples())
        {
            int seed = SkyMoons.WorldSeed(ordinal);
            var world = HeadlessWorld.Build(seed, positions, adjacency);
            var geography = HistoryGeography.Build(world.Regions, world.VertexCount, v => world.Adjacency[v], v => world.Biome[v],
                                                   world.Variant.Shape.SettlementDensity);

            WorldHistory history, again;
            try
            {
                history = WorldHistoryGenerator.Generate(geography, seed);
                again = WorldHistoryGenerator.Generate(geography, seed);
            }
            catch (Exception ex)
            {
                faults.Add($"moon {ordinal}: generation threw {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            sb.AppendLine($"  {ordinal,4}  {Trim(history.World.Name, 15),-15}  {history.World.Status,-9}  {world.Variant.Id,-12}  "
                        + $"{geography.Regions.Count,7}  {history.Chronology.Count,6}  {history.Figures.Count,7}  "
                        + $"{history.Realms.Count,5}({history.LivingRealms.Count(),3})  "
                        + $"{history.Religions.Count,6}({history.LivingFaiths.Count(),2}/{history.LivingFaiths.Count(f => history.PresenceOf(f) == FaithPresence.Clandestine),2})  "
                        + $"{history.Organisations.Count,6}({history.LivingOrganisations.Count(),2}/{history.LivingOrganisations.Count(o => o.Clandestine),2})  "
                        + $"{history.Places.Count,6}  {history.Wars.Count,4}  {history.GenerationMilliseconds,4}  {history.Hash:X8}");

            string where = $"moon {ordinal} ({history.World.Name})";
            if (again.Hash != history.Hash)
                faults.Add($"{where}: generated twice from one seed, hashed {history.Hash:X8} then {again.Hash:X8} - Continue would refuse every save on it");
            if (expectedName != null && history.World.Name != expectedName)
                faults.Add($"{where}: the lore names this world {expectedName}");
            if (expectedVariant != null && world.Variant.GetType() != expectedVariant)
                faults.Add($"{where}: built {world.Variant.Id}, the lore forces {expectedVariant.Name}");
            if (history.GenerationMilliseconds > SlowMilliseconds)
                faults.Add($"{where}: generation took {history.GenerationMilliseconds} ms (ceiling {SlowMilliseconds}) - it runs on every New and Continue");
            CheckWorld(faults, where, history);

            if (showcase == null && history.Profile is BeluneProfile) showcase = history;
            if (afterEmpire.Count < 3 && history.Profile is GenericWorldProfile && history.World.Status == ImperialStatus.Held)
                afterEmpire.Add(history);
        }
        sb.AppendLine();

        // What became of the empire's branches and hidden cells: the one place a branch's successor and
        // a cell's independence can be read side by side. Lineage is the ImperialCounterpart an heir keeps.
        foreach (var h in afterEmpire)
        {
            sb.AppendLine($"SAMPLE: the empire's branches and cells on {h.World.Name}, and what became of them");
            sb.AppendLine("----------------------------------");
            foreach (var o in h.Organisations.Where(o => o.ImperialCounterpart != null))
            {
                string end = o.Dissolved.IsKnown
                    ? $"ended {o.Dissolved} ({o.Events.OfType<FactionDissolvedEvent>().LastOrDefault()?.How ?? "?"})"
                    : o.Clandestine ? "standing, in hiding" : "standing";
                var independent = o.Events.OfType<OrganisationEvent>().FirstOrDefault(e => e.Change == OrganisationChange.BecameIndependent);
                sb.AppendLine($"  {o.Name} [{o.Kind}, of {o.ImperialCounterpart!.Name}] {o.Founded} -> {end}");
                if (independent != null) sb.AppendLine($"      {independent.Date}: {independent.Describe()}");
            }
            sb.AppendLine();
        }
        int branches = afterEmpire.Sum(h => h.Organisations.Count(o => o.Kind == OrganisationKind.ImperialBranch));
        int heirs = afterEmpire.Sum(h => h.Organisations.Count(o => o.Kind != OrganisationKind.ImperialBranch && o.ImperialCounterpart != null
                                                                   && o.Founded.IsKnown && o.Founded.Round > HistoryCalendar.HatchingRound));
        if (afterEmpire.Count > 0 && branches > 0 && heirs == 0)
            faults.Add($"{branches} imperial branches across {afterEmpire.Count} sampled held worlds and not one survived the empire as a faction of its own");

        if (Fingerprint(lore) != empireFingerprint)
            faults.Add("the empire catalogue changed while worlds were generated - a world wrote into the shared lore (a death, a child, a marriage)");

        if (showcase != null)
        {
            sb.AppendLine($"SAMPLE: the imperial chapter of {showcase.World.Name}");
            sb.AppendLine("----------------------------------");
            // The lore beats and the empire's steps; the native coronations would drown them.
            foreach (var e in showcase.Chronology.Events.Where(e => e is ImperialEvent or ChronicleEvent && e.Date.Round >= -1000).Take(50))
                sb.AppendLine($"  {e}");
            sb.AppendLine();
            sb.AppendLine($"SAMPLE: hidden faiths and factions of {showcase.World.Name}");
            sb.AppendLine("----------------------------------");
            foreach (var e in showcase.Chronology.Events.Where(e => e is FaithPresenceEvent or OrganisationEvent
                                                                    || e is FactionFoundedEvent { Faction: Organisation }
                                                                    || e is FactionDissolvedEvent { Faction: Organisation }).Take(45))
                sb.AppendLine($"  {e}");
            sb.AppendLine();
        }

        sb.AppendLine("VERDICT");
        sb.AppendLine("-------");
        if (faults.Count == 0) sb.AppendLine("  No faults.");
        else
        {
            // The cross run_tests.sh greps for.
            sb.AppendLine($"  {faults.Count} fault(s):");
            foreach (var f in faults) sb.AppendLine($"    ✗ {f}");
        }
        return sb.ToString();
    }

    private static string Trim(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + ".";

    /// <summary>Every named lore moon, a few unnamed group members, and the first generic moons of each status.</summary>
    private static IEnumerable<(int Ordinal, string? Name, Type? Variant)> Samples()
    {
        foreach (var e in LoreMoons.Named) yield return (e.Ordinal, e.Name, e.Variant);

        foreach (var group in LoreMoons.All.Where(e => e.Name.Length == 0).GroupBy(e => e.Profile().GetType()))
            foreach (var e in group.Take(UnnamedGroupSamples))
                yield return (e.Ordinal, null, null);

        var taken = new Dictionary<ImperialStatus, int>();
        for (int ordinal = 0; ordinal < 383; ordinal++)
        {
            int seed = SkyMoons.WorldSeed(ordinal);
            if (LoreMoons.ForSeed(seed) != null) continue;
            var status = GenericWorldProfile.StatusForSeed(seed);
            int count = taken.TryGetValue(status, out int c) ? c : 0;
            if (count >= GenericSamplesPerStatus) continue;
            taken[status] = count + 1;
            yield return (ordinal, null, null);
        }
    }

    // ── The empire ──────────────────────────────────────────────────────────────

    private static readonly Regex Numbered = new(@"^(\w+) ([IVXL]+) Ban", RegexOptions.Compiled);

    private static void CheckEmpire(StringBuilder sb, List<string> faults, EmpireLore lore)
    {
        sb.AppendLine("EMPIRE");
        sb.AppendLine("------");
        sb.AppendLine($"  {lore.Figures.Count} figures, {lore.Factions.Count} factions, {lore.Religions.Count} faiths, "
                    + $"{lore.Worlds.Count} worlds, {lore.Places.Count} places, {lore.Wars.Count} wars, "
                    + $"{lore.Chronology.Count} events.");

        // The Register of Blood: a numbered name implies every lower number, in birth order.
        var byName = new Dictionary<string, List<(int Number, HistoricFigure Figure)>>();
        foreach (var f in lore.Figures)
        {
            var m = Numbered.Match(f.Name);
            if (!m.Success) continue;
            if (!byName.TryGetValue(m.Groups[1].Value, out var list)) byName[m.Groups[1].Value] = list = new();
            list.Add((Roman(m.Groups[2].Value), f));
        }
        foreach (var (name, list) in byName.OrderBy(kv => kv.Key))
        {
            var numbers = list.Select(x => x.Number).OrderBy(x => x).ToList();
            for (int i = 0; i < numbers.Count; i++)
                if (numbers[i] != i + 1) { faults.Add($"Register: {name} has numbers {string.Join(",", numbers)} - a gap or a double"); break; }
            var byBirth = list.Where(x => x.Figure.Born.IsKnown).OrderBy(x => x.Figure.Born.Round).Select(x => x.Number).ToList();
            if (!byBirth.SequenceEqual(byBirth.OrderBy(x => x)))
                faults.Add($"Register: {name}'s numerals are not in birth order ({string.Join(",", byBirth)})");
        }
        sb.AppendLine($"  Register: {byName.Count} numbered names, {byName.Sum(kv => kv.Value.Count)} bearers.");

        foreach (var f in lore.Figures)
        {
            if (f.Born.IsKnown && f.Died.IsKnown && f.Died.Round < f.Born.Round)
                faults.Add($"{f.Name}: dies ({f.Died}) before being born ({f.Born})");
            foreach (var p in f.Parents)
            {
                if (!p.Born.IsKnown || !f.Born.IsKnown) continue;
                if (f.Born.Round < p.Born.Round + 12)
                    faults.Add($"{f.Name} (b. {f.Born}) is born when their parent {p.Name} (b. {p.Born}) is under twelve");
                if (p.Died.IsKnown && f.Born.Round > p.Died.Round + 1)
                    faults.Add($"{f.Name} (b. {f.Born}) is born after their parent {p.Name} died ({p.Died})");
            }
        }
        foreach (var a in lore.Chronology.Events.OfType<AccessionEvent>())
            if (!a.Figure.IsAliveAt(a.Date.Round) && a.Figure.Born.IsKnown)
                faults.Add($"{a.Figure.Name} accedes as {a.Title} in {a.Date}, outside their life ({a.Figure.Born} to {a.Figure.Died})");
        sb.AppendLine();
    }

    private static int Roman(string s)
    {
        var v = new Dictionary<char, int> { ['I'] = 1, ['V'] = 5, ['X'] = 10, ['L'] = 50 };
        int total = 0;
        for (int i = 0; i < s.Length; i++)
        {
            int cur = v[s[i]];
            if (i + 1 < s.Length && v[s[i + 1]] > cur) total -= cur; else total += cur;
        }
        return total;
    }

    /// <summary>What a world could corrupt in the shared catalogue: deaths, kin, event lists.</summary>
    private static string Fingerprint(EmpireLore lore)
        => string.Join("|", lore.Figures.Select(f => $"{f.Died}/{f.Children.Count}/{f.Spouses.Count}/{f.Events.Count}"))
         + string.Join("|", lore.Factions.Select(f => f.Events.Count))
         + string.Join("|", lore.Religions.Select(r => $"{r.Events.Count}/{r.Proscribed}"));

    // ── A world ─────────────────────────────────────────────────────────────────

    private static void CheckWorld(List<string> faults, string where, WorldHistory h)
    {
        int present = HistoryCalendar.PresentRound;
        var regions = h.Geography.Regions;

        // Ownership: the table and each realm's own set agree, and no standing realm is empty.
        foreach (var r in regions)
        {
            var owner = h.OwnerOf(r.Id);
            if (owner != null && !owner.Regions.Contains(r.Id))
                faults.Add($"{where}: region {r.Id} is owned by {owner.Name}, which does not list it");
        }
        foreach (var realm in h.Realms)
        {
            foreach (int r in realm.Regions)
                if (h.OwnerOf(r) != realm) faults.Add($"{where}: {realm.Name} lists region {r}, which belongs to {h.OwnerOf(r)?.Name ?? "no one"}");
            if (realm.Dissolved.IsKnown && realm.Regions.Count > 0)
                faults.Add($"{where}: {realm.Name} was dissolved in {realm.Dissolved} and still holds {realm.Regions.Count} region(s)");
            if (realm.Founded.IsKnown && realm.Dissolved.IsKnown && realm.Dissolved.Round < realm.Founded.Round)
                faults.Add($"{where}: {realm.Name} ends before it begins");
        }

        // People.
        foreach (var f in h.Figures)
        {
            if (f.Born.IsKnown && f.Died.IsKnown && f.Died.Round < f.Born.Round)
                faults.Add($"{where}: {f.Name} dies before being born");
            if (f.Born.IsKnown && !f.Died.IsKnown && present - f.Born.Round > 120)
                faults.Add($"{where}: {f.Name}, born {f.Born}, is still alive at {present - f.Born.Round} rounds");
        }
        foreach (var a in h.Chronology.Events.OfType<AccessionEvent>())
            // Strictly before: crowned and dead in the same round is a short reign, not a crowned corpse.
            if (a.Figure.Scope == HistoryScope.World && a.Figure.Died.IsKnown && a.Figure.Died.Round < a.Date.Round)
                faults.Add($"{where}: {a.Figure.Name} is crowned in {a.Date} after dying in {a.Figure.Died}");

        // Places against the ground they stand on: in their region, on a cell their kind fits, alone on it.
        var occupied = new Dictionary<int, Place>();
        foreach (var p in h.Places)
        {
            if (p.Region < 0 || p.Region >= regions.Count) { faults.Add($"{where}: {p.Name} stands in no region"); continue; }
            if (p.Ruined.IsKnown && p.Founded.IsKnown && p.Ruined.Round < p.Founded.Round)
                faults.Add($"{where}: {p.Name} is ruined before it is founded");
            if (p.Vertex < 0) continue;
            var cell = regions[p.Region].Cells.FirstOrDefault(c => c.Vertex == p.Vertex);
            if (cell.Biome == null)
                faults.Add($"{where}: {p.Name} stands on vertex {p.Vertex}, outside its region {p.Region}");
            else if (!PlaceSites.Fits(p.Kind, cell))
                faults.Add($"{where}: the {p.Kind} {p.Name} stands on {cell.Biome}, which a {p.Kind} cannot stand on");
            if (occupied.TryGetValue(p.Vertex, out var other))
                faults.Add($"{where}: {p.Name} and {other.Name} share vertex {p.Vertex}");
            else occupied[p.Vertex] = p;
        }

        // Every realm that rose on its own builds its founder a castle; one standing a generation and
        // more without one is a castle seed that never sprouted though the realm had room for it.
        foreach (var realm in h.Realms.Where(r => r.RoseOnFreeLand && r.Founded.IsKnown))
        {
            bool lived = (realm.Dissolved.IsKnown ? realm.Dissolved.Round : present) - realm.Founded.Round > 20;
            bool hasCastle = h.Places.Any(pl => pl.Kind == PlaceKind.Castle && pl.Builder == realm);
            bool couldBuild = realm.Regions.Any(r => regions[r].Cells.Any(c => PlaceSites.Fits(PlaceKind.Castle, c)));
            if (lived && !hasCastle && couldBuild && !realm.Dissolved.IsKnown)
                faults.Add($"{where}: {realm.Name}, founded {realm.Founded}, has no castle though its land could hold one");
        }

        // Wars end, and after they start.
        foreach (var w in h.Wars)
        {
            if (!w.Ended.IsKnown && w.Started.Round < present - 12)
                faults.Add($"{where}: {w.Name}, begun {w.Started}, never ends");
            if (w.Ended.IsKnown && w.Ended.Round < w.Started.Round)
                faults.Add($"{where}: {w.Name} ends before it starts");
        }

        // Faiths: what a realm keeps exists, and nothing is open where the world may not hold it.
        foreach (var realm in h.LivingRealms.Where(r => r.StateReligion != null))
        {
            if (h.PresenceOf(realm.StateReligion!) == FaithPresence.Extinct)
                faults.Add($"{where}: {realm.Name} keeps {realm.StateReligion!.Name}, which is extinct here");
            if (!h.Profile.MayHoldOpenly(realm.StateReligion!))
                faults.Add($"{where}: {realm.Name} keeps {realm.StateReligion!.Name} openly, which this world may only hold in hiding");
        }
        foreach (var faith in h.Religions.Where(f => h.PresenceOf(f) == FaithPresence.Open && !h.Profile.MayHoldOpenly(f)))
            faults.Add($"{where}: {faith.Name} is open here, and may only be a hidden sect off its own world");

        // Organisations: seated somewhere real, not hiding once gone, and no imperial branch outliving the empire.
        foreach (var org in h.Organisations)
        {
            if (org.HomeRegion < 0 || org.HomeRegion >= regions.Count)
                faults.Add($"{where}: {org.Name} has no home region");
            if (org.Dissolved.IsKnown && org.Clandestine)
                faults.Add($"{where}: {org.Name} ended in {org.Dissolved} and is still recorded as in hiding");
            if (org.Kind == OrganisationKind.ImperialBranch && org.Founded.IsKnown && org.Founded.Round >= HistoryCalendar.HatchingRound)
                faults.Add($"{where}: {org.Name}, a branch of the empire, was opened after the empire ended");
        }
        foreach (var branch in h.LivingOrganisations.Where(o => o.Kind == OrganisationKind.ImperialBranch))
            faults.Add($"{where}: {branch.Name} is still an imperial branch at the present, six centuries after the last ship");

        // Every lore beat sprouted: a skipped one is lore that never happened.
        if (h.SeedStats.TryGetValue("ScriptedSeed", out var scripted) && scripted.Skipped > 0)
            faults.Add($"{where}: {scripted.Skipped} lore beat(s) did not sprout");
        if (h.SeedStats.TryGetValue("ScriptedSeed", out scripted) && scripted.Sown != scripted.Sprouted + scripted.Skipped)
            faults.Add($"{where}: {scripted.Sown - scripted.Sprouted - scripted.Skipped} lore beat(s) were sown past the present");

        // The imperial chapter matches the world's status.
        var stages = h.Chronology.Events.OfType<ImperialEvent>().Select(e => e.Stage).ToHashSet();
        switch (h.World.Status)
        {
            case ImperialStatus.Held:
                foreach (var stage in new[] { ImperialStage.Contact, ImperialStage.ConquestBegun, ImperialStage.ConquestCompleted, ImperialStage.InquisitionArrives, ImperialStage.Stranding })
                    if (!stages.Contains(stage)) faults.Add($"{where}: a held world with no {stage} event");
                break;
            case ImperialStatus.Visited:
                if (!stages.Contains(ImperialStage.Contact)) faults.Add($"{where}: a visited world with no Contact event");
                if (stages.Contains(ImperialStage.ConquestBegun)) faults.Add($"{where}: a visited world was conquered");
                break;
            case ImperialStatus.Unvisited:
                if (stages.Count > 0) faults.Add($"{where}: an unvisited world has imperial events ({string.Join(", ", stages)})");
                break;
        }

        // The present is the lore's post-imperial map: realms and free cities, neither one crown over a
        // whole world nor a crown per region.
        int standing = h.LivingRealms.Count();
        if (standing == 0)
            faults.Add($"{where}: no realm stands at the present");
        else if (regions.Count >= 20 && standing < MinStandingRealms)
            faults.Add($"{where}: {standing} realm(s) hold all {regions.Count} regions at the present (floor {MinStandingRealms}) - one crown over a world");
        if (standing > regions.Count / 2)
            faults.Add($"{where}: {standing} realms over {regions.Count} regions at the present - a crown per region");
    }
}
