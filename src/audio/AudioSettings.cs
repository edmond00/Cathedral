using System;
using System.IO;
using System.Text.Json;

namespace Cathedral.Audio;

/// <summary>
/// User-adjustable audio settings, persisted to %APPDATA%\Cathedral\settings.json.
/// Volumes are stored as integer percentages (0–100). This is the game's first
/// persisted settings store; loading is defensive and never throws.
/// </summary>
public static class AudioSettings
{
    /// <summary>Music (procedural ambient + filter layers) volume, 0–100.</summary>
    public static int MusicVolume { get; set; } = 100;

    /// <summary>Sound-effects (UI ticks + MIDI stings) volume, 0–100.</summary>
    public static int SfxVolume { get; set; } = 100;

    /// <summary>Music volume as a 0..1 scale for the audio engine.</summary>
    public static float MusicVolume01 => Math.Clamp(MusicVolume, 0, 100) / 100f;

    /// <summary>SFX volume as a 0..1 scale for the audio engine.</summary>
    public static float SfxVolume01 => Math.Clamp(SfxVolume, 0, 100) / 100f;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Cathedral", "settings.json");

    /// <summary>
    /// Loads settings from disk. Missing or corrupt files silently fall back to defaults.
    /// </summary>
    public static void Load()
    {
        try
        {
            string path = SettingsPath;
            if (!File.Exists(path)) return;

            var dto = JsonSerializer.Deserialize<SettingsDto>(File.ReadAllText(path));
            if (dto == null) return;

            MusicVolume = Math.Clamp(dto.MusicVolume, 0, 100);
            SfxVolume   = Math.Clamp(dto.SfxVolume, 0, 100);
        }
        catch
        {
            // Corrupt/unreadable settings — keep defaults.
        }
    }

    /// <summary>
    /// Persists current settings to disk, creating the directory if needed.
    /// Failures are swallowed (e.g. read-only profile) — settings still apply in-session.
    /// </summary>
    public static void Save()
    {
        try
        {
            string path = SettingsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var dto = new SettingsDto { MusicVolume = MusicVolume, SfxVolume = SfxVolume };
            File.WriteAllText(path, JsonSerializer.Serialize(dto,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Best-effort persistence.
        }
    }

    private sealed class SettingsDto
    {
        public int MusicVolume { get; set; } = 100;
        public int SfxVolume { get; set; } = 100;
    }
}
