# Phase 5: Error Handling - No Mock Fallbacks

## Overview

Removed all mock action/narrative fallbacks. The system now shows clear error messages directing users to check logs when LLM generation fails, rather than silently falling back to mock content.

## Changes Made

### 1. **Action Generation Failures**

**Before:**
```csharp
var llmActions = await _llmActionExecutor.GenerateActionsAsync(...);
_currentActions = llmActions ?? GenerateMockActions(); // Silent fallback
```

**After:**
```csharp
var llmActions = await _llmActionExecutor.GenerateActionsAsync(...);
if (llmActions == null || llmActions.Count == 0)
{
    Console.Error.WriteLine("Failed to generate actions from LLM");
    _terminalUI?.ShowResultMessage(
        "ERROR: Failed to generate actions.\n\n" +
        "The LLM did not return valid actions. This could be due to:\n" +
        "- LLM server not responding\n" +
        "- Invalid response format\n" +
        "- Timeout\n\n" +
        "Check logs/llm_communication_*.log for details.\n\n" +
        "Click anywhere to return to world view...",
        false
    );
    _waitingForClickToExit = true;
    return; // Exit instead of continuing with mock data
}
```

### 2. **Narrative Generation Failures**

**Before:**
```csharp
var llmNarrative = await _llmActionExecutor.GenerateNarrativeAsync(...);
_currentNarrative = llmNarrative ?? GenerateMockNarrative(blueprint); // Silent fallback
```

**After:**
```csharp
var llmNarrative = await _llmActionExecutor.GenerateNarrativeAsync(...);
if (string.IsNullOrWhiteSpace(llmNarrative))
{
    Console.Error.WriteLine("Failed to generate narrative from LLM");
    _terminalUI?.ShowResultMessage(
        "ERROR: Failed to generate narrative.\n\n" +
        "The LLM did not return a valid narrative description. This could be due to:\n" +
        "- LLM server not responding\n" +
        "- Invalid response format\n" +
        "- Timeout\n\n" +
        "Check logs/llm_communication_*.log for details.\n\n" +
        "Click anywhere to return to world view...",
        false
    );
    _waitingForClickToExit = true;
    return;
}
```

### 3. **LLM Executor Not Available**

**Before:**
```csharp
if (_llmActionExecutor != null) {
    // Use LLM
} else {
    _currentActions = GenerateMockActions(); // Silent fallback
}
```

**After:**
```csharp
if (_llmActionExecutor != null) {
    // Use LLM
} else {
    Console.Error.WriteLine("LLM executor not available");
    _terminalUI?.ShowResultMessage(
        "ERROR: LLM system not initialized.\n\n" +
        "The location interaction system requires LLM to be enabled.\n" +
        "Restart the application with LLM enabled.\n\n" +
        "Click anywhere to return to world view...",
        false
    );
    _waitingForClickToExit = true;
    return;
}
```

### 4. **Action Regeneration Failures**

**Before:**
```csharp
var llmActions = await _llmActionExecutor.GenerateActionsAsync(...);
_currentActions = llmActions ?? GenerateMockActions();
```

**After:**
```csharp
var llmActions = await _llmActionExecutor.GenerateActionsAsync(...);
if (llmActions == null || llmActions.Count == 0)
{
    Console.Error.WriteLine("Failed to regenerate actions from LLM");
    _terminalUI?.ShowResultMessage(
        "ERROR: Failed to generate new actions.\n\n" +
        "The LLM did not return valid actions after your last action.\n" +
        "Check logs/llm_communication_*.log for details.\n\n" +
        "Click anywhere to return to world view...",
        false
    );
    _waitingForClickToExit = true;
    return;
}
```

### 5. **Removed Mock Methods**

**Deleted:**
- `GenerateMockActions()` - No longer used
- `GenerateMockNarrative()` - No longer used

These methods created placeholder content that masked real failures. Now failures are explicit and debuggable.

## Error Message Structure

All error messages follow this pattern:

```
ERROR: [Brief description of what failed]

[More detailed explanation of what happened]
This could be due to:
- [Possible cause 1]
- [Possible cause 2]
- [Possible cause 3]

Check logs/llm_communication_*.log for details.

Click anywhere to return to world view...
```

## User Experience Flow

### Success Path
1. User clicks on location
2. Loading indicator shows "Generating actions..."
3. Actions generated successfully
4. Loading indicator shows "Generating narrative..."
5. Narrative generated successfully
6. UI renders with actions and narrative

### Failure Path (New Behavior)
1. User clicks on location
2. Loading indicator shows "Generating actions..."
3. **LLM fails to return actions**
4. **Error message displayed with log file location**
5. **User clicks to acknowledge**
6. **Returns to world view (safe state)**

### Old Behavior (Removed)
1. User clicks on location
2. Loading indicator shows "Generating actions..."
3. LLM fails to return actions
4. ~~Silently falls back to mock actions~~
5. ~~User sees generic mock content~~
6. ~~No indication that LLM failed~~

## Benefits

### 1. **Transparency**
- Users know immediately when LLM fails
- No confusion between real and mock content
- Clear distinction between working and broken states

### 2. **Debuggability**
- Error messages point to log files
- Console errors logged for developer visibility
- Failures don't cascade into corrupt game state

### 3. **Production Readiness**
- Mock fallbacks hidden production issues
- Now failures are visible and actionable
- Forces proper LLM initialization and monitoring

### 4. **User Control**
- Click-to-exit gives users time to read error
- Returns to safe state (world view)
- No broken interaction states

## Debugging Workflow

When user sees an error message:

1. **Check Console Output**
   ```
   RenderLocationUIAsync: Failed to generate actions from LLM
   ```

2. **Check LLM Log File**
   ```
   logs/llm_communication_2025-11-23_14-30-00.log
   ```

3. **Look for Failure Indicators**
   ```
   RESPONSE #5: Director ✗ (5000ms) | 2025-11-23 14:35:45.123
   TIMEOUT: Request exceeded 5000ms

   OR

   PARSE ERROR: Director | 2025-11-23 14:35:45.123
   Error: Missing 'actions' property
   [Raw Response]
   { "invalid": "json" }

   OR

   FALLBACK: LLM server not ready
   Timestamp: 2025-11-23 14:35:45.123
   Using SimpleActionExecutor instead.
   ```

4. **Identify Root Cause**
   - Server not running? Start llama-server
   - Timeout? Model too large/slow
   - Parse error? Fix prompt or GBNF grammar
   - Fallback? Check server initialization

5. **Fix and Retry**
   - Restart app with proper LLM setup
   - Try again with working configuration

## Testing Scenarios

### Scenario 1: LLM Server Not Running
**Setup:** Start app without starting llama-server first

**Expected:**
- Error on first location click
- Message: "LLM server not ready"
- Log shows FALLBACK event
- User returned to world view

**Action:**
- Start llama-server
- Restart app
- Try again

### Scenario 2: Invalid Model
**Setup:** Start llama-server with corrupt model file

**Expected:**
- Error on first location click
- Timeout or invalid response
- Log shows request but no valid response
- User returned to world view

**Action:**
- Fix model file path
- Restart llama-server
- Try again

### Scenario 3: Slow Response
**Setup:** Use large model (phi-4) with high context

**Expected:**
- Long loading animation (10-15 seconds)
- Eventually succeeds OR times out
- If timeout, error message shown
- Log shows duration

**Action:**
- Increase timeout in LlamaServerManager
- OR use smaller model ("tiny" instead of "medium")

### Scenario 4: Malformed JSON
**Setup:** LLM returns text without proper JSON

**Expected:**
- Error after loading completes
- Message: "Invalid response format"
- Log shows PARSE ERROR with raw response
- User returned to world view

**Action:**
- Improve prompt to emphasize JSON format
- Add more examples to system prompt
- Adjust temperature/sampling parameters

## Configuration Recommendations

### Development
- **useLLM = true** (test real LLM)
- **modelAlias = "tiny"** (faster iteration)
- **timeout = 5000ms** (fail fast)
- Check logs frequently

### Production
- **useLLM = true** (always)
- **modelAlias = "medium"** (better quality)
- **timeout = 10000ms** (allow for slower responses)
- Monitor logs for patterns
- Alert on high failure rates

## Related Files

- **LocationTravelGameController.cs** - Error handling logic
- **LLMActionExecutor.cs** - LLM integration with fallback to SimpleActionExecutor
- **LLMLogger.cs** - Comprehensive logging
- **TerminalLocationUI.cs** - Error message display

## Migration Notes

**If you see errors after this change:**

1. ✅ **This is expected!** Mock fallbacks were hiding issues.

2. **Check your setup:**
   - Is llama-server running?
   - Is the model file valid?
   - Is the port correct (8080)?

3. **Review logs:**
   - `logs/llm_communication_*.log` has all details
   - Console output shows real-time errors

4. **Fix root cause:**
   - Don't just add mock fallbacks back
   - Fix the actual LLM integration issue

## Success Metrics

**Before (with mock fallbacks):**
- Users saw generic actions
- No idea if LLM was working
- Failures hidden until development

**After (explicit errors):**
- Users know LLM status immediately
- Failures are debuggable
- Production readiness improved

---

**Implementation Status:** ✅ Complete  
**Testing Status:** ⏳ Pending  
**Impact:** High - No more silent failures
