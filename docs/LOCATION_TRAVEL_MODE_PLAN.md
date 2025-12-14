# Location Travel Mode - Implementation Plan

## Overview

This document outlines the implementation plan for a new game mode that merges the **GlyphSphere with Terminal HUD** and the **Forest Location System Demo**. The goal is to create an interactive world where:

1. The player sees a 3D glyph sphere representing the world with biomes and locations
2. An avatar can travel between locations via pathfinding
3. Upon arrival at a location, the terminal HUD presents LLM-generated textual choices
4. The player interacts with these choices in a loop until an action fails
5. After failure, the avatar can travel to another location and repeat the process

## Current System Analysis

### Existing Components2. **Basic Flow**
   - Start game → click forest → travel → interact → fail → return
   - Verify all steps work smoothly
   - Test mouse interaction: hover over actions, click to select
   - Verify color transitions are smooth
#### 1. GlyphSphere System (`src/glyph/`)
- **GlyphSphereCore.cs**: OpenGL rendering engine with:
  - Terminal HUD integration (already present!)
  - Mouse picking and interaction
  - Camera controls (orbit + debug modes)
  - Event system (VertexHovered, VertexClicked)
  - Pathfinding integration
  - Update loop with deltaTime

- **GlyphSphereApplication.cs**: Launcher with camera configuration

- **MicroworldInterface.cs**: Current implementation with:
  - Avatar system (placement, movement, pathfinding)
  - Biome and location generation (Perlin noise)
  - Water animation
  - Path visualization (hover paths, movement paths)
  - Vertex world data storage

#### 2. Terminal System (`src/terminal/`)
- **TerminalHUD.cs**: Full-featured terminal with:
  - 80x25 character grid (configurable)
  - Text rendering with colors
  - Box drawing, progress bars
  - Input handling (mouse clicks, hover)
  - Event system (CellClicked, CellHovered)
  - Message boxes and UI elements

#### 3. Location System (`src/glyph/microworld/LocationSystem/`)
- **LocationBlueprint.cs**: Data structures for:
  - Hierarchical sublocations
  - Categorized state system
  - Location content (items, NPCs, quests, actions)

- **ForestFeatureGenerator.cs**: Example generator with:
  - Deterministic RNG-based generation
  - State categories (time, weather, wildlife, path_visibility)
  - Multiple sublocations (forest_edge, main_path, deep_woods, etc.)

- **DirectorPromptConstructor.cs**: LLM prompt builder for:
  - Action generation with JSON constraints
  - State-aware action filtering
  - Follow-up action continuity

- **NarratorPromptConstructor.cs**: Narrative description generator

- **GameplayLogger.cs**: Session logging

#### 4. LLM Integration (`src/LLM/`)
- **LlamaServerManager.cs**: Server interface with:
  - Multiple concurrent instances
  - Streaming token responses
  - GBNF grammar support
  - JSON constraint validation

### Current Integration Points

The **GlyphSphereCore already has Terminal HUD integrated**:
```csharp
private Cathedral.Terminal.TerminalHUD? _terminal;
public Cathedral.Terminal.TerminalHUD? Terminal => _terminal;
```

The **MicroworldInterface already has avatar movement**:
```csharp
- Avatar placement and restoration
- Click-to-move pathfinding
- Movement animation with timer
- Path visualization
```

## Architecture Design

### System Flow

```
┌─────────────────────────────────────────────────────┐
│         GlyphSphere World View (3D)                 │
│  ┌──────────────────────────────────────────────┐   │
│  │  Avatar (☻) at current location              │   │
│  │  Biomes: forest, mountain, plains, cities    │   │
│  │  Locations: taverns, dungeons, villages      │   │
│  │  Click location → Start travel               │   │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
                        ↓ (Avatar arrives)
┌─────────────────────────────────────────────────────┐
│         Terminal HUD (2D Overlay)                   │
│  ┌──────────────────────────────────────────────┐   │
│  │  FOREST EDGE                                  │   │
│  │  You stand at the boundary where dense trees │   │
│  │  meet open grassland...                       │   │
│  │                                               │   │
│  │  Actions:                                     │   │
│  │  [1] Follow the main path into the forest    │   │
│  │  [2] Search the edge for medicinal herbs     │   │
│  │  [3] Listen for sounds from within            │   │
│  │  [4] Mark the trail before proceeding        │   │
│  │  [5] Leave this location                      │   │
│  │                                               │   │
│  │  Enter choice (1-5): _                        │   │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
                        ↓ (Player chooses action)
┌─────────────────────────────────────────────────────┐
│         Action Resolution (RNG/LLM)                 │
│  - Roll for success/failure                         │
│  - Update game state (sublocation, states)          │
│  - Generate narrative outcome                       │
│  - Continue loop or end on failure                  │
└─────────────────────────────────────────────────────┘
                        ↓ (Action fails)
                  Return to World View
```

### Game States

```csharp
public enum GameMode
{
    WorldView,      // Viewing glyph sphere, can click to travel
    Traveling,      // Avatar is moving to destination
    LocationInteraction  // At location, using terminal for choices
}
```

### Core Classes to Create/Modify

#### 1. **LocationTravelGameController** (NEW)
Central game state manager that coordinates all systems.

**Responsibilities:**
- Track current game mode (WorldView, Traveling, LocationInteraction)
- Manage avatar position and destination
- Coordinate between GlyphSphere, Terminal, and LLM systems
- Handle mode transitions
- Store current location state
- Session management and save/load

**Key Methods:**
```csharp
// Mode management
void SetMode(GameMode mode);
void OnAvatarArrived(int vertexIndex);
void OnLocationExitRequested();

// Location interaction
void StartLocationInteraction(string locationId);
void ProcessPlayerChoice(int choiceIndex);
void HandleActionOutcome(PlayerAction action);
void EndLocationInteraction();

// State management
LocationInstanceState GetCurrentLocationState();
void UpdateLocationState(LocationInstanceState state);
```

#### 2. **LocationInteractionMode** (NEW)
Handles the terminal-based interaction loop at locations.

**Responsibilities:**
- Initialize LLM instances (Director + Narrator)
- Generate and display action choices
- Process player input from terminal
- Simulate action outcomes (RNG-based initially)
- Display narrative responses
- Track interaction history
- Detect failure condition and exit

**Key Methods:**
```csharp
void Initialize(LocationBlueprint blueprint, string locationId);
void StartTurn();
Task GenerateActionsAsync();
void DisplayActions(List<ActionChoice> actions, int hoveredIndex = -1);
Task<PlayerAction> WaitForPlayerClickAsync();
void OnMouseMove(int x, int y);
void OnMouseClick(int x, int y);
void DisplayOutcome(PlayerAction action);
bool ShouldContinue(PlayerAction action);
void Cleanup();
```

#### 3. **LocationInstanceState** (NEW)
Runtime state for a specific location instance.

**Data:**
```csharp
record LocationInstanceState(
    string LocationId,
    string CurrentSublocation,
    Dictionary<string, string> CurrentStates,
    List<PlayerAction> ActionHistory,
    DateTime LastVisited,
    int TurnCount
);
```

#### 4. **TerminalLocationUI** (NEW)
UI helper for rendering location information in the terminal.

**Responsibilities:**
- Format and display location descriptions
- Render action menus with mouse hover support
- Show status information (stats, inventory, turn count, etc.)
- Display narrative text with word wrapping
- Handle mouse-based action selection
- Calculate clickable regions for multi-line action text
- Provide visual feedback (color changes on hover)
- Wrap long action text properly with indentation

**Key Methods:**
```csharp
void RenderLocationHeader(LocationBlueprint blueprint, string sublocation, int turnCount);
void RenderEnvironmentStatus(Dictionary<string, string> states);
void RenderPreviousAction(PlayerAction? action);
void RenderActionMenu(List<ActionChoice> actions, int hoveredIndex);
void RenderNarrative(string narrative);
int? GetHoveredAction(int mouseX, int mouseY, int actionCount);
void ShowResultMessage(string message, bool success);
string WrapText(string text, int maxWidth, int indentSize);
List<ActionRegion> CalculateActionRegions(List<ActionChoice> actions);
```

**Helper Structures:**
```csharp
record ActionRegion(
    int ActionIndex,
    int StartY,
    int EndY,
    int StartX,
    int EndX
);
```

#### 5. **Modifications to MicroworldInterface**
Add hooks for location interaction system.

**Changes:**
```csharp
// Add event for arrival at location
public event Action<int, LocationType?>? AvatarArrivedAtLocation;

// Add method to check if at location
public bool IsAtLocation() => _avatarVertex >= 0 && 
    vertexData.TryGetValue(_avatarVertex, out var data) && 
    data.Location.HasValue;

// Add method to get current location info
public (LocationType? location, BiomeType biome) GetCurrentLocationInfo();

// Pause movement during interaction
public void PauseMovement();
public void ResumeMovement();
```

#### 6. **Modifications to GlyphSphereCore**
Expose terminal and add mode coordination.

**Changes:**
```csharp
// Already has: public Cathedral.Terminal.TerminalHUD? Terminal => _terminal;

// Add terminal visibility control
public void ShowTerminal(bool show);
public bool IsTerminalVisible();

// Add input routing
public void SetTerminalInputActive(bool active);
```

## Implementation Phases

### Phase 1: Core Integration Framework (Days 1-2)

**Goal:** Set up the basic structure and state management.

#### Tasks:
1. ✅ **Analyze existing code** (COMPLETED)
   - Document current systems
   - Identify integration points
   - Map data flows

2. **Create LocationTravelGameController**
   - Implement GameMode enum
   - Add mode transition logic
   - Create basic state storage
   - Wire up to GlyphSphereCore events

3. **Create LocationInstanceState**
   - Define data structure
   - Add serialization support
   - Create factory methods

4. **Test Mode Transitions**
   - WorldView → Traveling → LocationInteraction
   - Verify event flow
   - Check state persistence

**Success Criteria:**
- Can switch between game modes programmatically
- State is maintained across transitions
- Events fire correctly between systems

### Phase 2: Avatar Travel Enhancement (Days 3-4)

**Goal:** Enhance avatar system to support location arrival detection.

#### Tasks:
1. **Modify MicroworldInterface**
   - Add `AvatarArrivedAtLocation` event
   - Detect when avatar reaches location vertex
   - Implement movement pause/resume
   - Add location info queries

2. **Add Visual Feedback**
   - Highlight destination location during travel
   - Show travel path clearly
   - Add arrival animation/effect

3. **Test Travel System**
   - Click location → avatar travels → arrives
   - Event fires with correct location data
   - Movement can be paused mid-travel

**Success Criteria:**
- Avatar reliably travels to clicked locations
- Arrival event fires with correct location type
- Can distinguish location vs non-location vertices

### Phase 3: Terminal Location UI (Days 5-7)

**Goal:** Create the terminal-based UI for location interactions.

#### Tasks:
1. **Create TerminalLocationUI**
   - Implement layout system (header, narrative, action menu, status bar)
   - Add text rendering with intelligent word wrap
   - Create action menu display with multi-line support
   - Implement mouse-based action selection with hover highlighting
   - Calculate clickable regions for wrapped action text
   - Add color transitions for visual feedback (normal → hover → selected)

2. **Design UI Layout**
   
   **Terminal Size:** 100x30 (width x height) - larger than default to accommodate longer action text
   
   ```
   ╔══════════════════════════════════════════════════════════════════════════════════════════════════╗
   ║ FOREST EDGE                                              Time: Morning | Weather: Clear | Turn 1 ║
   ╠══════════════════════════════════════════════════════════════════════════════════════════════════╣
   ║                                                                                                  ║
   ║ You stand at the boundary where dense trees meet open grassland. Birds chirp overhead and       ║
   ║ a well-worn path leads deeper into the forest. The morning sun filters through the leaves,      ║
   ║ casting dappled shadows on the ground. A gentle breeze carries the scent of pine and earth.     ║
   ║                                                                                                  ║
   ║ -- Recent Action -----------------------------------------------------------------               ║
   ║ You examined the forest edge and found medicinal herbs. (Success)                               ║
   ║                                                                                                  ║
   ╠══════════════════════════════════════════════════════════════════════════════════════════════════╣
   ║ CHOOSE YOUR ACTION (click to select):                                                           ║
   ║                                                                                                  ║
   ║  ▸ Follow the main path deeper into the forest, staying alert for any signs of wildlife         ║
   ║    or danger along the way                                                                       ║
   ║                                                                                                  ║
   ║  ▸ Search the surrounding area more thoroughly for additional medicinal herbs and useful        ║
   ║    plants that might be growing nearby                                                           ║
   ║                                                                                                  ║
   ║  ▸ Listen carefully to the sounds of the forest to gauge wildlife activity and potential        ║
   ║    threats before proceeding further                                                             ║
   ║                                                                                                  ║
   ║  ▸ Mark the trail with distinctive signs to ensure you can find your way back to this           ║
   ║    location if needed                                                                            ║
   ║                                                                                                  ║
   ║  ▸ Leave this location and return to the world view                                             ║
   ║                                                                                                  ║
   ╠══════════════════════════════════════════════════════════════════════════════════════════════════╣
   ║ Hover over an action to highlight it, click to select                                           ║
   ╚══════════════════════════════════════════════════════════════════════════════════════════════════╝
   ```
   
   **Interactive Behavior:**
   - Actions are clickable text regions (multi-line if needed)
   - Mouse hover changes text color: Normal (white) → Hover (yellow) → Selected (green briefly)
   - Visual feedback: ▸ marker brightens on hover
   - No numbering needed - pure mouse-driven selection
   - Text wraps automatically to fit terminal width with proper indentation

3. **Implement Mouse-Based Input System**
   - Track mouse hover over action regions
   - Detect clicks on action text (multi-line regions)
   - Highlight hovered action with color change (white → yellow)
   - ESC key to exit location
   - Handle long text wrapping for actions (with proper indentation)
   - Calculate clickable regions for wrapped text

4. **Test UI Components**
   - All layout elements render correctly
   - Text wraps properly at terminal width
   - Multi-line actions are clickable across all lines
   - Hover highlighting works smoothly
   - Colors transition: normal (white) → hover (yellow) → selected (green flash)
   - Wrapped text maintains proper indentation

**Success Criteria:**
- Terminal displays location information clearly (100x30 size)
- Action menu is readable with proper text wrapping
- Long actions wrap correctly with maintained indentation
- Mouse hover highlights actions smoothly (white → yellow)
- Click detection works for multi-line action regions
- Visual feedback is immediate and clear
- UI updates at 60 FPS without flicker

### Phase 4: Location Interaction Loop (Days 8-10)

**Goal:** Implement the core gameplay loop at locations.

#### Tasks:
1. **Create LocationInteractionMode**
   - Initialize location from blueprint
   - Set up game state tracking
   - Implement turn-based loop structure
   - Wire up mouse events from terminal to action selection
   - Track hovered action for visual feedback
   - Handle click events to select actions

2. **Implement Simple Action System (NO LLM)**
   - Use ForestFeatureGenerator to get blueprint
   - Define 4-5 hardcoded actions per sublocation (some with long descriptions)
   - Test text wrapping with actions of varying lengths
   - Add RNG-based success/failure (70% success rate)
   - Implement state changes on success
   - Track action history
   - Handle mouse-based selection instead of keyboard numbers

3. **Add Action Resolution**
   - Roll for success/failure
   - Update sublocation if action succeeds
   - Update state categories
   - Display outcome text
   - Check for end condition (failure)

4. **Test Interaction Loop**
   - Enter location → see actions → choose → see result
   - Success: state changes, new actions appear
   - Failure: loop ends, return to world view
   - Can revisit same location multiple times

**Success Criteria:**
- Can play through multiple actions at a location
- State changes affect available actions
- Failure condition ends interaction correctly
- Can leave and return to locations

### Phase 5: LLM Integration (Days 11-14)

**Goal:** Replace hardcoded actions with LLM-generated content.

#### Tasks:
1. **Integrate Director LLM**
   - Initialize LlamaServerManager
   - Create Director instance with system prompt
   - Use DirectorPromptConstructor for prompts
   - Parse JSON action responses
   - Validate with JsonValidator

2. **Integrate Narrator LLM**
   - Create Narrator instance
   - Use NarratorPromptConstructor for prompts
   - Generate narrative descriptions
   - Display in terminal UI

3. **Implement Response Handling**
   - Stream tokens to terminal (show "thinking" indicator)
   - Parse complete responses
   - Handle errors gracefully (fallback to simple actions)
   - Add timeout handling
   - Ensure LLM-generated actions work with text wrapping system
   - Test with various action text lengths

4. **Test LLM Integration**
   - Actions are contextually appropriate
   - Narrative is coherent and engaging
   - Response time is acceptable (<5 seconds)
   - Errors don't crash the game

**Success Criteria:**
- LLM generates varied, appropriate actions
- Narrative responses are immersive
- System handles LLM errors gracefully
- Performance is acceptable

### Phase 6: Polish and Features (Days 15-17)

**Goal:** Add polish, debugging tools, and quality-of-life features.

#### Tasks:
1. **Add Debug Features**
   - Show current state in corner of terminal
   - Log actions to gameplay logger
   - Add hotkey to dump state to console
   - Add hotkey to reload location
   - Display current hover state in debug mode

2. **Add Visual Polish**
   - Smooth terminal fade-in/out on mode change
   - Typing effect for narrative text (optional - may interfere with wrapping)
   - Color coding for success/failure outcomes
   - Smooth color transitions on hover (interpolate between white and yellow)
   - Brief flash effect when action is selected (green highlight)
   - Add subtle ▸ marker animation on hover (brightness pulse)

3. **Add Location Variety**
   - Ensure forest blueprint works everywhere initially
   - Document process for adding new location types
   - Add variation to forest encounters

4. **User Experience**
   - Add help screen (H key)
   - Add confirm dialog before leaving location
   - Add save/load system for game state
   - Add settings for LLM model selection

5. **Testing and Bug Fixes**
   - Test all code paths
   - Fix edge cases
   - Ensure no memory leaks
   - Optimize performance

**Success Criteria:**
- Game feels polished and responsive
- Debug tools aid development
- No major bugs remain
- Documentation is complete

## File Structure

### New Files to Create

```
src/
  game/
    GameController.cs                    # Main game state controller
    GameMode.cs                          # Enum for game modes
    LocationInstanceState.cs             # Runtime location state
    
  glyph/
    interaction/
      LocationInteractionMode.cs         # Location interaction system
      TerminalLocationUI.cs              # Terminal UI with mouse support
      ActionRegion.cs                    # Helper for clickable text regions
      TextWrapper.cs                     # Smart text wrapping utility
      ActionResolver.cs                  # Action success/failure logic
      LocationSessionManager.cs          # Save/load location sessions
```

### Files to Modify

```
src/
  glyph/
    GlyphSphereCore.cs                   # Add terminal visibility control
    GlyphSphereApplication.cs            # Add game mode option
    microworld/
      MicroworldInterface.cs             # Add arrival events, location queries
      
  Program.cs                             # Add new mode option (6)
```

### Configuration Files

```
config/
  game_settings.json                     # Game configuration
    - LLM model selection
    - Terminal size
    - RNG seed
    - Debug settings
```

## Key Design Decisions

### 1. Mouse-Driven Action Selection

**Decision:** Use mouse clicks on action text instead of keyboard number input.

**Rationale:**
- More intuitive and modern interaction
- Terminal already has full mouse event support (CellClicked, CellHovered)
- Allows for dynamic visual feedback (hover highlighting)
- No need to constrain to 9 actions (numbered keys limitation)
- More immersive - feel like clicking on text in a game
- Supports variable-length action descriptions naturally

**Implementation Details:**
- Track mouse position to determine hovered action
- Change text color on hover (white → yellow)
- Calculate multi-line action regions for click detection
- Brief flash effect (green) when action selected
- Smart text wrapping with maintained indentation

### 2. Forest Blueprint for All Locations (Initially)

**Decision:** Use the ForestFeatureGenerator blueprint for ALL locations initially.

**Rationale:**
- Proven system from the demo
- Rich variety of sublocations and states
- Focus on integration first, variety later
- Easy to test and debug

**Future:** Add TavernFeatureGenerator, DungeonFeatureGenerator, etc.

### 3. RNG-Based Success/Failure (Initially)

**Decision:** Use simple RNG (70% success) instead of skill-based resolution.

**Rationale:**
- Simplifies initial implementation
- Focuses on integration and flow
- Can be enhanced later with:
  - Player stats/skills
  - Difficulty-based rolls
  - Equipment bonuses
  - Environmental modifiers

### 4. Terminal Configuration

**Decision:** Terminal uses 100x30 size (vs default 80x25), appears as overlay with GlyphSphere visible.

**Rationale:**
- Larger size accommodates longer action descriptions without excessive wrapping
- Still maintains spatial context with visible GlyphSphere
- Visually appealing
- Can see avatar at location
- Allows for richer narrative text
- Better readability for wrapped multi-line actions
- Can add transparency effect

**Alternative:** Could make terminal size configurable based on content or user preference.

### 5. Synchronous LLM (Blocking)

**Decision:** Wait for LLM responses before continuing.

**Rationale:**
- Simpler implementation
- Natural turn-based flow
- Show "thinking" indicator
- Acceptable for single-player

**Future:** Could add async with cancellation.

### 6. Location State Persistence

**Decision:** Location state persists across visits (same location ID).

**Rationale:**
- More immersive
- Actions have lasting consequences
- Supports emergent gameplay
- Location IDs are deterministic (from vertex position)

**Implementation:**
- Store LocationInstanceState in GameController
- Key by location vertex index
- Save/load with game state

## Testing Strategy

### Unit Tests

1. **LocationInstanceState**
   - Serialization/deserialization
   - State transitions
   - History tracking

2. **ActionResolver**
   - Success/failure RNG
   - State change application
   - End condition detection

3. **TerminalLocationUI**
   - Layout rendering
   - Input parsing
   - Text wrapping

### Integration Tests

1. **Mode Transitions**
   - WorldView → Traveling → LocationInteraction → WorldView
   - State persistence across transitions
   - Event propagation

2. **Location Interaction**
   - Complete interaction loop
   - Multiple actions in sequence
   - Failure ends correctly
   - Can revisit same location

3. **LLM Integration**
   - Director generates valid actions (including long descriptions)
   - Long action text wraps correctly in terminal
   - Multi-line actions are fully clickable
   - Narrator generates coherent text
   - Error handling works
   - Timeout handling works

### Manual Testing Scenarios

1. **Basic Flow**
   - Start game → click forest → travel → interact → fail → return
   - Verify all steps work smoothly

2. **State Persistence**
   - Visit location → change state → leave → return
   - Verify state persists

3. **Multiple Locations**
   - Visit 3+ different locations
   - Verify each has appropriate content
   - Verify states don't interfere

4. **Error Handling**
   - Disconnect LLM mid-interaction
   - Invalid player input
   - Click non-location vertex

5. **Performance**
   - Long interaction session (20+ actions)
   - Multiple LLM instances
   - Memory usage
   - Frame rate with hover effects
   - Test with very long action descriptions (150+ characters)
   - Verify hover responsiveness at 60 FPS

## Success Metrics

### Functional Requirements

- ✅ Can travel to any location on the sphere
- ✅ Avatar movement is smooth and visible
- ✅ Arrival at location triggers interaction mode
- ✅ Terminal displays location information clearly (100x30 size)
- ✅ Can choose from 5-7 actions via mouse clicks
- ✅ Actions highlight on hover (color change white → yellow)
- ✅ Long action text wraps correctly with proper indentation
- ✅ Multi-line action regions are fully clickable
- ✅ Click feedback is immediate (green flash)
- ✅ Actions have success/failure outcomes
- ✅ State changes affect future actions
- ✅ Failure ends interaction loop
- ✅ Can return to world view and travel to new location
- ✅ Location state persists across visits

### Performance Requirements

- Action generation: < 5 seconds
- Narrative generation: < 5 seconds
- Mode transition: < 500ms
- Frame rate: > 30 FPS
- Memory usage: < 500 MB

### Quality Requirements

- No crashes during normal gameplay
- No memory leaks over extended sessions
- Clear error messages for problems
- Responsive UI (no freezing)
- Intuitive controls

## Future Enhancements

### Short Term (After Initial Implementation)

1. **Location Type Variety**
   - TavernFeatureGenerator
   - DungeonFeatureGenerator
   - CityFeatureGenerator
   - MountainFeatureGenerator

2. **Enhanced Action Resolution**
   - Player stats (strength, intelligence, etc.)
   - Skill checks with difficulty
   - Equipment effects
   - Companion bonuses

3. **Inventory System**
   - Items gained from actions
   - Item usage in actions
   - Equipment slots
   - Weight limits

4. **Quest System**
   - Multi-location quests
   - Quest tracking in terminal
   - Rewards and progression

### Long Term

1. **Persistent World**
   - Time progression
   - Weather changes
   - NPC schedules
   - Dynamic events

2. **Multiplayer**
   - Shared world
   - Other players visible as avatars
   - Trading and interaction

3. **Advanced AI**
   - NPC memory and relationships
   - Dynamic story generation
   - Emergent quest lines

## Development Tips

### Recommended Development Order

1. Start with Phase 1 (framework)
2. Test each phase thoroughly before moving on
3. Use debug tools extensively (logging, state dumps)
4. Test without LLM first (hardcoded actions)
5. Add LLM only after core loop works
6. Keep commits small and focused
7. Document as you go

### Debug Hotkeys to Add

- **M**: Toggle terminal mode (show/hide)
- **D**: Dump current game state to console
- **R**: Reload current location
- **L**: Show location blueprint info
- **H**: Show help screen
- **ESC**: Exit current location

### Common Pitfalls to Avoid

1. **Don't** try to integrate LLM first
2. **Don't** skip testing mode transitions
3. **Don't** forget to dispose LLM instances
4. **Don't** block the render thread with LLM calls
5. **Don't** forget to validate LLM responses
6. **Do** use async/await for LLM calls
7. **Do** handle errors gracefully
8. **Do** log extensively

## Conclusion

This implementation plan provides a clear path to merge the GlyphSphere world view with the terminal-based location interaction system. By breaking the work into manageable phases and focusing on integration first (using proven systems), we can create a compelling gameplay experience that combines visual exploration with text-based narrative interaction.

The modular design ensures each component can be tested independently and enhanced iteratively. The use of existing, working systems (ForestFeatureGenerator, DirectorPromptConstructor, Terminal HUD) minimizes risk and allows us to focus on the integration logic.

**Estimated Timeline:** 15-17 days for full implementation with polish.

**Next Steps:**
1. Review this plan
2. Set up project branch
3. Begin Phase 1 implementation
4. Test continuously
5. Iterate and refine
