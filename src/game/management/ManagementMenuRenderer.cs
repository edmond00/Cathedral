using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Creation;

namespace Cathedral.Game.Management;

/// <summary>
/// Defines the available view tabs in the management menu.
/// </summary>
public enum ManagementTab
{
    Body,
    Inventory,
    Journal,
    Memory,
    Humors
}

/// <summary>
/// Renders and manages the protagonist/companion management menu.
/// Features a full-height left panel for navigation, body art in the center, and stats on the right.
///
/// Layout:
///   Cols  0-15 : Left panel (full height, subtle background)
///   Col  16    : Vertical separator
///   Cols 17-56 : Body art (40 wide, shifted right by 17)
///   Col  61    : Vertical separator
///   Cols 62-99 : Right panel (title + organ stats + footer)
/// </summary>
public class ManagementMenuRenderer
{
    private readonly TerminalHUD _terminal;
    private readonly Protagonist _protagonist;
    private readonly BodyArtViewer _bodyViewer;
    private readonly MemoryPanelRenderer _memoryPanel;
    private readonly HumorMenuRenderer _humorMenu;
    private readonly InventoryMenuRenderer _inventoryMenu;

    // ── Tab state ────────────────────────────────────────────────
    private ManagementTab _activeTab = ManagementTab.Body;
    private int _selectedCharacterIndex = 0; // 0 = protagonist, 1+ = companions

    // ── Tab definitions ──────────────────────────────────────────
    private static readonly TabDefinition[] AllTabs = new[]
    {
        new TabDefinition("Body",      ManagementTab.Body,      AllCharacters: true),
        new TabDefinition("Inventory", ManagementTab.Inventory,  AllCharacters: true),
        new TabDefinition("Journal",   ManagementTab.Journal,    AllCharacters: false), // protagonist only
        new TabDefinition("Memory",    ManagementTab.Memory,    AllCharacters: true),
        new TabDefinition("Humors",    ManagementTab.Humors,    AllCharacters: true),
    };

    // ── Hover state ──────────────────────────────────────────────
    private int _hoveredTabIndex = -1;
    private int _hoveredCharIndex = -1;
    private bool _backHovered;

    // ── Left panel layout (full height, no boxes) ────────────────
    private const int PanelLeft = 0;
    private const int PanelLeftW = 16;        // cols 0-15
    private const int SepCol = 16;            // vertical separator column
    private const int ContentX = 2;           // text content starts at col 2
    private const int ContentW = 13;          // usable text width inside panel

    // Subtle panel background
    private static readonly Vector4 PanelBg = new(0.04f, 0.04f, 0.04f, 1.0f);

    // Menu section (top of panel)
    private const int MenuTitleRow = 2;
    private const int MenuDividerRow = 3;
    private const int MenuFirstItemRow = 5;
    // Items at rows 5, 6, 7 (one per tab)

    // Party section (bottom of panel, always visible)
    private const int PartyTitleRow = 86;
    private const int PartyDividerRow = 87;
    private const int PartyFirstItemRow = 89;
    // Items start at row 89

    // Back button (very bottom)
    private const int BackRow = 96;

    // ── Right panel layout ───────────────────────────────────────
    private const int RightTitleRow = 1;
    private const int RightSepRow = 3;
    private const int ContentStartRow = 4;
    private const int FooterSepRow = 92;

    // ── Hit-test regions (computed on render) ────────────────────
    private readonly List<(int row, int tabIndex)> _tabHitRows = new();
    private readonly List<(int row, int charIndex)> _charHitRows = new();

    /// <summary>Callback for when the player clicks Back.</summary>
    public Action? OnBack { get; set; }

    public ManagementMenuRenderer(TerminalHUD terminal, Protagonist protagonist, BodyArtData artData,
                                   PopupTerminalHUD? popup = null)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _protagonist = protagonist ?? throw new ArgumentNullException(nameof(protagonist));

        _bodyViewer = new BodyArtViewer(terminal, protagonist, artData)
        {
            ArtOffsetX = 17,  // Shift art right to make room for left panel
            StatsStartRow = ContentStartRow,
            ShowScoreEditControls = false,
            ShowClickHints = false
        };
        _bodyViewer.ComputeLayout();

        _memoryPanel = new MemoryPanelRenderer(terminal, popup);

        var gearData = GearAnchorData.Load("assets/art/body/human");
        _inventoryMenu = new InventoryMenuRenderer(terminal, _bodyViewer, gearData, popup);

        var humorArtData = HumorArtData.Load("assets/art/humors");
        var heparMap     = HumorQueuePositionMap.Load("assets/art/humors/hepar.txt",    "hepar");
        var paunchMap    = HumorQueuePositionMap.Load("assets/art/humors/paunch.txt",   "paunch");
        var pulmonesMap  = HumorQueuePositionMap.Load("assets/art/humors/pulmones.txt", "pulmones");
        var spleenMap    = HumorQueuePositionMap.Load("assets/art/humors/spleen.txt",   "spleen");
        _humorMenu = new HumorMenuRenderer(terminal, humorArtData, heparMap, paunchMap, pulmonesMap, spleenMap);
    }

    // ── Party helpers ────────────────────────────────────────────

    /// <summary>Total party size: protagonist + companions.</summary>
    private int PartyCount => 1 + _protagonist.CompanionParty.Count;

    /// <summary>Display name for a slot index (0 = protagonist, 1+ = companions).</summary>
    private string GetCharacterName(int index) =>
        index == 0 ? _protagonist.DisplayName : _protagonist.CompanionParty[index - 1].Name;

    /// <summary>Whether slot index belongs to the protagonist.</summary>
    private bool IsProtagonistSlot(int index) => index == 0;

    /// <summary>Get the <see cref="PartyMember"/> for a slot index.</summary>
    private PartyMember GetPartyMember(int index) =>
        index == 0 ? _protagonist : (PartyMember)_protagonist.CompanionParty[index - 1];

    // ═══════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Full render of the management screen.</summary>
    public void Render()
    {
        _terminal.Fill(' ', Config.Colors.Black, Config.Colors.Black);
        _terminal.Visible = true;

        // Body art (only on Body tab)
        if (_activeTab == ManagementTab.Body)
        {
            _bodyViewer.ShowWounds = true;
            _bodyViewer.RenderBodyArt();
        }
        else if (_activeTab != ManagementTab.Memory && _activeTab != ManagementTab.Humors)
        {
            _bodyViewer.ShowWounds = false;
            // Draw separator on non-body, non-memory, non-humors tabs
            int sepX = BodyArtViewer.PanelX - 1;
            for (int y = 0; y < 100; y++)
                _terminal.SetCell(sepX, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);
        }

        // Right panel
        if (_activeTab != ManagementTab.Memory && _activeTab != ManagementTab.Humors)
            RenderPanelHeader();

        switch (_activeTab)
        {
            case ManagementTab.Body:
                int lastRow = _bodyViewer.RenderOrganStats();
                _bodyViewer.RenderHoveredDetail(lastRow);
                break;
            case ManagementTab.Inventory:
                _inventoryMenu.Render(GetPartyMember(_selectedCharacterIndex));
                break;
            case ManagementTab.Journal:
                RenderJournalPlaceholder();
                break;
            case ManagementTab.Memory:
                _memoryPanel.Render(GetPartyMember(_selectedCharacterIndex));
                break;
            case ManagementTab.Humors:
                _humorMenu.Render(GetPartyMember(_selectedCharacterIndex));
                break;
        }

        // Left panel (rendered AFTER all tab content so it overlays cleanly)
        RenderLeftPanel();

        if (_activeTab != ManagementTab.Memory && _activeTab != ManagementTab.Humors)
            RenderFooter();
    }

    /// <summary>Called every frame for animations.</summary>
    public void Update()
    {
        if (_activeTab == ManagementTab.Body && _bodyViewer.UpdateBlink())
        {
            _bodyViewer.ShowWounds = true;
            _bodyViewer.RenderBodyArt();
            int lastRow = _bodyViewer.RenderOrganStats();
            _bodyViewer.RenderHoveredDetail(lastRow);
            // Re-render left panel on top after art redraw
            RenderLeftPanel();
        }

        if (_activeTab == ManagementTab.Inventory && _inventoryMenu.Update())
            Render();
    }

    /// <summary>Handle mouse hover at terminal coordinates.</summary>
    public void OnMouseMove(int x, int y)
    {
        bool changed = false;

        // Check view tab rows in sidebar
        int newTabHover = GetTabAtPosition(x, y);
        if (newTabHover != _hoveredTabIndex) { _hoveredTabIndex = newTabHover; changed = true; }

        // Check character rows in sidebar
        int newCharHover = GetCharacterAtPosition(x, y);
        if (newCharHover != _hoveredCharIndex) { _hoveredCharIndex = newCharHover; changed = true; }

        // Check back button
        bool newBackHovered = IsOnBackButton(x, y);
        if (newBackHovered != _backHovered) { _backHovered = newBackHovered; changed = true; }

        // Delegate to body viewer on Body tab, memory panel on Memory tab, humor menu on Humors tab
        if (_activeTab == ManagementTab.Body)
        {
            if (_bodyViewer.ProcessHover(x, y))
                changed = true;
        }
        else if (_activeTab == ManagementTab.Memory)
        {
            if (_memoryPanel.ProcessHover(x, y))
                changed = true;
        }
        else if (_activeTab == ManagementTab.Humors)
        {
            if (_humorMenu.ProcessHover(x, y))
                changed = true;
        }
        else if (_activeTab == ManagementTab.Inventory)
        {
            if (_inventoryMenu.ProcessHover(x, y))
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

        // View tab click
        int tabIdx = GetTabAtPosition(x, y);
        if (tabIdx >= 0 && tabIdx < AllTabs.Length)
        {
            var tabDef = AllTabs[tabIdx];
            if (tabDef.Enabled)
            {
                _activeTab = tabDef.Tab;

                if (!IsCharacterVisibleForTab(_selectedCharacterIndex, _activeTab))
                    _selectedCharacterIndex = 0;

                if (_activeTab != ManagementTab.Body)
                    _bodyViewer.ClearHover();
                if (_activeTab != ManagementTab.Memory)
                    _memoryPanel.ClearHover();
                if (_activeTab != ManagementTab.Humors)
                    _humorMenu.ClearHover();
                if (_activeTab != ManagementTab.Inventory)
                    _inventoryMenu.ClearHover();

                Render();
                return;
            }
        }

        // Character tab click
        int charIdx = GetCharacterAtPosition(x, y);
        if (charIdx >= 0 && charIdx < PartyCount)
        {
            if (IsCharacterVisibleForTab(charIdx, _activeTab))
            {
                _selectedCharacterIndex = charIdx;
                // Swap the body art subject when switching party member on Body tab
                if (_activeTab == ManagementTab.Body)
                    _bodyViewer.SwapSubject(GetPartyMember(charIdx));
                // Memory panel re-renders automatically via Render() with GetPartyMember(_selectedCharacterIndex)
                Render();
                return;
            }
        }

        // Memory panel interactive clicks (slot selection + buttons)
        if (_activeTab == ManagementTab.Memory)
        {
            if (_memoryPanel.ProcessClick(x, y))
                Render();
        }

        // Inventory tab clicks
        if (_activeTab == ManagementTab.Inventory)
        {
            if (_inventoryMenu.ProcessClick(x, y))
                Render();
        }
    }

    /// <summary>Handle right click (no special behavior in management mode).</summary>
    public void OnRightClick(int x, int y)
    {
        // Read-only mode — no score modification
    }

    /// <summary>Handle mouse-up (completes drag operations in the Inventory tab).</summary>
    public void OnMouseUp(int x, int y)
    {
        if (_activeTab == ManagementTab.Inventory)
        {
            if (_inventoryMenu.ProcessMouseUp(x, y))
                Render();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Left panel: full-height vertical pane
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders the entire left panel: background fill, separator line,
    /// menu section at top, party section near bottom, back button at bottom.
    /// </summary>
    private void RenderLeftPanel()
    {
        _tabHitRows.Clear();
        _charHitRows.Clear();

        // ─── Background fill ─────────────────────────────────────
        for (int y = 0; y < 100; y++)
            for (int x = PanelLeft; x < PanelLeft + PanelLeftW; x++)
                _terminal.SetCell(x, y, ' ', Config.Colors.Black, PanelBg);

        // ─── Vertical separator line ─────────────────────────────
        for (int y = 0; y < 100; y++)
            _terminal.SetCell(SepCol, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);

        // ─── Menu section (top) ──────────────────────────────────
        RenderMenuSection();

        // ─── Party section (bottom, always visible) ──────────────
        RenderPartySection();

        // ─── Back button (very bottom) ───────────────────────────
        RenderBackButton();
    }

    private void RenderMenuSection()
    {
        // Section title
        string title = "M E N U";
        int titleX = ContentX + (ContentW - title.Length) / 2;
        _terminal.Text(titleX, MenuTitleRow, title, Config.Colors.DarkYellowGrey, PanelBg);

        // Thin divider
        for (int tx = ContentX; tx < ContentX + ContentW; tx++)
            _terminal.SetCell(tx, MenuDividerRow, '─', Config.Colors.DarkGray35, PanelBg);

        // Tab items
        for (int i = 0; i < AllTabs.Length; i++)
        {
            int row = MenuFirstItemRow + i;
            var tab = AllTabs[i];
            bool isActive = tab.Tab == _activeTab;
            bool isHovered = i == _hoveredTabIndex;

            Vector4 textColor;
            Vector4 bgColor;
            if (!tab.Enabled)
            {
                textColor = Config.Colors.DarkGray35;
                bgColor = PanelBg;
            }
            else if (isActive)
            {
                textColor = Config.Colors.BrightYellow;
                bgColor = new Vector4(0.10f, 0.09f, 0.02f, 1.0f);
            }
            else if (isHovered)
            {
                textColor = Config.Colors.MediumYellow;
                bgColor = new Vector4(0.07f, 0.06f, 0.01f, 1.0f);
            }
            else
            {
                textColor = Config.Colors.MediumGray60;
                bgColor = PanelBg;
            }

            // Fill row background across inner panel
            for (int tx = PanelLeft; tx < PanelLeft + PanelLeftW; tx++)
                _terminal.SetCell(tx, row, ' ', textColor, bgColor);

            string marker = isActive ? "▸ " : "  ";
            string label = marker + tab.Label;
            _terminal.Text(ContentX, row, label, textColor, bgColor);

            _tabHitRows.Add((row, i));
        }
    }

    private void RenderPartySection()
    {
        // Section title
        string title = "P A R T Y";
        int titleX = ContentX + (ContentW - title.Length) / 2;
        _terminal.Text(titleX, PartyTitleRow, title, Config.Colors.DarkYellowGrey, PanelBg);

        // Thin divider
        for (int tx = ContentX; tx < ContentX + ContentW; tx++)
            _terminal.SetCell(tx, PartyDividerRow, '─', Config.Colors.DarkGray35, PanelBg);

        // Build the visible character list for this tab
        var visibleIndices = new List<int>();
        for (int i = 0; i < PartyCount; i++)
            if (IsCharacterVisibleForTab(i, _activeTab))
                visibleIndices.Add(i);

        // Character items
        for (int vi = 0; vi < visibleIndices.Count; vi++)
        {
            int charIdx = visibleIndices[vi];
            int row = PartyFirstItemRow + vi;
            bool isSelected = charIdx == _selectedCharacterIndex;
            bool isHovered = charIdx == _hoveredCharIndex;

            Vector4 textColor;
            Vector4 bgColor;
            if (isSelected)
            {
                textColor = Config.Colors.BrightYellow;
                bgColor = new Vector4(0.10f, 0.09f, 0.02f, 1.0f);
            }
            else if (isHovered)
            {
                textColor = Config.Colors.MediumYellow;
                bgColor = new Vector4(0.07f, 0.06f, 0.01f, 1.0f);
            }
            else
            {
                textColor = Config.Colors.MediumGray60;
                bgColor = PanelBg;
            }

            // Fill row background
            for (int tx = PanelLeft; tx < PanelLeft + PanelLeftW; tx++)
                _terminal.SetCell(tx, row, ' ', textColor, bgColor);

            string marker = isSelected ? "◆ " : "  ";
            string label = marker + GetCharacterName(charIdx);
            if (label.Length > ContentW)
                label = label[..ContentW];
            _terminal.Text(ContentX, row, label, textColor, bgColor);

            _charHitRows.Add((row, charIdx));
        }
    }

    private void RenderBackButton()
    {
        Vector4 textColor = _backHovered ? Config.Colors.BrightYellow : Config.Colors.MediumGray60;
        Vector4 bgColor = _backHovered ? new Vector4(0.10f, 0.09f, 0.02f, 1.0f) : PanelBg;

        // Fill row
        for (int tx = PanelLeft; tx < PanelLeft + PanelLeftW; tx++)
            _terminal.SetCell(tx, BackRow, ' ', textColor, bgColor);

        string label = "← BACK";
        int labelX = ContentX + (ContentW - label.Length) / 2;
        _terminal.Text(labelX, BackRow, label, textColor, bgColor);
    }

    // ═══════════════════════════════════════════════════════════════
    // Right panel
    // ═══════════════════════════════════════════════════════════════

    private void RenderPanelHeader()
    {
        string title = _activeTab switch
        {
            ManagementTab.Body      => "B O D Y  /  O R G A N S",
            ManagementTab.Inventory => "I N V E N T O R Y",
            ManagementTab.Journal   => "J O U R N A L",
            ManagementTab.Memory    => "",  // memory panel renders its own full-width title
            ManagementTab.Humors    => "",  // humor menu renders its own full-width title
            _                       => ""
        };

        _terminal.Text(BodyArtViewer.PanelContentX, RightTitleRow, title, Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.Text(BodyArtViewer.PanelContentX, RightSepRow, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
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
    // Footer (right panel)
    // ═══════════════════════════════════════════════════════════════

    private void RenderFooter()
    {
        _terminal.Text(BodyArtViewer.PanelContentX, FooterSepRow, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);

        if (_activeTab == ManagementTab.Body)
        {
            int totalScore = _bodyViewer.GetTotalScore();
            string pointsText = $"Total Points: {totalScore}";
            _terminal.Text(BodyArtViewer.PanelContentX, FooterSepRow + 2, pointsText, Config.Colors.LightGray75, Config.Colors.Black);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Hit testing
    // ═══════════════════════════════════════════════════════════════

    private int GetTabAtPosition(int x, int y)
    {
        if (x < PanelLeft || x >= PanelLeft + PanelLeftW) return -1;
        foreach (var (row, idx) in _tabHitRows)
        {
            if (y == row) return idx;
        }
        return -1;
    }

    private int GetCharacterAtPosition(int x, int y)
    {
        if (x < PanelLeft || x >= PanelLeft + PanelLeftW) return -1;
        foreach (var (row, idx) in _charHitRows)
        {
            if (y == row) return idx;
        }
        return -1;
    }

    private bool IsOnBackButton(int x, int y)
    {
        return y == BackRow && x >= PanelLeft && x < PanelLeft + PanelLeftW;
    }

    /// <summary>
    /// Returns true when a character slot is visible for the given tab.
    /// Journal is protagonist-only; all other tabs show the full party.
    /// </summary>
    private bool IsCharacterVisibleForTab(int characterIndex, ManagementTab tab)
    {
        if (characterIndex < 0 || characterIndex >= PartyCount) return false;
        foreach (var tabDef in AllTabs)
            if (tabDef.Tab == tab)
                return tabDef.AllCharacters || IsProtagonistSlot(characterIndex);
        return false;
    }

    // ═══════════════════════════════════════════════════════════════
    // Data types
    // ═══════════════════════════════════════════════════════════════

    private record struct TabDefinition(string Label, ManagementTab Tab, bool AllCharacters)
    {
        public bool Enabled => true;
    }
}
