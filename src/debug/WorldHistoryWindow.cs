using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Cathedral.Game.History;
using Cathedral.Game.History.Lore;

namespace Cathedral.Debug;

/// <summary>
/// The world history viewer: everything a world's past holds, in tabs (overview, chronology, realms,
/// figures, faiths, places, wars, regions, and the empire's own lore), each a searchable list with a
/// details pane. Linked entries in a details pane jump to their own tab on a double-click.
///
/// <para>Opened by <see cref="WorldHistoryViewerManager"/> on the world-selection screen when viewers
/// are on, and fed the history of whichever moon is chosen. A <see cref="WorldHistory"/> is never
/// written after generation, so reading it from this window's thread is safe.</para>
/// </summary>
public sealed class WorldHistoryWindow : Form
{
    private readonly Label _status;
    private readonly TabControl _tabs;
    private readonly Dictionary<Type, (TabPage Page, InfoTab Tab)> _jumpTargets = new();
    private readonly List<(TabPage Page, InfoTab Tab)> _empireTabs = new();
    private TabControl? _empireSubTabs;
    private TabPage? _empirePage;

    public WorldHistoryWindow()
    {
        Text = "World History";
        Size = new Size(1300, 820);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(40, 40);
        Font = new Font("Segoe UI", 9f);

        _status = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(40, 40, 48),
            ForeColor = Color.Gainsboro,
        };
        _tabs = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(_tabs);
        Controls.Add(_status);
        ShowMessage("Choose a moon to read its history.");
    }

    public void ShowMessage(string message)
    {
        _status.Text = message;
        // A message replaces whatever world was on show: a new moon is being built, or none is chosen.
        _tabs.TabPages.Clear();
        _jumpTargets.Clear();
        _empireTabs.Clear();
        var page = new TabPage("History");
        page.Controls.Add(new Label { Text = message, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 12f) });
        _tabs.TabPages.Add(page);
    }

    public void ShowHistory(WorldHistory h)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try { Fill(h); }
        finally { ResumeLayout(); }
        Console.WriteLine($"[WorldHistoryViewer] showing {h.World.Name}: {_tabs.TabPages.Count} tabs in {sw.ElapsedMilliseconds} ms");
    }

    private void Fill(WorldHistory h)
    {
        SuspendLayout();
        _tabs.TabPages.Clear();
        _jumpTargets.Clear();
        _empireTabs.Clear();

        _status.Text = $"{h.World.Name}  —  {h.World.Status}  —  {h.Geography.Regions.Count} regions  —  "
                     + $"{h.Chronology.Count} events  —  {h.LivingRealms.Count()} realms standing in {HistoricDate.At(HistoryCalendar.PresentRound)}";

        AddTextPage("Overview", Overview(h));
        AddChronology("Chronology", h, h.Chronology.Events, includeEmpireToggle: true);
        AddList<Realm>("Realms", h.Realms.OrderBy(r => r.Dissolved.IsKnown).ThenByDescending(r => r.Regions.Count).ToList(),
            new Col<Realm>[]
            {
                new("Realm", 260, r => r.Name), new("Government", 110, r => r.Government.ToString()),
                new("Founded", 80, r => r.Founded.ToString()), new("Ended", 80, r => r.Dissolved.IsKnown ? r.Dissolved.ToString() : "standing"),
                new("Regions now", 80, r => r.Regions.Count.ToString()), new("Ruler", 180, r => r.Ruler?.FullName ?? ""),
                new("Faith", 200, r => r.StateReligion?.Name ?? ""),
            }, r => RealmDetails(h, r), r => RealmLinks(r));
        AddList<HistoricFigure>("Figures", h.Figures.OrderBy(f => f.Born.IsKnown ? f.Born.Round : f.Events.FirstOrDefault()?.Date.Round ?? 0).ToList(),
            new Col<HistoricFigure>[]
            {
                new("Name", 200, f => f.FullName), new("Known as", 330, KnownAs), new("Born", 80, f => f.Born.ToString()),
                new("Died", 80, f => f.Died.ToString()), new("Affiliation", 220, f => f.Affiliation?.Name ?? ""),
            }, FigureDetails, FigureLinks);
        AddList<Religion>("Faiths", h.Religions.ToList(),
            new Col<Religion>[]
            {
                new("Faith", 260, r => r.Name), new("Kind", 110, r => r.Kind.ToString()), new("Founded", 90, r => r.Founded.ToString()),
                new("Gods", 50, r => r.Deities.Count.ToString()), new("Presence now", 100, r => h.PresenceOf(r).ToString()),
                new("Proscribed now", 100, r => h.Proscribed.Contains(r) ? "yes" : ""), new("Origin", 70, r => r.Scope == HistoryScope.Empire ? "empire" : "native"),
            }, r => FaithDetails(h, r), r => r.Events.Cast<object>().Concat(Opt(r.Founder)).Concat(Opt(r.Parent)));
        AddList<Organisation>("Factions", h.Organisations.OrderBy(o => o.Dissolved.IsKnown).ThenBy(o => o.Founded.Round).ToList(),
            new Col<Organisation>[]
            {
                new("Faction", 280, o => o.Name), new("Kind", 120, o => o.Kind.ToString()),
                new("Standing", 90, o => o.Dissolved.IsKnown ? "gone" : o.Clandestine ? "in hiding" : "open"),
                new("Founded", 80, o => o.Founded.ToString()), new("Ended", 80, o => o.Dissolved.IsKnown ? o.Dissolved.ToString() : ""),
                new("Seat", 130, o => RegionName(h, o.HomeRegion)), new("Patron", 200, o => o.Patron?.Name ?? ""),
                new("Of the empire", 200, o => o.ImperialCounterpart?.Name ?? ""),
            }, o => OrganisationDetails(h, o),
            o => o.Events.Cast<object>().Concat(Opt(o.Founder)).Concat(Opt(o.Patron)).Concat(Opt(o.Faith)).Concat(Opt(o.ImperialCounterpart)));
        AddList<Place>("Places", h.Places.ToList(),
            new Col<Place>[]
            {
                new("Place", 260, p => p.Name), new("Kind", 100, p => p.Kind.ToString()), new("Region", 140, p => RegionName(h, p.Region)),
                new("Founded", 80, p => p.Founded.ToString()), new("Ruined", 80, p => p.Ruined.IsKnown ? p.Ruined.ToString() : ""),
                new("Built by", 220, p => p.Builder?.Name ?? ""),
            }, p => PlaceDetails(h, p), p => p.Events.Cast<object>().Concat(Opt(p.Builder)).Concat(Opt(h.OwnerOf(p.Region))));
        AddList<War>("Wars", h.Wars.ToList(),
            new Col<War>[]
            {
                new("War", 320, w => w.Name), new("Started", 80, w => w.Started.ToString()), new("Ended", 80, w => w.Ended.ToString()),
                new("Outcome", 100, w => w.Outcome.ToString()),
                new("Sides", 400, w => $"{string.Join(", ", w.Attackers.Select(a => a.Name))}  vs  {string.Join(", ", w.Defenders.Select(d => d.Name))}"),
            }, WarDetails, w => w.Events.Cast<object>().Concat(w.Belligerents));
        AddList<RegionProfile>("Regions", h.Geography.Regions.ToList(),
            new Col<RegionProfile>[]
            {
                new("Id", 40, r => r.Id.ToString()), new("Name", 140, r => h.RegionNames[r.Id]), new("Landmass", 70, r => r.LandmassId.ToString()),
                new("Cells", 50, r => r.CellCount.ToString()), new("Coastal", 60, r => r.Coastal ? "yes" : ""),
                new("Held by now", 260, r => h.OwnerOf(r.Id)?.Name ?? "unclaimed"),
                new("Country", 220, r => $"{r.LivableCells} livable, {r.PlainCells} plain, {r.ForestCells} wooded, {r.MountainCells} high"),
            }, r => RegionDetails(h, r), r => Opt(h.OwnerOf(r.Id)).Concat(h.Places.Where(p => p.Region == r.Id)));
        AddEmpire(h.Empire);
    }

    // ── Pages ───────────────────────────────────────────────────────────────────

    private void AddTextPage(string title, string text)
    {
        var page = new TabPage(title);
        page.Controls.Add(ReadOnlyText(text));
        _tabs.TabPages.Add(page);
    }

    private void AddChronology(string title, WorldHistory? h, IReadOnlyList<HistoricEvent> events, bool includeEmpireToggle,
                               TabControl? into = null)
    {
        var page = new TabPage(title);
        var tab = new InfoTab(this, new[] { ("Date", 90), ("Event", 820), ("Kind", 140), ("Scope", 70) },
            o => { var e = (HistoricEvent)o; return new[] { e.Date.ToString(), e.Describe(), e.GetType().Name.Replace("Event", ""), e.Scope.ToString() }; },
            EventDetails, o => ((HistoricEvent)o).Involved);

        var era = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
        era.Items.AddRange(new object[] { "All eras", "Before the Foundation", "The empire (0 to 3579 FC)", "After the Hatching" });
        era.SelectedIndex = 0;
        var kinds = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
        kinds.Items.Add("All kinds");
        foreach (var k in events.Select(e => e.GetType().Name.Replace("Event", "")).Distinct().OrderBy(k => k)) kinds.Items.Add(k);
        kinds.SelectedIndex = 0;
        var withEmpire = new CheckBox { Text = "with the empire's own history", AutoSize = true, Visible = includeEmpireToggle && h != null };
        var hideCrowns = new CheckBox { Text = "hide births, deaths and accessions", AutoSize = true, Checked = true };

        void Refill()
        {
            IEnumerable<HistoricEvent> source = events;
            if (withEmpire.Checked && h != null)
                source = source.Concat(h.Empire.Chronology.Events).OrderBy(e => e.Date.Round);
            source = era.SelectedIndex switch
            {
                1 => source.Where(e => e.Date.Round < HistoryCalendar.FoundationRound),
                2 => source.Where(e => e.Date.Round >= HistoryCalendar.FoundationRound && e.Date.Round <= HistoryCalendar.HatchingRound),
                3 => source.Where(e => e.Date.Round > HistoryCalendar.HatchingRound),
                _ => source,
            };
            if (kinds.SelectedIndex > 0)
            {
                string k = (string)kinds.SelectedItem!;
                source = source.Where(e => e.GetType().Name.Replace("Event", "") == k);
            }
            if (hideCrowns.Checked) source = source.Where(e => e is not (BirthEvent or DeathEvent or AccessionEvent));
            tab.SetRows(source.Cast<object>().ToList());
        }
        era.SelectedIndexChanged += (_, _) => Refill();
        kinds.SelectedIndexChanged += (_, _) => Refill();
        withEmpire.CheckedChanged += (_, _) => Refill();
        hideCrowns.CheckedChanged += (_, _) => Refill();
        tab.AddToolbar(era, kinds, hideCrowns, withEmpire);
        Refill();

        page.Controls.Add(tab.Root);
        (into ?? _tabs).TabPages.Add(page);
        if (into == null) _jumpTargets[typeof(HistoricEvent)] = (page, tab);
        else _empireTabs.Add((page, tab));
    }

    private sealed record Col<T>(string Header, int Width, Func<T, string> Value);

    private void AddList<T>(string title, IReadOnlyList<T> rows, Col<T>[] cols, Func<T, string> details, Func<T, IEnumerable<object>> links,
                            TabControl? into = null) where T : class
    {
        var page = new TabPage($"{title} ({rows.Count})");
        var tab = new InfoTab(this, cols.Select(c => (c.Header, c.Width)).ToArray(),
            o => cols.Select(c => c.Value((T)o)).ToArray(), o => details((T)o), o => links((T)o));
        tab.SetRows(rows.Cast<object>().ToList());
        page.Controls.Add(tab.Root);
        (into ?? _tabs).TabPages.Add(page);
        if (into == null) _jumpTargets[typeof(T)] = (page, tab);
        else _empireTabs.Add((page, tab));
    }

    private void AddEmpire(EmpireLore e)
    {
        _empirePage = new TabPage("Empire lore");
        _empireSubTabs = new TabControl { Dock = DockStyle.Fill };
        _empirePage.Controls.Add(_empireSubTabs);
        _tabs.TabPages.Add(_empirePage);

        AddChronology("Chronology", null, e.Chronology.Events, includeEmpireToggle: false, into: _empireSubTabs);
        AddList<HistoricFigure>("Figures", e.Figures.OrderBy(f => f.Born.IsKnown ? f.Born.Round : 0).ToList(),
            new Col<HistoricFigure>[]
            {
                new("Name", 240, f => f.FullName), new("Known as", 300, KnownAs), new("Born", 80, f => f.Born.ToString()),
                new("Died", 80, f => f.Died.ToString()), new("House", 220, f => f.Affiliation?.Name ?? ""),
            }, FigureDetails, FigureLinks, _empireSubTabs);
        AddList<HistoricFaction>("Houses and institutions", e.Factions.ToList(),
            new Col<HistoricFaction>[]
            {
                new("Name", 300, f => f.Name), new("Founded", 90, f => f.Founded.ToString()), new("Ended", 90, f => f.Dissolved.ToString()),
                new("Founder", 220, f => f.Founder?.FullName ?? ""),
            }, f => $"{f.Name}\r\n{f.Description}\r\n\r\nFounded {f.Founded}, ended {f.Dissolved}.\r\nFounder: {f.Founder?.FullName ?? "unknown"}\r\n\r\n{Events(f.Events)}",
            f => f.Events.Cast<object>().Concat(Opt(f.Founder)), _empireSubTabs);
        AddList<Religion>("Faiths", e.Religions.ToList(),
            new Col<Religion>[]
            {
                new("Faith", 260, r => r.Name), new("Kind", 110, r => r.Kind.ToString()), new("Founded", 90, r => r.Founded.ToString()),
                new("Branch of", 200, r => r.Parent?.Name ?? ""),
            }, r => FaithDetails(null, r), r => r.Events.Cast<object>().Concat(Opt(r.Parent)), _empireSubTabs);
        AddList<WorldInfo>("Worlds", e.Worlds.ToList(),
            new Col<WorldInfo>[] { new("World", 200, w => w.Name), new("Status", 100, w => w.Status.ToString()), new("Description", 700, w => w.Description) },
            w => $"{w.Name} ({w.Status})\r\n\r\n{w.Description}\r\n\r\n{Events(w.Events)}", w => w.Events.Cast<object>(), _empireSubTabs);
        AddList<Place>("Places", e.Places.ToList(),
            new Col<Place>[] { new("Place", 260, p => p.Name), new("Kind", 100, p => p.Kind.ToString()), new("World", 100, p => p.World?.Name ?? ""),
                               new("Founded", 80, p => p.Founded.ToString()), new("Ruined", 80, p => p.Ruined.ToString()) },
            p => $"{p.Name} ({p.Kind}) on {p.World?.Name}\r\n{p.Description}\r\n\r\nFounded {p.Founded}, ruined {p.Ruined}.\r\n\r\n{Events(p.Events)}",
            p => p.Events.Cast<object>(), _empireSubTabs);
    }

    /// <summary>Selects <paramref name="target"/> in its own tab, if it has one.</summary>
    internal void JumpTo(object target)
    {
        // The empire's own infos live in the empire sub-tabs; try those first for an empire-scoped info.
        bool empire = target is HistoricInfo { Scope: HistoryScope.Empire } || target is HistoricEvent { Scope: HistoryScope.Empire };
        if (empire && _empirePage != null && _empireSubTabs != null)
            foreach (var (page, tab) in _empireTabs)
                if (tab.Select(target))
                {
                    _tabs.SelectedTab = _empirePage;
                    _empireSubTabs.SelectedTab = page;
                    return;
                }

        foreach (var (type, (page, tab)) in _jumpTargets)
            if (type.IsInstanceOfType(target) && tab.Select(target))
            {
                _tabs.SelectedTab = page;
                return;
            }
    }

    // ── Details ─────────────────────────────────────────────────────────────────

    private static IEnumerable<object> Opt(object? o) => o == null ? Array.Empty<object>() : new[] { o };

    private static string RegionName(WorldHistory h, int region)
        => region >= 0 && region < h.RegionNames.Length ? $"{h.RegionNames[region]} ({region})" : "";

    private static string Events(IEnumerable<HistoricEvent> events)
    {
        var list = events.OrderBy(e => e.Date.Round).ToList();
        if (list.Count == 0) return "";
        var sb = new StringBuilder("EVENTS\r\n");
        foreach (var e in list) sb.Append($"  {e.Date,-12} {e.Describe()}\r\n");
        return sb.ToString();
    }

    private static string Overview(WorldHistory h)
    {
        var sb = new StringBuilder();
        sb.Append($"{h.World.Name.ToUpperInvariant()}\r\n");
        if (h.World.Description.Length > 0) sb.Append($"{h.World.Description}\r\n");
        sb.Append($"\r\nImperial status: {h.World.Status}, {h.EmpireRelation()}.  History profile: {h.Profile.GetType().Name}.\r\n");
        if (h.World.NativeName.Length > 0 && h.World.NativeName != h.World.Name) sb.Append($"Its own people called it {h.World.NativeName}.\r\n");
        sb.Append($"{h.Geography.Regions.Count} regions on {h.Geography.LandmassCount} landmass(es).\r\n");
        sb.Append($"{h.Chronology.Count} events, {h.Figures.Count} figures, {h.Realms.Count} realms ({h.LivingRealms.Count()} standing), "
                + $"{h.Religions.Count} faiths, {h.Places.Count} places, {h.Wars.Count} wars.  Hash {h.Hash:X8}, generated in {h.GenerationMilliseconds} ms.\r\n");

        sb.Append($"\r\nTHE EMPIRE AND THIS WORLD\r\n");
        var imperial = h.Chronology.Events.OfType<ImperialEvent>().ToList();
        if (imperial.Count == 0) sb.Append("  The empire never came.\r\n");
        foreach (var e in imperial) sb.Append($"  {e.Date,-12} {e.Describe()}\r\n");

        var lore = h.Chronology.Events.OfType<ChronicleEvent>().Where(e => e.Title.Length > 0).ToList();
        if (lore.Count > 0)
        {
            // Titled chronicle events: the narrative ones no typed event covers. On a lore world these
            // are its lore beats; elsewhere the Stranding, the first goblins, submissions, burials.
            sb.Append("\r\nNOTABLE EVENTS (the named episodes of this world's past; on a lore world, its lore)\r\n");
            foreach (var e in lore) sb.Append($"  {e.Date,-12} {e.Describe()}\r\n");
        }

        sb.Append($"\r\nTHE WORLD TODAY ({HistoricDate.At(HistoryCalendar.PresentRound)})\r\n");
        foreach (var r in h.LivingRealms.OrderByDescending(r => r.Regions.Count))
            sb.Append($"  {r.Name} — {r.Government}, {r.Regions.Count} region(s), ruled by {r.Ruler?.FullName ?? "no one"}, "
                    + $"faith: {r.StateReligion?.Name ?? "none"}\r\n");
        int unclaimed = h.Geography.Regions.Count(r => h.OwnerOf(r.Id) == null);
        if (unclaimed > 0) sb.Append($"  {unclaimed} region(s) held by no one.\r\n");

        sb.Append("\r\nFAITHS KEPT TODAY\r\n");
        foreach (var r in h.LivingFaiths)
            sb.Append($"  {r.Name} ({r.Kind}{(h.PresenceOf(r) == FaithPresence.Clandestine ? ", in hiding" : "")}"
                    + $"{(h.Proscribed.Contains(r) ? ", proscribed" : "")}): {r.Description}\r\n");
        int lost = h.Religions.Count - h.LivingFaiths.Count();
        if (lost > 0) sb.Append($"  ...and {lost} faith(s) this world has lost.\r\n");

        sb.Append("\r\nFACTIONS TODAY\r\n");
        foreach (var o in h.LivingOrganisations)
            sb.Append($"  {o.Name} ({o.Kind}{(o.Clandestine ? ", in hiding" : "")}), seated in {RegionName(h, o.HomeRegion)}\r\n");
        return sb.ToString();
    }

    private static string EventDetails(object o)
    {
        var e = (HistoricEvent)o;
        var sb = new StringBuilder();
        sb.Append($"{e.Date}  —  {e.GetType().Name.Replace("Event", "")}  —  {e.Scope}\r\n\r\n{e.Describe()}\r\n\r\nINVOLVED\r\n");
        foreach (var i in e.Involved) sb.Append($"  {i.Name}  ({i.GetType().Name})\r\n");
        return sb.ToString();
    }

    /// <summary>
    /// Why history remembers this person, one line per reason: what they were made up for (their
    /// description), every title they held, every realm, faith or house they founded, whoever they
    /// killed. Their own reason first.
    /// </summary>
    private static List<string> Significance(HistoricFigure f)
    {
        var reasons = new List<string>();
        if (f.Description.Length > 0) reasons.Add(f.Description);
        foreach (var a in f.Events.OfType<AccessionEvent>().Where(a => a.Figure == f))
            reasons.Add($"{a.Title} of {a.Office.Name}, from {a.Date}.");
        foreach (var e in f.Events.OfType<FactionFoundedEvent>().Where(e => e.Faction.Founder == f))
            reasons.Add($"Founder of {e.Faction.Name} ({e.Date}).");
        foreach (var e in f.Events.OfType<ReligionFoundedEvent>().Where(e => e.Religion.Founder == f))
            reasons.Add($"Founder of {e.Religion.Name} ({e.Date}).");
        foreach (var e in f.Events.OfType<DeathEvent>().Where(e => e.Killer == f))
            reasons.Add($"Killed {e.Figure.FullName} ({e.Date}).");
        if (reasons.Count == 0 && f.Parents.Count > 0)
            reasons.Add($"Child of {string.Join(" and ", f.Parents.Select(p => p.FullName))}.");
        return reasons;
    }

    /// <summary>The one-line answer for the list: the last title held, else the first reason.</summary>
    private static string KnownAs(HistoricFigure f)
    {
        var crown = f.Events.OfType<AccessionEvent>().LastOrDefault(a => a.Figure == f);
        if (crown != null) return $"{crown.Title} of {crown.Office.Name}";
        var reasons = Significance(f);
        return reasons.Count > 0 ? reasons[0].TrimEnd('.') : "";
    }

    private static string FigureDetails(HistoricFigure f)
    {
        var sb = new StringBuilder();
        sb.Append($"{f.FullName}\r\n{f.Sex}, born {f.Born}, died {f.Died}");
        if (f.Born.IsKnown && f.Died.IsKnown) sb.Append($" (aged {f.Died.Round - f.Born.Round})");
        sb.Append("\r\n");
        if (f.Affiliation != null) sb.Append($"Of {f.Affiliation.Name}.\r\n");
        var why = Significance(f);
        if (why.Count > 0)
        {
            sb.Append("\r\nWHY REMEMBERED\r\n");
            foreach (var w in why) sb.Append($"  {w}\r\n");
        }
        if (f.Parents.Count > 0) sb.Append($"\r\nParents: {string.Join(", ", f.Parents.Select(p => p.FullName))}\r\n");
        if (f.Spouses.Count > 0) sb.Append($"Spouses: {string.Join(", ", f.Spouses.Select(p => p.FullName))}\r\n");
        if (f.Children.Count > 0) sb.Append($"Children: {string.Join(", ", f.Children.Select(p => p.FullName))}\r\n");
        sb.Append($"\r\n{Events(f.Events)}");
        return sb.ToString();
    }

    private static IEnumerable<object> FigureLinks(HistoricFigure f)
        => f.Events.Cast<object>().Concat(Opt(f.Affiliation)).Concat(f.Parents).Concat(f.Spouses).Concat(f.Children);

    private static string RealmDetails(WorldHistory h, Realm r)
    {
        var sb = new StringBuilder();
        sb.Append($"{r.Name}\r\n{r.Government}, founded {r.Founded}, {(r.Dissolved.IsKnown ? $"ended {r.Dissolved}" : "standing today")}.\r\n");
        if (r.Predecessor != null) sb.Append($"Successor of {r.Predecessor.Name}.\r\n");
        if (r.Founder != null) sb.Append($"Founded by {r.Founder.FullName}.\r\n");
        if (r.StateReligion != null) sb.Append($"Faith: {r.StateReligion.Name}.\r\n");
        if (r.Regions.Count > 0)
            sb.Append($"Holds: {string.Join(", ", r.Regions.Select(x => h.RegionNames[x]))}; seat at {RegionName(h, r.CapitalRegion)}.\r\n");
        sb.Append("\r\nRULERS\r\n");
        foreach (var a in r.Events.OfType<AccessionEvent>())
            sb.Append($"  {a.Date,-12} {a.Title} {a.Figure.FullName}  (born {a.Figure.Born}, died {a.Figure.Died})\r\n");
        sb.Append($"\r\n{Events(r.Events.Where(e => e is not AccessionEvent))}");
        return sb.ToString();
    }

    private static IEnumerable<object> RealmLinks(Realm r)
        => r.Events.Cast<object>().Concat(r.Rulers).Concat(Opt(r.StateReligion)).Concat(Opt(r.Predecessor));

    private static string FaithDetails(WorldHistory? h, Religion r)
    {
        var sb = new StringBuilder();
        sb.Append($"{r.Name}\r\n{r.Kind}, founded {r.Founded}");
        if (r.Founder != null) sb.Append($" by {r.Founder.FullName}");
        if (r.Parent != null) sb.Append($", from {r.Parent.Name}");
        sb.Append(".\r\n");
        if (h != null && h.Proscribed.Contains(r)) sb.Append("Proscribed on this world today.\r\n");
        sb.Append($"\r\n{r.Description}\r\n");
        if (r.Deities.Count > 0)
        {
            sb.Append("\r\nGODS\r\n");
            foreach (var d in r.Deities) sb.Append($"  {d.Name}: {d.Domain}, {d.Nature}, {d.Form}. {d.Description}\r\n");
        }
        if (h != null)
        {
            var adoptions = h.Chronology.Events.OfType<ReligiousChangeEvent>().Where(e => e.Religion == r).ToList();
            if (adoptions.Count > 0)
            {
                sb.Append("\r\nON THIS WORLD\r\n");
                foreach (var e in adoptions) sb.Append($"  {e.Date,-12} {e.Describe()}\r\n");
            }
        }
        sb.Append($"\r\n{Events(r.Events)}");
        return sb.ToString();
    }

    private static string PlaceDetails(WorldHistory h, Place p)
    {
        var sb = new StringBuilder();
        sb.Append($"{p.Name}\r\n{p.Kind} in {RegionName(h, p.Region)}, founded {p.Founded}");
        sb.Append(p.Ruined.IsKnown ? $", ruined {p.Ruined}.\r\n" : ", standing today.\r\n");
        if (p.Builder != null) sb.Append($"Built by {p.Builder.Name}.\r\n");
        sb.Append($"The region is held today by {h.OwnerOf(p.Region)?.Name ?? "no one"}.\r\n\r\n{Events(p.Events)}");
        return sb.ToString();
    }

    private static string OrganisationDetails(WorldHistory h, Organisation o)
    {
        var sb = new StringBuilder();
        sb.Append($"{o.Name}\r\n{o.Kind}, founded {o.Founded}");
        sb.Append(o.Dissolved.IsKnown ? $", ended {o.Dissolved}.\r\n" : o.Clandestine ? ", living in hiding today.\r\n" : ", open today.\r\n");
        sb.Append($"{o.Description}\r\n\r\nSeat: {RegionName(h, o.HomeRegion)}.\r\n");
        if (o.Founder != null) sb.Append($"Founder: {o.Founder.FullName}.\r\n");
        if (o.Patron != null) sb.Append($"Patron: {o.Patron.Name}.\r\n");
        if (o.Faith != null) sb.Append($"Serves: {o.Faith.Name}.\r\n");
        if (o.ImperialCounterpart != null) sb.Append($"A branch, cell or heir of {o.ImperialCounterpart.Name}.\r\n");
        sb.Append($"\r\n{Events(o.Events)}");
        return sb.ToString();
    }

    private static string WarDetails(War w)
        => $"{w.Name}\r\n{w.Started} to {w.Ended}: {w.Outcome}.\r\n\r\nAttackers: {string.Join(", ", w.Attackers.Select(a => a.Name))}\r\n"
         + $"Defenders: {string.Join(", ", w.Defenders.Select(d => d.Name))}\r\n\r\n{Events(w.Events)}";

    private static string RegionDetails(WorldHistory h, RegionProfile r)
    {
        var sb = new StringBuilder();
        sb.Append($"{h.RegionNames[r.Id]} (region {r.Id}), landmass {r.LandmassId}, {r.CellCount} cells{(r.Coastal ? ", on the sea" : "")}.\r\n");
        sb.Append($"{r.LivableCells} livable (habitability {r.Habitability}), {r.PlainCells} plain, {r.ForestCells} wooded, {r.MountainCells} high.\r\n");
        sb.Append($"Held today by {h.OwnerOf(r.Id)?.Name ?? "no one"}.\r\n");
        sb.Append($"Borders: {string.Join(", ", r.Neighbours.Select(n => h.RegionNames[n]))}.\r\n");
        var places = h.Places.Where(p => p.Region == r.Id).ToList();
        if (places.Count > 0)
        {
            sb.Append("\r\nPLACES\r\n");
            foreach (var p in places) sb.Append($"  {p.Name} ({p.Kind}), {p.Founded}{(p.Ruined.IsKnown ? $", ruined {p.Ruined}" : "")}\r\n");
        }
        var territory = h.Chronology.Events.OfType<TerritoryEvent>().Where(e => e.Regions.Contains(r.Id)).ToList();
        if (territory.Count > 0)
        {
            sb.Append("\r\nCHANGES OF HANDS\r\n");
            foreach (var e in territory) sb.Append($"  {e.Date,-12} {e.Gainer.Name} takes it{(e.Loser == null ? "" : $" from {e.Loser.Name}")} ({e.How})\r\n");
        }
        return sb.ToString();
    }

    internal static TextBox ReadOnlyText(string text) => new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = true,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9.5f),
        BackColor = Color.White,
        Text = text,
    };

    /// <summary>
    /// One list tab: a filter box and optional toolbar above a virtual list, and a details pane with
    /// the entry's links beside it. Virtual because a chronology runs to twenty thousand rows.
    /// </summary>
    private sealed class InfoTab
    {
        private readonly WorldHistoryWindow _owner;
        private readonly Func<object, string[]> _cells;
        private readonly Func<object, string> _details;
        private readonly Func<object, IEnumerable<object>> _links;
        private readonly ListView _list;
        private readonly TextBox _filter;
        private readonly TextBox _detailsBox;
        private readonly ListBox _linkBox;
        private readonly FlowLayoutPanel _toolbar;
        private readonly Label _count;
        private List<object> _all = new();
        private List<(object Row, string[] Cells)> _shown = new();
        private readonly Dictionary<object, string[]> _cellCache = new();
        private readonly string[] _headers;
        private int _sortColumn = -1;     // -1: the order the rows were given in
        private bool _sortDescending;

        public Control Root { get; }

        private static readonly System.Text.RegularExpressions.Regex DateCell =
            new(@"^(c\. )?(-?\d+) (FC|AH)$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// How a cell sorts, read off its text so no column has to declare it: a date by its round
        /// ("612 AH" after "3500 FC"), a number by value, anything else alphabetically. Empty cells and
        /// unknown dates go last whichever way the column is sorted.
        /// </summary>
        private static (int Rank, double Number, string Text) SortKey(string cell)
        {
            if (cell.Length == 0 || cell == "date unknown") return (2, 0, "");
            var m = DateCell.Match(cell);
            if (m.Success)
                return (0, int.Parse(m.Groups[2].Value) + (m.Groups[3].Value == "AH" ? HistoryCalendar.HatchingRound : 0), "");
            if (double.TryParse(cell, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double n))
                return (0, n, "");
            return (1, 0, cell);
        }

        private int Compare(string a, string b)
        {
            var ka = SortKey(a);
            var kb = SortKey(b);
            // Blanks last in both directions: only the comparison of real values flips.
            if (ka.Rank == 2 || kb.Rank == 2) return ka.Rank.CompareTo(kb.Rank);
            int c = ka.Rank != kb.Rank ? ka.Rank.CompareTo(kb.Rank)
                  : ka.Rank == 0 ? ka.Number.CompareTo(kb.Number)
                  : string.Compare(ka.Text, kb.Text, StringComparison.OrdinalIgnoreCase);
            return _sortDescending ? -c : c;
        }

        public InfoTab(WorldHistoryWindow owner, (string Header, int Width)[] columns, Func<object, string[]> cells,
                       Func<object, string> details, Func<object, IEnumerable<object>> links)
        {
            _owner = owner;
            _cells = cells;
            _details = details;
            _links = links;

            _list = new ListView { View = View.Details, FullRowSelect = true, VirtualMode = true, Dock = DockStyle.Fill, HideSelection = false, MultiSelect = false };
            foreach (var (h, w) in columns) _list.Columns.Add(h, w);
            _list.RetrieveVirtualItem += (_, e) => e.Item = new ListViewItem(_shown[e.ItemIndex].Cells);
            _list.SelectedIndexChanged += (_, _) => ShowSelected();
            _headers = columns.Select(c => c.Header).ToArray();
            _list.ColumnClick += (_, e) =>
            {
                // A second click on the same column reverses it; a new column starts ascending.
                _sortDescending = _sortColumn == e.Column && !_sortDescending;
                _sortColumn = e.Column;
                for (int i = 0; i < _list.Columns.Count; i++)
                    _list.Columns[i].Text = _headers[i] + (i == _sortColumn ? (_sortDescending ? " ▼" : " ▲") : "");
                Apply();
            };

            _filter = new TextBox { Width = 260, PlaceholderText = "search..." };
            _filter.TextChanged += (_, _) => Apply();
            _count = new Label { AutoSize = true, Padding = new Padding(8, 6, 0, 0) };
            _toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(4), WrapContents = false };
            _toolbar.Controls.Add(_filter);

            _detailsBox = ReadOnlyText("");
            _linkBox = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            _linkBox.DoubleClick += (_, _) => { if (_linkBox.SelectedItem is Link l) _owner.JumpTo(l.Target); };
            var linkPanel = new Panel { Dock = DockStyle.Fill };
            linkPanel.Controls.Add(_linkBox);
            linkPanel.Controls.Add(new Label { Text = "Linked (double-click to open)", Dock = DockStyle.Top, Height = 18 });

            var right = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 420 };
            right.Panel1.Controls.Add(_detailsBox);
            right.Panel2.Controls.Add(linkPanel);

            var left = new Panel { Dock = DockStyle.Fill };
            left.Controls.Add(_list);
            left.Controls.Add(_toolbar);

            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 760 };
            split.Panel1.Controls.Add(left);
            split.Panel2.Controls.Add(right);
            Root = split;
        }

        public void AddToolbar(params Control[] controls)
        {
            foreach (var c in controls) _toolbar.Controls.Add(c);
        }

        public void SetRows(List<object> rows)
        {
            _all = rows;
            Apply();
        }

        private string[] CellsOf(object o)
        {
            if (!_cellCache.TryGetValue(o, out var c)) _cellCache[o] = c = _cells(o);
            return c;
        }

        private void Apply()
        {
            string f = _filter.Text.Trim();
            _shown = _all.Select(o => (o, CellsOf(o)))
                         .Where(x => f.Length == 0 || x.Item2.Any(c => c.Contains(f, StringComparison.OrdinalIgnoreCase)))
                         .ToList();
            if (_sortColumn >= 0)
            {
                // Stable, so ties keep the given order (a chronology sorted by kind stays dated within each kind).
                int col = _sortColumn;
                _shown = _shown.Select((x, i) => (x, i))
                               .OrderBy(p => p.x.Cells[col], Comparer<string>.Create(Compare))
                               .ThenBy(p => p.i)
                               .Select(p => p.x)
                               .ToList();
            }
            _list.SelectedIndices.Clear();
            _list.VirtualListSize = _shown.Count;
            _list.Invalidate();
            _count.Text = $"{_shown.Count} of {_all.Count}";
            if (!_toolbar.Controls.Contains(_count)) _toolbar.Controls.Add(_count);
        }

        private void ShowSelected()
        {
            if (_list.SelectedIndices.Count == 0) return;
            var row = _shown[_list.SelectedIndices[0]].Row;
            _detailsBox.Text = _details(row);
            _linkBox.BeginUpdate();
            _linkBox.Items.Clear();
            foreach (var l in _links(row).Distinct().Take(400)) _linkBox.Items.Add(new Link(l));
            _linkBox.EndUpdate();
        }

        /// <summary>Selects <paramref name="target"/> if this tab lists it, clearing the filter if it hides it.</summary>
        public bool Select(object target)
        {
            if (!_all.Contains(target)) return false;
            int i = _shown.FindIndex(x => ReferenceEquals(x.Row, target));
            if (i < 0)
            {
                _filter.Text = "";
                i = _shown.FindIndex(x => ReferenceEquals(x.Row, target));
                if (i < 0) return false;
            }
            _list.SelectedIndices.Clear();
            _list.SelectedIndices.Add(i);
            _list.EnsureVisible(i);
            _list.Focus();
            return true;
        }
    }

    private sealed record Link(object Target)
    {
        public override string ToString() => Target switch
        {
            HistoricEvent e => $"{e.Date}: {e.Describe()}",
            HistoricInfo i => $"{i.Name}  [{i.GetType().Name}]",
            _ => Target.ToString() ?? "",
        };
    }
}
