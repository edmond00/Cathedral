using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Catalyst;
using Catalyst.Models;
using Mosaik.Core;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;
using Cathedral.Game;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// 3-layer pipeline that intercepts every LLM-generated text before it reaches the UI
/// and corrects anachronisms, modern concepts, real-world proper names, and informal language.
///
/// Layer 1 — ForbiddenWordsDictionary: direct word replacement (sync, always active).
/// Layer 2 — Catalyst WikiNER: detects real-world named entities (person/location/org/misc).
/// Layer 3 — Catalyst Spotter: detects domain-specific forbidden terms from SpottedTerms.
///
/// If Layer 2 or 3 detects anything, one LLM rewrite call corrects the text.
/// Texts coming FROM the rewriter are not re-fed into this pipeline (no infinite loop).
///
/// Mirrors the static-singleton pattern of NounExtractor.
/// </summary>
public static class TextSanitizationPipeline
{
    // Shared Catalyst pipeline — Spotter (always) + WikiNER (if available)
    private static Pipeline?            _catalystPipeline;
    private static LlamaServerManager?  _llamaServer;
    private static int                  _rewriterSlot = -1;
    private static bool                 _catalystReady = false;
    private static bool                 _llmReady      = false;
    private static bool                 _initialized   = false;

    // Regex fallback for Layer 3 when Catalyst fails to initialise at all
    private static Regex? _spottedPattern;

    // Entity type the Layer 3 Spotter tags its hits with; used to tell Spotter
    // (anachronism) hits apart from WikiNER (real-world name) hits.
    private const string SpotterTag = "FORBIDDEN";

    public static bool IsReady => _initialized;

    // ── Initialisation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the pipeline. Safe to call multiple times (idempotent).
    /// Failures are caught and logged; each layer degrades independently.
    /// </summary>
    public static async Task InitializeAsync(string modelStoragePath, LlamaServerManager llamaServer)
    {
        if (_initialized) return;

        _llamaServer = llamaServer;

        // ── Common-word guard for Layer 2 (WikiNER false positives) ───────────
        CommonWordLexicon.Initialize();

        // ── Build regex fallback for Layer 3 (always available) ───────────────
        var escaped = SpottedTerms.All
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(Regex.Escape);
        _spottedPattern = new Regex(
            @"\b(?:" + string.Join("|", escaped) + @")\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ── Catalyst pipeline (Layer 2 NER + Layer 3 Spotter) ─────────────────
        try
        {
            // NounExtractor.InitializeAsync registers English models and sets Storage.Current
            await NounExtractor.InitializeAsync(modelStoragePath);

            var pipeline = await Pipeline.ForAsync(Language.English);

            // Layer 3 — Spotter (no external model required, populated inline)
            var spotter = new Spotter(Language.English, 0, "ForbiddenTerms", SpotterTag);
            spotter.IgnoreCase = true;
            spotter.AppendList(SpottedTerms.All.Where(t => !string.IsNullOrWhiteSpace(t)));
            pipeline.Add(spotter);

            // Layer 2 — WikiNER (requires downloaded model; gracefully skipped if absent)
            try
            {
                var ner = await AveragePerceptronEntityRecognizer.FromStoreAsync(
                    Language.English, 0, "WikiNER");
                pipeline.Add(ner);
                Console.WriteLine("TextSanitizationPipeline: WikiNER NER layer ready.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"TextSanitizationPipeline: WikiNER unavailable, NER layer disabled. {ex.Message}");
            }

            _catalystPipeline = pipeline;
            _catalystReady    = true;
            Console.WriteLine("TextSanitizationPipeline: Catalyst pipeline ready (Spotter active).");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"TextSanitizationPipeline: Catalyst init failed, using regex fallback for Layer 3. {ex.Message}");
        }

        // ── Rewriter LLM slot ──────────────────────────────────────────────────
        try
        {
            _rewriterSlot = await llamaServer.CreateInstanceAsync(RewriterSystemPrompt);
            _llmReady     = true;
            Console.WriteLine($"TextSanitizationPipeline: Rewriter LLM slot {_rewriterSlot} ready.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"TextSanitizationPipeline: Failed to create rewriter LLM slot. {ex.Message}");
        }

        _initialized = true;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the 3-layer sanitization on <paramref name="text"/> and returns the corrected result.
    /// Always runs Layer 1. Layers 2/3 degrade gracefully when unavailable.
    /// </summary>
    public static async Task<string> SanitizeAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Layer 1 — dictionary replacement (always runs, sync)
        text = ForbiddenWordsDictionary.Apply(text);

        // In playground mode, observation text is already a placeholder — skip Catalyst + LLM.
        if (PlaygroundMode.IsActive)
            return text;

        // Layer 3 (Spotter / SpottedTerms) → genuine anachronisms.
        // Layer 2 (WikiNER)                → real-world proper names.
        // These are described differently in the rewrite prompt.
        var anachronisms   = new List<string>();
        var realWorldNames = new List<string>();

        // Layers 2 + 3 via Catalyst pipeline
        if (_catalystReady && _catalystPipeline != null)
        {
            try
            {
                var doc = new Document(text, Language.English);
                _catalystPipeline.ProcessSingle(doc);

                foreach (var span in doc)
                    foreach (var token in span)
                    {
                        if (token.EntityTypes.Count == 0)
                            continue;

                        // The Spotter tags its hits with type "FORBIDDEN"; anything else
                        // is a WikiNER named-entity type (Person/Location/Organization/…).
                        bool isAnachronism = token.EntityTypes.Any(et => et.Type == SpotterTag);

                        if (isAnachronism)
                        {
                            anachronisms.Add(token.Value);
                        }
                        else if (CommonWordLexicon.IsCommonWord(token.Value))
                        {
                            // Layer 2 false positive: an ordinary common word that WikiNER
                            // flagged only because it was capitalised (e.g. "Nostalgia").
                            Console.WriteLine(
                                $"TextSanitizationPipeline: guard dropped WikiNER false positive '{token.Value}'.");
                        }
                        else
                        {
                            realWorldNames.Add(token.Value);
                        }
                    }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"TextSanitizationPipeline: Catalyst detection failed. {ex.Message}");
            }
        }
        else if (_spottedPattern != null)
        {
            // Regex fallback for Layer 3 when Catalyst is unavailable (anachronisms only).
            foreach (Match m in _spottedPattern.Matches(text))
                anachronisms.Add(m.Value);
        }

        if (anachronisms.Count == 0 && realWorldNames.Count == 0)
            return text;

        if (!_llmReady || _llamaServer == null || _rewriterSlot < 0)
        {
            var all = anachronisms.Concat(realWorldNames).Distinct();
            Console.WriteLine(
                $"TextSanitizationPipeline: detected [{string.Join(", ", all)}] but rewriter LLM unavailable.");
            return text;
        }

        return await RewriteAsync(text, anachronisms, realWorldNames);
    }

    // ── Rewriter ───────────────────────────────────────────────────────────────

    private static async Task<string> RewriteAsync(
        string text, List<string> anachronisms, List<string> realWorldNames)
    {
        try
        {
            var distinctAnachronisms = anachronisms.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var distinctNames        = realWorldNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Console.WriteLine(
                "TextSanitizationPipeline: rewriting — " +
                $"anachronisms [{string.Join(", ", distinctAnachronisms)}] " +
                $"names [{string.Join(", ", distinctNames)}]");

            var prompt  = BuildRewritePrompt(text, distinctAnachronisms, distinctNames);
            var schema  = LLMSchemaConfig.CreateContinuationObservationSchema("rewritten_text");
            var grammar = JsonConstraintGenerator.GenerateGBNF(schema);

            var json = await _llamaServer!.GenerateConstrainedStringAsync(
                _rewriterSlot, prompt, grammar, maxTokens: 300, skipReset: false);

            if (string.IsNullOrWhiteSpace(json))
            {
                Console.Error.WriteLine("TextSanitizationPipeline: Rewriter returned empty response.");
                return text;
            }

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rewritten_text", out var prop))
            {
                var rewritten = prop.GetString();
                if (!string.IsNullOrWhiteSpace(rewritten))
                {
                    Console.WriteLine($"TextSanitizationPipeline: rewrite done ({rewritten.Length} chars).");
                    return rewritten;
                }
            }

            Console.Error.WriteLine("TextSanitizationPipeline: Could not parse rewriter response.");
            return text;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"TextSanitizationPipeline: Rewrite call failed. {ex.Message}");
            try { _llamaServer?.ResetInstance(_rewriterSlot); } catch { /* ignore */ }
            return text;
        }
    }

    private static string BuildRewritePrompt(
        string text, List<string> anachronisms, List<string> realWorldNames)
    {
        var sb = new StringBuilder();

        if (anachronisms.Count > 0)
            sb.AppendLine(
                $"Anachronistic or modern terms to replace with low-fantasy medieval equivalents: " +
                $"{string.Join(", ", anachronisms)}");

        if (realWorldNames.Count > 0)
            sb.AppendLine(
                $"Real-world proper names to replace with fitting invented in-world names: " +
                $"{string.Join(", ", realWorldNames)}");

        sb.AppendLine();
        sb.AppendLine("Rewrite the following text, replacing those terms accordingly.");
        sb.AppendLine("Keep the meaning and tone. Return only the corrected text:");
        sb.AppendLine();
        sb.Append(text);
        return sb.ToString().TrimEnd();
    }

    // ── System prompt ──────────────────────────────────────────────────────────

    private const string RewriterSystemPrompt =
        "You are a text corrector for a low-fantasy medieval narrative game. " +
        "When given a passage of text, you may be told about anachronistic or modern terms " +
        "(replace them with fitting medieval equivalents) and/or real-world proper names " +
        "(replace them with fitting invented in-world names). " +
        "You preserve the sentence structure and tone. " +
        "You return ONLY the corrected text — no explanation, no preamble.";
}
