using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cathedral.Game.Save;

/// <summary>
/// The save file on disk. Knows where it lives, how to write it without risking the previous one, how
/// to read it back, and how to delete it. Knows nothing about the game.
///
/// <para>Follows <see cref="UserSettings"/>'s persistence idiom — reflection-based
/// <c>System.Text.Json</c>, string enums, and never throwing at a caller — with the two departures
/// that file does not need. A torn settings file costs a volume level; a torn save costs a run.</para>
/// </summary>
public static class SaveFile
{
    /// <summary>
    /// Beside settings.json, so one folder holds everything the game remembers. That folder is
    /// named per build — see <see cref="AppData"/> — so a shipped game never reads a save left by
    /// development work.
    /// </summary>
    private static string DefaultPath => AppData.PathTo("save.json");

    private static string? _pathOverride;
    private static bool _disabled;

    /// <summary>Where the save lives for this process.</summary>
    public static string Path_ => _pathOverride ?? DefaultPath;

    /// <summary>
    /// True when this process must not read or write a save at all.
    ///
    /// <para><b>Default-on under <c>--cli</c>.</b> Autosave fires on every entry to the world map, so
    /// without this every scripted run in the suite would overwrite the player's own save — hundreds
    /// of times per full run, silently. A script that is actually testing saving passes
    /// <c>--save-path</c>, which turns it back on against a file of its own.</para>
    /// </summary>
    public static bool Disabled => _disabled;

    /// <summary>
    /// Settles the save location for this process. Call once, early — <see cref="PeekSeed"/> runs
    /// before the master seed is locked, so this has to have happened by then.
    /// </summary>
    public static void Configure(string? pathOverride, bool noSave, bool cliActive)
    {
        _pathOverride = string.IsNullOrWhiteSpace(pathOverride) ? null : pathOverride;
        _disabled     = noSave || (cliActive && _pathOverride == null);

        if (_disabled)
            Console.WriteLine(noSave
                ? "SaveFile: saving disabled (--no-save)."
                : "SaveFile: saving disabled (--cli without --save-path, so the real save is left alone).");
        else if (_pathOverride != null)
            Console.WriteLine($"SaveFile: using save path '{_pathOverride}'.");
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        // Unindented on purpose: nobody hand-edits a save, and this one is written on every entry to
        // the world map. Settings are indented because a person may well open them.
        WriteIndented = false,
        Converters    = { new JsonStringEnumConverter() },
    };

    /// <summary>True when a save exists that this build can actually read.</summary>
    public static bool Exists() => !_disabled && Read() != null;

    /// <summary>
    /// The saved run, or null when there is none, it cannot be read, or it was written by a different
    /// build. All three are the same answer to the caller — Continue greys out — and none of them
    /// throw.
    /// </summary>
    public static SaveGame? Read()
    {
        if (_disabled) return null;
        try
        {
            if (!File.Exists(Path_)) return null;
            var save = JsonSerializer.Deserialize<SaveGame>(File.ReadAllText(Path_), Options);
            if (save == null) return null;
            if (save.Version != SaveGame.CurrentVersion)
            {
                Console.WriteLine(
                    $"SaveFile: save is version {save.Version}, this build reads {SaveGame.CurrentVersion} — ignoring it.");
                return null;
            }
            return save;
        }
        catch (Exception ex)
        {
            // A corrupt or unreadable save is treated as no save. Deliberately not repaired and
            // deliberately not partially read: half a run is worse than none when there is only one.
            Console.Error.WriteLine($"SaveFile: could not read the save ({ex.GetType().Name}: {ex.Message}).");
            return null;
        }
    }

    /// <summary>
    /// The seed of the saved run without building anything, or null when there is no readable save.
    ///
    /// <para>Exists because of an ordering constraint: the world is generated from the master seed at
    /// startup, and the seed locks before the window opens. Reading the seed here lets the process
    /// boot straight into the saved world, so Continue does no world-building at all.</para>
    /// </summary>
    public static int? PeekSeed() => Read()?.Seed;

    /// <summary>
    /// Writes <paramref name="save"/>, replacing any previous one. Never throws: a save that cannot be
    /// written must not take the run down with it.
    ///
    /// <para>Written to a temporary file in the same directory and then moved over the target, so a
    /// crash mid-write leaves the previous save intact rather than a truncated one. The move is atomic
    /// on the same volume, which is why the temporary file is not put in the system temp directory.</para>
    /// </summary>
    public static bool Write(SaveGame save)
    {
        if (_disabled) return false;
        try
        {
            string path = Path_;
            string? dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(save, Options));
            File.Move(tmp, path, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SaveFile: could not write the save ({ex.GetType().Name}: {ex.Message}).");
            return false;
        }
    }

    /// <summary>
    /// Removes the save — permadeath, and starting a new run. Also clears any temporary file left by a
    /// failed write, so a later read cannot find a stale one.
    /// </summary>
    public static void Delete()
    {
        if (_disabled) return;
        try
        {
            if (File.Exists(Path_))         File.Delete(Path_);
            if (File.Exists(Path_ + ".tmp")) File.Delete(Path_ + ".tmp");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SaveFile: could not delete the save ({ex.GetType().Name}: {ex.Message}).");
        }
    }
}
