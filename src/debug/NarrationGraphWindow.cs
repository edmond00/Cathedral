// Disambiguate conflicting names between WinForms, System.Drawing and MSAGL Drawing
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
using Cathedral.Game.Npc;
using Microsoft.Msagl.GraphViewerGdi;

namespace Cathedral.Debug;

/// <summary>
/// WinForms window that renders the narration graph for the current location using MSAGL.
/// Shows NPC schedules in the details panel and highlights NPC-occupied nodes.
/// </summary>
public class NarrationGraphWindow : Form
{
    private readonly GViewer _viewer;
    private readonly RichTextBox _detailsBox;
    private readonly MsaglGraph _msaglGraph;
    private readonly NarrationGraph _graph;
    private string _currentNodeId;
    private readonly Dictionary<string, NarrationNode>     _allNodes        = new();
    private readonly Dictionary<string, ObservationObject> _allObservations = new();

    // ── Node fill colours ────────────────────────────────────────────
    private static readonly MsaglColor ColorNpc         = new(140,  70, 200); // purple  — has NPCs in schedule
    private static readonly MsaglColor ColorItems       = new( 90, 145, 210); // blue    — has items only
    private static readonly MsaglColor ColorPlain       = new(160, 160, 170); // gray    — plain
    private static readonly MsaglColor ColorObservation = new(210, 160,  50); // amber   — ObservationObject

    // ── Border styles ────────────────────────────────────────────────
    private static readonly MsaglColor BorderNormal  = new( 80,  80,  90);
    private static readonly MsaglColor BorderCurrent = new(255, 220,   0);
    private const double LineWidthNormal  = 1.0;
    private const double LineWidthCurrent = 4.0;

    public NarrationGraphWindow(NarrationGraph graph, int locationId)
    {
        _graph = graph;
        Text   = $"Narration Graph — Location {locationId}";
        Width  = 1200;
        Height = 820;
        MinimumSize = new Size(700, 500);

        // ── Title bar with legend ────────────────────────────────────
        var titlePanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 26,
            BackColor = Color.FromArgb(45, 45, 48),
        };

        var locationLabel = new Label
        {
            Text      = $"Location {locationId}  •  Entry: {graph.EntryNode.NodeId}  •  NPCs: {graph.Npcs.Count}",
            AutoSize  = true,
            Font      = new Font("Consolas", 9, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location  = new Point(8, 5),
        };
        titlePanel.Controls.Add(locationLabel);

        var legendItems = new (Color fill, Color border, string text)[]
        {
            (Color.FromArgb(140,  70, 200), Color.FromArgb( 80,  80,  90), " NPC "),
            (Color.FromArgb( 90, 145, 210), Color.FromArgb( 80,  80,  90), " items "),
            (Color.FromArgb(160, 160, 170), Color.FromArgb( 80,  80,  90), " plain "),
            (Color.FromArgb(210, 160,  50), Color.FromArgb( 80,  80,  90), " observation "),
            (Color.FromArgb(45,  45,  48),  Color.FromArgb(255, 220,   0), " current "),
        };

        int legendX = 520;
        foreach (var (fill, border, text) in legendItems)
        {
            var square = new Panel { Size = new Size(14, 14), BackColor = fill, Location = new Point(legendX, 6) };
            square.Paint += (_, pe) => pe.Graphics.DrawRectangle(new System.Drawing.Pen(border, 2), 1, 1, 11, 11);
            titlePanel.Controls.Add(square);
            legendX += 18;

            var lbl = new Label
            {
                Text      = text,
                AutoSize  = true,
                Font      = new Font("Consolas", 8),
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                Location  = new Point(legendX, 5),
            };
            titlePanel.Controls.Add(lbl);
            legendX += lbl.PreferredWidth + 6;
        }

        // ── SplitContainer ───────────────────────────────────────────
        var split = new SplitContainer
        {
            Dock        = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor   = Color.FromArgb(30, 30, 30),
        };
        Load += (_, _) =>
        {
            split.Panel2MinSize    = 280;
            split.SplitterDistance = (int)(split.Width * 0.68);
        };

        _viewer = new GViewer { Dock = DockStyle.Fill, NavigationVisible = true };

        var detailsHeader = new Label
        {
            Text      = "  Node Details",
            Dock      = DockStyle.Top,
            Height    = 24,
            Font      = new Font("Consolas", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
        };

        _detailsBox = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            Font        = new Font("Consolas", 9),
            BackColor   = Color.FromArgb(28, 28, 28),
            ForeColor   = Color.LightGray,
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
        };

        split.Panel1.Controls.Add(_viewer);
        split.Panel2.Controls.Add(_detailsBox);
        split.Panel2.Controls.Add(detailsHeader);

        Controls.Add(split);
        Controls.Add(titlePanel);

        // ── Build MSAGL graph ────────────────────────────────────────
        _allNodes.Clear();
        foreach (var (id, node) in graph.AllNodes)
            _allNodes[id] = node;

        _currentNodeId = graph.EntryNode.NodeId;
        _msaglGraph    = BuildMsaglGraph(graph);
        _viewer.Graph  = _msaglGraph;

        _viewer.MouseClick += OnViewerMouseClick;
        ShowNodeDetails(graph.EntryNode);
    }

    // ── Public API ───────────────────────────────────────────────────

    public void UpdateCurrentNode(string newNodeId)
    {
        if (InvokeRequired) { Invoke(() => UpdateCurrentNode(newNodeId)); return; }

        if (_currentNodeId.Length > 0 && _msaglGraph.FindNode(_currentNodeId) is { } prev)
        {
            prev.Attr.Color     = BorderNormal;
            prev.Attr.LineWidth = LineWidthNormal;
        }

        _currentNodeId = newNodeId;

        if (_msaglGraph.FindNode(newNodeId) is { } cur)
        {
            cur.Attr.Color     = BorderCurrent;
            cur.Attr.LineWidth = LineWidthCurrent;
        }

        _viewer.Graph = _msaglGraph;

        if (_allNodes.TryGetValue(newNodeId, out var nodeData))
            ShowNodeDetails(nodeData);
    }

    // ── Private: graph building ──────────────────────────────────────

    private MsaglGraph BuildMsaglGraph(NarrationGraph graph)
    {
        var msagl   = new MsaglGraph("narration");
        var visited = new HashSet<string>();
        var queue   = new Queue<NarrationNode>();
        queue.Enqueue(graph.EntryNode);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!visited.Add(node.NodeId)) continue;

            var msaglNode = msagl.AddNode(node.NodeId);
            msaglNode.LabelText      = node.NodeId;
            msaglNode.Attr.Shape     = MsaglShape.Box;
            msaglNode.Attr.FillColor = NodeFillColor(node, graph);
            msaglNode.Attr.Color     = node.NodeId == _currentNodeId ? BorderCurrent : BorderNormal;
            msaglNode.Attr.LineWidth = node.NodeId == _currentNodeId ? LineWidthCurrent : LineWidthNormal;
            msaglNode.Label.FontSize = 9;

            foreach (var outcome in node.PossibleOutcomes)
            {
                if (outcome is NarrationNode child)
                {
                    msagl.AddEdge(node.NodeId, child.NodeId);
                    queue.Enqueue(child);
                }
                else if (outcome is ObservationObject obs)
                {
                    string obsId = $"obs:{obs.ObservationId}";
                    _allObservations.TryAdd(obsId, obs);
                    var obsNode = msagl.AddNode(obsId);
                    obsNode.LabelText      = obs.ObservationId;
                    obsNode.Attr.Shape     = MsaglShape.Diamond;
                    obsNode.Attr.FillColor = ColorObservation;
                    obsNode.Attr.Color     = BorderNormal;
                    obsNode.Attr.LineWidth = LineWidthNormal;
                    obsNode.Label.FontSize = 8;
                    msagl.AddEdge(node.NodeId, obsId);
                }
            }
        }

        return msagl;
    }

    private static MsaglColor NodeFillColor(NarrationNode node, NarrationGraph graph)
    {
        // Purple if any NPC is scheduled for this node during any period
        if (graph.GetNpcsScheduledForNode(node.NodeId).Any()) return ColorNpc;
        if (node.GetAvailableItems().Count > 0)               return ColorItems;
        return ColorPlain;
    }

    // ── Private: click handling ──────────────────────────────────────

    private void OnViewerMouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (_viewer.SelectedObject is not MsaglNode selected) return;

        if (_allNodes.TryGetValue(selected.Id, out var node))
            ShowNodeDetails(node);
        else if (_allObservations.TryGetValue(selected.Id, out var obs))
            ShowObservationDetails(obs);
    }

    // ── Private: detail panels ───────────────────────────────────────

    private void ShowNodeDetails(NarrationNode node)
    {
        _detailsBox.Clear();

        AppendColored($"=== {node.NodeId} ===\n",         Color.Orange,         bold: true);
        AppendColored("Context:\n",                        Color.CornflowerBlue, bold: true);
        AppendColored(node.ContextDescription + "\n",      Color.LightGray);
        AppendColored("\n");

        AppendColored("Transition:\n",                     Color.CornflowerBlue, bold: true);
        AppendColored(node.TransitionDescription + "\n",   Color.LightGray);
        AppendColored("\n");

        AppendColored("Indirect keywords:\n",              Color.CornflowerBlue, bold: true);
        AppendColored(string.Join(", ", node.NodeKeywordsInContext.Select(k => k.Keyword)) + "\n", Color.LightGray);

        var items = node.GetAvailableItems();
        if (items.Count > 0)
        {
            AppendColored("\nItems:\n", Color.MediumSeaGreen, bold: true);
            foreach (var item in items)
                AppendColored(
                    $"  • {item.DisplayName}\n    {string.Join(", ", item.OutcomeKeywordsInContext.Select(k => k.Keyword).Take(6))}\n",
                    Color.LightGray);
        }

        var observations = node.PossibleOutcomes.OfType<ObservationObject>().ToList();
        if (observations.Count > 0)
        {
            AppendColored("\nObservations:\n", Color.FromArgb(210, 160, 50), bold: true);
            foreach (var obs in observations)
            {
                AppendColored($"  ◇ {obs.ObservationId}  ({obs.SubOutcomes.Count} sub-outcomes)\n", Color.LightGray);
                foreach (var sub in obs.SubOutcomes)
                    AppendColored($"      • {sub.DisplayName}\n", Color.DarkGray);
            }
        }

        // NPC schedule: show which NPCs are routed here during which periods
        var scheduledNpcs = _graph.GetNpcsScheduledForNode(node.NodeId).ToList();
        if (scheduledNpcs.Count > 0)
        {
            AppendColored("\nNPCs (schedule):\n", Color.FromArgb(180, 100, 240), bold: true);
            foreach (var gnpc in scheduledNpcs)
            {
                var presentPeriods = gnpc.Schedule.ActivePeriods
                    .Where(p => p.NodeId == node.NodeId)
                    .Select(p => p.Period.Label())
                    .ToList();
                AppendColored(
                    $"  ★ {gnpc.Entity.DisplayName}  [{gnpc.Entity.Archetype.ArchetypeId}]\n",
                    Color.FromArgb(200, 150, 255));
                AppendColored(
                    $"      Periods: {string.Join(", ", presentPeriods)}\n",
                    Color.DarkGray);
                AppendColored(
                    $"      CanDialogue: {gnpc.Entity.CanDialogue}  Hostile: {gnpc.Entity.IsHostile}\n",
                    Color.DarkGray);
            }
        }

        var encounters = node.PossibleEncounters;
        if (encounters.Count > 0)
        {
            AppendColored("\nEncounter slots (graph build):\n", Color.LightCoral, bold: true);
            foreach (var enc in encounters)
                AppendColored(
                    $"  ! {enc.Archetype.ArchetypeId}  ({(int)(enc.SpawnChance * 100)}% inclusion chance, max {enc.MaxCount})\n",
                    Color.LightGray);
        }

        var connected = node.PossibleOutcomes.OfType<NarrationNode>().ToList();
        if (connected.Count > 0)
        {
            AppendColored("\nConnected nodes:\n", Color.CornflowerBlue, bold: true);
            foreach (var n in connected)
                AppendColored($"  → {n.NodeId}\n", Color.LightGray);
        }
    }

    private void ShowObservationDetails(ObservationObject obs)
    {
        _detailsBox.Clear();

        AppendColored($"◇ {obs.ObservationId}\n",              Color.FromArgb(210, 160, 50), bold: true);
        AppendColored($"{obs.GenerateNeutralDescription(0)}\n", Color.LightGray);
        AppendColored("\n");

        AppendColored("Indirect keywords:\n",                   Color.CornflowerBlue, bold: true);
        AppendColored(string.Join(", ", obs.ObservationKeywordsInContext.Select(k => k.Keyword)) + "\n", Color.LightGray);

        AppendColored("Direct keywords:\n",                     Color.CornflowerBlue, bold: true);
        AppendColored(string.Join(", ", obs.DirectObservationKeywords) + "\n", Color.LightGray);

        if (obs.SubOutcomes.Count > 0)
        {
            AppendColored("\nSub-outcomes:\n", Color.MediumSeaGreen, bold: true);
            foreach (var sub in obs.SubOutcomes)
            {
                AppendColored($"  • {sub.DisplayName}\n", Color.LightGray);
                var subKws = sub.OutcomeKeywordsInContext.Select(k => k.Keyword).Take(6).ToList();
                if (subKws.Count > 0)
                    AppendColored($"    {string.Join(", ", subKws)}\n", Color.DarkGray);
            }
        }

        if (obs.AssociatedEncounters.Count > 0)
        {
            AppendColored("\nAssociated encounter slots (graph build):\n", Color.LightCoral, bold: true);
            foreach (var enc in obs.AssociatedEncounters)
                AppendColored(
                    $"  ! {enc.Archetype.ArchetypeId}  ({(int)(enc.SpawnChance * 100)}% inclusion chance, max {enc.MaxCount})\n",
                    Color.LightGray);
        }
    }

    private void AppendColored(string text, Color? color = null, bool bold = false)
    {
        int start = _detailsBox.TextLength;
        _detailsBox.AppendText(text);
        if (color.HasValue || bold)
        {
            _detailsBox.Select(start, text.Length);
            if (color.HasValue)  _detailsBox.SelectionColor = color.Value;
            if (bold)            _detailsBox.SelectionFont  = new Font(_detailsBox.Font, FontStyle.Bold);
            _detailsBox.SelectionStart  = _detailsBox.TextLength;
            _detailsBox.SelectionLength = 0;
        }
    }
}
