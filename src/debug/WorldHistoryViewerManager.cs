using System;
using System.Threading;
using System.Windows.Forms;
using Cathedral.Game;
using Cathedral.Game.History;

namespace Cathedral.Debug;

/// <summary>
/// The world history viewer's lifecycle, on its own STA thread like the other viewers. Opened on the
/// world-selection screen when viewers are on (<c>--view</c>, or <c>--debug</c> without
/// <c>--hidden</c>). Every method is safe from the game thread, and the latest content is kept so a
/// window still being created shows it the moment it exists.
/// </summary>
public static class WorldHistoryViewerManager
{
    private static WorldHistoryWindow? _window;
    private static Thread? _uiThread;
    private static bool _appInitialized;
    private static readonly object _lock = new();

    // What the window should be showing: a message, or a history.
    private static string _message = "Choose a moon to read its history.";
    private static WorldHistory? _history;

    public static bool IsOpen => _window is { IsDisposed: false } || _uiThread is { IsAlive: true };

    /// <summary>Opens the window if viewers are on and it is not already open. No-op otherwise.</summary>
    public static void Show()
    {
        if (!DebugMode.ShowViewers || IsOpen) return;

        _uiThread = new Thread(() =>
        {
            try
            {
                if (!_appInitialized)
                {
                    try
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                    }
                    catch { /* Another viewer already initialised WinForms for this process. */ }
                    _appInitialized = true;
                }

                var window = new WorldHistoryWindow();
                // Content posted before the handle exists cannot be BeginInvoked and is dropped by
                // Post, so the window takes whatever is current once it has loaded.
                window.Load += (_, _) =>
                {
                    WorldHistory? history;
                    string message;
                    lock (_lock) { history = _history; message = _message; }
                    if (history != null) window.ShowHistory(history);
                    else window.ShowMessage(message);
                };
                lock (_lock) _window = window;
                Application.Run(window);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WorldHistoryViewer] window error: {ex.GetType().Name}: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
            finally
            {
                lock (_lock) _window = null;
            }
        });
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Name = "WorldHistoryViewerUI";
        _uiThread.Start();
    }

    /// <summary>Shows a line in place of a history ("building the history of Belune...").</summary>
    public static void ShowMessage(string message)
    {
        lock (_lock)
        {
            _message = message;
            _history = null;
        }
        Post(w => w.ShowMessage(message));
    }

    public static void ShowHistory(WorldHistory history)
    {
        lock (_lock) _history = history;
        Post(w => w.ShowHistory(history));
    }

    private static void Post(Action<WorldHistoryWindow> act)
    {
        var w = _window;
        if (w is null || w.IsDisposed || !w.IsHandleCreated) return;
        try { w.BeginInvoke(() => act(w)); }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }
}
