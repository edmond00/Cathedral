using System;
using System.Threading;
using System.Windows.Forms;
using Cathedral.Game;
using Cathedral.Game.Narrative;

namespace Cathedral.Debug;

/// <summary>
/// Manages the narration-graph debug window lifecycle.
/// When DebugMode is active, Show() spawns a dedicated STA thread that runs the
/// WinForms message loop alongside the OpenTK game window.
/// All public methods are safe to call from the main game thread.
/// </summary>
public static class NarrationGraphDebugManager
{
    private static NarrationGraphWindow? _window;
    private static Thread? _uiThread;

    private static bool _appInitialized;

    /// <summary>
    /// Opens (or re-opens) the graph window for the given graph.
    /// No-op when DebugMode is inactive.
    /// </summary>
    public static void Show(NarrationGraph graph, int locationId)
    {
        if (!DebugMode.IsActive) return;

        CloseInternal();

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
                    catch { }
                    _appInitialized = true;
                }

                _window = new NarrationGraphWindow(graph, locationId);
                Application.Run(_window);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NarrationGraphDebugManager] Graph window error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
            finally
            {
                _window = null;
            }
        });

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Name = "NarrationGraphDebugUI";
        _uiThread.Start();
    }

    /// <summary>
    /// Updates the current-node highlight in the graph window.
    /// Thread-safe — may be called from the game (OpenTK) thread.
    /// </summary>
    public static void UpdateCurrentNode(NarrationNode node)
    {
        if (_window is null || _window.IsDisposed) return;
        try
        {
            _window.Invoke(() => _window.UpdateCurrentNode(node.NodeId));
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    /// <summary>Closes the debug window if open.</summary>
    public static void Close() => CloseInternal();

    private static void CloseInternal()
    {
        if (_window is null || _window.IsDisposed) return;
        try { _window.Invoke(() => _window.Close()); }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        _window = null;
    }
}
