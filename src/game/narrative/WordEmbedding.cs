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
/// base directory to the project's <c>models</c> folder (same pattern as the llama models). The
/// file is required: if it is missing the game prints download instructions and exits.
/// </summary>
public static class WordEmbedding
{
    /// <summary>Vectors file name expected in <c>models/embeddings/</c>.</summary>
    private const string VectorsFileName = "glove.6B.100d.txt";
    /// <summary>Where to obtain the vectors file when it is missing.</summary>
    private const string GloveDownloadUrl = "https://nlp.stanford.edu/data/glove.6B.zip";

    /// <summary>
    /// How many lines of the vectors file form the centrality pool. The file is written in
    /// descending frequency order, so the head is ordinary English vocabulary — which is the pool a
    /// narration sentence's words are actually drawn from, and therefore the right thing to measure
    /// a word's centrality against. Measuring against the whole 400k file instead would measure
    /// centrality among rare tokens, numbers and misspellings, which inverts the answer: "time"
    /// scores as the *least* central word in the file and "pigsty" as the most.
    /// </summary>
    private const int CentralityPoolSize = 20000;

    private static Dictionary<string, float[]>? _vectors;
    /// <summary>Mean of the unit vectors of the centrality pool. See <see cref="Centrality"/>.</summary>
    private static float[]? _poolCentroid;
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
                Console.Error.WriteLine($"WordEmbedding: required vectors file '{VectorsFileName}' not found in models/embeddings/.");
                Console.Error.WriteLine($"Download GloVe vectors from {GloveDownloadUrl}, extract '{VectorsFileName}', and place it in models/embeddings/, then restart.");
                Environment.Exit(1);
            }
            await Task.Run(() => Load(path));
            Console.WriteLine($"WordEmbedding: loaded {_vectors?.Count ?? 0} vectors from {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WordEmbedding: failed to load '{VectorsFileName}'. {ex.Message}");
            Environment.Exit(1);
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

    /// <summary>
    /// <paramref name="word"/>'s mean cosine to ordinary English vocabulary — how close it sits to
    /// <b>everything at once</b>. 0 when the word is unknown, so an absent word is neither rewarded
    /// nor punished.
    ///
    /// <para>This is the correction for <i>hubness</i>. Cosine in a co-occurrence embedding measures
    /// relatedness, and a frequent, general word is related to everything: measured against the
    /// 18,499 common words in the pool, "time" and "way" score +0.27 and sit above 99% of the
    /// vocabulary, while the words this game is made of sit at the far end — "hearth" +0.03, "anvil"
    /// +0.01, "pallet" −0.04, "pigsty" −0.12. A hub therefore carries a floor of similarity to any
    /// anchor you ask about, and wins a raw-cosine ranking against a specific word that is genuinely
    /// closer. Subtracting this baseline from the similarity (see <c>KeywordExtractor</c>) scores a
    /// candidate on how much closer it is to the anchor <i>than to words in general</i>.</para>
    ///
    /// <para>Cheap because the mean of cosines is a cosine with the mean: mean_i(unit(w)·unit(p_i))
    /// = unit(w)·mean_i(unit(p_i)), so the whole pool collapses to one centroid at load time and a
    /// query is a single dot product.</para>
    ///
    /// <para>Note the obvious alternative — mean-centering the vectors, the usual remedy for GloVe's
    /// anisotropy — does <b>not</b> work here and was measured making it worse: centred, "wolf" ranks
    /// <c>time</c> above <c>shadow</c> and <c>fur</c>, promoting the hub from third place to first.</para>
    /// </summary>
    public static double Centrality(string word)
    {
        var v = GetVector(word);
        if (v == null || _poolCentroid == null || v.Length != _poolCentroid.Length) return 0;
        double dot = 0, na = 0;
        for (int i = 0; i < v.Length; i++) { dot += v[i] * _poolCentroid[i]; na += v[i] * v[i]; }
        return na == 0 ? 0 : dot / Math.Sqrt(na);
    }

    // ── Loading ──────────────────────────────────────────────────────────────

    private static void Load(string path)
    {
        var dict = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
        double[]? centroid = null;
        int pooled = 0, lineNo = 0;

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
            if (!ok) continue;

            dict[parts[0]] = vec;

            // Accumulate the centrality pool from the frequency-ordered head, taking only plain
            // alphabetic words: the head also holds punctuation, digits and markup, none of which a
            // narration sentence competes against.
            if (lineNo++ < CentralityPoolSize && IsPlainWord(parts[0]))
            {
                centroid ??= new double[vec.Length];
                if (centroid.Length == vec.Length) AddUnit(centroid, vec, ref pooled);
            }
        }

        _vectors = dict;
        _poolCentroid = pooled > 0 && centroid != null ? Finish(centroid, pooled) : null;
        if (_poolCentroid == null)
            Console.Error.WriteLine("WordEmbedding: no centrality pool built — keyword ranking falls back to raw similarity.");
        else
            Console.WriteLine($"WordEmbedding: centrality pool of {pooled} common words.");
    }

    private static bool IsPlainWord(string w)
    {
        if (w.Length < 3) return false;
        foreach (var c in w) if (c is < 'a' or > 'z') return false;
        return true;
    }

    /// <summary>Adds <paramref name="vec"/>'s unit vector into the running sum; a zero vector is skipped.</summary>
    private static void AddUnit(double[] sum, float[] vec, ref int pooled)
    {
        double norm = 0;
        foreach (var f in vec) norm += f * f;
        if (norm <= 0) return;
        norm = Math.Sqrt(norm);
        for (int i = 0; i < vec.Length; i++) sum[i] += vec[i] / norm;
        pooled++;
    }

    private static float[] Finish(double[] sum, int pooled)
    {
        var result = new float[sum.Length];
        for (int i = 0; i < sum.Length; i++) result[i] = (float)(sum[i] / pooled);
        return result;
    }

    private static string? ResolveVectorsPath()
    {
        var candidate = ModelsDirectory.PathTo("embeddings", VectorsFileName);
        return candidate != null && File.Exists(candidate) ? candidate : null;
    }
}
