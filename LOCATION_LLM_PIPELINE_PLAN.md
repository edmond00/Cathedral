# Location-Based LLM Dungeon Master Pipeline Implementation Plan

## Overview

This document outlines the implementation plan for a location-based LLM Dungeon Master pipeline that generates contextual actions and consequences based on game state and location blueprints. The system leverages the existing JSON constraint framework to ensure LLM outputs conform to game rules.

## Architecture Overview

```
Location ID + RNG Seed
         ↓
   LocationFeatureGenerator
    (Tavern, Forest, etc.)
         ↓
    ┌─────────────────┐
    │ Context String  │  →  LLM Input
    │ LocationBlueprint│  →  Blueprint2Constraint
    └─────────────────┘           ↓
                            JSON Constraints (GBNF)
                                   ↓
                              LLM DM Output
                            (Constrained JSON)
```

## Core Components

### 1. Location System Foundation

#### 1.1 Core Data Structures

**LocationBlueprint** - Core data structure defining location mechanics:
```csharp
public record LocationBlueprint(
    string LocationId,
    string LocationType,
    Dictionary<string, LocationState> States,
    Dictionary<string, Sublocation> Sublocations,
    Dictionary<string, List<string>> StateTransitions,
    Dictionary<string, Dictionary<string, LocationContent>> ContentMap
);

public record LocationState(
    string Id,
    string Name,
    string Description,
    bool IsDefault = false
);

public record Sublocation(
    string Id, 
    string Name,
    string Description,
    List<string> RequiredStates,
    List<string> ForbiddenStates
);

public record LocationContent(
    List<string> AvailableItems,
    List<string> AvailableCompanions, 
    List<string> AvailableQuests,
    List<string> AvailableNPCs
);
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

#### 2.1 TavernFeatureGenerator Example

```csharp
public class TavernFeatureGenerator : LocationFeatureGenerator
{
    public override string GenerateContext(string locationId)
    {
        SetSeed(locationId);
        
        var crowdLevel = Rng.Next(3); // 0=quiet, 1=moderate, 2=busy
        var timeOfDay = Rng.Next(4); // 0=morning, 1=afternoon, 2=evening, 3=night
        var mood = Rng.Next(3); // 0=somber, 1=neutral, 2=festive
        
        return $"The tavern {GetCrowdDescription(crowdLevel)} with patrons. " +
               $"It's {GetTimeDescription(timeOfDay)} and the atmosphere is {GetMoodDescription(mood)}. " +
               $"{GetSpecialFeatures()}";
    }
    
    public override LocationBlueprint GenerateBlueprint(string locationId)
    {
        SetSeed(locationId);
        
        var states = new Dictionary<string, LocationState>
        {
            ["gate_open"] = new("gate_open", "Gate Open", "The tavern is open for business", true),
            ["gate_closed"] = new("gate_closed", "Gate Closed", "The tavern is closed"),
            ["brawl_active"] = new("brawl_active", "Brawl in Progress", "A fight is happening")
        };
        
        var sublocations = new Dictionary<string, Sublocation>
        {
            ["main_hall"] = new("main_hall", "Main Hall", "The main drinking area",
                RequiredStates: new List<string> { "gate_open" },
                ForbiddenStates: new List<string>()),
            ["private_room"] = new("private_room", "Private Room", "A quiet back room",
                RequiredStates: new List<string> { "gate_open" },
                ForbiddenStates: new List<string> { "brawl_active" }),
            ["cellar"] = new("cellar", "Cellar", "Storage area below",
                RequiredStates: new List<string> { "gate_open" },
                ForbiddenStates: new List<string>())
        };
        
        // Define what actions can change states
        var stateTransitions = new Dictionary<string, List<string>>
        {
            ["main_hall"] = new List<string> { "brawl_active", "gate_closed" },
            ["private_room"] = new List<string> { "gate_closed" }
        };
        
        // Define available content for each sublocation/state combination
        var contentMap = GenerateContentMap();
        
        return new LocationBlueprint(locationId, "tavern", states, sublocations, stateTransitions, contentMap);
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
        List<string> currentStates)
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
        List<string> currentStates)
    {
        return new CompositeField("success_consequences",
            GenerateStateChangeConstraints(blueprint, currentSublocation),
            GenerateSublocationChangeConstraints(blueprint, currentStates),
            GenerateItemGainConstraints(blueprint, currentSublocation, currentStates),
            GenerateCompanionGainConstraints(blueprint, currentSublocation, currentStates),
            GenerateQuestGainConstraints(blueprint, currentSublocation, currentStates)
        );
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
  "action_text": "Approach the mysterious hooded figure in the corner",
  "success_consequences": {
    "state_change": "brawl_active",
    "sublocation_change": null,
    "item_gained": null,
    "companion_gained": "mysterious_ally", 
    "quest_gained": "find_the_artifact"
  },
  "failure_consequences": {
    "type": "damage",
    "description": "The figure draws a blade and attacks"
  },
  "related_skill": "diplomacy",
  "difficulty": 3
}
```

#### 4.2 Generated Constraints Example

For a tavern in `main_hall` with states `["gate_open"]`:

```csharp
var actionConstraints = new CompositeField("ActionChoice",
    new StringField("action_text", 10, 100),
    new CompositeField("success_consequences",
        new OptionalField("state_change", 
            new ChoiceField<string>("state_change", "brawl_active", "gate_closed")),
        new OptionalField("sublocation_change",
            new ChoiceField<string>("sublocation_change", "private_room", "cellar")),
        new OptionalField("item_gained",
            new ChoiceField<string>("item_gained", "tavern_key", "mysterious_letter")),
        new OptionalField("companion_gained", 
            new ChoiceField<string>("companion_gained", "tavern_keeper", "drunk_warrior")),
        new OptionalField("quest_gained",
            new ChoiceField<string>("quest_gained", "find_missing_patron", "retrieve_stolen_ale"))
    ),
    new CompositeField("failure_consequences",
        new ChoiceField<string>("type", "damage", "disease", "imprisonment", "ejection", "none"),
        new StringField("description", 5, 50)
    ),
    new ChoiceField<string>("related_skill", "strength", "diplomacy", "stealth", "magic"),
    new ChoiceField<int>("difficulty", 1, 2, 3, 4, 5)
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

#### Step 2.1: Tavern Implementation (Reference)
- [ ] Complete `TavernFeatureGenerator` implementation
- [ ] Define tavern-specific states and sublocations  
- [ ] Create content generation logic
- [ ] Add tavern-specific context generation
- [ ] Create comprehensive unit tests

#### Step 2.2: Additional Location Types
- [ ] Implement `ForestFeatureGenerator`
- [ ] Implement `CityFeatureGenerator` 
- [ ] Implement `FieldFeatureGenerator`
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

## Design Principles

### 1. Deterministic Generation
- Location features must be consistent across visits
- Use location ID as RNG seed for reproducibility
- Cache generated blueprints for performance

### 2. Small LLM Optimization
- Keep context strings under 200 words for phi3/phi4
- Use clear, simple language in prompts
- Minimize JSON schema complexity
- Provide concrete examples in prompts

### 3. Modular Architecture
- Each location type is independently implementable
- Blueprint generation is separate from constraint generation
- Easy to add new location types without modifying existing code
- Clear separation between game logic and LLM interaction

### 4. Robust Validation
- All LLM outputs are validated against constraints
- Fallback mechanisms for invalid responses
- Clear error messages for debugging
- Comprehensive logging for troubleshooting

## Example Usage Scenarios

### Scenario 1: Entering a Tavern

```csharp
var generator = new TavernFeatureGenerator();
var context = generator.GenerateContext("tavern_001");
var blueprint = generator.GenerateBlueprint("tavern_001");

var constraints = Blueprint2Constraint.GenerateActionConstraints(
    blueprint, "main_hall", new List<string> { "gate_open" });

var gbnf = JsonConstraintGenerator.GenerateGBNF(constraints);
var template = JsonConstraintGenerator.GenerateTemplate(constraints);

// Send to LLM with context + template
var llmResponse = await llmManager.GenerateJsonAsync(
    prompt: $"{context}\n\nGenerate an action choice:\n{template}",
    gbnf: gbnf);
```

### Scenario 2: Forest Exploration

```csharp
var generator = new ForestFeatureGenerator();
var context = generator.GenerateContext("forest_042");
// Context might be: "Ancient oaks tower overhead, their branches forming a dense canopy. 
// Shafts of golden sunlight pierce through gaps, illuminating patches of moss-covered ground. 
// The air smells of damp earth and pine. You hear rustling in nearby bushes and distant bird calls."

var blueprint = generator.GenerateBlueprint("forest_042");
// Blueprint includes: clearings, hidden groves, ancient trees, possible encounters

var constraints = Blueprint2Constraint.GenerateActionConstraints(
    blueprint, "forest_path", new List<string> { "daylight", "safe" });
// Generates constraints for actions like: explore clearing, climb tree, search for herbs, 
// set up camp, track animals
```

## Files to Create

### Core Infrastructure
1. `/src/LLM/LocationSystem/LocationBlueprint.cs` - Core data structures
2. `/src/LLM/LocationSystem/LocationFeatureGenerator.cs` - Abstract base class
3. `/src/LLM/LocationSystem/Blueprint2Constraint.cs` - Constraint generation
4. `/src/LLM/LocationSystem/GameStateManager.cs` - State persistence

### Location Implementations  
5. `/src/LLM/LocationSystem/Generators/TavernFeatureGenerator.cs`
6. `/src/LLM/LocationSystem/Generators/ForestFeatureGenerator.cs` 
7. `/src/LLM/LocationSystem/Generators/CityFeatureGenerator.cs`
8. `/src/LLM/LocationSystem/Generators/FieldFeatureGenerator.cs`
9. `/src/LLM/LocationSystem/LocationGeneratorFactory.cs`

### Integration & Testing
10. `/src/LLM/LocationSystem/LocationDMPipeline.cs` - Main orchestration
11. `/tests/LocationSystem/LocationGeneratorTests.cs` - Unit tests
12. `/tests/LocationSystem/Blueprint2ConstraintTests.cs` - Constraint tests  
13. `/tests/LocationSystem/LocationDMIntegrationTests.cs` - E2E tests

### Content & Configuration
14. `/data/LocationContent/` - JSON files with location-specific content
15. `/data/LocationPrompts/` - LLM prompt templates
16. `/config/LocationConfig.json` - Configuration settings

## Success Metrics

- **Deterministic Generation**: Same location ID always produces identical features
- **Constraint Adherence**: 100% of LLM outputs validate against generated schemas
- **Response Quality**: Actions are contextually appropriate and engaging
- **Performance**: Sub-2 second response time with phi3/phi4 models
- **Extensibility**: New location types implementable in under 1 day
- **Small LLM Compatibility**: Works effectively with 1B-7B parameter models

This pipeline will create a robust, scalable system for location-based narrative generation that maintains game consistency while leveraging the creativity of local LLMs.