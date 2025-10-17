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
        float distance = 3.2f;
        
        // Mouse interaction
        int hoveredVertexIndex = -1;
        int lastHoveredVertexIndex = -2;
        
        // Debug shader modes
        int debugShaderMode = 0; // 0=normal, 1=vertex colors only, 2=texture only, 3=wireframe
        int debugProgram1, debugProgram2, debugProgram3;

        // Glyphs
        const string GlyphSet = "@#%*+=-:. "; // from dense to sparse
        const int glyphPixelSize = 25; // raster size
        const int glyphCell = 50; // cell in atlas

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
            
            // Build sphere low-res
            BuildSphere(28, 28, 1.0f);
            
            // Create background sphere (90% radius, opaque)
            BuildBackgroundSphere(28, 28, 0.9f);

            // Build glyph atlas
            glyphInfos = BuildGlyphAtlas(GlyphSet, glyphCell, glyphPixelSize, out Image<Rgba32> atlasImage);
            
            // Save atlas texture for debugging
            try 
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"atlas_debug_{timestamp}.png";
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
            const float zoomSpeed = 1.5f;
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left))
                yaw -= rotSpeed * (float)args.Time;
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right))
                yaw += rotSpeed * (float)args.Time;
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up))
                pitch = Math.Clamp(pitch + rotSpeed * (float)args.Time, -89f, 89f);
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down))
                pitch = Math.Clamp(pitch - rotSpeed * (float)args.Time, -89f, 89f);
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W))
                distance = MathF.Max(0.5f, distance - zoomSpeed * (float)args.Time);
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S))
                distance = MathF.Min(10f, distance + zoomSpeed * (float)args.Time);

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
            var ndcX = (float)(mouse.X / Size.X * 2.0 - 1.0);
            var ndcY = (float)(1.0 - mouse.Y / Size.Y * 2.0);
            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), (float)Size.X / Size.Y, 0.01f, 100f);
            var invVP = (GetViewMatrix() * proj).Inverted();
            Vector4 near = new Vector4(ndcX, ndcY, -1f, 1f);
            Vector4 far = new Vector4(ndcX, ndcY, 1f, 1f);
            Vector4 pN = near * invVP;
            Vector4 pF = far * invVP;
            var pNear = new Vector3(pN.X / pN.W, pN.Y / pN.W, pN.Z / pN.W);
            var pFar = new Vector3(pF.X / pF.W, pF.Y / pF.W, pF.Z / pF.W);
            var rayDir = Vector3.Normalize(pFar - pNear);
            var rayOrig = pNear;

            // Use ray-quad intersection to find the exact quad hit by the mouse
            int newHover = FindVertexByRaySphereIntersection(rayOrig, rayDir);
            
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
                var ndcX = (float)(mouse.X / Size.X * 2.0 - 1.0);
                var ndcY = (float)(1.0 - mouse.Y / Size.Y * 2.0);
                var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), (float)Size.X / Size.Y, 0.01f, 100f);
                var invVP = (GetViewMatrix() * proj).Inverted();
                Vector4 near = new Vector4(ndcX, ndcY, -1f, 1f);
                Vector4 far = new Vector4(ndcX, ndcY, 1f, 1f);
                Vector4 pN = near * invVP;
                Vector4 pF = far * invVP;
                var pNear = new Vector3(pN.X / pN.W, pN.Y / pN.W, pN.Z / pN.W);
                var pFar = new Vector3(pF.X / pF.W, pF.Y / pF.W, pF.Z / pF.W);
                var rayDir = Vector3.Normalize(pFar - pNear);
                var rayOrig = pNear;

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
            // Test ray intersection with each rendered quad
            Vector3 cameraPos = rayOrigin;
            float closestT = float.MaxValue;
            int closestVertex = -1;
            
            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                Vector3 vertexPos = v.Position;
                Vector3 vertexNormal = Vector3.Normalize(vertexPos);
                Vector3 toCam = Vector3.Normalize(cameraPos - vertexPos);
                
                // Only consider front-facing vertices
                if (Vector3.Dot(vertexNormal, toCam) <= 0) continue;
                
                // Calculate the quad's tangent space (same as UpdateInstanceBuffer)
                var normal = Vector3.Normalize(v.Position);
                Vector3 pole = Vector3.UnitY;
                var poleProj = pole - normal * Vector3.Dot(pole, normal);
                if (poleProj.LengthSquared < 1e-6f)
                    poleProj = Vector3.Cross(normal, Vector3.UnitX);
                poleProj = Vector3.Normalize(poleProj);
                var right = Vector3.Normalize(Vector3.Cross(poleProj, normal));
                var up = poleProj;
                float size = 0.045f; // Same as in UpdateInstanceBuffer
                
                // Calculate quad plane normal (facing camera)
                Vector3 quadNormal = Vector3.Normalize(Vector3.Cross(right, up));
                
                // Ray-plane intersection
                float denom = Vector3.Dot(rayDirection, quadNormal);
                if (Math.Abs(denom) < 1e-6f) continue; // Ray parallel to quad
                
                float t = Vector3.Dot(vertexPos - rayOrigin, quadNormal) / denom;
                if (t < 0) continue; // Behind ray origin
                
                // Get intersection point
                Vector3 intersectionPoint = rayOrigin + rayDirection * t;
                
                // Check if intersection is within quad bounds
                Vector3 toIntersection = intersectionPoint - vertexPos;
                float rightProj = Vector3.Dot(toIntersection, right);
                float upProj = Vector3.Dot(toIntersection, up);
                
                float quadHalfSize = size; // quad extends from -size to +size in each direction
                if (Math.Abs(rightProj) <= quadHalfSize && Math.Abs(upProj) <= quadHalfSize)
                {
                    // Ray hits this quad
                    if (t < closestT)
                    {
                        closestT = t;
                        closestVertex = i;
                    }
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
                float size = 0.045f; // quad scale

                // uv rect: glyphInfos[v.GlyphIndex]
                var gi = glyphInfos[v.GlyphIndex];
                // uvRect: x,y,w,h in normalized 0..1
                var uvx = gi.UvX; var uvy = gi.UvY; var uvw = gi.UvW; var uvh = gi.UvH;
                
                // Debug: Print UV rect for first few vertices
                if (i < 3)
                {
                    Console.WriteLine($"Vertex {i}: GlyphIndex={v.GlyphIndex} '{v.GlyphChar}' -> UvRect({uvx:F3}, {uvy:F3}, {uvw:F3}, {uvh:F3})");
                }

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

            // load font - try some common monospace; fallback to system family
            Font font;
            try
            {
                var coll = new FontCollection();
                FontFamily fam;
                // Try to use a cleaner monospace font
                if (System.IO.File.Exists("FreeMono.ttf"))
                    fam = coll.Add("FreeMono.ttf");
                else
                {
                    // Use first available font family as fallback
                    fam = SystemFonts.Families.First();
                }
                font = fam.CreateFont(fontPxSize, FontStyle.Regular);
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
            return tex;
        }

        private void BuildSphere(int latCount, int lonCount, float radius)
        {
            vertices.Clear();
            indices.Clear();

            // Collect noise statistics
            var noiseValues = new List<float>();
            var glyphCounts = new int[GlyphSet.Length];

            for (int lat = 0; lat <= latCount; lat++)
            {
                float theta = lat * MathF.PI / latCount; // 0..PI
                float sinT = MathF.Sin(theta), cosT = MathF.Cos(theta);
                for (int lon = 0; lon <= lonCount; lon++)
                {
                    float phi = lon * 2f * MathF.PI / lonCount; // 0..2PI
                    float sinP = MathF.Sin(phi), cosP = MathF.Cos(phi);
                    Vector3 pos = new Vector3(cosP * sinT, cosT, sinP * sinT) * radius;
                    float n = SimplexNoise.Noise(pos.X * 1.6f, pos.Y * 1.6f, pos.Z * 1.6f) * 0.5f + 0.5f;
                    // Collect noise value for statistics
                    noiseValues.Add(n);

                    vertices.Add(new Vertex { Position = pos, GlyphIndex = 0, GlyphChar = GlyphSet[0], Noise = n, Color = Vector4.One });
                }
            }

            // Find actual noise range and normalize
            float minNoise = noiseValues.Min();
            float maxNoise = noiseValues.Max();
            float noiseRange = maxNoise - minNoise;

            // Reassign glyph indices with normalized noise
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                float n = vertex.Noise;
                
                // Normalize noise to 0-1 range
                float normalizedNoise = (n - minNoise) / noiseRange;
                
                // Map normalized noise to glyph index
                int gi = (int)(normalizedNoise * GlyphSet.Length);
                gi = Math.Clamp(gi, 0, GlyphSet.Length - 1);
                Vector4 col = MapColorFromGlyphIndex(gi);

                // Count glyph usage
                glyphCounts[gi]++;

                // Debug: Print first few vertex mappings
                if (i < 5)
                {
                    Console.WriteLine($"Vertex {i}: noise={n:F3} -> normalized={normalizedNoise:F3} -> glyph_index={gi} -> '{GlyphSet[gi]}'");
                }

                vertices[i] = new Vertex { Position = vertex.Position, GlyphIndex = gi, GlyphChar = GlyphSet[gi], Noise = n, Color = col };
            }

            int lonp1 = lonCount + 1;
            for (int lat = 0; lat < latCount; lat++)
            {
                for (int lon = 0; lon < lonCount; lon++)
                {
                    int a = lat * lonp1 + lon;
                    int b = (lat + 1) * lonp1 + lon;
                    int c = (lat + 1) * lonp1 + (lon + 1);
                    int d = lat * lonp1 + (lon + 1);
                    indices.Add((uint)a); indices.Add((uint)b); indices.Add((uint)c);
                    indices.Add((uint)c); indices.Add((uint)d); indices.Add((uint)a);
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

                Console.WriteLine($"\nGlyph Distribution:");
                for (int i = 0; i < GlyphSet.Length; i++)
                {
                    float percentage = (glyphCounts[i] / (float)noiseValues.Count) * 100f;
                    float expectedRange = (float)i / GlyphSet.Length;
                    float nextRange = (float)(i + 1) / GlyphSet.Length;
                    Console.WriteLine($"  '{GlyphSet[i]}' (index {i}): {glyphCounts[i]} vertices ({percentage:F1}%) - Expected range: [{expectedRange:F3}, {nextRange:F3})");
                }
            }

            // for this prototype we use only vertices for instancing (one instance per vertex)
            instanceCount = vertices.Count;
        }

        private void BuildBackgroundSphere(int latCount, int lonCount, float radius)
        {
            var backgroundVertices = new List<Vector3>();
            var backgroundIndices = new List<uint>();

            // Generate vertices for background sphere
            for (int lat = 0; lat <= latCount; lat++)
            {
                float theta = lat * MathF.PI / latCount; // 0..PI
                float sinT = MathF.Sin(theta), cosT = MathF.Cos(theta);
                for (int lon = 0; lon <= lonCount; lon++)
                {
                    float phi = lon * 2f * MathF.PI / lonCount; // 0..2PI
                    float sinP = MathF.Sin(phi), cosP = MathF.Cos(phi);
                    Vector3 pos = new Vector3(cosP * sinT, cosT, sinP * sinT) * radius;
                    backgroundVertices.Add(pos);
                }
            }

            // Generate indices for background sphere
            int lonp1 = lonCount + 1;
            for (int lat = 0; lat < latCount; lat++)
            {
                for (int lon = 0; lon < lonCount; lon++)
                {
                    int a = lat * lonp1 + lon;
                    int b = (lat + 1) * lonp1 + lon;
                    int c = (lat + 1) * lonp1 + (lon + 1);
                    int d = lat * lonp1 + (lon + 1);
                    backgroundIndices.Add((uint)a); backgroundIndices.Add((uint)b); backgroundIndices.Add((uint)c);
                    backgroundIndices.Add((uint)c); backgroundIndices.Add((uint)d); backgroundIndices.Add((uint)a);
                }
            }

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
        private readonly string vertexShaderSrc = @"
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
{
    // local quad point to world:
    vec3 worldPos = iPos + (iRight * aLocalPos.x + iUp * aLocalPos.y) * iSize * 2.0;
    gl_Position = uProj * uView * vec4(worldPos, 1.0);
    vUv = vec2(iUvRect.x + aLocalUV.x * iUvRect.z, iUvRect.y + aLocalUV.y * iUvRect.w);
    vColor = iColor;
}";

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