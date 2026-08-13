# Text Truncation Feature

## Overview
This feature handles LLM-generated text that may be truncated due to max token limits. When displaying text in the UI, the system automatically cleans up incomplete sentences or words and appends "..." to indicate truncation.

## Implementation

### Core Utility: `TextTruncationUtils`
Located at: `src/game/narrative/TextTruncationUtils.cs`

The `CleanTruncatedText` method processes potentially truncated text:

1. **If text ends with proper punctuation (`.`, `!`, `?`)**: Returns text unchanged (it's complete)

2. **If text has incomplete sentences**:
   - Finds the last complete sentence
   - If the text before that sentence is ≥ 50 characters, removes the incomplete sentence
   - Replaces the final period with "..."
   - Example: `"The forest is dark. You see something str"` → `"The forest is dark..."`

3. **If no period found OR text would be too short after removal**:
   - Removes the last incomplete word instead
   - Replaces the final space with "..."
   - Example: `"You see a bi"` → `"You see a..."`

4. **Edge case (single word or no spaces)**:
   - Simply appends "..."
   - Example: `"Hello"` → `"Hello..."`

### Integration: `NarrationScrollBuffer`
Located at: `src/game/NarrationScrollBuffer.cs`

The `AddBlock` method now automatically applies text truncation cleanup:

```csharp
public void AddBlock(NarrationBlock block)
{
    // Clean potentially truncated text before storing
    string cleanedText = TextTruncationUtils.CleanTruncatedText(block.Text);
    
    // Create a new block with cleaned text if it was modified
    var blockToAdd = cleanedText != block.Text 
        ? block with { Text = cleanedText }
        : block;
    
    _blocks.Add(blockToAdd);
    RegenerateRenderedLines();
}
```

All narration blocks (Observation, Thinking, Action, Outcome) are automatically processed when added to the scroll buffer.

## Testing

A demo program is available at `src/game/narrative/TextTruncationDemo.cs` to test various scenarios:
- Complete text (no changes)
- Incomplete sentences
- Short text with incomplete words
- Text with different punctuation marks (!, ?)
- Edge cases

To run the demo, call `TextTruncationDemo.RunDemo()` from your code.

## Configuration

The minimum text length for sentence removal can be configured:

```csharp
// Default: 50 characters
string result = TextTruncationUtils.CleanTruncatedText(text);

// Custom minimum: 100 characters
string result = TextTruncationUtils.CleanTruncatedText(text, minLengthForSentence: 100);
```

## Notes

- This feature is transparent to the rest of the codebase
- No changes needed to LLM prompts or narration generators
- Works automatically for all narration block types
- Preserves the original text if it's already complete
