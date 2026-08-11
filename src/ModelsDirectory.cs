using System;
using System.IO;

namespace Cathedral;

/// <summary>
/// Locates the <c>models</c> directory — the model, the llama.cpp runtime, the GloVe vectors and
/// the sanitizer word lists.
/// <para>
/// Two layouts have to work from one lookup. A shipped game puts <c>models</c> beside the
/// executable; a developer running <c>dotnet run</c> gets a base directory of
/// <c>bin/Debug/net8.0-windows/</c> with <c>models</c> three levels up at the repository root. So
/// the search checks the base directory first — the shipped layout wins when both could match —
/// and then walks up.
/// </para>
/// <para>
/// The walk is <b>bounded</b> (<see cref="MaxParentLevels"/>), which is the point of this class.
/// Three call sites each had their own copy of <c>while (dir != null &amp;&amp; !Directory.Exists(...))</c>,
/// which on a machine with no <c>models</c> folder does not stop at the install directory: it climbs
/// to the drive root, and would happily bind to an unrelated <c>C:\models</c> or to a folder in the
/// player's profile. Six levels reaches the repository root from a build output directory and
/// nothing further.
/// </para>
/// </summary>
public static class ModelsDirectory
{
    /// <summary>Directory name searched for. Also the name the resolved path ends in.</summary>
    public const string FolderName = "models";

    /// <summary>
    /// How far above the base directory to look. Four levels reaches the repository root from
    /// <c>bin/Debug/net8.0-windows/</c>; six leaves room for a deeper build layout without letting
    /// the search escape to the drive root.
    /// </summary>
    private const int MaxParentLevels = 6;

    private static string? _resolved;
    private static bool _searched;

    /// <summary>
    /// The absolute path of the models directory, or <c>null</c> if there is none within reach.
    /// Resolved once and cached; callers may hold the result.
    /// </summary>
    public static string? Resolve()
    {
        if (_searched) return _resolved;
        _searched = true;

        var baseDir = AppContext.BaseDirectory;

        var beside = Path.Combine(baseDir, FolderName);
        if (Directory.Exists(beside))
        {
            _resolved = Path.GetFullPath(beside);
            return _resolved;
        }

        var dir = new DirectoryInfo(baseDir);
        for (int level = 0; level < MaxParentLevels && dir != null; level++)
        {
            var candidate = Path.Combine(dir.FullName, FolderName);
            if (Directory.Exists(candidate))
            {
                _resolved = Path.GetFullPath(candidate);
                return _resolved;
            }
            dir = dir.Parent;
        }

        return _resolved;
    }

    /// <summary>
    /// The models directory, or a <see cref="DirectoryNotFoundException"/> naming where it looked.
    /// For callers that cannot proceed without it.
    /// </summary>
    public static string Require()
        => Resolve() ?? throw new DirectoryNotFoundException(
            $"Could not find a '{FolderName}' directory beside '{AppContext.BaseDirectory}' " +
            $"or within {MaxParentLevels} parent directories of it.");

    /// <summary>
    /// A path inside the models directory, or <c>null</c> if the directory itself is missing.
    /// Does not check that the file exists.
    /// </summary>
    public static string? PathTo(params string[] relativeParts)
    {
        var root = Resolve();
        if (root == null) return null;

        var parts = new string[relativeParts.Length + 1];
        parts[0] = root;
        Array.Copy(relativeParts, 0, parts, 1, relativeParts.Length);
        return Path.Combine(parts);
    }
}
