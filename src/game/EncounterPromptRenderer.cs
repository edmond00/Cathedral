using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Renders the encounter prompt box shown when a travel encounter is rolled.
/// Same purple palette as <see cref="DeathScreenRenderer"/>; the player must
/// click <c>[ ENGAGE ]</c> to proceed to the fight.
/// </summary>
public sealed class EncounterPromptRenderer
{
    private readonly TerminalHUD _terminal;

    // Layout — same width as the death screen
    private const int BoxW = 44;
    private const int BoxH = 14;

    private int _boxX, _boxY;
    private int _btnX, _btnY;
    private const string BtnLabel = "[ ENGAGE ]";
    private const int BtnW = 10; // BtnLabel.Length

    private int _hoverX = -1;
    private int _hoverY = -1;

    // Colors — identical purple palette as DeathScreenRenderer
    private static readonly Vector4 BgColor      = new(0.06f, 0.0f, 0.10f, 1.0f);
    private static readonly Vector4 BorderColor  = Config.Colors.BrightPurple;
    private static readonly Vector4 TitleColor   = Config.Colors.BrightPurple;
    private static readonly Vector4 SubjectColor = Config.Colors.LightPurple;
    private static readonly Vector4 TextColor    = Config.Colors.LightPurpleGray;
    private static readonly Vector4 DividerColor = Config.Colors.Purple;
    private static readonly Vector4 BtnText      = Config.Colors.BrightPurple;
    private static readonly Vector4 BtnHover     = Config.Colors.White;
    private static readonly Vector4 BtnBgHover   = Config.Colors.DarkPurple;

    public EncounterPromptRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    public void SetHover(int cellX, int cellY) { _hoverX = cellX; _hoverY = cellY; }

    public bool IsOverEngageButton(int cellX, int cellY)
        => cellY == _btnY && cellX >= _btnX && cellX < _btnX + BtnW;

    /// <summary>
    /// The ENGAGE button's own cell, for <c>--cli</c> to click by name. Null until
    /// <see cref="Draw"/> has run, since the layout is resolved there.
    /// </summary>
    public (int X, int Y)? EngageButtonCell => _btnY > 0 ? (_btnX, _btnY) : null;

    /// <summary>
    /// Draws the encounter prompt. Call again on hover change to refresh button highlight.
    /// </summary>
    /// <summary>
    /// Draws the encounter prompt.
    /// <paramref name="npcName"/> is shown as the subject title (matches the fight screen).
    /// <paramref name="creatureType"/> selects the flavor text (wolf/bear/bandit/brigand/…).
    /// Call again on hover change to refresh button highlight.
    /// </summary>
    public void Draw(string npcName, string creatureType, string biomeName)
    {
        _terminal.Visible = true;

        _boxX = (_terminal.Width  - BoxW) / 2;
        _boxY = (_terminal.Height - BoxH) / 2;

        // Background fill
        for (int dy = 0; dy < BoxH; dy++)
            for (int dx = 0; dx < BoxW; dx++)
                _terminal.SetCell(_boxX + dx, _boxY + dy, ' ', BgColor, BgColor);

        DrawBox(_boxX, _boxY, BoxW, BoxH);

        int innerW = BoxW - 4;
        int row    = _boxY + 1;

        // Title
        DrawCentered(row, "── ENCOUNTER ──", TitleColor);
        row += 2;

        // NPC name — same name the player will see in the fight screen
        DrawCentered(row, npcName, SubjectColor);
        row += 2;

        // Divider
        DrawCentered(row, new string('─', innerW), DividerColor);
        row += 2;

        // Flavor text (up to two lines split on \n)
        var flavor = GetFlavor(creatureType).Split('\n');
        foreach (var line in flavor)
        {
            DrawCentered(row, line, TextColor);
            row++;
        }

        row++;

        // Biome context
        DrawCentered(row, $"in the {biomeName}", TextColor);

        // Engage button — 3 rows above bottom border
        _btnY = _boxY + BoxH - 3;
        _btnX = _boxX + (BoxW - BtnW) / 2;

        bool hover = IsOverEngageButton(_hoverX, _hoverY);
        _terminal.Text(_btnX, _btnY, BtnLabel,
            hover ? BtnHover  : BtnText,
            hover ? BtnBgHover : BgColor);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GetFlavor(string creatureType) => creatureType switch
    {
        "wolf"    => "Fangs bared and hackles raised,\nthe beast lunges from the undergrowth.",
        "bear"    => "With a thunderous roar the creature rears up,\nblocking every path forward.",
        "bandit"  => "An armed figure steps out of hiding,\nweapon drawn and eyes cold.",
        "brigand" => "A hooded figure drops from the branches,\nblade drawn and ready.",
        _         => "Something dangerous crosses your path,\nblocking the way forward.",
    };

    private static string Capitalize(string s)
        => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private void DrawCentered(int row, string text, Vector4 color)
    {
        int x = _boxX + (BoxW - text.Length) / 2;
        if (x < _boxX + 1) x = _boxX + 1;
        _terminal.Text(x, row, text, color, BgColor);
    }

    private void DrawBox(int x, int y, int w, int h)
    {
        _terminal.SetCell(x,         y,         '┌', BorderColor, BgColor);
        _terminal.SetCell(x + w - 1, y,         '┐', BorderColor, BgColor);
        _terminal.SetCell(x,         y + h - 1, '└', BorderColor, BgColor);
        _terminal.SetCell(x + w - 1, y + h - 1, '┘', BorderColor, BgColor);

        for (int dx = 1; dx < w - 1; dx++)
        {
            _terminal.SetCell(x + dx, y,         '─', BorderColor, BgColor);
            _terminal.SetCell(x + dx, y + h - 1, '─', BorderColor, BgColor);
        }
        for (int dy = 1; dy < h - 1; dy++)
        {
            _terminal.SetCell(x,         y + dy, '│', BorderColor, BgColor);
            _terminal.SetCell(x + w - 1, y + dy, '│', BorderColor, BgColor);
        }
    }
}
