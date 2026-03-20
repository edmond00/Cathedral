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
    [JsonPropertyName("narration_text")]
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
