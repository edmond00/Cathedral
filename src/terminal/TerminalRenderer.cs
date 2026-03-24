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
    /// Handles OpenGL rendering for the terminal using instanced rendering and dual-pass approach.
    /// First pass renders backgrounds, second pass renders character glyphs.
    /// </summary>
    public class TerminalRenderer : IDisposable
    {
        private readonly GlyphAtlas _atlas;
        private readonly TerminalView _view;
        
        // OpenGL objects
        private int _program;
        private int _vao, _vbo, _ebo;
        private int _instanceVbo;
        private TerminalInstance[] _instances;
        private bool _instanceBufferDirty;
        private float _darkenFactor = 1.0f;
        
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

        private bool _disposed;

        public TerminalRenderer(TerminalView view, GlyphAtlas atlas)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
            
            _instances = new TerminalInstance[_view.Width * _view.Height];
            _instanceBufferDirty = true;
            
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
            
            Console.WriteLine("Terminal: Renderer initialized successfully");
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
                throw new Exception($"Terminal shader program linking failed: {infoLog}");
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
                throw new Exception($"Terminal {type} shader compilation failed: {infoLog}");
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
        /// Renders the terminal with the specified projection matrix and window size
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
            
            // Set darken factor
            int darkenLoc = GL.GetUniformLocation(_program, "uDarkenFactor");
            GL.Uniform1(darkenLoc, _darkenFactor);
            
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

            // Calculate terminal layout
            CalculateTerminalLayout(windowSize, out Vector2 terminalSize, out Vector2 cellSize, out Vector2 offset);
            
            // Update instance data for all cells
            int instanceIndex = 0;
            foreach (var (x, y, cell) in _view.EnumerateCells())
            {
                // Calculate screen position (top-left origin)
                Vector3 position = new Vector3(
                    offset.X + x * cellSize.X,
                    offset.Y + y * cellSize.Y, // Direct mapping for screen coordinates
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

        private void CalculateTerminalLayout(Vector2i windowSize, out Vector2 terminalSize, out Vector2 cellSize, out Vector2 offset)
        {
            // Calculate target aspect ratio
            float targetAspect = (float)_view.Width / _view.Height;
            float windowAspect = (float)windowSize.X / windowSize.Y;
            
            if (windowAspect > targetAspect)
            {
                // Window is wider - fit to height
                terminalSize = new Vector2(windowSize.Y * targetAspect, windowSize.Y);
                offset = new Vector2((windowSize.X - terminalSize.X) * 0.5f, 0);
            }
            else
            {
                // Window is taller - fit to width
                terminalSize = new Vector2(windowSize.X, windowSize.X / targetAspect);
                offset = new Vector2(0, (windowSize.Y - terminalSize.Y) * 0.5f);
            }
            
            // Calculate cell size
            cellSize = new Vector2(terminalSize.X / _view.Width, terminalSize.Y / _view.Height);
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Forces a complete refresh of the instance buffer
        /// </summary>
        public void ForceRefresh()
        {
            _instanceBufferDirty = true;
            _view.MarkAllDirty();
        }
        
        /// <summary>
        /// Sets the darken factor for the terminal (0.0 = black, 1.0 = normal)
        /// </summary>
        public void SetDarkenFactor(float factor)
        {
            _darkenFactor = Math.Clamp(factor, 0.0f, 1.0f);
        }

        /// <summary>
        /// Gets information about the current terminal layout for the given window size
        /// </summary>
        public (Vector2 terminalSize, Vector2 cellSize, Vector2 offset) GetLayoutInfo(Vector2i windowSize)
        {
            CalculateTerminalLayout(windowSize, out Vector2 terminalSize, out Vector2 cellSize, out Vector2 offset);
            return (terminalSize, cellSize, offset);
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
uniform float uDarkenFactor;

out vec4 FragColor;

void main()
{
    if (uRenderPass == 0) {
        // Apply darken factor to background RGB
        FragColor = vec4(vBgColor.rgb * uDarkenFactor, vBgColor.a);
    } else {
        vec4 atlasTexel = texture(uGlyphAtlas, vUV);
        float glyphAlpha = atlasTexel.r;
        
        if (glyphAlpha < 0.1) {
            discard;
        }
        
        // Apply darken factor to text RGB
        FragColor = vec4(vTextColor.rgb * uDarkenFactor, vTextColor.a * glyphAlpha);
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