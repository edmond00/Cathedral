using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Glyph;

/// <summary>
/// The GLFW error callback, replacing OpenTK's, which throws on every error.
///
/// <para>Wayland refuses a few window requests outright — reading or setting the window's position,
/// above all — and GLFW reports each as <see cref="ErrorCode.FeatureUnavailable"/>. Thrown, that
/// took the game down the moment F11 was pressed on a Wayland desktop. Such a request changes
/// nothing there whether or not it is made, so it is logged and play goes on. Every other error
/// still throws, as before.</para>
/// </summary>
public static class GlfwErrors
{
    // Held so the delegate GLFW calls into is never collected.
    private static GLFWCallbacks.ErrorCallback? _callback;

    /// <summary>The platform GLFW is drawing through: Win32, X11, Wayland…</summary>
    public static Platform CurrentPlatform => GLFW.GetPlatform();

    public static bool OnWayland => CurrentPlatform == Platform.Wayland;

    /// <summary>Installs the callback. Call once GLFW is initialised (after the window exists).</summary>
    public static void Install()
    {
        _callback = OnError;
        // Through OpenTK's provider rather than GLFW directly, so OpenTK does not put its own back.
        OpenTK.Windowing.Desktop.GLFWProvider.SetErrorCallback(_callback);
        System.Console.WriteLine($"GLFW platform: {CurrentPlatform}");
    }

    private static void OnError(ErrorCode code, string description)
    {
        if (code == ErrorCode.FeatureUnavailable)
        {
            System.Console.WriteLine($"GLFW: {description} (not available on this platform; ignored)");
            return;
        }
        throw new GLFWException($"{description} (this is thrown from a GLFW callback)", code);
    }
}
