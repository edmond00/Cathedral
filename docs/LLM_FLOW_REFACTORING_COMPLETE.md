# LLM Flow Refactoring - Implementation Complete

## Summary

Successfully refactored the LLM interaction flow in **Location Travel Mode (Phase 1)** from Director-controlled outcomes to a Critic-evaluated, programmatically-simulated system.

## Changes Implemented

### New Files Created

1. **ActionOutcomeSimulator.cs** (132 lines)
   - Programmatic outcome simulation using RNG
   - Replaces Director LLM for outcome determination
   - 70% default success rate
   - Parses consequences from ParsedAction and applies them

2. **ActionParser.cs** (167 lines)
   - Parses Director JSON into ParsedAction objects
   - Extracts success/failure consequences
   - Handles state changes, items, companions, sublocation changes

3. **ActionScorer.cs** (129 lines)
   - Scores actions using Critic evaluator
   - Weighted scoring: 40% skill coherence, 40% consequence plausibility, 20% context
   - Returns sorted list of ScoredAction objects

4. **ScoredAction.cs** (14 lines)
   - Data class for actions with evaluation scores
   - Tracks individual scores and total composite score

5. **ParsedAction.cs** (Updated - 31 lines)
   - Enhanced to include full consequence data
   - Success consequences: description, state changes, sublocation, items, companions
   - Failure consequences: type, description

### Modified Files

1. **LLMActionExecutor.cs**
   - Added `numberOfActions` parameter to `GenerateActionsAsync()` (default: 6, can now be 12)
   - Added `GenerateActionsWithRawJsonAsync()` - returns both ActionInfo and raw JSON
   - Added `GenerateFailureNarrativeAsync()` - creates dramatic failure narratives
   - Added `GetLlamaServerManager()` - exposes server for Critic initialization

2. **LocationTravelGameController.cs**
   - Added new fields: `_actionOutcomeSimulator`, `_criticEvaluator`, `_actionScorer`, `_currentParsedActions`
   - Updated `SetLLMActionExecutor()` to initialize Critic and ActionScorer
   - **Refactored `RegenerateActionsAsync()`**:
     * Generates 12 actions from Director
     * Parses full consequence data via ActionParser
     * Scores all actions via Critic (if available)
     * Selects top 6 based on scores
     * Falls back gracefully if Critic unavailable
   - **Refactored `ExecuteActionAsync()`**:
     * Uses `ActionOutcomeSimulator` for programmatic outcomes
     * On failure: calls Narrator for dramatic failure narrative, then exits
     * On success: continues with new action generation
   - Added Critic disposal in `Dispose()`

## New Flow Architecture

```
[Director: 12 actions] 
    ↓
[Parse JSON → ParsedAction[12]]
    ↓
[Critic: Score each action]
    ├─ Skill coherence (40%)
    ├─ Consequence plausibility (40%)
    └─ Context coherence (20%)
    ↓
[Select top 6 by score]
    ↓
[Narrator: Create scene narrative]
    ↓
[Player selects action]
    ↓
[ActionOutcomeSimulator: RNG outcome]
    ├─ Success (70%): Continue → Generate 12 new actions
    └─ Failure (30%): Narrator generates failure narrative → Exit
```

## Key Features

### 1. Critic-Based Quality Control
- Every action evaluated for coherence before presentation
- Only highest-quality actions shown to player
- Configurable scoring weights for different criteria

### 2. Programmatic Outcome Simulation
- Director no longer decides outcomes
- RNG-based with configurable success rates
- Deterministic consequence application
- Future-ready for inventory, skill checks, etc.

### 3. Graceful Fallbacks
- System works without Critic (uses first 6 actions)
- Handles JSON parsing failures
- Falls back to ActionInfo if ParsedAction unavailable

### 4. Enhanced Failure Handling
- Failure triggers special Narrator call for dramatic closure
- Interaction ends on failure (no retry loop)
- Failure narrative provides story conclusion

## Testing Checklist

- [ ] Test with Critic enabled (12 actions → score → top 6)
- [ ] Test without Critic (should use first 6 actions)
- [ ] Test action success flow (should loop back)
- [ ] Test action failure flow (should generate failure narrative and exit)
- [ ] Verify JSON parsing works correctly
- [ ] Check probability scores are logged
- [ ] Verify state changes apply correctly
- [ ] Test sublocation changes
- [ ] Test item gains
- [ ] Verify failure types are captured

## Performance Considerations

### Current Performance
- 12 actions × 3 evaluations each = 36 Critic calls
- Each Critic call: ~300-500ms
- **Total evaluation time: ~15-20 seconds per turn**

### Future Optimizations
1. Parallelize Critic evaluations (reduce to ~3-5 seconds)
2. Cache common action patterns
3. Reduce number of actions generated
4. Adjust Critic timeouts

## Rollback Strategy

If issues arise:
1. Set `numberOfActions` back to 6 in `RegenerateActionsAsync()`
2. Skip Critic evaluation (condition already exists: `if (_actionScorer != null)`)
3. System will still use programmatic outcomes (architectural improvement preserved)

## Next Steps

1. **Test the implementation**
   - Run option 5 from main menu
   - Enter a location and test the new flow
   - Verify Critic evaluations appear in console
   - Test both success and failure scenarios

2. **Monitor LLM logs**
   - Check `logs/llm_communication_*.log` for:
     * Director generating 12 actions
     * Critic evaluation calls
     * Narrator failure narrative calls

3. **Tune parameters**
   - Adjust scoring weights if needed
   - Modify success rate (currently 70%)
   - Experiment with different numberOfActions (8, 10, 12)

4. **Performance optimization**
   - Profile Critic evaluation time
   - Implement parallel evaluation if needed
   - Consider caching strategies

## Build Status

✅ **Build successful** - No compilation errors
✅ All new classes created
✅ All integrations complete
✅ Graceful fallback paths implemented

The refactoring is complete and ready for testing!
