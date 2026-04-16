using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Cathedral.Game;

namespace Cathedral.Debug;

/// <summary>
/// Manages the LLM communication monitor window lifecycle.
/// When DebugMode is active, Show() spawns a dedicated STA thread that runs the
/// WinForms message loop alongside the OpenTK game window.
/// All public methods are safe to call from the main game thread.
/// </summary>
public static class LlmMonitorDebugManager
{
    private static LlmMonitorWindow? _window;
    private static Thread? _uiThread;

    // WinForms bootstrap must happen exactly once per process.
    private static bool _appInitialized;

    /// <summary>
    /// Opens the LLM monitor window, auto-discovering the current/latest LLM session
    /// from <paramref name="logsBaseDir"/> (typically "logs").
    /// The window keeps watching the directory and switches to any new session automatically.
    /// No-op when DebugMode is inactive.
    /// </summary>
    public static void Show(string logsBaseDir = "logs")
    {
        if (!DebugMode.ShowViewers) return;

        // Close any previous window
        CloseInternal();

        // Resolve to absolute path so the window still works regardless of cwd changes.
        var absLogsDir = Path.GetFullPath(logsBaseDir);
        Directory.CreateDirectory(absLogsDir); // ensure it exists

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
                    catch { /* Another debug window already initialized WinForms for this process. */ }
                    _appInitialized = true;
                }

                _window = new LlmMonitorWindow(absLogsDir);
                Application.Run(_window);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[LlmMonitorDebugManager] LLM monitor window error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
            finally
            {
                _window = null;
            }
        });

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Name = "LlmMonitorDebugUI";
        _uiThread.Start();
    }

    /// <summary>
    /// Closes the monitor window if open.
    /// </summary>
    public static void Close() => CloseInternal();

    private static void CloseInternal()
    {
        if (_window is null || _window.IsDisposed) return;
        try
        {
            _window.Invoke(() => _window.Close());
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }
}
