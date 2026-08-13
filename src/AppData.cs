using System;
using System.IO;

namespace Cathedral;

/// <summary>
/// Where the game keeps what it remembers between runs: the save and the settings.
///
/// <para><b>The folder differs between a development build and a shipped one</b>, and that is the
/// whole point of this class. They used to share <c>%APPDATA%\Cathedral</c>, which meant a
/// <c>dotnet run</c> session's save and settings were the ones a shipped build read — so testing
/// the shipped game was never testing a clean install. A save left by development work would
/// light up Continue in the packaged game, and a compute device probed in one would be inherited
/// by the other.</para>
///
/// <para>There is deliberately <b>no migration</b> from the old folder. Copying development data
/// into the shipped build's folder is exactly the coupling this separation removes; a shipped
/// build starting empty is the correct behaviour, not a gap.</para>
///
/// <para>Note this is the one place the development name survives into a path. Nothing a player
/// sees says "Cathedral" — but a developer's own folder is not something a player sees.</para>
/// </summary>
public static class AppData
{
    /// <summary>
    /// The folder under <c>%APPDATA%</c>. Conditioned on the same SHIP constant as the executable
    /// name, so the two cannot drift: a build that calls itself ProscribedPalimpsest.exe keeps its
    /// data beside that name.
    /// </summary>
    public const string FolderName =
#if SHIP
        "ProscribedPalimpsest";
#else
        "Cathedral";
#endif

    /// <summary>
    /// <c>%APPDATA%\&lt;FolderName&gt;</c>. Not created here — the writers create it when they
    /// first write, and a folder that exists but is empty would be indistinguishable from a
    /// finished run that saved nothing.
    /// </summary>
    public static string Directory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        FolderName);

    /// <summary>A file inside that folder. Does not check that it exists.</summary>
    public static string PathTo(string fileName) => Path.Combine(Directory, fileName);
}
