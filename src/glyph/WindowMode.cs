using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Cathedral.Glyph;

/// <summary>
/// Windowed ↔ fullscreen for the game window.
///
/// <para><b>Borderless, not exclusive.</b> The border is hidden and the window is sized to the
/// monitor it is on, rather than asking GLFW for an exclusive video mode. Exclusive fullscreen
/// changes the display resolution, which on this game buys nothing — everything is drawn into a
/// terminal grid that rescales to whatever client size it is given — and costs the two things
/// players notice: a black flicker on every alt-tab, and a desktop left at the wrong resolution if
/// the process dies while it holds the mode.</para>
///
/// <para>The windowed geometry is remembered on the way in and restored on the way out, because
/// GLFW does not: coming back from fullscreen without it leaves the window at monitor size with a
/// title bar, half off the screen.</para>
/// </summary>
public static class WindowMode
{
    private static bool     _isFullscreen;
    private static Vector2i _windowedSize     = new(1200, 900);
    private static Vector2i _windowedLocation = new(100, 100);

    /// <summary>Whether the window is currently borderless-fullscreen.</summary>
    public static bool IsFullscreen => _isFullscreen;

    /// <summary>Switches to <paramref name="fullscreen"/>, doing nothing if already there.</summary>
    public static void Apply(NativeWindow window, bool fullscreen)
    {
        if (window == null || fullscreen == _isFullscreen) return;

        if (fullscreen)
        {
            // Remember where the window was before it stops being a window.
            _windowedSize     = window.ClientSize;
            _windowedLocation = window.Location;

            var monitor = Monitors.GetMonitorFromWindow(window);
            window.WindowBorder = WindowBorder.Hidden;
            window.Location     = new Vector2i(monitor.ClientArea.Min.X, monitor.ClientArea.Min.Y);
            window.ClientSize   = new Vector2i(monitor.ClientArea.Size.X, monitor.ClientArea.Size.Y);
        }
        else
        {
            window.WindowBorder = WindowBorder.Resizable;
            window.ClientSize   = _windowedSize;
            window.Location     = _windowedLocation;
        }

        _isFullscreen = fullscreen;
        System.Console.WriteLine($"WindowMode: {(fullscreen ? "fullscreen" : "windowed")} "
                               + $"({window.ClientSize.X}x{window.ClientSize.Y})");
    }

    /// <summary>Flips the mode and returns the new state. The F11 key and the Settings row.</summary>
    public static bool Toggle(NativeWindow window)
    {
        Apply(window, !_isFullscreen);
        return _isFullscreen;
    }
}
