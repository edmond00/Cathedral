using System;
using System.Threading;
using System.Windows.Forms;

namespace Cathedral.Debug;

/// <summary>
/// Standalone launcher for the dialogue-tree viewer (<c>--dialogue-view</c>).
/// Opens <see cref="DialogueViewWindow"/> on a dedicated STA thread and blocks until it is closed,
/// so the process stays alive for the lifetime of the window. No LLM server or game state needed.
/// </summary>
public static class DialogueViewLauncher
{
    public static void Launch()
    {
        Exception? error = null;

        var uiThread = new Thread(() =>
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new DialogueViewWindow());
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Name = "DialogueViewUI";
        uiThread.Start();
        uiThread.Join();

        if (error != null)
        {
            Console.WriteLine($"[DialogueViewLauncher] {error.GetType().Name}: {error.Message}");
            Console.WriteLine(error.StackTrace);
        }
    }
}
