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
using Microsoft.Msagl.GraphViewerGdi;

namespace Cathedral.Debug;

/// <summary>
/// WinForms window that renders the narration graph for the current location using MSAGL.
/// Opened automatically when --debug is active. Highlights the current node in real-time
/// and shows full node details in a side panel on click.
/// </summary>
public class NarrationGraphWindow : Form
{
    private readonly GViewer _viewer;
    private readonly RichTextBox _detailsBox;
    private readonly MsaglGraph _msaglGraph;
    private string _currentNodeId;
    private readonly Dictionary<string, NarrationNode> _allNodes = new();

    // ── Node fill colours (by category priority: entry > encounter > items > plain) ──
    private static readonly MsaglColor ColorEntry       = new( 90, 185, 110); // green   — entry node
    private static readonly MsaglColor ColorEncounter   = new(210,  90,  90); // red     — has encounters
    private static readonly MsaglColor ColorItems       = new( 90, 145, 210); // blue    — has items only
    private static readonly MsaglColor ColorPlain       = new(160, 160, 170); // gray    — no items, no encounters
    private static readonly MsaglColor ColorObservation = new(210, 160,  50); // amber   — ObservationObject

    // ── Border styles ────────────────────────────────────────────────
    private static readonly MsaglColor BorderNormal  = new( 80,  80,  90); // thin dark border (non-current)
    private static readonly MsaglColor BorderCurrent = new(255, 220,   0); // thick yellow border (current node)
    private const double LineWidthNormal  = 1.0;
    private const double LineWidthCurrent = 4.0;

    public NarrationGraphWindow(NarrationNode entryNode, int locationId)
    {
        Text = $"Narration Graph — Location {locationId}";
        Width  = 1200;
        Height = 820;
        MinimumSize = new Size(700, 500);

        // ── title bar with inline coloured legend ─────────────────────
        var titlePanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 26,
            BackColor = Color.FromArgb(45, 45, 48),
        };

        // Location label (left side)
        var locationLabel = new Label
        {
            Text      = $"Location {locationId}  •  Entry: {entryNode.NodeId}",
            AutoSize  = true,
            Font      = new Font("Consolas", 9, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location  = new Point(8, 5),
        };
        titlePanel.Controls.Add(locationLabel);

        // Legend items: (fill color, border color, label)
        var legendItems = new (Color fill, Color border, string text)[]
        {
            (Color.FromArgb( 90, 185, 110), Color.FromArgb( 80,  80,  90), " entry "),
            (Color.FromArgb(210,  90,  90), Color.FromArgb( 80,  80,  90), " encounter "),
            (Color.FromArgb( 90, 145, 210), Color.FromArgb( 80,  80,  90), " items "),
            (Color.FromArgb(160, 160, 170), Color.FromArgb( 80,  80,  90), " plain "),
            (Color.FromArgb(210, 160,  50), Color.FromArgb( 80,  80,  90), " observation "),
            (Color.FromArgb(45,  45,  48),  Color.FromArgb(255, 220,   0), " current "),
        };

        int legendX = 500;
        foreach (var (fill, border, text) in legendItems)
        {
            // Coloured square
            var square = new Panel
            {
                Size      = new Size(14, 14),
                BackColor = fill,
                Location  = new Point(legendX, 6),
            };
            square.Paint += (_, pe) =>
            {
                pe.Graphics.DrawRectangle(new System.Drawing.Pen(border, 2), 1, 1, 11, 11);
            };
            titlePanel.Controls.Add(square);
            legendX += 18;

            // Text label
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

        // ── SplitContainer: graph left | details right ────────────────
        var split = new SplitContainer
        {
            Dock        = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor   = Color.FromArgb(30, 30, 30),
        };
        // Panel2MinSize and SplitterDistance both call set_SplitterDistance internally.
        // They must be deferred to Load when the control has its real pixel dimensions.
        Load += (_, _) =>
        {
            split.Panel2MinSize    = 260;
            split.SplitterDistance = (int)(split.Width * 0.70);
        };

        // Left: MSAGL GViewer
        _viewer = new GViewer
        {
            Dock              = DockStyle.Fill,
            NavigationVisible = true,
        };

        // Right: details header + richtext
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

        // ── build graph ──────────────────────────────────────────────
        CollectAllNodes(entryNode);
        _currentNodeId = entryNode.NodeId;
        _msaglGraph    = BuildMsaglGraph(entryNode);
        _viewer.Graph  = _msaglGraph;

        // GViewer sets SelectedObject before raising MouseClick, so we
        // subscribe to MouseClick to detect node selection.
        _viewer.MouseClick += OnViewerMouseClick;

        // Show entry node details immediately
        ShowNodeDetails(entryNode);
    }

    // ── Public API ───────────────────────────────────────────────────

    /// <summary>
    /// Highlights <paramref name="newNodeId"/> as the current node (thick yellow border).
    /// Thread-safe — may be called from the game thread.
    /// </summary>
    public void UpdateCurrentNode(string newNodeId)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateCurrentNode(newNodeId));
            return;
        }

        // Restore previous node to its normal border
        if (_currentNodeId.Length > 0 && _msaglGraph.FindNode(_currentNodeId) is { } prevMsagl)
        {
            prevMsagl.Attr.Color     = BorderNormal;
            prevMsagl.Attr.LineWidth = LineWidthNormal;
        }

        _currentNodeId = newNodeId;

        // Apply thick yellow border to current node
        if (_msaglGraph.FindNode(newNodeId) is { } curMsagl)
        {
            curMsagl.Attr.Color     = BorderCurrent;
            curMsagl.Attr.LineWidth = LineWidthCurrent;
        }

        // Reassign graph to force a redraw
        _viewer.Graph = _msaglGraph;

        if (_allNodes.TryGetValue(newNodeId, out var nodeData))
            ShowNodeDetails(nodeData);
    }

    // ── Private helpers ──────────────────────────────────────────────

    private void CollectAllNodes(NarrationNode entry)
    {
        var queue = new Queue<NarrationNode>();
        queue.Enqueue(entry);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (_allNodes.ContainsKey(node.NodeId)) continue;
            _allNodes[node.NodeId] = node;
            foreach (var child in node.PossibleOutcomes.OfType<NarrationNode>())
                queue.Enqueue(child);
        }
    }

    private MsaglGraph BuildMsaglGraph(NarrationNode entry)
    {
        var graph   = new MsaglGraph("narration");
        var visited = new HashSet<string>();
        var queue   = new Queue<NarrationNode>();
        queue.Enqueue(entry);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!visited.Add(node.NodeId)) continue;

            var msaglNode = graph.AddNode(node.NodeId);
            msaglNode.LabelText      = node.NodeId;
            msaglNode.Attr.Shape     = MsaglShape.Box;
            msaglNode.Attr.FillColor = NodeFillColor(node);
            msaglNode.Attr.Color     = node.NodeId == _currentNodeId ? BorderCurrent : BorderNormal;
            msaglNode.Attr.LineWidth = node.NodeId == _currentNodeId ? LineWidthCurrent : LineWidthNormal;
            msaglNode.Label.FontSize = 9;

            foreach (var outcome in node.PossibleOutcomes)
            {
                if (outcome is NarrationNode child)
                {
                    graph.AddEdge(node.NodeId, child.NodeId);
                    queue.Enqueue(child);
                }
                else if (outcome is ObservationObject obs)
                {
                    string obsId = $"obs:{obs.ObservationId}";
                    var obsNode = graph.AddNode(obsId);
                    obsNode.LabelText      = obs.ObservationId;
                    obsNode.Attr.Shape     = MsaglShape.Diamond;
                    obsNode.Attr.FillColor = ColorObservation;
                    obsNode.Attr.Color     = BorderNormal;
                    obsNode.Attr.LineWidth = LineWidthNormal;
                    obsNode.Label.FontSize = 8;
                    graph.AddEdge(node.NodeId, obsId);
                }
            }
        }

        return graph;
    }

    /// Returns the fill colour for a node based on its category.
    /// Priority: entry &gt; has encounters &gt; has items &gt; plain.
    private static MsaglColor NodeFillColor(NarrationNode node)
    {
        if (node.IsEntryNode)                      return ColorEntry;
        if (node.GetAllEncounters().Count > 0)     return ColorEncounter;
        if (node.GetAvailableItems().Count > 0)    return ColorItems;
        return ColorPlain;
    }

    private void OnViewerMouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (_viewer.SelectedObject is MsaglNode selectedNode &&
            _allNodes.TryGetValue(selectedNode.Id, out var node))
        {
            ShowNodeDetails(node);
        }
    }

    private void ShowNodeDetails(NarrationNode node)
    {
        _detailsBox.Clear();

        AppendColored($"=== {node.NodeId} ===\n",          Color.Orange,          bold: true);
        AppendColored($"Entry node: {node.IsEntryNode}\n",  Color.LightYellow);
        AppendColored("\n");

        AppendColored("Context:\n",                         Color.CornflowerBlue,  bold: true);
        AppendColored(node.ContextDescription + "\n",       Color.LightGray);
        AppendColored("\n");

        AppendColored("Transition:\n",                      Color.CornflowerBlue,  bold: true);
        AppendColored(node.TransitionDescription + "\n",    Color.LightGray);
        AppendColored("\n");

        AppendColored("Keywords:\n",                        Color.CornflowerBlue,  bold: true);
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

        var encounters = node.PossibleEncounters;
        if (encounters.Count > 0)
        {
            AppendColored("\nEncounters:\n", Color.LightCoral, bold: true);
            foreach (var enc in encounters)
                AppendColored(
                    $"  ! {enc.Archetype.ArchetypeId}  ({(int)(enc.SpawnChance * 100)}% chance, max {enc.MaxCount})\n",
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

        var connected = node.PossibleOutcomes.OfType<NarrationNode>().ToList();
        if (connected.Count > 0)
        {
            AppendColored("\nConnected nodes:\n", Color.CornflowerBlue, bold: true);
            foreach (var n in connected)
                AppendColored($"  → {n.NodeId}\n", Color.LightGray);
        }
    }

    private void AppendColored(string text, Color? color = null, bool bold = false)
    {
        int start = _detailsBox.TextLength;
        _detailsBox.AppendText(text);
        if (color.HasValue || bold)
        {
            _detailsBox.Select(start, text.Length);
            if (color.HasValue)
                _detailsBox.SelectionColor = color.Value;
            if (bold)
                _detailsBox.SelectionFont = new Font(_detailsBox.Font, FontStyle.Bold);
            _detailsBox.SelectionStart  = _detailsBox.TextLength;
            _detailsBox.SelectionLength = 0;
        }
    }
}
