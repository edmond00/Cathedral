// GlyphSphereCore.cs - Core rendering engine for the glyph sphere
// Contains OpenGL functionality, shaders, mesh generation, camera controls, and mouse collision detection
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using Cathedral.Pathfinding;
using Cathedral.Engine;

using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;
using Matrix4 = OpenTK.Mathematics.Matrix4;

namespace Cathedral.Glyph
{
    public class GlyphSphereCore : GameWindow
    {
        // GL objects
        int program;
        int vao, vbo, ebo;
        int instanceVbo;
        int glyphTexture;
        int indexCount;
        
        // Background sphere (opaque backdrop)
        int backgroundProgram;
        int backgroundVao, backgroundVbo, backgroundEbo;
        int backgroundIndexCount;

        // Debug rendering
        int debugProgram;
        int debugVao, debugVbo;
        List<Vector3> debugVertices = new List<Vector3>();
        List<Vector3> debugColors = new List<Vector3>();

        // Data
        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();
        GlyphInfo[] glyphInfos = null!;
        int instanceCount = 0;

        // Camera system
        private Camera _camera;
        
        // Mouse interaction
        int hoveredVertexIndex = -1;
        int lastHoveredVertexIndex = -2;
        
        // Debug visualization
        Vector3 debugCameraPos = Vector3.Zero;
        Vector3 debugRayOrigin = Vector3.Zero;
        Vector3 debugRayDirection = Vector3.Zero;
        Vector3 debugIntersectionPoint = Vector3.Zero;
        OpenTK.Mathematics.Vector2 debugMousePos = OpenTK.Mathematics.Vector2.Zero;
        bool debugShowMarkers = false; // Toggle with 'M' key - enabled by default for avatar debugging
        int debugAvatarVertex = -1; // Track avatar vertex for debug ray
        
        // Debug shader modes
        int debugShaderMode = 0; // 0=normal, 1=vertex colors only, 2=texture only, 3=wireframe, 4=grayscale
        int debugProgram1, debugProgram2, debugProgram3, debugProgram4;

        // Shared constants to avoid hardcoded duplicates
        const float QUAD_SIZE = 0.3f; // Size of each glyph quad on the sphere
        const float VERTEX_SHADER_SIZE_MULTIPLIER = 2.0f; // Multiplier used in vertex shader
        const float SPHERE_RADIUS = 25.0f; // Main sphere radius
        const int SPHERE_SUBDIVISIONS = 5; // Icosphere subdivision level (affects vertex density)
        const float CAMERA_DEFAULT_DISTANCE = 80.0f; // Default camera distance
        const float CAMERA_MIN_DISTANCE = 30.0f; // Minimum camera distance
        const float CAMERA_MAX_DISTANCE = 200f; // Maximum camera distance
        
        // Default glyph settings
        const char DEFAULT_GLYPH = '.';
        const int GLYPH_PIXEL_SIZE = 65; // raster size
        const int GLYPH_CELL = 50; // cell in atlas

        // Update timing for interface animations
        const float UPDATE_INTERVAL = 0.1f; // Update every 0.5 seconds (2 Hz)
        private float updateTimer = 0.0f;

        // Pathfinding
        private GlyphSphereGraph? _graph;
        private PathfindingService? _pathfindingService;

        // Events for interface interaction
        public event Action<int, OpenTK.Mathematics.Vector2>? VertexHovered;
        public event Action<int, OpenTK.Mathematics.Vector2>? VertexClicked;
        public event Action? CoreLoaded;
        public event Action<float>? UpdateRequested;
        
        // Terminal HUD
        private Cathedral.Terminal.TerminalHUD? _terminal;
        
        // Popup Terminal HUD (mouse-following)
        private Cathedral.Terminal.PopupTerminalHUD? _popupTerminal;

        public GlyphSphereCore(GameWindowSettings g, NativeWindowSettings n, Camera camera) : base(g, n)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }
        
        /// <summary>
        /// Gets the terminal HUD instance, or null if not initialized
        /// </summary>
        public Cathedral.Terminal.TerminalHUD? Terminal => _terminal;
        
        /// <summary>
        /// Gets the popup terminal HUD instance, or null if not initialized
        /// </summary>
        public Cathedral.Terminal.PopupTerminalHUD? PopupTerminal => _popupTerminal;
        
        /// <summary>
        /// Gets the currently hovered vertex index, or -1 if no vertex is hovered
        /// </summary>
        public int HoveredVertexIndex => hoveredVertexIndex;

        // Public interface for vertex manipulation
        public int VertexCount => vertices.Count;
        public Vector3 GetVertexPosition(int index) => vertices[index].Position;
        
        // Public access to camera for external control
        /// <summary>
        /// Gets the camera instance for external manipulation.
        /// This allows you to control camera settings, respond to events, or implement custom camera behaviors.
        /// </summary>
        public Camera Camera => _camera;
        
        private string currentGlyphSet = DEFAULT_GLYPH.ToString();
        
        public void SetVertexGlyph(int index, char glyph, Vector4 color, float size = 1.0f)
        {
            if (index >= 0 && index < vertices.Count)
            {
                // Find glyph index in the current glyph set
                int glyphIndex = currentGlyphSet.IndexOf(glyph);
                if (glyphIndex == -1) 
                {
                    // If glyph not found, add it to the set and rebuild atlas
                    currentGlyphSet += glyph;
                    RebuildGlyphAtlas(currentGlyphSet);
                    glyphIndex = currentGlyphSet.Length - 1;
                }
                
                vertices[index] = new Vertex 
                { 
                    Position = vertices[index].Position,
                    GlyphIndex = glyphIndex,
                    GlyphChar = glyph,
                    Noise = vertices[index].Noise,
                    Color = color,
                    Size = size
                };
            }
        }
        
        /// <summary>
        /// Sets a vertex glyph using BiomeType/LocationType data, automatically using the appropriate size factor
        /// </summary>
        public void SetVertexBiome(int index, string biomeName, Vector4? colorOverride = null)
        {
            if (Cathedral.Glyph.Microworld.BiomeDatabase.Biomes.TryGetValue(biomeName, out var biome))
            {
                Vector4 color = colorOverride ?? new Vector4(biome.Color.X / 255f, biome.Color.Y / 255f, biome.Color.Z / 255f, 1.0f);
                SetVertexGlyph(index, biome.Glyph, color, biome.Size);
            }
        }
        
        /// <summary>
        /// Sets a vertex glyph using LocationType data, automatically using the appropriate size factor
        /// </summary>
        public void SetVertexLocation(int index, string locationName, Vector4? colorOverride = null)
        {
            if (Cathedral.Glyph.Microworld.BiomeDatabase.Locations.TryGetValue(locationName, out var location))
            {
                Vector4 color = colorOverride ?? new Vector4(location.Color.X / 255f, location.Color.Y / 255f, location.Color.Z / 255f, 1.0f);
                SetVertexGlyph(index, location.Glyph, color, location.Size);
            }
        }
        
        private void RebuildGlyphAtlas(string glyphSet)
        {
            Console.WriteLine($"Rebuilding atlas with glyphs: \"{glyphSet}\"");
            
            // Dispose old texture
            if (glyphTexture != 0)
            {
                GL.DeleteTexture(glyphTexture);
            }
            
            // Build new atlas
            glyphInfos = BuildGlyphAtlas(glyphSet, GLYPH_CELL, GLYPH_PIXEL_SIZE, out Image<Rgba32> atlasImage);
            glyphTexture = LoadTexture(atlasImage);
            atlasImage.Dispose();
            
            // Update texture uniform
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, glyphTexture);
            int texLoc = GL.GetUniformLocation(program, "uAtlas");
            GL.Uniform1(texLoc, 0);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Build shader
            program = CreateProgram(vertexShaderSrc, fragmentShaderSrc);
            
            // Build debug shaders
            debugProgram1 = CreateProgram(vertexShaderSrc, debugFragmentSrc1); // vertex colors only
            debugProgram2 = CreateProgram(vertexShaderSrc, debugFragmentSrc2); // texture only
            debugProgram3 = CreateProgram(vertexShaderSrc, debugFragmentSrc3); // wireframe
            debugProgram4 = CreateProgram(vertexShaderSrc, debugFragmentSrc4); // grayscale

            // Build background sphere shader
            backgroundProgram = CreateProgram(backgroundVertexShaderSrc, backgroundFragmentShaderSrc);
            
            // Build debug rendering shader
            debugProgram = CreateProgram(debugVertexShaderSrc, debugFragmentShaderSrc);
            SetupDebugRendering();
            
            GL.UseProgram(program);
            
            // Build sphere with default glyphs (green dots)
            BuildSphere(SPHERE_SUBDIVISIONS, 0, SPHERE_RADIUS);
            
            // Create background sphere (90% radius, opaque)
            BuildBackgroundSphere(SPHERE_SUBDIVISIONS, 0, SPHERE_RADIUS * 0.98f);

            // Build glyph atlas with default glyph
            string defaultGlyphSet = DEFAULT_GLYPH.ToString();
            Console.WriteLine($"Generated GlyphSet: \"{defaultGlyphSet}\" ({defaultGlyphSet.Length} characters)");
            glyphInfos = BuildGlyphAtlas(defaultGlyphSet, GLYPH_CELL, GLYPH_PIXEL_SIZE, out Image<Rgba32> atlasImage);
            
            glyphTexture = LoadTexture(atlasImage);
            atlasImage.Dispose();

            // Create VBO/VAO for quad + instance data approach
            SetupInstancedRendering();

            // Fill instance buffer now
            UpdateInstanceBuffer();

            // Set texture uniform
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, glyphTexture);
            int texLoc = GL.GetUniformLocation(program, "uAtlas");
            GL.Uniform1(texLoc, 0);

            // Set initial projection
            GL.Viewport(0, 0, Size.X, Size.Y);
            UpdateProjection();

            // For mouse click reading
            CursorState = CursorState.Normal;
            
            // Initialize pathfinding
            InitializePathfinding();
            
            // Initialize terminal HUD (100x30 for Location Travel Mode UI)
            try 
            {
                _terminal = new Cathedral.Terminal.TerminalHUD(100, 30, 16, 14);
                // Event handlers removed - terminal events will be handled by game modes
                
                // Set border height function for proper mouse position correction
                _terminal.SetBorderHeightFunction(() => GetWindowBorderHeight());
                
                // Terminal starts hidden - will be shown by game modes as needed
                _terminal.Visible = false;
                
                Console.WriteLine("Terminal: HUD integrated with GlyphSphereCore (100x30 for Location Travel Mode)");
                
                // Initialize popup terminal (30x30, shares atlas with main terminal)
                _popupTerminal = new Cathedral.Terminal.PopupTerminalHUD(40, 40, 16, _terminal.Atlas);
                Console.WriteLine("Popup Terminal: HUD integrated with GlyphSphereCore (30x30 mouse-following)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Terminal: Failed to initialize HUD - {ex.Message}");
                _terminal = null;
                _popupTerminal = null;
            }
            
            // Log window border information
            LogWindowBorderInfo();
            
            // Fire loaded event for interface setup
            CoreLoaded?.Invoke();
        }
        
        #region Window Border Information
        
        private void LogWindowBorderInfo()
        {
            // Log border information for debugging
            Console.WriteLine("=== Window Border Information ===");
            Console.WriteLine($"Client Size (content area): {ClientSize}");
            Console.WriteLine($"Window Size (including borders): {Size}");
            Console.WriteLine($"Window Position: {Location}");
            Console.WriteLine($"Window Border: {WindowBorder}");
            
            // Calculate border sizes
            var borderWidth = Size.X - ClientSize.X;
            var borderHeight = Size.Y - ClientSize.Y;
            
            Console.WriteLine($"Total border width (left + right): {borderWidth}px");
            Console.WriteLine($"Total border height (top + bottom): {borderHeight}px");
            
            // On Windows, typical values:
            // - Title bar height: ~30-32px (depends on DPI/scaling)
            // - Side borders: ~8-16px total (depends on DPI/scaling)
            Console.WriteLine($"Estimated title bar height: ~{borderHeight - 8}px (assuming 4px top/bottom borders)");
            Console.WriteLine("Note: Border sizes depend on OS theme, DPI scaling, and window style");
            Console.WriteLine("=====================================");
        }
        
        /// <summary>
        /// Gets window border information that can be used by terminal for positioning
        /// </summary>
        public (int titleBarHeight, int borderWidth, int borderHeight) GetWindowBorderInfo()
        {
            var totalBorderWidth = Size.X - ClientSize.X;
            var totalBorderHeight = Size.Y - ClientSize.Y;
            
            // The empirically correct title bar height for mouse positioning is 38px
            // but our naive calculation (total - 8px) gives 31px. This 7px difference
            // likely comes from OS window decoration complexities that aren't captured
            // by simple Size vs ClientSize measurements.
            
            // Use an empirically-corrected calculation:
            // - Start with the naive estimation
            // - Apply a correction factor based on observed difference
            var naiveEstimate = totalBorderHeight - 8; // Original calculation
            var empiricalCorrection = 7; // Observed difference (38 - 31)
            var correctedTitleBarHeight = naiveEstimate + empiricalCorrection;
            
            // Ensure we don't exceed the total border height
            var titleBarHeight = Math.Min(correctedTitleBarHeight, totalBorderHeight);
            
            return (titleBarHeight, totalBorderWidth, totalBorderHeight);
        }
        
        /// <summary>
        /// Gets the window border height (title bar) for mouse position correction.
        /// This is the global function for consistent border height handling across the application.
        /// </summary>
        public float GetWindowBorderHeight()
        {
            var (titleBarHeight, _, _) = GetWindowBorderInfo();
            return Math.Max(0, titleBarHeight); // Ensure non-negative
        }
        
        #endregion

        #region Terminal Management
        
        // DEPRECATED: Old terminal demo setup - replaced by LocationTravelMode system
        // Kept for reference but no longer called
        /*
        private void SetupTerminalDemo()
        {
            if (_terminal == null) return;
            
            // Clear terminal
            _terminal.Clear();
            
            // Draw title box
            _terminal.DrawBox(2, 2, 76, 5, Cathedral.Terminal.Utils.BoxStyle.Double, 
                              Cathedral.Terminal.Utils.Colors.Cyan, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.CenteredText(3, "CATHEDRAL GLYPH SPHERE", Cathedral.Terminal.Utils.Colors.Yellow, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.CenteredText(4, "Terminal HUD Integration Demo", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.CenteredText(5, "Click on terminal cells or sphere vertices to interact", Cathedral.Terminal.Utils.Colors.Gray, Cathedral.Terminal.Utils.Colors.Black);
            
            // Draw info panel
            _terminal.DrawBox(2, 8, 35, 10, Cathedral.Terminal.Utils.BoxStyle.Single, 
                              Cathedral.Terminal.Utils.Colors.Green, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(3, 9, "Controls:", Cathedral.Terminal.Utils.Colors.Green, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(3, 10, "• Mouse: Rotate camera", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(3, 11, "• Scroll: Zoom in/out", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(3, 12, "• Click: Select vertex", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(3, 13, "• M: Toggle debug markers", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(3, 14, "• D: Cycle debug shaders", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(3, 15, "• Space: Avatar movement", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(3, 16, "• ESC: Exit application", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            
            // Draw status panel
            _terminal.DrawBox(40, 8, 38, 15, Cathedral.Terminal.Utils.BoxStyle.Single, 
                              Cathedral.Terminal.Utils.Colors.Blue, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(41, 9, "Status:", Cathedral.Terminal.Utils.Colors.Blue, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(41, 10, "Vertices: " + vertices.Count.ToString(), Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(41, 11, "Hovered: None", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(41, 12, "Selected: None", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            
            // Add window border information
            var (titleBarHeight, borderWidth, borderHeight) = GetWindowBorderInfo();
            _terminal.Text(41, 14, "Window Info:", Cathedral.Terminal.Utils.Colors.Blue, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(41, 15, $"Client: {ClientSize.X}x{ClientSize.Y}", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(41, 16, $"Total: {Size.X}x{Size.Y}", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(41, 17, $"Title bar: ~{titleBarHeight}px", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(41, 18, $"Borders: {borderWidth}x{borderHeight}", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            
            // Draw progress bar demo
            _terminal.Text(3, 20, "System Status:", Cathedral.Terminal.Utils.Colors.Yellow, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.ProgressBar(3, 21, 30, 75.0f, Cathedral.Terminal.Utils.Colors.Green, Cathedral.Terminal.Utils.Colors.DarkGray, Cathedral.Terminal.Utils.Colors.Black);
            _terminal.Text(35, 21, "75%", Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            
            // Footer
            _terminal.CenteredText(23, "Terminal HUD is now active - hover and click to test interaction", 
                                  Cathedral.Terminal.Utils.Colors.Gray, Cathedral.Terminal.Utils.Colors.Black);
        }
        */
        
        // DEPRECATED: Old terminal event handlers - replaced by game-specific handlers
        /*
        private void OnTerminalCellClicked(int x, int y)
        {
            Console.WriteLine($"Terminal: Cell clicked at ({x}, {y})");
            // Example: Set a marker on the clicked cell
            if (_terminal != null)
            {
                _terminal.SetCell(x, y, '@', Cathedral.Terminal.Utils.Colors.Yellow, Cathedral.Terminal.Utils.Colors.Red);
                // Update click coordinates in status panel
                _terminal.Text(41, 13, "Clicked: " + $"({x},{y})".PadRight(15), Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
            }
        }
        
        private void OnTerminalCellHovered(int x, int y)
        {
            // Show coordinates in top-left corner
            if (_terminal != null)
            {
                _terminal.Text(1, 1, $"Terminal: ({x:D2},{y:D2})", Cathedral.Terminal.Utils.Colors.Cyan, Cathedral.Terminal.Utils.Colors.Black);
            }
        }
        
        private void OnVertexHovered(int vertexIndex, OpenTK.Mathematics.Vector2 mousePos)
        {
            if (_terminal != null)
            {
                if (vertexIndex >= 0)
                {
                    _terminal.Text(41, 11, "Hovered: " + vertexIndex.ToString().PadRight(15), 
                                  Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
                    _terminal.Text(1, 1, $"Sphere: Vertex {vertexIndex}", Cathedral.Terminal.Utils.Colors.Yellow, Cathedral.Terminal.Utils.Colors.Black);
                }
                else
                {
                    _terminal.Text(41, 11, "Hovered: None".PadRight(15), 
                                  Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
                    _terminal.Text(1, 1, "Sphere: No vertex", Cathedral.Terminal.Utils.Colors.Gray, Cathedral.Terminal.Utils.Colors.Black);
                }
            }
        }
        
        private void OnVertexClicked(int vertexIndex, OpenTK.Mathematics.Vector2 mousePos)
        {
            if (_terminal != null)
            {
                _terminal.Text(41, 12, "Selected: " + vertexIndex.ToString().PadRight(15), 
                              Cathedral.Terminal.Utils.Colors.White, Cathedral.Terminal.Utils.Colors.Black);
                
                // Show vertex position information
                if (vertexIndex >= 0 && vertexIndex < vertices.Count)
                {
                    var pos = vertices[vertexIndex].Position;
                    _terminal.Text(41, 14, $"Pos: ({pos.X:F1},{pos.Y:F1},{pos.Z:F1})".PadRight(20), 
                                  Cathedral.Terminal.Utils.Colors.Cyan, Cathedral.Terminal.Utils.Colors.Black);
                }
            }
        }
        */
        
        #endregion

        private void SetupInstancedRendering()
        {
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            // Quad (two triangles) in local quad space [-0.5, -0.5] .. [0.5, 0.5]
            float[] quadVerts = new float[] {
                -0.5f, -0.5f, 0f, 0f,
                 0.5f, -0.5f, 1f, 0f,
                 0.5f,  0.5f, 1f, 1f,
                -0.5f,  0.5f, 0f, 1f
            };
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVerts.Length * sizeof(float), quadVerts, BufferUsageHint.StaticDraw);

            // attrib 0: local pos.xy (vec2)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            // attrib 1: local uv (vec2)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            // EBO
            uint[] quadIdx = new uint[] { 0, 1, 2, 2, 3, 0 };
            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, quadIdx.Length * sizeof(uint), quadIdx, BufferUsageHint.StaticDraw);

            // Instance buffer setup
            instanceVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVbo);
            int maxInstances = vertices.Count;
            GL.BufferData(BufferTarget.ArrayBuffer, maxInstances * 18 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

            int attribIndex = 2;
            int stride = 18 * sizeof(float);

            // instancePos vec3
            GL.EnableVertexAttribArray(attribIndex);
            GL.VertexAttribPointer(attribIndex, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.VertexAttribDivisor(attribIndex, 1); attribIndex++;

            // right vec3
            GL.EnableVertexAttribArray(attribIndex);
            GL.VertexAttribPointer(attribIndex, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.VertexAttribDivisor(attribIndex, 1); attribIndex++;

            // up vec3
            GL.EnableVertexAttribArray(attribIndex);
            GL.VertexAttribPointer(attribIndex, 3, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.VertexAttribDivisor(attribIndex, 1); attribIndex++;

            // size float
            GL.EnableVertexAttribArray(attribIndex);
            GL.VertexAttribPointer(attribIndex, 1, VertexAttribPointerType.Float, false, stride, 9 * sizeof(float));
            GL.VertexAttribDivisor(attribIndex, 1); attribIndex++;

            // uvRect vec4
            GL.EnableVertexAttribArray(attribIndex);
            GL.VertexAttribPointer(attribIndex, 4, VertexAttribPointerType.Float, false, stride, 10 * sizeof(float));
            GL.VertexAttribDivisor(attribIndex, 1); attribIndex++;

            // color vec4
            GL.EnableVertexAttribArray(attribIndex);
            GL.VertexAttribPointer(attribIndex, 4, VertexAttribPointerType.Float, false, stride, 14 * sizeof(float));
            GL.VertexAttribDivisor(attribIndex, 1); attribIndex++;

            GL.BindVertexArray(0);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // Input -> camera control
            HandleInput(args);

            // Update timing for interface animations
            updateTimer += (float)args.Time;
            if (updateTimer >= UPDATE_INTERVAL)
            {
                UpdateRequested?.Invoke(updateTimer);
                updateTimer = 0.0f;
            }

            // Update debug info
            UpdateDebugInfo();

            // Update instances for dynamic color changes (hover highlighting)
            UpdateInstanceBuffer();

            // Clear
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Build view & proj
            var view = GetViewMatrix();
            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), (float)Size.X / Size.Y, 0.01f, 100f);

            // Render background sphere first (opaque backdrop)
            RenderBackgroundSphere(view, proj);

            // Render main sphere
            RenderGlyphSphere(view, proj);

            // Render debug markers if enabled
            if (debugShowMarkers)
            {
                RenderDebugMarkers(view, proj);
            }

            // Render terminal HUD overlay
            if (_terminal != null)
            {
                _terminal.Render(Size);
            }
            
            // Render popup terminal on top of everything
            if (_popupTerminal != null)
            {
                _popupTerminal.Render(Size);
            }

            SwapBuffers();
        }

        private void HandleInput(FrameEventArgs args)
        {
            // Let camera handle its input first
            bool cameraHandled = _camera.ProcessInput(KeyboardState, args);

            // Debug shader switching (D key)
            if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D))
            {
                debugShaderMode = (debugShaderMode + 1) % 5;
                string description = debugShaderMode switch
                {
                    0 => "Normal rendering (texture + vertex colors)",
                    1 => "Vertex colors only (no texture masking)",
                    2 => "Texture only (white on black)",
                    3 => "Wireframe/Debug view",
                    4 => "Grayscale (luminosity-based grayscale)",
                    _ => "Unknown"
                };
                Console.WriteLine($"Debug shader mode: {debugShaderMode} - {description}");
            }

            // Debug markers toggle (M key)
            if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.M))
            {
                debugShowMarkers = !debugShowMarkers;
                Console.WriteLine($"Debug markers: {(debugShowMarkers ? "ON" : "OFF")}");
            }

            // Center camera on avatar (SPACE key)
            if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space))
            {
                if (debugAvatarVertex >= 0)
                {
                    CenterCameraOnGlyph(debugAvatarVertex);
                    Console.WriteLine($"🎯 Camera re-centered on avatar at vertex {debugAvatarVertex} (Press SPACE anytime to re-focus)");
                }
                else
                {
                    Console.WriteLine("❌ No avatar found - cannot center camera. Avatar must be placed first.");
                }
            }
        }

        private void RenderBackgroundSphere(Matrix4 view, Matrix4 proj)
        {
            GL.UseProgram(backgroundProgram);
            var model = Matrix4.Identity;
            int bgViewLoc = GL.GetUniformLocation(backgroundProgram, "uView");
            int bgProjLoc = GL.GetUniformLocation(backgroundProgram, "uProj");
            int bgModelLoc = GL.GetUniformLocation(backgroundProgram, "uModel");
            GL.UniformMatrix4(bgViewLoc, false, ref view);
            GL.UniformMatrix4(bgProjLoc, false, ref proj);
            GL.UniformMatrix4(bgModelLoc, false, ref model);
            
            GL.BindVertexArray(backgroundVao);
            GL.DrawElements(PrimitiveType.Triangles, backgroundIndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);
        }

        private void RenderGlyphSphere(Matrix4 view, Matrix4 proj)
        {
            // Select shader program based on debug mode
            int currentProgram = debugShaderMode switch
            {
                0 => program,           // normal rendering
                1 => debugProgram1,     // vertex colors only
                2 => debugProgram2,     // texture only
                3 => debugProgram3,     // wireframe
                4 => debugProgram4,     // grayscale
                _ => program
            };
            
            GL.UseProgram(currentProgram);
            
            int viewLoc = GL.GetUniformLocation(currentProgram, "uView");
            int projLoc = GL.GetUniformLocation(currentProgram, "uProj");
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref proj);
            
            // Set texture uniform for current program
            int texLoc = GL.GetUniformLocation(currentProgram, "uAtlas");
            if (texLoc >= 0)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, glyphTexture);
                GL.Uniform1(texLoc, 0);
            }

            // Draw instanced quads
            GL.BindVertexArray(vao);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero, instanceCount);
            GL.BindVertexArray(0);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            
            var mouse = MousePosition;
            
            // Update popup terminal position (only if not in fixed mode)
            if (_popupTerminal != null)
            {
                _popupTerminal.SetMousePosition(mouse);
            }
            
            // Handle terminal input first (HUD takes priority)
            if (_terminal != null)
            {
                _terminal.HandleMouseMove(mouse, Size);
            }
            
            var (rayOrig, rayDir) = GetMouseRay(mouse);

            // Store debug info
            debugRayOrigin = rayOrig;
            debugRayDirection = rayDir;
            debugMousePos = mouse;

            // Find hovered vertex (only if mouse is not over terminal)
            if (_terminal == null || !_terminal.IsPositionInTerminal(mouse, Size))
            {
                int newHover = FindVertexByMagentaRayIntersection(mouse);
                
                if (newHover == -1)
                {
                    newHover = FindClosestVertexInScreenSpace(mouse);
                }
                
                if (newHover != hoveredVertexIndex)
                {
                    hoveredVertexIndex = newHover;
                    
                    // Fire event for interface (including -1 for "no hover")
                    VertexHovered?.Invoke(newHover, mouse);
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Console.WriteLine($"Mouse button pressed: {e.Button}");
            
            var mouse = MousePosition;
            
            // Handle terminal input first (HUD takes priority)
            if (_terminal != null && _terminal.HandleMouseDown(mouse, Size, e.Button))
            {
                Console.WriteLine("Terminal handled mouse click");
                return; // Terminal handled the event
            }
            
            if (e.Button == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left)
            {
                Console.WriteLine("Left mouse button detected - processing 3D interaction");
                Console.WriteLine($"Mouse position: {mouse}");

                // Use the same vertex detection method as hover detection
                int hitIdx = FindVertexByMagentaRayIntersection(mouse);
                
                if (hitIdx == -1)
                {
                    hitIdx = FindClosestVertexInScreenSpace(mouse);
                }
                
                Console.WriteLine($"Hit vertex index: {hitIdx}");
                if (hitIdx >= 0)
                {
                    Console.WriteLine($"Firing VertexClicked event for vertex {hitIdx}");
                    // Fire event for interface
                    VertexClicked?.Invoke(hitIdx, mouse);
                }
                else
                {
                    Console.WriteLine("No vertex hit");
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            
            // Handle terminal input
            if (_terminal != null)
            {
                _terminal.HandleMouseUp(e.Button);
            }
        }

        // Mouse ray casting and vertex finding methods remain the same
        private int FindVertexByMagentaRayIntersection(OpenTK.Mathematics.Vector2 mousePos)
        {
            Vector3 mouseProjection = GetMouseProjectionOnScreen(mousePos);
            
            Vector3 cameraPos = _camera.GetCameraPosition();
            
            Vector3 rayOrigin = cameraPos;
            Vector3 rayDirection = Vector3.Normalize(mouseProjection - cameraPos);
            
            Vector3 sphereCenter = Vector3.Zero;
            float sphereRadius = SPHERE_RADIUS;
            
            Vector3 oc = rayOrigin - sphereCenter;
            float a = Vector3.Dot(rayDirection, rayDirection);
            float b = 2.0f * Vector3.Dot(oc, rayDirection);
            float c = Vector3.Dot(oc, oc) - sphereRadius * sphereRadius;
            
            float discriminant = b * b - 4 * a * c;
            
            if (discriminant < 0) return -1;
            
            float sqrtDiscriminant = MathF.Sqrt(discriminant);
            float t = (-b - sqrtDiscriminant) / (2.0f * a);
            
            if (t <= 0) return -1;
            
            Vector3 intersectionPoint = rayOrigin + rayDirection * t;
            
            float closestDist = float.MaxValue;
            int closestVertex = -1;
            
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertexPos = vertices[i].Position;
                float dist = Vector3.Distance(intersectionPoint, vertexPos);
                
                float maxDist = QUAD_SIZE * VERTEX_SHADER_SIZE_MULTIPLIER;
                if (dist <= maxDist && dist < closestDist)
                {
                    closestDist = dist;
                    closestVertex = i;
                }
            }
            
            return closestVertex;
        }

        private int FindClosestVertexInScreenSpace(OpenTK.Mathematics.Vector2 mousePos)
        {
            var view = GetViewMatrix();
            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), (float)Size.X / Size.Y, 0.01f, 100f);
            var viewProj = view * proj;
            
            float bestDist = float.MaxValue;
            int best = -1;
            
            for (int i = 0; i < vertices.Count; i++)
            {
                var worldPos = new Vector4(vertices[i].Position, 1.0f);
                var clipPos = worldPos * viewProj;
                
                if (clipPos.W <= 0) continue;
                
                var ndc = new Vector3(clipPos.X / clipPos.W, clipPos.Y / clipPos.W, clipPos.Z / clipPos.W);
                
                if (ndc.X < -1 || ndc.X > 1 || ndc.Y < -1 || ndc.Y > 1) continue;
                
                var screenX = (ndc.X + 1.0f) * 0.5f * Size.X;
                var screenY = (1.0f - ndc.Y) * 0.5f * Size.Y;
                
                float dx = screenX - mousePos.X;
                float dy = screenY - mousePos.Y;
                float dist = dx * dx + dy * dy;
                
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }
            
            return (bestDist < 20 * 20) ? best : -1;
        }

        private int RayPickNearestVertex(Vector3 rayO, Vector3 rayD, float pickRadius)
        {
            float bestT = float.MaxValue;
            int best = -1;
            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i].Position;
                Vector3 w = v - rayO;
                float proj = Vector3.Dot(w, rayD);
                if (proj < 0) continue;
                Vector3 closest = rayO + rayD * proj;
                float d2 = Vector3.DistanceSquared(closest, v);
                if (d2 < pickRadius * pickRadius && proj < bestT)
                {
                    bestT = proj;
                    best = i;
                }
            }
            return best;
        }

        private (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay(OpenTK.Mathematics.Vector2 mousePos)
        {
            // Use the Camera class method for mouse ray calculation
            var (rayOrigin, rayDirection) = _camera.GetMouseRay(new OpenTK.Mathematics.Vector2(mousePos.X, mousePos.Y), Size.X, Size.Y);
            return (rayOrigin, rayDirection);
        }

        // Legacy method for compatibility - TODO: Remove when all callers updated
        private (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay_Legacy(OpenTK.Mathematics.Vector2 mousePos)
        {
            float x = (2.0f * mousePos.X) / Size.X - 1.0f;
            float y = 1.0f - (2.0f * mousePos.Y) / Size.Y;
            
            Vector3 camDir = _camera.GetCameraDirection();
            Vector3 rayOrigin = _camera.GetCameraPosition();
            
            Vector3 up = Vector3.UnitY;
            Vector3 right = Vector3.Normalize(Vector3.Cross(camDir, up));
            Vector3 cameraUp = Vector3.Cross(right, camDir);
            
            float fovY = MathHelper.DegreesToRadians(60f);
            float aspect = (float)Size.X / Size.Y;
            
            float tanHalfFov = MathF.Tan(fovY / 2.0f);
            Vector3 rayDirection = Vector3.Normalize(
                camDir + 
                (right * x * tanHalfFov * aspect) + 
                (cameraUp * y * tanHalfFov)
            );
            
            return (rayOrigin, rayDirection);
        }

        // All sphere building, camera, and debug methods remain the same...
        // [Include all the remaining methods from the original file]

        private void BuildSphere(int subdivisions, int unused, float radius)
        {
            vertices.Clear();
            indices.Clear();

            BuildIcosphere(subdivisions, radius);

            // Initialize all vertices with default green dots
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = new Vertex 
                { 
                    Position = vertices[i].Position,
                    GlyphIndex = 0,
                    GlyphChar = DEFAULT_GLYPH,
                    Noise = 0,
                    Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // Green color
                    Size = 1.0f // Default size
                };
            }

            instanceCount = vertices.Count;
            Console.WriteLine($"Generated {vertices.Count} vertices with default green dots");
        }

        private void BuildIcosphere(int subdivisions, float radius)
        {
            // Create base icosahedron
            float t = (1.0f + MathF.Sqrt(5.0f)) / 2.0f; // golden ratio
            float scale = radius / MathF.Sqrt(1 + t * t);

            // 12 vertices of icosahedron
            var baseVertices = new List<Vector3>
            {
                new Vector3(-1,  t,  0) * scale,
                new Vector3( 1,  t,  0) * scale,
                new Vector3(-1, -t,  0) * scale,
                new Vector3( 1, -t,  0) * scale,
                
                new Vector3( 0, -1,  t) * scale,
                new Vector3( 0,  1,  t) * scale,
                new Vector3( 0, -1, -t) * scale,
                new Vector3( 0,  1, -t) * scale,
                
                new Vector3( t,  0, -1) * scale,
                new Vector3( t,  0,  1) * scale,
                new Vector3(-t,  0, -1) * scale,
                new Vector3(-t,  0,  1) * scale
            };

            // Normalize to sphere surface
            for (int i = 0; i < baseVertices.Count; i++)
            {
                baseVertices[i] = Vector3.Normalize(baseVertices[i]) * radius;
            }

            // 20 triangular faces of icosahedron
            var baseIndices = new List<uint>
            {
                // 5 faces around point 0
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                
                // 5 adjacent faces
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                
                // 5 faces around point 3
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                
                // 5 adjacent faces
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            // Start with base icosahedron
            var currentVertices = new List<Vector3>(baseVertices);
            var currentIndices = new List<uint>(baseIndices);

            // Subdivide
            for (int level = 0; level < subdivisions; level++)
            {
                var newVertices = new List<Vector3>(currentVertices);
                var newIndices = new List<uint>();
                var midPointCache = new Dictionary<(int, int), int>();

                // Process each triangle
                for (int i = 0; i < currentIndices.Count; i += 3)
                {
                    uint i1 = currentIndices[i];
                    uint i2 = currentIndices[i + 1];
                    uint i3 = currentIndices[i + 2];

                    // Get midpoints (or create them)
                    int a = GetMidpoint(i1, i2, currentVertices, newVertices, midPointCache, radius);
                    int b = GetMidpoint(i2, i3, currentVertices, newVertices, midPointCache, radius);
                    int c = GetMidpoint(i3, i1, currentVertices, newVertices, midPointCache, radius);

                    // Create 4 new triangles from 1 old triangle
                    newIndices.AddRange(new uint[] { i1, (uint)a, (uint)c });
                    newIndices.AddRange(new uint[] { i2, (uint)b, (uint)a });
                    newIndices.AddRange(new uint[] { i3, (uint)c, (uint)b });
                    newIndices.AddRange(new uint[] { (uint)a, (uint)b, (uint)c });
                }

                currentVertices = newVertices;
                currentIndices = newIndices;
            }

            // Convert to vertex objects
            vertices.Clear();
            indices.Clear();

            foreach (var pos in currentVertices)
            {
                vertices.Add(new Vertex { Position = pos, GlyphIndex = 0, GlyphChar = DEFAULT_GLYPH, Noise = 0, Color = Vector4.One, Size = 1.0f });
            }

            indices.AddRange(currentIndices);
        }

        private int GetMidpoint(uint i1, uint i2, List<Vector3> oldVertices, List<Vector3> newVertices, Dictionary<(int, int), int> cache, float radius)
        {
            var key = i1 < i2 ? ((int)i1, (int)i2) : ((int)i2, (int)i1);

            if (cache.TryGetValue(key, out int cachedIndex))
            {
                return cachedIndex;
            }

            Vector3 mid = (oldVertices[(int)i1] + oldVertices[(int)i2]) / 2.0f;
            mid = Vector3.Normalize(mid) * radius;

            int newIndex = newVertices.Count;
            newVertices.Add(mid);
            cache[key] = newIndex;

            return newIndex;
        }

        private void BuildBackgroundSphere(int subdivisions, int unused, float radius)
        {
            var backgroundVertices = new List<Vector3>();
            var backgroundIndices = new List<uint>();

            BuildIcosphereGeometry(subdivisions, radius, backgroundVertices, backgroundIndices);

            backgroundIndexCount = backgroundIndices.Count;

            backgroundVao = GL.GenVertexArray();
            backgroundVbo = GL.GenBuffer();
            backgroundEbo = GL.GenBuffer();

            GL.BindVertexArray(backgroundVao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, backgroundVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, backgroundVertices.Count * 3 * sizeof(float), backgroundVertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, backgroundEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, backgroundIndices.Count * sizeof(uint), backgroundIndices.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindVertexArray(0);
        }

        private void BuildIcosphereGeometry(int subdivisions, float radius, List<Vector3> outVertices, List<uint> outIndices)
        {
            // Same as BuildIcosphere but outputs to provided lists
            float t = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;
            float scale = radius / MathF.Sqrt(1 + t * t);

            var baseVertices = new List<Vector3>
            {
                new Vector3(-1,  t,  0) * scale, new Vector3( 1,  t,  0) * scale,
                new Vector3(-1, -t,  0) * scale, new Vector3( 1, -t,  0) * scale,
                new Vector3( 0, -1,  t) * scale, new Vector3( 0,  1,  t) * scale,
                new Vector3( 0, -1, -t) * scale, new Vector3( 0,  1, -t) * scale,
                new Vector3( t,  0, -1) * scale, new Vector3( t,  0,  1) * scale,
                new Vector3(-t,  0, -1) * scale, new Vector3(-t,  0,  1) * scale
            };

            for (int i = 0; i < baseVertices.Count; i++)
            {
                baseVertices[i] = Vector3.Normalize(baseVertices[i]) * radius;
            }

            var baseIndices = new List<uint>
            {
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            var currentVertices = new List<Vector3>(baseVertices);
            var currentIndices = new List<uint>(baseIndices);

            for (int level = 0; level < subdivisions; level++)
            {
                var newVertices = new List<Vector3>(currentVertices);
                var newIndices = new List<uint>();
                var midPointCache = new Dictionary<(int, int), int>();

                for (int i = 0; i < currentIndices.Count; i += 3)
                {
                    uint i1 = currentIndices[i], i2 = currentIndices[i + 1], i3 = currentIndices[i + 2];
                    int a = GetMidpoint(i1, i2, currentVertices, newVertices, midPointCache, radius);
                    int b = GetMidpoint(i2, i3, currentVertices, newVertices, midPointCache, radius);
                    int c = GetMidpoint(i3, i1, currentVertices, newVertices, midPointCache, radius);
                    newIndices.AddRange(new uint[] { i1, (uint)a, (uint)c, i2, (uint)b, (uint)a, i3, (uint)c, (uint)b, (uint)a, (uint)b, (uint)c });
                }
                currentVertices = newVertices;
                currentIndices = newIndices;
            }

            outVertices.AddRange(currentVertices);
            outIndices.AddRange(currentIndices);
        }

        private Matrix4 GetViewMatrix()
        {
            return _camera.GetViewMatrix();
        }

        private void UpdateInstanceBuffer()
        {
            instanceCount = vertices.Count;
            float[] data = new float[instanceCount * 18];
            for (int i = 0; i < instanceCount; i++)
            {
                var v = vertices[i];

                var normal = Vector3.Normalize(v.Position);
                Vector3 pole = Vector3.UnitY;
                var poleProj = pole - normal * Vector3.Dot(pole, normal);
                if (poleProj.LengthSquared < 1e-6f)
                    poleProj = Vector3.Cross(normal, Vector3.UnitX);
                poleProj = Vector3.Normalize(poleProj);
                var right = Vector3.Normalize(Vector3.Cross(poleProj, normal));
                var up = poleProj;
                float size = QUAD_SIZE * v.Size;

                var gi = glyphInfos[v.GlyphIndex];
                var uvx = gi.UvX; var uvy = gi.UvY; var uvw = gi.UvW; var uvh = gi.UvH;

                Vector4 col = v.Color;
                if (i == hoveredVertexIndex)
                {
                    col = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
                }

                int baseIdx = i * 18;
                data[baseIdx + 0] = v.Position.X; data[baseIdx + 1] = v.Position.Y; data[baseIdx + 2] = v.Position.Z;
                data[baseIdx + 3] = right.X; data[baseIdx + 4] = right.Y; data[baseIdx + 5] = right.Z;
                data[baseIdx + 6] = up.X; data[baseIdx + 7] = up.Y; data[baseIdx + 8] = up.Z;
                data[baseIdx + 9] = size;
                data[baseIdx + 10] = uvx; data[baseIdx + 11] = uvy; data[baseIdx + 12] = uvw; data[baseIdx + 13] = uvh;
                data[baseIdx + 14] = col.X; data[baseIdx + 15] = col.Y; data[baseIdx + 16] = col.Z; data[baseIdx + 17] = col.W;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.Length * sizeof(float), data);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private GlyphInfo[] BuildGlyphAtlas(string glyphs, int cellSize, int fontPxSize, out Image<Rgba32> atlas)
        {
            int cols = glyphs.Length;
            int rows = 1;
            int atlasW = cols * cellSize;
            int atlasH = rows * cellSize;
            
            atlas = new Image<Rgba32>(atlasW, atlasH, Color.Transparent);

            Font font;
            try
            {
                var coll = new FontCollection();
                string fontPath = "assets/fonts/FreeMono.ttf";
                if (System.IO.File.Exists(fontPath))
                {
                    var fam = coll.Add(fontPath);
                    font = fam.CreateFont(fontPxSize, FontStyle.Regular);
                }
                else
                {
                    throw new FileNotFoundException($"Required font not found: {fontPath}");
                }
            }
            catch
            {
                font = SystemFonts.CreateFont("Consolas", fontPxSize, FontStyle.Regular);
            }

            var infos = new GlyphInfo[glyphs.Length];
            for (int i = 0; i < glyphs.Length; i++)
            {
                int cx = i % cols;
                int cy = i / cols;
                var glyph = glyphs[i].ToString();
                int x = cx * cellSize;
                int y = cy * cellSize;

                atlas.Mutate(ctx =>
                {
                    var textOptions = new RichTextOptions(font)
                    {
                        Origin = new PointF(x + cellSize / 2f, y + cellSize / 2f),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    ctx.DrawText(textOptions, glyph, Color.White);
                });

                infos[i] = new GlyphInfo
                {
                    Glyph = glyphs[i],
                    UvX = (float)x / atlasW,
                    UvY = (float)y / atlasH,
                    UvW = (float)cellSize / atlasW,
                    UvH = (float)cellSize / atlasH
                };
            }

            return infos;
        }

        private int LoadTexture(Image<Rgba32> img)
        {
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            img.Mutate(x => x.Flip(FlipMode.Vertical));
            var pixels = new Rgba32[img.Width * img.Height];
            img.CopyPixelDataTo(pixels);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, img.Width, img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            
            return tex;
        }

        private int CreateProgram(string vsSrc, string fsSrc)
        {
            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vsSrc);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int status);
            if (status == 0) throw new Exception("VS compile: " + GL.GetShaderInfoLog(vs));

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fsSrc);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out status);
            if (status == 0) throw new Exception("FS compile: " + GL.GetShaderInfoLog(fs));

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, vs);
            GL.AttachShader(prog, fs);
            GL.LinkProgram(prog);
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out status);
            if (status == 0) throw new Exception("Linker: " + GL.GetProgramInfoLog(prog));

            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
            return prog;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
            UpdateProjection();
            
            // Force terminal to recalculate its layout immediately
            if (_terminal != null)
            {
                _terminal.ForceRefresh();
            }
            
            // Log border info on resize (useful for debugging terminal positioning)
            var (titleBarHeight, borderWidth, borderHeight) = GetWindowBorderInfo();
            Console.WriteLine($"Window resized - Client: {ClientSize}, Total: {Size}, Title bar: ~{titleBarHeight}px");
        }

        private void UpdateProjection()
        {
            // Implementation if needed
        }

        // Debug methods and other utilities...
        private void SetupDebugRendering()
        {
            debugVao = GL.GenVertexArray();
            debugVbo = GL.GenBuffer();
            
            GL.BindVertexArray(debugVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, debugVbo);
            
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            GL.BindVertexArray(0);
        }

        private void UpdateDebugInfo()
        {
            debugCameraPos = _camera.GetCameraPosition();

            if (debugRayDirection != Vector3.Zero)
            {
                Vector3 oc = debugRayOrigin;
                float a = Vector3.Dot(debugRayDirection, debugRayDirection);
                float b = 2.0f * Vector3.Dot(oc, debugRayDirection);
                float c = Vector3.Dot(oc, oc) - SPHERE_RADIUS * SPHERE_RADIUS;
                
                float discriminant = b * b - 4 * a * c;
                if (discriminant >= 0)
                {
                    float t1 = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
                    float t2 = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);
                    float t = (t1 > 0) ? t1 : t2;
                    if (t > 0)
                    {
                        debugIntersectionPoint = debugRayOrigin + debugRayDirection * t;
                    }
                }
            }
        }

        private void RenderDebugMarkers(Matrix4 view, Matrix4 proj)
        {
            GL.UseProgram(debugProgram);
            
            int viewLoc = GL.GetUniformLocation(debugProgram, "uView");
            int projLoc = GL.GetUniformLocation(debugProgram, "uProj");
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref proj);
            
            debugVertices.Clear();
            debugColors.Clear();
            
            AddScreenCanvasGrid();
            
            // Add camera-to-avatar debug ray every frame
            if (debugAvatarVertex >= 0)
            {
                AddDebugCameraToAvatarRay(debugAvatarVertex);
            }
            
            if (debugMousePos != OpenTK.Mathematics.Vector2.Zero)
            {
                Vector3 mouseProjection = GetMouseProjectionOnScreen(debugMousePos);
                debugVertices.Add(mouseProjection);
                debugColors.Add(new Vector3(1, 0, 0));
                
                AddMouseOrthogonalRay(mouseProjection);
                AddSphereIntersectionMarker(mouseProjection);
                AddCameraToMouseRay(mouseProjection);
                AddMagentaRaySphereIntersection(mouseProjection);
            }
            
            if (debugVertices.Count > 0)
            {
                float[] data = new float[debugVertices.Count * 6];
                for (int i = 0; i < debugVertices.Count; i++)
                {
                    data[i * 6 + 0] = debugVertices[i].X;
                    data[i * 6 + 1] = debugVertices[i].Y;
                    data[i * 6 + 2] = debugVertices[i].Z;
                    data[i * 6 + 3] = debugColors[i].X;
                    data[i * 6 + 4] = debugColors[i].Y;
                    data[i * 6 + 5] = debugColors[i].Z;
                }
                
                GL.BindVertexArray(debugVao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, debugVbo);
                GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);
                
                GL.PointSize(10.0f);
                GL.DrawArrays(PrimitiveType.Points, 0, debugVertices.Count);
                GL.DrawArrays(PrimitiveType.Lines, 0, debugVertices.Count);
                
                GL.BindVertexArray(0);
            }
        }

        private Vector3 GetMouseProjectionOnScreen(OpenTK.Mathematics.Vector2 mousePos)
        {
            float pixelOffsetY = GetWindowBorderHeight();
            float correctedMouseY = mousePos.Y + pixelOffsetY;
            
            float x = (2.0f * mousePos.X) / Size.X - 1.0f;
            float y = 1.0f - (2.0f * correctedMouseY) / Size.Y;
            
            Vector3 camDir = _camera.GetCameraDirection();
            Vector3 cameraPos = _camera.GetCameraPosition();
            
            Vector3 up = Vector3.UnitY;
            Vector3 right = Vector3.Normalize(Vector3.Cross(camDir, up));
            Vector3 cameraUp = Vector3.Cross(right, camDir);
            
            float fovY = MathHelper.DegreesToRadians(60f);
            float aspect = (float)Size.X / Size.Y;
            float nearDist = 1.0f;
            
            float tanHalfFov = MathF.Tan(fovY / 2.0f);
            float screenHeight = 2.0f * tanHalfFov * nearDist;
            float screenWidth = screenHeight * aspect;
            
            Vector3 screenCenter = cameraPos + camDir * nearDist;
            
            Vector3 mouseOnScreen = screenCenter + 
                (right * x * screenWidth * 0.5f) + 
                (cameraUp * y * screenHeight * 0.5f);
            
            return mouseOnScreen;
        }

        // Debug visualization methods
        private void AddScreenCanvasGrid()
        {
            // Get camera properties
            Vector3 camDir = _camera.GetCameraDirection();
            Vector3 cameraPos = _camera.GetCameraPosition();
            
            // Calculate camera basis vectors
            Vector3 up = Vector3.UnitY;
            Vector3 right = Vector3.Normalize(Vector3.Cross(camDir, up));
            Vector3 cameraUp = Vector3.Cross(right, camDir);
            
            // Screen parameters
            float fovY = MathHelper.DegreesToRadians(60f);
            float aspect = (float)Size.X / Size.Y;
            float nearDist = 1.0f; // Distance from camera where we draw the screen
            
            float tanHalfFov = MathF.Tan(fovY / 2.0f);
            float screenHeight = 2.0f * tanHalfFov * nearDist;
            float screenWidth = screenHeight * aspect;
            
            // Screen center position
            Vector3 screenCenter = cameraPos + camDir * nearDist;
            
            // Screen corners
            Vector3 topLeft = screenCenter + (-right * screenWidth * 0.5f) + (cameraUp * screenHeight * 0.5f);
            Vector3 topRight = screenCenter + (right * screenWidth * 0.5f) + (cameraUp * screenHeight * 0.5f);
            Vector3 bottomLeft = screenCenter + (-right * screenWidth * 0.5f) + (cameraUp * -screenHeight * 0.5f);
            Vector3 bottomRight = screenCenter + (right * screenWidth * 0.5f) + (cameraUp * -screenHeight * 0.5f);
            
            // Draw screen border (white)
            Vector3 gridColor = new Vector3(1, 1, 1); // White
            AddDebugLine(topLeft, topRight, gridColor);
            AddDebugLine(topRight, bottomRight, gridColor);
            AddDebugLine(bottomRight, bottomLeft, gridColor);
            AddDebugLine(bottomLeft, topLeft, gridColor);
            
            // Draw grid lines
            int gridLines = 10;
            
            // Vertical lines
            for (int i = 1; i < gridLines; i++)
            {
                float t = (float)i / gridLines;
                Vector3 top = Vector3.Lerp(topLeft, topRight, t);
                Vector3 bottom = Vector3.Lerp(bottomLeft, bottomRight, t);
                AddDebugLine(top, bottom, gridColor * 0.5f); // Dimmed white
            }
            
            // Horizontal lines  
            for (int i = 1; i < gridLines; i++)
            {
                float t = (float)i / gridLines;
                Vector3 leftPoint = Vector3.Lerp(topLeft, bottomLeft, t);
                Vector3 rightPoint = Vector3.Lerp(topRight, bottomRight, t);
                AddDebugLine(leftPoint, rightPoint, gridColor * 0.5f); // Dimmed white
            }
            
            // Draw center cross (brighter white)
            Vector3 centerH1 = screenCenter + (-right * screenWidth * 0.1f);
            Vector3 centerH2 = screenCenter + (right * screenWidth * 0.1f);
            Vector3 centerV1 = screenCenter + (cameraUp * screenHeight * 0.1f);
            Vector3 centerV2 = screenCenter + (cameraUp * -screenHeight * 0.1f);
            AddDebugLine(centerH1, centerH2, gridColor);
            AddDebugLine(centerV1, centerV2, gridColor);
        }

        private void AddMouseOrthogonalRay(Vector3 mouseProjection)
        {
            // Get camera properties to calculate the screen normal
            Vector3 camDir = _camera.GetCameraDirection();
            Vector3 cameraPos = _camera.GetCameraPosition();
            
            // Calculate camera basis vectors to get screen normal
            Vector3 up = Vector3.UnitY;
            Vector3 right = Vector3.Normalize(Vector3.Cross(camDir, up));
            Vector3 cameraUp = Vector3.Cross(right, camDir);
            
            // Calculate the screen normal (perpendicular to the screen grid)
            Vector3 screenNormal = Vector3.Normalize(Vector3.Cross(right, cameraUp));
            
            // Make sure the normal points away from the camera (toward the scene)
            if (Vector3.Dot(screenNormal, camDir) < 0)
            {
                screenNormal = -screenNormal;
            }
            
            Vector3 rayDirection = screenNormal;
            
            // Create a ray extending both directions from the mouse projection point
            float rayLength = 100.0f; // Increased length to make it more visible
            Vector3 rayStart = mouseProjection - rayDirection * rayLength;
            Vector3 rayEnd = mouseProjection + rayDirection * rayLength;
            
            // Add the orthogonal ray as a cyan line
            AddDebugLine(rayStart, rayEnd, new Vector3(0, 1, 1)); // Cyan color
            
            // Optional: Add small markers at the endpoints to show the full extent
            debugVertices.Add(rayStart);
            debugColors.Add(new Vector3(0, 0.5f, 0.5f)); // Dark cyan dot at start
            debugVertices.Add(rayEnd);
            debugColors.Add(new Vector3(0, 0.5f, 0.5f)); // Dark cyan dot at end
        }

        private void AddSphereIntersectionMarker(Vector3 mouseProjection)
        {
            // Get camera properties for the ray direction
            Vector3 camDir = _camera.GetCameraDirection();
            Vector3 cameraPos = _camera.GetCameraPosition();
            
            // Calculate camera basis vectors to get screen normal
            Vector3 up = Vector3.UnitY;
            Vector3 right = Vector3.Normalize(Vector3.Cross(camDir, up));
            Vector3 cameraUp = Vector3.Cross(right, camDir);
            
            // Calculate the screen normal (same as in AddMouseOrthogonalRay)
            Vector3 screenNormal = Vector3.Normalize(Vector3.Cross(right, cameraUp));
            if (Vector3.Dot(screenNormal, camDir) < 0)
            {
                screenNormal = -screenNormal;
            }
            
            // The orthogonal ray starts from the mouse projection and goes in screen normal direction
            Vector3 rayOrigin = mouseProjection;
            Vector3 rayDirection = screenNormal;
            
            // Calculate ray-sphere intersection
            Vector3 sphereCenter = Vector3.Zero;
            float sphereRadius = SPHERE_RADIUS;
            
            // Ray-sphere intersection math
            Vector3 oc = rayOrigin - sphereCenter;
            float a = Vector3.Dot(rayDirection, rayDirection);
            float b = 2.0f * Vector3.Dot(oc, rayDirection);
            float c = Vector3.Dot(oc, oc) - sphereRadius * sphereRadius;
            
            float discriminant = b * b - 4 * a * c;
            
            if (discriminant >= 0)
            {
                // Calculate both intersection points
                float sqrtDiscriminant = MathF.Sqrt(discriminant);
                float t1 = (-b - sqrtDiscriminant) / (2.0f * a);
                float t2 = (-b + sqrtDiscriminant) / (2.0f * a);
                
                // Add markers for both intersection points (entry and exit)
                if (t1 > 0) // Only show if intersection is in front of ray origin
                {
                    Vector3 intersection1 = rayOrigin + rayDirection * t1;
                    debugVertices.Add(intersection1);
                    debugColors.Add(new Vector3(0, 1, 1)); // Cyan marker for sphere intersection (was yellow)
                }
                
                // Note: Only showing the first (closest) intersection to avoid unwanted line artifacts
            }
        }

        private void AddCameraToMouseRay(Vector3 mouseProjection)
        {
            // Get main camera position
            Vector3 cameraPos = _camera.GetCameraPosition();
            
            // Calculate ray direction from camera to mouse projection
            Vector3 rayDirection = Vector3.Normalize(mouseProjection - cameraPos);
            
            // Extend the ray in both directions
            float rayLength = 100.0f; // Same length as orthogonal ray
            Vector3 rayStart = cameraPos - rayDirection * rayLength;
            Vector3 rayEnd = mouseProjection + rayDirection * rayLength;
            
            // Add extended magenta ray
            AddDebugLine(rayStart, rayEnd, new Vector3(1, 0, 1)); // Magenta color
            
            // Add markers at key points
            debugVertices.Add(cameraPos);
            debugColors.Add(new Vector3(0.8f, 0, 0.8f)); // Dark magenta marker for camera position
            
            debugVertices.Add(rayStart);
            debugColors.Add(new Vector3(0.5f, 0, 0.5f)); // Dark magenta dot at ray start
            
            debugVertices.Add(rayEnd);
            debugColors.Add(new Vector3(0.5f, 0, 0.5f)); // Dark magenta dot at ray end
        }

        private void AddMagentaRaySphereIntersection(Vector3 mouseProjection)
        {
            // Get main camera position
            Vector3 cameraPos = _camera.GetCameraPosition();
            
            // Calculate ray from camera through mouse projection
            Vector3 rayOrigin = cameraPos;
            Vector3 rayDirection = Vector3.Normalize(mouseProjection - cameraPos);
            
            // Calculate ray-sphere intersection
            Vector3 sphereCenter = Vector3.Zero;
            float sphereRadius = SPHERE_RADIUS;
            
            // Ray-sphere intersection math
            Vector3 oc = rayOrigin - sphereCenter;
            float a = Vector3.Dot(rayDirection, rayDirection);
            float b = 2.0f * Vector3.Dot(oc, rayDirection);
            float c = Vector3.Dot(oc, oc) - sphereRadius * sphereRadius;
            
            float discriminant = b * b - 4 * a * c;
            
            if (discriminant >= 0)
            {
                // Calculate the closest intersection point (entry point)
                float sqrtDiscriminant = MathF.Sqrt(discriminant);
                float t1 = (-b - sqrtDiscriminant) / (2.0f * a);
                
                if (t1 > 0) // Only show if intersection is in front of camera
                {
                    Vector3 intersection = rayOrigin + rayDirection * t1;
                    debugVertices.Add(intersection);
                    debugColors.Add(new Vector3(0, 1, 0)); // Green marker for magenta ray intersection
                }
            }
        }

        private void AddDebugLine(Vector3 start, Vector3 end, Vector3 color)
        {
            debugVertices.Add(start);
            debugColors.Add(color);
            debugVertices.Add(end);
            debugColors.Add(color);
        }

        private void AddDebugCross(Vector3 center, float size, Vector3 color)
        {
            Vector3 right = Vector3.UnitX * size;
            Vector3 up = Vector3.UnitY * size;
            Vector3 forward = Vector3.UnitZ * size;
            
            // X axis
            debugVertices.Add(center - right);
            debugColors.Add(color);
            debugVertices.Add(center + right);
            debugColors.Add(color);
            
            // Y axis
            debugVertices.Add(center - up);
            debugColors.Add(color);
            debugVertices.Add(center + up);
            debugColors.Add(color);
            
            // Z axis
            debugVertices.Add(center - forward);
            debugColors.Add(color);
            debugVertices.Add(center + forward);
            debugColors.Add(color);
        }

        // Vertex and GlyphInfo structures
        private class Vertex
        {
            public Vector3 Position;
            public int GlyphIndex;
            public char GlyphChar;
            public float Noise;
            public Vector4 Color;
            public float Size; // Size factor from BiomeType/LocationType
        }

        private struct GlyphInfo
        {
            public char Glyph;
            public float UvX, UvY, UvW, UvH;
        }

        // Shader sources
        private readonly string vertexShaderSrc = $@"
#version 330 core
layout(location = 0) in vec2 aLocalPos;
layout(location = 1) in vec2 aLocalUV;
layout(location = 2) in vec3 iPos;
layout(location = 3) in vec3 iRight;
layout(location = 4) in vec3 iUp;
layout(location = 5) in float iSize;
layout(location = 6) in vec4 iUvRect;
layout(location = 7) in vec4 iColor;

uniform mat4 uView;
uniform mat4 uProj;

out vec2 vUv;
out vec4 vColor;

void main()
{{
    vec3 worldPos = iPos + (iRight * aLocalPos.x + iUp * aLocalPos.y) * iSize * {VERTEX_SHADER_SIZE_MULTIPLIER};
    gl_Position = uProj * uView * vec4(worldPos, 1.0);
    vUv = vec2(iUvRect.x + aLocalUV.x * iUvRect.z, iUvRect.y + aLocalUV.y * iUvRect.w);
    vColor = iColor;
}}";

        private readonly string fragmentShaderSrc = @"
#version 330 core
in vec2 vUv;
in vec4 vColor;
out vec4 FragColor;

uniform sampler2D uAtlas;

void main()
{
    vec4 texSample = texture(uAtlas, vUv);
    float luminance = dot(texSample.rgb, vec3(0.299, 0.587, 0.114));
    
    if (luminance > 0.1) {
        FragColor = vec4(vColor.rgb, 1.0);
    } else {
        discard;
    }
}";

        private readonly string debugFragmentSrc1 = @"
#version 330 core
in vec2 vUv;
in vec4 vColor;
out vec4 FragColor;
uniform sampler2D uAtlas;
void main() { FragColor = vColor; }";

        private readonly string debugFragmentSrc2 = @"
#version 330 core
in vec2 vUv;
in vec4 vColor;
out vec4 FragColor;
uniform sampler2D uAtlas;
void main() {
    vec4 texSample = texture(uAtlas, vUv);
    float luminance = dot(texSample.rgb, vec3(0.299, 0.587, 0.114));
    if (luminance > 0.1) { FragColor = vec4(1.0, 1.0, 1.0, 1.0); } else { discard; }
}";

        private readonly string debugFragmentSrc3 = @"
#version 330 core
in vec2 vUv;
in vec4 vColor;
out vec4 FragColor;
uniform sampler2D uAtlas;
void main() {
    vec4 texSample = texture(uAtlas, vUv);
    float luminance = dot(texSample.rgb, vec3(0.299, 0.587, 0.114));
    vec2 grid = abs(fract(vUv * 10.0) - 0.5);
    float line = smoothstep(0.0, 0.05, min(grid.x, grid.y));
    if (luminance > 0.1) { FragColor = vec4(mix(vec3(1.0, 0.0, 0.0), vColor.rgb, line), 1.0); } else { discard; }
}";

        private readonly string debugFragmentSrc4 = @"
#version 330 core
in vec2 vUv;
in vec4 vColor;
out vec4 FragColor;
uniform sampler2D uAtlas;
void main() {
    vec4 texSample = texture(uAtlas, vUv);
    float texLuminance = dot(texSample.rgb, vec3(0.299, 0.587, 0.114));
    
    if (texLuminance > 0.1) {
        // Compute luminosity of the original vertex color
        float colorLuminance = dot(vColor.rgb, vec3(0.299, 0.587, 0.114));
        // Use the luminosity as a grayscale value
        FragColor = vec4(colorLuminance, colorLuminance, colorLuminance, 1.0);
    } else {
        discard;
    }
}";

        private readonly string backgroundVertexShaderSrc = @"
#version 330 core
layout(location = 0) in vec3 aPosition;
uniform mat4 uView;
uniform mat4 uProj;
uniform mat4 uModel;
void main() { gl_Position = uProj * uView * uModel * vec4(aPosition, 1.0); }";

        private readonly string backgroundFragmentShaderSrc = @"
#version 330 core
out vec4 FragColor;
void main() { FragColor = vec4(0.0, 0.0, 0.0, 1.0); }";

        private readonly string debugVertexShaderSrc = @"
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;
uniform mat4 uView;
uniform mat4 uProj;
out vec3 vColor;
void main() { gl_Position = uProj * uView * vec4(aPosition, 1.0); vColor = aColor; }";

        private readonly string debugFragmentShaderSrc = @"
#version 330 core
in vec3 vColor;
out vec4 FragColor;
void main() { FragColor = vec4(vColor, 1.0); }";

        // Pathfinding methods
        private void InitializePathfinding()
        {
            _pathfindingService = new PathfindingService();
            _graph = new GlyphSphereGraph(this);
            
            var debugInfo = _graph.GetDebugInfo();
            Console.WriteLine($"Pathfinding initialized: {debugInfo.nodes} nodes, {debugInfo.edges} edges, {debugInfo.avgConnections:F1} avg connections per node");
        }

        /// <summary>
        /// Gets the pathfinding graph for the sphere
        /// </summary>
        public GlyphSphereGraph? GetGraph() => _graph;

        /// <summary>
        /// Gets the pathfinding service
        /// </summary>
        public PathfindingService? GetPathfindingService() => _pathfindingService;

        /// <summary>
        /// Requests pathfinding between two vertices asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<Cathedral.Pathfinding.Path?> FindPathAsync(int fromVertex, int toVertex)
        {
            if (_pathfindingService == null || _graph == null)
                throw new InvalidOperationException("Pathfinding not initialized");

            return _pathfindingService.FindPathAsync(_graph, fromVertex, toVertex);
        }

        /// <summary>
        /// Finds the closest vertex to a world position
        /// </summary>
        public int FindClosestVertex(Vector3 worldPosition)
        {
            if (_graph == null)
                return -1;

            var numericPos = new System.Numerics.Vector3(worldPosition.X, worldPosition.Y, worldPosition.Z);
            return _graph.FindClosestNode(numericPos);
        }

        /// <summary>
        /// Gets the triangle indices for the icosphere mesh
        /// </summary>
        public List<uint> GetTriangleIndices()
        {
            return new List<uint>(indices);
        }

        /// <summary>
        /// Centers the camera on a specific glyph, positioning it to focus on that location
        /// </summary>
        public void CenterCameraOnGlyph(int vertexIndex)
        {
            if (vertexIndex >= 0 && vertexIndex < vertices.Count)
            {
                Vector3 avatarPosition = GetVertexPosition(vertexIndex);
                
                // Use the camera's focus method to center on the glyph position
                _camera.FocusOnPosition(avatarPosition, true);
                
                // STEP 1: Position camera at avatar position (this we know works)
                // Calculate the direction from sphere center to avatar
                Vector3 fromCenterToAvatar = avatarPosition.Normalized();
                
                // STEP 2: Move camera back along the same line to create "drone above" effect
                // The camera should be on the line from center through avatar, but at original distance
                // Camera positioning handled by Camera class
                
                // STEP 3: Calculate camera angles to look toward sphere center from drone position
                // camDir should point from drone position toward sphere center
                // Camera direction handled by Camera class
                
                // Camera parameters updated by Camera class
                
                // Store avatar vertex for continuous debug ray rendering
                debugAvatarVertex = vertexIndex;
                
                // Verify the calculation
                Vector3 calculatedCameraPos = _camera.GetCameraPosition();
                
                Console.WriteLine($"� Camera positioned like drone above avatar");
                Console.WriteLine($"  Avatar position: {avatarPosition}");
                Console.WriteLine($"  Camera position: {calculatedCameraPos}");
                Console.WriteLine($"  Calculated camera pos: {calculatedCameraPos}");
                Console.WriteLine($"  Camera focused on target: {Vector3.Distance(avatarPosition, Vector3.Zero) > 0}");
                Console.WriteLine($"  Camera distance: {_camera.Distance:F2}");
                Console.WriteLine($"  Camera angles: yaw={_camera.Yaw:F1}°, pitch={_camera.Pitch:F1}°");
            }
        }

        /// <summary>
        /// Adds a debug ray from camera position to avatar glyph for visual debugging
        /// </summary>
        private void AddDebugCameraToAvatarRay(int avatarVertexIndex)
        {
            if (avatarVertexIndex >= 0 && avatarVertexIndex < vertices.Count)
            {
                // Get camera position from the Camera class
                Vector3 cameraPos = _camera.GetCameraPosition();
                Vector3 avatarPos = GetVertexPosition(avatarVertexIndex);
                
                // Camera-to-avatar debug ray: cyan camera marker, yellow avatar marker
                debugVertices.Add(cameraPos);
                debugColors.Add(new Vector3(0.0f, 1.0f, 1.0f)); // Cyan for camera position
                
                debugVertices.Add(avatarPos);
                debugColors.Add(new Vector3(1.0f, 1.0f, 0.0f)); // Yellow for avatar position (target)
            }
        }

        protected override void OnUnload()
        {
            _pathfindingService?.Dispose();
            _terminal?.Dispose();
            base.OnUnload();
        }
    }
}