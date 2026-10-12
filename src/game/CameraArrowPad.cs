// CameraArrowPad.cs — the compass rose in the top-right corner, by which the camera is turned with
// the mouse alone. Drawn over the world map and over the world-selection sky.
using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Game
{
    /// <summary>Which way an arm of the rose turns the camera.</summary>
    public enum CameraArrow
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// A compass rose, drawn as ink on the sky in the corner of the screen the way a compass is drawn
    /// in the corner of a map: four arms, four cardinal letters, no frame and no fill.
    ///
    /// <para><b>Why it exists.</b> Turning the camera was reachable only from the arrow keys, and the
    /// sky is a sphere — half the moons of the world-selection screen, and a good part of the world
    /// map, sit behind the viewer until something brings them round. Every other control in the game
    /// is a click, so this was the one place a mouse could get stuck. The keys still work and do
    /// exactly the same thing.</para>
    ///
    /// <para><b>Only the ink is clickable.</b> A cell painted with a character is opaque to the
    /// terminal's passthrough rule and reaches this class; a blank one falls through to the sky
    /// behind. That is what keeps the rose frameless — but it also means each arm has to be drawn
    /// with enough ink to be hit, which is why the arms carry their rays and their letters rather
    /// than a bare arrowhead.</para>
    ///
    /// <para><b>Held, not tapped.</b> The rose reports which arm the cursor is over; the caller turns
    /// the camera for as long as the button is down. A click released before the next frame would
    /// move the camera by a fraction of a degree and read as a dead control, so a press also turns it
    /// a fixed step at once — see <see cref="Config.CameraPad.ClickStep"/>.</para>
    /// </summary>
    public sealed class CameraArrowPad
    {
        private readonly TerminalHUD _terminal;

        private int _x, _y;
        private CameraArrow _hovered = CameraArrow.None;
        private bool _painted;

        // The rose, 13 x 5, drawn around a centre at column 6, row 2:
        //
        //          N
        //        ·  ▲  ·
        //     W ◄──  ──► E
        //        ·  ▼  ·
        //          S
        //
        // The four diagonal dots are the rose's minor points. They are ornament, and they are also
        // hit area: they widen the two vertical arms, which would otherwise be one cell across.
        private const int Width  = Config.CameraPad.Width;   // 13
        private const int Height = Config.CameraPad.Height;  // 5
        private const int MidX   = 6;
        private const int MidY   = 2;

        public CameraArrowPad(TerminalHUD terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        /// <summary>The arm the cursor was last over, or <see cref="CameraArrow.None"/>.</summary>
        public CameraArrow Hovered => _hovered;

        /// <summary>
        /// Records the hover position. Returns true when the arm under the cursor changed, which is
        /// the caller's cue to tick and repaint.
        /// </summary>
        public bool SetHover(int cellX, int cellY)
        {
            var arrow = ArrowAt(cellX, cellY);
            if (arrow == _hovered) return false;
            _hovered = arrow;
            return true;
        }

        /// <summary>
        /// The arm at a cell, or <see cref="CameraArrow.None"/>. The four regions are rectangles
        /// rather than the exact painted cells, so a click that lands a character to one side of an
        /// arrowhead still turns the camera.
        /// </summary>
        public CameraArrow ArrowAt(int cellX, int cellY)
        {
            if (!_painted) return CameraArrow.None;

            int dx = cellX - _x, dy = cellY - _y;
            if (dx < 0 || dx >= Width || dy < 0 || dy >= Height) return CameraArrow.None;

            if (dy == MidY)
            {
                if (dx >= 1 && dx <= MidX - 1)          return CameraArrow.Left;
                if (dx >= MidX + 1 && dx <= Width - 2)  return CameraArrow.Right;
                return CameraArrow.None;
            }
            if (dx < MidX - 2 || dx > MidX + 2) return CameraArrow.None;
            return dy < MidY ? CameraArrow.Up : CameraArrow.Down;
        }

        /// <summary>The cell a script should click to press <paramref name="arrow"/> — an inked one.</summary>
        public (int X, int Y) CellFor(CameraArrow arrow) => arrow switch
        {
            CameraArrow.Up    => (_x + MidX,     _y + MidY - 1),
            CameraArrow.Down  => (_x + MidX,     _y + MidY + 1),
            CameraArrow.Left  => (_x + MidX - 3, _y + MidY),
            CameraArrow.Right => (_x + MidX + 3, _y + MidY),
            _                 => (-1, -1),
        };

        /// <summary>Draws the rose in the top-right corner of the grid.</summary>
        public void Draw()
        {
            _x = _terminal.Width - Width - Config.CameraPad.RightMargin;
            _y = Config.CameraPad.TopMargin;
            _painted = true;

            // Nothing is cleared first and no background is painted: every cell this does not ink
            // stays as it was, which over a transparent overlay means the sky shows through it.
            Ink(MidX,     MidY - 2, 'N', CameraArrow.Up,    InkRole.Label);
            Ink(MidX,     MidY - 1, '▲', CameraArrow.Up,    InkRole.Head);
            Ink(MidX - 2, MidY - 1, '·', CameraArrow.Up,    InkRole.Ray);
            Ink(MidX + 2, MidY - 1, '·', CameraArrow.Up,    InkRole.Ray);

            Ink(MidX,     MidY + 2, 'S', CameraArrow.Down,  InkRole.Label);
            Ink(MidX,     MidY + 1, '▼', CameraArrow.Down,  InkRole.Head);
            Ink(MidX - 2, MidY + 1, '·', CameraArrow.Down,  InkRole.Ray);
            Ink(MidX + 2, MidY + 1, '·', CameraArrow.Down,  InkRole.Ray);

            Ink(MidX - 5, MidY, 'W', CameraArrow.Left,  InkRole.Label);
            Ink(MidX - 3, MidY, '◄', CameraArrow.Left,  InkRole.Head);
            Ink(MidX - 2, MidY, '─', CameraArrow.Left,  InkRole.Ray);
            Ink(MidX - 1, MidY, '─', CameraArrow.Left,  InkRole.Ray);

            Ink(MidX + 5, MidY, 'E', CameraArrow.Right, InkRole.Label);
            Ink(MidX + 3, MidY, '►', CameraArrow.Right, InkRole.Head);
            Ink(MidX + 2, MidY, '─', CameraArrow.Right, InkRole.Ray);
            Ink(MidX + 1, MidY, '─', CameraArrow.Right, InkRole.Ray);

            // The hub. Belongs to no arm, so a press on it turns nothing — which is right: it is the
            // point the rose turns about, not a fifth direction.
            Ink(MidX, MidY, '◆', CameraArrow.None, InkRole.Ray);
        }

        /// <summary>Wipes the rose back to transparent cells, restoring passthrough under it.</summary>
        public void Erase()
        {
            if (!_painted) return;
            for (int dy = 0; dy < Height; dy++)
                for (int dx = 0; dx < Width; dx++)
                    _terminal.SetCell(_x + dx, _y + dy, ' ',
                        Colors.Transparent, Colors.Transparent);
            _painted = false;
            _hovered = CameraArrow.None;
        }

        /// <summary>What a given cell of the rose is, which decides how far back it sits when unlit.</summary>
        private enum InkRole { Head, Ray, Label }

        /// <summary>
        /// Paints one cell. An arm lights <b>whole</b> when the cursor is on it — head, rays and
        /// letter together — because the hit region is the whole arm, and a highlight smaller than
        /// the thing it stands for teaches the wrong target.
        /// </summary>
        private void Ink(int dx, int dy, char glyph, CameraArrow arm, InkRole role)
        {
            Vector4 color =
                arm != CameraArrow.None && _hovered == arm ? Config.CameraPad.ArrowHoverColor
                : role switch
                {
                    InkRole.Head  => Config.CameraPad.ArrowColor,
                    InkRole.Label => Config.CameraPad.LabelColor,
                    _             => Config.CameraPad.RayColor,
                };

            // Transparent background throughout: the cell is ink, not a chip. It is still opaque to
            // the click-passthrough rule, because that rule tests the character as well.
            _terminal.SetCell(_x + dx, _y + dy, glyph, color, Colors.Transparent);
        }
    }
}
