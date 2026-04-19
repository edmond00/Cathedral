using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Provides a fallback keyword when normal keyword extraction fails for an observation text part.
///
/// Workflow:
///   1. <see cref="NounExtractor"/> extracts candidate nouns from the observation text.
///   2. The critic LLM (GBNF-constrained to the noun list) picks the noun most
///      semantically related to the observation object's description.
///   3. The chosen noun is returned as the fallback keyword used for UI highlighting
///      and narrative routing.
///
/// The service manages its own LLM slot and initialises asynchronously.
/// All public methods degrade gracefully (returning null) when the server is
/// unavailable or no suitable noun is found.
/// </summary>
public sealed class KeywordFallbackService : IDisposable
{
    private readonly LlamaServerManager _llamaServer;
    private int _slotId = -1;
    private bool _isInitialized = false;
    private bool _disposed = false;

    /// <summary>True when the service has a valid LLM slot and the server is ready.</summary>
    public bool IsReady => _isInitialized && _llamaServer.IsServerReady && _slotId >= 0;

    public KeywordFallbackService(LlamaServerManager llamaServer)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
    }

    // ── Initialisation ─────────────────────────────────────────────────────────

    /// <summary>Creates the LLM slot. Must be called (and awaited) before use.</summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized || _disposed) return;

        try
        {
            Console.WriteLine("KeywordFallbackService: Initialising...");

            var modelPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "catalyst-models");
            await NounExtractor.InitializeAsync(modelPath);

            _slotId = await _llamaServer.CreateInstanceAsync(SystemPrompt);
            _isInitialized = true;
            Console.WriteLine($"KeywordFallbackService: Created slot {_slotId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"KeywordFallbackService: Failed to initialise: {ex.Message}");
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts nouns from <paramref name="observationText"/> via <see cref="NounExtractor"/>,
    /// then asks the critic LLM which noun best identifies <paramref name="observationDescription"/>.
    /// </summary>
    /// <param name="observationText">
    ///     The LLM-generated text for this observation part (transition + focus sentences, or
    ///     the focus-only sentence).  The returned keyword must be findable in this text.
    /// </param>
    /// <param name="observationDescription">
    ///     Natural-language label for the observation object (e.g. "musky fox den").
    ///     Used to ask the critic which noun is most related.
    /// </param>
    /// <returns>
    ///     A lowercase word present in <paramref name="observationText"/>, or null if
    ///     the service is unavailable or no suitable noun is found.
    /// </returns>
    public async Task<string?> FindBestKeywordAsync(string observationText, string observationDescription)
    {
        if (!IsReady || string.IsNullOrWhiteSpace(observationText))
            return null;

        var nouns = NounExtractor.ExtractNouns(observationText);
        if (nouns.Count == 0)
        {
            Console.WriteLine("KeywordFallbackService: No noun candidates extracted.");
            return null;
        }

        // Single candidate — skip the LLM call
        if (nouns.Count == 1)
        {
            Console.WriteLine($"KeywordFallbackService: Single noun candidate '{nouns[0]}', using directly.");
            return nouns[0];
        }

        Console.WriteLine($"KeywordFallbackService: Asking critic to pick from [{string.Join(", ", nouns)}] for '{observationDescription}'");

        try
        {
            var prompt  = BuildPrompt(observationText, observationDescription, nouns);
            var grammar = BuildGrammar(nouns);

            // skipReset=false — each fallback call is independent; the slot is reset afterwards.
            var result = await _llamaServer.GenerateConstrainedStringAsync(
                _slotId, prompt, grammar, maxTokens: 20, skipReset: false);

            var chosen = result.Trim().ToLowerInvariant();

            if (nouns.Any(n => n.Equals(chosen, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"KeywordFallbackService: Critic chose '{chosen}'");
                return chosen;
            }

            // LLM returned something unexpected despite grammar constraint — use first noun
            Console.WriteLine($"KeywordFallbackService: Unexpected critic response '{chosen}', falling back to '{nouns[0]}'");
            return nouns[0];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"KeywordFallbackService: LLM call failed: {ex.Message}");
            // Best-effort slot reset before returning
            try { _llamaServer.ResetInstance(_slotId); } catch { /* ignore */ }
            return nouns.Count > 0 ? nouns[0] : null;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string BuildPrompt(string text, string observationDescription, List<string> nouns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Text: \"{text}\"");
        sb.AppendLine();
        sb.AppendLine($"This text describes a scene observation of: {observationDescription}");
        sb.AppendLine();
        sb.AppendLine("From the words below, choose the ONE word that best identifies or represents this observation object in the text.");
        sb.AppendLine("Respond with ONLY that word, exactly as written:");
        sb.AppendLine(string.Join(", ", nouns));
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates a GBNF grammar that constrains the output to exactly one of the given nouns.
    /// Example output:
    ///   root ::= response
    ///   response ::= "leaf" | "stone" | "path"
    /// </summary>
    private static string BuildGrammar(List<string> nouns)
    {
        var options = string.Join(" | ", nouns.Select(n => $"\"{EscapeGbnf(n)}\""));
        return $"root ::= response\nresponse ::= {options}";
    }

    private static string EscapeGbnf(string word)
        => word.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private const string SystemPrompt =
        "You are a language critic. Given a short observation text and the name of an " +
        "observation object, select the single word from the provided list that best " +
        "identifies or represents that object in the text. " +
        "Respond with exactly one word from the list — nothing else.";

    // ── IDisposable ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
