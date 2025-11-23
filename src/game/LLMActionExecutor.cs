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
            // Create dedicated slots for Director and Narrator
            _directorSlotId = await _llamaServer.CreateInstanceAsync("You are a game director.");
            _narratorSlotId = await _llamaServer.CreateInstanceAsync("You are a game narrator.");
            
            _isInitialized = true;
            Console.WriteLine($"LLMActionExecutor: Created Director slot {_directorSlotId}, Narrator slot {_narratorSlotId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMActionExecutor: Failed to initialize: {ex.Message}");
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
            Console.WriteLine("LLMActionExecutor: Using fallback executor (LLM disabled, not initialized, or server not ready)");
            LLMLogger.LogFallback("LLM disabled, not initialized, or server not ready");
            return _fallbackExecutor.ExecuteAction(actionText, currentState, blueprint);
        }

        try
        {
            // Generate outcome using Director LLM
            var outcome = await GenerateOutcomeAsync(actionText, currentState, blueprint, previousAction);
            
            if (outcome == null)
            {
                Console.WriteLine("LLMActionExecutor: Director LLM failed, using fallback");
                LLMLogger.LogFallback("Director LLM returned null outcome");
                return _fallbackExecutor.ExecuteAction(actionText, currentState, blueprint);
            }

            // Convert LLM outcome to ActionResult
            return ConvertToActionResult(outcome, actionText);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMActionExecutor: Error in LLM execution: {ex.Message}");
            Console.WriteLine("LLMActionExecutor: Falling back to SimpleActionExecutor");
            LLMLogger.LogFallback($"Exception: {ex.Message}");
            return _fallbackExecutor.ExecuteAction(actionText, currentState, blueprint);
        }
    }

    /// <summary>
    /// Generates action choices using Director LLM.
    /// Returns null on failure (caller should use fallback).
    /// </summary>
    public async Task<List<string>?> GenerateActionsAsync(
        LocationInstanceState currentState,
        LocationBlueprint blueprint,
        PlayerAction? previousAction = null)
    {
        if (!_useLLM || !_isInitialized || !_llamaServer.IsServerReady)
        {
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

            // Request from LLM
            var response = await RequestFromLLMAsync(
                _directorSlotId,
                systemPrompt,
                userPrompt,
                gbnf,
                timeoutSeconds: 30);

            if (string.IsNullOrWhiteSpace(response))
            {
                Console.WriteLine("LLMActionExecutor: Director returned empty response");
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
        List<string>? availableActions = null)
    {
        if (!_useLLM || !_isInitialized || !_llamaServer.IsServerReady)
        {
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

            // Request from LLM
            var response = await RequestFromLLMAsync(
                _narratorSlotId,
                systemPrompt,
                userPrompt,
                gbnf,
                timeoutSeconds: 20);

            if (string.IsNullOrWhiteSpace(response))
            {
                Console.WriteLine("LLMActionExecutor: Narrator returned empty response");
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
        // Build prompt for outcome generation
        var director = new DirectorPromptConstructor(
            blueprint,
            currentState.CurrentSublocation,
            currentState.CurrentStates,
            numberOfActions: 1); // We only need outcome, not new actions

        var systemPrompt = @"You are the DIRECTOR of a fantasy RPG game. Generate the outcome of a player action as JSON.

Your response MUST be valid JSON with this structure:
{
    ""success"": true or false,
    ""narrative"": ""description of what happened"",
    ""state_changes"": {""category"": ""new_state""},
    ""new_sublocation"": ""optional new location"",
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

        Console.WriteLine($"LLMActionExecutor: Requesting outcome from Director LLM for action: {actionText}");

        var response = await RequestFromLLMAsync(
            _directorSlotId,
            systemPrompt,
            userPrompt,
            null, // No GBNF for now - would need to define outcome schema
            timeoutSeconds: 25);

        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        // Parse outcome
        return ParseOutcomeFromJson(response);
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

        // Set up callbacks
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

        // Register callbacks
        _llamaServer.TokenStreamed += (sender, e) => OnToken(e.Token, e.SlotId);
        _llamaServer.RequestCompleted += (sender, e) => OnCompleted(e.SlotId, e.FullResponse, e.WasCancelled);

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
    }

    /// <summary>
    /// Parses actions from Director JSON response.
    /// </summary>
    private List<string>? ParseActionsFromJson(string json)
    {
        try
        {
            // Try to extract JSON if there's extra text
            var jsonStart = json.IndexOf('{');
            var jsonEnd = json.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                json = json.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Look for "actions" array
            if (root.TryGetProperty("actions", out var actionsElement))
            {
                var actions = new List<string>();
                foreach (var action in actionsElement.EnumerateArray())
                {
                    if (action.TryGetProperty("action_text", out var textElement))
                    {
                        var text = textElement.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            actions.Add(text);
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
            // Try to extract JSON if there's extra text
            var jsonStart = json.IndexOf('{');
            var jsonEnd = json.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                json = json.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

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

            // Parse new sublocation
            if (root.TryGetProperty("new_sublocation", out var sublocationElement))
            {
                outcome.NewSublocation = sublocationElement.GetString();
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
