using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// The ☰ in the top-right corner of the screen: the main menu by mouse. Shown in exactly the phases
/// in which Escape pauses (<c>LocationTravelGameController.PausedPhaseName</c>), so the game can be
/// played, paused and quit without a keyboard.
///
/// <para><b>Drawn over whatever the phase drew there</b>, every frame, after the phase has drawn.
/// The phases do not leave the corner free — a fight's info box has its frame there — and asking
/// each of a dozen renderers to keep three cells clear would be a dozen places to forget it. What it
/// covered is kept and put back when the button goes (a modal box over the phase, a phase without
/// it), so a screen that is not redrawn every frame is not left with a dead button on it.</para>
///
/// <para>A cell is only written when it differs: every write marks the terminal changed, and the
/// grid would otherwise be uploaded again on every frame of a screen that is not moving.</para>
/// </summary>
public sealed class MenuButton
{
    public const char Glyph = '☰';
    private const string Face = " ☰ ";

    private readonly TerminalHUD _terminal;
    private readonly (char C, Vector4 Fg, Vector4 Bg)[] _under = new (char, Vector4, Vector4)[Face.Length];
    private bool _drawn;

    public MenuButton(TerminalHUD terminal) => _terminal = terminal;

    public int X => _terminal.Width - Face.Length;
    public int Y => 0;

    /// <summary>The cell a press lands on, for the CLI; null while the button is not on screen.</summary>
    public (int X, int Y)? Cell => _drawn ? (X + 1, Y) : null;

    public bool Contains(int x, int y) => _drawn && y == Y && x >= X && x < X + Face.Length;

    /// <summary>Puts the button up (or keeps it up), highlighted while the pointer is on it.</summary>
    public void Draw(bool hovered)
    {
        Vector4 fg = hovered ? Config.NarrativeUI.ContinueButtonHoverColor : Config.NarrativeUI.ContinueButtonColor;
        Vector4 bg = hovered ? Config.NarrativeUI.ContinueButtonHoverBackgroundColor : Config.NarrativeUI.ContinueButtonBackgroundColor;
        for (int i = 0; i < Face.Length; i++)
        {
            var cell = _terminal[X + i, Y];
            if (cell.Character == Face[i] && cell.TextColor == fg && cell.BackgroundColor == bg) continue;
            // Not what the button left there: the phase has drawn over it since, so this is what the
            // button now covers. (The button in its other highlight is still the button.)
            if (!_drawn || !IsButton(i))
                _under[i] = (cell.Character, cell.TextColor, cell.BackgroundColor);
            _terminal.SetCell(X + i, Y, Face[i], fg, bg);
        }
        _drawn = true;
    }

    /// <summary>Takes the button down, putting back what it covered.</summary>
    public void Hide()
    {
        if (!_drawn) return;
        _drawn = false;
        for (int i = 0; i < Face.Length; i++)
        {
            // Left alone if the phase has drawn there since: that is the screen now, not the button.
            if (!IsButton(i)) continue;
            var (c, fg, bg) = _under[i];
            _terminal.SetCell(X + i, Y, c, fg, bg);
        }
    }

    /// <summary>Whether cell <paramref name="i"/> of the button still shows the button, in either highlight.</summary>
    private bool IsButton(int i)
    {
        var cell = _terminal[X + i, Y];
        return cell.Character == Face[i]
            && (cell.BackgroundColor == Config.NarrativeUI.ContinueButtonBackgroundColor
                || cell.BackgroundColor == Config.NarrativeUI.ContinueButtonHoverBackgroundColor);
    }
}
