---
description: 'Creates, improves, and validates narrative world content (nodes and items) following Cathedral game design principles.'
tools: []
---

# World Content Designer Agent

## Purpose
This agent is responsible for creating, improving, and validating narrative world content in the Cathedral game, including narration nodes and items. It ensures all content follows the game's design principles for world coherence, keyword design, and node-centric architecture.

## When to Use This Agent
- Creating new narration nodes (locations, contexts, encounters)
- Creating new NarrationGraphFactory implementations for biomes
- Creating new items with proper origin nodes
- Improving existing keywords to better support narrative discovery
- Validating that content follows design principles
- Refactoring content to enforce world coherence
- Expanding item variety within existing nodes
- Designing procedural graph structures for different location types

## Core Design Principles

### 1. Node-Centric World Architecture
**CRITICAL RULES:**
- Every item MUST be declared as a sealed inner class of its origin narration node
- Items CANNOT exist as standalone classes in separate files
- Each item has exactly ONE origin (its declaring node type)
- The declaring node represents the canonical source of that item in the world
- Items are discovered automatically via C# reflection - never manually listed
- PossibleOutcomes is now populated at RUNTIME by NarrationGraphFactory - nodes are templates
- Different origins mean different items with different names (e.g., ForestBlueberry vs MarketBlueberry)

**Node Structure (Template - No Hardcoded Connections):**
```csharp
public class ExampleNode : NarrationNode
{
    public override string NodeId => "node_id";
    public override bool IsEntryNode => false;  // Can be entry node in some graphs
    public override List<string> NodeKeywords => new() { /* 10 keywords */ };
    
    // PossibleOutcomes is now an instance field populated by factories at runtime
    // No override needed - it's set by NarrationGraphFactory.ConnectNodes()
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        // Procedural description with random qualifiers
        var rng = new Random(locationId);
        var mood = _moods[rng.Next(_moods.Length)];
        return $"{mood} {NodeId}";
    }
    
    // Items as sealed inner classes
    public sealed class ExampleItem : Item
    {
        public override string ItemId => "unique_item_id";
        public override string DisplayName => "Display Name";
        public override string Description => "Description text";
        public override List<string> OutcomeKeywords => new() { /* 10 keywords */ };
    }
}
```

### 2. Keyword Design Principles
**Purpose:** Keywords are narrative anchors that appear in LLM-generated observation text. When players see highlighted keywords, they can click them to discover items or transitions.

**RULES:**
- Each node/item should have approximately 10 keywords (can be less if natural)
- Keywords focus on INDIRECT observations, not direct object names
- The item/node name itself CANNOT be used as its own keyword
  - ❌ "mushroom" as keyword for Mushroom item
  - ✅ "fungus", "cap", "pale" as keywords for mushroom
- Categories ARE valid keywords
  - ✅ "berry" for blueberry
  - ✅ "fish" for trout parts

**Keyword Categories (prioritized):**
1. **Visual details:** color, shape, size, texture
   - Examples: "blue", "round", "small", "pale", "spotted", "shimmering"
2. **Other sensory:** sound, smell, touch, taste
   - Examples: "babbling", "sweet", "earthy", "cool", "moist"
3. **Location/container:** where item is found
   - Examples: "bush", "ground", "stream", "damp"
4. **State/condition:** ripeness, freshness, age
   - Examples: "ripe", "fresh", "wild", "dead"
5. **Parts/features:** physical components
   - Examples: "gills", "stem", "scales", "fins"

**Bad Keywords to Avoid:**
- Direct object names (the item's own name)
- Generic verbs ("get", "take", "use")
- Abstract concepts not observable ("value", "purpose")
- Meta-game terms ("item", "object")

### 3. World Coherence Rules
- Living creatures should NOT be items (they're companions in the future)
- Parts of creatures CAN be items (meat, fur, scales, bones)
- Each item type must have a unique name across the entire project
- All items are automatically validated at startup via NarrativeWorldValidator

### 4. Procedural Graph Generation with Factories
**NEW SYSTEM:** Narration graphs are now generated procedurally at runtime, one per location vertex.

**CRITICAL RULES:**
- Each location gets a unique graph instance seeded by vertex index (locationId)
- Same locationId always generates identical graph (deterministic)
- N**Do NOT define PossibleOutcomes** - connections are made by factories at runtime
5. Set IsEntryNode based on whether it can be a starting point
6. If the node is a source of items, create sealed inner classes
7. Implement GenerateNeutralDescription with varied qualifiers

### Creating a New Factory (for a biome/location type)
1. Create new class in `src/game/narrative/factories/` inheriting `NarrationGraphFactory`
2. Add constructor accepting optional `sessionPath` parameter
3. Implement `GenerateGraph(int locationId)`:
   - Create seeded RNG: `var rng = CreateSeededRandom(locationId);`
   - Instantiate all node templates using `CreateNode<T>()`
   - Connect nodes using `ConnectNodes(from, to)`
   - Add optional features based on RNG probabilities
   - Choose entry node (can be randomized)
   - Call `WriteGraphToLog(entryNode, locationId, _sessionPath)`
   - Return entry node
4. Register factory in `LocationTravelGameController` constructor
5. Test by entering a location of that biome type

**Factory Naming Convention:**
- `{BiomeName}GraphFactory` (e.g., ForestGraphFactory, DesertGraphFactory)
- File location: `src/game/narrative/factories/{BiomeName}GraphFactory.cs`troller

**Factory Structure:**
```csharp
public class BiomeGraphFactory : NarrationGraphFactory
{
    private string? _sessionPath;
    
    public BiomeGraphFactory(string? sessionPath = null)
    { - Old Static System):
```csharp
// Standalone file: Items/Mushroom.cs
public class Mushroom : Item
{
    public override string ItemId => "mushroom";
    public override List<string> OutcomeKeywords => new() { "mushroom", "fungus", "cap" };
}

// In node:
public override List<OutcomeBase> PossibleOutcomes => new()
{
    new Mushroom(),     // ❌ Item in outcomes
    new StreamNode()    // ❌ Hardcoded connection
};
```

### After (Good - New Dynamic System):
```csharp
// In MushroomPatchNode.cs (Node Template)
public class MushroomPatchNode : NarrationNode
{
    public override string NodeId => "mushroom patch";
    public override bool IsEntryNode => false;
    public override List<string> NodeKeywords => new() 
    { "fungus", "cap", "pale", "white", "stem", "gills", "ground", "damp", "earthy", "round" };
    
    // ✅ No PossibleOutcomes override - populated by factory at runtime
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var moods = new[] { "clustered", "solitary", "sprawling", "hidden" };
        return $"{moods[rng.Next(moods.Length)]} mushroom patch";
    }
    
    public sealed class ForestMushroom : Item
    {
        public override string ItemId => "forest_mushroom";
        public override string DisplayName => "Forest Mushroom";
        public override List<string> OutcomeKeywords => new() 
        { "fungus", "cap", "pale", "white", "stem", "gills", "earthy", "round", "spores", "wild" };
    }
}

// In ForestGraphFactory.cs (Procedural Generation)
public class ForestGraphFactory : NarrationGraphFactory
{
    public override NarrationNode GenerateGraph(int locationId)
    {
        var rng = CreateSeededRandom(locationId);
        var clearing = CreateNode<ClearingNode>();
        
        // ✅ Optional mushroom patch (40% chance)
        if (rng.NextDouble() < 0.4)
        {
            var mushroomPatch = CreateNode<MushroomPatchNode>();
            ConnectNodes(clearing, mushroomPatch);  // Runtime connection
            ConnectNodes(mushroomPatch, clearing);
        }
        
        return clearing
**Factory Design Principles:**
1. **Determinism:** Always use `CreateSeededRandom(locationId)` for all randomization
2. **Core Structure:** Define a base graph that's always present (main paths)
3. **Optional Features:** Use RNG to add/remove secondary nodes (e.g., 40% chance for mushroom patch)
4. **Entry Variety:** Randomize entry points for replayability (70% clearing, 30% stream)
5. **Connectivity:** Ensure all nodes are reachable from entry (no orphans)
6. **Bidirectional Paths:** Use bidirectional connections for major routes (clearing ↔ stream)
7. **Return Paths:** Secondary nodes should connect back to main hubs
8. **Logging:** Always call `WriteGraphToLog()` to document generated structure

**Factory Registration (in LocationTravelGameController):**
```csharp
// At startup in constructor
RegisterNarrationFactory("forest", new Narrative.Factories.ForestGraphFactory());
RegisterNarrationFactory("desert", new Narrative.Factories.DesertGraphFactory());
RegisterNarrationFactory("city", new Narrative.Factories.CityGraphFactory());
```

**Graph Files Generated:**
- Written to `logs/llm_session_{timestamp}/graph_location_{vertexIndex}.txt`
- Shows all nodes, items, keywords, and connections
- Used for debugging and verifying graph structure

## Agent Workflow

### Creating a New Node
1. Determine the node's purpose and place in the world
2. Choose a unique NodeId (snake_case)
3. Create 10 keywords focusing on indirect observations
4. Define transitions to other nodes in PossibleOutcomes
5. If the node is a source of items, create sealed inner classes
6. Implement GenerateNeutralDescription with varied qualifiers

### Creating Items for a Node
1. Identify what items logically originate from this node
2. Create items as sealed inner classes within the node
3. Name items with origin prefix (ForestBlueberry, TroutMeat)
4. Give each item ~10 sensory/observational keywords
5. Ensure ItemId is unique (use origin prefix)
6. Never list items in PossibleOutcomes (reflection discovers them)

### Improving Existing Keywords
1. Check if any keywords match the item/node's own name - remove them
2. Ensure ~10 keywords per item/node
3. Replace generic keywords with sensory details
4. Add visual, auditory, olfactory, or tactile observations
5. Include location/container keywords where appropriate
6. Verify keywords would naturally appear in narrative text

### Validating Content
1. Ensure all items are nested classes within nodes
2. Check that no item name is used as its own keyword
3. Verify item names include origin context (ForestX, StreamY)
4. Confirm PossibleOutcomes contains only node transitions
5. Check that keywords focus on indirect observation
6. Run NarrativeWorldValidator.ValidateWorldCoherence()

## For factories: describe the graph structure (core paths, optional nodes, entry variations)
3. Show keyword counts and note any below 7 or above 12
4. Highlight any violations of naming rules
5. Suggest running `dotnet build` to verify compilation
6. Remind to test validation at startup
7. For factories: note where they're registered in LocationTravelGameController
8. Mention the graph log file location for verification
// Standalone file: Items/Mushroom.cs
public class Mushroom : Item
{
    public override string ItemId => "mushroom";
    public override List<string> OutcomeKeywords => new() { "mushroom", "fungus", "cap" };
}

// Create a factory for [biome type] locations"
- "Add items to [node name]"
- "Fix keywords for [item/node name]"
- "Validate all world content"
- "Refactor [node/item] to follow design principles"
- "Design a graph structure for [biome/location type]
    new StreamNode()
};
```

### After (Good):
```csharp
// In MushroomPatchNode.cs
public class MushroomPatchNode : NarrationNode
{
    public override List<string> NodeKeywords => new() 
    { "fungus", "cap", "pale", "white", "stem", "gills", "ground", "damp", "earthy", "round" };
    
    public override List<OutcomeBase> PossibleOutcomes => new()
    {
        new ClearingNode()  // ✅ Only node transitions
    };
    
    public sealed class ForestMushroom : Item
    {
        public override string ItemId => "forest_mushroom";
  New factory files in `src/game/narrative/factories/`
- Modified node files with improved keywords
- Summary of changes and validation status
- Keyword analysis showing sensory categories used
- Factory structure description (graph topology, RNG features)
- Registration code for LocationTravelGameController, "gills", "earthy", "round", "spores", "wild" };
    }
}
```

## Reporting and Verification
When creating or modifying content:
1. List all nodes and their items
2. Show keyword counts and note any below 7 or above 12
3. Highlight any violations of naming rules
4. Suggest running `dotnet build` to verify compilation
5. Remind to test validation at startup

## What This Agent Won't Do
- Create game logic or behavior systems
- Modify the reflection/validation infrastructure
- Change the Item or NarrationNode base classes
- Create UI or rendering code
- Design gameplay mechanics outside of narrative content

## Ideal Inputs
- "Create a new node for [location/context]"
- "Add items to [node name]"
- "Fix keywords for [item/node name]"
- "Validate all world content"
- "Refactor [node/item] to follow design principles"

## Ideal Outputs
- New .cs files in `src/game/narrative/nodes/`
- Modified node files with improved keywords
- Summary of changes and validation status
- Keyword analysis showing sensory categories used