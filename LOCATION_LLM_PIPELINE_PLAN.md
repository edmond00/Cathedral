# Location-Based LLM Dungeon Master Pipeline Implementation Plan

## Overview

This document outlines the implementation plan for a location-based LLM Dungeon Master pipeline that generates contextual actions and consequences based on game state and location blueprints. The system leverages the existing JSON constraint framework to ensure LLM outputs conform to game rules.

**Final Implementation Goal**: A terminal-based demo showcasing the complete forest exploration system where players can interactively explore procedurally generated forest blueprints through a series of constrained LLM-generated actions with realistic consequences and state progression.

## Architecture Overview

```
Location ID as RNG Seed
         ↓
   LocationFeatureGenerator
    (Tavern, Forest, etc.)
         ↓
    ┌──────────────────┐
    │ Context String   │  →  LLM Input
    │ LocationBlueprint│  →  Blueprint2Constraint
    └──────────────────┘           ↓
                            JSON Constraints (GBNF)
                            JSON Template
                                   ↓
                              LLM DM Output
                            (Constrained JSON)
```

## Core Components

### 1. Location System Foundation

#### 1.1 Core Data Structures

**LocationBlueprint** - Core data structure defining granular location mechanics:
```csharp
public record LocationBlueprint(
    string LocationId,
    string LocationType,
    Dictionary<string, StateCategory> StateCategories,
    Dictionary<string, Sublocation> Sublocations,
    Dictionary<string, List<string>> SublocationConnections,
    Dictionary<string, Dictionary<string, LocationContent>> ContentMap
);

// Categorized state system - each category can only have one active state
public record StateCategory(
    string CategoryId,
    string Name,
    Dictionary<string, LocationState> PossibleStates,
    string DefaultStateId,
    StateScope Scope // Location-wide or specific sublocation
);

public record LocationState(
    string Id,
    string Name,
    string Description,
    List<string> RequiredStates = null, // Cross-category dependencies
    List<string> ForbiddenStates = null
);

// Hierarchical sublocation system with granular connections
public record Sublocation(
    string Id, 
    string Name,
    string Description,
    string ParentSublocationId, // null for top-level
    List<string> DirectConnections, // Adjacent sublocations
    List<string> RequiredStates, // Required for access
    List<string> ForbiddenStates,
    Dictionary<string, string> LocalStates // Sublocation-specific state categories
);

public record LocationContent(
    List<string> AvailableItems,
    List<string> AvailableCompanions, 
    List<string> AvailableQuests,
    List<string> AvailableNPCs,
    List<string> AvailableActions // Small-scale contextual actions
);

public enum StateScope
{
    Location, // Affects entire location
    Sublocation // Affects only specific sublocation
}
```

#### 1.2 LocationFeatureGenerator Base Class

```csharp
public abstract class LocationFeatureGenerator
{
    protected Random Rng { get; private set; }
    
    public abstract string GenerateContext(string locationId);
    public abstract LocationBlueprint GenerateBlueprint(string locationId);
    
    public void SetSeed(string locationId)
    {
        // Deterministic seed from location ID
        Rng = new Random(locationId.GetHashCode());
    }
}
```

### 2. Specialized Location Generators

#### 2.1 ForestFeatureGenerator Example

```csharp
public class ForestFeatureGenerator : LocationFeatureGenerator
{
    public override string GenerateContext(string locationId)
    {
        SetSeed(locationId);
        
        var canopyDensity = Rng.Next(3); // 0=sparse, 1=moderate, 2=dense
        var timeOfDay = Rng.Next(4); // 0=dawn, 1=morning, 2=afternoon, 3=dusk
        var weather = Rng.Next(4); // 0=clear, 1=misty, 2=drizzle, 3=storm
        var wildlifeActivity = Rng.Next(3); // 0=quiet, 1=active, 2=agitated
        
        return $"The forest {GetCanopyDescription(canopyDensity)} overhead. " +
               $"It's {GetTimeDescription(timeOfDay)} and {GetWeatherDescription(weather)}. " +
               $"Wildlife seems {GetWildlifeDescription(wildlifeActivity)}. {GetSpecialFeatures()}";
    }
    
    public override LocationBlueprint GenerateBlueprint(string locationId)
    {
        SetSeed(locationId);
        
        // Generate varied forest features based on RNG
        var forestType = Rng.Next(4); // Different forest characteristics
        var hasWaterFeature = Rng.NextDouble() > 0.5;
        var hasElevationChange = Rng.NextDouble() > 0.4;
        var specialFeature = Rng.Next(5); // ancient_grove, fairy_ring, ruins, cave_system, sacred_site
        
        // Categorized state system for natural environment
        var stateCategories = new Dictionary<string, StateCategory>
        {
            ["time_of_day"] = new(
                "time_of_day", "Time of Day",
                new Dictionary<string, LocationState>
                {
                    ["dawn"] = new("dawn", "Dawn", "First light filters through the canopy"),
                    ["morning"] = new("morning", "Morning", "Bright daylight illuminates the forest floor"),
                    ["afternoon"] = new("afternoon", "Afternoon", "Dappled sunlight creates shifting patterns"),
                    ["dusk"] = new("dusk", "Dusk", "Golden light fades as shadows deepen"),
                    ["night"] = new("night", "Night", "Darkness envelops the forest")
                },
                "morning", StateScope.Location),
                
            ["weather"] = new(
                "weather", "Weather",
                new Dictionary<string, LocationState>
                {
                    ["clear"] = new("clear", "Clear", "Bright sky visible through branches"),
                    ["misty"] = new("misty", "Misty", "Soft fog drifts between trees"),
                    ["drizzle"] = new("drizzle", "Drizzling", "Light rain patters on leaves"),
                    ["storm"] = new("storm", "Storm", "Heavy rain and wind shake the canopy")
                },
                "clear", StateScope.Location),
                
            ["wildlife_state"] = new(
                "wildlife_state", "Wildlife Activity",
                new Dictionary<string, LocationState>
                {
                    ["calm"] = new("calm", "Calm", "Animals go about their normal activities"),
                    ["alert"] = new("alert", "Alert", "Wildlife is wary and watching"),
                    ["agitated"] = new("agitated", "Agitated", "Animals are disturbed and fleeing"),
                    ["hunting"] = new("hunting", "Hunting", "Predators are actively stalking")
                },
                "calm", StateScope.Location),
                
            ["path_visibility"] = new(
                "path_visibility", "Path Visibility",
                new Dictionary<string, LocationState>
                {
                    ["clear_trail"] = new("clear_trail", "Clear Trail", "Well-defined path is easy to follow"),
                    ["faint_trail"] = new("faint_trail", "Faint Trail", "Barely visible track requires attention"),
                    ["overgrown"] = new("overgrown", "Overgrown", "Path is choked with vegetation"),
                    ["lost"] = new("lost", "Lost", "No visible path remains")
                },
                "clear_trail", StateScope.Sublocation)
        };
        
        // Generate granular, varied sublocations
        var sublocations = GenerateVariedForestSublocations(forestType, hasWaterFeature, hasElevationChange, specialFeature);
        
        // Define hierarchical connections between sublocations
        var connections = GenerateSublocationConnections(sublocations.Keys.ToList());
        
        // Generate content map with natural environment interactions
        var contentMap = GenerateGranularForestContentMap(sublocations);
        
        return new LocationBlueprint(locationId, "forest", stateCategories, sublocations, connections, contentMap);
    }
    
    private Dictionary<string, Sublocation> GenerateVariedForestSublocations(int forestType, bool hasWaterFeature, bool hasElevationChange, int specialFeature)
    {
        var sublocations = new Dictionary<string, Sublocation>
        {
            // Core forest areas (all forests have these)
            ["forest_edge"] = new("forest_edge", "Forest Edge", "Where open land meets the tree line",
                null, new List<string> { "outer_grove", "main_path" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>()),
                
            ["main_path"] = new("main_path", "Main Forest Path", "Primary trail winding through the trees",
                "forest_edge", new List<string> { "forest_edge", "path_fork", "dense_thicket" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>
                {
                    ["path_visibility"] = "clear_trail"
                }),
                
            ["outer_grove"] = new("outer_grove", "Outer Grove", "Younger trees with more open spacing",
                "forest_edge", new List<string> { "forest_edge", "main_path", "berry_patch" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>()),
                
            ["path_fork"] = new("path_fork", "Path Fork", "Where the main trail splits in multiple directions",
                "main_path", new List<string> { "main_path", "deep_woods", "hidden_trail" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>()),
                
            ["dense_thicket"] = new("dense_thicket", "Dense Thicket", "Tightly packed undergrowth and brambles",
                "main_path", new List<string> { "main_path", "small_clearing" },
                new List<string>(), new List<string> { "weather.storm" }, // Dangerous in storms
                new Dictionary<string, string>()),
                
            ["deep_woods"] = new("deep_woods", "Deep Woods", "Ancient trees with thick canopy overhead",
                "path_fork", new List<string> { "path_fork", "old_growth_area", "moss_covered_rocks" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>())
        };
        
        // Add water features if generated
        if (hasWaterFeature)
        {
            var waterType = Rng.Next(3);
            switch (waterType)
            {
                case 0: // Stream
                    sublocations["stream_crossing"] = new("stream_crossing", "Stream Crossing", "Babbling brook with stepping stones",
                        "path_fork", new List<string> { "path_fork", "stream_bank", "upstream_pool" },
                        new List<string>(), new List<string>(),
                        new Dictionary<string, string>());
                    sublocations["stream_bank"] = new("stream_bank", "Stream Bank", "Muddy shore with animal tracks",
                        "stream_crossing", new List<string> { "stream_crossing", "reed_bed" },
                        new List<string>(), new List<string>(),
                        new Dictionary<string, string>());
                    sublocations["upstream_pool"] = new("upstream_pool", "Upstream Pool", "Deep, clear pool perfect for drinking",
                        "stream_crossing", new List<string> { "stream_crossing" },
                        new List<string>(), new List<string>(),
                        new Dictionary<string, string>());
                    break;
                    
                case 1: // Pond
                    sublocations["hidden_pond"] = new("hidden_pond", "Hidden Pond", "Still water surrounded by cattails",
                        "deep_woods", new List<string> { "deep_woods", "pond_edge", "lily_pad_area" },
                        new List<string> { "path_visibility.faint_trail" }, // Hard to find
                        new List<string>(),
                        new Dictionary<string, string>());
                    break;
            }
        }
        
        // Add elevation changes if generated
        if (hasElevationChange)
        {
            sublocations["hill_base"] = new("hill_base", "Hill Base", "Gentle slope begins to rise",
                "deep_woods", new List<string> { "deep_woods", "steep_climb", "rocky_outcrop" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>());
            sublocations["steep_climb"] = new("steep_climb", "Steep Climb", "Challenging upward path through roots and rocks",
                "hill_base", new List<string> { "hill_base", "ridge_top" },
                new List<string>(), new List<string> { "weather.storm" },
                new Dictionary<string, string>());
            sublocations["ridge_top"] = new("ridge_top", "Ridge Top", "High vantage point above the canopy",
                "steep_climb", new List<string> { "steep_climb", "overlook" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>());
        }
        
        // Add varied special features based on RNG
        switch (specialFeature)
        {
            case 0: // Ancient Grove
                sublocations["ancient_grove_entrance"] = new("ancient_grove_entrance", "Ancient Grove Entrance", "Circle of massive, gnarled oak trees",
                    "deep_woods", new List<string> { "deep_woods", "grove_center", "ritual_circle" },
                    new List<string>(), new List<string>(),
                    new Dictionary<string, string>());
                sublocations["grove_center"] = new("grove_center", "Grove Center", "Sacred heart of the ancient trees",
                    "ancient_grove_entrance", new List<string> { "ancient_grove_entrance", "spirit_tree" },
                    new List<string> { "wildlife_state.calm" }, new List<string> { "wildlife_state.agitated" },
                    new Dictionary<string, string>());
                break;
                
            case 1: // Fairy Ring
                sublocations["fairy_ring"] = new("fairy_ring", "Fairy Ring", "Perfect circle of mushrooms in a small clearing",
                    "dense_thicket", new List<string> { "dense_thicket", "mushroom_patch" },
                    new List<string> { "time_of_day.dusk", "time_of_day.night" }, new List<string>(),
                    new Dictionary<string, string>());
                break;
                
            case 2: // Overgrown Ruins
                sublocations["moss_covered_stones"] = new("moss_covered_stones", "Moss-Covered Stones", "Ancient worked stones reclaimed by nature",
                    "old_growth_area", new List<string> { "old_growth_area", "collapsed_foundation", "vine_covered_arch" },
                    new List<string>(), new List<string>(),
                    new Dictionary<string, string>());
                sublocations["collapsed_foundation"] = new("collapsed_foundation", "Collapsed Foundation", "Broken stone foundation overgrown with roots",
                    "moss_covered_stones", new List<string> { "moss_covered_stones", "hidden_chamber" },
                    new List<string>(), new List<string>(),
                    new Dictionary<string, string>
                    {
                        ["chamber_access"] = "sealed" // Requires clearing debris
                    });
                break;
                
            case 3: // Cave System
                sublocations["cave_entrance"] = new("cave_entrance", "Cave Entrance", "Dark opening in a moss-covered hillside",
                    "rocky_outcrop", new List<string> { "rocky_outcrop", "cave_mouth", "entrance_chamber" },
                    new List<string>(), new List<string>(),
                    new Dictionary<string, string>());
                sublocations["entrance_chamber"] = new("entrance_chamber", "Entrance Chamber", "Twilight zone where forest light fades",
                    "cave_entrance", new List<string> { "cave_entrance", "deeper_tunnels" },
                    new List<string>(), new List<string> { "time_of_day.night" }, // Need light at night
                    new Dictionary<string, string>());
                break;
        }
        
        // Add small detail sublocations for granular exploration
        sublocations["berry_patch"] = new("berry_patch", "Berry Patch", "Wild bushes heavy with ripe berries",
            "outer_grove", new List<string> { "outer_grove" },
            new List<string>(), new List<string>(),
            new Dictionary<string, string>
            {
                ["berry_season"] = "ripe" // Seasonal sublocation state
            });
            
        sublocations["fallen_log"] = new("fallen_log", "Fallen Log", "Massive tree trunk across the path",
            "main_path", new List<string> { "main_path", "log_interior" },
            new List<string>(), new List<string>(),
            new Dictionary<string, string>());
        
        return sublocations;
    }
}
```

### 3. Blueprint2Constraint Module

#### 3.1 Core Interface

```csharp
public static class Blueprint2Constraint
{
    public static JsonField GenerateActionConstraints(
        LocationBlueprint blueprint, 
        string currentSublocation, 
        Dictionary<string, string> currentStates) // Category -> StateId mapping
    {
        return new CompositeField("ActionChoice", 
            new StringField("action_text", 10, 100),        // LLM-generated
            GenerateSuccessConstraints(blueprint, currentSublocation, currentStates),
            GenerateFailureConstraints(),                    // LLM-generated
            new ChoiceField<string>("related_skill", GetAvailableSkills()),
            new ChoiceField<int>("difficulty", 1, 2, 3, 4, 5)
        );
    }
    
    private static JsonField GenerateSuccessConstraints(
        LocationBlueprint blueprint, 
        string currentSublocation, 
        Dictionary<string, string> currentStates)
    {
        return new CompositeField("success_consequences",
            GenerateCategorizedStateChangeConstraints(blueprint, currentSublocation, currentStates),
            GenerateHierarchicalSublocationChangeConstraints(blueprint, currentSublocation, currentStates),
            GenerateItemGainConstraints(blueprint, currentSublocation, currentStates),
            GenerateCompanionGainConstraints(blueprint, currentSublocation, currentStates),
            GenerateQuestGainConstraints(blueprint, currentSublocation, currentStates)
        );
    }
    
    private static JsonField GenerateCategorizedStateChangeConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var availableStateChanges = new List<CompositeField>();
        
        // Find state categories that can be changed from current sublocation
        foreach (var (categoryId, category) in blueprint.StateCategories)
        {
            // Check if this state category is relevant to current sublocation
            if (CanInfluenceStateCategory(blueprint, currentSublocation, categoryId))
            {
                var possibleStates = GetAccessibleStates(category, currentStates);
                if (possibleStates.Any())
                {
                    availableStateChanges.Add(new CompositeField($"change_{categoryId}",
                        new ConstantStringField("category", categoryId),
                        new ChoiceField<string>("new_state", possibleStates.ToArray())
                    ));
                }
            }
        }
        
        return new OptionalField("state_changes", 
            new VariantField("state_change", availableStateChanges.ToArray()));
    }
    
    private static JsonField GenerateHierarchicalSublocationChangeConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var currentSubLocation = blueprint.Sublocations[currentSublocation];
        var accessibleSublocations = new List<string>();
        
        // Add directly connected sublocations
        foreach (var connectedId in currentSubLocation.DirectConnections)
        {
            var connected = blueprint.Sublocations[connectedId];
            if (CanAccessSublocation(connected, currentStates))
            {
                accessibleSublocations.Add(connectedId);
            }
        }
        
        // Add child sublocations (one level down)
        foreach (var (sublocationId, sublocation) in blueprint.Sublocations)
        {
            if (sublocation.ParentSublocationId == currentSublocation &&
                CanAccessSublocation(sublocation, currentStates))
            {
                accessibleSublocations.Add(sublocationId);
            }
        }
        
        // Add parent sublocation (moving back up)
        if (currentSubLocation.ParentSublocationId != null)
        {
            accessibleSublocations.Add(currentSubLocation.ParentSublocationId);
        }
        
        return accessibleSublocations.Any() 
            ? new OptionalField("sublocation_change",
                new ChoiceField<string>("sublocation_change", accessibleSublocations.ToArray()))
            : new OptionalField("sublocation_change", new ConstantStringField("no_movement", "none"));
    }
}
```

#### 3.2 Dynamic Constraint Generation

The system will analyze the current game state and generate appropriate constraints:

- **State Changes**: Only allow transitions defined in `blueprint.StateTransitions[currentSublocation]`
- **Sublocation Changes**: Only allow access to sublocations permitted by current states
- **Content Access**: Only offer items/companions/quests available in current sublocation+state

### 4. Action Constraint Schema

#### 4.1 Expected LLM Output Format

```json
{
  "action_text": "Carefully examine the fallen log for signs of insects or small animals",
  "success_consequences": {
    "state_changes": null,
    "sublocation_change": "log_interior",
    "item_gained": "beetle_collection",
    "companion_gained": null, 
    "quest_gained": null
  },
  "failure_consequences": {
    "type": "minor_injury",
    "description": "A startled creature bites your hand as you investigate"
  },
  "related_skill": "nature_lore",
  "difficulty": 2
}
```

#### 4.2 Generated Constraints Example - Granular Forest

For a forest with stream system in `main_path` with states `{"time_of_day": "morning", "weather": "clear", "wildlife_state": "calm", "path_visibility": "clear_trail"}`:

```csharp
var actionConstraints = new CompositeField("ActionChoice",
    new StringField("action_text", 10, 100),
    new CompositeField("success_consequences",
        new OptionalField("state_changes", new VariantField("state_change",
            new CompositeField("change_wildlife_state",
                new ConstantStringField("category", "wildlife_state"),
                new ChoiceField<string>("new_state", "alert", "agitated")),
            new CompositeField("change_path_visibility",
                new ConstantStringField("category", "path_visibility"),
                new ChoiceField<string>("new_state", "faint_trail", "overgrown")))),
        new OptionalField("sublocation_change",
            new ChoiceField<string>("sublocation_change", "path_fork", "dense_thicket", "outer_grove", "forest_edge")),
        new OptionalField("item_gained",
            new ChoiceField<string>("item_gained", "medicinal_herb", "animal_track_knowledge", "edible_berry")),
        new OptionalField("companion_gained", 
            new ChoiceField<string>("companion_gained", "forest_spirit", "woodland_creature")),
        new OptionalField("quest_gained",
            new ChoiceField<string>("quest_gained", "track_wounded_deer", "find_rare_flower"))
    ),
    new CompositeField("failure_consequences",
        new ChoiceField<string>("type", "lost", "injured", "startled_wildlife", "none"),
        new StringField("description", 5, 50)
    ),
    new ChoiceField<string>("related_skill", "nature_lore", "tracking", "stealth", "survival"),
    new ChoiceField<int>("difficulty", 1, 2, 3)
);
```

## Implementation Plan

### Phase 1: Foundation (Days 1-3)

#### Step 1.1: Core Data Structures
- [ ] Create `LocationBlueprint` and related records
- [ ] Create `LocationFeatureGenerator` abstract base class
- [ ] Add location state management interfaces
- [ ] Create unit tests for data structures

#### Step 1.2: Blueprint2Constraint Core
- [ ] Implement `Blueprint2Constraint` static class
- [ ] Create constraint generation methods
- [ ] Add validation logic for state transitions
- [ ] Create unit tests for constraint generation

### Phase 2: Location Generators (Days 4-7)

#### Step 2.1: Forest Implementation (Reference)
- [ ] Complete `ForestFeatureGenerator` implementation
- [ ] Define forest-specific natural states and hierarchical sublocations  
- [ ] Create environmental content generation logic
- [ ] Add forest-specific context generation with weather/wildlife variance
- [ ] Create comprehensive unit tests for natural environment simulation

#### Step 2.2: Additional Location Types
- [ ] Implement `MountainFeatureGenerator` (building on altitude-based hierarchy)
- [ ] Implement `CityFeatureGenerator` (for future NPC integration)
- [ ] Implement `FieldFeatureGenerator` (open environment variant)
- [ ] Create factory pattern for location type selection

### Phase 3: Integration & Testing (Days 8-10)

#### Step 3.1: LLM Integration
- [ ] Integrate with existing `LlamaServerManager`
- [ ] Create DM prompt templates optimized for small LLMs
- [ ] Add response validation and error handling
- [ ] Test with phi3/phi4 models

#### Step 3.2: End-to-End Testing
- [ ] Create integration tests with real LLM instances
- [ ] Test deterministic location generation
- [ ] Validate constraint adherence
- [ ] Performance optimization for small models

### Phase 4: Advanced Features (Days 11-14)

#### Step 4.1: Dynamic Content System
- [ ] Implement location content database
- [ ] Add quest chain support
- [ ] Create NPC interaction framework
- [ ] Add inventory and companion management

#### Step 4.2: State Persistence
- [ ] Add game state serialization
- [ ] Implement location state history
- [ ] Create save/load functionality
- [ ] Add state validation and recovery

### Phase 5: Terminal Demo Implementation (Days 15-17)

#### Step 5.1: Forest Exploration Demo
- [ ] Create interactive terminal interface for forest exploration
- [ ] Implement random forest blueprint generation system
- [ ] Build action list generation and display system
- [ ] Add player input handling for action selection
- [ ] Implement random success/failure resolution mechanics

#### Step 5.2: Game Loop Integration
- [ ] Create state update system based on action outcomes
- [ ] Implement location/sublocation progression tracking
- [ ] Build new action generation after state changes
- [ ] Add context display showing current location and state
- [ ] Create demo loop: Generate → Display → Choose → Resolve → Update → Repeat

#### Step 5.3: Demo Polish & Testing
- [ ] Add clear terminal output formatting and user prompts
- [ ] Implement demo session persistence (save/resume exploration)
- [ ] Create comprehensive demo scenarios (different forest variants)
- [ ] Add debug mode showing blueprint structure and state transitions
- [ ] Performance testing with multiple forest generation cycles

## Terminal Demo: Forest Exploration Game Loop

The final implementation will showcase the complete system through an interactive terminal-based forest exploration demo. This demo serves as both a proof-of-concept and a testing platform for the location blueprint mechanics.

### Demo Architecture

```
Demo Start
     ↓
 Generate Random Forest Blueprint (based on timestamp seed)
     ↓
Set Initial State: {time_of_day: "morning", weather: "clear", wildlife_state: "calm"}
Set Initial Location: "forest_edge"
     ↓
┌───────────────────────────────── GAME LOOP ─────────────────────────────────┐
│ 1. Display Current Context                                                    │
│    - Show current sublocation name and description                          │
│    - Display current state categories (time, weather, wildlife)            │
│    - Show any items/companions gained in previous actions                   │
│                                                                             │
│ 2. Generate Action Constraints                                              │
│    - Use Blueprint2Constraint.GenerateActionConstraints()                  │
│    - Based on current sublocation + current state combination              │
│    - Create GBNF grammar and JSON template                                 │
│                                                                             │
│ 3. Generate Action List via LLM                                            │
│    - Send context + constraints to LLM (phi3/phi4)                        │
│    - LLM generates 3-5 contextual actions respecting constraints           │
│    - Each action has: description, difficulty, related_skill               │
│                                                                             │
│ 4. Present Choices to Player                                               │
│    - Display numbered list of available actions                            │
│    - Show difficulty and skill for each action                             │
│    - Allow player to select by number (1-5)                               │
│                                                                             │
│ 5. Resolve Action Success/Failure                                          │
│    - Random roll vs difficulty (simple: roll 1d6, succeed if >= difficulty) │
│    - Apply success_consequences OR failure_consequences                     │
│    - Update game state and current sublocation accordingly                 │
│                                                                             │
│ 6. Display Action Results                                                  │
│    - Show what happened (success/failure description)                      │
│    - Display any state changes, location changes, items gained             │
│    - Update context for next iteration                                     │
│                                                                             │
│ 7. Check for Demo End Conditions                                           │
│    - Player reaches special locations (ancient grove center, summit)       │
│    - Player chooses to "Leave the forest"                                  │
│    - Time limit reached (20 actions max)                                   │
│                                                                             │
└───────────────────────────────────────────────────────────────────────────────┘
                                         ↓
                                    Loop Back to 1
                                         ↓
                                 Demo End → Show Summary
```

### Sample Demo Session

```
=== Forest Exploration Demo ===
Generating forest blueprint for 'forest_1732012345'...

--- TURN 1 ---
Location: Forest Edge
Time: Morning | Weather: Clear | Wildlife: Calm

You stand where the open meadow meets the ancient forest. Towering oaks create
a natural cathedral overhead, their branches filtering the morning sunlight
into dappled golden patterns on the moss-covered ground.

Available Actions:
1. [Easy] Follow the main forest path deeper into the woods (navigation)
2. [Medium] Search the edge area for useful plants or herbs (nature_lore)
3. [Easy] Examine the largest oak tree near the entrance (perception)
4. [Medium] Listen carefully for sounds deeper in the forest (survival)

Choose an action (1-4): 1

Rolling 1d6 vs difficulty 1... Rolled 4 - SUCCESS!

You follow the well-worn path as it winds between the massive tree trunks.
After a few minutes of walking, you arrive at a fork where the path splits.

Location changed: forest_edge → path_fork

--- TURN 2 ---
Location: Path Fork
Time: Morning | Weather: Clear | Wildlife: Calm

The main trail splits here into three directions. To the left, you hear the
bubbling of water. Straight ahead, the path continues into darker, denser woods.
To the right, you notice disturbed undergrowth where something large passed recently.

Available Actions:
1. [Easy] Take the left path toward the water sounds (navigation)
2. [Medium] Follow the main path into the deep woods (navigation)
3. [Hard] Investigate the disturbed undergrowth to the right (tracking)
4. [Medium] Climb a nearby tree to get a better view (athletics)
5. [Easy] Rest here and observe the area quietly (perception)

Choose an action (1-5): 3

Rolling 1d6 vs difficulty 4... Rolled 2 - FAILURE!

As you examine the broken branches and trampled earth, you accidentally step
on a dry twig. The loud crack echoes through the forest, and you hear rustling
as nearby animals flee from the disturbance.

Wildlife state changed: calm → alert

[...demo continues for up to 20 turns or until player reaches end goal...]
```

### Demo Features

- **Procedural Forest Generation**: Each demo run creates a unique forest layout
- **State Persistence**: Player choices affect future action availability
- **Realistic Progression**: Small steps gradually reveal the forest structure
- **LLM Integration**: Actions generated dynamically based on current context
- **Clear Feedback**: Every action shows immediate and long-term consequences
- **Exploration Goals**: Hidden areas and special features to discover

## Design Principles

### 1. Deterministic Generation with High Variance
- Location features must be consistent across visits for same location ID
- Use location ID as RNG seed for reproducibility
- Generate significant variety between different locations of same type
- Cache generated blueprints for performance
- Same tavern always has same layout, but different taverns have dramatically different features

### 2. Granular, Small-Step Interactions
- Actions represent small, incremental changes rather than major shifts
- Multiple actions required to achieve significant location changes
- Each sublocation offers 2-5 specific, contextual actions
- State changes affect future action availability progressively
- Players must navigate hierarchical location structure step by step

### 3. Simulation-Like State System
- Categorized states prevent impossible combinations (only one mood, one time-of-day, etc.)
- States scoped to appropriate level (location-wide vs sublocation-specific)
- Cross-sublocation state dependencies create emergent gameplay
- Small actions can have cascading effects through state system
- Realistic cause-and-effect relationships between player actions and world state

### 4. Hierarchical Location Design
- Sublocations have clear parent-child relationships
- Movement between sublocations follows logical spatial connections
- Hidden areas require specific states or items to discover
- Vertical and horizontal location structure reflects real spatial relationships
- Each level of hierarchy offers different types of interactions

### 5. Small LLM Optimization
- Keep context strings under 200 words for phi3/phi4
- Use clear, simple language in prompts
- Minimize JSON schema complexity while maintaining rich constraints
- Provide concrete, specific examples in prompts
- Focus on immediate, actionable choices rather than abstract concepts

### 6. Modular Architecture with Rich Variation
- Each location type generates dozens of different configurations
- Blueprint generation is separate from constraint generation
- Easy to add new location types without modifying existing code
- Clear separation between game logic and LLM interaction
- State categories and sublocation templates enable systematic variety

### 7. Robust Validation with Simulation Integrity
- All LLM outputs validated against current game state
- State transitions checked for logical consistency
- Sublocation access validated against hierarchical rules
- Fallback mechanisms for invalid responses
- Comprehensive logging for debugging complex state interactions

## Example Usage Scenarios

### Scenario 1: Entering a Varied Forest with Ancient Grove

```csharp
var generator = new ForestFeatureGenerator();
var context = generator.GenerateContext("forest_001");
// Context: "Ancient oaks spread their gnarled branches overhead, creating a dense canopy 
// that filters the morning sunlight into dappled patterns on the moss-covered ground. 
// The air is clear and still, filled with the scent of earth and pine. 
// Wildlife moves calmly through the underbrush."

var blueprint = generator.GenerateBlueprint("forest_001");
// Generated variant: Forest with ancient grove, stream system, elevation changes
// State categories: time_of_day=morning, weather=clear, wildlife_state=calm, path_visibility=clear_trail
// Hierarchical sublocations: forest_edge → (main_path → path_fork → deep_woods → ancient_grove_entrance → grove_center)
//                                        ↳ (stream_crossing → stream_bank), (hill_base → steep_climb → ridge_top)

var currentGameState = new Dictionary<string, string>
{
    ["time_of_day"] = "morning",
    ["weather"] = "clear", 
    ["wildlife_state"] = "calm",
    ["path_visibility"] = "clear_trail"
};

var constraints = Blueprint2Constraint.GenerateActionConstraints(
    blueprint, "forest_edge", currentGameState);

var gbnf = JsonConstraintGenerator.GenerateGBNF(constraints);
var template = JsonConstraintGenerator.GenerateTemplate(constraints);

// Send to LLM with context + template
var llmResponse = await llmManager.GenerateJsonAsync(
    prompt: $"{context}\n\nYou stand at the forest's edge. Generate a small, specific action:\n{template}",
    gbnf: gbnf);
    
// Expected LLM output might be:
// {
//   "action_text": "Follow the main path deeper into the forest",
//   "success_consequences": {
//     "sublocation_change": "main_path",
//     "state_changes": null,
//     "item_gained": null
//   },
//   "related_skill": "navigation",
//   "difficulty": 1
// }
```

### Scenario 2: Mountain Exploration - Granular Altitude System

```csharp
var generator = new MountainFeatureGenerator();
var context = generator.GenerateContext("mountain_085");
// Context: "Jagged peaks rise before you, their snow-capped summits piercing low clouds. 
// The mountain face shows multiple ledges and outcroppings. A narrow trail winds upward 
// from the foothills, while a dark cave entrance yawns in the rock face at mid-level."

var blueprint = generator.GenerateBlueprint("mountain_085");
// Generated sublocations showing granular design:
// - "base_camp" (altitude: 100m)
//   └─ "supply_cache" (hidden alcove)
//   └─ "trail_start" (beginning of ascent)
// - "lower_ledge" (altitude: 300m)
//   └─ "rest_area" (wide stone platform)
//   └─ "cliff_face" (vertical climbing section)
// - "cave_entrance" (altitude: 450m, branch from lower_ledge)
//   └─ "outer_chamber" (shallow cave area)
//   └─ "inner_depths" (requires torch/light)
// - "mid_slope" (altitude: 600m)
//   └─ "avalanche_zone" (dangerous loose rocks)
//   └─ "secure_outcrop" (safe resting point)
// - "grotto_entrance" (altitude: 580m, hidden from mid_slope)
//   └─ "crystal_chamber" (requires key/puzzle solution)
// - "upper_ledge" (altitude: 800m)
//   └─ "wind_shelter" (protected from weather)
//   └─ "eagle_nest" (active wildlife)
// - "summit_approach" (altitude: 950m)
//   └─ "final_ascent" (challenging climb)
//   └─ "summit_peak" (top of mountain)

// State categories for mountain:
// - "weather": ["clear", "cloudy", "storm", "blizzard"]
// - "time_of_day": ["dawn", "morning", "noon", "evening", "night"]
// - "climbing_gear": ["none", "basic_rope", "full_equipment"]
// - "grotto_access": ["sealed", "unlocked"] (sublocation-specific)
// - "avalanche_risk": ["stable", "unstable", "active"] (affects avalanche_zone)

var constraints = Blueprint2Constraint.GenerateActionConstraints(
    blueprint, "lower_ledge", 
    new Dictionary<string, string> 
    {
        ["weather"] = "clear",
        ["time_of_day"] = "morning", 
        ["climbing_gear"] = "basic_rope",
        ["avalanche_risk"] = "stable"
    });
    
// Generated constraints allow small-step actions like:
// - "Examine the cliff face for handholds" (movement to cliff_face)
// - "Search the rest area for supplies" (item discovery)
// - "Test the stability of loose rocks" (avalanche_risk state change)
// - "Look for the cave entrance" (discovery of cave_entrance)
// - "Continue climbing upward" (movement to mid_slope, requires climbing_gear check)
```

## Files to Create

### Core Infrastructure
1. `/src/LLM/LocationSystem/LocationBlueprint.cs` - Core data structures
2. `/src/LLM/LocationSystem/LocationFeatureGenerator.cs` - Abstract base class
3. `/src/LLM/LocationSystem/Blueprint2Constraint.cs` - Constraint generation
4. `/src/LLM/LocationSystem/GameStateManager.cs` - State persistence

### Location Implementations  
5. `/src/microworld/LocationSystem/Generators/ForestFeatureGenerator.cs` (Reference implementation)
9. `/src/microworld/LocationSystem/LocationGeneratorFactory.cs`

### Integration & Testing
10. `/src/microworld/LocationSystem/LocationDMPipeline.cs` - Main orchestration
11. `/tests/LocationSystem/LocationGeneratorTests.cs` - Unit tests
12. `/tests/LocationSystem/Blueprint2ConstraintTests.cs` - Constraint tests  
13. `/tests/LocationSystem/LocationDMIntegrationTests.cs` - E2E tests

### Terminal Demo
14. `/src/microworld/Demo/ForestExplorationDemo.cs` - Interactive terminal demo
15. `/src/microworld/Demo/DemoGameLoop.cs` - Game state and action resolution loop
16. `/src/microworld/Demo/TerminalInterface.cs` - User interface and display formatting

## Success Metrics

### Core System Metrics
- **Deterministic Generation**: Same location ID always produces identical features across visits
- **Blueprint Variance**: Different locations of same type have 80%+ unique sublocation combinations
- **Constraint Adherence**: 100% of LLM outputs validate against generated schemas
- **Granular Progression**: Average 3-5 actions required to move between major location areas
- **State System Integrity**: No invalid state combinations possible through any action sequence
- **Hierarchical Navigation**: All sublocation connections follow logical spatial relationships
- **Response Quality**: Actions are contextually appropriate and encourage systematic exploration
- **Simulation Depth**: State changes have observable effects on available actions within 2 turns
- **Performance**: Sub-2 second response time with phi3/phi4 models
- **Extensibility**: New location types implementable in under 1 day with full variance support
- **Small LLM Compatibility**: Works effectively with 1B-7B parameter models
- **Action Granularity**: 95%+ of actions represent small, logical steps rather than major changes

### Terminal Demo Metrics
- **Demo Completeness**: Full game loop functional from forest generation to action resolution
- **User Experience**: Clear, intuitive terminal interface with helpful prompts and feedback
- **Session Engagement**: Average demo session lasts 8-15 actions before natural conclusion
- **Exploration Depth**: Players discover 60%+ of available sublocations in typical session
- **Action Variety**: LLM generates 4-6 distinct action types per sublocation on average
- **State Progression**: Observable consequences from player choices within 1-3 action cycles
- **Error Handling**: Graceful recovery from invalid inputs or LLM generation failures
- **Performance Consistency**: Demo maintains <2 second response time throughout 20+ action cycles

This pipeline will create a robust, scalable system for location-based narrative generation that maintains game consistency while leveraging the creativity of local LLMs. The system emphasizes simulation-like depth and granular progression, making each location feel like a richly detailed, systematically explorable space rather than a simple menu of abstract choices.

**The terminal demo will serve as a concrete proof-of-concept, demonstrating how the abstract blueprint system translates into engaging, interactive gameplay with meaningful player agency and realistic world simulation.**