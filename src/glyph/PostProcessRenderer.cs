using System;
using System.Threading;
using Cathedral.Audio;
using OpenTK.Graphics.OpenGL4;

namespace Cathedral.Glyph
{
    /// <summary>
    /// Final full-screen shader layer.
    ///
    /// The whole frame (sphere, sky, terminal HUD, popup) is rendered into an offscreen
    /// framebuffer instead of the window, then blitted to the window through a fragment
    /// shader — so anything written here affects the *global* render, not one pass.
    ///
    /// Currently that shader is a dither: the colour is quantised to a small number of
    /// levels per channel and the quantisation error is scattered with an ordered
    /// (Bayer) or noise threshold matrix, which is what gives the banded, printed look.
    ///
    /// Usage per frame:
    ///     _post.Begin(w, h);      // binds the offscreen target, sets the viewport
    ///     ... draw everything ...
    ///     _post.End(w, h);        // resolves to the window through the dither shader
    ///
    /// When <see cref="Mode"/> is Off the pass is skipped entirely and rendering goes
    /// straight to the window, so the effect costs nothing when it is not wanted.
    /// </summary>
    public class PostProcessRenderer : IDisposable
    {
        public enum DitherMode
        {
            Off = 0,
            Bayer8 = 1,      // 8x8 ordered dither, per-channel quantisation
            Bayer4Mono = 2,  // 4x4 ordered dither collapsed to two tones
            Noise = 3        // hashed (blue-noise-ish) dither, per-channel
        }

        private DitherMode _mode = (DitherMode)Cathedral.Config.PostProcess.DitherMode;

        // Remembered so Enabled=true can restore whatever dither was in use before it was
        // switched off, rather than snapping everyone back to one hardcoded mode.
        private DitherMode _lastEnabledMode =
            (DitherMode)Cathedral.Config.PostProcess.DitherMode is var m && m != DitherMode.Off
                ? m : DitherMode.Bayer8;

        /// <summary>Which dither to apply. Off bypasses the whole pass.</summary>
        public DitherMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                if (value != DitherMode.Off) _lastEnabledMode = value;
            }
        }

        /// <summary>
        /// Master switch for the whole layer — the resting dither and the event pulses
        /// alike, since a pulse is only ever a temporary override of the resting mode.
        /// Turning it back on restores the dither that was in use before.
        /// </summary>
        public bool Enabled
        {
            get => Mode != DitherMode.Off;
            set => Mode = value ? _lastEnabledMode : DitherMode.Off;
        }

        /// <summary>Quantisation steps per channel. 2 = 1 bit per channel, 16 = subtle.</summary>
        public int Levels { get; set; } = Cathedral.Config.PostProcess.Levels;

        /// <summary>
        /// Size of a dither cell in real pixels. 1 = per-pixel (fine grain),
        /// 3-4 = chunky, low-res look — the frame is also point-sampled at that scale.
        /// </summary>
        public int PixelScale { get; set; } = Cathedral.Config.PostProcess.PixelScale;

        /// <summary>Blend between the original frame (0) and the dithered result (1).</summary>
        public float Strength { get; set; } = Cathedral.Config.PostProcess.Strength;

        // ── Glow ──────────────────────────────────────────────────────────────────
        //
        // Every lit glyph bleeds a little of its own light into the black around it, the way a
        // phosphor or an LED does. Computed in the same resolve pass, ahead of the dither, from
        // the mip chain of the finished frame: the light of each pixel is spread at six scales
        // (2 to 64 pixels) and added back over the sharp frame. The frame itself is never
        // blurred - the halo only ever adds to it - so a letter keeps its edges and stands out
        // further from the dark ground. Its hue is the hue of what emits it, so the three-hue
        // rule holds by construction, and a sum that would clip is scaled down as a whole
        // rather than per channel, so a bright yellow does not wash out to white.

        /// <summary>Whether the glow is on. Independent of the dither: either may run alone.</summary>
        public bool GlowEnabled { get; set; } = Cathedral.Config.PostProcess.GlowEnabled;

        /// <summary>Overall intensity of the halo added over the frame.</summary>
        public float GlowStrength { get; set; } = Cathedral.Config.PostProcess.GlowStrength;

        /// <summary>
        /// Share of the halo given to the tight scales (2-4 px: the bloom of one glyph) against
        /// the wide ones (8-64 px: the light of a crowd of them lifting the whole region).
        /// </summary>
        public float GlowTightness { get; set; } = Cathedral.Config.PostProcess.GlowTightness;

        /// <summary>
        /// How far the halo falters, as a share of its strength (0 = steady). Only the halo moves:
        /// the glyphs themselves are drawn at a constant brightness, so text never flickers, only
        /// the light around it. Kept slow (under 2 Hz) and shallow so it reads as a lamp, never as
        /// a strobe.
        /// </summary>
        public float GlowFlicker { get; set; } = Cathedral.Config.PostProcess.GlowFlicker;

        /// <summary>
        /// How firmly dark ink on a lit surface is protected (0 = not at all, 1 = fully). On a
        /// bright fill - black text on a yellow card, a highlighted menu row - the fill's own halo
        /// spilled over the dark strokes and thinned them to nothing. Where the ground is lit, the
        /// halo is withheld and a pixel darker than that ground is deepened a little, which gives
        /// the strokes back their weight. Light text on a dark ground is untouched.
        /// </summary>
        public float GlowProtect { get; set; } = Cathedral.Config.PostProcess.GlowProtect;

        /// <summary>
        /// The other half of keeping text sharp: light text on a dark ground. The halo used to start
        /// at the very edge of a stroke and fill the gaps inside and between letters, which read as
        /// blur. Within <see cref="Cathedral.Config.PostProcess.GlowOutlineWidth"/> pixels of a lit
        /// stroke, a dark pixel is kept from the halo and pressed toward black - a thin dark contour
        /// round each glyph, the light beginning just outside it. 0 = off, 1 = full.
        /// </summary>
        public float GlowOutline { get; set; } = Cathedral.Config.PostProcess.GlowOutline;

        /// <summary>
        /// Pins the flicker's clock to a given second instead of real time. Null (the default) runs
        /// it on the frame clock. For filming: a script steps it by a fixed amount between snaps so
        /// the frames are evenly spaced whatever the renderer's frame rate.
        /// </summary>
        public float? GlowClockOverride { get; set; }

        private double _glowClock;
        private float GlowTime => GlowClockOverride ?? (float)(_glowClock % 3600.0);

        /// <summary>
        /// The whole screen's falter at time t, around 1. Three slow incommensurate sines make the
        /// lamp breathe; then, in roughly one window of 2.3 s in three, a short dip with a quick
        /// double catch, which is the "faltering". Hashed from the window index, never drawn from
        /// a generator: it is decoration and must not touch GameRng.
        /// </summary>
        private static float GlobalFalter(float t, float amount)
        {
            const float Tau = MathF.PI * 2f;
            float breathe = 0.50f * MathF.Sin(Tau * 0.37f * t)
                          + 0.30f * MathF.Sin(Tau * 0.83f * t + 1.3f)
                          + 0.20f * MathF.Sin(Tau * 1.70f * t + 0.7f);

            const float Window = 2.3f;
            int slot = (int)MathF.Floor(t / Window);
            float h1 = Hash01(slot * 2 + 1), h2 = Hash01(slot * 2 + 2);
            float dip = 0f;
            if (h1 < 0.35f)
            {
                const float Length = 0.32f;
                float start = h2 * (Window - Length);
                float u = (t - slot * Window - start) / Length;
                if (u >= 0f && u <= 1f)
                    dip = MathF.Sin(MathF.PI * u) * (0.75f + 0.25f * MathF.Cos(Tau * 2.5f * u));
            }
            return 1f + amount * (0.5f * breathe - 1.6f * dip);
        }

        private static float Hash01(int n)
        {
            uint x = (uint)n * 747796405u + 2891336453u;
            x = ((x >> (int)((x >> 28) + 4u)) ^ x) * 277803737u;
            x = (x >> 22) ^ x;
            return (x & 0xFFFFFF) / (float)0x1000000;
        }

        // ── Event pulses ──────────────────────────────────────────────────────────
        //
        // A game event briefly overrides the resting dither, then decays back — the
        // visual counterpart of a UI sound effect. Fed from AmbianceEngine.GameEventFired
        // so a pulse fires from exactly the same call sites as the sound it mirrors,
        // and stays in step with it for free.

        /// <summary>The dither a given event switches to, and for how long it holds.</summary>
        private readonly struct Pulse
        {
            public readonly DitherMode Mode;
            public readonly int Levels;
            public readonly int PixelScale;
            /// <summary>Seconds to hold, or null to use <see cref="PulseDuration"/>.</summary>
            public readonly float? Duration;

            public Pulse(DitherMode mode, int levels, int pixelScale, float? duration = null)
            {
                Mode = mode; Levels = levels; PixelScale = pixelScale; Duration = duration;
            }
        }

        /// <summary>
        /// Indexed by <see cref="GameEventType"/>. Retune freely — this table is the
        /// whole vocabulary of the effect.
        /// </summary>
        private static readonly Pulse[] PulseTable =
        {
            // Hover holds much shorter than the rest: it fires on every cell the mouse
            // crosses, so at the standard duration a sweep would smear into one long pulse.
            new(DitherMode.Noise,   6, 1, 0.05f), // SmallInteraction — hover: grain appears, geometry unchanged
            new(DitherMode.Bayer8,  3, 1), // StrongInteraction — click: palette collapses, grain unchanged
            new(DitherMode.Noise,   2, 1), // PositiveOutcome   — success
            new(DitherMode.Noise,   2, 1), // NegativeOutcome   — failure
            new(DitherMode.Noise,   2, 1), // NeutralOutcome
            // Hit — a blow lands: two tones in coarse blocks, the frame shattered for a beat.
            new(DitherMode.Bayer4Mono, 2, 4, 0.18f),
            // Transition — a change of phase or screen. Frequent, so it is a click's weight with
            // the click's palette and duration; only the grain differs (noise, not Bayer), so
            // the two read as related but distinct. Never pixelated: that is the hit's.
            new(DitherMode.Noise,   3, 1),
        };

        /// <summary>Whether event pulses fire at all. F/G/H tune the resting state regardless.</summary>
        public bool PulsesEnabled { get; set; } = Cathedral.Config.PostProcess.PulsesEnabled;

        /// <summary>
        /// How long a pulse holds before snapping back to the resting dither, for rows
        /// in <see cref="PulseTable"/> that do not carry their own duration.
        /// </summary>
        public float PulseDuration { get; set; } = Cathedral.Config.PostProcess.PulseDuration;

        // Events arrive from whatever thread raised them; the render thread drains this
        // in Update(). -1 = nothing pending.
        private int _pendingPulse = -1;
        private GameEventType _activePulse;
        private float _pulseRemaining;

        // ── Magnifier ─────────────────────────────────────────────────────────────
        //
        // The finished frame is already in a texture, so magnifying it is a UV remap in
        // the resolve shader — no camera change, no re-layout, nothing the game can see.
        // It reads the frame that was drawn, so game state is untouched by construction.

        /// <summary>Magnification while the zoom is held. 1.0 = none.</summary>
        public float ZoomFactor { get; set; } = 3.0f;

        private bool _zoomActive;
        private float _zoomU, _zoomV;

        /// <summary>
        /// Points the magnifier at a spot on screen, in UV (0..1, v measured from the
        /// bottom).
        ///
        /// The centre needs no inset: sampleUv = c + (uv - c) / z maps uv in [0,1] onto
        /// [c - c/z, c + (1-c)/z], which already stays inside [0,1] for any c in [0,1],
        /// whatever the zoom. Holding the centre away from the edges only makes the outer
        /// band of the frame unreachable, so c is clamped to [0,1] and nothing more — the
        /// cursor stays the fixed point of the transform right up to the screen edge.
        /// </summary>
        public void SetZoom(bool active, float u, float v)
        {
            _zoomActive = active && ZoomFactor > 1.0f;
            if (!_zoomActive) return;

            // Guards only against a cursor position reported outside the client area.
            _zoomU = Math.Clamp(u, 0.0f, 1.0f);
            _zoomV = Math.Clamp(v, 0.0f, 1.0f);
        }

        /// <summary>
        /// The pass has to run for the magnifier even with the dither switched off, so
        /// "is the dither on" is no longer the same question as "is the pass on".
        /// </summary>
        private bool PassActive => Mode != DitherMode.Off || _zoomActive || GlowActive;

        private bool GlowActive => GlowEnabled && GlowStrength > 0f;

        private int _fbo;
        private int _colorTex;
        private int _depthRbo;
        private int _width;
        private int _height;

        private int _program;
        private int _emptyVao;

        private int _uScene, _uMode, _uLevels, _uPixelScale, _uStrength, _uResolution;
        private int _uZoom, _uZoomCenter;
        private int _uGlowSrc, _uGlow, _uGlowTight, _uGlowMaxLod, _uGlowTime, _uGlowFlicker, _uGlowProtect, _uGlowProtectKnee, _uGlowOutline;

        // The glow's source: the frame's bright parts only, at half resolution, read through
        // its mip chain by a trilinear sampler on unit 1. A bright-pass rather than the frame
        // itself, because a halo spread from everything lifted every dim panel fill into a haze:
        // what should shine is a lit glyph, not a large surface of dark gold.
        private int _glowSampler;
        private int _brightProgram;
        private int _uBrightScene, _uBrightKnee;
        private int _glowFbo, _glowTex, _glowW, _glowH;

        // Mirrors the filter currently set on _colorTex, so the zoom toggle only issues
        // TexParameter calls when it actually changes.
        private TextureMinFilter _currentFilter = TextureMinFilter.Nearest;

        private bool _initialized;

        public void Initialize()
        {
            if (_initialized) return;

            _program = CreateProgram(VertexSrc, FragmentSrc);
            _uScene = GL.GetUniformLocation(_program, "uScene");
            _uMode = GL.GetUniformLocation(_program, "uMode");
            _uLevels = GL.GetUniformLocation(_program, "uLevels");
            _uPixelScale = GL.GetUniformLocation(_program, "uPixelScale");
            _uStrength = GL.GetUniformLocation(_program, "uStrength");
            _uResolution = GL.GetUniformLocation(_program, "uResolution");
            _uZoom = GL.GetUniformLocation(_program, "uZoom");
            _uZoomCenter = GL.GetUniformLocation(_program, "uZoomCenter");
            _uGlowSrc = GL.GetUniformLocation(_program, "uGlowSrc");
            _uGlow = GL.GetUniformLocation(_program, "uGlow");
            _uGlowTight = GL.GetUniformLocation(_program, "uGlowTight");
            _uGlowMaxLod = GL.GetUniformLocation(_program, "uGlowMaxLod");
            _uGlowTime = GL.GetUniformLocation(_program, "uGlowTime");
            _uGlowFlicker = GL.GetUniformLocation(_program, "uGlowFlicker");
            _uGlowProtect = GL.GetUniformLocation(_program, "uGlowProtect");
            _uGlowProtectKnee = GL.GetUniformLocation(_program, "uGlowProtectKnee");
            _uGlowOutline = GL.GetUniformLocation(_program, "uGlowOutline");

            _brightProgram = CreateProgram(VertexSrc, BrightPassSrc);
            _uBrightScene = GL.GetUniformLocation(_brightProgram, "uScene");
            _uBrightKnee = GL.GetUniformLocation(_brightProgram, "uKnee");

            _glowSampler = GL.GenSampler();
            GL.SamplerParameter(_glowSampler, SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.SamplerParameter(_glowSampler, SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.SamplerParameter(_glowSampler, SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.SamplerParameter(_glowSampler, SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // Fullscreen triangle is generated from gl_VertexID; no buffers needed,
            // but core profile still requires a bound VAO.
            _emptyVao = GL.GenVertexArray();

            _initialized = true;
        }

        /// <summary>
        /// Requests a pulse. Thread-safe — called from wherever the matching sound is
        /// fired. Only the highest-priority request in a frame survives, so a success
        /// landing on the same frame as the click that caused it is not swallowed by it.
        /// </summary>
        public void TriggerPulse(GameEventType evt)
        {
            if (!PulsesEnabled) return;

            int incoming = (int)evt;
            while (true)
            {
                int current = Volatile.Read(ref _pendingPulse);
                if (current >= 0 && PulsePriority(current) > PulsePriority(incoming)) return;
                if (Interlocked.CompareExchange(ref _pendingPulse, incoming, current) == current) return;
            }
        }

        /// <summary>Advances the pulse timer. Call once per frame, before <see cref="Begin"/>.</summary>
        public void Update(float deltaSeconds)
        {
            int pending = Interlocked.Exchange(ref _pendingPulse, -1);
            if (pending >= 0)
            {
                // A running pulse is only interrupted by one at least as important,
                // so a hover cannot cut a success short.
                if (_pulseRemaining <= 0f || PulsePriority(pending) >= PulsePriority((int)_activePulse))
                {
                    _activePulse = (GameEventType)pending;
                    _pulseRemaining = PulseTable[pending].Duration ?? PulseDuration;
                }
            }

            if (_pulseRemaining > 0f) _pulseRemaining -= deltaSeconds;
            _glowClock += deltaSeconds;
        }

        /// <summary>
        /// A hit outranks outcomes, which outrank clicks, which outrank hovers. A transition sits
        /// with the outcomes, so the click that caused it does not hide it in the same frame.
        /// </summary>
        private static int PulsePriority(int evt) => (GameEventType)evt switch
        {
            GameEventType.SmallInteraction => 0,
            GameEventType.StrongInteraction => 1,
            GameEventType.Hit => 3,
            _ => 2
        };

        private bool PulseActive => _pulseRemaining > 0f && Mode != DitherMode.Off;

        /// <summary>
        /// While the magnifier is held the dither drops to two-tone, which reads as the
        /// deliberate mode change it is. Gated on the dither being on at all: with it off
        /// the magnifier stays a clean passthrough rather than switching one on.
        /// </summary>
        private bool ZoomDither => _zoomActive && Mode != DitherMode.Off;

        // Precedence: zoom (held, deliberate) beats a pulse (transient), which beats the
        // resting state. Zoom also suppresses the pulse's levels/scale, so a hover landing
        // mid-zoom cannot shift the grain under a held magnifier. Mode Off stays off
        // throughout - disabling the layer disables the pulses with it.
        private DitherMode EffectiveMode =>
            ZoomDither    ? DitherMode.Bayer4Mono
          : PulseActive   ? PulseTable[(int)_activePulse].Mode
          :                 Mode;

        private int EffectiveLevels =>
            !ZoomDither && PulseActive ? PulseTable[(int)_activePulse].Levels : Levels;

        private int EffectivePixelScale =>
            !ZoomDither && PulseActive ? PulseTable[(int)_activePulse].PixelScale : PixelScale;

        /// <summary>
        /// Binds the offscreen target. Returns false when the pass is disabled or the
        /// window has no area, in which case the caller keeps drawing to the window.
        /// </summary>
        public bool Begin(int width, int height)
        {
            if (!PassActive || width <= 0 || height <= 0) return false;
            if (!_initialized) Initialize();

            EnsureTarget(width, height);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            GL.Viewport(0, 0, width, height);
            return true;
        }

        /// <summary>
        /// Where <see cref="End"/> resolves the frame: 0 is the window. <c>--record</c> points it at
        /// the recorder's own target, which reads the finished frame back and presents it itself.
        /// </summary>
        public int OutputFramebuffer { get; set; }

        /// <summary>Resolves the offscreen frame to the window through the dither shader.</summary>
        public void End(int width, int height)
        {
            if (!PassActive || width <= 0 || height <= 0) return;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, OutputFramebuffer);
            GL.Viewport(0, 0, width, height);

            // The blit owns every pixel; depth and blending would only interfere.
            bool depthWasOn = GL.IsEnabled(EnableCap.DepthTest);
            bool blendWasOn = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            GL.UseProgram(_program);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _colorTex);

            // Nearest keeps the dither pixel-exact at 1:1, but magnified it turns every
            // texel into a hard 3x3 block. Smooth the frame only while zooming.
            var filter = _zoomActive ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
            if (filter != _currentFilter)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int)(_zoomActive ? TextureMagFilter.Linear : TextureMagFilter.Nearest));
                _currentFilter = filter;
            }

            bool glow = GlowActive;
            if (glow)
            {
                // The halo is read from the mip chain of this very frame's bright-pass, so both
                // are rebuilt every frame.
                RenderBrightPass(width, height);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, OutputFramebuffer);
                GL.Viewport(0, 0, width, height);
                GL.UseProgram(_program);

                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, _glowTex);
                GL.BindSampler(1, _glowSampler);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _colorTex);
            }

            GL.Uniform1(_uScene, 0);
            GL.Uniform1(_uGlowSrc, 1);
            float flicker = Math.Clamp(GlowFlicker, 0f, 0.5f);
            GL.Uniform1(_uGlow, glow ? Math.Max(0f, GlowStrength) * GlobalFalter(GlowTime, flicker) : 0f);
            GL.Uniform1(_uGlowTime, GlowTime);
            GL.Uniform1(_uGlowFlicker, flicker);
            GL.Uniform1(_uGlowProtect, Math.Clamp(GlowProtect, 0f, 1f));
            GL.Uniform2(_uGlowOutline, Math.Clamp(GlowOutline, 0f, 1f),
                        Math.Max(1f, Cathedral.Config.PostProcess.GlowOutlineWidth) / (_zoomActive ? ZoomFactor : 1f));
            GL.Uniform3(_uGlowProtectKnee, Cathedral.Config.PostProcess.GlowProtectLow,
                        Cathedral.Config.PostProcess.GlowProtectHigh, Cathedral.Config.PostProcess.GlowProtectLod);
            GL.Uniform1(_uGlowTight, Math.Clamp(GlowTightness, 0f, 1f));
            GL.Uniform1(_uGlowMaxLod, (float)Math.Floor(Math.Log2(Math.Max(1, Math.Max(_glowW, _glowH)))));
            GL.Uniform1(_uZoom, _zoomActive ? ZoomFactor : 1.0f);
            GL.Uniform2(_uZoomCenter, _zoomU, _zoomV);
            GL.Uniform1(_uMode, (int)EffectiveMode);
            GL.Uniform1(_uLevels, Math.Max(2, EffectiveLevels));
            GL.Uniform1(_uPixelScale, (float)Math.Max(1, EffectivePixelScale));
            GL.Uniform1(_uStrength, Math.Clamp(Strength, 0f, 1f));
            GL.Uniform2(_uResolution, (float)width, (float)height);

            GL.BindVertexArray(_emptyVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            GL.BindVertexArray(0);

            if (glow)
            {
                GL.BindSampler(1, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
            }

            if (depthWasOn) GL.Enable(EnableCap.DepthTest);
            if (blendWasOn) GL.Enable(EnableCap.Blend);
        }

        /// <summary>Cycles Off -> Bayer8 -> Bayer4Mono -> Noise -> Off and reports it.</summary>
        public string CycleMode()
        {
            Mode = Mode switch
            {
                DitherMode.Off => DitherMode.Bayer8,
                DitherMode.Bayer8 => DitherMode.Bayer4Mono,
                DitherMode.Bayer4Mono => DitherMode.Noise,
                _ => DitherMode.Off
            };
            return Describe();
        }

        public string Describe()
            => $"dither={Mode} levels={Levels} pixelScale={PixelScale} strength={Strength:0.00} " +
               $"glow={(GlowEnabled ? $"on ({GlowStrength:0.00})" : "off")} " +
               $"pulses={(PulsesEnabled ? $"on ({PulseDuration:0.00}s)" : "off")}";

        private void EnsureTarget(int width, int height)
        {
            if (_fbo != 0 && width == _width && height == _height) return;

            DisposeTarget();
            _width = width;
            _height = height;

            _colorTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _colorTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            // Nearest: the dither is a pixel-exact effect, any filtering would smear it.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            _currentFilter = TextureMinFilter.Nearest; // fresh texture, filter state reset

            _depthRbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            _fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                                    TextureTarget.Texture2D, _colorTex, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
                                       RenderbufferTarget.Renderbuffer, _depthRbo);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine($"PostProcess: framebuffer incomplete ({status}) - disabling final shader layer");
                DisposeTarget();
                Mode = DitherMode.Off;
            }
        }

        /// <summary>
        /// Writes the frame's bright parts into the half-resolution glow texture and rebuilds its
        /// mip chain. Leaves the glow framebuffer bound; the caller rebinds its own.
        /// </summary>
        private void RenderBrightPass(int width, int height)
        {
            int gw = Math.Max(1, width / 2), gh = Math.Max(1, height / 2);
            if (_glowFbo == 0 || gw != _glowW || gh != _glowH)
            {
                DisposeGlowTarget();
                _glowW = gw; _glowH = gh;
                _glowTex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _glowTex);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, gw, gh, 0,
                              PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                _glowFbo = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glowFbo);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                                        TextureTarget.Texture2D, _glowTex, 0);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glowFbo);
            GL.Viewport(0, 0, gw, gh);
            GL.UseProgram(_brightProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _colorTex);
            GL.Uniform1(_uBrightScene, 0);
            GL.Uniform2(_uBrightKnee, Cathedral.Config.PostProcess.GlowKneeLow, Cathedral.Config.PostProcess.GlowKneeHigh);
            GL.BindVertexArray(_emptyVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            GL.BindVertexArray(0);

            GL.BindTexture(TextureTarget.Texture2D, _glowTex);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void DisposeGlowTarget()
        {
            if (_glowFbo != 0) { GL.DeleteFramebuffer(_glowFbo); _glowFbo = 0; }
            if (_glowTex != 0) { GL.DeleteTexture(_glowTex); _glowTex = 0; }
            _glowW = _glowH = 0;
        }

        private void DisposeTarget()
        {
            DisposeGlowTarget();
            if (_fbo != 0) { GL.DeleteFramebuffer(_fbo); _fbo = 0; }
            if (_colorTex != 0) { GL.DeleteTexture(_colorTex); _colorTex = 0; }
            if (_depthRbo != 0) { GL.DeleteRenderbuffer(_depthRbo); _depthRbo = 0; }
            _width = _height = 0;
        }

        public void Dispose()
        {
            DisposeTarget();
            if (_program != 0) { GL.DeleteProgram(_program); _program = 0; }
            if (_emptyVao != 0) { GL.DeleteVertexArray(_emptyVao); _emptyVao = 0; }
            if (_glowSampler != 0) { GL.DeleteSampler(_glowSampler); _glowSampler = 0; }
            if (_brightProgram != 0) { GL.DeleteProgram(_brightProgram); _brightProgram = 0; }
            _initialized = false;
        }

        private static int CreateProgram(string vsSrc, string fsSrc)
        {
            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vsSrc);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int status);
            if (status == 0) throw new Exception("PostProcess VS compile: " + GL.GetShaderInfoLog(vs));

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fsSrc);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out status);
            if (status == 0) throw new Exception("PostProcess FS compile: " + GL.GetShaderInfoLog(fs));

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, vs);
            GL.AttachShader(prog, fs);
            GL.LinkProgram(prog);
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out status);
            if (status == 0) throw new Exception("PostProcess link: " + GL.GetProgramInfoLog(prog));

            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
            return prog;
        }

        // Fullscreen triangle from gl_VertexID - no vertex buffer at all.
        private const string VertexSrc = @"
#version 330 core
out vec2 vUv;
void main()
{
    vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
    vUv = p;
    gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
}";

        private const string FragmentSrc = @"
#version 330 core
in vec2 vUv;
out vec4 FragColor;

uniform sampler2D uScene;
uniform int   uMode;
uniform int   uLevels;
uniform float uPixelScale;
uniform float uStrength;
uniform vec2  uResolution;
uniform float uZoom;        // 1.0 = no magnification
uniform vec2  uZoomCenter;  // UV the magnifier is centred on
uniform sampler2D uGlowSrc; // the frame's bright-pass at half size, through its mip chain
uniform float uGlow;        // halo intensity, 0 = no glow
uniform float uGlowTight;   // share of the halo given to the tight scales
uniform float uGlowMaxLod;  // last mip level of the frame
uniform float uGlowTime;    // seconds, for the falter
uniform float uGlowFlicker; // how far the halo falters, as a share of it
uniform float uGlowProtect; // how firmly dark ink on a lit surface is kept from the halo
uniform vec2  uGlowOutline;     // contour strength round light strokes, its width in frame pixels
uniform vec3  uGlowProtectKnee; // ground at which protection starts, is full; the mip level it is read at

float vhash(vec3 p)
{
    p = fract(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return fract((p.x + p.y) * p.z);
}

// Smooth value noise over (x, y, t): the screen is divided into a few broad regions whose halos
// breathe slightly out of step, so the light wavers like a flame rather than pulsing as one.
float vnoise(vec3 p)
{
    vec3 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = mix(mix(vhash(i),                  vhash(i + vec3(1, 0, 0)), f.x),
                  mix(vhash(i + vec3(0, 1, 0)),  vhash(i + vec3(1, 1, 0)), f.x), f.y);
    float b = mix(mix(vhash(i + vec3(0, 0, 1)),  vhash(i + vec3(1, 0, 1)), f.x),
                  mix(vhash(i + vec3(0, 1, 1)),  vhash(i + vec3(1, 1, 1)), f.x), f.y);
    return mix(a, b, f.z);
}

// One mip level, smoothed: four bilinear taps half a texel of that level off the centre,
// so the level reads as a soft disc instead of the square block a single mip sample is.
vec3 glowLevel(vec2 uv, float lod)
{
    vec2 o = exp2(lod) / uResolution;   // half a texel of this level; level 0 is 2 px
    return 0.25 * (textureLod(uGlowSrc, uv + vec2( o.x,  o.y), lod).rgb
                 + textureLod(uGlowSrc, uv + vec2(-o.x,  o.y), lod).rgb
                 + textureLod(uGlowSrc, uv + vec2( o.x, -o.y), lod).rgb
                 + textureLod(uGlowSrc, uv + vec2(-o.x, -o.y), lod).rgb);
}

// The light the neighbourhood of uv sheds onto it, at six scales. Tight levels make each
// glyph bloom; wide ones let a crowd of glyphs lift the dark around it. A mip level is an
// average, so a level's light is spread over its area: the weights rise with the scale to
// keep a lone dot visible in the wide halo, and each half is normalised to sum to one.
vec3 glow(vec2 uv)
{
    vec3 tight = 0.45 * glowLevel(uv, 0.0) + 0.55 * glowLevel(uv, 1.0);
    vec3 wide  = 0.20 * glowLevel(uv, min(2.0, uGlowMaxLod))
               + 0.25 * glowLevel(uv, min(3.0, uGlowMaxLod))
               + 0.30 * glowLevel(uv, min(4.0, uGlowMaxLod))
               + 0.25 * glowLevel(uv, min(5.0, uGlowMaxLod));
    float local = 1.0 + uGlowFlicker * 1.2 * (vnoise(vec3(uv * vec2(3.0, 2.25), uGlowTime * 0.9)) - 0.5);
    return mix(wide * 2.0, tight, uGlowTight) * local;
}

// Adds the halo without letting it clip. A channel driven past 1 scales the whole colour
// down with it, so the hue of what glows is kept instead of drifting to white.
//
// Dark ink on a lit surface is kept from the halo. The ground is the bright-pass averaged over
// about a glyph cell: on a yellow card or a lit menu initial it is the fill, around light text on
// black it is mostly black. Where
// it is lit, a pixel well below that ground - a stroke of ink - is kept from the halo, whose spill
// from the fill is what thinned it, and deepened in proportion, which restores the anti-aliased
// edge of each stroke and so its weight. The fill itself glows as everything lit does.
// Brightest channel of the frame at an offset of so many frame pixels from uv.
float litAt(vec2 uv, vec2 px)
{
    vec3 s = texture(uScene, uv + px / uResolution).rgb;
    return max(max(s.r, s.g), s.b);
}

// How much this pixel is the dark ground right beside a lit stroke: the brightest pixel within
// the contour's width, two rings of eight, the outer weighted down so the contour fades out.
// A pixel well below that neighbour is ground, not the stroke's own anti-aliased edge.
float contourAt(vec2 uv, float lum)
{
    float w = uGlowOutline.y, h = 0.5 * w;
    float n1 = max(max(max(litAt(uv, vec2( h, 0)), litAt(uv, vec2(-h, 0))),
                       max(litAt(uv, vec2(0,  h)), litAt(uv, vec2(0, -h)))),
                   max(max(litAt(uv, vec2( h,  h)), litAt(uv, vec2(-h,  h))),
                       max(litAt(uv, vec2( h, -h)), litAt(uv, vec2(-h, -h)))));
    float n2 = max(max(max(litAt(uv, vec2( w, 0)), litAt(uv, vec2(-w, 0))),
                       max(litAt(uv, vec2(0,  w)), litAt(uv, vec2(0, -w)))),
                   max(max(litAt(uv, vec2( w,  h)), litAt(uv, vec2(-w, -h))),
                       max(litAt(uv, vec2( h, -w)), litAt(uv, vec2(-h,  w)))));
    float near = max(n1, 0.7 * n2);
    return smoothstep(0.25, 0.55, near) * smoothstep(0.35, 0.65, 1.0 - lum / max(near, 1e-3));
}

vec3 addGlow(vec3 c, vec2 uv)
{
    if (uGlow <= 0.0) return c;

    // The contour round light strokes, read before the ink step below changes c.
    float edge = uGlowOutline.x > 0.0 ? uGlowOutline.x * contourAt(uv, max(max(c.r, c.g), c.b)) : 0.0;

    float L = uGlowProtectKnee.z;
    vec3 groundRgb = 0.5 * (textureLod(uGlowSrc, uv, L - 0.5).rgb + textureLod(uGlowSrc, uv, L + 0.5).rgb);
    float ground = max(max(groundRgb.r, groundRgb.g), groundRgb.b);
    float protect = uGlowProtect * smoothstep(uGlowProtectKnee.x, uGlowProtectKnee.y, ground);

    float lum = max(max(c.r, c.g), c.b);
    // How far below the ground this pixel sits: 0 for the fill itself, 1 for a black stroke on it.
    float below = 1.0 - lum / max(ground, 1e-3);
    float shade = protect * smoothstep(0.12, 0.45, below);   // kept from the halo
    float ink   = protect * smoothstep(0.35, 0.75, below);   // and deepened
    c *= 1.0 - 0.70 * ink;
    c *= 1.0 - 0.60 * edge;

    vec3 g = c + (1.0 - max(shade, edge)) * uGlow * glow(uv);
    float m = max(max(g.r, g.g), g.b);
    return m > 1.0 ? g / m : g;
}

// Ordered dither matrices, normalised to [0,1) and biased to -0.5..0.5 at use.
const float BAYER8[64] = float[64](
     0.0/64.0, 32.0/64.0,  8.0/64.0, 40.0/64.0,  2.0/64.0, 34.0/64.0, 10.0/64.0, 42.0/64.0,
    48.0/64.0, 16.0/64.0, 56.0/64.0, 24.0/64.0, 50.0/64.0, 18.0/64.0, 58.0/64.0, 26.0/64.0,
    12.0/64.0, 44.0/64.0,  4.0/64.0, 36.0/64.0, 14.0/64.0, 46.0/64.0,  6.0/64.0, 38.0/64.0,
    60.0/64.0, 28.0/64.0, 52.0/64.0, 20.0/64.0, 62.0/64.0, 30.0/64.0, 54.0/64.0, 22.0/64.0,
     3.0/64.0, 35.0/64.0, 11.0/64.0, 43.0/64.0,  1.0/64.0, 33.0/64.0,  9.0/64.0, 41.0/64.0,
    51.0/64.0, 19.0/64.0, 59.0/64.0, 27.0/64.0, 49.0/64.0, 17.0/64.0, 57.0/64.0, 25.0/64.0,
    15.0/64.0, 47.0/64.0,  7.0/64.0, 39.0/64.0, 13.0/64.0, 45.0/64.0,  5.0/64.0, 37.0/64.0,
    63.0/64.0, 31.0/64.0, 55.0/64.0, 23.0/64.0, 61.0/64.0, 29.0/64.0, 53.0/64.0, 21.0/64.0
);

const float BAYER4[16] = float[16](
     0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
    12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
     3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
    15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
);

float hash21(vec2 p)
{
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

void main()
{
    // Dither cells are uPixelScale pixels wide; point-sample the cell centre so the
    // frame itself reads as low-resolution rather than a fine grain over a sharp image.
    vec2 pixel = vUv * uResolution;
    vec2 cell  = floor(pixel / uPixelScale);
    vec2 sampleUv = (cell + 0.5) * uPixelScale / uResolution;

    // Magnify by shrinking the sampled region around the centre. Only the sample
    // coordinate moves: cell stays in screen space, so the dither grid keeps its
    // real pixel size instead of being blown up along with the picture.
    // (Keep this file's GLSL strictly ASCII - the source character set is ASCII and
    //  drivers reject stray bytes even inside comments.)
    sampleUv = uZoomCenter + (sampleUv - uZoomCenter) / uZoom;

    vec3 original = addGlow(texture(uScene, sampleUv).rgb, sampleUv);

    // Mode 0 is a pure passthrough. It is reachable now that the magnifier can run the
    // pass with the dither switched off; without this the frame would be dithered anyway.
    if (uMode == 0)
    {
        FragColor = vec4(original, 1.0);
        return;
    }

    float levels = float(uLevels);
    float steps  = levels - 1.0;

    ivec2 c = ivec2(mod(cell, 8.0));
    float threshold;
    vec3 dithered;

    if (uMode == 2)
    {
        // Two-tone: dither the luminance against a 4x4 matrix, keep it black-or-white.
        ivec2 c4 = ivec2(mod(cell, 4.0));
        threshold = BAYER4[c4.y * 4 + c4.x] - 0.5;
        float lum = dot(original, vec3(0.299, 0.587, 0.114));
        dithered = vec3(step(0.5, lum + threshold));
    }
    else
    {
        if (uMode == 3)
            threshold = hash21(cell) - 0.5;
        else
            threshold = BAYER8[c.y * 8 + c.x] - 0.5;

        // Nudge by one quantisation step, then snap: the error becomes a pattern
        // instead of a band.
        dithered = floor(original * steps + 0.5 + threshold) / steps;
    }

    FragColor = vec4(clamp(mix(original, dithered, uStrength), 0.0, 1.0), 1.0);
}";

        // The glow's source, drawn at half resolution: each output texel takes the four frame
        // pixels under it, keeps of each only what rises above the knee, and averages them.
        // Thresholding before averaging keeps a one-pixel stroke of a letter as bright as it
        // is; averaging first would have let thin text fall below the knee. The knee reads the
        // brightest channel, so a saturated purple counts as fully as a grey of the same peak.
        private const string BrightPassSrc = @"
#version 330 core
in vec2 vUv;
out vec4 FragColor;

uniform sampler2D uScene;
uniform vec2 uKnee;   // brightest channel at which a pixel starts, and finishes, shining

vec3 bright(ivec2 p)
{
    vec3 c = texelFetch(uScene, p, 0).rgb;
    float m = max(max(c.r, c.g), c.b);
    return c * smoothstep(uKnee.x, uKnee.y, m);
}

void main()
{
    ivec2 size = textureSize(uScene, 0);
    ivec2 p = min(ivec2(gl_FragCoord.xy) * 2, size - 2);
    vec3 sum = bright(p) + bright(p + ivec2(1, 0)) + bright(p + ivec2(0, 1)) + bright(p + ivec2(1, 1));
    FragColor = vec4(sum * 0.25, 1.0);
}";
    }
}
