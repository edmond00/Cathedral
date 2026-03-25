# Cathedral

A Windows desktop narrative RPG built in C# combining a 3D glyph-sphere world with local LLM-driven storytelling. The aesthetic blends roguelike exploration with Chain-of-Thought narrative AI, inspired by Disco Elysium.

## Tech Stack

- **Language:** C# (.NET 8.0, Windows)
- **Rendering:** OpenTK 4.x (OpenGL 3.3+)
- **UI:** ASCII/glyph terminal rendered via OpenGL (100x100 main, 40x40 popup)
- **LLM:** llama.cpp HTTP server (localhost:8080), GGUF models
- **Fonts/Images:** SixLabors.ImageSharp, SixLabors.Fonts

## Build & Run

```bash
dotnet build
dotnet run                  # main game (Location Travel Mode)
dotnet run -- --fight       # combat test
dotnet run -- --dialogue    # dialogue demo
dotnet run -- --help        # all options
```

LLM features require a llama.cpp server running separately on `localhost:8080`.

## Architecture

```
src/
├── game/
│   ├── narrative/          # Story nodes, locations, body/anatomy, humor system
│   ├── dialogue/           # NPC conversation system
│   ├── LocationTravelGameController.cs   # Main game state machine
│   ├── LLMActionExecutor.cs              # LLM narrative integration
│   └── SimpleActionExecutor.cs          # Fallback (no LLM)
├── LLM/
│   ├── LlamaServerManager.cs            # Server lifecycle & slot management
│   ├── LlamaInstance.cs                 # Per-context generation
│   └── JsonConstraints/                 # GBNF grammar for structured LLM output
├── glyph/
│   ├── GlyphSphereCore.cs               # Main OpenGL window & rendering
│   └── microworld/                      # World topology, biomes, pathfinding
├── fight/                  # Turn-based grid combat
├── terminal/               # HUD rendering (TerminalHUD, GlyphAtlas, popups)
├── pathfinding/            # A* and movement
├── engine/                 # Camera, rendering utilities
└── Config.cs               # Central config (dimensions, colors, settings)
```

**Game flow:** `LocationTravelModeLauncher` → `GlyphSphereCore` (rendering) + `MicroworldInterface` (world) + `LocationTravelGameController` (state). Game modes: WorldView → Traveling → NarrativeCOT (Observation → Thinking → Action) → LocationInteraction.

## Key Systems

- **Humor system:** Medieval 4-humor model (blood, phlegm, yellow bile, black bile) influences dice rolls and stats
- **Body/anatomy:** Species, bodyparts, organs, wounds, derived stats
- **LLM narrative:** 3-phase CoT prompting with JSON-constrained output (GBNF grammars)
- **Narrative graph:** Location/scenario nodes defining the world
- **Combat:** Turn-based grid with skills and AI

## Notes

- This project is actively evolving — docs in `docs/` may be outdated, prefer reading source
- No test suite currently; iterate by running the game
- The project is Windows-only (OpenTK + Windows Forms dependency)
