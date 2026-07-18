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
using Cathedral.Game;
using Cathedral.Game.Dialogue.Tree;
using Microsoft.Msagl.GraphViewerGdi;

namespace Cathedral.Debug;

/// <summary>
/// WinForms window that renders every registered <see cref="DialogueTree"/> as a graph, one tab per
/// tree. Each node carries the neutral replica text (as fed to the persona rewriter) for both the
/// NPC opening line and the player's reply; terminal nodes also show the success/failure reaction and
/// the outcomes that fire. Purely static inspection — no LLM, no game state.
/// Reuses the same MSAGL <see cref="GViewer"/> stack as the scene debug window.
/// </summary>
public class DialogueViewWindow : Form
{
    private readonly TabControl _tabs;

    // ── Node fill colours ────────────────────────────────────────
    private static readonly MsaglColor ColorEntry        = new(  0, 160, 160); // teal
    private static readonly MsaglColor ColorIntermediate = new( 90, 145, 210); // blue
    private static readonly MsaglColor ColorTerminal     = new(210, 160,  50); // amber

    private static readonly MsaglColor BorderNormal = new(80, 80, 90);

    // ── Edge colours by branch condition ─────────────────────────
    private static readonly MsaglColor EdgeEither  = new(120, 120, 130);
    private static readonly MsaglColor EdgeSuccess = new( 90, 190, 110);
    private static readonly MsaglColor EdgeFailure = new(210,  90,  90);

    public DialogueViewWindow()
    {
        Text        = "Dialogue Trees — Neutral Replica View";
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

        var viewer = new GViewer { Dock = DockStyle.Fill, NavigationVisible = true };
        viewer.Graph = BuildTreeGraph(tree);

        // ── Right-hand info panel: tree subject + selected node details ──
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

        // Index nodes by id so clicks can resolve them (ids are unique within a tree).
        var byId = new Dictionary<string, DialogueTreeNode>();
        CollectNodes(tree.EntryNode, byId);

        viewer.MouseClick += (_, e) =>
        {
            var obj = viewer.GetObjectAt(e.X, e.Y);
            if (obj is Microsoft.Msagl.Drawing.IViewerNode vn && byId.TryGetValue(vn.Node.Id, out var node))
                detailsBox.Text = NodeDetails(node);
        };

        split.Panel1.Controls.Add(viewer);
        split.Panel2.Controls.Add(right);
        tab.Controls.Add(split);
        return tab;
    }

    // ══════════════════════════════════════════════════════════════
    //  GRAPH CONSTRUCTION
    // ══════════════════════════════════════════════════════════════

    private MsaglGraph BuildTreeGraph(DialogueTree tree)
    {
        var msagl = new MsaglGraph(tree.TreeId)
        {
            LayoutAlgorithmSettings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings(),
        };

        var visited = new HashSet<string>();
        AddNodeRecursive(msagl, tree, tree.EntryNode, visited);
        return msagl;
    }

    private void AddNodeRecursive(MsaglGraph msagl, DialogueTree tree, DialogueTreeNode node,
                                  HashSet<string> visited)
    {
        if (!visited.Add(node.NodeId)) return;

        var gnode = msagl.AddNode(node.NodeId);
        gnode.LabelText      = NodeLabel(node);
        gnode.Attr.FillColor = node.IsEntry ? ColorEntry
                             : node.IsTerminal ? ColorTerminal
                             : ColorIntermediate;
        gnode.Attr.Color     = BorderNormal;
        gnode.Attr.Shape     = node.IsTerminal ? MsaglShape.Box : MsaglShape.Box;
        gnode.Label.FontColor = new MsaglColor(245, 245, 245);
        gnode.Label.FontSize  = 9;

        foreach (var branch in node.Branches)
        {
            AddNodeRecursive(msagl, tree, branch.TargetNode, visited);
            var edge = msagl.AddEdge(node.NodeId, EdgeLabel(branch.Condition), branch.TargetNode.NodeId);
            edge.Attr.Color = ConditionColor(branch.Condition);
        }
    }

    /// <summary>Multi-line node label carrying the direct neutral replica plus its intent.</summary>
    private static string NodeLabel(DialogueTreeNode node)
    {
        string title = node.IsEntry ? $"★ {node.NodeId}"
                     : node.IsTerminal ? $"⊙ {node.NodeId}"
                     : node.NodeId;

        var lines = new List<string>
        {
            title,
            $"“{NeutralNarration.NpcOpening(node.Replica)}”",
            $"intent: {node.Description}",
        };

        if (node.IsTerminal)
        {
            lines.Add($"✓ {NeutralNarration.NpcReaction(true)}");
            lines.Add($"✗ {NeutralNarration.NpcReaction(false)}");
            foreach (var oc in node.Outcomes)
                lines.Add($"⇒ [{EdgeLabel(oc.Condition, "always")}] {oc.Outcome.Description}");
        }

        return string.Join("\n", lines);
    }

    // ══════════════════════════════════════════════════════════════
    //  DETAILS PANEL
    // ══════════════════════════════════════════════════════════════

    private static string NodeDetails(DialogueTreeNode node)
    {
        var lines = new List<string>
        {
            $"Node id     : {node.NodeId}",
            $"Kind        : {(node.IsEntry ? "entry " : "")}{(node.IsTerminal ? "terminal" : "intermediate")}".Trim(),
            $"Intent      : {node.Description}",
            "",
            "─── Direct neutral replica ───",
            $"Spoken line : {NeutralNarration.NpcOpening(node.Replica)}",
            "  (same line whether the NPC opens with it or the player replies with it)",
        };

        if (node.IsTerminal)
        {
            lines.Add($"React (win) : {NeutralNarration.NpcReaction(true)}");
            lines.Add($"React (lose): {NeutralNarration.NpcReaction(false)}");
            lines.Add("");
            lines.Add($"─── Outcomes ({node.Outcomes.Count}) ───");
            if (node.Outcomes.Count == 0)
                lines.Add("  (none)");
            else
                foreach (var oc in node.Outcomes)
                    lines.Add($"  [{oc.Condition}] {oc.Outcome.Description}");
        }
        else
        {
            lines.Add("");
            lines.Add($"─── Branches ({node.Branches.Count}) ───");
            foreach (var b in node.Branches)
                lines.Add($"  [{b.Condition}] → {b.TargetNode.NodeId}  ({b.TargetNode.Description})");
        }

        return string.Join(Environment.NewLine, lines);
    }

    // ══════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════

    private static void CollectNodes(DialogueTreeNode node, Dictionary<string, DialogueTreeNode> into)
    {
        if (into.ContainsKey(node.NodeId)) return;
        into[node.NodeId] = node;
        foreach (var b in node.Branches) CollectNodes(b.TargetNode, into);
    }

    private static string EdgeLabel(BranchCondition c, string eitherText = "") => c switch
    {
        BranchCondition.Success => "success",
        BranchCondition.Failure => "failure",
        _                       => eitherText,
    };

    private static MsaglColor ConditionColor(BranchCondition c) => c switch
    {
        BranchCondition.Success => EdgeSuccess,
        BranchCondition.Failure => EdgeFailure,
        _                       => EdgeEither,
    };

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
            (Color.FromArgb(  0, 160, 160), " entry "),
            (Color.FromArgb( 90, 145, 210), " step "),
            (Color.FromArgb(210, 160,  50), " terminal "),
            (Color.FromArgb( 90, 190, 110), " success edge "),
            (Color.FromArgb(210,  90,  90), " failure edge "),
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
