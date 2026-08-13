# Terminal OpenGL Implementation Design

## Overview

This document outlines the design for recreating the Unity-based terminal module using pure C#/OpenTK, following the existing GlyphSphere patterns. The new terminal will be a HUD overlay that can display a grid of characters with customizable colors and backgrounds, handle mouse interactions, and render efficiently using OpenGL shaders.

## Analysis of Existing Code

### Unity Terminal Features (Deprecated)
- **CharPixel.cs**: Individual character cells with text, background color, and font size
- **TerminalView.cs**: Grid management with PixelView objects containing character, foreground/background colors
- **Terminal.cs**: Main terminal with mouse handling, text rendering, progress bars, and refresh system

### Current OpenTK Patterns (GlyphSphereCore.cs)
- Dynamic glyph atlas generation from fonts
- Instanced rendering for performance
- Mouse ray-casting for 3D interaction
- Camera-relative positioning
- Shader-based rendering with transparency support

## Design Goals

1. **HUD Overlay**: Fixed-size grid that follows camera as overlay
2. **Performance**: Use instanced rendering like GlyphSphere
3. **Flexibility**: Maintain API compatibility with original terminal
4. **Integration**: Work alongside existing 3D graphics
5. **Responsiveness**: Handle window resize while maintaining grid aspect ratio

## Architecture Design

### Core Components

#### 1. TerminalHUD Class
```csharp
public class TerminalHUD : IDisposable
{
    // Configuration
    public int GridWidth { get; }
    public int GridHeight { get; }
    public float CellAspectRatio { get; }
    
    // Rendering
    private TerminalRenderer _renderer;
    private TerminalView _view;
    
    // Interaction
    private TerminalInputHandler _inputHandler;
    
    // Events
    public event Action<int, int>? CellClicked;
    public event Action<int, int>? CellHovered;
    public event Action<int, int>? CellRightClicked;
}
```

#### 2. TerminalRenderer Class
```csharp
public class TerminalRenderer : IDisposable
{
    // OpenGL resources
    private int _program;
    private int _vao, _vbo, _ebo;
    private int _instanceVbo;
    private int _glyphTexture;
    
    // Glyph atlas management
    private GlyphAtlas _atlas;
    private Dictionary<char, GlyphInfo> _glyphLookup;
    
    // Instance data buffer
    private TerminalInstance[] _instances;
    private bool _instanceBufferDirty;
}
```

#### 3. TerminalView Class
```csharp
public class TerminalView
{
    private TerminalCell[,] _cells;
    public int Width { get; }
    public int Height { get; }
    
    public TerminalCell this[int x, int y] { get; }
    
    // High-level operations
    public void SetCell(int x, int y, char c, Vector4 textColor, Vector4 bgColor);
    public void Text(int x, int y, string text, TextAlignment align = TextAlignment.Left);
    public void Fill(char c, Vector4 textColor, Vector4 bgColor);
    public void Clear();
}
```

#### 4. TerminalCell Class
```csharp
public class TerminalCell
{
    public char Character { get; set; }
    public Vector4 TextColor { get; set; }
    public Vector4 BackgroundColor { get; set; }
    public bool IsDirty { get; set; }
    
    // Change tracking for efficient updates
    private char _lastCharacter;
    private Vector4 _lastTextColor;
    private Vector4 _lastBackgroundColor;
}
```

#### 5. TerminalInputHandler Class
```csharp
public class TerminalInputHandler
{
    private Camera _camera;
    private int _gridWidth, _gridHeight;
    
    public (int x, int y)? GetCellFromMouse(Vector2 mousePos, Vector2i windowSize);
    private (Vector3 origin, Vector3 direction) GetMouseRay(Vector2 mousePos, Vector2i windowSize);
}
```

## Technical Implementation Details

### Rendering Strategy

#### 1. HUD Positioning System
```csharp
// Calculate terminal screen space coordinates
private Matrix4 GetHUDProjectionMatrix()
{
    // Orthographic projection for HUD overlay
    return Matrix4.CreateOrthographic(WindowWidth, WindowHeight, -1f, 1f);
}

private void CalculateTerminalBounds(Vector2i windowSize, out float terminalWidth, out float terminalHeight, out Vector2 offset)
{
    // Maintain fixed aspect ratio
    float targetAspect = (float)GridWidth / GridHeight;
    float windowAspect = (float)windowSize.X / windowSize.Y;
    
    if (windowAspect > targetAspect)
    {
        // Window too wide - center horizontally
        terminalHeight = windowSize.Y;
        terminalWidth = terminalHeight * targetAspect;
        offset = new Vector2((windowSize.X - terminalWidth) * 0.5f, 0);
    }
    else
    {
        // Window too tall - center vertically
        terminalWidth = windowSize.X;
        terminalHeight = terminalWidth / targetAspect;
        offset = new Vector2(0, (windowSize.Y - terminalHeight) * 0.5f);
    }
}
```

#### 2. Instanced Rendering
```csharp
[StructLayout(LayoutKind.Sequential)]
public struct TerminalInstance
{
    public Vector3 Position;     // Screen space position
    public Vector2 Size;         // Cell size in pixels
    public Vector4 UvRect;       // Glyph UV coordinates in atlas
    public Vector4 TextColor;    // Character color with alpha
    public Vector4 BackgroundColor; // Background color with alpha
}
```

#### 3. Dual-Pass Rendering
```glsl
// Pass 1: Background quads
// - Render solid colored backgrounds
// - Use simple quad geometry
// - Alpha blending support

// Pass 2: Character glyphs  
// - Use glyph atlas texture
// - Discard pixels based on glyph alpha
// - Render character with text color
```

### Shader Design

#### Vertex Shader
```glsl
#version 330 core
layout(location = 0) in vec2 aLocalPos;    // Quad vertices (-0.5 to 0.5)
layout(location = 1) in vec2 aLocalUV;     // Quad UVs (0 to 1)

// Instance attributes
layout(location = 2) in vec3 iPosition;    // Cell screen position
layout(location = 3) in vec2 iSize;        // Cell size in pixels
layout(location = 4) in vec4 iUvRect;      // Glyph atlas UV rect
layout(location = 5) in vec4 iTextColor;   // Character color
layout(location = 6) in vec4 iBgColor;     // Background color

uniform mat4 uProjection;  // Orthographic projection for HUD
uniform int uRenderPass;   // 0=background, 1=glyph

out vec2 vUV;
out vec4 vTextColor;
out vec4 vBgColor;

void main()
{
    // Convert to screen space
    vec2 screenPos = iPosition.xy + aLocalPos * iSize;
    gl_Position = uProjection * vec4(screenPos, 0.0, 1.0);
    
    // Calculate UV coordinates
    if (uRenderPass == 0) {
        // Background pass - no UV needed
        vUV = vec2(0.0);
    } else {
        // Glyph pass - map to atlas
        vUV = vec2(iUvRect.x + aLocalUV.x * iUvRect.z, 
                   iUvRect.y + aLocalUV.y * iUvRect.w);
    }
    
    vTextColor = iTextColor;
    vBgColor = iBgColor;
}
```

#### Fragment Shader
```glsl
#version 330 core
in vec2 vUV;
in vec4 vTextColor;
in vec4 vBgColor;

uniform sampler2D uGlyphAtlas;
uniform int uRenderPass;

out vec4 FragColor;

void main()
{
    if (uRenderPass == 0) {
        // Background pass
        FragColor = vBgColor;
    } else {
        // Glyph pass
        vec4 atlasTexel = texture(uGlyphAtlas, vUV);
        float glyphAlpha = atlasTexel.r; // Use red channel as mask
        
        if (glyphAlpha < 0.1) {
            discard; // Transparent areas
        }
        
        FragColor = vec4(vTextColor.rgb, vTextColor.a * glyphAlpha);
    }
}
```

### Mouse Interaction System

#### Ray-to-Grid Mapping
```csharp
public (int x, int y)? GetCellFromMouse(Vector2 mousePos, Vector2i windowSize)
{
    // Get terminal bounds in screen space
    CalculateTerminalBounds(windowSize, out float termWidth, out float termHeight, out Vector2 offset);
    
    // Convert mouse to terminal local coordinates
    Vector2 localMouse = mousePos - offset;
    
    // Check if mouse is within terminal bounds
    if (localMouse.X < 0 || localMouse.X > termWidth ||
        localMouse.Y < 0 || localMouse.Y > termHeight)
        return null;
    
    // Map to grid coordinates
    int gridX = (int)(localMouse.X / termWidth * GridWidth);
    int gridY = (int)(localMouse.Y / termHeight * GridHeight);
    
    // Clamp to valid range
    gridX = Math.Clamp(gridX, 0, GridWidth - 1);
    gridY = Math.Clamp(gridY, 0, GridHeight - 1);
    
    return (gridX, gridY);
}
```

### Performance Optimizations

#### 1. Dirty Cell Tracking
```csharp
public void UpdateInstanceBuffer()
{
    if (!_instanceBufferDirty) return;
    
    // Only update changed cells
    for (int y = 0; y < Height; y++)
    {
        for (int x = 0; x < Width; x++)
        {
            var cell = _cells[x, y];
            if (cell.IsDirty)
            {
                UpdateInstanceData(x, y, cell);
                cell.IsDirty = false;
            }
        }
    }
    
    // Upload to GPU
    GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, 
                    _instances.Length * Marshal.SizeOf<TerminalInstance>(), 
                    _instances);
    
    _instanceBufferDirty = false;
}
```

#### 2. Atlas Management
```csharp
public class GlyphAtlas
{
    private Dictionary<char, GlyphInfo> _glyphMap;
    private int _textureId;
    private bool _needsRebuild;
    
    public GlyphInfo GetGlyph(char c)
    {
        if (!_glyphMap.TryGetValue(c, out var glyph))
        {
            // Add new glyph and mark for atlas rebuild
            _needsRebuild = true;
            return AddGlyph(c);
        }
        return glyph;
    }
}
```

## API Design

### Main Interface
```csharp
// Creation
var terminal = new TerminalHUD(80, 25, camera); // 80x25 character grid

// Basic operations
terminal.SetCell(x, y, '@', Color4.White, Color4.Black);
terminal.Text(10, 5, "Hello World", TextAlignment.Center);
terminal.Fill(' ', Color4.Green, Color4.Black);
terminal.Clear();

// Areas and boxes
terminal.DrawBox(x, y, width, height, BoxStyle.Single);
terminal.FillRect(x, y, width, height, ' ', Color4.White, Color4.Blue);

// Event handling
terminal.CellClicked += (x, y) => Console.WriteLine($"Clicked {x},{y}");
terminal.CellHovered += (x, y) => terminal.SetCell(x, y, '█', Color4.Yellow, Color4.Black);

// Rendering integration
terminal.Render(projectionMatrix, windowSize);
```

### Advanced Features
```csharp
// Progress bars
terminal.ProgressBar(y: 10, percent: 75.0f, width: 20);

// Text input areas
var inputBox = terminal.CreateInputBox(x: 5, y: 15, width: 30);
inputBox.TextChanged += (text) => ProcessInput(text);

// Scrollable regions
var scrollArea = terminal.CreateScrollableRegion(x: 0, y: 0, width: 40, height: 20);
scrollArea.AppendLine("Log message...");
```

## Integration Points

### 1. Rendering Loop Integration
```csharp
protected override void OnRenderFrame(FrameEventArgs args)
{
    // Clear and render 3D scene
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    
    // Render sphere and other 3D elements
    RenderGlyphSphere(viewMatrix, projectionMatrix);
    
    // Enable alpha blending for HUD
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    GL.Disable(EnableCap.DepthTest);
    
    // Render terminal HUD
    _terminal.Render(Size);
    
    // Restore 3D rendering state
    GL.Enable(EnableCap.DepthTest);
    GL.Disable(EnableCap.Blend);
    
    SwapBuffers();
}
```

### 2. Input Event Integration
```csharp
protected override void OnMouseMove(MouseMoveEventArgs e)
{
    // Handle 3D mouse interaction
    Handle3DMouseMove(e);
    
    // Handle terminal mouse interaction
    _terminal.HandleMouseMove(e.Position);
}

protected override void OnMouseDown(MouseButtonEventArgs e)
{
    // Try terminal first (HUD takes priority)
    if (_terminal.HandleMouseClick(e.Position, e.Button))
        return; // Terminal handled the click
        
    // Fall back to 3D interaction
    Handle3DMouseClick(e);
}
```

## File Structure

```
src/terminal/
├── TerminalHUD.cs              # Main terminal class
├── TerminalRenderer.cs         # OpenGL rendering
├── TerminalView.cs             # Grid management
├── TerminalCell.cs             # Individual cell data
├── TerminalInputHandler.cs     # Mouse/keyboard input
├── GlyphAtlas.cs              # Font texture management
├── Shaders/
│   ├── terminal.vert          # Vertex shader
│   ├── terminal.frag          # Fragment shader
│   └── terminal_bg.frag       # Background pass shader
└── Utils/
    ├── TextAlignment.cs       # Text alignment enum
    ├── BoxStyle.cs           # Box drawing styles
    └── TerminalInstance.cs    # GPU instance data
```

## Implementation Phases

### Phase 1: Core Rendering
- [ ] Basic TerminalHUD class
- [ ] Simple quad rendering with orthographic projection
- [ ] Basic glyph atlas generation
- [ ] Single character display

### Phase 2: Grid System
- [ ] Full grid implementation
- [ ] Cell management and dirty tracking
- [ ] Text and background colors
- [ ] Window resize handling

### Phase 3: Mouse Interaction
- [ ] Mouse-to-grid coordinate mapping
- [ ] Click and hover event system
- [ ] Integration with existing input handling

### Phase 4: Advanced Features
- [ ] Text rendering utilities
- [ ] Progress bars and UI elements
- [ ] Input boxes and interactive components
- [ ] Performance optimizations

### Phase 5: Integration
- [ ] Full integration with GlyphSphereCore
- [ ] Event system refinement
- [ ] Documentation and examples
- [ ] Testing and debugging tools

## Future Enhancements

1. **Multiple Terminals**: Support for multiple terminal instances
2. **Themes**: Color scheme and styling system
3. **Animations**: Smooth transitions and effects
4. **Unicode Support**: Extended character set support
5. **Terminal Emulation**: VT100/ANSI escape sequence support
6. **3D Terminal**: Option for terminal as 3D object in scene