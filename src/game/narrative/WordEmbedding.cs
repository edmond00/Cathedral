using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Loads a pretrained word-embedding dictionary (GloVe / fastText text format: one word + its
/// floats per line) and exposes vector lookup + cosine similarity. Used to pick the observation
/// keyword as the noun most semantically related to the observation object's reference word.
///
/// The vectors file ships in <c>models/embeddings/</c> and is resolved by walking up from the app
/// base directory to the project's <c>models</c> folder (same pattern as the llama models). If the
/// file is absent or fails to load, <see cref="IsReady"/> stays false and callers fall back to a
/// heuristic — the feature degrades gracefully and never blocks startup.
/// </summary>
public static class WordEmbedding
{
    private static Dictionary<string, float[]>? _vectors;
    private static bool _initialized;

    public static bool IsReady => _vectors is { Count: > 0 };

    public static async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            var path = ResolveVectorsPath();
            if (path == null)
            {
                Console.WriteLine("WordEmbedding: no vectors file in models/embeddings — keyword similarity disabled.");
                return;
            }
            await Task.Run(() => Load(path));
            Console.WriteLine($"WordEmbedding: loaded {_vectors?.Count ?? 0} vectors from {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WordEmbedding: load failed, similarity disabled. {ex.Message}");
            _vectors = null;
        }
    }

    /// <summary>Returns the vector for <paramref name="word"/> (case-insensitive), or null if absent.</summary>
    public static float[]? GetVector(string word)
        => _vectors != null && !string.IsNullOrEmpty(word) && _vectors.TryGetValue(word.ToLowerInvariant(), out var v)
            ? v : null;

    public static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        if (na == 0 || nb == 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    // ── Loading ──────────────────────────────────────────────────────────────

    private static void Load(string path)
    {
        var dict = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StreamReader(path);
        string? line;
        bool first = true;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0) continue;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Skip a fastText-style header line ("<count> <dim>"). GloVe has no header.
            if (first)
            {
                first = false;
                if (parts.Length == 2 && int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _)) continue;
            }
            if (parts.Length < 3) continue;

            var vec = new float[parts.Length - 1];
            bool ok = true;
            for (int i = 1; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out vec[i - 1])) { ok = false; break; }
            }
            if (ok) dict[parts[0]] = vec;
        }
        _vectors = dict;
    }

    private static string? ResolveVectorsPath()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "models")))
            dir = Directory.GetParent(dir)?.FullName;
        if (dir == null) return null;
        var candidate = Path.Combine(dir, "models", "embeddings", "glove.6B.100d.txt");
        return File.Exists(candidate) ? candidate : null;
    }
}
