using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game.Narrative;
using Cathedral.Game.Creation;

namespace Cathedral.Game.Management;

/// <summary>
/// Defines the available view tabs in the management menu.
/// </summary>
public enum ManagementTab
{
    Body,
    Inventory,
    Journal
}

/// <summary>
/// Renders and manages the protagonist/companion management menu.
/// Features a tabbed interface with character selection and view tabs.
///
/// Layout (right panel, cols 62-99):
///   Row 1:  Character selector (Protagonist / companions)
///   Row 2:  Separator
///   Row 3:  View tabs [Body] [Inventory] [Journal]
///   Row 4:  Separator
///   Row 5:  Tab title
///   Row 7+: Tab content (organ stats for Body, placeholders for others)
///   Row 92: Separator
///   Row 96: [ BACK ] button
///
/// Left side (cols 0-60): Body art display when Body tab is active.
/// </summary>
public class ManagementMenuRenderer
{
    private readonly TerminalHUD _terminal;
    private readonly Avatar _avatar;
    private readonly BodyArtViewer _bodyViewer;

    // ── Tab state ────────────────────────────────────────────────
    private ManagementTab _activeTab = ManagementTab.Body;
    private int _selectedCharacterIndex = 0; // 0 = protagonist

    // ── Character definitions ────────────────────────────────────
    private readonly List<CharacterEntry> _characters = new();

    // ── Tab definitions ──────────────────────────────────────────
    private static readonly TabDefinition[] AllTabs = new[]
    {
        new TabDefinition("Body",      ManagementTab.Body,      AllCharacters: true),
        new TabDefinition("Inventory", ManagementTab.Inventory,  AllCharacters: true),
        new TabDefinition("Journal",   ManagementTab.Journal,    AllCharacters: false), // protagonist only
    };

    // ── Hover state ──────────────────────────────────────────────
    private int _hoveredTabIndex = -1;
    private int _hoveredCharIndex = -1;
    private bool _backHovered;

    // ── Tab button positions (computed on render) ────────────────
    private readonly List<(int x, int width, ManagementTab tab)> _tabButtons = new();
    private readonly List<(int x, int width, int charIndex)> _charButtons = new();

    // ── Layout constants ─────────────────────────────────────────
    private const int CharTabRow = 1;
    private const int ViewTabRow = 3;
    private const int TabTitleRow = 5;
    private const int ContentStartRow = 7;
    private const int BackButtonY = 96;
    private const int BackButtonX = 72;
    private const int BackButtonW = 18;

    /// <summary>Callback for when the player clicks Back.</summary>
    public Action? OnBack { get; set; }

    public ManagementMenuRenderer(TerminalHUD terminal, Avatar avatar, BodyArtData artData)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _avatar = avatar ?? throw new ArgumentNullException(nameof(avatar));

        _bodyViewer = new BodyArtViewer(terminal, avatar, artData)
        {
            StatsStartRow = ContentStartRow,
            ShowScoreEditControls = false,
            ShowClickHints = false
        };
        _bodyViewer.ComputeLayout();

        // Initialize characters (only protagonist for now)
        _characters.Add(new CharacterEntry("Protagonist", IsProtagonist: true));
    }

    // ═══════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Full render of the management screen.</summary>
    public void Render()
    {
        _terminal.Fill(' ', Config.Colors.Black, Config.Colors.Black);
        _terminal.Visible = true;

        // Left side: body art (only on Body tab)
        if (_activeTab == ManagementTab.Body)
        {
            _bodyViewer.RenderBodyArt();
        }
        else
        {
            // Draw separator even on non-body tabs for consistency
            int sepX = BodyArtViewer.PanelX - 1;
            for (int y = 0; y < 100; y++)
                _terminal.SetCell(sepX, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);
        }

        // Right panel
        RenderCharacterTabs();
        RenderSeparator(2);
        RenderViewTabs();
        RenderSeparator(4);
        RenderTabTitle();

        // Tab content
        switch (_activeTab)
        {
            case ManagementTab.Body:
                int lastRow = _bodyViewer.RenderOrganStats();
                _bodyViewer.RenderHoveredDetail(lastRow);
                break;
            case ManagementTab.Inventory:
                RenderInventoryPlaceholder();
                break;
            case ManagementTab.Journal:
                RenderJournalPlaceholder();
                break;
        }

        RenderFooter();
    }

    /// <summary>Called every frame for animations.</summary>
    public void Update()
    {
        if (_activeTab == ManagementTab.Body && _bodyViewer.UpdateBlink())
            _bodyViewer.RenderBodyArt();
    }

    /// <summary>Handle mouse hover at terminal coordinates.</summary>
    public void OnMouseMove(int x, int y)
    {
        bool changed = false;

        // Check character tabs
        int newCharHover = GetCharacterTabAt(x, y);
        if (newCharHover != _hoveredCharIndex) { _hoveredCharIndex = newCharHover; changed = true; }

        // Check view tabs
        int newTabHover = GetViewTabAt(x, y);
        if (newTabHover != _hoveredTabIndex) { _hoveredTabIndex = newTabHover; changed = true; }

        // Check back button
        bool newBackHovered = IsOnBackButton(x, y);
        if (newBackHovered != _backHovered) { _backHovered = newBackHovered; changed = true; }

        // Delegate to body viewer on Body tab
        if (_activeTab == ManagementTab.Body)
        {
            if (_bodyViewer.ProcessHover(x, y))
                changed = true;
        }

        if (changed)
            Render();
    }

    /// <summary>Handle left click at terminal coordinates.</summary>
    public void OnMouseClick(int x, int y)
    {
        // Back button
        if (IsOnBackButton(x, y))
        {
            OnBack?.Invoke();
            return;
        }

        // Character tab click
        int charIdx = GetCharacterTabAt(x, y);
        if (charIdx >= 0 && charIdx < _characters.Count)
        {
            if (IsCharacterVisibleForTab(charIdx, _activeTab))
            {
                _selectedCharacterIndex = charIdx;
                Render();
                return;
            }
        }

        // View tab click
        int tabIdx = GetViewTabAt(x, y);
        if (tabIdx >= 0 && tabIdx < AllTabs.Length)
        {
            var tabDef = AllTabs[tabIdx];
            if (tabDef.Enabled)
            {
                _activeTab = tabDef.Tab;

                // If switching to a tab that doesn't support the current character, reset to protagonist
                if (!IsCharacterVisibleForTab(_selectedCharacterIndex, _activeTab))
                    _selectedCharacterIndex = 0;

                // Clear body viewer hover when leaving body tab
                if (_activeTab != ManagementTab.Body)
                    _bodyViewer.ClearHover();

                Render();
                return;
            }
        }

        // Body tab: no score modification (read-only), but clicks on art still work for hover feedback
    }

    /// <summary>Handle right click (no special behavior in management mode).</summary>
    public void OnRightClick(int x, int y)
    {
        // Read-only mode — no score modification
    }

    // ═══════════════════════════════════════════════════════════════
    // Character tabs
    // ═══════════════════════════════════════════════════════════════

    private void RenderCharacterTabs()
    {
        _charButtons.Clear();
        int cx = BodyArtViewer.PanelContentX;

        // Filter characters visible for the current tab
        for (int i = 0; i < _characters.Count; i++)
        {
            if (!IsCharacterVisibleForTab(i, _activeTab)) continue;

            var ch = _characters[i];
            bool isSelected = i == _selectedCharacterIndex;
            bool isHovered = i == _hoveredCharIndex;

            string label = isSelected ? $"◆ {ch.Name}" : $"  {ch.Name}";

            Vector4 textColor, bgColor;
            if (isSelected)
            {
                textColor = Config.Colors.BrightYellow;
                bgColor = Config.Colors.Black;
            }
            else if (isHovered)
            {
                textColor = Config.Colors.MediumYellow;
                bgColor = new Vector4(0.06f, 0.06f, 0.0f, 1.0f);
            }
            else
            {
                textColor = Config.Colors.MediumGray60;
                bgColor = Config.Colors.Black;
            }

            _terminal.Text(cx, CharTabRow, label, textColor, bgColor);
            _charButtons.Add((cx, label.Length, i));
            cx += label.Length + 2;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // View tabs
    // ═══════════════════════════════════════════════════════════════

    private void RenderViewTabs()
    {
        _tabButtons.Clear();
        int cx = BodyArtViewer.PanelContentX;

        for (int i = 0; i < AllTabs.Length; i++)
        {
            var tab = AllTabs[i];
            bool isActive = tab.Tab == _activeTab;
            bool isHovered = i == _hoveredTabIndex;

            string label = $"[{tab.Label}]";

            Vector4 textColor, bgColor;
            if (!tab.Enabled)
            {
                textColor = Config.Colors.DarkGray35;
                bgColor = Config.Colors.Black;
            }
            else if (isActive)
            {
                textColor = Config.Colors.BrightYellow;
                bgColor = new Vector4(0.1f, 0.1f, 0.0f, 1.0f);
            }
            else if (isHovered)
            {
                textColor = Config.Colors.MediumYellow;
                bgColor = new Vector4(0.05f, 0.05f, 0.0f, 1.0f);
            }
            else
            {
                textColor = Config.Colors.MediumGray60;
                bgColor = Config.Colors.Black;
            }

            _terminal.Text(cx, ViewTabRow, label, textColor, bgColor);
            _tabButtons.Add((cx, label.Length, tab.Tab));
            cx += label.Length + 1;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Tab content
    // ═══════════════════════════════════════════════════════════════

    private void RenderTabTitle()
    {
        string title = _activeTab switch
        {
            ManagementTab.Body      => "B O D Y  /  O R G A N S",
            ManagementTab.Inventory => "I N V E N T O R Y",
            ManagementTab.Journal   => "J O U R N A L",
            _                       => ""
        };

        _terminal.Text(BodyArtViewer.PanelContentX, TabTitleRow, title, Config.Colors.DarkYellowGrey, Config.Colors.Black);
    }

    private void RenderInventoryPlaceholder()
    {
        int row = ContentStartRow;
        _terminal.Text(BodyArtViewer.PanelContentX, row, "No items yet.", Config.Colors.DarkGray35, Config.Colors.Black);
        row += 2;
        _terminal.Text(BodyArtViewer.PanelContentX, row, "Items collected during your", Config.Colors.DarkGray35, Config.Colors.Black);
        row++;
        _terminal.Text(BodyArtViewer.PanelContentX, row, "journey will appear here.", Config.Colors.DarkGray35, Config.Colors.Black);
    }

    private void RenderJournalPlaceholder()
    {
        int row = ContentStartRow;
        _terminal.Text(BodyArtViewer.PanelContentX, row, "No journal entries yet.", Config.Colors.DarkGray35, Config.Colors.Black);
        row += 2;
        _terminal.Text(BodyArtViewer.PanelContentX, row, "Your experiences and", Config.Colors.DarkGray35, Config.Colors.Black);
        row++;
        _terminal.Text(BodyArtViewer.PanelContentX, row, "discoveries will be recorded", Config.Colors.DarkGray35, Config.Colors.Black);
        row++;
        _terminal.Text(BodyArtViewer.PanelContentX, row, "here.", Config.Colors.DarkGray35, Config.Colors.Black);
    }

    // ═══════════════════════════════════════════════════════════════
    // Footer
    // ═══════════════════════════════════════════════════════════════

    private void RenderFooter()
    {
        _terminal.Text(BodyArtViewer.PanelContentX, 92, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);

        if (_activeTab == ManagementTab.Body)
        {
            int totalScore = _bodyViewer.GetTotalScore();
            string pointsText = $"Total Points: {totalScore}";
            _terminal.Text(BodyArtViewer.PanelContentX, 94, pointsText, Config.Colors.LightGray75, Config.Colors.Black);
        }

        // Back button
        Vector4 btnText, btnBg;
        if (_backHovered)
        {
            btnText = Config.Colors.BrightYellow;
            btnBg = Config.Colors.DarkYellow;
        }
        else
        {
            btnText = Config.Colors.White;
            btnBg = Config.Colors.Black;
        }

        _terminal.FillRect(BackButtonX, BackButtonY, BackButtonW, 1, ' ', btnText, btnBg);
        string btnLabel = "[ BACK ]";
        int lblX = BackButtonX + (BackButtonW - btnLabel.Length) / 2;
        _terminal.Text(lblX, BackButtonY, btnLabel, btnText, btnBg);
    }

    // ═══════════════════════════════════════════════════════════════
    // Separator helper
    // ═══════════════════════════════════════════════════════════════

    private void RenderSeparator(int row)
    {
        _terminal.Text(BodyArtViewer.PanelContentX, row, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
    }

    // ═══════════════════════════════════════════════════════════════
    // Hit testing
    // ═══════════════════════════════════════════════════════════════

    private int GetCharacterTabAt(int x, int y)
    {
        if (y != CharTabRow) return -1;
        foreach (var (bx, bw, idx) in _charButtons)
        {
            if (x >= bx && x < bx + bw) return idx;
        }
        return -1;
    }

    private int GetViewTabAt(int x, int y)
    {
        if (y != ViewTabRow) return -1;
        for (int i = 0; i < _tabButtons.Count; i++)
        {
            var (bx, bw, _) = _tabButtons[i];
            if (x >= bx && x < bx + bw) return i;
        }
        return -1;
    }

    private bool IsOnBackButton(int x, int y)
    {
        return y == BackButtonY && x >= BackButtonX && x < BackButtonX + BackButtonW;
    }

    /// <summary>
    /// Determines whether a character is visible for the given tab.
    /// Journal tab is protagonist-only; other tabs show all characters.
    /// </summary>
    private bool IsCharacterVisibleForTab(int characterIndex, ManagementTab tab)
    {
        if (characterIndex < 0 || characterIndex >= _characters.Count) return false;
        var ch = _characters[characterIndex];

        // Find the tab definition
        foreach (var tabDef in AllTabs)
        {
            if (tabDef.Tab == tab)
                return tabDef.AllCharacters || ch.IsProtagonist;
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════
    // Data types
    // ═══════════════════════════════════════════════════════════════

    private record struct CharacterEntry(string Name, bool IsProtagonist);

    private record struct TabDefinition(string Label, ManagementTab Tab, bool AllCharacters)
    {
        /// <summary>Whether this tab is currently implemented (false = placeholder, still clickable).</summary>
        public bool Enabled => true; // All tabs are selectable; content may be placeholder
    }
}
