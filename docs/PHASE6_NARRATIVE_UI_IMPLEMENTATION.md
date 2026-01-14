# Phase 6 Narrative COT UI Implementation

## Summary
Successfully implemented a comprehensive UI system that integrates the Phase 6 Chain-of-Thought narrative system into the Phase 1 Location Travel Mode. The implementation provides a scrollable main menu with clickable keyword links, a thinking skill selection popup, ASCII animations for LLM operations, and full keyboard/mouse input handling.

## Architecture Overview

### New Game Mode
- Added `GameMode.NarrativeCOT` to the existing game mode enum
- Transitions: `WorldView` → `Traveling` → `NarrativeCOT`
- Players enter NarrativeCOT mode when arriving at a forest location

### Core Components

#### 1. NarrativeUI (`src/game/narrative/NarrativeUI.cs`)
**Purpose:** Manages rendering of the narrative COT system to terminal views.

**Key Features:**
- Scrollable narration display with text wrapping
- Clickable keyword links with numbered indicators
- Thinking skill popup with hover effects
- ASCII art loading animations
- Skill check dice rolling animation
- State management for different UI phases

**API:**
```csharp
public void SetNarration(string text, List<string> keywords)
public void SetThinkingSkills(List<string> skills)
public void SetLoadingState(string message)
public void SetSkillCheckAnimation()
public void Scroll(int delta)
public void Render()
public int? GetKeywordIndexAt(int screenX, int screenY)
public int? GetThinkingSkillIndexAt(int popupX, int popupY)
```

**UI States:**
- `Narration` - Display observation with available keywords
- `SelectingThinkingSkill` - Show popup for skill selection
- `Loading` - Animated "Thinking..." indicator
- `SkillCheck` - Dice rolling animation

#### 2. NarrativeCOTController (`src/game/narrative/NarrativeCOTController.cs`)
**Purpose:** Controls the narrative COT game flow and coordinates UI updates.

**COT Phases:**
1. **Observation** - Generate and display narration with keywords
2. **Thinking** - Present relevant thinking skills for selection
3. **Action** - Execute selected action with chosen skill
4. **Outcome** - Display results and return to observation

**Key Methods:**
```csharp
public async Task InitializeAsync()
public async Task HandleKeywordSelectionAsync(int keywordIndex)
public async Task HandleThinkingSkillSelectionAsync(int skillIndex)
public void HandleScroll(int delta)
public void HandleEscape()
```

**Features:**
- Uses `NarrationNode.GenerateNeutralDescription()` for observations
- Determines relevant thinking skills based on keywords
- Finds outcomes matching selected keywords
- Applies outcome effects (items, mood changes, etc.)
- Async/await pattern for LLM operations

#### 3. NarrativeLocationIntegration (`src/game/narrative/NarrativeLocationIntegration.cs`)
**Purpose:** Integration bridge between Location Travel Mode and Narrative COT system.

**Responsibilities:**
- Lifecycle management (start, update, render, end)
- Event routing (clicks, hovers, scrolls, keyboard)
- Integration with existing `TerminalHUD` and `PopupTerminalHUD`
- Avatar location tracking

**API:**
```csharp
public void StartNarrative(NarrationNode startingNode, int locationId)
public void Update()
public void Render()
public void SelectKeyword(int index)
public void SelectThinkingSkill(int index)
public void Scroll(int delta)
public void HandleEscape()
public void HandleTerminalClick(int x, int y)
public void HandlePopupClick(int x, int y)
```

**Events:**
- `ReturnToWorldMap` - Signals when player exits narrative mode

### Integration Points

#### LocationTravelGameController Updates
**Modified Methods:**
- `Update()` - Routes to narrative integration in NarrativeCOT mode
- `OnTerminalCellClicked()` - Routes clicks to narrative system
- `OnTerminalCellHovered()` - Future keyword highlighting support
- `StartLocationInteraction()` - Switches to NarrativeCOT mode
- `StartBiomeInteraction()` - Switches to NarrativeCOT mode
- `EndLocationInteraction()` - Handles both modes

**Behavior Changes:**
- Arriving at any location now enters NarrativeCOT mode (if narrative integration available)
- Falls back to LocationInteraction mode if narrative integration unavailable
- ESC key properly handled in both modes

#### LocationTravelModeLauncher Updates
**Keyboard Input:**
- `ESC` - Routes to narrative integration or exits location
- `1-9` - Select keywords by number in narrative mode
- `D` - Dump debug info (existing)

**Mouse Input:**
- Click on terminal cells - Select keywords
- Click on popup cells - Select thinking skills
- Mouse wheel - Scroll narration up/down

**Removed:**
- Obsolete `NarrativeLocationIntegration.InitializeAsync()` call

## User Experience

### Entering a Location
1. Click on a vertex in world view
2. Travel animation plays
3. Arrive at location
4. Automatically enter NarrativeCOT mode
5. Loading animation: "Observing the surroundings..."
6. Narration appears with keyword list

### Selecting a Keyword
**Option 1: Click**
- Click directly on a keyword link in the terminal

**Option 2: Number Key**
- Press `1-9` to select corresponding keyword

### Thinking Phase
1. Loading animation: "Contemplating possibilities..."
2. Popup appears with thinking skills:
   - Careful Analysis
   - Quick Intuition
   - Creative Thinking
   - Logical Deduction
   - Emotional Intelligence
3. Hover over skills for visual feedback
4. Click to select or press `ESC` to cancel

### Action Phase
1. Loading animation: "Performing action..."
2. Dice rolling animation
3. Action result displayed
4. Automatically returns to observation after 2 seconds

### Navigation
- **Scroll**: Mouse wheel or arrow keys
- **Exit**: ESC to return to world view
- **Cancel**: ESC during skill selection returns to observation

## Visual Design

### Main Terminal (100x30)
```
=== NARRATION ===

[Neutral description text wraps naturally across
multiple lines with proper word boundaries...]

Available Keywords:
  [1] stream
  [2] clearing
  [3] berries

---
Instructions: Click keyword or press number | Scroll: Mouse wheel | ESC: Exit
```

### Popup Terminal (Thinking Skills)
```
╔═══════════════════════════════════════╗
║  Select Thinking Skill                ║
╠═══════════════════════════════════════╣
║  > 1. Careful Analysis                ║
║    2. Quick Intuition                 ║
║    3. Creative Thinking               ║
║    4. Logical Deduction               ║
║    5. Emotional Intelligence          ║
╚═══════════════════════════════════════╝
```

### Loading Animation
```
  ╔══════════════╗
  ║   THINKING   ║
  ╚══════════════╝

⠋ Observing the surroundings... ⠋
```

## Color Scheme

### Narration Display
- Title: Cyan `(0, 255, 200)`
- Body text: White `(255, 255, 255)`
- Keywords heading: Yellow-ish `(200, 200, 100)`
- Keyword normal: Cyan `(150, 200, 255)`
- Keyword selected: Bright yellow `(255, 255, 100)`
- Instructions: Gray `(150, 150, 150)`

### Popup
- Border: White
- Title: Yellow `(255, 255, 100)`
- Skill normal: Light gray `(200, 200, 200)`
- Skill hovered: Yellow `(255, 255, 100)`

### Animations
- Loading spinner: Light blue `(100, 200, 255)`
- Loading box: Purple-gray `(150, 150, 200)`
- Skill check: Orange-yellow `(255, 200, 100)`

## Technical Details

### Terminal API Usage
The implementation uses the `TerminalView` API correctly:
- `Text(x, y, string, Vector4 textColor, Vector4 backgroundColor)` for rendering text
- `DrawBox(x, y, width, height, BoxStyle, textColor, backgroundColor)` for borders
- `Clear()` to reset terminal state
- `Fill()` for background fills

### Scroll System
- Scroll offset tracked in lines
- Max scroll calculated from content height
- Mouse wheel delta multiplied by 3 for reasonable speed
- Clamped to prevent scrolling beyond content

### Click Detection
Keywords store their rendered position:
```csharp
public class KeywordLink
{
    public string Text { get; set; }
    public int StartLine { get; set; }
    public int StartCol { get; set; }
    public int EndCol { get; set; }
}
```

Clicks are converted from screen coordinates to terminal coordinates accounting for scroll.

### Animation System
- 8-frame spinner animation (⠋⠙⠹⠸⠼⠴⠦⠧)
- 6-frame dice animation (⚀⚁⚂⚃⚄⚅)
- Updates every 100ms
- Frame counter wraps using modulo

## Future Enhancements

### Planned Features
1. **LLM Integration**
   - Use `NarrativeObservationExecutor` for richer observations
   - Use `NarrativeThinkingExecutor` for context-aware skill suggestions
   - Use `NarrativeActionExecutor` for dynamic action outcomes

2. **Skill System**
   - Load from `data/skills.csv`
   - Filter by skill categories
   - Track skill proficiency
   - Show skill descriptions in popup

3. **Transition Support**
   - Navigate between nodes (clearing → stream)
   - Update observation when transitioning
   - Maintain breadcrumb trail

4. **Enhanced UI**
   - Keyword highlighting on hover
   - Smooth scroll animation
   - Fade transitions between phases
   - Achievement notifications

5. **Inventory System**
   - `Avatar.AddItem()` implementation
   - Display acquired items
   - Use items as keywords

### Known Limitations
1. **Simplified Outcomes**
   - Currently uses outcome's `ToNaturalLanguageString()`
   - No LLM-generated action descriptions yet

2. **Fixed Thinking Skills**
   - Hardcoded list of 5 generic skills
   - Should query from skills CSV

3. **No Node Transitions**
   - Child nodes not yet supported
   - Transitions to other narration nodes not implemented

4. **No Inventory**
   - Items can be found but not stored
   - `Avatar.AddItem()` doesn't exist yet

## Testing Instructions

1. **Launch Game**
   ```
   dotnet run
   ```

2. **Select Option 6**
   - "6. Test Location Travel Mode"

3. **Navigate to Forest**
   - Click on any vertex in the GlyphSphere
   - Avatar will travel there

4. **Test Narrative Mode**
   - Observe narration appears
   - Try clicking on a keyword
   - Try pressing number keys (1-3)
   - Scroll with mouse wheel
   - Select a thinking skill
   - Watch action animation
   - Press ESC to exit

5. **Verify Features**
   - [ ] Narration displays correctly
   - [ ] Keywords are clickable
   - [ ] Number keys select keywords
   - [ ] Thinking popup appears
   - [ ] Skills are hoverable/clickable
   - [ ] Scroll works with mouse wheel
   - [ ] Loading animations play
   - [ ] ESC cancels/exits appropriately
   - [ ] Returns to world view after ESC

## File Summary

### New Files
1. `src/game/narrative/NarrativeUI.cs` (376 lines)
   - UI rendering and state management

2. `src/game/narrative/NarrativeCOTController.cs` (258 lines)
   - COT flow control and game logic

3. `src/game/narrative/NarrativeLocationIntegration.cs` (177 lines)
   - Integration bridge with existing systems

### Modified Files
1. `src/game/GameMode.cs`
   - Added `NarrativeCOT` enum value

2. `src/game/LocationTravelGameController.cs`
   - Updated Update() for NarrativeCOT mode
   - Updated input handlers
   - Updated mode transitions
   - Updated EndLocationInteraction()

3. `src/game/LocationTravelModeLauncher.cs`
   - Added keyboard input routing (ESC, 1-9)
   - Added mouse wheel handler
   - Removed obsolete InitializeAsync() call

### Build Status
✅ **Build Succeeded** - 0 Errors, 9 Warnings (pre-existing)

## Conclusion
The narrative COT system is now fully integrated into the location travel mode with a complete, functional UI. Players can experience the Observation → Thinking → Action → Outcome loop with smooth interactions, clear visuals, and intuitive controls. The system is ready for testing via option 6 in the main menu.
