using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Response from observation LLM request.
/// Keywords are extracted from narration text, not provided by LLM.
/// </summary>
public class ObservationResponse
{
    [JsonPropertyName("what_do_i_feel_and_observe")]
    public string NarrationText { get; set; } = "";
}

/// <summary>
/// Executes observation modusMentis LLM requests to generate environment perceptions.
/// Manages observation modusMentis slots (0-9) with cached persona prompts.
/// Implements keyword fallback strategy.
/// </summary>
public class ObservationExecutor
{
    private readonly LlamaServerManager _llamaServer;
    private readonly ObservationPromptConstructor _promptConstructor;
    private readonly ModusMentisSlotManager _slotManager;
    
    public ObservationExecutor(LlamaServerManager llamaServer, ModusMentisSlotManager slotManager)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _promptConstructor = new ObservationPromptConstructor();
    }
    
    /// <summary>
    /// Generates an observation narration using an observation modusMentis.
    /// Implements two-tier keyword fallback strategy.
    /// </summary>
    public async Task<ObservationResponse> GenerateObservationAsync(
        ModusMentis observationModusMentis,
        NarrationNode node,
        Protagonist protagonist)
    {
        // Use all node keywords by default
        return await GenerateObservationAsync(observationModusMentis, node, protagonist, node.OutcomeKeywords);
    }
    
    /// <summary>
    /// Generates an observation narration using an observation modusMentis with specific keywords.
    /// Used for focused observations that target a specific outcome's keywords.
    /// </summary>
    public async Task<ObservationResponse> GenerateObservationAsync(
        ModusMentis observationModusMentis,
        NarrationNode node,
        Protagonist protagonist,
        List<string> targetKeywords)
    {
        if (!observationModusMentis.Functions.Contains(ModusMentisFunction.Observation))
            throw new ArgumentException($"ModusMentis {observationModusMentis.DisplayName} is not an observation modusMentis");
        
        if (string.IsNullOrEmpty(observationModusMentis.PersonaPrompt))
            throw new ArgumentException($"Observation modusMentis {observationModusMentis.DisplayName} has no persona prompt");
        
        // Get or create slot for this modusMentis
        int slotId = await GetOrCreateSlotForModusMentisAsync(observationModusMentis);
        
        // First attempt: Natural narration (prompted but not constrained)
        try
        {
            var response = await GenerateObservationNaturalAsync(slotId, node, protagonist, observationModusMentis, targetKeywords);
            
            // Extract keywords from response text to check quality
            var segments = new KeywordRenderer().ParseNarrationWithKeywords(
                response.NarrationText,
                targetKeywords
            );
            int keywordCount = segments.Count(s => s.IsKeyword);
            
            if (keywordCount >= 1)
            {
                return response; // Success!
            }
            
            Console.WriteLine($"ObservationExecutor: Natural prompt returned {keywordCount} keywords, trying fallback");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ObservationExecutor: Natural prompt failed: {ex.Message}, trying fallback");
        }
        
        // Fallback: Use keyword intro examples to force inclusion
        return await GenerateObservationWithFallbackAsync(slotId, node, protagonist, observationModusMentis, targetKeywords);
    }
    
    private async Task<ObservationResponse> GenerateObservationNaturalAsync(
        int slotId,
        NarrationNode node,
        Protagonist protagonist,
        ModusMentis observationModusMentis,
        List<string> targetKeywords)
    {
        var prompt = _promptConstructor.BuildObservationPrompt(node, protagonist, observationModusMentis, targetKeywords, promptKeywordUsage: true);
        var schema = LLMSchemaConfig.CreateObservationSchema();
        var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
        
        var response = await RequestFromLLMAsync(slotId, prompt, gbnf);
        
        return ParseObservationResponse(response ?? "", targetKeywords);
    }
    
    private async Task<ObservationResponse> GenerateObservationWithFallbackAsync(
        int slotId,
        NarrationNode node,
        Protagonist protagonist,
        ModusMentis observationModusMentis,
        List<string> targetKeywords)
    {
        var prompt = _promptConstructor.BuildObservationPromptWithIntros(node, protagonist, observationModusMentis, targetKeywords);
        var schema = LLMSchemaConfig.CreateObservationSchemaWithIntros(targetKeywords);
        var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
        
        var response = await RequestFromLLMAsync(slotId, prompt, gbnf);
        
        return ParseObservationResponse(response ?? "", targetKeywords);
    }
    
    /// <summary>
    /// Makes a request to the LLM with timeout and event handling.
    /// </summary>
    private async Task<string?> RequestFromLLMAsync(
        int slotId,
        string userPrompt,
        string? gbnfGrammar,
        int timeoutSeconds = 60)
    {
        var tcs = new TaskCompletionSource<string>();
        
        void OnCompleted(int slot, string response, bool wasCancelled)
        {
            if (slot == slotId)
            {
                if (wasCancelled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(response);
                }
            }
        }
        
        // Register callback
        EventHandler<RequestCompletedEventArgs>? completedHandler = null;
        completedHandler = (sender, e) => OnCompleted(e.SlotId, e.FullResponse, e.WasCancelled);
        
        _llamaServer.RequestCompleted += completedHandler;
        
        try
        {
            // Start request
            _ = Task.Run(async () =>
            {
                try
                {
                    await _llamaServer.ContinueRequestAsync(
                        slotId,
                        userPrompt,
                        null, // onTokenStreamed
                        null, // onCompleted (handled by event)
                        gbnfGrammar);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"ObservationExecutor: Request error: {ex.Message}");
                    tcs.TrySetException(ex);
                }
            });
            
            // Wait with timeout
            var timeout = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completedTask = await Task.WhenAny(tcs.Task, timeout);
            
            if (completedTask == timeout)
            {
                Console.WriteLine($"ObservationExecutor: Request timed out after {timeoutSeconds}s");
                return null;
            }
            
            var result = await tcs.Task;
            Console.WriteLine($"ObservationExecutor: Request completed, response length: {result?.Length ?? 0}");
            
            // Small delay to ensure LlamaServerManager's finally block completes cleanup
            await Task.Delay(100);
            
            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationExecutor: Error in request: {ex.Message}");
            return null;
        }
        finally
        {
            // Cleanup event handler
            _llamaServer.RequestCompleted -= completedHandler;
        }
    }
    
    private async Task<int> GetOrCreateSlotForModusMentisAsync(ModusMentis modusMentis)
    {
        return await _slotManager.GetOrCreateSlotForModusMentisAsync(modusMentis);
    }

    /// <summary>
    /// Public accessor for slot acquisition — used by ObservationPhaseController
    /// when it manages the slot lifecycle directly (reset + sentence loop).
    /// </summary>
    public Task<int> GetOrCreateSlotForModusMentisPublicAsync(ModusMentis modusMentis)
        => GetOrCreateSlotForModusMentisAsync(modusMentis);

    /// <summary>
    /// Resets a modusMentis slot conversation history before starting a new observation batch.
    /// Preserves the cached persona prompt (system prompt).
    /// </summary>
    public void ResetSlot(int slotId)
    {
        _llamaServer.ResetInstance(slotId);
        Console.WriteLine($"ObservationExecutor: Reset slot {slotId} for new observation batch");
    }

    /// <summary>
    /// Generates one observation sentence focused on a specific outcome.
    /// Uses a first-sentence prompt with full context, or a continuation prompt if not the first.
    /// Returns the raw narration text string.
    /// </summary>
    public async Task<string> GenerateSentenceAsync(
        int slotId,
        NarrationNode node,
        int locationId,
        ConcreteOutcome outcome,
        bool isFirstSentence,
        string personaTone,
        CancellationToken ct = default)
    {
        var prompt = isFirstSentence
            ? _promptConstructor.BuildFirstSentencePrompt(node, locationId, outcome, personaTone)
            : _promptConstructor.BuildContinuationSentencePrompt(outcome);

        var schema = isFirstSentence
            ? LLMSchemaConfig.CreateObservationSchema()
            : LLMSchemaConfig.CreateContinuationObservationSchema();
        var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        var jsonResponse = await RequestFromLLMAsync(slotId, prompt, gbnf);
        var parsed = ParseObservationResponse(jsonResponse ?? "", new List<string>());
        return parsed.NarrationText;
    }

    /// <summary>
    /// Generates one observation sentence using a pre-built prompt string.
    /// Used by the refactored observation phase which composes prompts externally.
    /// </summary>
    public async Task<string> GenerateSentenceFromPromptAsync(
        int slotId,
        string prompt,
        bool isFirstInBatch = false,
        CancellationToken ct = default)
    {
        var schema = isFirstInBatch
            ? LLMSchemaConfig.CreateObservationSchema()
            : LLMSchemaConfig.CreateContinuationObservationSchema();
        var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
        var jsonResponse = await RequestFromLLMAsync(slotId, prompt, gbnf);
        var parsed = ParseObservationResponse(jsonResponse ?? "", new List<string>());
        return parsed.NarrationText;
    }

    /// <summary>
    /// Extracts the single best keyword from a sentence, given a list of candidate keywords.
    /// Returns the first candidate keyword found in the sentence (earliest position).
    /// Falls back to the longest word in the sentence if no candidate keyword is found.
    /// </summary>
    public string ExtractKeywordFromSentence(string sentence, List<string> outcomeKeywords)
    {
        if (outcomeKeywords.Count > 0)
        {
            var segments = new KeywordRenderer().ParseNarrationWithKeywords(sentence, outcomeKeywords);
            var firstFound = segments.FirstOrDefault(s => s.IsKeyword);
            if (firstFound != null && !string.IsNullOrEmpty(firstFound.KeywordValue))
                return firstFound.KeywordValue;
        }

        // Fallback: longest word in sentence
        var words = sentence.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
        return words.OrderByDescending(w => w.Length).FirstOrDefault() ?? sentence;
    }

    /// <summary>
    /// Scans the combined text of a transition + focus sentence for matching outcome keywords,
    /// randomly samples up to <paramref name="maxCount"/> of them, and returns the list.
    /// Falls back to the longest word of the focus sentence if no keyword is found.
    /// </summary>
    public List<string> ExtractKeywordsFromSentences(
        string transitionText,
        string focusText,
        List<string> outcomeKeywords,
        Random rng,
        int maxCount = 3)
    {
        var combined = (transitionText + " " + focusText).Trim();
        var foundKeywords = new List<string>();

        if (outcomeKeywords.Count > 0)
        {
            var renderer = new KeywordRenderer();
            var segments = renderer.ParseNarrationWithKeywords(combined, outcomeKeywords);
            var distinct = segments
                .Where(s => s.IsKeyword && !string.IsNullOrEmpty(s.KeywordValue))
                .Select(s => s.KeywordValue!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Randomly sample up to maxCount
            if (distinct.Count > maxCount)
                distinct = distinct.OrderBy(_ => rng.Next()).Take(maxCount).ToList();

            foundKeywords.AddRange(distinct);
        }

        if (foundKeywords.Count == 0)
        {
            // Fallback: longest word of focus sentence
            var words = focusText.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
            var longest = words.OrderByDescending(w => w.Length).FirstOrDefault();
            if (!string.IsNullOrEmpty(longest))
                foundKeywords.Add(longest);
        }

        return foundKeywords;
    }

    /// <summary>
    /// Assigns each keyword to either the transition or focus sentence by checking
    /// which sentence text contains the keyword. Keywords only in focus go to focus;
    /// ones in both or only in transition go to transition. Longest-word fallbacks go to focus.
    /// </summary>
    public (List<string> TransitionKeywords, List<string> FocusKeywords) AssignKeywordsToSentences(
        List<string> keywords,
        string transitionText,
        string focusText)
    {
        var transKws = new List<string>();
        var focKws = new List<string>();
        var renderer = new KeywordRenderer();

        foreach (var kw in keywords)
        {
            var kwList = new List<string> { kw };
            bool inTransition = transitionText.Length > 0 &&
                renderer.ParseNarrationWithKeywords(transitionText, kwList).Any(s => s.IsKeyword);
            bool inFocus = renderer.ParseNarrationWithKeywords(focusText, kwList).Any(s => s.IsKeyword);

            if (inTransition)
                transKws.Add(kw);
            else
                focKws.Add(kw); // inFocus only, or fallback longest-word
        }

        return (transKws, focKws);
    }


    private ObservationResponse ParseObservationResponse(string jsonResponse, List<string> availableKeywords)
    {
        try
        {
            Console.WriteLine($"ObservationExecutor: Parsing JSON response ({jsonResponse.Length} chars)");
            Console.WriteLine($"ObservationExecutor: Raw JSON: {jsonResponse}");
            
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var response = JsonSerializer.Deserialize<ObservationResponse>(jsonResponse, options);
            
            if (response == null || string.IsNullOrWhiteSpace(response.NarrationText))
            {
                throw new JsonException("Deserialized response is null or empty");
            }
            
            Console.WriteLine($"ObservationExecutor: Successfully parsed narration ({response.NarrationText.Length} chars)");
            
            return response;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"ObservationExecutor: Failed to parse JSON: {ex.Message}");
            Console.Error.WriteLine($"Raw response: {jsonResponse}");
            
            // Return empty response on parse failure
            return new ObservationResponse
            {
                NarrationText = "You observe the environment carefully."
            };
        }
    }
}
