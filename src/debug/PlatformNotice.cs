#if !WINDOWS
using System;
using System.Collections.Generic;

namespace Cathedral.Debug;

/// <summary>
/// The developer debug windows are WinForms, and WinForms exists only on Windows: elsewhere they
/// are compiled out (the <c>WINDOWS</c> constant, set by Cathedral.csproj) and their managers keep
/// their public surface so the game calls them unchanged. Asking for one prints this, once per
/// window per process — <c>--debug</c> opens the viewers on every scene, and a line per scene
/// would bury the log.
/// </summary>
internal static class PlatformNotice
{
    private static readonly HashSet<string> _told = new();

    public static void WindowUnavailable(string window, string? hint = null)
    {
        lock (_told)
            if (!_told.Add(window)) return;
        Console.Error.WriteLine($"[debug] The {window} is not available on this platform: it is a WinForms window, Windows only.{(hint is null ? "" : " " + hint)}");
    }
}
#endif
