using System;
using System.Threading;
using System.Windows.Forms;
using Cathedral.Game;
using Cathedral.Game.Scene;

namespace Cathedral.Debug;

/// <summary>
/// Manages the scene debug window lifecycle.
/// When DebugMode is active, Show() spawns a dedicated STA thread.
/// </summary>
public static class SceneDebugManager
{
    private static SceneDebugWindow? _window;
    private static Thread? _uiThread;
    private static bool _appInitialized;

    /// <summary>
    /// Opens (or re-opens) the scene debug window.
    /// No-op when DebugMode is inactive.
    /// </summary>
    public static void Show(Cathedral.Game.Scene.Scene scene, PoV? pov, int locationId)
    {
        if (!DebugMode.ShowViewers) return;

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

                _window = new SceneDebugWindow(scene, pov, locationId);
                _window.Show();
                Application.Run(_window);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SceneDebugManager] Scene debug window error: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                    Console.WriteLine($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            finally
            {
                _window = null;
            }
        });

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Name = "SceneDebugUI";
        _uiThread.Start();
    }

    /// <summary>
    /// Updates the PoV highlight in the scene debug window.
    /// Thread-safe.
    /// </summary>
    public static void UpdatePoV(PoV pov)
    {
        if (_window is null || _window.IsDisposed) return;
        try
        {
            _window.Invoke(() => _window.UpdatePoV(pov));
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    /// <summary>Closes the window if open.</summary>
    public static void Close()
    {
        CloseInternal();
    }

    private static void CloseInternal()
    {
        if (_window is not null && !_window.IsDisposed)
        {
            try { _window.Invoke(() => _window.Close()); }
            catch { }
        }
        _window = null;

        if (_uiThread is not null && _uiThread.IsAlive)
        {
            _uiThread.Join(500);
            _uiThread = null;
        }
    }
}
