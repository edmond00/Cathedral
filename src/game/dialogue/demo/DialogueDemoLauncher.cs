using System;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Cathedral.Game.Narrative;
using Cathedral.LLM;
using Cathedral.Terminal;

namespace Cathedral.Game.Dialogue.Demo;

/// <summary>
/// Standalone window for the dialogue demo — a simple 100×40 terminal that runs the
/// full greeting → replica → dice-roll → response loop with the InnKeeper NPC.
/// Launch via: dotnet run -- --dialogue
/// </summary>
public static class DialogueDemoLauncher
{
    public static void Launch()
    {
        Console.WriteLine("=== Cathedral – Dialogue System Demo ===\n");
        Console.WriteLine("Starting InnKeeper conversation test...");
        Console.WriteLine("• LLM server will be started automatically.");
        Console.WriteLine("• Press ESC in the window to quit.\n");

        var native = new NativeWindowSettings
        {
            ClientSize  = new Vector2i(
                Config.Terminal.MainWidth  * Config.Terminal.MainCellSize,
                Config.Terminal.MainHeight * Config.Terminal.MainCellSize),
            Title       = "Cathedral – Dialogue Demo",
            Flags       = ContextFlags.Default,
            API         = ContextAPI.OpenGL,
            APIVersion  = new Version(3, 3),
            WindowBorder = WindowBorder.Resizable
        };

        using var window = new DialogueDemoWindow(GameWindowSettings.Default, native);
        window.Run();
    }

    // ────────────────────────────────────────────────────────────────────────
    private sealed class DialogueDemoWindow : GameWindow
    {
        private TerminalHUD? _terminal;

        // LLM
        private LlamaServerManager? _llmManager;
        private ModusMentisSlotManager? _slotManager;

        // Dialogue
        private NpcInstance?            _npc;
        private Protagonist?            _protagonist;
        private DialogueModeController? _controller;

        // Boot state
        private bool _llmReady      = false;
        private bool _setupStarted  = false;
        private bool _setupComplete = false;
        private string _statusLine  = "Waiting for LLM server…";

        public DialogueDemoWindow(GameWindowSettings gs, NativeWindowSettings ns)
            : base(gs, ns) { }

        // ── OpenGL setup ─────────────────────────────────────────────────────

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _terminal = new TerminalHUD(
                Config.Terminal.MainWidth,
                Config.Terminal.MainHeight,
                Config.Terminal.MainCellSize,
                Config.Terminal.MainFontSize);

            _terminal.CellClicked += OnCellClicked;
            _terminal.CellHovered += OnCellHovered;

            // Start LLM server in background; dialogue begins in CoreReady callback
            StartLlmServerAsync();

            ShowBootMessage("Starting LLM server…");
        }

        // ── Frame loop ────────────────────────────────────────────────────────

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }

            if (_controller != null)
            {
                if (_controller.HasRequestedExit)
                {
                    Close();
                    return;
                }
                _controller.Update();
            }
            else if (!_setupComplete && _llmReady && !_setupStarted)
            {
                _setupStarted = true;
                _ = Task.Run(SetupDialogueAsync);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            _terminal?.Render(new Vector2i(ClientSize.X, ClientSize.Y));
            SwapBuffers();
        }

        // ── Input ─────────────────────────────────────────────────────────────

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            _terminal?.HandleMouseMove(MousePosition, ClientSize);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            _terminal?.HandleMouseDown(MousePosition, ClientSize, e.Button);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _controller?.OnMouseWheel((int)e.OffsetY);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            _controller?.OnKeyPress(e.Key);
        }

        private void OnCellClicked(int x, int y) => _controller?.OnMouseClick(x, y);
        private void OnCellHovered(int x, int y) => _controller?.OnMouseMove(x, y);

        // ── LLM server startup ────────────────────────────────────────────────

        private void StartLlmServerAsync()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    LLMLogger.Initialize();
                    _llmManager = new LlamaServerManager();

                    _llmManager.ServerReady += (_, e) =>
                    {
                        _llmReady = e.IsReady;
                        _statusLine = e.IsReady
                            ? "LLM server ready — setting up dialogue…"
                            : $"LLM server error: {e.Message}";
                        ShowBootMessage(_statusLine);
                    };

                    await _llmManager.StartServerAsync(modelAlias: null);
                }
                catch (Exception ex)
                {
                    _statusLine = $"LLM startup failed: {ex.Message}";
                    ShowBootMessage(_statusLine);
                    Console.WriteLine($"✗ {_statusLine}");
                }
            });
        }

        // ── Dialogue setup (runs once after LLM is ready) ─────────────────────

        private async Task SetupDialogueAsync()
        {
            try
            {
                ShowBootMessage("Initialising protagonist…");
                _protagonist = new Protagonist();
                _protagonist.InitializeModiMentis(ModusMentisRegistry.Instance, modusMentisCount: 50);

                int maxReplicas = GetTongueStat(_protagonist);
                int visage      = GetVisageStat(_protagonist);

                ShowBootMessage("Creating NPC…");
                var personaFactory  = new InnKeeperPersonaFactory();
                var graphFactory    = new InnKeeperConversationGraph();
                var persona         = personaFactory.CreatePersona();
                var conversationRoot = graphFactory.CreateGraph();

                _npc = new NpcInstance(
                    npcId:            "innkeeper_demo",
                    persona:          persona,
                    conversationRoot: conversationRoot,
                    initialAffinity:  visage);

                ShowBootMessage("Acquiring NPC LLM slot…");
                _npc.LlmSlotId = await _llmManager!.CreateInstanceAsync(persona.PersonaPrompt);

                ShowBootMessage("Initialising slot manager…");
                _slotManager = new ModusMentisSlotManager(_llmManager);

                ShowBootMessage("Starting dialogue…");
                _controller = new DialogueModeController(
                    npc:         _npc,
                    protagonist: _protagonist,
                    maxReplicas: maxReplicas,
                    llmManager:  _llmManager,
                    slotManager: _slotManager,
                    terminal:    _terminal!);

                _setupComplete = true;
                _controller.Start();
            }
            catch (Exception ex)
            {
                _statusLine = $"Setup error: {ex.Message}";
                ShowBootMessage(_statusLine);
                Console.WriteLine($"✗ Dialogue setup failed: {ex}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static int GetTongueStat(Protagonist p)
        {
            var stat = p.DerivedStats.FirstOrDefault(s => s.Name == "tongue");
            return stat?.GetValue(p) ?? 3;
        }

        private static int GetVisageStat(Protagonist p)
        {
            var stat = p.DerivedStats.FirstOrDefault(s => s.Name == "visage");
            return stat?.GetValue(p) ?? 50;
        }

        private void ShowBootMessage(string message)
        {
            if (_terminal == null) return;
            _terminal.Clear();
            _terminal.CenteredText(
                Config.Terminal.MainHeight / 2 - 1,
                "CATHEDRAL – DIALOGUE DEMO",
                Config.Colors.Yellow,
                Config.Colors.Black);
            _terminal.CenteredText(
                Config.Terminal.MainHeight / 2 + 1,
                message,
                Config.Colors.LightGray,
                Config.Colors.Black);
        }
    }
}
