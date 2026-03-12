using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;
using Cathedral.Terminal;
using Cathedral.Fight.Generators;

namespace Cathedral.Fight
{
    /// <summary>
    /// Launches the fight area test display mode.
    /// Any keypress regenerates the map; ESC closes the window.
    /// </summary>
    public static class FightAreaTestLauncher
    {
        public static void Launch(string mode, Dictionary<string, string> modeArgs)
        {
            Console.WriteLine("=== Fight Area Generator Test ===\n");
            Console.WriteLine(mode == "random" ? "Mode: random (new mode + settings each regeneration)" : $"Mode: {mode}");

            var native = new NativeWindowSettings
            {
                ClientSize = new Vector2i(
                    Config.Terminal.MainWidth  * Config.Terminal.MainCellSize,
                    Config.Terminal.MainHeight * Config.Terminal.MainCellSize),
                Title = $"Cathedral - Fight Area [{mode}]",
                Flags = ContextFlags.Default,
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                WindowBorder = WindowBorder.Resizable
            };

            using var window = new FightAreaWindow(GameWindowSettings.Default, native, mode, modeArgs);
            window.Run();
        }

        // -------------------------------------------------------------------------
        private class FightAreaWindow : GameWindow
        {
            private TerminalHUD? _terminal;
            private readonly string _mode;
            private readonly Dictionary<string, string> _args;
            private bool _needsRegenerate = true;
            private bool _prevAnyKeyDown = false;
            private int _seed = 0;
            private FightArea? _currentArea;
            private double _blinkTimer = 0;
            private bool _blinkOn = true;

            public FightAreaWindow(GameWindowSettings gs, NativeWindowSettings ns,
                string mode, Dictionary<string, string> args)
                : base(gs, ns)
            {
                _mode = mode;
                _args = args;
            }

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
            }

            protected override void OnUpdateFrame(FrameEventArgs args)
            {
                base.OnUpdateFrame(args);

                if (KeyboardState.IsKeyDown(Keys.Escape))
                {
                    Close();
                    return;
                }

                // Detect any key released (rising-edge of "any key pressed")
                bool anyKeyDown = false;
                foreach (Keys key in Enum.GetValues<Keys>())
                {
                    if (key == Keys.Unknown) continue;
                    if (KeyboardState.IsKeyDown(key)) { anyKeyDown = true; break; }
                }

                if (!anyKeyDown && _prevAnyKeyDown)
                    _needsRegenerate = true;

                _prevAnyKeyDown = anyKeyDown;

                if (_needsRegenerate)
                {
                    _needsRegenerate = false;
                    RegenerateAndRender();
                }

                // Blink the exit glyph (0.8s period)
                _blinkTimer += args.Time;
                bool newBlink = (_blinkTimer % 0.8) < 0.4;
                if (newBlink != _blinkOn && _currentArea != null && _terminal != null)
                {
                    _blinkOn = newBlink;
                    FightAreaRenderer.UpdateBlink(_terminal, _blinkOn);
                }
            }

            protected override void OnRenderFrame(FrameEventArgs args)
            {
                base.OnRenderFrame(args);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                _terminal?.Render(new Vector2i(ClientSize.X, ClientSize.Y));
                SwapBuffers();
            }

            protected override void OnResize(ResizeEventArgs e)
            {
                base.OnResize(e);
                GL.Viewport(0, 0, e.Width, e.Height);
                _terminal?.ForceRefresh();
            }

            protected override void OnUnload()
            {
                _terminal?.Dispose();
                base.OnUnload();
            }

            private void RegenerateAndRender()
            {
                if (_terminal == null) return;

                _seed = Environment.TickCount;
                string effectiveMode = _mode == "random" ? PickRandomMode() : _mode;
                var generator = BuildGenerator(effectiveMode, _args, _seed);
                _currentArea = generator.Generate();
                _blinkTimer = 0;
                _blinkOn = true;
                FightAreaRenderer.Render(_terminal, _currentArea, effectiveMode, _seed);
                Console.WriteLine($"Generated [{effectiveMode}] seed={_seed}");
            }

            private string PickRandomMode()
            {
                var modes = new[] { "noisy", "geometric", "rooms", "corridor", "wave", "radiant", "arena" };
                return modes[new Random(_seed).Next(modes.Length)];
            }

            private static IFightAreaGenerator BuildGenerator(string mode, Dictionary<string, string> a, int seed)
            {
                var rng = new Random(seed);
                return mode.ToLowerInvariant() switch
                {
                    "noisy" => new NoisyGenerator
                    {
                        Seed       = seed,
                        Density    = GetFloat(a, "density",     RandFloat(rng, 0.62f, 0.82f)),
                        NoiseScale = GetFloat(a, "noise-scale", RandFloat(rng, 0.07f, 0.18f)),
                    },
                    "geometric" => new GeometricGenerator
                    {
                        ShapeCount = GetInt(a, "shapes",   rng.Next(5, 15)),
                        MinSize    = GetInt(a, "min-size", rng.Next(2, 4)),
                        MaxSize    = GetInt(a, "max-size", rng.Next(5, 12)),
                    },
                    "rooms" => new RoomsGenerator
                    {
                        RoomCount     = GetInt(a,  "rooms",          rng.Next(4, 10)),
                        MinRoomSize   = GetInt(a,  "min-room",       rng.Next(4, 7)),
                        MaxRoomSize   = GetInt(a,  "max-room",       rng.Next(8, 16)),
                        CorridorWidth = GetInt(a,  "corridor-width", rng.Next(1, 4)),
                        CentralArena  = GetBool(a, "central-arena",  rng.Next(2) == 0),
                    },
                    "corridor" => new CorridorGenerator
                    {
                        BranchFactor   = GetInt(a, "branches",   rng.Next(2, 5)),
                        MaxBranchDepth = GetInt(a, "depth",      rng.Next(3, 7)),
                        MinLength      = GetInt(a, "min-length", rng.Next(3, 8)),
                    },
                    "wave" => new WaveGenerator
                    {
                        Seed        = seed,
                        BaseDensity = GetFloat(a, "density",    RandFloat(rng, 0.62f, 0.80f)),
                        Amplitude   = GetFloat(a, "amplitude",  RandFloat(rng, 0.18f, 0.32f)),
                        WaveFreq    = GetFloat(a, "freq",       RandFloat(rng, 1.5f,  4.0f)),
                        PhaseOffset = RandFloat(rng, 0f, 2f * MathF.PI),
                        NoiseScale  = GetFloat(a, "noise-scale",RandFloat(rng, 0.07f, 0.18f)),
                    },
                    "radiant" => new RadiantGenerator
                    {
                        Seed          = seed,
                        CentreDensity = GetFloat(a, "centre-density", RandFloat(rng, 0.78f, 0.95f)),
                        EdgeDensity   = GetFloat(a, "edge-density",   RandFloat(rng, 0.28f, 0.52f)),
                        FalloffPower  = GetFloat(a, "falloff",        RandFloat(rng, 1.2f,  2.4f)),
                        NoiseScale    = GetFloat(a, "noise-scale",    RandFloat(rng, 0.07f, 0.18f)),
                    },
                    "arena" => new ArenaGenerator
                    {
                        Seed           = seed,
                        StructureCount = GetInt(a, "structures",   rng.Next(8, 20)),
                        MaxStructureSize = GetInt(a, "max-size",   rng.Next(3, 6)),
                        ArenaPadding   = GetInt(a, "padding",      rng.Next(4, 8)),
                    },
                    _ => throw new ArgumentException($"Unknown fight area mode: {mode}. Valid: noisy, geometric, rooms, corridor, wave, radiant, arena, random")
                };
            }

            private static float RandFloat(Random rng, float min, float max)
                => min + (float)rng.NextDouble() * (max - min);

            private static float GetFloat(Dictionary<string, string> a, string key, float def)
                => a.TryGetValue(key, out var v) && float.TryParse(v, System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out float r) ? r : def;

            private static int GetInt(Dictionary<string, string> a, string key, int def)
                => a.TryGetValue(key, out var v) && int.TryParse(v, out int r) ? r : def;

            private static bool GetBool(Dictionary<string, string> a, string key, bool def)
                => a.TryGetValue(key, out var v) ? v is "1" or "true" or "yes" : def;
        }
    }
}
