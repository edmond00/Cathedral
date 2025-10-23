// GlyphSphereLauncher.cs
// Requires NuGet: OpenTK, SixLabors.ImageSharp, SixLabors.Fonts, FastNoiseLite
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;


using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;
using Matrix4 = OpenTK.Mathematics.Matrix4;
using static Cathedral.Glyph.BiomeDatabase;

namespace Cathedral.Glyph
{

public static class SimplexNoise
{
    private static int[] perm = Enumerable.Range(0, 256).ToArray();
    private static readonly Random rand = new Random(1337);

    static SimplexNoise()
    {
        for (int i = 0; i < 256; i++)
        {
            int swap = rand.Next(256);
            (perm[i], perm[swap]) = (perm[swap], perm[i]);
        }
        perm = perm.Concat(perm).ToArray();
    }

    public static float Noise(float x, float y, float z)
    {
        int X = (int)MathF.Floor(x) & 255;
        int Y = (int)MathF.Floor(y) & 255;
        int Z = (int)MathF.Floor(z) & 255;
        x -= MathF.Floor(x);
        y -= MathF.Floor(y);
        z -= MathF.Floor(z);
        float u = Fade(x);
        float v = Fade(y);
        float w = Fade(z);

        int A = perm[X] + Y, AA = perm[A] + Z, AB = perm[A + 1] + Z;
        int B = perm[X + 1] + Y, BA = perm[B] + Z, BB = perm[B + 1] + Z;

        float res = Lerp(w,
            Lerp(v,
                Lerp(u, Grad(perm[AA], x, y, z),
                         Grad(perm[BA], x - 1, y, z)),
                Lerp(u, Grad(perm[AB], x, y - 1, z),
                         Grad(perm[BB], x - 1, y - 1, z))),
            Lerp(v,
                Lerp(u, Grad(perm[AA + 1], x, y, z - 1),
                         Grad(perm[BA + 1], x - 1, y, z - 1)),
                Lerp(u, Grad(perm[AB + 1], x, y - 1, z - 1),
                         Grad(perm[BB + 1], x - 1, y - 1, z - 1))));
        return (res + 1f) * 0.5f;
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float t, float a, float b) => a + t * (b - a);
    private static float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
public static class GlyphSphereLauncher
{
    // Public method you call from your terminal-based app.
    public static void LaunchGlyphSphere(int windowWidth = 900, int windowHeight = 900)
    {
        // OpenTK/GLFW requires running on the main thread
        var native = new NativeWindowSettings()
        {
            ClientSize = new OpenTK.Mathematics.Vector2i(windowWidth, windowHeight),
            Title = "Glyph Sphere Prototype",
            Flags = ContextFlags.Default,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            WindowBorder = WindowBorder.Resizable
        };

        using var window = new GlyphSphereWindow(GameWindowSettings.Default, native);
        window.Run();
    }

    // Small helper window deriving GameWindow
    private class GlyphSphereWindow : GameWindow
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

        // Data
        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();
        GlyphInfo[] glyphInfos = null!;
        int instanceCount = 0;

        // Camera
        float yaw = 0;
        float pitch = 0;
        float distance = CAMERA_DEFAULT_DISTANCE; // Adjusted for sphere radius
        
        // Mouse interaction
        int hoveredVertexIndex = -1;
        int lastHoveredVertexIndex = -2;
        
        // Debug shader modes
        int debugShaderMode = 0; // 0=normal, 1=vertex colors only, 2=texture only, 3=wireframe
        int debugProgram1, debugProgram2, debugProgram3;

        // Glyphs - dynamically generated from BiomeData
        static string GlyphSet => BiomeDatabase.GenerateGlyphSet();
        const int glyphPixelSize = 35; // raster size
        const int glyphCell = 50; // cell in atlas
        
        // Shared constants to avoid hardcoded duplicates
        const float QUAD_SIZE = 0.3f; // Size of each glyph quad on the sphere
        const float VERTEX_SHADER_SIZE_MULTIPLIER = 2.0f; // Multiplier used in vertex shader
        const float SPHERE_RADIUS = 25.0f; // Main sphere radius
        const float CAMERA_DEFAULT_DISTANCE = 80.0f; // Default camera distance
        const float CAMERA_MIN_DISTANCE = 30.0f; // Minimum camera distance
        const float CAMERA_MAX_DISTANCE = 200f; // Maximum camera distance

        public GlyphSphereWindow(GameWindowSettings g, NativeWindowSettings n) : base(g, n)
        {
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

            // Build background sphere shader
            backgroundProgram = CreateProgram(backgroundVertexShaderSrc, backgroundFragmentShaderSrc);
            
            GL.UseProgram(program);
            
            // Build sphere with parameters matching old Unity code
            // Old code: radius=25, divisions=5 (TextSphere parameters)
            // Use subdivision levels for icosphere (3-4 levels gives good detail)
            BuildSphere(6, 0, SPHERE_RADIUS);
            
            // Create background sphere (90% radius, opaque)
            BuildBackgroundSphere(5, 0, SPHERE_RADIUS * 0.9f); // 90% of sphere radius, using icosphere

            // Build glyph atlas
            string dynamicGlyphSet = GlyphSet;
            Console.WriteLine($"Generated GlyphSet: \"{dynamicGlyphSet}\" ({dynamicGlyphSet.Length} characters)");
            glyphInfos = BuildGlyphAtlas(dynamicGlyphSet, glyphCell, glyphPixelSize, out Image<Rgba32> atlasImage);
            
            // Save atlas texture for debugging
            try 
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"atlas_debug.png";
                atlasImage.SaveAsPng(filename);
                Console.WriteLine($"Saved atlas texture to: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not save atlas texture: {ex.Message}");
            }
            
            glyphTexture = LoadTexture(atlasImage);

            atlasImage.Dispose();

            // Create VBO/VAO for quad + instance data approach (we'll upload quads per vertex)
            // We'll create a single quad VBO (positions 2D) and use instancing attributes for world position, right, up, size, uvRect, color.
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

            // Instance buffer will contain per-instance data:
            // vec3 instancePos; vec3 right; vec3 up; float size; vec4 uvRect; vec4 color; int glyphIndex
            // Pack as floats: 3+3+3+1 +4+4 = 18 floats ~= 72 bytes per instance
            instanceVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVbo);
            // allocate a big buffer (we'll update with BufferSubData)
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
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // Input -> camera control
            const float rotSpeed = 60f;
            const float zoomSpeed = 15.0f; // Increased for larger sphere
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left))
                yaw -= rotSpeed * (float)args.Time;
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right))
                yaw += rotSpeed * (float)args.Time;
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up))
                pitch = Math.Clamp(pitch + rotSpeed * (float)args.Time, -89f, 89f);
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down))
                pitch = Math.Clamp(pitch - rotSpeed * (float)args.Time, -89f, 89f);
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W))
                distance = MathF.Max(CAMERA_MIN_DISTANCE, distance - zoomSpeed * (float)args.Time); // Min distance to see sphere
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S))
                distance = MathF.Min(CAMERA_MAX_DISTANCE, distance + zoomSpeed * (float)args.Time); // Max distance

            // Debug shader switching (D key)
            if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D))
            {
                debugShaderMode = (debugShaderMode + 1) % 4;
                string description = debugShaderMode switch
                {
                    0 => "Normal rendering (texture + vertex colors)",
                    1 => "Vertex colors only (no texture masking)",
                    2 => "Texture only (white on black)",
                    3 => "Wireframe/Debug view",
                    _ => "Unknown"
                };
                Console.WriteLine($"Debug shader mode: {debugShaderMode} - {description}");
            }

            // Update instances for dynamic color changes (hover highlighting)
            UpdateInstanceBuffer();

            // Clear
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Build view & proj
            var view = GetViewMatrix();
            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), (float)Size.X / Size.Y, 0.01f, 100f);

            // Render background sphere first (opaque backdrop)
            GL.UseProgram(backgroundProgram);
            var model = Matrix4.Identity; // Background sphere at origin
            int bgViewLoc = GL.GetUniformLocation(backgroundProgram, "uView");
            int bgProjLoc = GL.GetUniformLocation(backgroundProgram, "uProj");
            int bgModelLoc = GL.GetUniformLocation(backgroundProgram, "uModel");
            GL.UniformMatrix4(bgViewLoc, false, ref view);
            GL.UniformMatrix4(bgProjLoc, false, ref proj);
            GL.UniformMatrix4(bgModelLoc, false, ref model);
            
            GL.BindVertexArray(backgroundVao);
            GL.DrawElements(PrimitiveType.Triangles, backgroundIndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);

            // Select shader program based on debug mode
            int currentProgram = debugShaderMode switch
            {
                0 => program,           // normal rendering
                1 => debugProgram1,     // vertex colors only
                2 => debugProgram2,     // texture only
                3 => debugProgram3,     // wireframe
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

            SwapBuffers();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            
            // Update hovered vertex for red highlighting
            var mouse = MousePosition;
            var (rayOrig, rayDir) = GetMouseRay(mouse);

            // Use ray-quad intersection to find the exact quad hit by the mouse
            int newHover = FindVertexByRaySphereIntersection(rayOrig, rayDir);
            
            // If ray-quad intersection fails, fall back to screen-space distance
            if (newHover == -1)
            {
                newHover = FindClosestVertexInScreenSpace(mouse);
            }
            
            // Debug output when hover changes
            if (newHover != hoveredVertexIndex && newHover >= 0)
            {
                Console.WriteLine($"Mouse: ({mouse.X:F0}, {mouse.Y:F0}) -> Vertex {newHover} at {vertices[newHover].Position}");
            }
            
            hoveredVertexIndex = newHover;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left)
            {
                // Simple ray pick
                var mouse = MousePosition;
                var (rayOrig, rayDir) = GetMouseRay(mouse);

                int hitIdx = RayPickNearestVertex(rayOrig, rayDir, 0.05f);
                if (hitIdx >= 0)
                {
                    Console.WriteLine($"Picked vertex {hitIdx}, glyph '{vertices[hitIdx].GlyphChar}', noise {vertices[hitIdx].Noise:F3}");
                }
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
            UpdateProjection();
        }

        private void UpdateProjection()
        {
            // done in OnRenderFrame per-frame; kept for completeness
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
                
                // Skip if behind camera
                if (clipPos.W <= 0) continue;
                
                // Convert to NDC
                var ndc = new Vector3(clipPos.X / clipPos.W, clipPos.Y / clipPos.W, clipPos.Z / clipPos.W);
                
                // Skip if outside NDC cube  
                if (ndc.X < -1 || ndc.X > 1 || ndc.Y < -1 || ndc.Y > 1) continue;
                
                // Convert to screen space
                var screenX = (ndc.X + 1.0f) * 0.5f * Size.X;
                var screenY = (1.0f - ndc.Y) * 0.5f * Size.Y;
                
                // Calculate distance to mouse
                float dx = screenX - mousePos.X;
                float dy = screenY - mousePos.Y;
                float dist = dx * dx + dy * dy;
                
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }
            
            return (bestDist < 20 * 20) ? best : -1; // 20 pixel threshold
        }

        private int FindBetterMatch(int baseVertex, OpenTK.Mathematics.Vector2 mousePos)
        {
            if (baseVertex < 0) return baseVertex;
            
            int lonCount = 28;
            int rowSize = lonCount + 1;
            
            // Try several nearby vertices and pick the one that feels most accurate
            int[] candidates = new int[]
            {
                baseVertex,                    // Original
                baseVertex + rowSize,         // One row down (south)
                baseVertex - rowSize,         // One row up (north)  
                baseVertex + 1,               // One column right (east)
                baseVertex - 1,               // One column left (west)
                baseVertex + rowSize + 1,     // Southeast
                baseVertex + rowSize - 1,     // Southwest  
                baseVertex - rowSize + 1,     // Northeast
                baseVertex - rowSize - 1      // Northwest
            };
            
            var view = GetViewMatrix();
            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), (float)Size.X / Size.Y, 0.01f, 100f);
            var viewProj = view * proj;
            
            float bestDist = float.MaxValue;
            int best = baseVertex;
            
            foreach (int candidate in candidates)
            {
                if (candidate < 0 || candidate >= vertices.Count) continue;
                
                var worldPos = new Vector4(vertices[candidate].Position, 1.0f);
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
                    best = candidate;
                }
            }
            
            return best;
        }

        private int RayPickNearestVertex(Vector3 rayO, Vector3 rayD, float pickRadius)
        {
            // Simple distance-based picking - find closest vertex to ray
            float bestT = float.MaxValue;
            int best = -1;
            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i].Position;
                Vector3 w = v - rayO;
                float proj = Vector3.Dot(w, rayD);
                if (proj < 0) continue; // Behind ray origin
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

        private int FindVertexByRaySphereIntersection(Vector3 rayOrigin, Vector3 rayDirection)
        {
            // First, intersect ray with the sphere to get the intersection point on sphere surface
            float sphereRadius = SPHERE_RADIUS; // Match the sphere radius used in BuildSphere
            Vector3 sphereCenter = Vector3.Zero;
            
            // Ray-sphere intersection
            Vector3 oc = rayOrigin - sphereCenter;
            float a = Vector3.Dot(rayDirection, rayDirection);
            float b = 2.0f * Vector3.Dot(oc, rayDirection);
            float c = Vector3.Dot(oc, oc) - sphereRadius * sphereRadius;
            
            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return -1; // Ray doesn't hit sphere
            
            // Get the closest intersection point (we want the front face)
            float t1 = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
            float t2 = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);
            
            float t = (t1 > 0) ? t1 : t2; // Use closest positive t
            if (t < 0) return -1;
            
            Vector3 intersectionPoint = rayOrigin + rayDirection * t;
            
            // Now find the closest vertex to this intersection point on the sphere
            float closestDist = float.MaxValue;
            int closestVertex = -1;
            
            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                Vector3 vertexPos = v.Position;
                
                // Check if this vertex is on the front-facing side of the sphere
                Vector3 vertexNormal = Vector3.Normalize(vertexPos);
                Vector3 toCam = Vector3.Normalize(rayOrigin - vertexPos);
                if (Vector3.Dot(vertexNormal, toCam) <= 0) continue;
                
                // Calculate distance from intersection point to this vertex
                float dist = Vector3.Distance(intersectionPoint, vertexPos);
                
                // Only consider vertices within a reasonable range (based on quad size)
                float maxDist = QUAD_SIZE * VERTEX_SHADER_SIZE_MULTIPLIER; // Max distance to consider
                if (dist <= maxDist && dist < closestDist)
                {
                    closestDist = dist;
                    closestVertex = i;
                }
            }
            
            return closestVertex;
        }

        private Matrix4 GetViewMatrix()
        {
            // build camera from yaw/pitch/distance
            float yawR = MathHelper.DegreesToRadians(yaw);
            float pitchR = MathHelper.DegreesToRadians(pitch);
            Vector3 camDir = new Vector3(
                MathF.Cos(pitchR) * MathF.Cos(yawR),
                MathF.Sin(pitchR),
                MathF.Cos(pitchR) * MathF.Sin(yawR)
            );
            Vector3 camPos = -camDir * distance;
            return Matrix4.LookAt(camPos, Vector3.Zero, Vector3.UnitY);
        }

        private (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay(OpenTK.Mathematics.Vector2 mousePos)
        {
            // Convert mouse position to NDC
            var ndcX = (float)(mousePos.X / Size.X * 2.0 - 1.0);
            var ndcY = (float)(1.0 - mousePos.Y / Size.Y * 2.0);
            
            // Get camera position directly from camera parameters
            float yawR = MathHelper.DegreesToRadians(yaw);
            float pitchR = MathHelper.DegreesToRadians(pitch);
            Vector3 camDir = new Vector3(
                MathF.Cos(pitchR) * MathF.Cos(yawR),
                MathF.Sin(pitchR),
                MathF.Cos(pitchR) * MathF.Sin(yawR)
            );
            Vector3 rayOrigin = -camDir * distance; // Camera position
            
            // Calculate ray direction using proper inverse projection
            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), (float)Size.X / Size.Y, 0.01f, 100f);
            var view = GetViewMatrix();
            var invViewProj = (view * proj).Inverted();
            
            // Transform mouse position to world space direction
            Vector4 nearPoint = new Vector4(ndcX, ndcY, -1f, 1f);
            Vector4 farPoint = new Vector4(ndcX, ndcY, 1f, 1f);
            Vector4 nearWorld = nearPoint * invViewProj;
            Vector4 farWorld = farPoint * invViewProj;
            
            nearWorld /= nearWorld.W;
            farWorld /= farWorld.W;
            
            var rayDirection = Vector3.Normalize(new Vector3(farWorld.X - nearWorld.X, farWorld.Y - nearWorld.Y, farWorld.Z - nearWorld.Z));
            
            return (rayOrigin, rayDirection);
        }

        private void UpdateInstanceBuffer()
        {
            // Build instance data per vertex
            instanceCount = vertices.Count;
            float[] data = new float[instanceCount * 18];
            for (int i = 0; i < instanceCount; i++)
            {
                var v = vertices[i];

                // compute surface tangent toward north pole (+Y)
                var normal = Vector3.Normalize(v.Position);
                Vector3 pole = Vector3.UnitY;
                var poleProj = pole - normal * Vector3.Dot(pole, normal);
                if (poleProj.LengthSquared < 1e-6f)
                    poleProj = Vector3.Cross(normal, Vector3.UnitX);
                poleProj = Vector3.Normalize(poleProj);
                var right = Vector3.Normalize(Vector3.Cross(poleProj, normal));
                var up = poleProj;
                float size = QUAD_SIZE; // quad scale - increased for radius=25 sphere

                // uv rect: glyphInfos[v.GlyphIndex]
                var gi = glyphInfos[v.GlyphIndex];
                // uvRect: x,y,w,h in normalized 0..1
                var uvx = gi.UvX; var uvy = gi.UvY; var uvw = gi.UvW; var uvh = gi.UvH;
                
                // Debug: Print UV rect for first few vertices (commented out to avoid spam in render loop)
                // if (i < 3)
                // {
                //     Console.WriteLine($"Vertex {i}: GlyphIndex={v.GlyphIndex} '{v.GlyphChar}' -> UvRect({uvx:F3}, {uvy:F3}, {uvw:F3}, {uvh:F3})");
                // }

                // color - use red if this vertex is hovered, otherwise use terrain color
                Vector4 col = v.Color;
                if (i == hoveredVertexIndex)
                {
                    col = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
                }

                int baseIdx = i * 18;
                data[baseIdx + 0] = v.Position.X;
                data[baseIdx + 1] = v.Position.Y;
                data[baseIdx + 2] = v.Position.Z;

                data[baseIdx + 3] = right.X;
                data[baseIdx + 4] = right.Y;
                data[baseIdx + 5] = right.Z;

                data[baseIdx + 6] = up.X;
                data[baseIdx + 7] = up.Y;
                data[baseIdx + 8] = up.Z;

                data[baseIdx + 9] = size;

                data[baseIdx + 10] = uvx;
                data[baseIdx + 11] = uvy;
                data[baseIdx + 12] = uvw;
                data[baseIdx + 13] = uvh;

                data[baseIdx + 14] = col.X;
                data[baseIdx + 15] = col.Y;
                data[baseIdx + 16] = col.Z;
                data[baseIdx + 17] = col.W;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.Length * sizeof(float), data);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private GlyphInfo[] BuildGlyphAtlas(string glyphs, int cellSize, int fontPxSize, out Image<Rgba32> atlas)
        {
            int cols = glyphs.Length; // Put all glyphs in a single row for testing
            int rows = 1;
            int atlasW = cols * cellSize;
            int atlasH = rows * cellSize;
            
            Console.WriteLine($"Atlas: {glyphs.Length} glyphs, {cols}x{rows} grid, {atlasW}x{atlasH} pixels, {cellSize}x{cellSize} cells");
            
            atlas = new Image<Rgba32>(atlasW, atlasH, Color.Transparent);

            // load font - always use FreeMono.ttf from assets/fonts
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
                // Fallback only if the project font is missing
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
                
                Console.WriteLine($"Glyph[{i}] '{glyph}' -> grid({cx},{cy}) -> pixels({x},{y})");

                // Draw the actual glyph character
                atlas.Mutate(ctx =>
                {
                    var textOptions = new RichTextOptions(font)
                    {
                        Origin = new PointF(x + cellSize / 2f, y + cellSize / 2f),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    // Use bright white color to ensure maximum contrast
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

                // Debug: Print UV coordinates for each glyph
                Console.WriteLine($"Glyph[{i}] '{glyphs[i]}' -> UV({infos[i].UvX:F3}, {infos[i].UvY:F3}, {infos[i].UvW:F3}, {infos[i].UvH:F3}) at atlas({x}, {y})");
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
            
            // Use nearest neighbor filtering to avoid bleeding between atlas cells
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            
            // Set wrapping to clamp to avoid edge artifacts
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Console.WriteLine($"Loaded texture ID {tex}, size {img.Width}x{img.Height}");
            return tex;
        }

        private void BuildSphere(int subdivisions, int unused, float radius)
        {
            vertices.Clear();
            indices.Clear();

            // Build icosphere using subdivision
            BuildIcosphere(subdivisions, radius);

            // Collect noise statistics
            var noiseValues = new List<float>();
            var glyphCounts = new int[GlyphSet.Length];

            // Apply noise and biome generation to all vertices
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                Vector3 pos = vertex.Position;
                
                // Multi-scale Perlin noise like the old Unity code
                // Generate random offsets (using simple deterministic approach for now)
                Vector3 off1 = new Vector3(1337.0f, 2468.0f, 9876.0f);
                Vector3 off2 = new Vector3(5432.0f, 8765.0f, 1234.0f);
                Vector3 off3 = new Vector3(9999.0f, 3333.0f, 7777.0f);
                
                Vector3 p1 = (off1 + pos) / 12f;
                Vector3 p2 = (off2 + pos) / 3f;
                Vector3 p3 = (off3 + pos) / 8f;
                
                float perlinNoise1 = Perlin.Noise(p1.X, p1.Y, p1.Z);
                float perlinNoise2 = Perlin.Noise(p2.X, p2.Y, p2.Z);
                float perlinNoise3 = Perlin.Noise(p3.X, p3.Y, p3.Z);
                
                // Determine biome based on the three noise layers (matching Unity logic)
                BiomeType biome = DetermineBiome(perlinNoise1, perlinNoise2, perlinNoise3);
                
                // Calculate location spawn chance and determine if a location should spawn
                LocationType? location = DetermineLocation(biome, pos);
                
                // Get glyph and color based on location first, then biome
                char glyphChar;
                Vector3 color;
                if (location.HasValue)
                {
                    glyphChar = location.Value.Glyph;
                    color = location.Value.Color;
                }
                else
                {
                    glyphChar = biome.Glyph;
                    color = biome.Color;
                }
                
                // Convert to Vector4 color for vertex (normalize from 0-255 to 0-1 range)
                Vector4 vertexColor = new Vector4(color.X / 255.0f, color.Y / 255.0f, color.Z / 255.0f, 1.0f);
                
                // Find glyph index in the glyph set (fallback to first if not found)
                int glyphIndex = GlyphSet.IndexOf(glyphChar);
                if (glyphIndex == -1) glyphIndex = 0;
                
                // Collect noise value for statistics
                float n = (perlinNoise1 + perlinNoise2 + perlinNoise3) / 3.0f;
                noiseValues.Add(n);

                vertices[i] = new Vertex { Position = pos, GlyphIndex = glyphIndex, GlyphChar = glyphChar, Noise = n, Color = vertexColor };
            }

            // Count glyph usage and debug output for biome-based assignments
            var biomeCounts = new Dictionary<string, int>();
            var locationCounts = new Dictionary<string, int>();
            
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                
                // Count glyph usage
                glyphCounts[vertex.GlyphIndex]++;

                // Debug: Print first few vertex mappings
                if (i < 5)
                {
                    Console.WriteLine($"Vertex {i}: noise={vertex.Noise:F3} -> glyph='{vertex.GlyphChar}' -> color=({vertex.Color.X:F2},{vertex.Color.Y:F2},{vertex.Color.Z:F2})");
                }
            }

            // Calculate and print noise statistics
            if (noiseValues.Count > 0)
            {
                noiseValues.Sort();
                float min = noiseValues[0];
                float max = noiseValues[noiseValues.Count - 1];
                float mean = noiseValues.Average();
                float median = noiseValues[noiseValues.Count / 2];
                
                // Calculate standard deviation
                double sumSquaredDiffs = noiseValues.Sum(x => Math.Pow(x - mean, 2));
                float stdDev = (float)Math.Sqrt(sumSquaredDiffs / noiseValues.Count);

                // Calculate percentiles
                float p10 = noiseValues[(int)(noiseValues.Count * 0.1)];
                float p25 = noiseValues[(int)(noiseValues.Count * 0.25)];
                float p75 = noiseValues[(int)(noiseValues.Count * 0.75)];
                float p90 = noiseValues[(int)(noiseValues.Count * 0.9)];

                Console.WriteLine($"\nNoise Distribution Statistics ({noiseValues.Count} vertices):");
                Console.WriteLine($"  Min: {min:F3}, Max: {max:F3}");
                Console.WriteLine($"  Mean: {mean:F3}, Median: {median:F3}, StdDev: {stdDev:F3}");
                Console.WriteLine($"  Percentiles - P10: {p10:F3}, P25: {p25:F3}, P75: {p75:F3}, P90: {p90:F3}");

                Console.WriteLine($"\nBiome-Based Glyph Distribution:");
                for (int i = 0; i < GlyphSet.Length; i++)
                {
                    if (glyphCounts[i] > 0)
                    {
                        float percentage = (glyphCounts[i] / (float)noiseValues.Count) * 100f;
                        Console.WriteLine($"  '{GlyphSet[i]}' (index {i}): {glyphCounts[i]} vertices ({percentage:F1}%)");
                    }
                }
            }

            // for this prototype we use only vertices for instancing (one instance per vertex)
            instanceCount = vertices.Count;
            Console.WriteLine($"Generated {vertices.Count} vertices, {indices.Count} indices, instanceCount = {instanceCount}");
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
                vertices.Add(new Vertex { Position = pos, GlyphIndex = 0, GlyphChar = GlyphSet[0], Noise = 0, Color = Vector4.One });
            }

            indices.AddRange(currentIndices);
        }

        private int GetMidpoint(uint i1, uint i2, List<Vector3> oldVertices, List<Vector3> newVertices, Dictionary<(int, int), int> cache, float radius)
        {
            // Ensure consistent ordering for cache key
            var key = i1 < i2 ? ((int)i1, (int)i2) : ((int)i2, (int)i1);

            if (cache.TryGetValue(key, out int cachedIndex))
            {
                return cachedIndex;
            }

            // Create new midpoint
            Vector3 mid = (oldVertices[(int)i1] + oldVertices[(int)i2]) / 2.0f;
            mid = Vector3.Normalize(mid) * radius; // Project to sphere surface

            int newIndex = newVertices.Count;
            newVertices.Add(mid);
            cache[key] = newIndex;

            return newIndex;
        }

        private void BuildBackgroundSphere(int subdivisions, int unused, float radius)
        {
            var backgroundVertices = new List<Vector3>();
            var backgroundIndices = new List<uint>();

            // Generate icosphere for background
            BuildIcosphereGeometry(subdivisions, radius, backgroundVertices, backgroundIndices);

            backgroundIndexCount = backgroundIndices.Count;

            // Create background sphere VAO/VBO/EBO
            backgroundVao = GL.GenVertexArray();
            backgroundVbo = GL.GenBuffer();
            backgroundEbo = GL.GenBuffer();

            GL.BindVertexArray(backgroundVao);

            // Upload vertices
            GL.BindBuffer(BufferTarget.ArrayBuffer, backgroundVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, backgroundVertices.Count * 3 * sizeof(float), backgroundVertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Upload indices
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, backgroundEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, backgroundIndices.Count * sizeof(uint), backgroundIndices.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindVertexArray(0);
        }

        private void BuildIcosphereGeometry(int subdivisions, float radius, List<Vector3> outVertices, List<uint> outIndices)
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

            outVertices.AddRange(currentVertices);
            outIndices.AddRange(currentIndices);
        }

        private Vector4 MapColorFromGlyphIndex(int glyphIndex)
        {
            // Map colors to terrain types based on glyph index (elevation)
            return glyphIndex switch
            {
                0 => new Vector4(0.1f, 0.1f, 0.4f, 1f),    // '@' - Deep water (dark blue)
                1 => new Vector4(0.2f, 0.4f, 0.8f, 1f),    // '#' - Deep water (medium blue)
                2 => new Vector4(0.3f, 0.6f, 1.0f, 1f),    // '%' - Shallow water (light blue)
                3 => new Vector4(0.8f, 0.7f, 0.4f, 1f),    // '*' - Beach/Sand (sandy yellow)
                4 => new Vector4(0.6f, 0.8f, 0.3f, 1f),    // '+' - Low plains (light green)
                5 => new Vector4(0.4f, 0.7f, 0.2f, 1f),    // '=' - Hills (medium green)
                6 => new Vector4(0.3f, 0.5f, 0.1f, 1f),    // '-' - High hills (dark green)
                7 => new Vector4(0.5f, 0.4f, 0.3f, 1f),    // ':' - Lower mountains (brown)
                8 => new Vector4(0.6f, 0.5f, 0.4f, 1f),    // '.' - High mountains (light brown/grey)
                9 => new Vector4(0.9f, 0.9f, 0.9f, 1f),    // ' ' - Peaks (white/snow)
                _ => new Vector4(1.0f, 0.0f, 1.0f, 1f)     // Error color (magenta)
            };
        }

        private BiomeType DetermineBiome(float perlinNoise1, float perlinNoise2, float perlinNoise3)
        {
            // Based on Unity Microworld.cs biome classification logic (exact match)
            // perlinNoise1: water classification (-1 to 1 range)
            // perlinNoise2: cities/forests/fields classification (-1 to 1 range)  
            // perlinNoise3: mountains classification (-1 to 1 range)

            // WATER (perlinNoise1)
            if (perlinNoise1 <= -0.25f)
                return Biomes["ocean"];
            if (perlinNoise1 <= 0.0f)
                return Biomes["sea"];

            // MOUNTAIN (perlinNoise3)
            if (perlinNoise3 > 0.5f)
                return Biomes["peak"];
            if (perlinNoise3 > 0.3f)
                return Biomes["mountain"];

            // CITY (perlinNoise2)
            if (perlinNoise2 < -0.4f)
                return Biomes["city"];

            // COAST (perlinNoise1)
            if (perlinNoise1 <= 0.065f)
                return Biomes["coast"];

            // FOREST (perlinNoise2)
            if (perlinNoise2 > 0.25f)
                return Biomes["forest"];

            // FIELD (perlinNoise2)
            if (perlinNoise2 < -0.15f)
                return Biomes["field"];

            // PLAIN (default fallback)
            return Biomes["plain"];
        }

        private LocationType? DetermineLocation(BiomeType biome, Vector3 position)
        {
            // Generate a pseudo-random value based on position for consistency
            // Use a simple hash function based on position coordinates
            int seed = (int)(position.X * 1000 + position.Y * 2000 + position.Z * 3000);
            var random = new Random(Math.Abs(seed));
            
            // Check if a location should spawn based on biome density
            if (random.NextDouble() > biome.Density)
                return null;

            // Get locations that can spawn in this biome
            var compatibleLocations = new List<LocationType>();
            foreach (var locationPair in Locations)
            {
                var location = locationPair.Value;
                if (location.AllowedBiomes.Contains(biome.Name))
                {
                    compatibleLocations.Add(location);
                }
            }

            // If no compatible locations, return null
            if (compatibleLocations.Count == 0)
                return null;

            // Randomly select a compatible location
            int locationIndex = random.Next(compatibleLocations.Count);
            return compatibleLocations[locationIndex];
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

        // vertex structure for per-vertex bookkeeping
        private class Vertex
        {
            public Vector3 Position;
            public int GlyphIndex;
            public char GlyphChar;
            public float Noise;
            public Vector4 Color;
        }

        private struct GlyphInfo
        {
            public char Glyph;
            public float UvX, UvY, UvW, UvH;
        }

        // very small shaders: expand instanced quad via local pos.xy (attrib 0) and instance data
        private readonly string vertexShaderSrc = $@"
#version 330 core
layout(location = 0) in vec2 aLocalPos;    // quad local pos (-0.5..0.5)
layout(location = 1) in vec2 aLocalUV;

layout(location = 2) in vec3 iPos;       // instance
layout(location = 3) in vec3 iRight;
layout(location = 4) in vec3 iUp;
layout(location = 5) in float iSize;
layout(location = 6) in vec4 iUvRect;    // x,y,w,h
layout(location = 7) in vec4 iColor;

uniform mat4 uView;
uniform mat4 uProj;

out vec2 vUv;
out vec4 vColor;

void main()
{{
    // local quad point to world:
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
    
    // Use luminance of texture as mask for the glyph
    float luminance = dot(texSample.rgb, vec3(0.299, 0.587, 0.114));
    
    // Show vertex color where there's text content
    if (luminance > 0.1) {
        FragColor = vec4(vColor.rgb, 1.0);
    } else {
        discard;
    }
}
";

        // Debug shader 1: Show vertex colors only (no texture masking)
        private readonly string debugFragmentSrc1 = @"
#version 330 core
in vec2 vUv;
in vec4 vColor;
out vec4 FragColor;

uniform sampler2D uAtlas;

void main()
{
    // Show vertex color directly - no texture masking
    FragColor = vColor;
}
";

        // Debug shader 2: Show texture only (black and white)
        private readonly string debugFragmentSrc2 = @"
#version 330 core
in vec2 vUv;
in vec4 vColor;
out vec4 FragColor;

uniform sampler2D uAtlas;

void main()
{
    vec4 texSample = texture(uAtlas, vUv);
    float luminance = dot(texSample.rgb, vec3(0.299, 0.587, 0.114));
    
    // Show texture content in white
    if (luminance > 0.1) {
        FragColor = vec4(1.0, 1.0, 1.0, 1.0);
    } else {
        discard;
    }
}
";

        // Debug shader 3: Show wireframe/outline
        private readonly string debugFragmentSrc3 = @"
#version 330 core
in vec2 vUv;
in vec4 vColor;
out vec4 FragColor;

uniform sampler2D uAtlas;

void main()
{
    vec4 texSample = texture(uAtlas, vUv);
    float luminance = dot(texSample.rgb, vec3(0.299, 0.587, 0.114));
    
    // Show edges only
    vec2 grid = abs(fract(vUv * 10.0) - 0.5);
    float line = smoothstep(0.0, 0.05, min(grid.x, grid.y));
    
    if (luminance > 0.1) {
        FragColor = vec4(mix(vec3(1.0, 0.0, 0.0), vColor.rgb, line), 1.0);
    } else {
        discard;
    }
}
";

        // Background sphere shaders (simple solid sphere)
        private readonly string backgroundVertexShaderSrc = @"
#version 330 core
layout(location = 0) in vec3 aPosition;

uniform mat4 uView;
uniform mat4 uProj;
uniform mat4 uModel;

void main()
{
    gl_Position = uProj * uView * uModel * vec4(aPosition, 1.0);
}";

        private readonly string backgroundFragmentShaderSrc = @"
#version 330 core
out vec4 FragColor;

void main()
{
    // Dark opaque color for the background sphere
    FragColor = vec4(0.05, 0.05, 0.1, 1.0); // Very dark blue-grey
}
";
    }
}
}