# Cathedral Project - AI Agent Guidelines

## Project Overview

Cathedral is a narrative RPG with Chain-of-Thought (CoT) reasoning, featuring:
- **OpenTK-based terminal UI** overlaying 3D glyph spheres (like roguelike ASCII but in OpenGL)
- **Local LLM integration** via llama.cpp server for narrative generation
- **Skill-based narrative personas** inspired by Disco Elysium
- **JSON constraint system** for structured LLM outputs using GBNF grammars

## Architecture & Key Components

### Game Modes & Flow
```csharp
GameMode.WorldView → GameMode.Traveling → GameMode.NarrativeCOT
```
- **WorldView**: Click vertices on 3D glyph sphere to travel
- **Traveling**: Avatar pathfinding animation
- **NarrativeCOT**: Observation → Thinking → Action phases with LLM-generated content

### Terminal System (Core UI)
- **TerminalHUD**: Main 100x30 character display with OpenGL instanced rendering
- **PopupTerminalHUD**: 40x40 mouse-following overlay for menus
- All UI uses **Vector4 colors** from `Config.Colors` - never OpenTK.Color4
- Text rendering via **glyph atlas system**, not immediate mode
- **Always use absolute file paths** when dealing with terminal file operations

### LLM Integration Architecture
```
LlamaServerManager → LlamaInstance (slotId) → HttpClient (localhost:8080)
```
- **llama.cpp server** runs locally with GBNF grammar constraints
- **JsonConstraintGenerator** creates GBNF rules from schema definitions
- **Model aliases**: "tiny" (qwen2-0_5b), "medium" (phi-4-Q4_1)
- **Logging**: All LLM requests/responses go to `logs/llm_session_*/`

### Data & Configuration
- **Config.cs**: Centralized colors, dimensions, messages - modify here, not hardcode values
- **data/**: JSON item definitions, CSV skills - use existing schemas
- **DEPRECATED/**: Never reference files here, they're excluded from build

## Development Patterns

### LLM Request Pattern
```csharp
// Always use async and proper slot management
var slotId = await llamaServer.AcquireSlotAsync();
var result = await llamaServer.GenerateWithConstraintsAsync(slotId, prompt, gbnfGrammar);
await llamaServer.ReleaseSlotAsync(slotId);
```

### Terminal UI Updates
```csharp
// Use TerminalView, not direct OpenGL calls
terminal.Clear();
terminal.Text(x, y, "content", Config.Colors.White, Config.Colors.Black);
terminal.DrawBox(x, y, w, h, BoxStyle.Single, textColor, bgColor);
```

### Error Handling Philosophy
- **No mock fallbacks** - LLM failures should be visible, not hidden
- **Comprehensive logging** via `LLMLogger` for debugging
- **User-visible error states** in terminal UI
- **Graceful degradation** to SimpleActionExecutor when LLM unavailable

## Testing & Debugging

### Build & Run
```powershell
dotnet run  # Validates narrative structure first, then launches
# Option 5: Location Travel Mode
```

### Key Debug Commands
- **D key**: Dump current game state to console
- **ESC**: Exit location interaction mode
- **Mouse wheel**: Scroll terminal content
- **1-9 keys**: Select numbered options in narrative mode

### Local LLM Server
```bash
# Start server manually if needed (usually auto-started)
./llama-server --model models/qwen2-0_5b-instruct-q4_k_m.gguf --port 8080
```

## File Organization Conventions

### Namespace Structure
```
Cathedral.Game.*        // Game logic, modes, controllers
Cathedral.Terminal.*    // UI and rendering
Cathedral.LLM.*        // LLM integration
Cathedral.Glyph.*      // 3D sphere and interactions
```

### Documentation Pattern
- **PHASE6_**: Current narrative CoT system docs
- **PHASE5_**: LLM integration implementation
- **docs/*.md**: Implementation summaries, not aspirational plans

## Common Pitfalls to Avoid

1. **Never use Color4** - use Vector4 colors from Config.Colors
2. **Don't block UI thread** - LLM calls must be async
3. **Don't hardcode terminal dimensions** - use Config.Terminal constants
4. **Don't reference DEPRECATED/** - files excluded from build
5. **Always dispose LLM resources** - use using statements or try/finally
6. **Check narrative validation** - app validates structure at startup

## Integration Points

### Adding New Narrative Content
1. Define JSON schema using `CompositeField`, `StringField`, etc.
2. Generate GBNF grammar via `JsonConstraintGenerator.GenerateGBNF()`
3. Create prompt templates with structured output requirements
4. Validate responses using `JsonValidator`

### Terminal UI Extensions
1. Extend `TerminalView` for new character grid operations
2. Use `Config.Colors.*` for consistent theming
3. Handle mouse events via `CellClicked`, `CellHovered`
4. Implement scrolling for content longer than 30 lines

## Phase 6: Chain-of-Thought Narrative System

### Architecture Overview
Cathedral's core innovation is a **Chain-of-Thought narrative RPG system** inspired by Disco Elysium's skill personas:

```
Observation Phase → Thinking Phase → Action Phase → Outcome Phase
```

### Three-Function Skill System
Every skill has 1-3 functions defining its role:
- **Observation Skills**: Generate environmental perceptions with unique voices
- **Thinking Skills**: Provide Chain-of-Thought reasoning and generate 2-5 actions
- **Action Skills**: Execute chosen actions via skill checks (Level 1-10)

### Slot Management Architecture
**Critical Pattern**: One LLM slot per skill for persona consistency
```
Slots 0-9:   Observation skills (persona prompts cached)
Slots 10-29: Thinking skills (persona prompts cached)  
Slots 30-49: Action skills (reserved for future)
Slot 50:     Critic evaluator (yes/no judgments)
Slot 51:     Narrator (outcome descriptions)
```

### Narrative Node System
**NarrationNode**: Discrete narrative contexts within locations
- **Keywords**: 5-10 interactive elements (extracted from observations)
- **Outcomes**: Each keyword maps to 2-5 possible results
- **Items**: Auto-discovered via reflection from nested Item classes
- **Transitions**: Connections to other nodes

**Critical Pattern**: Items must be sealed nested classes within NarrationNode:
```csharp
public class ClearingNode : NarrationNode
{
    public sealed class Berries : Item { } // ✓ Correct pattern
}
```

### LLM Request Patterns

#### Observation Generation
```csharp
// Multi-skill approach: 2-3 observation skills run sequentially
var observationSkills = avatar.GetObservationSkills().Take(3);
foreach (var skill in observationSkills)
{
    int slotId = await skillSlotManager.GetOrCreateSlotForSkillAsync(skill);
    var prompt = promptConstructor.BuildObservationPrompt(node, avatar, skill);
    var schema = LLMSchemaConfig.CreateObservationSchema();
    var response = await llmServer.GenerateWithConstraintsAsync(slotId, prompt, JsonConstraintGenerator.GenerateGBNF(schema));
}
```

#### Thinking Generation
```csharp
// Player selects thinking skill, LLM generates CoT reasoning + actions
var prompt = thinkingPromptConstructor.BuildThinkingPrompt(keyword, node, possibleOutcomes, actionSkills, avatar, thinkingSkill);
var schema = LLMSchemaConfig.CreateThinkingSchema(actionSkills, possibleOutcomes);
var response = await thinkingExecutor.GenerateThinkingAsync(thinkingSkill, keyword, node, possibleOutcomes, actionSkills, avatar);
```

### Key Components

#### Phase Controllers
- **ObservationPhaseController**: Manages 2-3 sequential observation generations
- **ThinkingPhaseController**: Handles skill popup, CoT generation, fallback actions
- **ActionExecutionController**: Processes player action choice, skill checks, outcomes

#### Executors
- **ObservationExecutor**: Manages observation skill slots (0-9), implements keyword fallback
- **ThinkingExecutor**: Manages thinking skill slots (10-29), JSON-constrained action generation
- **OutcomeNarrator**: Narrates action results from action skill's perspective

#### Core Classes
- **Skill**: Abstract base with SkillId, Functions[], PersonaPrompt, PersonaTone
- **SkillSlotManager**: Centralized skill-to-slot mapping with cached persona prompts
- **NarrativeValidator**: Ensures Item classes are nested and sealed within nodes
- **SkillRegistry**: Query skills by function, category, body part associations

### Skill Persona Design
Each observation/thinking skill has a **PersonaPrompt** defining its narrative voice:
```csharp
public override string PersonaPrompt => @"You are the inner voice of MYCOLOGY, specialized knowledge of fungi.
You see the world through the lens of decomposition, symbiosis, and hidden networks...";
```

**PersonaTone**: Short description for user prompts (e.g., "a systematic mapper who transforms space into navigable representation")

### Validation Requirements
1. **Narrative Structure**: App validates all Items are nested within NarrationNodes at startup
2. **Sealed Items**: All Item classes must be sealed (enforced by validator)
3. **Unique Names**: No duplicate Item class names across the codebase
4. **Schema Validation**: All LLM responses validated against JSON schemas

### Error Handling Philosophy
- **No mock fallbacks** for LLM failures - make errors visible to developers
- **Graceful degradation** to SimpleActionExecutor when LLM unavailable  
- **Comprehensive logging** via LLMLogger to `logs/llm_session_*/slot_*/`
- **User-visible error states** in terminal UI rather than hidden failures

Remember: This is a **working prototype** focused on narrative AI experimentation, not a production game. Prioritize rapid iteration and LLM integration testing over performance optimization.