using System;
using OpenTK.Mathematics;
using Cathedral.Audio;
using Cathedral.Game;
using Cathedral.Game.Creation;
using Cathedral.Game.Narrative;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Fight;

/// <summary>
/// Minimalist localization picker shown when a fighting skill lets the player choose where
/// to strike. Draws the target's body ASCII art (the same art used by the avatar creation /
/// body screens) inside a bordered box over the fight UI — but nothing else: no stats panel,
/// HP, organ levels, connector arrows or region highlight boxes.
///
/// The only feedback is a single 'X' cursor on the hovered tile when it maps to an organ or
/// body part; invalid (background) tiles show nothing. A mouse click on any valid tile returns
/// the localization as "organ_part_id,body_part_id" (or a bare body-part id when the tile has
/// no specific organ).
/// </summary>
public class FightLocalizationOverlay
{
    private static BodyArtData? _humanArtCache;
    private static BodyArtData? _beastArtCache;

    private readonly TerminalHUD _terminal;
    private readonly Fighter _target;
    private readonly BodyArtData _art;
    private readonly Action<string> _onSelected;
    private readonly Action? _onCancel;
    private readonly Action<GameEventType>? _sfx;
    private readonly string _skillName;

    // Body-cell bounding box in art coordinates (crops away background padding).
    private readonly int _artMinX, _artMinY, _artW, _artH;

    // Box geometry in terminal coordinates.
    private readonly int _boxX, _boxY, _boxW, _boxH;
    private readonly int _artOriginX, _artOriginY; // terminal position of art cell (_artMinX,_artMinY)

    // Cancel button (single row near the box bottom border).
    private readonly int _cancelX, _cancelY, _cancelW;

    // Hovered valid tile in terminal coordinates (-1 = none).
    private int _hoverTermX = -1, _hoverTermY = -1;
    private bool _cancelHovered;

    private static readonly Vector4 BoxBorder = Config.Colors.Orange;
    private static readonly Vector4 XColor    = Config.Colors.BrightYellow;
    private static readonly Vector4 XBg       = new(0.2f, 0.2f, 0.0f, 1.0f);

    public FightLocalizationOverlay(
        TerminalHUD terminal,
        Fighter target,
        string skillName,
        Action<string> onSelected,
        Action? onCancel = null,
        Action<GameEventType>? sfx = null)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _onSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
        _onCancel = onCancel;
        _sfx = sfx;
        _skillName = skillName ?? string.Empty;

        _art = GetBodyArtDataFor(target.Member);

        // Crop to the body-cell bounding box so the box hugs the figure.
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (int y = 0; y < _art.Height; y++)
            for (int x = 0; x < _art.Width; x++)
                if (_art.IsBodyCell(x, y))
                {
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
        if (maxX < 0) { minX = minY = 0; maxX = _art.Width - 1; maxY = _art.Height - 1; }

        _artMinX = minX;
        _artMinY = minY;
        _artW = maxX - minX + 1;
        _artH = maxY - minY + 1;

        // Interior: 1 blank pad + title row + art + instruction row + 1 blank pad.
        const int pad = 1;
        int interiorW = Math.Max(_artW, 18) + pad * 2;
        int interiorH = _artH + 2 /*title+instruction*/ + pad * 2;

        _boxW = Math.Min(interiorW + 2, _terminal.Width);
        _boxH = Math.Min(interiorH + 2, _terminal.Height);
        _boxX = Math.Max(0, (_terminal.Width  - _boxW) / 2);
        _boxY = Math.Max(0, (_terminal.Height - _boxH) / 2);

        // Art is centred horizontally inside the box, below the title row.
        _artOriginX = _boxX + 1 + pad + Math.Max(0, (_boxW - 2 - pad * 2 - _artW) / 2);
        _artOriginY = _boxY + 1 + pad + 1 /*title*/;

        _cancelY = _boxY + _boxH - 2;
        _cancelW = 12;
        _cancelX = _boxX + (_boxW - _cancelW) / 2;
    }

    private static BodyArtData GetBodyArtDataFor(PartyMember member)
    {
        if (member.AnatomyType == AnatomyType.Human)
        {
            _humanArtCache ??= BodyArtData.Load(
                member.Species.ArtFolderPath,
                AnatomyFactoryRegistry.GetFactory(AnatomyType.Human).GetZoneToBodyPartMapping());
            return _humanArtCache;
        }

        _beastArtCache ??= BodyArtData.Load(
            member.Species.ArtFolderPath,
            AnatomyFactoryRegistry.GetFactory(AnatomyType.Beast).GetZoneToBodyPartMapping());
        return _beastArtCache;
    }

    /// <summary>Full redraw of the overlay box on top of the (already-drawn) fight UI.</summary>
    public void Render()
    {
        _terminal.Visible = true;

        // Box background + border.
        _terminal.FillRect(_boxX, _boxY, _boxW, _boxH, ' ', Config.Colors.White, Config.Colors.Black);
        _terminal.DrawBox(_boxX, _boxY, _boxW, _boxH, BoxStyle.Double, BoxBorder, Config.Colors.Black);

        // Title row.
        string title = $"AIM — {_skillName}";
        if (title.Length > _boxW - 2) title = title[..(_boxW - 2)];
        _terminal.Text(_boxX + (_boxW - title.Length) / 2, _boxY + 1, title, BoxBorder, Config.Colors.Black);

        // Body art (cropped bounding box only; background cells stay blank).
        for (int ay = 0; ay < _artH; ay++)
        {
            for (int ax = 0; ax < _artW; ax++)
            {
                int sx = _artMinX + ax;
                int sy = _artMinY + ay;
                if (!_art.IsBodyCell(sx, sy)) continue;

                char glyph = _art.ArtGrid[sx, sy];
                if (glyph == ' ' || glyph == '\0') continue;

                _terminal.SetCell(_artOriginX + ax, _artOriginY + ay,
                    glyph, _art.GetLayerColorAt(sx, sy), Config.Colors.Black);
            }
        }

        // Hovered-tile 'X' cursor (single tile only — no region highlight).
        if (_hoverTermX >= 0 && _hoverTermY >= 0)
            _terminal.SetCell(_hoverTermX, _hoverTermY, 'X', XColor, XBg);

        RenderCancel();
    }

    private void RenderCancel()
    {
        Vector4 fg = _cancelHovered ? Config.Colors.BrightYellow : Config.Colors.LightGray75;
        Vector4 bg = _cancelHovered ? Config.Colors.DarkYellow   : Config.Colors.Black;
        _terminal.FillRect(_cancelX, _cancelY, _cancelW, 1, ' ', fg, bg);
        const string label = "[ CANCEL ]";
        _terminal.Text(_cancelX + (_cancelW - label.Length) / 2, _cancelY, label, fg, bg);
    }

    /// <summary>Hover handler — updates the 'X' cursor and cancel-button highlight.</summary>
    public void OnMouseMove(int x, int y)
    {
        bool newCancel = IsOnCancel(x, y);

        int newHoverX = -1, newHoverY = -1;
        if (TryArtCellAt(x, y, out int sx, out int sy) && IsValidTile(sx, sy))
        {
            newHoverX = x;
            newHoverY = y;
        }

        bool hoverChanged = newHoverX != _hoverTermX || newHoverY != _hoverTermY;

        if (newCancel != _cancelHovered || hoverChanged)
        {
            // Hover tick: only when entering a NEW valid tile (not while sweeping inside one,
            // and not when sliding off into invalid background). Matches the rest of the fight
            // UI, which uses SmallInteraction for hover transitions.
            if (hoverChanged && newHoverX >= 0)
                _sfx?.Invoke(GameEventType.SmallInteraction);

            _cancelHovered = newCancel;
            _hoverTermX = newHoverX;
            _hoverTermY = newHoverY;
            Render();
        }
    }

    /// <summary>Click handler — selects a localization or cancels.</summary>
    public void OnMouseClick(int x, int y)
    {
        if (IsOnCancel(x, y))
        {
            _sfx?.Invoke(GameEventType.NeutralOutcome);
            _onCancel?.Invoke();
            return;
        }

        if (!TryArtCellAt(x, y, out int sx, out int sy)) return;

        var organ = _art.GetOrganPartInfoAt(sx, sy);
        if (organ != null)
        {
            _sfx?.Invoke(GameEventType.StrongInteraction);
            _onSelected($"{organ.OrganPartName},{organ.BodyPartName}");
            return;
        }

        var bodyPart = _art.GetBodyPartIdAt(sx, sy);
        if (bodyPart != null)
        {
            _sfx?.Invoke(GameEventType.StrongInteraction);
            _onSelected(bodyPart);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>Maps a terminal cell to art coordinates; false when outside the rendered art.</summary>
    private bool TryArtCellAt(int x, int y, out int sx, out int sy)
    {
        int ax = x - _artOriginX;
        int ay = y - _artOriginY;
        sx = _artMinX + ax;
        sy = _artMinY + ay;
        return ax >= 0 && ax < _artW && ay >= 0 && ay < _artH;
    }

    /// <summary>A tile is valid when it belongs to an organ part or a body part.</summary>
    private bool IsValidTile(int sx, int sy) =>
        _art.IsBodyCell(sx, sy)
        && (_art.GetOrganPartInfoAt(sx, sy) != null || _art.GetBodyPartIdAt(sx, sy) != null);

    private bool IsOnCancel(int x, int y) =>
        y == _cancelY && x >= _cancelX && x < _cancelX + _cancelW;
}
