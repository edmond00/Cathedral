using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cathedral.Glyph.Microworld.LocationSystem;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game;

/// <summary>
/// LLM-based action executor for Phase 5.
/// Uses Director LLM to generate action outcomes and Narrator LLM for descriptions.
/// Falls back to SimpleActionExecutor on errors.
/// </summary>
public class LLMActionExecutor : IDisposable
{
    private readonly LlamaServerManager _llamaServer;
    private readonly SimpleActionExecutor _fallbackExecutor;
    private int _directorSlotId = -1;
    private int _narratorSlotId = -1;
    private bool _useLLM = true;
    private bool _isInitialized = false;
    
    // Statistics for session logging
    private int _totalRequests = 0;
    private int _successfulRequests = 0;
    private int _failedRequests = 0;
    private double _totalDurationMs = 0;

    public LLMActionExecutor(LlamaServerManager llamaServer, SimpleActionExecutor fallbackExecutor)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
        _fallbackExecutor = fallbackExecutor ?? throw new ArgumentNullException(nameof(fallbackExecutor));
    }

    /// <summary>
    /// Initializes the LLM slots. Must be called before using the executor.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            Console.WriteLine("LLMActionExecutor: Initializing Director and Narrator slots...");
            
            // Create dedicated slots for Director and Narrator
            _directorSlotId = await _llamaServer.CreateInstanceAsync("You are a game director.");
            LLMLogger.LogInstanceCreated(_directorSlotId, "Director", true);
            
            _narratorSlotId = await _llamaServer.CreateInstanceAsync("You are a game narrator.");
            LLMLogger.LogInstanceCreated(_narratorSlotId, "Narrator", true);
            
            _isInitialized = true;
            Console.WriteLine($"LLMActionExecutor: Created Director slot {_directorSlotId}, Narrator slot {_narratorSlotId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMActionExecutor: Failed to initialize: {ex.Message}");
            LLMLogger.LogInstanceCreated(-1, "LLMActionExecutor", false, ex.Message);
            _isInitialized = false;
        }
    }

    /// <summary>
    /// Executes an action using LLM-generated outcomes.
    /// Falls back to SimpleActionExecutor on errors.
    /// </summary>
    public async Task<ActionResult> ExecuteActionAsync(
        string actionText,
        LocationInstanceState currentState,
        LocationBlueprint blueprint,
        PlayerAction? previousAction = null)
    {
        // Special handling for exit actions (no LLM needed)
        if (IsExitAction(actionText))
        {
            return ActionResult.CreateExit("You leave the area and return to exploring the world.");
        }

        // If LLM is disabled, not initialized, or server not ready, use fallback
        if (!_useLLM || !_isInitialized || !_llamaServer.IsServerReady)
        {
            var reason = !_useLLM ? "LLM disabled" : 
                        !_isInitialized ? "LLM not initialized" : 
                        "Server not ready";
            Console.WriteLine($"LLMActionExecutor: Using fallback executor ({reason})");
            LLMLogger.LogFallback($"Fallback used: {reason}");
            return _fallbackExecutor.ExecuteAction(actionText, currentState, blueprint);
        }
        
        // Additional safety check - verify slots are valid
        if (_directorSlotId < 0 || _narratorSlotId < 0)
        {
            Console.Error.WriteLine("LLMActionExecutor: Invalid slot IDs - using fallback");
            LLMLogger.LogFallback($"Invalid slot IDs: Director={_directorSlotId}, Narrator={_narratorSlotId}");
            return _fallbackExecutor.ExecuteAction(actionText, currentState, blueprint);
        }

        try
        {
            // Generate outcome using Director LLM
            var outcome = await GenerateOutcomeAsync(actionText, currentState, blueprint, previousAction);
            
            if (outcome == null)
            {
                Console.Error.WriteLine("LLMActionExecutor: Director LLM failed to generate outcome");
                LLMLogger.LogFallback("Director LLM returned null outcome");
                
                // Return error result instead of silent fallback
                return ActionResult.CreateFailure(
                    "ERROR: Failed to process your action.\n\n" +
                    "The LLM did not return a valid outcome. This could be due to:\n" +
                    "- LLM server timeout\n" +
                    "- Invalid response format\n" +
                    "- Slot conflict (already processing)\n\n" +
                    "Check logs/llm_communication_*.log for details.");
            }

            // Convert LLM outcome to ActionResult
            return ConvertToActionResult(outcome, actionText);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMActionExecutor: Error in LLM execution: {ex.Message}");
            LLMLogger.LogFallback($"Exception: {ex.Message}");
            
            // Return error result instead of silent fallback
            return ActionResult.CreateFailure(
                $"ERROR: Exception during action processing.\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"Check logs/llm_communication_*.log for details.");
        }
    }

    /// <summary>
    /// Generates action choices using Director LLM.
    /// Returns null on failure (caller should use fallback).
    /// </summary>
    public async Task<List<ActionInfo>?> GenerateActionsAsync(
        LocationInstanceState currentState,
        LocationBlueprint blueprint,
        PlayerAction? previousAction = null)
    {
        if (!_useLLM || !_isInitialized || !_llamaServer.IsServerReady || _directorSlotId < 0)
        {
            var reason = !_useLLM ? "disabled" : !_isInitialized ? "not initialized" : 
                        !_llamaServer.IsServerReady ? "server not ready" : "invalid director slot";
            Console.Error.WriteLine($"GenerateActionsAsync: Cannot generate actions - LLM {reason}");
            return null;
        }

        try
        {
            var director = new DirectorPromptConstructor(
                blueprint,
                currentState.CurrentSublocation,
                currentState.CurrentStates,
                numberOfActions: 6);

            // Update director with current state
            director.UpdateGameState(currentState.CurrentSublocation, currentState.CurrentStates);

            // Get system prompt and user prompt
            var systemPrompt = director.GetSystemPrompt();
            var userPrompt = director.ConstructPrompt(previousAction, null);
            var gbnf = director.GetGbnf();

            Console.WriteLine($"LLMActionExecutor: Requesting actions from Director LLM...");

            // Request from LLM with retry on empty response
            string? response = null;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                response = await RequestFromLLMAsync(
                    _directorSlotId,
                    systemPrompt,
                    userPrompt,
                    gbnf,
                    timeoutSeconds: 30);

                if (!string.IsNullOrWhiteSpace(response))
                    break;
                    
                if (attempt == 0)
                {
                    Console.WriteLine("LLMActionExecutor: Director returned empty response, retrying after delay...");
                    await Task.Delay(200); // Wait before retry
                }
            }

            if (string.IsNullOrWhiteSpace(response))
            {
                Console.Error.WriteLine("LLMActionExecutor: Director returned empty response after retry");
                return null;
            }

            // Parse JSON response
            var actions = ParseActionsFromJson(response);
            
            if (actions == null || actions.Count == 0)
            {
                Console.WriteLine("LLMActionExecutor: Failed to parse actions from Director response");
                return null;
            }

            Console.WriteLine($"LLMActionExecutor: Generated {actions.Count} actions from Director LLM");
            return actions;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMActionExecutor: Error generating actions: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates narrative description using Narrator LLM.
    /// Returns null on failure (caller should use fallback).
    /// </summary>
    public async Task<string?> GenerateNarrativeAsync(
        LocationInstanceState currentState,
        LocationBlueprint blueprint,
        PlayerAction? previousAction = null,
        List<ActionInfo>? availableActions = null)
    {
        if (!_useLLM || !_isInitialized || !_llamaServer.IsServerReady || _narratorSlotId < 0)
        {
            var reason = !_useLLM ? "disabled" : !_isInitialized ? "not initialized" : 
                        !_llamaServer.IsServerReady ? "server not ready" : "invalid narrator slot";
            Console.Error.WriteLine($"GenerateNarrativeAsync: Cannot generate narrative - LLM {reason}");
            return null;
        }

        try
        {
            var narrator = new NarratorPromptConstructor(
                blueprint,
                currentState.CurrentSublocation,
                currentState.CurrentStates);

            // Update narrator with current state
            narrator.UpdateGameState(currentState.CurrentSublocation, currentState.CurrentStates);

            // Get system prompt and user prompt
            var systemPrompt = narrator.GetSystemPrompt();
            var userPrompt = narrator.ConstructPrompt(previousAction, availableActions);
            var gbnf = narrator.GetGbnf(previousAction?.WasSuccessful);

            Console.WriteLine($"LLMActionExecutor: Requesting narrative from Narrator LLM...");

            // Request from LLM with retry on empty response
            string? response = null;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                response = await RequestFromLLMAsync(
                    _narratorSlotId,
                    systemPrompt,
                    userPrompt,
                    gbnf,
                    timeoutSeconds: 20);

                if (!string.IsNullOrWhiteSpace(response))
                    break;
                    
                if (attempt == 0)
                {
                    Console.WriteLine("LLMActionExecutor: Narrator returned empty response, retrying after delay...");
                    await Task.Delay(200); // Wait before retry
                }
            }

            if (string.IsNullOrWhiteSpace(response))
            {
                Console.Error.WriteLine("LLMActionExecutor: Narrator returned empty response after retry");
                return null;
            }

            Console.WriteLine($"LLMActionExecutor: Generated narrative from Narrator LLM");
            return response.Trim();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMActionExecutor: Error generating narrative: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates action outcome using Director LLM.
    /// </summary>
    private async Task<ActionOutcome?> GenerateOutcomeAsync(
        string actionText,
        LocationInstanceState currentState,
        LocationBlueprint blueprint,
        PlayerAction? previousAction)
    {
        var systemPrompt = @"You are the DIRECTOR of a fantasy RPG game. Generate the outcome of a player action as JSON.

Your response MUST be valid JSON with this structure:
{
    ""success"": true or false,
    ""narrative"": ""description of what happened"",
    ""state_changes"": {""category"": ""new_state""},
    ""new_sublocation"": ""optional new location or null"",
    ""items_gained"": [""optional items""],
    ""ends_interaction"": true or false
}

Rules:
- 70% of actions should succeed
- 15% should critically fail (ends_interaction: true)
- 15% should have neutral/minor outcomes
- Narrative should be 2-3 sentences
- State changes are optional
- Critical failures must end the interaction";

        var userPrompt = $@"CURRENT STATE:
Location: {currentState.CurrentSublocation}
Turn: {currentState.CurrentTurnCount}

ACTION TAKEN:
{actionText}

Generate the JSON outcome for this action.";

        // Generate GBNF grammar using JsonConstraintGenerator infrastructure
        var outcomeConstraints = GenerateOutcomeConstraints(blueprint, currentState.CurrentSublocation, currentState.CurrentStates);
        var gbnfGrammar = JsonConstraintGenerator.GenerateGBNF(outcomeConstraints);

        Console.WriteLine($"LLMActionExecutor: Requesting outcome from Director LLM for action: {actionText}");

        var response = await RequestFromLLMAsync(
            _directorSlotId,
            systemPrompt,
            userPrompt,
            gbnfGrammar,
            timeoutSeconds: 25);

        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        // Parse outcome
        return ParseOutcomeFromJson(response);
    }

    /// <summary>
    /// Generates JSON constraints for action outcome format using JsonConstraintGenerator infrastructure.
    /// </summary>
    private JsonField GenerateOutcomeConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        // Get accessible sublocations for movement
        var accessibleSublocations = new List<string> { "none" };
        if (blueprint.SublocationConnections.ContainsKey(currentSublocation))
        {
            accessibleSublocations.AddRange(blueprint.SublocationConnections[currentSublocation]);
        }

        // Get available items from content
        var availableItems = new List<string> { "none" };
        if (blueprint.ContentMap.ContainsKey(currentSublocation))
        {
            var content = blueprint.ContentMap[currentSublocation];
            if (content.ContainsKey("default"))
            {
                availableItems.AddRange(content["default"].AvailableItems);
            }
            // Add items from state-specific content
            foreach (var state in currentStates.Values)
            {
                if (content.ContainsKey(state))
                {
                    availableItems.AddRange(content[state].AvailableItems);
                }
            }
        }

        // State changes can be empty or specify a category/state change
        var stateChangeOptions = new List<CompositeField>
        {
            new CompositeField("no_change", 
                new ConstantStringField("category", "none"),
                new ConstantStringField("new_state", "none"))
        };

        // Add possible state changes for each category
        foreach (var (categoryId, category) in blueprint.StateCategories)
        {
            var possibleStates = category.PossibleStates.Keys.ToArray();
            if (possibleStates.Length > 0)
            {
                stateChangeOptions.Add(new CompositeField($"change_{categoryId}",
                    new ConstantStringField("category", categoryId),
                    new ChoiceField<string>("new_state", possibleStates)));
            }
        }

        // Build the outcome structure
        // Note: new_sublocation can be null or a string value
        // Use ChoiceField to allow "none" as the value (we'll handle null parsing)
        return new CompositeField("ActionOutcome",
            new BooleanField("success"),
            new StringField("narrative", 20, 300),
            new VariantField("state_changes", stateChangeOptions.ToArray()),
            new ChoiceField<string>("new_sublocation", accessibleSublocations.Concat(new[] { "none" }).ToArray()),
            new ArrayField("items_gained", new ChoiceField<string>("item", availableItems.Distinct().ToArray()), 0, 3),
            new BooleanField("ends_interaction")
        );
    }

    /// <summary>
    /// Makes a request to the LLM with timeout and logging.
    /// </summary>
    private async Task<string?> RequestFromLLMAsync(
        int slotId,
        string systemPrompt,
        string userPrompt,
        string? gbnfGrammar,
        int timeoutSeconds)
    {
        var startTime = DateTime.Now;
        var roleName = slotId == _directorSlotId ? "Director" : "Narrator";
        
        // Log request
        LLMLogger.LogRequest(roleName, slotId, systemPrompt, userPrompt, gbnfGrammar);
        _totalRequests++;
        
        var tcs = new TaskCompletionSource<string>();
        var responseBuilder = new System.Text.StringBuilder();

        // Set up callbacks with proper event handler references for cleanup
        EventHandler<TokenStreamedEventArgs>? tokenHandler = null;
        EventHandler<RequestCompletedEventArgs>? completedHandler = null;

        void OnToken(string token, int slot)
        {
            if (slot == slotId)
            {
                responseBuilder.Append(token);
            }
        }

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

        // Register callbacks with stored references for cleanup
        tokenHandler = (sender, e) => OnToken(e.Token, e.SlotId);
        completedHandler = (sender, e) => OnCompleted(e.SlotId, e.FullResponse, e.WasCancelled);
        
        _llamaServer.TokenStreamed += tokenHandler;
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
                        null, // onTokenStreamed handled by event
                        null, // onCompleted handled by event
                        gbnfGrammar);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"LLMActionExecutor: Request error: {ex.Message}");
                    tcs.TrySetException(ex);
                }
            });

            // Wait with timeout
            var timeout = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completedTask = await Task.WhenAny(tcs.Task, timeout);

            if (completedTask == timeout)
            {
                var duration = (DateTime.Now - startTime).TotalMilliseconds;
                Console.WriteLine($"LLMActionExecutor: Request timed out after {timeoutSeconds}s");
                LLMLogger.LogResponse(roleName, slotId, $"Timeout after {timeoutSeconds}s", false, duration);
                _failedRequests++;
                _totalDurationMs += duration;
                return null;
            }

            var response = await tcs.Task;
            var durationMs = (DateTime.Now - startTime).TotalMilliseconds;
            
            // Log response
            LLMLogger.LogResponse(roleName, slotId, response, true, durationMs);
            _successfulRequests++;
            _totalDurationMs += durationMs;
            
            return response;
        }
        catch (Exception ex)
        {
            var duration = (DateTime.Now - startTime).TotalMilliseconds;
            Console.Error.WriteLine($"LLMActionExecutor: Error in RequestFromLLMAsync: {ex.Message}");
            LLMLogger.LogResponse(roleName, slotId, $"Exception: {ex.Message}", false, duration);
            _failedRequests++;
            _totalDurationMs += duration;
            return null;
        }
        finally
        {
            // CRITICAL: Unregister event handlers to prevent memory leaks and race conditions
            if (tokenHandler != null)
                _llamaServer.TokenStreamed -= tokenHandler;
            if (completedHandler != null)
                _llamaServer.RequestCompleted -= completedHandler;
        }
    }

    /// <summary>
    /// Parses actions from Director JSON response.
    /// </summary>
    private List<ActionInfo>? ParseActionsFromJson(string json)
    {
        try
        {
            // Check for empty response first
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.Error.WriteLine("LLMActionExecutor: Cannot parse empty JSON response");
                LLMLogger.LogParseError("Director", "", "Empty response from LLM");
                return null;
            }
            
            // Try to extract JSON if there's extra text
            var jsonStart = json.IndexOf('{');
            var jsonEnd = json.LastIndexOf('}');
            
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                Console.Error.WriteLine($"LLMActionExecutor: No valid JSON found in response (length: {json.Length})");
                LLMLogger.LogParseError("Director", json, "No valid JSON structure found");
                return null;
            }
            
            json = json.Substring(jsonStart, jsonEnd - jsonStart + 1);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Look for "actions" array
            if (root.TryGetProperty("actions", out var actionsElement))
            {
                var actions = new List<ActionInfo>();
                int actionIndex = 0;
                foreach (var action in actionsElement.EnumerateArray())
                {
                    actionIndex++;
                    if (action.TryGetProperty("action_text", out var textElement))
                    {
                        var text = textElement.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            // Extract related_skill (now always uses this name in JSON)
                            string relatedSkill = "";
                            if (action.TryGetProperty("related_skill", out var skillElement))
                            {
                                relatedSkill = skillElement.GetString() ?? "";
                            }
                            
                            actions.Add(new ActionInfo(text, relatedSkill));
                        }
                    }
                }
                return actions.Count > 0 ? actions : null;
            }
            
            LLMLogger.LogParseError("Director", json, "Missing 'actions' property in response");
            return null;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"LLMActionExecutor: JSON parse error: {ex.Message}");
            LLMLogger.LogParseError("Director", json, $"JSON parse error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parses action outcome from Director JSON response.
    /// </summary>
    private ActionOutcome? ParseOutcomeFromJson(string json)
    {
        try
        {
            // Check for empty response first
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.Error.WriteLine("LLMActionExecutor: Cannot parse empty JSON response");
                LLMLogger.LogParseError("Director", "", "Empty response from LLM");
                return null;
            }
            
            // Try to extract JSON if there's extra text
            var jsonStart = json.IndexOf('{');
            var jsonEnd = json.LastIndexOf('}');
            
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                Console.Error.WriteLine($"LLMActionExecutor: No valid JSON found in outcome response (length: {json.Length})");
                LLMLogger.LogParseError("Director", json, "No valid JSON structure found in outcome");
                return null;
            }
            
            json = json.Substring(jsonStart, jsonEnd - jsonStart + 1);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var outcome = new ActionOutcome
            {
                Success = root.TryGetProperty("success", out var successElement) && successElement.GetBoolean(),
                Narrative = root.TryGetProperty("narrative", out var narrativeElement) ? narrativeElement.GetString() ?? "" : "",
                StateChanges = new Dictionary<string, string>(),
                ItemsGained = new List<string>(),
                EndsInteraction = root.TryGetProperty("ends_interaction", out var endsElement) && endsElement.GetBoolean()
            };

            // Parse state changes
            if (root.TryGetProperty("state_changes", out var stateChangesElement))
            {
                foreach (var property in stateChangesElement.EnumerateObject())
                {
                    outcome.StateChanges[property.Name] = property.Value.GetString() ?? "";
                }
            }

            // Parse new sublocation (treat "none" as null)
            if (root.TryGetProperty("new_sublocation", out var sublocationElement))
            {
                var sublocationValue = sublocationElement.GetString();
                outcome.NewSublocation = sublocationValue == "none" ? null : sublocationValue;
            }

            // Parse items gained
            if (root.TryGetProperty("items_gained", out var itemsElement))
            {
                foreach (var item in itemsElement.EnumerateArray())
                {
                    var itemName = item.GetString();
                    if (!string.IsNullOrWhiteSpace(itemName))
                    {
                        outcome.ItemsGained.Add(itemName);
                    }
                }
            }

            return outcome;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"LLMActionExecutor: JSON parse error for outcome: {ex.Message}");
            LLMLogger.LogParseError("Director", json, $"Outcome parse error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts ActionOutcome to ActionResult.
    /// </summary>
    private ActionResult ConvertToActionResult(ActionOutcome outcome, string actionText)
    {
        if (outcome.Success)
        {
            return ActionResult.CreateSuccess(
                outcome.Narrative,
                outcome.StateChanges.Count > 0 ? outcome.StateChanges : null,
                outcome.NewSublocation,
                newActions: null, // Will be regenerated
                itemsGained: outcome.ItemsGained.Count > 0 ? outcome.ItemsGained : null,
                endsInteraction: outcome.EndsInteraction);
        }
        else
        {
            return ActionResult.CreateFailure(outcome.Narrative);
        }
    }

    /// <summary>
    /// Checks if an action is an exit action.
    /// </summary>
    private bool IsExitAction(string actionText)
    {
        var lowerAction = actionText.ToLowerInvariant();
        return lowerAction.Contains("return") ||
               lowerAction.Contains("leave") ||
               lowerAction.Contains("exit") ||
               lowerAction.Contains("go back");
    }

    /// <summary>
    /// Enables or disables LLM usage (for testing/debugging).
    /// </summary>
    public void SetLLMEnabled(bool enabled)
    {
        _useLLM = enabled;
        Console.WriteLine($"LLMActionExecutor: LLM usage {(enabled ? "enabled" : "disabled")}");
    }
    
    /// <summary>
    /// Resets the conversation history for both Director and Narrator instances.
    /// Should be called when entering a new location to prevent pattern repetition.
    /// </summary>
    public void ResetForNewLocation()
    {
        if (!_isInitialized || !_llamaServer.IsServerReady)
        {
            Console.WriteLine("LLMActionExecutor: Cannot reset - not initialized or server not ready");
            return;
        }
        
        try
        {
            // Reset both Director and Narrator conversation histories
            if (_directorSlotId >= 0)
            {
                _llamaServer.ResetInstance(_directorSlotId);
                Console.WriteLine($"LLMActionExecutor: Reset Director slot {_directorSlotId} for new location");
                LLMLogger.LogSlotIssue(_directorSlotId, "Reset", "Conversation history cleared for new location");
            }
            
            if (_narratorSlotId >= 0)
            {
                _llamaServer.ResetInstance(_narratorSlotId);
                Console.WriteLine($"LLMActionExecutor: Reset Narrator slot {_narratorSlotId} for new location");
                LLMLogger.LogSlotIssue(_narratorSlotId, "Reset", "Conversation history cleared for new location");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMActionExecutor: Error resetting instances: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs session statistics when the executor is disposed.
    /// </summary>
    public void Dispose()
    {
        if (_totalRequests > 0)
        {
            var avgDuration = _totalDurationMs / _totalRequests;
            LLMLogger.LogStatistics(_totalRequests, _successfulRequests, _failedRequests, avgDuration);
        }
    }
}

/// <summary>
/// Intermediate structure for parsing LLM outcomes.
/// </summary>
internal class ActionOutcome
{
    public bool Success { get; set; }
    public string Narrative { get; set; } = "";
    public Dictionary<string, string> StateChanges { get; set; } = new();
    public string? NewSublocation { get; set; }
    public List<string> ItemsGained { get; set; } = new();
    public bool EndsInteraction { get; set; }
}
