using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cathedral.LLM;

namespace Cathedral;

/// <summary>
/// Everything the player can change and expects to still be there next time, persisted to
/// <c>%APPDATA%\Cathedral\settings.json</c>.
///
/// <para>Was <c>AudioSettings</c>. It holds the compute-device settings too now, and two classes
/// writing one file would have meant each one's save silently discarding the other's fields — the
/// file is rewritten whole, not merged. One store, one write.</para>
///
/// <para>Loading and saving are both defensive and never throw: a corrupt file falls back to
/// defaults, and a read-only profile still gets working settings for the session. Nothing here is
/// important enough to stop the game starting.</para>
/// </summary>
public static class UserSettings
{
    // ── Audio ────────────────────────────────────────────────────────────────

    /// <summary>Music (procedural ambient + filter layers) volume, 0–100.</summary>
    public static int MusicVolume { get; set; } = 100;

    /// <summary>Sound-effects (UI ticks + MIDI stings) volume, 0–100.</summary>
    public static int SfxVolume { get; set; } = 100;

    /// <summary>Music volume as a 0..1 scale for the audio engine.</summary>
    public static float MusicVolume01 => Math.Clamp(MusicVolume, 0, 100) / 100f;

    /// <summary>SFX volume as a 0..1 scale for the audio engine.</summary>
    public static float SfxVolume01 => Math.Clamp(SfxVolume, 0, 100) / 100f;

    // ── Video ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Whether the final full-screen dither layer is on. Defaults to true, matching
    /// <c>Config.PostProcess.DitherMode</c>'s default of Bayer 8x8.
    /// <para>
    /// Only the on/off state is persisted, not <i>which</i> dither. The renderer's
    /// <c>Enabled</c> is derived from its mode (<c>Mode != Off</c>) and restores whichever dither
    /// was last in use, so applying this at startup turns the layer on or off without disturbing a
    /// mode chosen by <c>--dither</c>. The F/G/H keys that retune mode, palette depth and grain
    /// remain live tuning, deliberately unsaved — persisting one of the three and not the others
    /// would be worse than persisting none.
    /// </para>
    /// </summary>
    public static bool DitherEnabled { get; set; } = true;

    /// <summary>
    /// Whether the game window opens borderless-fullscreen. Applied once the window exists (see
    /// <c>WindowMode</c>), and toggled either from the Settings screen or with F11 — both of which
    /// write back here, so the mode the player left in is the mode they come back to.
    /// </summary>
    public static bool Fullscreen { get; set; } = false;

    /// <summary>
    /// Index into <c>Config.Terminal.GlyphWeightSteps</c> — how heavily the terminal text is drawn.
    /// A playtester finding the text hard to read is the reason this is a setting: FreeMono is a
    /// hairline face and how well it survives the trip to a given panel depends on that panel.
    /// <para>Applied at startup, and the atlas must be re-rastered when it is not the default —
    /// half of a weight step is baked into the raster rather than shaded. See
    /// <c>Config.Terminal.ApplyGlyphWeight</c>.</para>
    /// </summary>
    public static int GlyphWeight { get; set; } = Config.Terminal.GlyphWeightDefaultStep;

    /// <summary>
    /// How far a glyph overflows its cell, and so how large the text reads at a fixed grid.
    /// <para>This is the nearest thing to a text-size setting the terminal can offer: the 100x100
    /// grid is part of the layout — every renderer hardcodes rows against it — so cell size is
    /// the window divided by a constant, and only the glyph inside the cell can grow.</para>
    /// </summary>
    public static float GlyphScale { get; set; } = Config.Terminal.GlyphScaleDefault;

    // ── Language model ───────────────────────────────────────────────────────
    //
    // These take effect at the next launch. The server loads the model once at startup and holds
    // it for the session; applying a device change in place would mean tearing down the server,
    // reloading two gigabytes, and rebuilding every cached persona slot — possibly mid-narration.
    // The Settings screen says so on the rows.

    /// <summary>
    /// The player's choice. <see cref="LlamaComputeDevice.Auto"/> defers to
    /// <see cref="LlmProbedDevice"/>; anything else overrides the probe.
    /// </summary>
    public static LlamaComputeDevice LlmDevice { get; set; } = LlamaComputeDevice.Auto;

    /// <summary>
    /// Layers to offload to the GPU. <b>-1 means unset</b>, which is the default and almost always
    /// right: llama.cpp's <c>--fit</c> is on by default and sizes the offload to fit device memory
    /// with a margin. Naming a number here overrides that, and a number too large for the card
    /// fails to allocate — which is exactly what the old hardcoded <c>-ngl 99</c> did.
    /// </summary>
    public static int LlmGpuLayers { get; set; } = -1;

    /// <summary>
    /// CPU threads for inference. <b>0 means unset</b> — llama.cpp picks from the core count, which
    /// it does well. Worth lowering on a thermally constrained laptop, where sustained full-core
    /// load costs more in clock throttling than it buys in tokens.
    /// </summary>
    public static int LlmCpuThreads { get; set; } = 0;

    // ── Probe results (written by LlamaProbe, not by the player) ──────────────

    /// <summary>What the probe measured as fastest. Consulted when <see cref="LlmDevice"/> is Auto.</summary>
    public static LlamaComputeDevice LlmProbedDevice { get; set; } = LlamaComputeDevice.Cpu;

    /// <summary>The backend folder the probe chose (<c>vulkan</c>, <c>cuda</c>), or null for CPU.</summary>
    public static string? LlmProbedBackend { get; set; }

    /// <summary>
    /// The model the probe measured against, as size:timestamp. Empty means never probed. When this
    /// stops matching the model file, the probe re-runs — replacing <c>model.gguf</c> is the
    /// documented way to change models, and a device chosen for a 2 GB model is not evidence about
    /// a 5 GB one.
    /// </summary>
    public static string LlmProbeSignature { get; set; } = "";

    /// <summary>The probe's one-line reasoning, shown in the Settings screen.</summary>
    public static string LlmProbeSummary { get; set; } = "";

    /// <summary>
    /// The device to actually launch with: the player's choice, or the probe's when that is Auto.
    /// </summary>
    public static LlamaComputeDevice EffectiveLlmDevice
        => LlmDevice == LlamaComputeDevice.Auto ? LlmProbedDevice : LlmDevice;

    // ── Persistence ──────────────────────────────────────────────────────────

    // Named per build (see AppData): a shipped game must not inherit a compute device probed by a
    // development run, or volumes set while testing.
    private static string SettingsPath => AppData.PathTo("settings.json");

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

            DitherEnabled = dto.DitherEnabled;
            Fullscreen    = dto.Fullscreen;

            // Clamped to exactly the range the Settings screen offers, because this file is
            // hand-editable and these two can make the game unreadable rather than merely ugly:
            // a scale of 0 draws no glyphs at all, and a weight index off the end of the ladder
            // throws inside the renderer. The screen is itself terminal text, so a bad value here
            // also breaks the one place a player would go to undo it.
            GlyphWeight = Math.Clamp(dto.GlyphWeight, 0, Config.Terminal.GlyphWeightSteps.Length - 1);
            GlyphScale  = Math.Clamp(dto.GlyphScale,
                Config.Terminal.GlyphScaleMin, Config.Terminal.GlyphScaleMax);

            LlmDevice     = dto.LlmDevice;
            LlmGpuLayers  = dto.LlmGpuLayers < 0 ? -1 : dto.LlmGpuLayers;
            LlmCpuThreads = Math.Clamp(dto.LlmCpuThreads, 0, 256);

            LlmProbedDevice   = dto.LlmProbedDevice;
            LlmProbedBackend  = dto.LlmProbedBackend;
            LlmProbeSignature = dto.LlmProbeSignature ?? "";
            LlmProbeSummary   = dto.LlmProbeSummary ?? "";
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
            var dto = new SettingsDto
            {
                MusicVolume = MusicVolume,
                SfxVolume   = SfxVolume,

                DitherEnabled = DitherEnabled,
                Fullscreen    = Fullscreen,
                GlyphWeight   = GlyphWeight,
                GlyphScale    = GlyphScale,

                LlmDevice     = LlmDevice,
                LlmGpuLayers  = LlmGpuLayers,
                LlmCpuThreads = LlmCpuThreads,

                LlmProbedDevice   = LlmProbedDevice,
                LlmProbedBackend  = LlmProbedBackend,
                LlmProbeSignature = LlmProbeSignature,
                LlmProbeSummary   = LlmProbeSummary
            };
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
        public bool DitherEnabled { get; set; } = true;
        public bool Fullscreen { get; set; } = false;
        public int GlyphWeight { get; set; } = Config.Terminal.GlyphWeightDefaultStep;
        public float GlyphScale { get; set; } = Config.Terminal.GlyphScaleDefault;

        // Written as "Auto"/"Gpu"/"Cpu" rather than 0/1/2, so the file stays hand-editable and a
        // reordered enum cannot silently change what an existing settings file means.
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LlamaComputeDevice LlmDevice { get; set; } = LlamaComputeDevice.Auto;
        public int LlmGpuLayers { get; set; } = -1;
        public int LlmCpuThreads { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LlamaComputeDevice LlmProbedDevice { get; set; } = LlamaComputeDevice.Cpu;
        public string? LlmProbedBackend { get; set; }
        public string? LlmProbeSignature { get; set; }
        public string? LlmProbeSummary { get; set; }
    }
}
