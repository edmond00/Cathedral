# Phase 5: LLM Outcome Generation Fixes

## Issues Identified from Logs

Based on the log file `llm_communication_2025-11-23_23-08-15.log`, several critical issues were discovered:

### Issue 1: Wrong Response Format ❌
**Problem:** When asked to generate an action **outcome**, Director returns action **choices** instead.

**Log Evidence:**
```
USER PROMPT:
  ACTION TAKEN:
  try to engage with wildlife without disturbing their behavior...
  
  Generate the JSON outcome for this action.

RESPONSE:
  {
  "actions":[{"action_text":"try to engage with wildlife",...}]}
```

**Expected Format:**
```json
{
  "success": true,
  "narrative": "You successfully...",
  "state_changes": {...},
  "new_sublocation": null,
  "items_gained": [],
  "ends_interaction": false
}
```

**Actual Format:**
```json
{
  "actions": [{...}, {...}, ...]
}
```

### Issue 2: Malformed JSON ❌
**Problem:** LLM response contains syntax errors and is truncated.

**Log Evidence:**
```
PARSE ERROR → Director
ERROR: Outcome parse error: 'r' is invalid after a property name. 
Expected a ':'. LineNumber: 1 | BytePositionInLine: 813.

RAW RESPONSE:
  ...,"description":"lost the compass on the first day. No shelter found, no food.","},...
  }]}
```

**Problems:**
- Extra quote before closing brace: `food.","`
- Truncated with `...}]}` instead of complete JSON

### Issue 3: Silent Fallback ❌
**Problem:** After LLM fails, system silently falls back to SimpleActionExecutor.

**Log Evidence:**
```
[23:10:29.926] PARSE ERROR → Director
[23:10:29.937] FALLBACK → Using SimpleActionExecutor
REASON: Director LLM returned null outcome
```

**User Experience:**
- No error message shown to user
- Player sees mock action outcome instead of real LLM-generated one
- No indication that LLM failed
- Debugging is difficult

### Issue 4: Slot Conflict ❌
**Problem:** Trying to use Director slot while it's already processing.

**Log Evidence:**
```
[23:10:29.970] RESPONSE ← Director (Slot 0) - ✗ FAILED
Duration: 12ms
ERROR: Exception: Instance 0 is already processing a request.
```

**Root Cause:**
- Director (Slot 0) is generating new action choices
- System tries to use same slot to generate action outcome
- llama-server rejects concurrent requests on same slot

## Root Causes

### 1. Missing GBNF Grammar for Outcomes
**File:** `src/game/LLMActionExecutor.cs`  
**Method:** `GenerateOutcomeAsync()`  
**Line:** ~279 (old code)

```csharp
var response = await RequestFromLLMAsync(
    _directorSlotId,
    systemPrompt,
    userPrompt,
    null, // No GBNF for now - would need to define outcome schema ❌
    timeoutSeconds: 25);
```

**Problem:** Without GBNF grammar constraints, the LLM sees the system prompt about "DIRECTOR" and defaults to generating action choices (which it was just doing).

### 2. Reusing Same Slot
**File:** `src/game/LLMActionExecutor.cs`  
**Method:** `GenerateOutcomeAsync()`

```csharp
var response = await RequestFromLLMAsync(
    _directorSlotId, // ❌ Same slot used for both actions and outcomes
    systemPrompt,
    userPrompt,
    gbnfGrammar,
    timeoutSeconds: 25);
```

**Problem:** 
1. `GenerateActionsAsync()` starts using Director slot
2. User clicks action
3. `ExecuteActionAsync()` calls `GenerateOutcomeAsync()`
4. `GenerateOutcomeAsync()` tries to use Director slot again
5. Slot is still busy → "Instance already processing" error

### 3. Silent Fallback Logic
**File:** `src/game/LLMActionExecutor.cs`  
**Method:** `ExecuteActionAsync()`

```csharp
if (outcome == null)
{
    Console.WriteLine("LLMActionExecutor: Director LLM failed, using fallback");
    LLMLogger.LogFallback("Director LLM returned null outcome");
    return _fallbackExecutor.ExecuteAction(...); // ❌ Silent fallback
}
```

**Problem:** User never sees that LLM failed. They get mock content and think LLM is working.

## Solutions Implemented

### Solution 1: Add GBNF Grammar for Outcomes ✅

**File:** `src/game/LLMActionExecutor.cs`  
**Method:** `GenerateOutcomeAsync()`

Added strict GBNF grammar to force correct JSON format:

```csharp
var gbnfGrammar = @"root ::= ActionOutcome

ActionOutcome ::= ""{"" ws ""\""\"""" ""success"" ""\""\"""" "":"" ws boolean ws "",""  ws ""\""\"""" ""narrative"" ""\""\"""" "":"" ws string ws "",""  ws ""\""\"""" ""state_changes"" ""\""\"""" "":"" ws state-changes ws "",""  ws ""\""\"""" ""new_sublocation"" ""\""\"""" "":"" ws sublocation-value ws "",""  ws ""\""\"""" ""items_gained"" ""\""\"""" "":"" ws items-array ws "",""  ws ""\""\"""" ""ends_interaction"" ""\""\"""" "":"" ws boolean ws ""}""

state-changes ::= ""{}""  | ""{"" ws ""\""\"""" ""category"" ""\""\"""" "":"" ws string ws "",""  ws ""\""\"""" ""new_state"" ""\""\"""" "":"" ws string ws ""}""

sublocation-value ::= string | ""null""

items-array ::= ""[]""  | ""[""  ws string (ws "",""  ws string)* ws ""]""

# JSON primitives
ws ::= [ \t\n\r]*
string ::= ""\""\"""" [^""\""\"""" \\]* ""\""\""""  
boolean ::= ""true""  | ""false"" ";
```

**Benefits:**
- Forces LLM to output correct format
- Prevents returning action choices when outcome expected
- Syntax errors less likely (GBNF enforces structure)
- More reliable parsing

### Solution 2: Replace Silent Fallback with Errors ✅

**File:** `src/game/LLMActionExecutor.cs`  
**Method:** `ExecuteActionAsync()`

**Before:**
```csharp
if (outcome == null) {
    return _fallbackExecutor.ExecuteAction(...); // Silent
}
```

**After:**
```csharp
if (outcome == null) {
    Console.Error.WriteLine("LLMActionExecutor: Director LLM failed to generate outcome");
    LLMLogger.LogFallback("Director LLM returned null outcome");
    
    return ActionResult.CreateFailure(
        "ERROR: Failed to process your action.\n\n" +
        "The LLM did not return a valid outcome. This could be due to:\n" +
        "- LLM server timeout\n" +
        "- Invalid response format\n" +
        "- Slot conflict (already processing)\n\n" +
        "Check logs/llm_communication_*.log for details.");
}
```

**Benefits:**
- User sees clear error message
- Directs user to log file for debugging
- No confusion about whether LLM is working
- Logs still capture failure reason

### Solution 3: Better Exception Handling ✅

**Before:**
```csharp
catch (Exception ex) {
    return _fallbackExecutor.ExecuteAction(...); // Silent
}
```

**After:**
```csharp
catch (Exception ex) {
    Console.Error.WriteLine($"LLMActionExecutor: Error in LLM execution: {ex.Message}");
    LLMLogger.LogFallback($"Exception: {ex.Message}");
    
    return ActionResult.CreateFailure(
        $"ERROR: Exception during action processing.\n\n" +
        $"Error: {ex.Message}\n\n" +
        $"Check logs/llm_communication_*.log for details.");
}
```

**Benefits:**
- Exception details shown to user
- Error logged for debugging
- No silent failures

## Slot Conflict Issue - Remaining Work ⚠️

The slot conflict issue (`Instance 0 is already processing`) is **partially addressed** but not fully solved:

### Current State:
- GBNF grammar should prevent format confusion
- Errors are no longer silent
- Logs show when slot conflicts occur

### Not Yet Solved:
The fundamental issue is that we're trying to use the **same slot** for two concurrent operations:
1. Generating new actions (after action completes)
2. Generating outcome for current action

### Possible Solutions:

#### Option A: Sequential Processing (Recommended)
Don't generate new actions until outcome is complete:
1. User clicks action
2. Generate outcome (use Director slot)
3. Wait for outcome to complete
4. Generate new actions (use Director slot again)

**Pros:** Simple, uses existing slots  
**Cons:** Slightly slower (sequential)

#### Option B: Separate Outcome Slot
Create a third slot specifically for outcomes:
```csharp
private int _directorSlotId = -1;      // For action generation
private int _narratorSlotId = -1;      // For narrative
private int _outcomeSlotId = -1;       // For action outcomes (NEW)
```

**Pros:** Parallel processing possible  
**Cons:** More slots = more memory, harder to manage

#### Option C: Action Queue
Queue action requests and process one at a time:
```csharp
private Queue<ActionRequest> _actionQueue = new();
```

**Pros:** Prevents conflicts completely  
**Cons:** More complex, adds latency

### Recommended Approach:
**Option A** - The code already does this somewhat, but we should ensure:
1. Outcome generation finishes first
2. Only then regenerate actions
3. Never call both concurrently

Check `LocationTravelGameController.ExecuteActionAsync()` to verify this order.

## Testing Checklist

### Test 1: Outcome Format ✅
- [x] GBNF grammar added
- [x] Compiles successfully
- [ ] Run app, click action
- [ ] Check log file for outcome request
- [ ] Verify response has correct format (success/narrative/etc)
- [ ] Verify NO action choices returned

### Test 2: Error Messages ✅
- [x] Error handling updated
- [x] Compiles successfully
- [ ] Trigger LLM failure (stop server mid-game)
- [ ] Verify user sees error message (not mock content)
- [ ] Verify error points to log file
- [ ] Check log file has FALLBACK entry

### Test 3: Slot Conflicts
- [ ] Run app with LLM enabled
- [ ] Click action rapidly multiple times
- [ ] Check if "Instance already processing" still occurs
- [ ] If yes, need Option A/B/C fix
- [ ] If no, GBNF grammar solved it

### Test 4: End-to-End
- [ ] Start llama-server with "tiny" model
- [ ] Run app, enter location
- [ ] Perform 5-10 actions
- [ ] Verify each action has real LLM outcome (not mock)
- [ ] Check log file for all requests/responses
- [ ] Verify no PARSE ERRORs
- [ ] Verify no FALLBACK events

## Files Modified

1. **src/game/LLMActionExecutor.cs**
   - Added GBNF grammar to `GenerateOutcomeAsync()` (~290 lines)
   - Replaced silent fallbacks with error returns (~95-118 lines)
   - Better exception handling with user-facing messages

2. **PHASE5_OUTCOME_FIXES.md** (NEW)
   - This document

## Expected Improvements

### Before Fixes:
- ❌ LLM returns action choices when asking for outcome
- ❌ Malformed JSON with syntax errors
- ❌ Silent fallback to mock content
- ❌ "Instance already processing" errors
- ❌ User has no idea LLM failed

### After Fixes:
- ✅ GBNF grammar enforces correct outcome format
- ✅ Stricter JSON structure reduces parse errors
- ✅ Clear error messages when LLM fails
- ✅ User directed to log file for debugging
- ⚠️ Slot conflicts may still occur (need testing)

## Next Steps

1. **Test the fixes:**
   ```bash
   dotnet run
   # Enter location
   # Click actions
   # Check logs/llm_communication_*.log
   ```

2. **Monitor for slot conflicts:**
   - Look for "Instance already processing" in logs
   - If occurs, implement Option A (sequential processing)

3. **Verify GBNF grammar:**
   - Check log file for outcome requests
   - Verify GBNF grammar is present
   - Verify responses match expected format

4. **Compare quality:**
   - Before: Mock outcomes
   - After: Real LLM outcomes
   - Document improvement in narrative quality

## Success Metrics

**Build:** ✅ Compiles without errors  
**GBNF Grammar:** ✅ Added to outcome generation  
**Error Handling:** ✅ No more silent fallbacks  
**User Experience:** ✅ Clear error messages  
**Logging:** ✅ All failures captured in logs  
**Testing:** ⏳ Pending - need to run app  
**Slot Conflicts:** ⚠️ May still need fix (test first)

---

**Status:** 🔧 Fixes implemented, awaiting testing  
**Priority:** HIGH - Blocks Phase 5 completion  
**Impact:** Major - Makes LLM outcomes actually work correctly
