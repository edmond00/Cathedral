using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.LLM;

namespace Cathedral.Game;

/// <summary>
/// Renders and manages the Settings screen on the TerminalHUD. Reached from the main menu; active
/// only during <see cref="GameMode.Settings"/>.
///
/// <para>Three groups, each under a heading, sharing one column geometry so every label, button and
/// value lines up down the screen:</para>
/// <list type="bullet">
/// <item><b>Audio</b> — music and SFX volumes as [ - ] / [ + ] steppers.</item>
/// <item><b>Video</b> — fullscreen, dither, and the two glyph rows. This was one "Audio &amp; video"
/// group holding dither as its only non-sound row; the glyph rows made that heading a list of
/// unrelated concerns, and the split also puts a boundary between the settings that apply at once
/// and the model rows below that do not — which the "next launch" note under them relies on being
/// read as covering that group and not the whole screen.</item>
/// <item><b>Language model</b> — compute device, GPU layers, thread count and re-detect. Unlike
/// everything above, none of these apply until the next launch; see the row comments.</item>
/// </list>
///
/// <para>The two glyph rows are live: a click is visible on this screen's own text before the
/// button is released, which is most of why they are worth having as settings rather than as
/// constants. What they cannot show is running lowercase prose at a small cell size, which is what
/// actually breaks — the player has to go back to a narration screen for that.</para>
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

    /// <summary>
    /// Index into <see cref="Config.Terminal.GlyphWeightSteps"/>. Initialize before Render() —
    /// from <c>Config.Terminal.GlyphWeightStep</c> rather than from the saved setting, which is the
    /// live truth for the same reason the dither toggle reads back from the renderer.
    /// </summary>
    public int GlyphWeight { get; set; } = Config.Terminal.GlyphWeightDefaultStep;

    /// <summary>Fired with the new step when a weight button is clicked.</summary>
    public Action<int>? OnGlyphWeightChanged { get; set; }

    /// <summary>Glyph size as a fraction of the cell. Initialize before Render().</summary>
    public float GlyphScale { get; set; } = Config.Terminal.GlyphScaleDefault;

    /// <summary>Fired with the new scale when a size button is clicked.</summary>
    public Action<float>? OnGlyphScaleChanged { get; set; }

    // ── World glyphs ─────────────────────────────────────────────────────────
    //
    // The same two questions asked of the sphere, and they are separate settings rather than one
    // pair applied twice because the two pipelines answer them differently: a terminal glyph is
    // bounded by its cell and blended, a world glyph is bounded by its neighbours and thresholded.
    // A player wanting a legible world map is usually not asking for larger UI text either.

    /// <summary>
    /// Index into <see cref="Config.GlyphSphere.WorldGlyphWeightSteps"/>. Initialize before
    /// Render() — from <c>Config.GlyphSphere.WorldGlyphWeightStep</c>, the live truth, for the same
    /// reason the UI weight row reads back from Config rather than from the saved setting.
    /// </summary>
    public int WorldGlyphWeight { get; set; } = Config.GlyphSphere.WorldGlyphWeightDefaultStep;

    /// <summary>Fired with the new step when a world weight button is clicked.</summary>
    public Action<int>? OnWorldGlyphWeightChanged { get; set; }

    /// <summary>Multiplier on every world glyph quad. Initialize before Render().</summary>
    public float WorldGlyphScale { get; set; } = Config.GlyphSphere.WorldGlyphScaleDefault;

    /// <summary>Fired with the new scale when a world size button is clicked.</summary>
    public Action<float>? OnWorldGlyphScaleChanged { get; set; }

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
    private const int CtlWeightMinus  = 13;
    private const int CtlWeightPlus   = 14;
    private const int CtlSizeMinus    = 15;
    private const int CtlSizePlus     = 16;
    private const int CtlWorldSizeMinus   = 17;
    private const int CtlWorldSizePlus    = 18;
    private const int CtlWorldWeightMinus = 19;
    private const int CtlWorldWeightPlus  = 20;
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

    // Layout. Three stacks with a heading each, sharing one column geometry so every label,
    // button and value lines up down the screen. Rows are three apart within a group and five
    // between groups, so the grouping reads without needing a rule drawn between them.
    //
    // The stack runs 26 (the title's ornament) to 84 against a 100-row terminal. That is most of
    // the height it has: one more group wants two columns or a scroll, not another five rows.
    private const int TitleRow      = 28;
    private const int AudioHeadRow  = 34;
    private const int MusicRow      = 37;
    private const int SfxRow        = 40;
    private const int VideoHeadRow  = 45;
    private const int FullscreenRow = 48;
    private const int DitherRow     = 51;
    // The four glyph settings are a 2x2 block rather than four rows: SIZE and WEIGHT down the
    // side, UI and WORLD across. Two rows instead of four, and the pairing is the point — the
    // question a player has is "is this about the text or about the world?", which a column
    // heading answers once for both rows and a stack of four prefixed labels never quite does.
    private const int GlyphHeadRow  = 54;   // the U I / W O R L D column headings
    private const int SizeRow       = 56;
    private const int WeightRow     = 58;
    private const int GlyphNoteRow  = 60;   // what a dimmed value means
    private const int ModelHeadRow  = 64;
    private const int DeviceRow     = 67;
    private const int LayersRow     = 70;
    private const int ThreadsRow    = 73;
    private const int RedetectRow   = 76;
    private const int ModelInfoRow  = 79;   // and the row below it
    private const int BackRow       = 84;
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

        DrawSectionHeading(AudioHeadRow, "A U D I O");
        DrawVolumeRow(MusicRow, "MUSIC", MusicVolume, CtlMusicMinus, CtlMusicPlus);
        DrawVolumeRow(SfxRow, "SFX", SfxVolume, CtlSfxMinus, CtlSfxPlus);

        // Coarsest lever first: fullscreen changes the cell size the two glyph rows are tuned
        // against, so a player who is going to change it should meet it before them.
        DrawSectionHeading(VideoHeadRow, "V I D E O");
        DrawFullscreenRow();
        DrawDitherRow();
        DrawGlyphBlock();

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

    // ── The glyph block: SIZE and WEIGHT × UI and WORLD ──────────────────────
    //
    // Four steppers on two rows. Each half is
    //     label(6) sp [ - ](5) sp value(7) sp [ + ](5)  =  26
    // and the two halves sit GlyphColGap apart, so the block is wider than the 47-column rows
    // above it but still centred on the same axis.
    private const int GlyphHalfW  = 26;
    private const int GlyphColGap = 6;
    private const int GlyphBlockW = GlyphHalfW * 2 + GlyphColGap;

    private int GlyphLeftX  => (_terminal.Width - GlyphBlockW) / 2;
    private int GlyphRightX => GlyphLeftX + GlyphHalfW + GlyphColGap;

    // Offsets within a half.
    private const int GlyphMinusDx = 7;
    private const int GlyphValueDx = 13;
    private const int GlyphPlusDx  = 21;

    /// <summary>
    /// The two glyph rows, each split between the UI and the world.
    ///
    /// <para><b>Why one block rather than four rows.</b> SIZE and WEIGHT are the same two questions
    /// asked twice, and the thing a player needs to know first is which of the two surfaces they
    /// are about to change. A column heading answers that once for both rows; four stacked rows
    /// with prefixed labels ("UI SIZE", "WORLD SIZE"…) make the reader parse the same distinction
    /// four times and cost twice the height on a stack that is already most of the screen.</para>
    ///
    /// <para>The pairing within a row is the same argument
    /// <see cref="Config.Terminal.GlyphWeightSteps"/> makes about a weight step being two constants:
    /// a player cannot say in advance which surface needs the change, so both are put in front of
    /// them side by side rather than one being buried.</para>
    /// </summary>
    private void DrawGlyphBlock()
    {
        DrawColumnHeading(GlyphLeftX,  "U I");
        DrawColumnHeading(GlyphRightX, "W O R L D");

        DrawStepperHalf(GlyphLeftX, SizeRow, "SIZE", $"{GlyphScale * 100f:0}%",
            CtlSizeMinus, CtlSizePlus,
            canDecrease: GlyphScale > Config.Terminal.GlyphScaleMin + ScaleEpsilon,
            canIncrease: GlyphScale < Config.Terminal.GlyphScaleMax - ScaleEpsilon,
            isDefault: MathF.Abs(GlyphScale - Config.Terminal.GlyphScaleDefault) < ScaleEpsilon);

        DrawStepperHalf(GlyphLeftX, WeightRow, "WEIGHT",
            Config.Terminal.GlyphWeightSteps[GlyphWeight].Label,
            CtlWeightMinus, CtlWeightPlus,
            canDecrease: GlyphWeight > 0,
            canIncrease: GlyphWeight < Config.Terminal.GlyphWeightSteps.Length - 1,
            isDefault: GlyphWeight == Config.Terminal.GlyphWeightDefaultStep);

        DrawStepperHalf(GlyphRightX, SizeRow, "SIZE", $"{WorldGlyphScale * 100f:0}%",
            CtlWorldSizeMinus, CtlWorldSizePlus,
            canDecrease: WorldGlyphScale > Config.GlyphSphere.WorldGlyphScaleMin + ScaleEpsilon,
            canIncrease: WorldGlyphScale < Config.GlyphSphere.WorldGlyphScaleMax - ScaleEpsilon,
            isDefault: MathF.Abs(WorldGlyphScale - Config.GlyphSphere.WorldGlyphScaleDefault) < ScaleEpsilon);

        DrawStepperHalf(GlyphRightX, WeightRow, "WEIGHT",
            Config.GlyphSphere.WorldGlyphWeightSteps[WorldGlyphWeight].Label,
            CtlWorldWeightMinus, CtlWorldWeightPlus,
            canDecrease: WorldGlyphWeight > 0,
            canIncrease: WorldGlyphWeight < Config.GlyphSphere.WorldGlyphWeightSteps.Length - 1,
            isDefault: WorldGlyphWeight == Config.GlyphSphere.WorldGlyphWeightDefaultStep);

        // The default marker moved from an "(default)" tag beside each value to the value's own
        // colour, because four of them would not fit beside four steppers — and one legend for the
        // block says the same thing in a quarter of the room. It still has to be said: every one of
        // these is a matter of taste with no right answer, so a player who has wandered off the
        // shipped value needs a way to recognise it again.
        _terminal.CenteredText(GlyphNoteRow, "a dimmed value is the shipped default",
            Config.Colors.DarkGray35, Config.Colors.Black);
    }

    /// <summary>A heading centred over one half of the glyph block.</summary>
    private void DrawColumnHeading(int x, string text)
        => _terminal.Text(x + (GlyphHalfW - text.Length) / 2, GlyphHeadRow, text,
            Config.Colors.MediumGray60, Config.Colors.Black);

    /// <summary>
    /// One stepper occupying half the glyph block: label, [ - ], value, [ + ].
    ///
    /// <para>The value is drawn dim when it is the shipped default and bright when it is not, which
    /// is the whole of the default marker — see the legend drawn under the block.</para>
    /// </summary>
    private void DrawStepperHalf(int x, int row, string label, string valueText,
        int minusCtl, int plusCtl, bool canDecrease, bool canIncrease, bool isDefault)
    {
        _terminal.FillRect(x, row, GlyphHalfW, 1, ' ', Config.Colors.White, Config.Colors.Black);
        _terminal.Text(x, row, label.PadRight(6), Config.Colors.MediumGray60, Config.Colors.Black);

        DrawStepButton(x + GlyphMinusDx, row, "[ - ]", minusCtl, canDecrease);
        DrawStepButton(x + GlyphPlusDx,  row, "[ + ]", plusCtl,  canIncrease);

        _terminal.Text(x + GlyphValueDx, row, valueText.PadRight(7),
            isDefault ? Config.Colors.DarkGray35 : Config.Colors.White, Config.Colors.Black);
    }

    /// <summary>
    /// Tolerance for comparing a glyph scale against a bound. The value is stepped by 0.05 and
    /// compared against ends it is expected to land exactly on, which is not something float
    /// arithmetic promises — without this the [ + ] button greys out one step early, or never.
    /// </summary>
    private const float ScaleEpsilon = 0.001f;

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
            case CtlWeightMinus: SetGlyphWeight(GlyphWeight - 1); break;
            case CtlWeightPlus:  SetGlyphWeight(GlyphWeight + 1); break;
            case CtlSizeMinus:   SetGlyphScale(GlyphScale - Config.Terminal.GlyphScaleStep); break;
            case CtlSizePlus:    SetGlyphScale(GlyphScale + Config.Terminal.GlyphScaleStep); break;
            case CtlWorldWeightMinus: SetWorldGlyphWeight(WorldGlyphWeight - 1); break;
            case CtlWorldWeightPlus:  SetWorldGlyphWeight(WorldGlyphWeight + 1); break;
            case CtlWorldSizeMinus:   SetWorldGlyphScale(WorldGlyphScale - Config.GlyphSphere.WorldGlyphScaleStep); break;
            case CtlWorldSizePlus:    SetWorldGlyphScale(WorldGlyphScale + Config.GlyphSphere.WorldGlyphScaleStep); break;
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

    private void SetGlyphWeight(int step)
    {
        int clamped = Math.Clamp(step, 0, Config.Terminal.GlyphWeightSteps.Length - 1);
        if (clamped == GlyphWeight) return;
        GlyphWeight = clamped;
        // The handler applies it and re-rasters the atlas; by the time Render() below draws this
        // screen it is already drawing it at the new weight.
        OnGlyphWeightChanged?.Invoke(clamped);
        Render();
    }

    private void SetGlyphScale(float value)
    {
        float clamped = Math.Clamp(value, Config.Terminal.GlyphScaleMin, Config.Terminal.GlyphScaleMax);

        // Snap to the step grid. Repeatedly adding 0.05f drifts, and the row prints a rounded
        // percentage — so drift shows first as a button that visibly changes nothing.
        clamped = MathF.Round(clamped / Config.Terminal.GlyphScaleStep) * Config.Terminal.GlyphScaleStep;

        if (MathF.Abs(clamped - GlyphScale) < ScaleEpsilon) return;
        GlyphScale = clamped;
        OnGlyphScaleChanged?.Invoke(clamped);
        Render();
    }

    /// <summary>
    /// The world's weight step. Unlike its UI counterpart the change is not visible on this screen
    /// — the sphere is behind it — so the player is expected to go back and look. That is also why
    /// the handler must rebuild the sphere atlas rather than leaving it to the next rebuild: there
    /// is no repaint here that would pick it up.
    /// </summary>
    private void SetWorldGlyphWeight(int step)
    {
        int clamped = Math.Clamp(step, 0, Config.GlyphSphere.WorldGlyphWeightSteps.Length - 1);
        if (clamped == WorldGlyphWeight) return;
        WorldGlyphWeight = clamped;
        OnWorldGlyphWeightChanged?.Invoke(clamped);
        Render();
    }

    private void SetWorldGlyphScale(float value)
    {
        float clamped = Math.Clamp(value,
            Config.GlyphSphere.WorldGlyphScaleMin, Config.GlyphSphere.WorldGlyphScaleMax);

        // Snapped to the step grid for the reason the UI scale is: repeated float addition drifts,
        // and the row prints a rounded percentage, so drift shows up as a button doing nothing.
        clamped = MathF.Round(clamped / Config.GlyphSphere.WorldGlyphScaleStep)
                * Config.GlyphSphere.WorldGlyphScaleStep;

        if (MathF.Abs(clamped - WorldGlyphScale) < ScaleEpsilon) return;
        WorldGlyphScale = clamped;
        OnWorldGlyphScaleChanged?.Invoke(clamped);
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

    /// <summary>
    /// The [ - ] / [ + ] of one half of the glyph block, or -1 when the cursor is elsewhere on the
    /// row. Both halves of a row are tested against this in turn, so the two columns cannot drift
    /// apart from the offsets <see cref="DrawStepperHalf"/> draws them at.
    /// </summary>
    private static int GlyphHalfHit(int x, int halfX, int minusCtl, int plusCtl)
    {
        if (x >= halfX + GlyphMinusDx && x < halfX + GlyphMinusDx + BtnW) return minusCtl;
        if (x >= halfX + GlyphPlusDx  && x < halfX + GlyphPlusDx + BtnW)  return plusCtl;
        return -1;
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
        else if (y == SizeRow)
        {
            int hit = GlyphHalfHit(x, GlyphLeftX, CtlSizeMinus, CtlSizePlus);
            if (hit >= 0) return hit;
            return GlyphHalfHit(x, GlyphRightX, CtlWorldSizeMinus, CtlWorldSizePlus);
        }
        else if (y == WeightRow)
        {
            int hit = GlyphHalfHit(x, GlyphLeftX, CtlWeightMinus, CtlWeightPlus);
            if (hit >= 0) return hit;
            return GlyphHalfHit(x, GlyphRightX, CtlWorldWeightMinus, CtlWorldWeightPlus);
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
        CtlWeightMinus => "settings:glyph-weight-minus",
        CtlWeightPlus  => "settings:glyph-weight-plus",
        CtlSizeMinus   => "settings:glyph-size-minus",
        CtlSizePlus    => "settings:glyph-size-plus",
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
