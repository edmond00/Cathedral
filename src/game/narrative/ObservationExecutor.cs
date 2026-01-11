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
/// Executes observation skill LLM requests to generate environment perceptions.
/// Manages observation skill slots (0-9) with cached persona prompts.
/// Implements keyword fallback strategy.
/// </summary>
public class ObservationExecutor
{
    private readonly LlamaServerManager _llamaServer;
    private readonly ObservationPromptConstructor _promptConstructor;
    private readonly SkillSlotManager _slotManager;
    
    public ObservationExecutor(LlamaServerManager llamaServer, SkillSlotManager slotManager)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _promptConstructor = new ObservationPromptConstructor();
    }
    
    /// <summary>
    /// Generates an observation narration using an observation skill.
    /// Implements two-tier keyword fallback strategy.
    /// </summary>
    public async Task<ObservationResponse> GenerateObservationAsync(
        Skill observationSkill,
        NarrationNode node,
        Avatar avatar)
    {
        if (!observationSkill.Functions.Contains(SkillFunction.Observation))
            throw new ArgumentException($"Skill {observationSkill.DisplayName} is not an observation skill");
        
        if (string.IsNullOrEmpty(observationSkill.PersonaPrompt))
            throw new ArgumentException($"Observation skill {observationSkill.DisplayName} has no persona prompt");
        
        // Get or create slot for this skill
        int slotId = await GetOrCreateSlotForSkillAsync(observationSkill);
        
        // First attempt: Natural narration (prompted but not constrained)
        try
        {
            var response = await GenerateObservationNaturalAsync(slotId, node, avatar, observationSkill);
            
            // Extract keywords from response text to check quality
            var segments = new KeywordRenderer().ParseNarrationWithKeywords(
                response.NarrationText,
                node.OutcomeKeywords
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
        return await GenerateObservationWithFallbackAsync(slotId, node, avatar, observationSkill);
    }
    
    private async Task<ObservationResponse> GenerateObservationNaturalAsync(
        int slotId,
        NarrationNode node,
        Avatar avatar,
        Skill observationSkill)
    {
        var prompt = _promptConstructor.BuildObservationPrompt(node, avatar, observationSkill, promptKeywordUsage: true);
        var schema = CreateObservationSchema();
        var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
        
        var response = await RequestFromLLMAsync(slotId, prompt, gbnf);
        
        return ParseObservationResponse(response ?? "", node.OutcomeKeywords);
    }
    
    private async Task<ObservationResponse> GenerateObservationWithFallbackAsync(
        int slotId,
        NarrationNode node,
        Avatar avatar,
        Skill observationSkill)
    {
        var prompt = _promptConstructor.BuildObservationPromptWithIntros(node, avatar, observationSkill);
        var schema = CreateObservationSchemaWithIntros(node);
        var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
        
        var response = await RequestFromLLMAsync(slotId, prompt, gbnf);
        
        return ParseObservationResponse(response ?? "", node.OutcomeKeywords);
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
    
    private async Task<int> GetOrCreateSlotForSkillAsync(Skill skill)
    {
        return await _slotManager.GetOrCreateSlotForSkillAsync(skill);
    }
    
    private CompositeField CreateObservationSchema()
    {
        return new CompositeField("ObservationResponse",
            new StringField("narration_text", MinLength: 50, MaxLength: 600, Hint: "A short description of what the avatar observes in the environment")
        );
    }
    
    private CompositeField CreateObservationSchemaWithIntros(NarrationNode node)
    {
        // For fallback: Use TemplateStringField to FORCE starting with a keyword intro
        // This guarantees at least one keyword will be in the output
        var keywords = node.OutcomeKeywords.Take(3).ToList();
        
        if (keywords.Count == 0)
        {
            // Fallback to simple schema if no keywords
            return CreateObservationSchema();
        }
        
        // Build template with first keyword intro: "You notice {keyword}"
        // The <generated> placeholder tells the LLM where to continue generating
        var firstIntro = $"You notice {keywords.First()}";
        
        return new CompositeField("ObservationResponse",
            new TemplateStringField(
                "narration_text",
                Template: firstIntro + " <generated>",  // Fixed intro + placeholder for LLM generation
                MinGenLength: 50,      // LLM must generate at least 50 more chars after intro
                MaxGenLength: 550      // Up to 250 more chars
            )
        );
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
