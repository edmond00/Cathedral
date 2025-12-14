# Director/Critic Refactoring - Implementation Summary

## Date: December 6, 2025

## Overview
Successfully refactored the Director/Critic flow to implement a two-pass evaluation system where:
1. **Director** focuses solely on generating action text with success consequences
2. **Critic First Pass** filters and scores all generated actions
3. **Critic Second Pass** evaluates the selected action's difficulty and determines failure outcomes

## Changes Made

### 1. Director Simplification

**Files Modified:**
- `src/glyph/microworld/LocationSystem/Blueprint2Constraint.cs`
- `src/glyph/microworld/LocationSystem/DirectorPromptConstructor.cs`

**Changes:**
- **Removed** `difficulty` field from action JSON structure
- **Removed** `failure_consequence` field from action JSON structure
- **Updated** Director system prompt to:
  - Focus only on skill coherence and success consequences
  - Emphasize concrete, specific actions (not abstract/general)
  - Emphasize location-appropriate actions

**Result:** Director now generates simpler JSON with only:
- `success_consequence` (pre-determined)
- `related_skill` (chosen from 5 candidates)
- `action_text` (generated, 3-8 words, concrete and specific)

### 2. Enhanced Critic First Pass (Action Filtering)

**Files Modified:**
- `src/game/ActionScorer.cs`
- `src/game/ScoredAction.cs`

**New Evaluation Criteria Added:**
1. **Location Coherence** - Does the action make sense in the current sublocation?
2. **Action Specificity** - Is the action concrete and specific (not abstract/general)?

**Existing Criteria:**
1. Skill coherence
2. Consequence plausibility
3. Context coherence (with previous action)

**Updated Scoring Weights:**
- Skill coherence: 25%
- Consequence plausibility: 25%
- Context coherence: 20%
- Location coherence: 15% (NEW)
- Action specificity: 15% (NEW)

**Enhanced Logging:**
- Detailed console output for each evaluation criterion
- Progress indicators showing scores for each question
- Top 3 actions summary after scoring

### 3. New Critic Second Pass (Post-Selection Evaluation)

**Files Created:**
- `src/game/ActionDifficultyEvaluator.cs`

**Functionality:**
- **Difficulty Evaluation:** Asks "Is this action easy to perform?" → converts to difficulty score (0.0-1.0)
- **Failure Consequence Evaluation:** Tests plausibility of each possible failure consequence:
  - injured
  - lost
  - equipment_loss
  - exhaustion
  - attacked
  - disease
- **Success Determination:** Uses difficulty score to calculate success probability:
  - Trivial (0.0): 95% success
  - Moderate (0.5): 70% success
  - Extreme (1.0): 40% success

**Difficulty Labels:**
- trivial: score < 0.15
- easy: score < 0.30
- basic: score < 0.45
- moderate: score < 0.60
- hard: score < 0.75
- very_hard: score < 0.90
- extreme: score >= 0.90

### 4. Integrated Two-Pass Flow

**Files Modified:**
- `src/game/LocationTravelGameController.cs`
- `src/game/ActionOutcomeSimulator.cs`

**Execution Pipeline:**
```
Player selects action
  ↓
[SECOND CRITIC PASS] Evaluate difficulty (7 criteria)
  ↓
[SECOND CRITIC PASS] Evaluate failure consequences (6 options)
  ↓
[OUTCOME SIMULATION] Determine success/failure based on difficulty
  ↓
[OUTCOME SIMULATION] Apply consequences
  ↓
[NARRATOR] Generate narrative
```

**ActionOutcomeSimulator Updates:**
- Added `forceSuccess` parameter to override RNG
- Added `overrideFailureConsequence` parameter for Critic-determined failures
- Now respects Critic's difficulty evaluation when determining outcomes

### 5. Comprehensive Logging

**Files Modified:**
- `src/game/LLMLogger.cs`

**New Logging Methods:**
- `LogCriticSecondPass()` - Logs difficulty evaluation and failure consequence plausibilities
- `LogError()` - General error logging

**Log Format:**
```
[SECOND CRITIC PASS] - Action Difficulty Evaluation
ACTION:
  try to climb the ancient tree
  
DIFFICULTY:
  Label: moderate
  Score: 0.5234 (52.3%)
  
FAILURE CONSEQUENCE PLAUSIBILITIES:
  injured              : 0.8521 (85.2%)
  exhaustion          : 0.7234 (72.3%)
  lost                : 0.3421 (34.2%)
  equipment_loss      : 0.2134 (21.3%)
  attacked            : 0.1523 (15.2%)
  disease             : 0.0421 (4.2%)
  
SELECTED FAILURE: injured
```

## Console Output Examples

### First Pass (Action Filtering):
```
🔍 Critic First Pass: Evaluating 12 actions...
   Checking: skill coherence, consequence plausibility, context, location fit, specificity

  [1/12] Evaluating: try to climb the ancient oak tree
    • Checking skill coherence with 'climbing'...
      → Score: 0.987
    • Checking consequence plausibility ('reach canopy')...
      → Score: 0.921
    • Checking context coherence with previous action...
      → Score: 0.834
    • Checking location coherence...
      → Score: 0.956
    • Checking action specificity...
      → Score: 0.912
    ✓ Total Score: 0.923 (duration: 1234ms)

✓ Critic First Pass Complete
  Top 3 actions:
    1. try to climb the ancient oak tree (score: 0.923)
    2. try to search for herbs in the undergrowth (score: 0.891)
    3. try to track the deer through the forest (score: 0.867)
```

### Second Pass (Difficulty Evaluation):
```
🎯 Evaluating selected action difficulty: try to climb the ancient oak tree
  📊 Difficulty: moderate (score: 0.523)
  🔍 Evaluating 6 possible failure consequences...
    • injured: 0.852
    • exhaustion: 0.723
    • lost: 0.342
    • equipment_loss: 0.213
    • attacked: 0.152
    • disease: 0.042
  ✓ Most plausible failure: injured (0.852)
  ⏱️  Evaluation duration: 1523ms

[OUTCOME SIMULATION] Critic-based success determination: SUCCESS
[OUTCOME SIMULATION] Difficulty score: 0.523, Success probability: 0.66
```

## Benefits

1. **Director Simplification**: Focuses on what LLMs do best (creative text generation) rather than game mechanics
2. **Better Action Quality**: Two-pass filtering ensures only coherent, location-appropriate, and specific actions reach the player
3. **Dynamic Difficulty**: Each action's difficulty is evaluated based on its content, not pre-assigned
4. **Intelligent Failures**: Failure consequences are contextually appropriate based on Critic evaluation
5. **Full Transparency**: Extensive logging at every step makes debugging and tuning easy
6. **Modular Design**: Each component has a single responsibility and can be tested independently

## Files Created
- `src/game/ActionDifficultyEvaluator.cs` (174 lines)

## Files Modified
- `src/glyph/microworld/LocationSystem/Blueprint2Constraint.cs`
- `src/glyph/microworld/LocationSystem/DirectorPromptConstructor.cs`
- `src/game/ActionScorer.cs`
- `src/game/ScoredAction.cs`
- `src/game/LocationTravelGameController.cs`
- `src/game/ActionOutcomeSimulator.cs`
- `src/game/LLMLogger.cs`

## Testing Status
- ✅ Code compiles successfully
- ⏳ Runtime testing pending (requires application restart)
- ⏳ Integration testing with LLM pending

## Next Steps for Testing
1. Restart the application
2. Enter a location and observe First Pass evaluation logs
3. Select an action and observe Second Pass evaluation logs
4. Verify difficulty-based success/failure works correctly
5. Check that failure consequences are appropriate
6. Review log files for completeness

## Notes
- The probability calculation fix (using average instead of sum) from the earlier session is included
- All logging is comprehensive and includes emojis for easy visual parsing
- The system gracefully falls back if Critic is unavailable
- Location blueprint information is now passed through the entire pipeline
