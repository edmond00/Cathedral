using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Response from observation LLM request.
/// </summary>
public class ObservationResponse
{
    public string NarrationText { get; set; } = "";
    public List<string> HighlightedKeywords { get; set; } = new();
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
    private readonly Dictionary<string, int> _skillSlotMapping = new();
    private readonly HashSet<int> _activeSlots = new();
    private int _nextObservationSlot = 0; // Slots 0-9 for observation skills
    
    public ObservationExecutor(LlamaServerManager llamaServer)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
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
            var response = await GenerateObservationNaturalAsync(slotId, node, avatar);
            
            if (response.HighlightedKeywords.Count >= 3)
            {
                return response; // Success!
            }
            
            Console.WriteLine($"ObservationExecutor: Natural prompt returned {response.HighlightedKeywords.Count} keywords, trying fallback");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ObservationExecutor: Natural prompt failed: {ex.Message}, trying fallback");
        }
        
        // Fallback: Use keyword intro examples to force inclusion
        return await GenerateObservationWithFallbackAsync(slotId, node, avatar);
    }
    
    private async Task<ObservationResponse> GenerateObservationNaturalAsync(
        int slotId,
        NarrationNode node,
        Avatar avatar)
    {
        var prompt = _promptConstructor.BuildObservationPrompt(node, avatar, promptKeywordUsage: true);
        var schema = CreateObservationSchema(node.Keywords);
        var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
        
        var response = await RequestFromLLMAsync(slotId, prompt, gbnf);
        
        return ParseObservationResponse(response ?? "");
    }
    
    private async Task<ObservationResponse> GenerateObservationWithFallbackAsync(
        int slotId,
        NarrationNode node,
        Avatar avatar)
    {
        var prompt = _promptConstructor.BuildObservationPromptWithIntros(node, avatar);
        var schema = CreateObservationSchema(node.Keywords);
        var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
        
        var response = await RequestFromLLMAsync(slotId, prompt, gbnf);
        
        return ParseObservationResponse(response ?? "");
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
            
            return await tcs.Task;
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
        if (_skillSlotMapping.TryGetValue(skill.SkillId, out int existingSlot))
        {
            return existingSlot;
        }
        
        // Create new slot with cached persona prompt
        int slotId = _nextObservationSlot++;
        
        if (slotId >= 10)
        {
            throw new InvalidOperationException("Too many observation skills (max 10)");
        }
        
        await _llamaServer.CreateInstanceAsync(skill.PersonaPrompt!, slotId);
        _skillSlotMapping[skill.SkillId] = slotId;
        _activeSlots.Add(slotId);
        
        Console.WriteLine($"ObservationExecutor: Created slot {slotId} for {skill.DisplayName}");
        
        return slotId;
    }
    
    private CompositeField CreateObservationSchema(List<string> availableKeywords)
    {
        return new CompositeField("ObservationResponse",
            new StringField("narration_text", MinLength: 50, MaxLength: 300),
            new ArrayField("highlighted_keywords",
                new ChoiceField<string>("keyword", availableKeywords.ToArray()),
                MinLength: 0,  // Allow 0 for natural attempt
                MaxLength: 5
            )
        );
    }
    
    private ObservationResponse ParseObservationResponse(string jsonResponse)
    {
        try
        {
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var response = JsonSerializer.Deserialize<ObservationResponse>(jsonResponse, options);
            
            if (response == null)
            {
                throw new JsonException("Deserialized response is null");
            }
            
            return response;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"ObservationExecutor: Failed to parse JSON: {ex.Message}");
            Console.Error.WriteLine($"Raw response: {jsonResponse}");
            
            // Return empty response on parse failure
            return new ObservationResponse
            {
                NarrationText = "You observe the environment carefully.",
                HighlightedKeywords = new List<string>()
            };
        }
    }
}
