# LLM Flow Refactoring Implementation Plan

## Current State Analysis

### Location Travel Mode (Phase 1) - Current Architecture

**Entry Point**: `Program.cs` → Case 5 → `Cathedral.Game.LocationTravelModeLauncher.Launch()`

**Flow**:
```
1. LocationTravelModeLauncher.Launch()
   ├─ Creates GlyphSphereCore (visual world)
   ├─ Creates MicroworldInterface (world data)
   ├─ Initializes LlamaServerManager (LLM server)
   ├─ Creates LLMActionExecutor (LLM orchestrator)
   └─ Creates LocationTravelGameController (game logic)

2. LocationTravelGameController manages game state
   ├─ Handles mode transitions (WorldView → Traveling → LocationInteraction)
   ├─ Manages location instances and blueprints
   └─ Coordinates action execution

3. When player enters location:
   ├─ StartLocationInteraction() called
   └─ RenderLocationUIAsync() generates initial content

4. RenderLocationUIAsync() flow:
   ├─ Calls RegenerateActionsAsync()
   │   └─ _llmActionExecutor.GenerateActionsAsync()
   │       └─ DirectorPromptConstructor generates 6 actions
   │
   ├─ Calls _llmActionExecutor.GenerateNarrativeAsync()
   │   └─ NarratorPromptConstructor creates scene description
   │
   └─ Displays in terminal UI

5. When player selects action:
   ├─ ExecuteActionAsync() called
   ├─ _llmActionExecutor.ExecuteActionAsync() processes action
   │   └─ Currently uses GenerateOutcomeAsync() (Director decides outcome)
   │
   ├─ ApplyActionResult() updates state
   ├─ If continues: RegenerateActionsAsync() + RenderLocationUIAsync()
   └─ If ends: HandleInteractionEnd()
```

### Current Implementation Details

**LocationTravelGameController.cs** (~817 lines)
- Line ~200-260: `ExecuteActionAsync()` - Main action execution
- Line ~260-470: Action regeneration and rendering
- Line ~470+: Location interaction management

**Key Issues**:
1. ❌ Director generates only 6 actions (hardcoded)
2. ❌ No quality filtering - all 6 actions shown
3. ❌ Director LLM decides outcomes (via `GenerateOutcomeAsync()`)
4. ❌ No Critic evaluation step
5. ❌ No coherence scoring

### What Works Well
1. ✅ LLMActionExecutor already separates Director/Narrator
2. ✅ Director uses JSON constraints (GBNF)
3. ✅ Fallback system exists
4. ✅ Logging infrastructure in place
5. ✅ CriticEvaluator fully implemented
6. ✅ ActionScorer implemented
7. ✅ ParsedAction/ScoredAction classes exist

---

## New Flow Design

### Proposed Architecture

```
LocationTravelGameController.ExecuteActionAsync()
  ↓
  [1] Generate 12 actions via Director
      └─ LLMActionExecutor.GenerateActionsAsync(numberOfActions: 12)
  ↓
  [2] Parse actions into ParsedAction objects
      └─ Extract: ActionText, Skill, SuccessConsequence, FailureConsequence
  ↓
  [3] Score all actions via Critic
      └─ ActionScorer.ScoreActionsAsync(actions, previousAction)
          ├─ For each action:
          │   ├─ Evaluate action-skill coherence
          │   ├─ Evaluate action-consequence plausibility
          │   └─ Evaluate context coherence (if not first turn)
          └─ Return sorted list (best first)
  ↓
  [4] Select top 6 actions
      └─ Keep only highest-scored actions
  ↓
  [5] Generate narrative with top actions
      └─ NarratorPromptConstructor.ConstructPrompt(previousAction, top6Actions)
  ↓
  [6] Display in UI
      └─ TerminalLocationUI shows narrative + 6 options
  ↓
  [Player selects action]
  ↓
  [7] Determine outcome PROGRAMMATICALLY
      ├─ RNG for success/failure (70% success for now)
      ├─ Parse consequences from selected action JSON
      └─ Build ActionResult object
  ↓
  [8] Apply state changes
      └─ LocationInstanceState.ApplyActionResult()
  ↓
  [9] Check outcome:
      ├─ SUCCESS → Loop back to [1]
      └─ FAILURE → Generate failure narrative and exit
```

### Detailed Changes Required

#### 1. **ActionOutcomeSimulator** (NEW CLASS)
**File**: `src/game/ActionOutcomeSimulator.cs`
**Purpose**: Replace Director-based outcome generation with programmatic RNG

```csharp
public class ActionOutcomeSimulator
{
    private readonly Random _random = new Random();
    
    public ActionResult SimulateOutcome(
        ParsedAction selectedAction,
        LocationInstanceState currentState,
        LocationBlueprint blueprint)
    {
        // RNG for success/failure
        bool success = _random.NextDouble() > 0.3; // 70% success
        
        // Build ActionResult from parsed consequences
        if (success)
        {
            return ActionResult.CreateSuccess(
                narrativeOutcome: selectedAction.SuccessConsequence,
                stateChanges: selectedAction.SuccessStateChanges,
                newSublocation: selectedAction.SuccessSublocationChange,
                itemsGained: selectedAction.SuccessItemsGained);
        }
        else
        {
            return ActionResult.CreateFailure(
                narrativeOutcome: selectedAction.FailureConsequence,
                endsInteraction: true); // Failure ends the game
        }
    }
}
```

#### 2. **ParsedAction** (NEW CLASS)
**File**: `src/game/ParsedAction.cs`
**Purpose**: Strongly-typed action data extracted from Director JSON

```csharp
public class ParsedAction
{
    public string ActionText { get; set; }
    public string Skill { get; set; }
    public int Difficulty { get; set; }
    public string Risk { get; set; }
    
    // Success consequences
    public string SuccessConsequence { get; set; }
    public Dictionary<string, string> SuccessStateChanges { get; set; }
    public string? SuccessSublocationChange { get; set; }
    public List<string>? SuccessItemsGained { get; set; }
    
    // Failure consequences
    public string FailureConsequence { get; set; }
    public string FailureType { get; set; }
}
```

#### 3. **ScoredAction** (NEW CLASS)
**File**: `src/game/ScoredAction.cs`
**Purpose**: Action with Critic evaluation scores

```csharp
public class ScoredAction
{
    public ParsedAction Action { get; set; }
    public double SkillScore { get; set; }
    public double ConsequenceScore { get; set; }
    public double ContextScore { get; set; }
    public double TotalScore { get; set; }
    public double EvaluationDurationMs { get; set; }
}
```

#### 4. **ActionScorer** (ALREADY EXISTS ✓)
**File**: `src/game/ActionScorer.cs`
**Status**: Already implemented with all required methods

#### 5. **LocationTravelGameController - Major Refactoring**
**File**: `src/game/LocationTravelGameController.cs`

**Changes to `RegenerateActionsAsync()`**:
```csharp
private async Task RegenerateActionsAsync()
{
    if (_llmActionExecutor == null || _currentLocationState == null || _currentBlueprint == null)
    {
        // Fallback to simple executor
        _currentActions = _simpleActionExecutor.GenerateActions(...);
        return;
    }
    
    // [1] Generate 12 actions via Director
    var rawActions = await _llmActionExecutor.GenerateActionsAsync(
        _currentLocationState,
        _currentBlueprint,
        _currentLocationState.GetLastAction(),
        numberOfActions: 12); // NEW PARAMETER
    
    if (rawActions == null || rawActions.Count == 0)
    {
        // Fallback
        _currentActions = _simpleActionExecutor.GenerateActions(...);
        return;
    }
    
    // [2] Parse into ParsedAction objects
    var parsedActions = ParseActions(rawActions);
    
    // [3] Score via Critic (if available)
    if (_criticEvaluator != null)
    {
        var scoredActions = await _actionScorer.ScoreActionsAsync(
            parsedActions,
            _currentLocationState.GetLastAction());
        
        // [4] Select top 6
        var topActions = scoredActions.Take(6).Select(s => s.Action).ToList();
        
        // Convert back to ActionInfo for display
        _currentActions = topActions.Select(a => new ActionInfo(a.ActionText, a.Skill)).ToList();
        
        // Store parsed actions for later outcome simulation
        _currentParsedActions = topActions;
    }
    else
    {
        // No Critic - use all actions (fallback)
        _currentActions = rawActions;
        _currentParsedActions = parsedActions.Take(6).ToList();
    }
}
```

**Changes to `ExecuteActionAsync()`**:
```csharp
private async Task ExecuteActionAsync(int actionIndex)
{
    // ... existing setup ...
    
    // [7] Determine outcome PROGRAMMATICALLY
    var selectedParsedAction = _currentParsedActions[actionIndex];
    var result = _actionOutcomeSimulator.SimulateOutcome(
        selectedParsedAction,
        _currentLocationState,
        _currentBlueprint);
    
    // ... existing state application ...
    
    // [9] Check outcome
    if (!result.Success)
    {
        // FAILURE - Generate failure narrative via Narrator
        var failureNarrative = await _llmActionExecutor.GenerateFailureNarrativeAsync(
            _currentLocationState,
            _currentBlueprint,
            lastAction,
            result.NarrativeOutcome);
        
        result = ActionResult.CreateFailure(
            failureNarrative ?? result.NarrativeOutcome,
            endsInteraction: true);
        
        HandleInteractionEnd(result);
        return;
    }
    
    // SUCCESS - Continue with new actions
    await RegenerateActionsAsync();
    await RenderLocationUIAsync();
}
```

#### 6. **LLMActionExecutor - Add Methods**
**File**: `src/game/LLMActionExecutor.cs`

**New method signature**:
```csharp
public async Task<List<ActionInfo>?> GenerateActionsAsync(
    LocationInstanceState currentState,
    LocationBlueprint blueprint,
    PlayerAction? previousAction = null,
    int numberOfActions = 6) // NEW PARAMETER
{
    var director = new DirectorPromptConstructor(
        blueprint,
        currentState.CurrentSublocation,
        currentState.CurrentStates,
        numberOfActions); // Pass through
    
    // ... rest of implementation ...
}
```

**New method for failure narrative**:
```csharp
public async Task<string?> GenerateFailureNarrativeAsync(
    LocationInstanceState currentState,
    LocationBlueprint blueprint,
    PlayerAction lastAction,
    string outcomeDescription)
{
    var narrator = new NarratorPromptConstructor(
        blueprint,
        currentState.CurrentSublocation,
        currentState.CurrentStates);
    
    // Build prompt specifically for failure
    var userPrompt = $@"FAILURE OUTCOME:
The player attempted: {lastAction.ActionText}
Result: {outcomeDescription}

Write a dramatic 2-3 sentence narrative describing this failure and its consequences.";
    
    var response = await RequestFromLLMAsync(
        _narratorSlotId,
        userPrompt,
        gbnfGrammar: null, // Free-form narrative
        timeoutSeconds: 30);
    
    return response?.Trim();
}
```

#### 7. **LocationTravelGameController - Add Members**
```csharp
private ActionScorer? _actionScorer;
private CriticEvaluator? _criticEvaluator;
private ActionOutcomeSimulator _actionOutcomeSimulator;
private List<ParsedAction> _currentParsedActions = new();
```

**Initialize in constructor**:
```csharp
public LocationTravelGameController(...)
{
    // ... existing code ...
    
    _actionOutcomeSimulator = new ActionOutcomeSimulator();
}
```

**Add in `SetLLMActionExecutor()`**:
```csharp
public void SetLLMActionExecutor(LLMActionExecutor executor)
{
    _llmActionExecutor = executor;
    
    // Also initialize Critic and ActionScorer
    if (executor != null)
    {
        _criticEvaluator = new CriticEvaluator(executor.GetLlamaServerManager());
        _ = Task.Run(async () => await _criticEvaluator.InitializeAsync());
        
        _actionScorer = new ActionScorer(_criticEvaluator);
    }
    
    Console.WriteLine("LocationTravelGameController: LLM action executor enabled");
}
```

---

## Implementation Checklist

### Phase 1: Create New Classes
- [x] ✓ Create `ActionScorer.cs` (already exists)
- [x] ✓ Create `ParsedAction.cs` (already exists)
- [x] ✓ Create `ScoredAction.cs` (already exists)
- [ ] Create `ActionOutcomeSimulator.cs`
- [ ] Add helper method to parse Director JSON → List<ParsedAction>

### Phase 2: Refactor LLMActionExecutor
- [ ] Add `numberOfActions` parameter to `GenerateActionsAsync()`
- [ ] Add `GenerateFailureNarrativeAsync()` method
- [ ] Expose `GetLlamaServerManager()` for Critic initialization

### Phase 3: Refactor LocationTravelGameController
- [ ] Add fields: `_criticEvaluator`, `_actionScorer`, `_actionOutcomeSimulator`, `_currentParsedActions`
- [ ] Update `SetLLMActionExecutor()` to initialize Critic
- [ ] Refactor `RegenerateActionsAsync()` with new flow
- [ ] Refactor `ExecuteActionAsync()` with programmatic outcomes
- [ ] Handle failure case with final narrative

### Phase 4: Testing
- [ ] Test with 12 actions generated
- [ ] Verify Critic evaluation (check logs)
- [ ] Verify top-6 selection works
- [ ] Test success loop
- [ ] Test failure exit with narrative
- [ ] Verify fallback when Critic unavailable

---

## Expected Behavior After Implementation

### Action Generation (First Turn)
```
1. Director generates 12 actions
2. Critic evaluates each (12 evaluations = ~10-20 seconds)
3. Actions sorted by score
4. Top 6 selected
5. Narrator creates scene with top 6 actions
6. Player sees 6 high-quality, coherent actions
```

### Action Execution (Success)
```
1. Player selects action
2. RNG determines success (70%)
3. State changes applied (items, sublocation, states)
4. Loop back to action generation
```

### Action Execution (Failure)
```
1. Player selects action
2. RNG determines failure (30%)
3. Narrator generates failure narrative
4. Game ends, returns to world view
```

### Performance Considerations
- 12 actions * 3 evaluations each = 36 Critic calls per turn
- Each Critic call: ~300-500ms
- Total evaluation time: ~15-20 seconds per turn
- **Optimization**: Could parallelize Critic evaluations

---

## Rollback Plan

If new flow causes issues:
1. Keep `numberOfActions = 6` (no Critic overhead)
2. Skip Critic evaluation step
3. Use all 6 actions generated
4. Keep programmatic outcome simulation

This preserves the main architectural improvement (programmatic outcomes) while allowing gradual Critic integration.
