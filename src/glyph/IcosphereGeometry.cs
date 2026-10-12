// IcosphereGeometry.cs — the sphere's mesh, on its own, with no OpenGL anywhere near it.
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Cathedral.Glyph
{
    /// <summary>
    /// The subdivided icosahedron the world is drawn on: positions and triangles, and the
    /// vertex adjacency those triangles imply.
    ///
    /// <para><b>Why this is its own class.</b> The mesh used to be built by two private copies of
    /// the same forty lines inside <see cref="GlyphSphereCore"/>, and a third in the sky renderer.
    /// Both copies in the core now call this, so the world sphere and the background sphere cannot
    /// drift apart — and, more to the point, something outside the window can build the same sphere.
    /// <c>--world-variant-audit</c> has to classify forty thousand vertices for every variant on
    /// every sample seed, and it cannot open a window to do it.</para>
    ///
    /// <para>Vertex order is part of the contract. Vertex <c>i</c> here is vertex <c>i</c> in the
    /// running game, so a headless measurement of a world describes the world a player would walk.
    /// That falls out of building the mesh the same way rather than being enforced, which is exactly
    /// why there must be only one way.</para>
    /// </summary>
    public static class IcosphereGeometry
    {
        /// <summary>
        /// Positions and triangle indices for the icosphere at <paramref name="subdivisions"/>.
        /// </summary>
        public static (List<Vector3> Vertices, List<uint> Indices) Build(int subdivisions, float radius)
        {
            float t = (1.0f + MathF.Sqrt(5.0f)) / 2.0f; // golden ratio
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
                baseVertices[i] = Vector3.Normalize(baseVertices[i]) * radius;

            var baseIndices = new List<uint>
            {
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            var currentVertices = new List<Vector3>(baseVertices);
            var currentIndices  = new List<uint>(baseIndices);

            for (int level = 0; level < subdivisions; level++)
            {
                var newVertices = new List<Vector3>(currentVertices);
                var newIndices  = new List<uint>();
                var midPointCache = new Dictionary<(int, int), int>();

                for (int i = 0; i < currentIndices.Count; i += 3)
                {
                    uint i1 = currentIndices[i], i2 = currentIndices[i + 1], i3 = currentIndices[i + 2];
                    int a = Midpoint(i1, i2, currentVertices, newVertices, midPointCache, radius);
                    int b = Midpoint(i2, i3, currentVertices, newVertices, midPointCache, radius);
                    int c = Midpoint(i3, i1, currentVertices, newVertices, midPointCache, radius);
                    newIndices.AddRange(new uint[]
                    {
                        i1, (uint)a, (uint)c,
                        i2, (uint)b, (uint)a,
                        i3, (uint)c, (uint)b,
                        (uint)a, (uint)b, (uint)c
                    });
                }

                currentVertices = newVertices;
                currentIndices  = newIndices;
            }

            return (currentVertices, currentIndices);
        }

        /// <summary>
        /// The neighbour list every vertex gets from the triangles that touch it — the same edges
        /// <see cref="GlyphSphereGraph"/> builds its pathfinding graph out of, without the costs.
        /// </summary>
        public static List<int>[] Adjacency(int vertexCount, List<uint> indices)
        {
            var sets = new HashSet<int>[vertexCount];
            for (int i = 0; i < vertexCount; i++) sets[i] = new HashSet<int>();

            for (int i = 0; i + 2 < indices.Count; i += 3)
            {
                int v0 = (int)indices[i], v1 = (int)indices[i + 1], v2 = (int)indices[i + 2];
                Link(sets, v0, v1);
                Link(sets, v1, v2);
                Link(sets, v2, v0);
            }

            var adjacency = new List<int>[vertexCount];
            for (int i = 0; i < vertexCount; i++) adjacency[i] = new List<int>(sets[i]);
            return adjacency;
        }

        private static void Link(HashSet<int>[] sets, int a, int b)
        {
            if (a == b) return;
            sets[a].Add(b);
            sets[b].Add(a);
        }

        private static int Midpoint(uint i1, uint i2, List<Vector3> oldVertices, List<Vector3> newVertices,
                                    Dictionary<(int, int), int> cache, float radius)
        {
            var key = i1 < i2 ? ((int)i1, (int)i2) : ((int)i2, (int)i1);
            if (cache.TryGetValue(key, out int cached)) return cached;

            Vector3 mid = (oldVertices[(int)i1] + oldVertices[(int)i2]) / 2.0f;
            mid = Vector3.Normalize(mid) * radius;

            int index = newVertices.Count;
            newVertices.Add(mid);
            cache[key] = index;
            return index;
        }
    }
}
