using Label       = System.Windows.Forms.Label;
using Orientation = System.Windows.Forms.Orientation;
using FontStyle   = System.Drawing.FontStyle;
using MsaglGraph  = Microsoft.Msagl.Drawing.Graph;
using MsaglNode   = Microsoft.Msagl.Drawing.Node;
using MsaglColor  = Microsoft.Msagl.Drawing.Color;
using MsaglShape  = Microsoft.Msagl.Drawing.Shape;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;
using Microsoft.Msagl.GraphViewerGdi;

namespace Cathedral.Debug;

/// <summary>
/// WinForms window with two tabs for inspecting a Scene:
///   • Frontend / PoV — the currently visible observations and verb outcomes
///   • Backend / Scene — the full Section→Area→Spot hierarchy, NPCs, area graph
/// </summary>
public class SceneDebugWindow : Form
{
    private readonly Cathedral.Game.Scene.Scene _scene;
    private PoV? _pov;
    private readonly int _locationId;

    // ── Shared UI ────────────────────────────────────────────────
    private readonly TabControl _tabs;
    private readonly Label _povLabel;

    // ── Frontend tab ─────────────────────────────────────────────
    private readonly GViewer _frontViewer;
    private readonly RichTextBox _frontDetailsBox;
    private MsaglGraph _frontGraph;

    // ── Backend tab ──────────────────────────────────────────────
    private readonly GViewer _backViewer;
    private readonly RichTextBox _backDetailsBox;
    private readonly RichTextBox _stateBox;
    private MsaglGraph _backGraph;
    private string _currentAreaId = "";

    // ── Node fill colours ────────────────────────────────────────
    private static readonly MsaglColor ColorSection     = new(  0, 160, 160); // teal
    private static readonly MsaglColor ColorArea        = new( 90, 145, 210); // blue
    private static readonly MsaglColor ColorSpot        = new(210, 160,  50); // amber
    private static readonly MsaglColor ColorItem        = new(120, 180, 120); // green
    private static readonly MsaglColor ColorNpc         = new(140,  70, 200); // purple
    private static readonly MsaglColor ColorVerb        = new(200, 100, 100); // red-ish
    private static readonly MsaglColor ColorKeyword     = new(180, 180, 130); // soft yellow
    private static readonly MsaglColor ColorReachable   = new(100, 130, 180); // muted blue

    private static readonly MsaglColor BorderNormal     = new( 80,  80,  90);
    private static readonly MsaglColor BorderCurrent    = new(255, 220,   0);
    private static readonly MsaglColor BorderFocus      = new(255, 140,  60);
    private const double LineWidthNormal  = 1.0;
    private const double LineWidthCurrent = 4.0;

    public SceneDebugWindow(Cathedral.Game.Scene.Scene scene, PoV? pov, int locationId)
    {
        _scene      = scene;
        _pov        = pov;
        _locationId = locationId;

        Text        = $"Scene Debug — Location {locationId}";
        Width       = 1400;
        Height      = 920;
        MinimumSize = new Size(900, 650);

        // ── PoV bar (shared, top of window) ──────────────────────
        _povLabel = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 32,
            Font      = new Font("Consolas", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.FromArgb(200, 220, 255),
            Padding   = new Padding(8, 6, 8, 6),
        };
        UpdatePovLabel();

        // ── Tab control ──────────────────────────────────────────
        _tabs = new TabControl
        {
            Dock      = DockStyle.Fill,
            Font      = new Font("Consolas", 10, FontStyle.Bold),
        };

        // === FRONTEND TAB ========================================
        var frontTab = new TabPage("Frontend / PoV") { BackColor = Color.FromArgb(30, 30, 30) };

        var frontSplit = new SplitContainer
        {
            Dock        = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor   = Color.FromArgb(30, 30, 30),
        };
        frontSplit.SizeChanged += (_, _) =>
        {
            try
            {
                if (frontSplit.Width < 400) return;
                frontSplit.Panel2MinSize = Math.Min(300, frontSplit.Width / 3);
                frontSplit.SplitterDistance = (int)(frontSplit.Width * 0.62);
            }
            catch (InvalidOperationException) { }
        };

        _frontViewer = new GViewer { Dock = DockStyle.Fill, NavigationVisible = true };
        _frontViewer.MouseClick += OnFrontViewerClick;

        var frontHeader = new Label
        {
            Text = "  Entry Details", Dock = DockStyle.Top, Height = 22,
            Font = new Font("Consolas", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White,
        };
        _frontDetailsBox = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            Font        = new Font("Consolas", 9),
            BackColor   = Color.FromArgb(28, 28, 28),
            ForeColor   = Color.LightGray,
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
        };

        var frontRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30) };
        frontRight.Controls.Add(_frontDetailsBox);
        frontRight.Controls.Add(frontHeader);
        frontRight.Controls.Add(BuildLegendPanel(frontend: true));

        frontSplit.Panel1.Controls.Add(_frontViewer);
        frontSplit.Panel2.Controls.Add(frontRight);
        frontTab.Controls.Add(frontSplit);

        // === BACKEND TAB =========================================
        var backTab = new TabPage("Backend / Scene") { BackColor = Color.FromArgb(30, 30, 30) };

        var backSplit = new SplitContainer
        {
            Dock        = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor   = Color.FromArgb(30, 30, 30),
        };
        backSplit.SizeChanged += (_, _) =>
        {
            try
            {
                if (backSplit.Width < 400) return;
                backSplit.Panel2MinSize = Math.Min(320, backSplit.Width / 3);
                backSplit.SplitterDistance = (int)(backSplit.Width * 0.65);
            }
            catch (InvalidOperationException) { }
        };

        _backViewer = new GViewer { Dock = DockStyle.Fill, NavigationVisible = true };
        _backViewer.MouseClick += OnBackViewerClick;

        var backDetailsHeader = new Label
        {
            Text = "  Element Details", Dock = DockStyle.Top, Height = 22,
            Font = new Font("Consolas", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White,
        };
        _backDetailsBox = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            Font        = new Font("Consolas", 9),
            BackColor   = Color.FromArgb(28, 28, 28),
            ForeColor   = Color.LightGray,
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
        };

        var stateHeader = new Label
        {
            Text = "  State Changes", Dock = DockStyle.Bottom, Height = 22,
            Font = new Font("Consolas", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White,
        };
        _stateBox = new RichTextBox
        {
            Dock        = DockStyle.Bottom,
            Height      = 120,
            ReadOnly    = true,
            Font        = new Font("Consolas", 9),
            BackColor   = Color.FromArgb(28, 28, 28),
            ForeColor   = Color.FromArgb(255, 180, 100),
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
        };

        var backRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30) };
        backRight.Controls.Add(_backDetailsBox);
        backRight.Controls.Add(backDetailsHeader);
        backRight.Controls.Add(_stateBox);
        backRight.Controls.Add(stateHeader);
        backRight.Controls.Add(BuildLegendPanel(frontend: false));

        backSplit.Panel1.Controls.Add(_backViewer);
        backSplit.Panel2.Controls.Add(backRight);
        backTab.Controls.Add(backSplit);

        // ── Assemble ─────────────────────────────────────────────
        _tabs.TabPages.Add(frontTab);
        _tabs.TabPages.Add(backTab);

        Controls.Add(_tabs);
        Controls.Add(_povLabel);

        // ── Build both graphs ────────────────────────────────────
        _backGraph  = BuildBackendGraph();
        _backViewer.Graph = _backGraph;

        _frontGraph = BuildFrontendGraph();
        _frontViewer.Graph = _frontGraph;

        if (pov != null)
        {
            _currentAreaId = pov.Where.Id.ToString();
            HighlightCurrentBackend();
        }

        UpdateStateBox();
    }

    // ══════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════════════

    public void UpdatePoV(PoV pov)
    {
        if (InvokeRequired) { Invoke(() => UpdatePoV(pov)); return; }

        _pov = pov;

        // Reset old backend highlight
        if (_currentAreaId.Length > 0 && _backGraph.FindNode(_currentAreaId) is { } prev)
        {
            prev.Attr.Color     = BorderNormal;
            prev.Attr.LineWidth = LineWidthNormal;
        }

        _currentAreaId = pov.Where.Id.ToString();
        HighlightCurrentBackend();
        UpdatePovLabel();
        UpdateStateBox();

        _backViewer.Graph = _backGraph;

        // Rebuild frontend graph entirely (different set of visible elements)
        _frontGraph = BuildFrontendGraph();
        _frontViewer.Graph = _frontGraph;
    }

    // ══════════════════════════════════════════════════════════════
    //  FRONTEND GRAPH (PoV view)
    // ══════════════════════════════════════════════════════════════

    private MsaglGraph BuildFrontendGraph()
    {
        var msagl = new MsaglGraph("frontend");

        if (_pov == null) return msagl;

        var view = _scene.View(_pov);
        var added = new HashSet<string>();

        // Central node: current area
        var areaId = view.CurrentArea.Id.ToString();
        AddNode(msagl, added, areaId, $"★ {view.CurrentArea.DisplayName}", ColorArea, MsaglShape.Diamond);

        foreach (var entry in view.Entries)
        {
            var elId = entry.Source.Id.ToString();
            if (elId == areaId) continue; // the hub is already placed

            // Pick colour by element type
            MsaglColor fill;
            string prefix = "";
            if (entry.Source is Spot)          { fill = ColorSpot;      }
            else if (entry.Source is ItemElement) { fill = ColorItem;   }
            else if (entry.Source is SceneNpc npcEntry) { fill = ColorNpc; prefix = $"NPC ({npcEntry.Entity.Archetype.Species.DisplayName}): "; }
            else if (entry.Source is Area)        { fill = ColorReachable; prefix = "→ "; }
            else                                  { fill = ColorKeyword; }

            AddNode(msagl, added, elId, $"{prefix}{entry.Source.DisplayName}", fill);
            msagl.AddEdge(areaId, "", elId).Attr.Color = new MsaglColor(100, 100, 110);

            // Verb verbatim sub-nodes
            for (int i = 0; i < entry.ApplicableVerbs.Count; i++)
            {
                var vv = entry.ApplicableVerbs[i];
                var verbNodeId = $"v_{elId}_{i}";
                AddNode(msagl, added, verbNodeId, vv.Verbatim, ColorVerb, MsaglShape.Octagon);
                msagl.AddEdge(elId, "", verbNodeId).Attr.Color = new MsaglColor(180, 80, 80);
            }
        }

        // Highlight focus if set
        if (view.Focus != null)
        {
            var focusId = view.Focus.Id.ToString();
            if (msagl.FindNode(focusId) is { } focusNode)
            {
                focusNode.Attr.Color     = BorderFocus;
                focusNode.Attr.LineWidth = 3.0;
            }
        }

        return msagl;
    }

    // ══════════════════════════════════════════════════════════════
    //  BACKEND GRAPH (full scene hierarchy)
    // ══════════════════════════════════════════════════════════════

    private MsaglGraph BuildBackendGraph()
    {
        var msagl = new MsaglGraph("scene");
        var addedNodes = new HashSet<string>();

        foreach (var section in _scene.Sections)
        {
            var sectionId = section.Id.ToString();
            AddNode(msagl, addedNodes, sectionId, $"§ {section.DisplayName}", ColorSection);

            foreach (var area in section.Areas)
            {
                var areaId = area.Id.ToString();
                AddNode(msagl, addedNodes, areaId, area.DisplayName, ColorArea);
                msagl.AddEdge(sectionId, "contains", areaId).Attr.Color = new MsaglColor(100, 100, 110);

                foreach (var spot in area.Spots)
                {
                    var spotId = spot.Id.ToString();
                    AddNode(msagl, addedNodes, spotId, spot.DisplayName, ColorSpot);
                    msagl.AddEdge(areaId, "", spotId).Attr.Color = new MsaglColor(180, 140, 40);

                    foreach (var itemEl in spot.Items)
                    {
                        var itemId = itemEl.Id.ToString();
                        AddNode(msagl, addedNodes, itemId, itemEl.DisplayName, ColorItem);
                        msagl.AddEdge(spotId, "", itemId).Attr.Color = new MsaglColor(100, 160, 100);
                    }
                }
            }
        }

        // Area-to-area connectivity edges
        foreach (var (fromId, toIds) in _scene.AreaGraph)
        {
            foreach (var toId in toIds)
            {
                var fromStr = fromId.ToString();
                var toStr   = toId.ToString();
                if (addedNodes.Contains(fromStr) && addedNodes.Contains(toStr))
                {
                    var edge = msagl.AddEdge(fromStr, "→", toStr);
                    edge.Attr.Color = new MsaglColor(60, 130, 200);
                    edge.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dashed);
                }
            }
        }

        // NPC nodes
        foreach (var npc in _scene.Npcs)
        {
            var npcId = npc.Id.ToString();
            AddNode(msagl, addedNodes, npcId, $"NPC ({npc.Entity.Archetype.Species.DisplayName}): {npc.DisplayName}", ColorNpc);

            if (_scene.NpcSchedules.TryGetValue(npc.Id, out var schedule))
            {
                foreach (var (_, nodeId) in schedule.ActivePeriods)
                {
                    var matchingArea = _scene.AllAreas.FirstOrDefault(a =>
                        string.Equals(a.DisplayName, nodeId, StringComparison.OrdinalIgnoreCase));

                    if (matchingArea != null && addedNodes.Contains(matchingArea.Id.ToString()))
                    {
                        var edge = msagl.AddEdge(npcId, "at", matchingArea.Id.ToString());
                        edge.Attr.Color = new MsaglColor(120, 60, 180);
                        edge.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dotted);
                    }
                }
            }
        }

        return msagl;
    }

    // ══════════════════════════════════════════════════════════════
    //  SHARED HELPERS
    // ══════════════════════════════════════════════════════════════

    private static void AddNode(MsaglGraph msagl, HashSet<string> added, string id, string label,
                                MsaglColor fill, MsaglShape shape = MsaglShape.Box)
    {
        if (!added.Add(id)) return;
        var node = msagl.AddNode(id);
        node.LabelText             = label;
        node.Attr.FillColor        = fill;
        node.Attr.Color            = new MsaglColor(80, 80, 90);
        node.Attr.LineWidth        = LineWidthNormal;
        node.Attr.Shape            = shape;
        node.Label.FontColor       = new MsaglColor(240, 240, 240);
        node.Label.FontSize        = 10;
    }

    private void HighlightCurrentBackend()
    {
        if (_currentAreaId.Length > 0 && _backGraph.FindNode(_currentAreaId) is { } cur)
        {
            cur.Attr.Color     = BorderCurrent;
            cur.Attr.LineWidth = LineWidthCurrent;
        }
    }

    private Panel BuildLegendPanel(bool frontend)
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 24,
            BackColor = Color.FromArgb(45, 45, 48),
        };

        var items = frontend
            ? new (Color fill, string text)[]
              {
                  (Color.FromArgb( 90, 145, 210), " area "),
                  (Color.FromArgb(210, 160,  50), " spot "),
                  (Color.FromArgb(120, 180, 120), " item "),
                  (Color.FromArgb(140,  70, 200), " NPC "),
                  (Color.FromArgb(100, 130, 180), " reachable "),
                  (Color.FromArgb(200, 100, 100), " verb "),
              }
            : new (Color fill, string text)[]
              {
                  (Color.FromArgb(  0, 160, 160), " section "),
                  (Color.FromArgb( 90, 145, 210), " area "),
                  (Color.FromArgb(210, 160,  50), " spot "),
                  (Color.FromArgb(120, 180, 120), " item "),
                  (Color.FromArgb(140,  70, 200), " NPC "),
              };

        int x = 6;
        foreach (var (fill, text) in items)
        {
            var sq = new Panel { Size = new Size(12, 12), BackColor = fill, Location = new Point(x, 6) };
            panel.Controls.Add(sq);
            x += 15;
            var lbl = new Label
            {
                Text = text, AutoSize = true, Font = new Font("Consolas", 8),
                ForeColor = Color.LightGray, BackColor = Color.Transparent,
                Location = new Point(x, 5),
            };
            panel.Controls.Add(lbl);
            x += lbl.PreferredWidth + 4;
        }

        return panel;
    }

    // ── Click handlers ───────────────────────────────────────────

    private void OnFrontViewerClick(object? sender, MouseEventArgs e)
    {
        var obj = _frontViewer.GetObjectAt(e.X, e.Y);
        if (obj is not Microsoft.Msagl.Drawing.IViewerNode vn) return;
        var nodeId = vn.Node.Id;

        // Verb nodes have synthetic IDs "v_{guid}_{index}" — extract the parent element
        if (nodeId.StartsWith("v_") && _pov != null)
        {
            var view = _scene.View(_pov);
            // Try to find the SceneViewEntry for the verb
            var parts = nodeId.Split('_', 3);
            if (parts.Length >= 3 && Guid.TryParse(parts[1], out var parentGuid))
            {
                var entry = view.Entries.FirstOrDefault(e => e.Source.Id == parentGuid);
                if (entry != null)
                {
                    ShowFrontendEntryDetails(entry);
                    return;
                }
            }
        }

        if (!Guid.TryParse(nodeId, out var guid)) return;

        if (_pov != null)
        {
            var view = _scene.View(_pov);
            var entry = view.Entries.FirstOrDefault(e => e.Source.Id == guid);
            if (entry != null)
            {
                ShowFrontendEntryDetails(entry);
                return;
            }
        }
    }

    private void OnBackViewerClick(object? sender, MouseEventArgs e)
    {
        var obj = _backViewer.GetObjectAt(e.X, e.Y);
        if (obj is not Microsoft.Msagl.Drawing.IViewerNode vn) return;
        var nodeId = vn.Node.Id;

        if (Guid.TryParse(nodeId, out var guid) && _scene.Elements.TryGetValue(guid, out var element))
            ShowBackendElementDetails(element);
    }

    // ── Frontend details ─────────────────────────────────────────

    private void ShowFrontendEntryDetails(SceneViewEntry entry)
    {
        var el = entry.Source;
        var lines = new List<string>
        {
            $"Type: {el.GetType().Name}",
            $"Name: {el.DisplayName}",
            "",
            "─── Observation Keywords ───",
        };
        foreach (var kic in entry.ObservationKeywords)
            lines.Add($"  [{kic.Keyword}] {kic.Context}");

        lines.Add("");
        lines.Add($"─── Applicable Verbs ({entry.ApplicableVerbs.Count}) ───");
        foreach (var vv in entry.ApplicableVerbs)
        {
            var targetName = vv.Target?.DisplayName ?? "(self)";
            lines.Add($"  • \"{vv.Verbatim}\"  [{vv.Verb.DisplayName} → {targetName}]");
        }

        if (el.Descriptions.Count > 0)
        {
            lines.Add("");
            lines.Add("─── Descriptions ───");
            lines.AddRange(el.Descriptions.Select(d => $"  {d}"));
        }

        _frontDetailsBox.Text = string.Join(Environment.NewLine, lines);
    }

    // ── Backend details ──────────────────────────────────────────

    private void ShowBackendElementDetails(Element element)
    {
        _backDetailsBox.Clear();
        var lines = new List<string>
        {
            $"Type: {element.GetType().Name}",
            $"UUID: {element.Id}",
            $"Name: {element.DisplayName}",
            "",
            "─── Descriptions ───",
        };
        lines.AddRange(element.Descriptions.Select(d => $"  {d}"));

        lines.Add("");
        lines.Add("─── Keywords ───");
        foreach (var kic in element.Keywords)
            lines.Add($"  [{kic.Keyword}] {kic.Context}");

        lines.Add("");
        lines.Add("─── State Properties ───");
        if (element.StateProperties.Count == 0)
            lines.Add("  (none)");
        else
            lines.AddRange(element.StateProperties.Select(s => $"  {s.GetType().Name}.{s}"));

        if (_pov != null)
        {
            lines.Add("");
            lines.Add("─── Applicable Verbs ───");
            foreach (var verb in _scene.Verbs)
            {
                var possible = verb.IsPossible(_scene, _pov, element);
                var verbatim = possible ? verb.Verbatim(_scene, _pov, element) : "(n/a)";
                var marker   = possible ? "✓" : "✗";
                lines.Add($"  {marker} {verb.DisplayName}: {verbatim}");
            }
        }

        if (element is Area area)
        {
            lines.Add("");
            lines.Add("─── Area Info ───");
            lines.Add($"  Context: {area.ContextDescription}");
            lines.Add($"  Transition: {area.TransitionDescription}");
            lines.Add($"  Spots: {area.Spots.Count}");
            var reachable = _scene.GetReachableAreas(area);
            lines.Add($"  Connects to: {string.Join(", ", reachable.Select(a => a.DisplayName))}");
        }
        else if (element is Spot spot)
        {
            lines.Add("");
            lines.Add("─── Spot Info ───");
            lines.Add($"  Items: {string.Join(", ", spot.Items.Select(i => i.DisplayName))}");
        }
        else if (element is SceneNpc npc)
        {
            lines.Add("");
            lines.Add("─── NPC Info ───");
            lines.Add($"  Species: {npc.Entity.Archetype.Species.DisplayName}");
            lines.Add($"  Archetype: {npc.Entity.Archetype.ArchetypeId}");
            lines.Add($"  Hostile: {npc.IsHostile}");
            lines.Add($"  Alive: {npc.IsAlive}");
            if (_scene.NpcSchedules.TryGetValue(npc.Id, out var schedule))
            {
                lines.Add("  Schedule:");
                foreach (var (period, nodeId) in schedule.ActivePeriods)
                    lines.Add($"    {period}: {nodeId}");
            }
        }

        _backDetailsBox.Text = string.Join(Environment.NewLine, lines);
    }

    // ── PoV and State panels ─────────────────────────────────────

    private void UpdatePovLabel()
    {
        if (_pov == null)
        {
            _povLabel.Text = "  PoV: (not set)";
            return;
        }

        var focusName = _pov.Focus?.DisplayName ?? "(none)";
        _povLabel.Text = $"  PoV — Where: {_pov.Where.DisplayName}  |  When: {_pov.When}  |  Focus: {focusName}";
    }

    private void UpdateStateBox()
    {
        _stateBox.Clear();
        if (_scene.StateChanges.IsEmpty)
        {
            _stateBox.Text = "  (no state changes)";
            return;
        }

        var lines = new List<string>();
        foreach (var (elementId, changes) in _scene.StateChanges.Changes)
        {
            var name = _scene.Elements.TryGetValue(elementId, out var el) ? el.DisplayName : elementId.ToString();
            lines.Add($"  {name}: {string.Join(", ", changes)}");
        }
        _stateBox.Text = string.Join(Environment.NewLine, lines);
    }
}
