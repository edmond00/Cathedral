using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Cathedral.Terminal.Utils;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Handles OpenGL rendering for the popup terminal that follows the mouse cursor.
    /// Similar to TerminalRenderer but with custom positioning logic.
    /// </summary>
    public class PopupRenderer : IDisposable
    {
        private readonly GlyphAtlas _atlas;
        private readonly TerminalView _view;
        
        // OpenGL objects
        private int _program;
        private int _vao, _vbo, _ebo;
        private int _instanceVbo;
        private TerminalInstance[] _instances;
        private bool _instanceBufferDirty;
        
        // Quad geometry (shared for all cells)
        private readonly float[] _quadVertices = {
            // Position   UV
            -0.5f, -0.5f, 0.0f, 0.0f,
             0.5f, -0.5f, 1.0f, 0.0f,
             0.5f,  0.5f, 1.0f, 1.0f,
            -0.5f,  0.5f, 0.0f, 1.0f
        };
        
        private readonly uint[] _quadIndices = {
            0, 1, 2,
            2, 3, 0
        };

        // Popup-specific parameters
        private Vector2 _mousePosition;
        private readonly int _cellPixelSize;
        private (int minX, int minY, int maxX, int maxY)? _visualBounds;
        private bool _disposed;

        public PopupRenderer(TerminalView view, GlyphAtlas atlas, int cellPixelSize)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
            _cellPixelSize = cellPixelSize;
            
            _instances = new TerminalInstance[_view.Width * _view.Height];
            _instanceBufferDirty = true;
            _mousePosition = Vector2.Zero;
            
            InitializeGL();
        }

        #region OpenGL Initialization

        private void InitializeGL()
        {
            // Load and compile shaders
            _program = CreateProgram();
            
            // Create quad geometry
            CreateQuadGeometry();
            
            // Create instance buffer
            CreateInstanceBuffer();
            
            Console.WriteLine("Popup Terminal: Renderer initialized successfully");
        }

        private int CreateProgram()
        {
            string vertexSource = LoadShaderSource("terminal.vert");
            string fragmentSource = LoadShaderSource("terminal.frag");
            
            int vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
            int fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);
            
            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            
            // Check linking status
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"Popup terminal shader program linking failed: {infoLog}");
            }
            
            // Clean up individual shaders
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            
            return program;
        }

        private int CreateShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int compileStatus);
            if (compileStatus == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Popup terminal {type} shader compilation failed: {infoLog}");
            }
            
            return shader;
        }

        private string LoadShaderSource(string filename)
        {
            string shaderPath = Path.Combine("src", "terminal", "Shaders", filename);
            if (File.Exists(shaderPath))
            {
                return File.ReadAllText(shaderPath);
            }
            
            // Fallback to embedded shaders if files don't exist
            switch (filename)
            {
                case "terminal.vert":
                    return GetEmbeddedVertexShader();
                case "terminal.frag":
                    return GetEmbeddedFragmentShader();
                default:
                    throw new FileNotFoundException($"Shader file not found: {shaderPath}");
            }
        }

        private void CreateQuadGeometry()
        {
            // Generate and bind VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            
            // Create vertex buffer
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _quadVertices.Length * sizeof(float), _quadVertices, BufferUsageHint.StaticDraw);
            
            // Create element buffer
            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _quadIndices.Length * sizeof(uint), _quadIndices, BufferUsageHint.StaticDraw);
            
            // Set vertex attributes (position and UV)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            
            GL.BindVertexArray(0);
        }

        private void CreateInstanceBuffer()
        {
            _instanceVbo = GL.GenBuffer();
            
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
            
            // Allocate buffer for all instances
            GL.BufferData(BufferTarget.ArrayBuffer, _instances.Length * TerminalInstance.SizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            
            // Set instance attributes
            int stride = TerminalInstance.SizeInBytes;
            
            // Position (location 2)
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.VertexAttribDivisor(2, 1);
            
            // Size (location 3)
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.VertexAttribDivisor(3, 1);
            
            // UV Rect (location 4)
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, stride, 5 * sizeof(float));
            GL.VertexAttribDivisor(4, 1);
            
            // Text Color (location 5)
            GL.EnableVertexAttribArray(5);
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, stride, 9 * sizeof(float));
            GL.VertexAttribDivisor(5, 1);
            
            // Background Color (location 6)
            GL.EnableVertexAttribArray(6);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, stride, 13 * sizeof(float));
            GL.VertexAttribDivisor(6, 1);
            
            GL.BindVertexArray(0);
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Renders the popup terminal at the current mouse position
        /// </summary>
        public void Render(Matrix4 projectionMatrix, Vector2i windowSize)
        {
            if (_disposed)
                return;

            // Update instance buffer if needed
            UpdateInstanceBuffer(windowSize);
            
            // Setup OpenGL state for HUD rendering
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            // Bind shader program
            GL.UseProgram(_program);
            
            // Set projection matrix
            int projLoc = GL.GetUniformLocation(_program, "uProjection");
            GL.UniformMatrix4(projLoc, false, ref projectionMatrix);
            
            // Bind atlas texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _atlas.TextureId);
            int atlasLoc = GL.GetUniformLocation(_program, "uGlyphAtlas");
            GL.Uniform1(atlasLoc, 0);
            
            // Bind vertex array
            GL.BindVertexArray(_vao);
            
            // Pass 1: Render backgrounds
            int renderPassLoc = GL.GetUniformLocation(_program, "uRenderPass");
            GL.Uniform1(renderPassLoc, 0);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, _quadIndices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero, _view.Width * _view.Height);
            
            // Pass 2: Render glyphs
            GL.Uniform1(renderPassLoc, 1);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, _quadIndices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero, _view.Width * _view.Height);
            
            // Clean up
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            
            // Restore OpenGL state
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
        }

        private void UpdateInstanceBuffer(Vector2i windowSize)
        {
            if (!_instanceBufferDirty && !_view.HasChanges)
                return;

            // Calculate popup layout
            CalculatePopupLayout(windowSize, out Vector2 cellSize, out Vector2 topLeft);
            
            // Update instance data for all cells
            int instanceIndex = 0;
            foreach (var (x, y, cell) in _view.EnumerateCells())
            {
                // Calculate screen position (top-left origin)
                Vector3 position = new Vector3(
                    topLeft.X + x * cellSize.X,
                    topLeft.Y + y * cellSize.Y,
                    0.0f
                );
                
                // Get glyph info
                var glyphInfo = _atlas.GetGlyph(cell.Character);
                
                // Create instance data
                _instances[instanceIndex] = new TerminalInstance(
                    position,
                    cellSize,
                    glyphInfo.UvRect,
                    cell.TextColor,
                    cell.BackgroundColor
                );
                
                instanceIndex++;
            }
            
            // Upload to GPU
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _instances.Length * TerminalInstance.SizeInBytes, _instances);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            
            _instanceBufferDirty = false;
            _view.MarkAllClean();
        }

        private void CalculatePopupLayout(Vector2i windowSize, out Vector2 cellSize, out Vector2 topLeft)
        {
            // Fixed cell size based on _cellPixelSize parameter
            cellSize = new Vector2(_cellPixelSize, _cellPixelSize);
            
            // Calculate popup dimensions
            float popupWidth = _view.Width * cellSize.X;
            float popupHeight = _view.Height * cellSize.Y;
            
            // Center the popup at mouse position (width/2, height/2) at mouse
            float centerX = _mousePosition.X;
            float centerY = _mousePosition.Y;
            
            // Calculate top-left corner
            float left = centerX - popupWidth / 2.0f;
            float top = centerY - popupHeight / 2.0f;
            
            // Screen edge clamping based on visual bounds (non-transparent cells)
            if (_visualBounds.HasValue)
            {
                var bounds = _visualBounds.Value;
                
                // Calculate pixel positions of visual content
                float visualLeft = left + bounds.minX * cellSize.X;
                float visualTop = top + bounds.minY * cellSize.Y;
                float visualRight = left + (bounds.maxX + 1) * cellSize.X;
                float visualBottom = top + (bounds.maxY + 1) * cellSize.Y;
                
                // Clamp based on visual content, not full popup size
                if (visualLeft < 0)
                    left -= visualLeft;
                if (visualTop < 0)
                    top -= visualTop;
                if (visualRight > windowSize.X)
                    left -= (visualRight - windowSize.X);
                if (visualBottom > windowSize.Y)
                    top -= (visualBottom - windowSize.Y);
            }
            
            topLeft = new Vector2(left, top);
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Updates the mouse position that the popup should follow
        /// </summary>
        public void SetMousePosition(Vector2 position)
        {
            _mousePosition = position;
            _instanceBufferDirty = true;
        }
        
        /// <summary>
        /// Sets the visual bounds (bounding box of non-transparent cells) for clamping
        /// </summary>
        public void SetVisualBounds((int minX, int minY, int maxX, int maxY)? bounds)
        {
            _visualBounds = bounds;
            _instanceBufferDirty = true;
        }

        /// <summary>
        /// Forces a complete refresh of the instance buffer
        /// </summary>
        public void ForceRefresh()
        {
            _instanceBufferDirty = true;
            _view.MarkAllDirty();
        }

        #endregion

        #region Embedded Shaders

        private string GetEmbeddedVertexShader()
        {
            return @"#version 330 core
layout(location = 0) in vec2 aLocalPos;
layout(location = 1) in vec2 aLocalUV;
layout(location = 2) in vec3 iPosition;
layout(location = 3) in vec2 iSize;
layout(location = 4) in vec4 iUvRect;
layout(location = 5) in vec4 iTextColor;
layout(location = 6) in vec4 iBgColor;

uniform mat4 uProjection;
uniform int uRenderPass;

out vec2 vUV;
out vec4 vTextColor;
out vec4 vBgColor;

void main()
{
    vec2 screenPos = iPosition.xy + aLocalPos * iSize;
    gl_Position = uProjection * vec4(screenPos, 0.0, 1.0);
    
    if (uRenderPass == 0) {
        vUV = vec2(0.0);
    } else {
        vUV = vec2(iUvRect.x + aLocalUV.x * iUvRect.z, 
                   iUvRect.y + aLocalUV.y * iUvRect.w);
    }
    
    vTextColor = iTextColor;
    vBgColor = iBgColor;
}";
        }

        private string GetEmbeddedFragmentShader()
        {
            return @"#version 330 core
in vec2 vUV;
in vec4 vTextColor;
in vec4 vBgColor;

uniform sampler2D uGlyphAtlas;
uniform int uRenderPass;

out vec4 FragColor;

void main()
{
    if (uRenderPass == 0) {
        FragColor = vBgColor;
    } else {
        vec4 atlasTexel = texture(uGlyphAtlas, vUV);
        float glyphAlpha = atlasTexel.r;
        
        if (glyphAlpha < 0.1) {
            discard;
        }
        
        FragColor = vec4(vTextColor.rgb, vTextColor.a * glyphAlpha);
    }
}";
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_program != 0)
                {
                    GL.DeleteProgram(_program);
                    _program = 0;
                }
                
                if (_vao != 0)
                {
                    GL.DeleteVertexArray(_vao);
                    _vao = 0;
                }
                
                if (_vbo != 0)
                {
                    GL.DeleteBuffer(_vbo);
                    _vbo = 0;
                }
                
                if (_ebo != 0)
                {
                    GL.DeleteBuffer(_ebo);
                    _ebo = 0;
                }
                
                if (_instanceVbo != 0)
                {
                    GL.DeleteBuffer(_instanceVbo);
                    _instanceVbo = 0;
                }
                
                _disposed = true;
            }
        }

        #endregion
    }
}
