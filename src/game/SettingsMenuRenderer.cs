using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.LLM;

namespace Cathedral.Game;

/// <summary>
/// Renders and manages the Settings screen on the TerminalHUD. Reached from the main menu; active
/// only during <see cref="GameMode.Settings"/>.
///
/// <para>Two groups, each under a heading, sharing one column geometry so every label, button and
/// value lines up down the screen:</para>
/// <list type="bullet">
/// <item><b>Audio &amp; video</b> — music and SFX volumes as [ - ] / [ + ] steppers, and the dither
/// toggle. Dither is a post-process shader rather than a sound setting, which is why the heading
/// names both and not just audio.</item>
/// <item><b>Language model</b> — compute device, GPU layers, thread count and re-detect. Unlike
/// everything above, none of these apply until the next launch; see the row comments.</item>
/// </list>
/// </summary>
public class SettingsMenuRenderer
{
    private readonly TerminalHUD _terminal;

    /// <summary>Music volume, 0–100. Initialize before Render().</summary>
    public int MusicVolume { get; set; } = 100;

    /// <summary>Sound-effects volume, 0–100. Initialize before Render().</summary>
    public int SfxVolume { get; set; } = 100;

    /// <summary>Fired with the new music volume (0–100) when a music button is clicked.</summary>
    public Action<int>? OnMusicVolumeChanged { get; set; }

    /// <summary>Fired with the new SFX volume (0–100) when an SFX button is clicked.</summary>
    public Action<int>? OnSfxVolumeChanged { get; set; }

    /// <summary>
    /// Whether the final full-screen dither layer is on. Covers the resting dither and
    /// the event pulses together — they are one effect. Initialize before Render().
    /// </summary>
    public bool DitherEnabled { get; set; } = true;

    /// <summary>Fired with the new state when the dither toggle is clicked.</summary>
    public Action<bool>? OnDitherChanged { get; set; }

    /// <summary>
    /// Whether the window is borderless-fullscreen. Read back from <c>WindowMode</c> rather than
    /// from the saved setting, so the row shows the true live state after an F11 press — the key and
    /// this toggle are two ways to the same switch and must not disagree about where it is.
    /// </summary>
    public bool Fullscreen { get; set; }

    /// <summary>Fired with the new state when the fullscreen toggle is clicked.</summary>
    public Action<bool>? OnFullscreenChanged { get; set; }

    // ── Language model ───────────────────────────────────────────────────────
    //
    // Every row below takes effect at the NEXT LAUNCH, and the screen says so. The server loads
    // the model once at startup and holds it for the session; changing a device in place would
    // mean killing it, re-reading two gigabytes and rebuilding every cached persona slot, possibly
    // mid-narration. Deferring is both simpler and the honest thing to show the player.

    /// <summary>Compute device preference. Initialize before Render().</summary>
    public LlamaComputeDevice LlmDevice { get; set; } = LlamaComputeDevice.Auto;

    /// <summary>Fired with the new device when the device button is clicked.</summary>
    public Action<LlamaComputeDevice>? OnLlmDeviceChanged { get; set; }

    /// <summary>GPU layers to offload; -1 means let llama.cpp fit it. Initialize before Render().</summary>
    public int LlmGpuLayers { get; set; } = -1;

    /// <summary>Fired with the new layer count (-1 for automatic).</summary>
    public Action<int>? OnLlmGpuLayersChanged { get; set; }

    /// <summary>CPU threads; 0 means let llama.cpp choose. Initialize before Render().</summary>
    public int LlmCpuThreads { get; set; }

    /// <summary>Fired with the new thread count (0 for automatic).</summary>
    public Action<int>? OnLlmCpuThreadsChanged { get; set; }

    /// <summary>
    /// Fired when the player asks for hardware detection to run again. The handler is expected to
    /// discard the saved probe result, not to measure anything here — see <see cref="RequestRedetect"/>.
    /// </summary>
    public Action? OnLlmRedetect { get; set; }

    /// <summary>Set once re-detection has been asked for, so the button can acknowledge it.</summary>
    private bool _redetectRequested;

    /// <summary>Fired when the Back button is clicked.</summary>
    public Action? OnBack { get; set; }

    // Control indices (also used as hover ids).
    private const int CtlMusicMinus   = 0;
    private const int CtlMusicPlus    = 1;
    private const int CtlSfxMinus     = 2;
    private const int CtlSfxPlus      = 3;
    private const int CtlBack         = 4;
    private const int CtlDither       = 5;
    private const int CtlDevice       = 6;
    private const int CtlLayersMinus  = 7;
    private const int CtlLayersPlus   = 8;
    private const int CtlThreadsMinus = 9;
    private const int CtlThreadsPlus  = 10;
    private const int CtlRedetect     = 11;
    private const int CtlFullscreen   = 12;
    private int _hoveredControl = -1;

    private const int Step = 10; // percent per click

    /// <summary>
    /// Ceiling for the manual GPU-layer setting. No model has more layers than this, and the
    /// setting is an override for people who know what they are doing — the useful value is the
    /// automatic one at the bottom of the range.
    /// </summary>
    private const int MaxGpuLayers = 99;

    /// <summary>Ceiling for the manual thread count. Above the core count it only adds contention.</summary>
    private const int MaxCpuThreads = 64;

    // Layout. Two stacks with a heading each, sharing one column geometry so every label,
    // button and value lines up down the screen.
    private const int TitleRow         = 28;
    private const int AudioVideoHeadRow = 34;
    private const int MusicRow      = 37;
    private const int SfxRow        = 40;
    private const int DitherRow     = 43;
    private const int FullscreenRow = 46;
    private const int ModelHeadRow  = 51;
    private const int DeviceRow     = 54;
    private const int LayersRow     = 57;
    private const int ThreadsRow    = 60;
    private const int RedetectRow   = 63;
    private const int ModelInfoRow  = 66;   // and the two rows below it
    private const int BackRow       = 72;
    private const int BarWidth      = 20;
    private const int RowWidth      = 47; // total width of a volume row (see column math below)
    private const string BackLabel = "[ Back ]";
    // Widest of the two states, so the hit region does not change size with the label.
    private const int ToggleW = 7; // "[ OFF ]"
    // Widest of the two states, so the hit region does not change size with the label.
    private const int FullscreenW = 10; // "[ WINDOW ]"

    /// <summary>Widest device label ("[ Auto ]"), so the hit region does not move as it cycles.</summary>
    private const int DeviceW = 8;

    private const string RedetectLabel     = "[ Re-detect hardware ]";
    private const string RedetectDoneLabel = "[ Will re-detect at next launch ]";

    public SettingsMenuRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    /// <summary>Renders the full settings screen to the terminal.</summary>
    public void Render()
    {
        _terminal.Fill(' ', Config.Colors.Black, Config.Colors.Black);
        _terminal.Visible = true;

        const string ornament = "─ · ─ · ─ · ─ · ─ · ─ · ─ · ─";
        _terminal.CenteredText(TitleRow - 2, ornament, Config.Colors.DarkGray35, Config.Colors.Black);
        _terminal.CenteredText(TitleRow, "S E T T I N G S", Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.CenteredText(TitleRow + 2, ornament, Config.Colors.DarkGray35, Config.Colors.Black);

        // Not "AUDIO": the dither toggle below is a post-process shader, not a sound setting.
        DrawSectionHeading(AudioVideoHeadRow, "A U D I O   &   V I D E O");
        DrawVolumeRow(MusicRow, "MUSIC", MusicVolume, CtlMusicMinus, CtlMusicPlus);
        DrawVolumeRow(SfxRow, "SFX", SfxVolume, CtlSfxMinus, CtlSfxPlus);
        DrawDitherRow();
        DrawFullscreenRow();

        DrawSectionHeading(ModelHeadRow, "L A N G U A G E   M O D E L");
        DrawDeviceRow();
        DrawAutoStepRow(LayersRow, "LAYERS", LlmGpuLayers, -1, CtlLayersMinus, CtlLayersPlus, MaxGpuLayers);
        DrawAutoStepRow(ThreadsRow, "THREADS", LlmCpuThreads, 0, CtlThreadsMinus, CtlThreadsPlus, MaxCpuThreads);
        DrawRedetectButton();
        DrawModelInfo();

        DrawBackButton();

        // Edge rules against the sphere, drawn last so nothing overwrites them
        _terminal.DrawSideRails();
    }

    // ── Column geometry (shared by render + hit-testing) ─────────────────────
    // label(7) sp(1) [ - ](5) sp(2) bar(20) sp(1) pct(4) sp(2) [ + ](5) = 47
    private int RowStartX => (_terminal.Width - RowWidth) / 2;
    private int MinusX => RowStartX + 8;
    private int BarX   => RowStartX + 15;
    private int PctX   => RowStartX + 36;
    private int PlusX  => RowStartX + 42;
    private const int BtnW = 5; // "[ - ]" / "[ + ]"

    private int BackStartX => (_terminal.Width - BackLabel.Length) / 2;

    private void DrawVolumeRow(int row, string label, int value, int minusCtl, int plusCtl)
    {
        int startX = RowStartX;
        _terminal.FillRect(startX, row, RowWidth, 1, ' ', Config.Colors.White, Config.Colors.Black);

        _terminal.Text(startX, row, label.PadRight(7), Config.Colors.MediumGray60, Config.Colors.Black);

        DrawStepButton(MinusX, row, "[ - ]", minusCtl, value > 0);
        DrawStepButton(PlusX, row, "[ + ]", plusCtl, value < 100);

        // Bar: filled cells proportional to value.
        int filled = (int)MathF.Round(value / 100f * BarWidth);
        for (int i = 0; i < BarWidth; i++)
        {
            bool on = i < filled;
            _terminal.Text(BarX + i, row, "█",
                on ? Config.Colors.BrightYellow : Config.Colors.DarkGray35, Config.Colors.Black);
        }

        _terminal.Text(PctX, row, $"{value,3}%", Config.Colors.White, Config.Colors.Black);
    }

    /// <summary>
    /// Dither on/off. Laid out on the same columns as a volume row — label at the left,
    /// toggle where the [ - ] button sits — so the three rows read as one stack.
    /// </summary>
    private void DrawDitherRow()
    {
        int startX = RowStartX;
        _terminal.FillRect(startX, DitherRow, RowWidth, 1, ' ', Config.Colors.White, Config.Colors.Black);

        _terminal.Text(startX, DitherRow, "DITHER".PadRight(7), Config.Colors.MediumGray60, Config.Colors.Black);

        bool hovered = _hoveredControl == CtlDither;
        Vector4 textColor = hovered      ? Config.Colors.BrightYellow
                          : DitherEnabled ? Config.Colors.White
                          :                 Config.Colors.DarkGray35;
        Vector4 bgColor = hovered ? Config.Colors.DarkYellow : Config.Colors.Black;

        string label = (DitherEnabled ? "[ ON ]" : "[ OFF ]").PadRight(ToggleW);
        _terminal.Text(MinusX, DitherRow, label, textColor, bgColor);
    }

    /// <summary>
    /// Fullscreen on/off, on the dither row's columns. The label carries the key that does the same
    /// thing, because a player who finds fullscreen here should not have to come back to this screen
    /// to leave it.
    /// </summary>
    private void DrawFullscreenRow()
    {
        int startX = RowStartX;
        _terminal.FillRect(startX, FullscreenRow, RowWidth, 1, ' ', Config.Colors.White, Config.Colors.Black);

        _terminal.Text(startX, FullscreenRow, "SCREEN".PadRight(7), Config.Colors.MediumGray60, Config.Colors.Black);

        bool hovered = _hoveredControl == CtlFullscreen;
        Vector4 textColor = hovered    ? Config.Colors.BrightYellow
                          : Fullscreen ? Config.Colors.White
                          :              Config.Colors.DarkGray35;
        Vector4 bgColor = hovered ? Config.Colors.DarkYellow : Config.Colors.Black;

        string label = (Fullscreen ? "[ FULL ]" : "[ WINDOW ]").PadRight(FullscreenW);
        _terminal.Text(MinusX, FullscreenRow, label, textColor, bgColor);

        _terminal.Text(MinusX + FullscreenW + 1, FullscreenRow, "F11",
            Config.Colors.DarkGray35, Config.Colors.Black);
    }

    private void DrawSectionHeading(int row, string text)
        => _terminal.CenteredText(row, text, Config.Colors.MediumGray60, Config.Colors.Black);

    /// <summary>
    /// The compute-device cycle: Auto → GPU → CPU. Laid out on the volume rows' columns, with the
    /// note to the right saying what Auto currently resolves to — a bare "Auto" tells the player
    /// nothing about whether their machine ended up on a GPU.
    /// </summary>
    private void DrawDeviceRow()
    {
        int startX = RowStartX;
        _terminal.FillRect(startX, DeviceRow, RowWidth, 1, ' ', Config.Colors.White, Config.Colors.Black);
        _terminal.Text(startX, DeviceRow, "DEVICE".PadRight(7), Config.Colors.MediumGray60, Config.Colors.Black);

        bool hovered = _hoveredControl == CtlDevice;
        Vector4 textColor = hovered ? Config.Colors.BrightYellow : Config.Colors.White;
        Vector4 bgColor   = hovered ? Config.Colors.DarkYellow   : Config.Colors.Black;

        string label = LlmDevice switch
        {
            LlamaComputeDevice.Gpu => "[ GPU ]",
            LlamaComputeDevice.Cpu => "[ CPU ]",
            _                      => "[ Auto ]"
        };
        _terminal.Text(MinusX, DeviceRow, label.PadRight(DeviceW), textColor, bgColor);

        string note = LlmDevice == LlamaComputeDevice.Auto
            ? $"using {DescribeEffectiveDevice()}"
            : "overriding detection";
        _terminal.Text(MinusX + DeviceW + 2, DeviceRow, Truncate(note, RowWidth - 8 - DeviceW - 2),
            Config.Colors.DarkGray35, Config.Colors.Black);
    }

    /// <summary>
    /// A stepper whose bottom value means "let llama.cpp decide" rather than zero. Both settings
    /// it serves are overrides: llama.cpp fits the GPU layers to available memory and picks a
    /// thread count from the core count, and both of those are better than a number the player
    /// guesses. So the automatic end is the default and is labelled, not shown as a bare 0 or -1.
    /// </summary>
    private void DrawAutoStepRow(int row, string label, int value, int autoValue, int minusCtl, int plusCtl, int max)
    {
        int startX = RowStartX;
        _terminal.FillRect(startX, row, RowWidth, 1, ' ', Config.Colors.White, Config.Colors.Black);
        _terminal.Text(startX, row, label.PadRight(7), Config.Colors.MediumGray60, Config.Colors.Black);

        bool isAuto = value <= autoValue;
        DrawStepButton(MinusX, row, "[ - ]", minusCtl, !isAuto);
        DrawStepButton(PlusX, row, "[ + ]", plusCtl, value < max);

        string text = isAuto ? "automatic" : value.ToString();
        _terminal.Text(BarX, row, text.PadRight(BarWidth),
            isAuto ? Config.Colors.DarkGray35 : Config.Colors.White, Config.Colors.Black);
    }

    private void DrawRedetectButton()
    {
        string label = _redetectRequested ? RedetectDoneLabel : RedetectLabel;
        int x = (_terminal.Width - label.Length) / 2;

        // Once asked for, it is done — acknowledge rather than invite a second click.
        bool hovered = !_redetectRequested && _hoveredControl == CtlRedetect;
        Vector4 textColor = _redetectRequested ? Config.Colors.DarkGray35
                          : hovered            ? Config.Colors.BrightYellow
                          :                      Config.Colors.White;
        Vector4 bgColor = hovered ? Config.Colors.DarkYellow : Config.Colors.Black;

        _terminal.FillRect(0, RedetectRow, _terminal.Width, 1, ' ', textColor, Config.Colors.Black);
        _terminal.Text(x, RedetectRow, label, textColor, bgColor);
    }

    /// <summary>
    /// Two lines of context under the model rows: what detection found, and when any of this takes
    /// effect.
    /// <para>The model itself is deliberately <b>not</b> named here. Which language model the game
    /// runs on is an implementation detail of the fiction, not a setting, and the row would put a
    /// vendor's model name in front of the player to no purpose. It is still recovered from the
    /// GGUF header and written to the startup log, which is where a person diagnosing an install
    /// needs it.</para>
    /// </summary>
    private void DrawModelInfo()
    {
        var summary = UserSettings.LlmProbeSummary;
        _terminal.CenteredText(ModelInfoRow,
            Truncate(summary.Length > 0 ? $"detected: {summary}" : "detected: not yet — runs at next launch", _terminal.Width - 4),
            Config.Colors.DarkGray35, Config.Colors.Black);

        _terminal.CenteredText(ModelInfoRow + 1, "changes above take effect at the next launch",
            Config.Colors.DarkGray35, Config.Colors.Black);
    }

    /// <summary>What Auto resolves to, in the player's words rather than the enum's.</summary>
    private static string DescribeEffectiveDevice()
        => UserSettings.LlmProbedDevice == LlamaComputeDevice.Gpu
            ? UserSettings.LlmProbedBackend ?? "GPU"
            : "CPU";

    private static string Truncate(string text, int width)
        => width <= 1 || text.Length <= width ? text : text[..Math.Max(0, width - 1)] + "…";

    private void DrawStepButton(int x, int row, string text, int ctl, bool enabled)
    {
        Vector4 textColor, bgColor;
        if (!enabled)
        {
            textColor = Config.Colors.DarkGray35; bgColor = Config.Colors.Black;
        }
        else if (_hoveredControl == ctl)
        {
            textColor = Config.Colors.BrightYellow; bgColor = Config.Colors.DarkYellow;
        }
        else
        {
            textColor = Config.Colors.White; bgColor = Config.Colors.Black;
        }
        _terminal.Text(x, row, text, textColor, bgColor);
    }

    private void DrawBackButton()
    {
        bool hovered = _hoveredControl == CtlBack;
        Vector4 textColor = hovered ? Config.Colors.BrightYellow : Config.Colors.White;
        Vector4 bgColor   = hovered ? Config.Colors.DarkYellow   : Config.Colors.Black;
        _terminal.FillRect(BackStartX, BackRow, BackLabel.Length, 1, ' ', textColor, bgColor);
        _terminal.Text(BackStartX, BackRow, BackLabel, textColor, bgColor);
    }

    // ── Input ────────────────────────────────────────────────────────────────

    /// <summary>Updates hover state and redraws if it changed.</summary>
    public void OnMouseMove(int x, int y)
    {
        int newHovered = GetControlAtPosition(x, y);
        if (newHovered != _hoveredControl)
        {
            _hoveredControl = newHovered;
            Render();
        }
    }

    /// <summary>Handles a click: steps a volume or invokes Back.</summary>
    public void OnMouseClick(int x, int y)
    {
        switch (GetControlAtPosition(x, y))
        {
            case CtlMusicMinus: SetMusic(MusicVolume - Step); break;
            case CtlMusicPlus:  SetMusic(MusicVolume + Step); break;
            case CtlSfxMinus:   SetSfx(SfxVolume - Step); break;
            case CtlSfxPlus:    SetSfx(SfxVolume + Step); break;
            case CtlDither:     ToggleDither(); break;
            case CtlFullscreen: ToggleFullscreen(); break;
            case CtlDevice:     CycleDevice(); break;
            case CtlLayersMinus:  SetGpuLayers(LlmGpuLayers < 0 ? -1 : LlmGpuLayers - LayerStep); break;
            case CtlLayersPlus:   SetGpuLayers(LlmGpuLayers < 0 ? LayerStep : LlmGpuLayers + LayerStep); break;
            case CtlThreadsMinus: SetCpuThreads(LlmCpuThreads - 1); break;
            case CtlThreadsPlus:  SetCpuThreads(LlmCpuThreads + 1); break;
            case CtlRedetect:     RequestRedetect(); break;
            case CtlBack:       OnBack?.Invoke(); break;
        }
    }

    /// <summary>
    /// Layers move four at a time. Unlike a thread count, which is a small number a player picks
    /// exactly, this is a fraction of a model's depth — reaching a useful value one click at a
    /// time would take dozens of presses.
    /// </summary>
    private const int LayerStep = 4;

    private void CycleDevice()
    {
        LlmDevice = LlmDevice switch
        {
            LlamaComputeDevice.Auto => LlamaComputeDevice.Gpu,
            LlamaComputeDevice.Gpu  => LlamaComputeDevice.Cpu,
            _                       => LlamaComputeDevice.Auto
        };
        OnLlmDeviceChanged?.Invoke(LlmDevice);
        Render();
    }

    private void SetGpuLayers(int v)
    {
        // Anything below the step floor collapses to automatic, so the bottom of the range is the
        // setting worth having rather than an unusable "0 layers on the GPU".
        int clamped = v < LayerStep ? -1 : Math.Min(v, MaxGpuLayers);
        if (clamped == LlmGpuLayers) return;
        LlmGpuLayers = clamped;
        OnLlmGpuLayersChanged?.Invoke(clamped);
        Render();
    }

    private void SetCpuThreads(int v)
    {
        int clamped = Math.Clamp(v, 0, MaxCpuThreads);
        if (clamped == LlmCpuThreads) return;
        LlmCpuThreads = clamped;
        OnLlmCpuThreadsChanged?.Invoke(clamped);
        Render();
    }

    /// <summary>
    /// Asks for hardware detection to run again. Nothing is measured here: the handler discards the
    /// saved result, and the probe re-runs during the next launch's model load, where it is already
    /// behind a loading screen. Measuring on the spot would freeze the game for as long as a
    /// benchmark takes, to produce a setting that does not apply until the next launch anyway.
    /// </summary>
    private void RequestRedetect()
    {
        if (_redetectRequested) return;
        _redetectRequested = true;
        OnLlmRedetect?.Invoke();
        Render();
    }

    private void ToggleDither()
    {
        DitherEnabled = !DitherEnabled;
        OnDitherChanged?.Invoke(DitherEnabled);
        Render();
    }

    private void ToggleFullscreen()
    {
        Fullscreen = !Fullscreen;
        OnFullscreenChanged?.Invoke(Fullscreen);
        Render();
    }

    private void SetMusic(int v)
    {
        int clamped = Math.Clamp(v, 0, 100);
        if (clamped == MusicVolume) return;
        MusicVolume = clamped;
        OnMusicVolumeChanged?.Invoke(clamped);
        Render();
    }

    private void SetSfx(int v)
    {
        int clamped = Math.Clamp(v, 0, 100);
        if (clamped == SfxVolume) return;
        SfxVolume = clamped;
        OnSfxVolumeChanged?.Invoke(clamped);
        Render();
    }

    /// <summary>Returns the control index under (x, y), or -1.</summary>
    public int GetControlAtPosition(int x, int y)
    {
        if (y == MusicRow)
        {
            if (x >= MinusX && x < MinusX + BtnW) return CtlMusicMinus;
            if (x >= PlusX  && x < PlusX + BtnW)  return CtlMusicPlus;
        }
        else if (y == SfxRow)
        {
            if (x >= MinusX && x < MinusX + BtnW) return CtlSfxMinus;
            if (x >= PlusX  && x < PlusX + BtnW)  return CtlSfxPlus;
        }
        else if (y == DitherRow)
        {
            if (x >= MinusX && x < MinusX + ToggleW) return CtlDither;
        }
        else if (y == FullscreenRow)
        {
            if (x >= MinusX && x < MinusX + FullscreenW) return CtlFullscreen;
        }
        else if (y == DeviceRow)
        {
            if (x >= MinusX && x < MinusX + DeviceW) return CtlDevice;
        }
        else if (y == LayersRow)
        {
            if (x >= MinusX && x < MinusX + BtnW) return CtlLayersMinus;
            if (x >= PlusX  && x < PlusX + BtnW)  return CtlLayersPlus;
        }
        else if (y == ThreadsRow)
        {
            if (x >= MinusX && x < MinusX + BtnW) return CtlThreadsMinus;
            if (x >= PlusX  && x < PlusX + BtnW)  return CtlThreadsPlus;
        }
        else if (y == RedetectRow && !_redetectRequested)
        {
            // Hit-tested against the un-clicked label: once requested the button is inert, and its
            // acknowledgement text is wider, which would otherwise leave a dead region that still
            // reports hover.
            int start = (_terminal.Width - RedetectLabel.Length) / 2;
            if (x >= start && x < start + RedetectLabel.Length) return CtlRedetect;
        }
        else if (y == BackRow)
        {
            if (x >= BackStartX && x < BackStartX + BackLabel.Length) return CtlBack;
        }
        return -1;
    }

    /// <summary>Stable hover id for the controller's hover-tick logic, or null.</summary>
    public string? GetHoveredControlId(int x, int y) => GetControlAtPosition(x, y) switch
    {
        CtlMusicMinus => "settings:music-minus",
        CtlMusicPlus  => "settings:music-plus",
        CtlSfxMinus   => "settings:sfx-minus",
        CtlSfxPlus    => "settings:sfx-plus",
        CtlDither     => "settings:dither",
        CtlFullscreen => "settings:fullscreen",
        CtlDevice        => "settings:llm-device",
        CtlLayersMinus   => "settings:llm-layers-minus",
        CtlLayersPlus    => "settings:llm-layers-plus",
        CtlThreadsMinus  => "settings:llm-threads-minus",
        CtlThreadsPlus   => "settings:llm-threads-plus",
        CtlRedetect      => "settings:llm-redetect",
        CtlBack       => "settings:back",
        _             => null,
    };
}
