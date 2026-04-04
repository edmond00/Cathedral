// SkyCloudRenderer.cs - Decorative cloud and star sky spheres
// Cloud sphere: slightly larger than the world, rotates slowly with Perlin noise patterns
// Sky sphere: much larger, contains stars/planets/moons as scattered glyphs
// Purely visual - no gameplay interaction
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;
using Matrix4 = OpenTK.Mathematics.Matrix4;

namespace Cathedral.Glyph
{
    /// <summary>
    /// Renders decorative cloud and star sky spheres around the world.
    /// Cloud sphere is slightly larger than the world and rotates slowly to simulate wind.
    /// Sky sphere is much larger and contains scattered stars, planets, and moons.
    /// Both are purely decorative with no gameplay interaction.
    /// </summary>
    public class SkyCloudRenderer : IDisposable
    {
        // Character sets pulled from config
        private static string CLOUD_CHARS => Cathedral.Config.SkyCloud.CloudChars;
        private static string SKY_CHARS => Cathedral.Config.SkyCloud.SkyChars;

        // Glyph atlas
        private struct GlyphUV { public float X, Y, W, H; }
        private GlyphUV[] _glyphUVs = null!;
        private Dictionary<char, int> _charToIndex = new();
        private int _glyphTexture;

        // Shader program (shared between cloud and sky)
        private int _shaderProgram;

        // Shared quad geometry
        private int _quadVbo, _quadEbo;

        // Cloud layer
        private int _cloudVao, _cloudInstanceVbo;
        private int _cloudActiveCount;
        private List<DecorativeVertex> _cloudVertices = new();

        // Sky layer
        private int _skyVao, _skyInstanceVbo;
        private int _skyActiveCount;
        private List<DecorativeVertex> _skyVertices = new();
        private float[] _skyStaticData = null!;

        // Cloud rotation (two axes for organic wind-like drift)
        private Vector3 _rotAxis1, _rotAxis2;
        private float _rotSpeed1, _rotSpeed2;
        private float _rotAngle1, _rotAngle2;

        private struct DecorativeVertex
        {
            public Vector3 Position;
            public int GlyphIndex;
            public Vector4 Color;
            public float Size;
        }

        /// <summary>
        /// Initializes all rendering resources for the sky and cloud layers.
        /// Must be called after GL context is active (during OnLoad).
        /// </summary>
        public void Initialize()
        {
            // Build combined character set (deduplicated)
            string allChars = new string((CLOUD_CHARS + SKY_CHARS).Distinct().ToArray());

            BuildGlyphAtlas(allChars);
            _shaderProgram = BuildShader();
            SetupQuadGeometry();

            GenerateCloudVertices();
            GenerateSkyVertices();

            SetupLayerVAO(out _cloudVao, out _cloudInstanceVbo, _cloudActiveCount);
            SetupLayerVAO(out _skyVao, out _skyInstanceVbo, _skyActiveCount);

            // Pre-compute static sky instance data (stars don't move)
            _skyStaticData = BuildInstanceData(_skyVertices, Matrix4.Identity);
            UploadBufferData(_skyInstanceVbo, _skyStaticData);

            // Initialize cloud rotation with two random axes for organic movement
            var rng = new Random(7777);
            _rotAxis1 = Vector3.Normalize(new Vector3(
                (float)(rng.NextDouble() * 2 - 1),
                0.5f + (float)(rng.NextDouble() * 0.5), // Bias upward for more natural spin
                (float)(rng.NextDouble() * 2 - 1)));
            _rotAxis2 = Vector3.Normalize(new Vector3(
                (float)(rng.NextDouble() * 2 - 1),
                (float)(rng.NextDouble() * 2 - 1),
                (float)(rng.NextDouble() * 2 - 1)));
            _rotSpeed1 = Cathedral.Config.SkyCloud.CloudRotationSpeed;
            _rotSpeed2 = Cathedral.Config.SkyCloud.CloudRotationSpeed * 0.37f; // Different speed for complexity

            // Initial cloud buffer upload
            UpdateCloudInstanceBuffer();

            Console.WriteLine($"SkyCloudRenderer initialized: {_cloudActiveCount} cloud glyphs, {_skyActiveCount} star glyphs");
        }

        /// <summary>
        /// Updates cloud rotation animation. Call once per frame.
        /// </summary>
        public void Update(float deltaTime)
        {
            _rotAngle1 += _rotSpeed1 * deltaTime;
            _rotAngle2 += _rotSpeed2 * deltaTime;
            UpdateCloudInstanceBuffer();
        }

        /// <summary>
        /// Renders the sky/star sphere. Call BEFORE the background sphere (stars appear behind the world).
        /// </summary>
        public void RenderSky(Matrix4 view, Matrix4 proj)
        {
            if (_skyActiveCount == 0) return;

            GL.UseProgram(_shaderProgram);
            SetUniforms(view, proj);

            GL.BindVertexArray(_skyVao);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero, _skyActiveCount);
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Renders the cloud sphere. Call AFTER the world glyphs (clouds overlay the world with transparency).
        /// </summary>
        public void RenderClouds(Matrix4 view, Matrix4 proj)
        {
            if (_cloudActiveCount == 0) return;

            GL.UseProgram(_shaderProgram);
            SetUniforms(view, proj);

            // Ensure blending and disable depth writing for transparent cloud rendering
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false);

            GL.BindVertexArray(_cloudVao);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero, _cloudActiveCount);
            GL.BindVertexArray(0);

            // Restore depth writing
            GL.DepthMask(true);
        }

        private void SetUniforms(Matrix4 view, Matrix4 proj)
        {
            int viewLoc = GL.GetUniformLocation(_shaderProgram, "uView");
            int projLoc = GL.GetUniformLocation(_shaderProgram, "uProj");
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref proj);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _glyphTexture);
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uAtlas"), 0);
        }

        #region Geometry Generation

        private void GenerateCloudVertices()
        {
            var positions = BuildIcospherePositions(
                Cathedral.Config.SkyCloud.CloudSubdivisions,
                Cathedral.Config.SkyCloud.CloudSphereRadius);

            _cloudVertices.Clear();
            float radius = Cathedral.Config.SkyCloud.CloudSphereRadius;
            float noiseScale = Cathedral.Config.SkyCloud.CloudNoiseScale;
            float threshold = Cathedral.Config.SkyCloud.CloudNoiseThreshold;

            foreach (var pos in positions)
            {
                // Sample 3D Perlin fBm noise at vertex position
                float noise = Perlin.Fbm(
                    pos.X * noiseScale / radius,
                    pos.Y * noiseScale / radius,
                    pos.Z * noiseScale / radius, 4);

                // Remap from [-1,1] to [0,1]
                noise = (noise + 1f) * 0.5f;

                if (noise <= threshold) continue;

                // Intensity 0..1 within cloud region
                float intensity = (noise - threshold) / (1f - threshold);

                // Choose cloud character from config string based on intensity
                string cloudChars = CLOUD_CHARS;
                int charIdx = Math.Clamp((int)(intensity * cloudChars.Length), 0, cloudChars.Length - 1);
                char c = cloudChars[charIdx];

                float size = MathHelper.Lerp(
                    Cathedral.Config.SkyCloud.CloudGlyphMinSize,
                    Cathedral.Config.SkyCloud.CloudGlyphMaxSize,
                    intensity);
                float opacity = Cathedral.Config.SkyCloud.CloudBaseOpacity * (0.5f + intensity * 0.5f);
                float brightness = 0.6f + intensity * 0.4f;

                _cloudVertices.Add(new DecorativeVertex
                {
                    Position = pos,
                    GlyphIndex = _charToIndex[c],
                    Color = new Vector4(brightness, brightness, brightness, opacity),
                    Size = size
                });
            }
            _cloudActiveCount = _cloudVertices.Count;
        }

        private void GenerateSkyVertices()
        {
            var positions = BuildIcospherePositions(
                Cathedral.Config.SkyCloud.SkySubdivisions,
                Cathedral.Config.SkyCloud.SkySphereRadius);
            var rng = new Random(9999);

            _skyVertices.Clear();
            foreach (var pos in positions)
            {
                if (rng.NextDouble() > Cathedral.Config.SkyCloud.StarDensity)
                    continue;

                double roll = rng.NextDouble();
                char starChar;
                float size;
                Vector4 color;

                // Distribute sky chars by rarity: first char most common, last char rarest
                // Build cumulative thresholds from SkyChars length
                string skyChars = SKY_CHARS;
                int skyCharCount = skyChars.Length;
                // Map roll to character index: exponential distribution favoring earlier chars
                // e.g. 4 chars -> thresholds ~0.65, 0.90, 0.97, 1.0
                int charIndex = 0;
                {
                    double cumulative = 0;
                    double remaining = 1.0;
                    for (int ci = 0; ci < skyCharCount - 1; ci++)
                    {
                        // Each successive char gets ~1/3 of the remaining probability
                        double band = remaining * 0.65;
                        if (skyCharCount - ci == 2) band = remaining * 0.7; // second-to-last gets more
                        cumulative += band;
                        remaining -= band;
                        if (roll < cumulative) { charIndex = ci; break; }
                        charIndex = ci + 1;
                    }
                }
                starChar = skyChars[charIndex];
                
                // Size and color vary by rarity (charIndex)
                float rarityFactor = (float)charIndex / Math.Max(1, skyCharCount - 1); // 0=common, 1=rare

                if (rarityFactor < 0.34f) // Dim/common
                {
                    float brightness = 0.3f + (float)rng.NextDouble() * 0.4f;
                    size = MathHelper.Lerp(
                        Cathedral.Config.SkyCloud.SkyStarMinSize,
                        Cathedral.Config.SkyCloud.SkyStarMinSize * 1.5f,
                        (float)rng.NextDouble());
                    color = new Vector4(brightness, brightness, brightness, 1.0f);
                }
                else if (rarityFactor < 0.67f) // Medium
                {
                    float brightness = 0.6f + (float)rng.NextDouble() * 0.4f;
                    size = MathHelper.Lerp(
                        Cathedral.Config.SkyCloud.SkyStarMinSize,
                        Cathedral.Config.SkyCloud.SkyStarMaxSize * 0.5f,
                        (float)rng.NextDouble());
                    color = new Vector4(brightness, brightness, brightness * 0.95f, 1.0f);
                }
                else if (rarityFactor < 0.9f) // Rare
                {
                    size = MathHelper.Lerp(
                        Cathedral.Config.SkyCloud.SkyStarMaxSize * 0.4f,
                        Cathedral.Config.SkyCloud.SkyStarMaxSize * 0.7f,
                        (float)rng.NextDouble());
                    float r = 0.5f + (float)rng.NextDouble() * 0.4f;
                    float g = 0.5f + (float)rng.NextDouble() * 0.4f;
                    float b = 0.5f + (float)rng.NextDouble() * 0.4f;
                    color = new Vector4(r, g, b, 1.0f);
                }
                else // Very rare (last char)
                {
                    size = MathHelper.Lerp(
                        Cathedral.Config.SkyCloud.SkyStarMaxSize * 0.6f,
                        Cathedral.Config.SkyCloud.SkyStarMaxSize,
                        (float)rng.NextDouble());
                    float tint = (float)rng.NextDouble() * 0.3f;
                    color = new Vector4(0.9f + tint * 0.1f, 0.9f + tint * 0.05f, 0.8f - tint * 0.2f, 1.0f);
                }

                _skyVertices.Add(new DecorativeVertex
                {
                    Position = pos,
                    GlyphIndex = _charToIndex[starChar],
                    Color = color,
                    Size = size
                });
            }
            _skyActiveCount = _skyVertices.Count;
        }

        /// <summary>
        /// Builds icosphere vertex positions (no indices needed for instanced rendering).
        /// </summary>
        private List<Vector3> BuildIcospherePositions(int subdivisions, float radius)
        {
            float t = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;
            float scale = radius / MathF.Sqrt(1 + t * t);

            var verts = new List<Vector3>
            {
                new Vector3(-1,  t,  0) * scale, new Vector3( 1,  t,  0) * scale,
                new Vector3(-1, -t,  0) * scale, new Vector3( 1, -t,  0) * scale,
                new Vector3( 0, -1,  t) * scale, new Vector3( 0,  1,  t) * scale,
                new Vector3( 0, -1, -t) * scale, new Vector3( 0,  1, -t) * scale,
                new Vector3( t,  0, -1) * scale, new Vector3( t,  0,  1) * scale,
                new Vector3(-t,  0, -1) * scale, new Vector3(-t,  0,  1) * scale
            };

            for (int i = 0; i < verts.Count; i++)
                verts[i] = Vector3.Normalize(verts[i]) * radius;

            var indices = new List<uint>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };

            for (int level = 0; level < subdivisions; level++)
            {
                var newVerts = new List<Vector3>(verts);
                var newIdx = new List<uint>();
                var cache = new Dictionary<(int, int), int>();

                for (int i = 0; i < indices.Count; i += 3)
                {
                    uint i1 = indices[i], i2 = indices[i + 1], i3 = indices[i + 2];
                    int a = Midpoint(i1, i2, verts, newVerts, cache, radius);
                    int b = Midpoint(i2, i3, verts, newVerts, cache, radius);
                    int c = Midpoint(i3, i1, verts, newVerts, cache, radius);
                    newIdx.AddRange(new uint[] {
                        i1, (uint)a, (uint)c,
                        i2, (uint)b, (uint)a,
                        i3, (uint)c, (uint)b,
                        (uint)a, (uint)b, (uint)c
                    });
                }
                verts = newVerts;
                indices = newIdx;
            }

            return verts;
        }

        private int Midpoint(uint i1, uint i2, List<Vector3> old, List<Vector3> current,
            Dictionary<(int, int), int> cache, float radius)
        {
            var key = i1 < i2 ? ((int)i1, (int)i2) : ((int)i2, (int)i1);
            if (cache.TryGetValue(key, out int idx)) return idx;
            var mid = Vector3.Normalize((old[(int)i1] + old[(int)i2]) / 2f) * radius;
            int newIdx = current.Count;
            current.Add(mid);
            cache[key] = newIdx;
            return newIdx;
        }

        #endregion

        #region Instance Data

        private float[] BuildInstanceData(List<DecorativeVertex> vertices, Matrix4 rotation, float quadSize = 0f)
        {
            if (quadSize <= 0f) quadSize = Cathedral.Config.GlyphSphere.QuadSize;
            float[] data = new float[vertices.Count * 18];
            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];

                // Apply rotation to position (for cloud drift)
                var pos4 = new Vector4(v.Position, 1.0f) * rotation;
                var pos = new Vector3(pos4.X, pos4.Y, pos4.Z);

                // Compute billboard orientation from the rotated position
                var normal = Vector3.Normalize(pos);
                Vector3 pole = Vector3.UnitY;
                var poleProj = pole - normal * Vector3.Dot(pole, normal);
                if (poleProj.LengthSquared < 1e-6f)
                    poleProj = Vector3.Cross(normal, Vector3.UnitX);
                poleProj = Vector3.Normalize(poleProj);
                var right = Vector3.Normalize(Vector3.Cross(poleProj, normal));
                var up = poleProj;

                float size = quadSize * v.Size;
                var uv = _glyphUVs[v.GlyphIndex];
                var col = v.Color;

                int b = i * 18;
                data[b + 0] = pos.X; data[b + 1] = pos.Y; data[b + 2] = pos.Z;
                data[b + 3] = right.X; data[b + 4] = right.Y; data[b + 5] = right.Z;
                data[b + 6] = up.X; data[b + 7] = up.Y; data[b + 8] = up.Z;
                data[b + 9] = size;
                data[b + 10] = uv.X; data[b + 11] = uv.Y; data[b + 12] = uv.W; data[b + 13] = uv.H;
                data[b + 14] = col.X; data[b + 15] = col.Y; data[b + 16] = col.Z; data[b + 17] = col.W;
            }
            return data;
        }

        private void UpdateCloudInstanceBuffer()
        {
            if (_cloudActiveCount == 0) return;

            var rot = Matrix4.CreateFromAxisAngle(_rotAxis1, MathHelper.DegreesToRadians(_rotAngle1))
                    * Matrix4.CreateFromAxisAngle(_rotAxis2, MathHelper.DegreesToRadians(_rotAngle2));

            float[] data = BuildInstanceData(_cloudVertices, rot, Cathedral.Config.SkyCloud.CloudQuadSize);
            UploadBufferData(_cloudInstanceVbo, data);
        }

        private void UploadBufferData(int vbo, float[] data)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.Length * sizeof(float), data);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        #endregion

        #region GL Setup

        private void SetupQuadGeometry()
        {
            float[] quadVerts = {
                -0.5f, -0.5f, 0f, 0f,
                 0.5f, -0.5f, 1f, 0f,
                 0.5f,  0.5f, 1f, 1f,
                -0.5f,  0.5f, 0f, 1f
            };
            _quadVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVerts.Length * sizeof(float), quadVerts, BufferUsageHint.StaticDraw);

            uint[] quadIdx = { 0, 1, 2, 2, 3, 0 };
            _quadEbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _quadEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, quadIdx.Length * sizeof(uint), quadIdx, BufferUsageHint.StaticDraw);
        }

        private void SetupLayerVAO(out int vao, out int instanceVbo, int maxInstances)
        {
            // Ensure at least 1 instance capacity to avoid zero-size buffer
            maxInstances = Math.Max(maxInstances, 1);

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            // Quad VBO: attrib 0 (pos.xy) and 1 (uv.xy)
            GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            // EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _quadEbo);

            // Instance VBO
            instanceVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, maxInstances * 18 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

            int stride = 18 * sizeof(float);
            int attr = 2;

            // instancePos vec3
            GL.EnableVertexAttribArray(attr);
            GL.VertexAttribPointer(attr, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.VertexAttribDivisor(attr++, 1);

            // right vec3
            GL.EnableVertexAttribArray(attr);
            GL.VertexAttribPointer(attr, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.VertexAttribDivisor(attr++, 1);

            // up vec3
            GL.EnableVertexAttribArray(attr);
            GL.VertexAttribPointer(attr, 3, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.VertexAttribDivisor(attr++, 1);

            // size float
            GL.EnableVertexAttribArray(attr);
            GL.VertexAttribPointer(attr, 1, VertexAttribPointerType.Float, false, stride, 9 * sizeof(float));
            GL.VertexAttribDivisor(attr++, 1);

            // uvRect vec4
            GL.EnableVertexAttribArray(attr);
            GL.VertexAttribPointer(attr, 4, VertexAttribPointerType.Float, false, stride, 10 * sizeof(float));
            GL.VertexAttribDivisor(attr++, 1);

            // color vec4
            GL.EnableVertexAttribArray(attr);
            GL.VertexAttribPointer(attr, 4, VertexAttribPointerType.Float, false, stride, 14 * sizeof(float));
            GL.VertexAttribDivisor(attr++, 1);

            GL.BindVertexArray(0);
        }

        #endregion

        #region Glyph Atlas & Shader

        private void BuildGlyphAtlas(string chars)
        {
            int cellSize = Cathedral.Config.GlyphSphere.GlyphCellSize;
            int fontPxSize = Cathedral.Config.GlyphSphere.GlyphFontSize;

            int cols = chars.Length;
            int atlasW = cols * cellSize;
            int atlasH = cellSize;

            using var atlas = new Image<Rgba32>(atlasW, atlasH, Color.Transparent);

            // Load font
            Font font;
            var coll = new FontCollection();
            try
            {
                string fontPath = Cathedral.Config.Terminal.FontPath;
                if (System.IO.File.Exists(fontPath))
                {
                    var fam = coll.Add(fontPath);
                    font = fam.CreateFont(fontPxSize, FontStyle.Regular);
                }
                else throw new FileNotFoundException($"Font not found: {fontPath}");
            }
            catch
            {
                font = SystemFonts.CreateFont("Consolas", fontPxSize, FontStyle.Regular);
            }

            _glyphUVs = new GlyphUV[chars.Length];
            _charToIndex.Clear();

            for (int i = 0; i < chars.Length; i++)
            {
                int x = i * cellSize;
                _charToIndex[chars[i]] = i;

                atlas.Mutate(ctx =>
                {
                    var opts = new RichTextOptions(font)
                    {
                        Origin = new PointF(x + cellSize / 2f, cellSize / 2f),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    ctx.DrawText(opts, chars[i].ToString(), Color.White);
                });

                _glyphUVs[i] = new GlyphUV
                {
                    X = (float)x / atlasW,
                    Y = 0f,
                    W = (float)cellSize / atlasW,
                    H = 1.0f
                };
            }

            // Upload texture
            _glyphTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _glyphTexture);

            atlas.Mutate(x => x.Flip(FlipMode.Vertical));
            var pixels = new Rgba32[atlas.Width * atlas.Height];
            atlas.CopyPixelDataTo(pixels);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                atlas.Width, atlas.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        private int BuildShader()
        {
            // Vertex shader - same billboard approach as the world glyphs
            string vs = $@"
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
    vec3 worldPos = iPos + (iRight * aLocalPos.x + iUp * aLocalPos.y) * iSize * {Cathedral.Config.GlyphSphere.VertexShaderSizeMultiplier};
    gl_Position = uProj * uView * vec4(worldPos, 1.0);
    vUv = vec2(iUvRect.x + aLocalUV.x * iUvRect.z, iUvRect.y + aLocalUV.y * iUvRect.w);
    vColor = iColor;
}}";

            // Fragment shader - supports alpha transparency for clouds
            string fs = @"
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
        FragColor = vec4(vColor.rgb, vColor.a);
    } else {
        discard;
    }
}";

            return CompileProgram(vs, fs);
        }

        private int CompileProgram(string vsSrc, string fsSrc)
        {
            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vsSrc);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int status);
            if (status == 0) throw new Exception("SkyCloud VS compile: " + GL.GetShaderInfoLog(vs));

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fsSrc);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out status);
            if (status == 0) throw new Exception("SkyCloud FS compile: " + GL.GetShaderInfoLog(fs));

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, vs);
            GL.AttachShader(prog, fs);
            GL.LinkProgram(prog);
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out status);
            if (status == 0) throw new Exception("SkyCloud Linker: " + GL.GetProgramInfoLog(prog));

            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
            return prog;
        }

        #endregion

        public void Dispose()
        {
            GL.DeleteVertexArray(_cloudVao);
            GL.DeleteVertexArray(_skyVao);
            GL.DeleteBuffer(_cloudInstanceVbo);
            GL.DeleteBuffer(_skyInstanceVbo);
            GL.DeleteBuffer(_quadVbo);
            GL.DeleteBuffer(_quadEbo);
            GL.DeleteTexture(_glyphTexture);
            GL.DeleteProgram(_shaderProgram);
        }
    }
}
