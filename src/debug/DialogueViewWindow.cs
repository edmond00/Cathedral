using Label       = System.Windows.Forms.Label;
using Orientation = System.Windows.Forms.Orientation;
using FontStyle   = System.Drawing.FontStyle;
using MsaglGraph  = Microsoft.Msagl.Drawing.Graph;
using MsaglColor  = Microsoft.Msagl.Drawing.Color;
using MsaglShape  = Microsoft.Msagl.Drawing.Shape;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Cathedral.Game.Dialogue.Tree;
using Microsoft.Msagl.GraphViewerGdi;

namespace Cathedral.Debug;

/// <summary>
/// WinForms window that renders every registered <see cref="DialogueTree"/> as a graph, one tab per
/// tree. Nodes are colour-coded by speaker: NPC lines, player options ("You"), and the resolution
/// (the single dice check with its success/failure lines and outcomes). Neutral replica text is shown
/// verbatim including <c>{scope:field}</c> template tokens, so authoring is easy to inspect.
/// Purely static — no LLM, no game state. Reuses the scene debug window's MSAGL <see cref="GViewer"/>.
/// </summary>
public class DialogueViewWindow : Form
{
    private readonly TabControl _tabs;

    // ── Node fill colours ────────────────────────────────────────
    private static readonly MsaglColor ColorNpc        = new( 90, 145, 210); // blue  — NPC speaks
    private static readonly MsaglColor ColorPlayer     = new( 90, 190, 110); // green — "You" option
    private static readonly MsaglColor ColorResolution = new(210, 160,  50); // amber — dice check

    private static readonly MsaglColor BorderNormal = new(80, 80, 90);

    public DialogueViewWindow()
    {
        Text        = "Dialogue Trees — Speaker View";
        Width       = 1400;
        Height      = 920;
        MinimumSize = new Size(900, 650);
        BackColor   = Color.FromArgb(30, 30, 30);

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10, FontStyle.Bold),
        };

        foreach (var tree in DialogueTreeRegistry.Instance.All.OrderBy(t => t.DisplayName))
            _tabs.TabPages.Add(BuildTreeTab(tree));

        Controls.Add(_tabs);
        Controls.Add(BuildLegendPanel());
    }

    // ══════════════════════════════════════════════════════════════
    //  ONE TAB PER TREE
    // ══════════════════════════════════════════════════════════════

    private TabPage BuildTreeTab(DialogueTree tree)
    {
        var tab = new TabPage(tree.DisplayName) { BackColor = Color.FromArgb(30, 30, 30) };

        var split = new SplitContainer
        {
            Dock        = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor   = Color.FromArgb(30, 30, 30),
        };
        split.SizeChanged += (_, _) =>
        {
            try
            {
                if (split.Width < 400) return;
                split.Panel2MinSize    = Math.Min(340, split.Width / 3);
                split.SplitterDistance = (int)(split.Width * 0.66);
            }
            catch (InvalidOperationException) { }
        };

        var detailsById = new Dictionary<string, string>();
        var viewer = new GViewer { Dock = DockStyle.Fill, NavigationVisible = true };
        viewer.Graph = BuildTreeGraph(tree, detailsById);

        var detailsBox = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            Font        = new Font("Consolas", 9),
            BackColor   = Color.FromArgb(28, 28, 28),
            ForeColor   = Color.LightGray,
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
        };
        var detailsHeader = new Label
        {
            Text = "  Node Details (click a node)", Dock = DockStyle.Top, Height = 22,
            Font = new Font("Consolas", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White,
        };
        var subjectBox = new RichTextBox
        {
            Dock        = DockStyle.Top,
            Height      = 88,
            ReadOnly    = true,
            Font        = new Font("Consolas", 9),
            BackColor   = Color.FromArgb(35, 35, 40),
            ForeColor   = Color.FromArgb(200, 220, 255),
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            Text = $"Tree id : {tree.TreeId}\r\n"
                 + $"Verb    : {tree.AssociatedVerbId}\r\n"
                 + $"Subject : {tree.Description}",
        };

        var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30) };
        right.Controls.Add(detailsBox);
        right.Controls.Add(detailsHeader);
        right.Controls.Add(subjectBox);

        viewer.MouseClick += (_, e) =>
        {
            var obj = viewer.GetObjectAt(e.X, e.Y);
            if (obj is Microsoft.Msagl.Drawing.IViewerNode vn && detailsById.TryGetValue(vn.Node.Id, out var text))
                detailsBox.Text = text;
        };

        split.Panel1.Controls.Add(viewer);
        split.Panel2.Controls.Add(right);
        tab.Controls.Add(split);
        return tab;
    }

    // ══════════════════════════════════════════════════════════════
    //  GRAPH CONSTRUCTION
    // ══════════════════════════════════════════════════════════════

    private MsaglGraph BuildTreeGraph(DialogueTree tree, Dictionary<string, string> detailsById)
    {
        var msagl = new MsaglGraph(tree.TreeId)
        {
            LayoutAlgorithmSettings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings(),
        };

        var visited = new HashSet<string>();
        AddNpcNode(msagl, tree.EntryNode, visited, detailsById, isEntry: true);
        return msagl;
    }

    private void AddNpcNode(MsaglGraph msagl, NpcLineNode node, HashSet<string> visited,
                            Dictionary<string, string> detailsById, bool isEntry = false)
    {
        if (!visited.Add(node.NodeId)) return;

        string title = isEntry ? $"★ NPC: {node.NodeId}" : $"NPC: {node.NodeId}";
        AddNode(msagl, node.NodeId, $"{title}\n“{node.Replica}”", ColorNpc);
        detailsById[node.NodeId] =
            $"NPC line node: {node.NodeId}\r\nSpeaker: NPC\r\n\r\nReplica:\r\n  {node.Replica}\r\n\r\nOptions: {node.Options.Count}";

        foreach (var opt in node.Options)
        {
            string optId = $"{node.NodeId}::{opt.OptionId}";
            AddNode(msagl, optId, $"You: {opt.OptionId}\nintent: {opt.Intent}\n“{opt.Replica}”", ColorPlayer);
            detailsById[optId] =
                $"Player option: {opt.OptionId}\r\nSpeaker: You\r\n\r\nIntent (for MM grading):\r\n  {opt.Intent}\r\n\r\nReplica:\r\n  {opt.Replica}\r\n\r\nLeads to: {opt.Next.NodeId}";
            msagl.AddEdge(node.NodeId, optId).Attr.Color = new MsaglColor(120, 120, 130);

            switch (opt.Next)
            {
                case NpcLineNode npcNext:
                    AddNpcNode(msagl, npcNext, visited, detailsById);
                    msagl.AddEdge(optId, npcNext.NodeId).Attr.Color = new MsaglColor(120, 120, 130);
                    break;
                case ResolutionNode res:
                    AddResolutionNode(msagl, res, visited, detailsById);
                    msagl.AddEdge(optId, res.NodeId).Attr.Color = new MsaglColor(120, 120, 130);
                    break;
            }
        }
    }

    private void AddResolutionNode(MsaglGraph msagl, ResolutionNode res, HashSet<string> visited,
                                   Dictionary<string, string> detailsById)
    {
        if (!visited.Add(res.NodeId)) return;

        string label = $"⊙ CHECK: {res.NodeId}  (need {res.Difficulty}×6)\n"
                     + $"✓ “{res.SuccessReplica}”\n"
                     + $"✗ “{res.FailureReplica}”";
        AddNode(msagl, res.NodeId, label, ColorResolution);

        var lines = new List<string>
        {
            $"Resolution node: {res.NodeId}",
            $"Speaker: NPC (post-check)",
            $"Difficulty: {res.Difficulty} six(es) needed",
            "",
            $"Success line:\r\n  {res.SuccessReplica}",
            $"Failure line:\r\n  {res.FailureReplica}",
            "",
            $"Outcomes ({res.Outcomes.Count}):",
        };
        if (res.Outcomes.Count == 0) lines.Add("  (none)");
        else foreach (var oc in res.Outcomes) lines.Add($"  [{oc.Condition}] {oc.Outcome.Description}");
        detailsById[res.NodeId] = string.Join("\r\n", lines);
    }

    private static void AddNode(MsaglGraph msagl, string id, string label, MsaglColor fill)
    {
        var node = msagl.AddNode(id);
        node.LabelText       = label;
        node.Attr.FillColor  = fill;
        node.Attr.Color      = BorderNormal;
        node.Attr.Shape      = MsaglShape.Box;
        node.Label.FontColor = new MsaglColor(245, 245, 245);
        node.Label.FontSize  = 9;
    }

    private Panel BuildLegendPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 24,
            BackColor = Color.FromArgb(45, 45, 48),
        };

        var items = new (Color fill, string text)[]
        {
            (Color.FromArgb( 90, 145, 210), " NPC line "),
            (Color.FromArgb( 90, 190, 110), " You (option) "),
            (Color.FromArgb(210, 160,  50), " Resolution (dice check) "),
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
}
