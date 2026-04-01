using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

// Explicit aliases — we don't use UseWindowsForms, so no implicit global usings.
using Label       = System.Windows.Forms.Label;
using Orientation = System.Windows.Forms.Orientation;
using FontStyle   = System.Drawing.FontStyle;
using Color       = System.Drawing.Color;

namespace Cathedral.Debug;

/// <summary>
/// WinForms window that displays LLM slot communications read from the session log directory.
/// Layout:
///   ┌──────────────────────────────────────────────────────┐
///   │  Title bar                                           │
///   ├──────────────┬───────────────────────────────────────┤
///   │ Slot list    │  Conversation view (RichTextBox)      │
///   │ (ListBox)    │                                       │
///   │              │                                       │
///   ├──────────────┴───────────────────────────────────────┤
///   │  Status strip                                        │
///   └──────────────────────────────────────────────────────┘
/// </summary>
public sealed class LlmMonitorWindow : Form
{
    // ─── colours ────────────────────────────────────────────────────────────
    private static readonly Color BgDark        = Color.FromArgb(18, 18, 24);
    private static readonly Color BgPanel       = Color.FromArgb(24, 24, 32);
    private static readonly Color FgDefault     = Color.FromArgb(200, 200, 210);
    private static readonly Color FgTitle       = Color.FromArgb(180, 160, 230);   // purple-ish
    private static readonly Color FgHeader      = Color.FromArgb(100, 160, 220);   // cornflower
    private static readonly Color FgSystem      = Color.FromArgb(160, 110, 200);   // violet
    private static readonly Color FgUser        = Color.FromArgb(100, 185, 240);   // sky blue
    private static readonly Color FgAssistant   = Color.FromArgb(100, 200, 140);   // sea green
    private static readonly Color FgTiming      = Color.FromArgb(220, 195, 80);    // gold
    private static readonly Color FgEmpty       = Color.FromArgb(210, 100, 100);   // coral
    private static readonly Color FgGbnf        = Color.FromArgb(130, 130, 145);   // muted gray
    private static readonly Color FgContext     = Color.FromArgb(80,  200, 180);   // teal
    private static readonly Color FgListNormal  = Color.FromArgb(190, 190, 200);
    private static readonly Color FgListSelect  = Color.FromArgb(255, 220, 160);
    private static readonly Color BgListSelect  = Color.FromArgb(50, 50, 70);
    private static readonly Color BgStatus      = Color.FromArgb(30, 30, 42);
    private static readonly Color FgStatus      = Color.FromArgb(120, 120, 140);

    // ─── controls ───────────────────────────────────────────────────────────
    private readonly ListBox      _slotList;
    private readonly RichTextBox  _conversation;
    private readonly Label        _statusLabel;

    // ─── state ──────────────────────────────────────────────────────────────
    private readonly string               _logsBaseDir;
    private string?                       _sessionDir;          // null until a session is discovered
    private readonly List<SlotEntry>      _slots = new();
    private int                           _selectedSlotIndex = -1;
    private FileSystemWatcher?            _sessionWatcher;      // watches logs/ for new sessions
    private FileSystemWatcher?            _slotWatcher;         // watches current session dir
    private System.Threading.Timer?       _debounceTimer;
    private const int DebounceMs = 350;

    // ─── constructor ────────────────────────────────────────────────────────
    public LlmMonitorWindow(string logsBaseDir)
    {
        _logsBaseDir = logsBaseDir;

        // ── form setup ──────────────────────────────────────────────────────
        Text            = "LLM Monitor — waiting for session...";
        Size            = new Size(1100, 700);
        MinimumSize     = new Size(700, 450);
        BackColor       = BgDark;
        ForeColor       = FgDefault;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition   = FormStartPosition.Manual;
        Location        = new Point(20, 60);

        // ── outer layout ────────────────────────────────────────────────────
        var split = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Vertical,
            BackColor        = BgDark,
            Panel1MinSize    = 200,
            FixedPanel       = FixedPanel.Panel1,
        };

        // ── left panel: slot list ────────────────────────────────────────────
        var listLabel = new Label
        {
            Text      = "SLOTS",
            Dock      = DockStyle.Top,
            Height    = 26,
            Font      = new Font("Consolas", 9f, FontStyle.Bold),
            ForeColor = FgTitle,
            BackColor = BgPanel,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
        };

        _slotList = new ListBox
        {
            Dock            = DockStyle.Fill,
            BackColor       = BgPanel,
            ForeColor       = FgListNormal,
            Font            = new Font("Consolas", 9f),
            BorderStyle     = BorderStyle.None,
            DrawMode        = DrawMode.OwnerDrawVariable,
            IntegralHeight  = false,
        };
        _slotList.MeasureItem   += SlotList_MeasureItem;
        _slotList.DrawItem      += SlotList_DrawItem;
        _slotList.SelectedIndexChanged += SlotList_SelectedIndexChanged;

        split.Panel1.Controls.Add(_slotList);
        split.Panel1.Controls.Add(listLabel);

        // ── right panel: conversation view ──────────────────────────────────
        _conversation = new RichTextBox
        {
            Dock         = DockStyle.Fill,
            ReadOnly     = true,
            BackColor    = BgDark,
            ForeColor    = FgDefault,
            Font         = new Font("Consolas", 9f),
            BorderStyle  = BorderStyle.None,
            WordWrap     = true,
            ScrollBars   = RichTextBoxScrollBars.Vertical,
        };

        split.Panel2.Controls.Add(_conversation);

        // ── status strip ─────────────────────────────────────────────────────
        _statusLabel = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 22,
            BackColor = BgStatus,
            ForeColor = FgStatus,
            Font      = new Font("Consolas", 8f),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0),
            Text      = "Watching for LLM activity...",
        };

        Controls.Add(split);
        Controls.Add(_statusLabel);

        // ── events ───────────────────────────────────────────────────────────
        Load        += OnLoad;
        FormClosed  += OnFormClosed;
    }

    // ─── load ───────────────────────────────────────────────────────────────
    private void OnLoad(object? sender, EventArgs e)
    {
        var split = Controls.OfType<SplitContainer>().First();
        split.Panel2MinSize    = 400;
        split.SplitterDistance = 260;

        // Try to find the latest already-existing session in logs/
        var latestSession = FindLatestSessionDir();
        if (latestSession != null)
            AttachToSession(latestSession);
        else
            SetStatus("No LLM session yet — waiting for server to start...");

        // Watch the logs/ root so we pick up any NEW session that gets created.
        StartLogsRootWatcher();
    }

    // ─── session watcher (logs/ root) ────────────────────────────────────────
    private void StartLogsRootWatcher()
    {
        if (!Directory.Exists(_logsBaseDir)) return;

        _sessionWatcher = new FileSystemWatcher(_logsBaseDir)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.DirectoryName,
            EnableRaisingEvents = true,
        };
        _sessionWatcher.Created += OnLogsRootChange;
    }

    private void OnLogsRootChange(object sender, FileSystemEventArgs e)
    {
        // A new subdirectory appeared in logs/ — check if it's a session dir.
        if (!e.Name?.StartsWith("llm_session_") ?? true) return;
        // Give the directory a moment to be fully created before switching.
        var newDir = e.FullPath;
        _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _debounceTimer = new System.Threading.Timer(_ =>
        {
            try { Invoke(() => AttachToSession(newDir)); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }, null, 500, Timeout.Infinite);
    }

    // ─── slot watcher (session dir) ──────────────────────────────────────────
    private void AttachToSession(string sessionDir)
    {
        _sessionDir = sessionDir;
        Text = $"LLM Monitor — {Path.GetFileName(sessionDir)}";

        // Dispose previous slot watcher
        _slotWatcher?.Dispose();
        _slotWatcher = null;

        ScanAndRefresh();

        if (!Directory.Exists(sessionDir)) return;

        _slotWatcher = new FileSystemWatcher(sessionDir)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
                         | NotifyFilters.DirectoryName
                         | NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
        };
        _slotWatcher.Created += OnSlotFsChange;
        _slotWatcher.Changed += OnSlotFsChange;
        _slotWatcher.Renamed += OnSlotFsChange;
    }

    private void OnSlotFsChange(object sender, FileSystemEventArgs e)
    {
        // Debounce: reset timer on every FS event burst
        _debounceTimer?.Change(DebounceMs, Timeout.Infinite);
        _debounceTimer ??= new System.Threading.Timer(_ =>
        {
            try { Invoke(ScanAndRefresh); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }, null, DebounceMs, Timeout.Infinite);
        _debounceTimer.Change(DebounceMs, Timeout.Infinite);
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        _sessionWatcher?.Dispose();
        _slotWatcher?.Dispose();
        _debounceTimer?.Dispose();
    }

    private string? FindLatestSessionDir()
    {
        if (!Directory.Exists(_logsBaseDir)) return null;
        return Directory.GetDirectories(_logsBaseDir, "llm_session_*")
            .OrderByDescending(d => d)
            .FirstOrDefault();
    }

    private void SetStatus(string text) => _statusLabel.Text = text;

    // ─── scanning ───────────────────────────────────────────────────────────
    private record SlotEntry(int SlotId, string Dir, string SlotName, int RequestCount);

    /// <summary>
    /// Extracts a short human-readable name from the system prompt text.
    /// Priority:
    ///   1. "inner voice of SKILL_NAME" → e.g. "Mycology"
    ///   2. "You are a ROLE" / "You are the ROLE" → e.g. "Critic Evaluator"
    ///   3. First non-empty line, truncated to 50 chars
    /// </summary>
    private static string ExtractSlotName(string systemPrompt)
    {
        // Pattern 1: "inner voice of WORD" — covers all ModusMentis personas
        var m = Regex.Match(systemPrompt,
            @"inner voice of ([A-Z][A-Z0-9_\- ]+)",
            RegexOptions.IgnoreCase);
        if (m.Success)
        {
            // Title-case the captured name (e.g. "ALGEBRAIC ANALYSIS" → "Algebraic Analysis")
            var raw = m.Groups[1].Value.Trim(' ', ',', '.');
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo
                       .ToTitleCase(raw.ToLowerInvariant());
        }

        // Pattern 2: "You are [a/the] ROLE [,.]" — covers Critic, Narrator, etc.
        m = Regex.Match(systemPrompt,
            @"^You are (?:a |the |an )?([^.,\n]{3,40})",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (m.Success)
        {
            var raw = m.Groups[1].Value.Trim();
            // Capitalise first letter only
            return char.ToUpper(raw[0]) + raw[1..];
        }

        // Fallback: first non-empty line, truncated
        var firstLine = systemPrompt
            .Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length > 0) ?? "(no prompt)";
        return firstLine.Length > 50 ? firstLine[..47] + "..." : firstLine;
    }

    private void ScanAndRefresh()
    {
        if (_sessionDir == null || !Directory.Exists(_sessionDir)) return;

        var slotDirs = Directory.GetDirectories(_sessionDir, "slot_*")
            .OrderBy(SlotSortKey)
            .ToList();

        _slots.Clear();
        foreach (var dir in slotDirs)
        {
            var dirName = Path.GetFileName(dir);
            if (!int.TryParse(dirName.Replace("slot_", ""), out int slotId)) continue;

            var systemPromptFile = Path.Combine(dir, "system_prompt.txt");
            string slotName = "(no system prompt)";
            if (File.Exists(systemPromptFile))
            {
                var promptText = File.ReadAllText(systemPromptFile);
                slotName = ExtractSlotName(promptText);
            }

            int reqCount = Directory.GetDirectories(dir, "request_*").Length;
            _slots.Add(new SlotEntry(slotId, dir, slotName, reqCount));
        }

        // Rebuild list items
        int prevSelection = _slotList.SelectedIndex;
        _slotList.BeginUpdate();
        _slotList.Items.Clear();
        foreach (var s in _slots)
            _slotList.Items.Add($"Slot {s.SlotId,2}  [{s.RequestCount} req]");
        _slotList.EndUpdate();

        // Restore selection if still valid
        if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _slotList.Items.Count)
            _slotList.SelectedIndex = _selectedSlotIndex;
        else if (_slotList.Items.Count > 0)
            _slotList.SelectedIndex = 0;

        // Refresh conversation if the selected slot changed or has new content
        if (_slotList.SelectedIndex >= 0)
            ShowSlot(_slotList.SelectedIndex);

        SetStatus($"Session: {Path.GetFileName(_sessionDir)}  |  {_slots.Count} slot(s)  |  {DateTime.Now:HH:mm:ss}");
    }

    private static int SlotSortKey(string dir)
    {
        var name = Path.GetFileName(dir);
        return int.TryParse(name.Replace("slot_", ""), out int n) ? n : 999;
    }

    // ─── slot display ────────────────────────────────────────────────────────
    private void SlotList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_slotList.SelectedIndex < 0) return;
        _selectedSlotIndex = _slotList.SelectedIndex;
        ShowSlot(_selectedSlotIndex);
    }

    private void ShowSlot(int index)
    {
        if (index < 0 || index >= _slots.Count) return;
        var slot = _slots[index];

        _conversation.SuspendLayout();
        _conversation.Clear();

        // ── system prompt block ─────────────────────────────────────────────
        var systemPromptFile = Path.Combine(slot.Dir, "system_prompt.txt");
        AppendLine("═══════════════════════════════════════════════════════════", FgSystem);
        AppendLine($"  SLOT {slot.SlotId}  —  {slot.SlotName.ToUpperInvariant()}  —  SYSTEM PROMPT", FgSystem, bold: true);
        AppendLine("═══════════════════════════════════════════════════════════", FgSystem);
        if (File.Exists(systemPromptFile))
        {
            var text = File.ReadAllText(systemPromptFile);
            AppendLine(text.TrimEnd(), FgSystem);
        }
        else
        {
            AppendLine("  (no system_prompt.txt found)", FgGbnf);
        }
        AppendLine("", FgDefault);

        // ── per-request blocks ──────────────────────────────────────────────
        var rawRequestDirs = Directory.Exists(slot.Dir)
            ? Directory.GetDirectories(slot.Dir, "request_*")
            : Array.Empty<string>();
        var requestDirs = rawRequestDirs.OrderBy(d => Path.GetFileName(d)).ToList();

        if (requestDirs.Count == 0)
        {
            AppendLine("  No requests logged yet.", FgGbnf);
        }

        // Build a sorted list of cache-reset markers: seq → timestamp string
        var resetMarkers = new HashSet<string>();
        if (Directory.Exists(slot.Dir))
        {
            foreach (var f in Directory.GetFiles(slot.Dir, "cache_reset_*.txt"))
            {
                var fname = Path.GetFileNameWithoutExtension(f);
                var parts = fname.Split('_');
                // format: cache_reset_NNN_HH-mm-ss-fff → parts[2] = NNN
                if (parts.Length >= 3)
                    resetMarkers.Add(parts[2]);
            }
        }

        foreach (var reqDir in requestDirs)
        {
            var reqName = Path.GetFileName(reqDir);
            var (seq, timestamp) = ParseRequestName(reqName);

            // header
            AppendLine($"─── Request {seq}  @  {timestamp} ────────────────────────────────────", FgTiming);

            // timing
            var timingFile = Path.Combine(reqDir, "timing.txt");
            if (File.Exists(timingFile))
            {
                var timingText = File.ReadAllText(timingFile);
                var ms = ParseDuration(timingText);
                AppendLine($"  ⏱  {ms}", FgTiming);
            }

            // context fill
            var ctxFile = Path.Combine(reqDir, "context_usage.txt");
            if (File.Exists(ctxFile))
            {
                var ctxText = File.ReadAllText(ctxFile);
                var (ptok, ctok, ctxSize, fillPct, isEstimate) = ParseContextUsage(ctxText);
                if (ctxSize > 0)
                {
                    var bar    = BuildContextBar(fillPct);
                    var prefix = isEstimate ? "~" : "";
                    AppendLine($"  ◈  {prefix}{ptok}+{ctok} / {ctxSize} tokens  {bar}  {fillPct:F1}%", FgContext);
                }
            }

            // GBNF note
            var gbnfFile = Path.Combine(reqDir, "gbnf_constraints.txt");
            if (File.Exists(gbnfFile))
                AppendLine("  [GBNF grammar constraints present]", FgGbnf);

            AppendLine("", FgDefault);

            // user message
            var userFile = Path.Combine(reqDir, "user_message.txt");
            if (File.Exists(userFile))
            {
                AppendLine("  USER:", FgUser, bold: true);
                var userText = File.ReadAllText(userFile).TrimEnd();
                AppendLine(IndentLines(userText, "    "), FgUser);
                AppendLine("", FgDefault);
            }

            // LLM response
            var responseFile = Path.Combine(reqDir, "llm_response.txt");
            if (File.Exists(responseFile))
            {
                var respText = File.ReadAllText(responseFile).TrimEnd();
                if (string.IsNullOrWhiteSpace(respText))
                {
                    AppendLine("  ASSISTANT: (empty — cancelled or failed)", FgEmpty, bold: true);
                }
                else
                {
                    AppendLine("  ASSISTANT:", FgAssistant, bold: true);
                    AppendLine(IndentLines(respText, "    "), FgAssistant);
                }
            }
            AppendLine("", FgDefault);

            // Cache-reset marker: show a separator if the instance was reset after this request
            if (resetMarkers.Contains(seq))
            {
                AppendLine("╔══════════════════════════════════════════════════════════╗", FgEmpty, bold: true);
                AppendLine("║          [ CACHE RESET — conversation cleared ]          ║", FgEmpty, bold: true);
                AppendLine("╚══════════════════════════════════════════════════════════╝", FgEmpty, bold: true);
                AppendLine("", FgDefault);
            }
        }

        _conversation.ResumeLayout();
        // Scroll to bottom to show latest exchange
        _conversation.SelectionStart = _conversation.TextLength;
        _conversation.ScrollToCaret();
    }

    // ─── RichTextBox helpers ─────────────────────────────────────────────────
    private void AppendLine(string text, Color color, bool bold = false)
    {
        int start = _conversation.TextLength;
        _conversation.AppendText(text + "\n");
        _conversation.Select(start, text.Length);
        _conversation.SelectionColor = color;
        if (bold)
        {
            _conversation.SelectionFont = new Font(_conversation.Font, FontStyle.Bold);
        }
        _conversation.SelectionStart = _conversation.TextLength;
    }

    // ─── owner-draw list ────────────────────────────────────────────────────
    private void SlotList_MeasureItem(object? sender, MeasureItemEventArgs e)
    {
        e.ItemHeight = 42;
    }

    private void SlotList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _slots.Count) return;
        var slot = _slots[e.Index];

        bool selected = (e.State & DrawItemState.Selected) != 0;
        var bgColor   = selected ? BgListSelect : BgPanel;
        var numColor  = selected ? FgListSelect : Color.FromArgb(160, 200, 240);
        var descColor = selected ? FgListNormal : Color.FromArgb(140, 140, 160);
        var cntColor  = selected ? FgTiming : Color.FromArgb(150, 150, 100);

        e.Graphics.FillRectangle(new SolidBrush(bgColor), e.Bounds);

        // left accent bar for selected
        if (selected)
            e.Graphics.FillRectangle(new SolidBrush(FgListSelect), new Rectangle(e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height));

        var fontBold   = new Font("Consolas", 9.5f, FontStyle.Bold);
        var fontNormal = new Font("Consolas", 8f);

        int x = e.Bounds.X + 10;
        int y = e.Bounds.Y + 4;

        // "Slot 3  Mycology" on the top line, request count on the right
        e.Graphics.DrawString($"Slot {slot.SlotId,-2}", fontBold, new SolidBrush(numColor), x, y);
        e.Graphics.DrawString(slot.SlotName, fontNormal, new SolidBrush(FgAssistant), x + 58, y + 2);
        e.Graphics.DrawString($"{slot.RequestCount} req", fontNormal, new SolidBrush(cntColor), x, y + 18);
    }

    // ─── parsing helpers ─────────────────────────────────────────────────────
    private static (string seq, string timestamp) ParseRequestName(string name)
    {
        // Format: request_001_14-23-55-123
        var parts = name.Split('_');
        var seq = parts.Length > 1 ? parts[1] : "?";
        var ts  = parts.Length > 2 ? parts[2] : "";
        return (seq, ts.Replace("-", ":"));
    }

    private static string ParseDuration(string timingText)
    {
        // e.g. "  LLM Duration:  425ms (0,42s)"
        var match = Regex.Match(timingText, @"LLM Duration:\s*(\d+)\s*ms");
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out int ms))
                return $"{ms:N0} ms  ({ms / 1000.0:F2} s)";
        }
        // Fallback: return first non-empty line
        return timingText.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() ?? "?";
    }

    private static string IndentLines(string text, string indent)
    {
        var lines = text.Split('\n');
        return string.Join("\n", lines.Select(l => indent + l));
    }

    private static (int ptok, int ctok, int ctxSize, double fillPct, bool isEstimate) ParseContextUsage(string text)
    {
        int Parse(string key)
        {
            var m = Regex.Match(text, $@"{key}:\s*(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }
        int ptok       = Parse("Prompt Tokens");
        int ctok       = Parse("Completion Tokens");
        int ctxSize    = Parse("Context Size");
        bool isEstimate = text.Contains("(estimated)");
        var fillM      = Regex.Match(text, @"Context Fill:\s*([\d.]+)%");
        double fill    = fillM.Success ? double.Parse(fillM.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
        return (ptok, ctok, ctxSize, fill, isEstimate);
    }

    private static string BuildContextBar(double fillPct)
    {
        const int w = 20;
        int filled = Math.Clamp((int)Math.Round(fillPct / 100.0 * w), 0, w);
        return "[" + new string('█', filled) + new string('░', w - filled) + "]";
    }
}
