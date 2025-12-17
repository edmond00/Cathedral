# Phase 6: Chain-of-Thought Narrative System - Design Document

**Date**: December 17, 2025  
**Status**: Design Phase  
**Goal**: Implement a Chain-of-Thought narrative RPG system inspired by Disco Elysium's skill personas, replacing the LocationBlueprint system with a more flexible NarrationNode graph.

---

## Table of Contents
1. [Overview](#overview)
2. [Design Clarifications](#design-clarifications)
3. [Core Concepts](#core-concepts)
4. [Data Structures](#data-structures)
5. [LLM Integration Strategy](#llm-integration-strategy)
6. [Player Interaction Flow](#player-interaction-flow)
7. [UI Specifications](#ui-specifications)
8. [Implementation Phases](#implementation-phases)
9. [Forest Content Examples](#forest-content-examples)
10. [Technical Considerations](#technical-considerations)

---

## Overview

### Vision
Create a narrative RPG where:
- **LLM generates actions** based on game system constraints (skills, environment, outcomes)
- **Chain-of-Thought reasoning is visible** as skill "personalities" (like Disco Elysium)
- **Skills have unique voices** that narrate observations and reasoning
- **Player agency** comes from choosing which skill perspective to explore

### Key Differences from Current System
| Current (LocationBlueprint) | New (NarrationNode CoT) |
|----------------------------|-------------------------|
| Location-based with state categories | Narration node graph |
| Director LLM generates 4-6 actions | Multi-step: Observe → Think → Act |
| Single action selection | Keyword exploration with thinking limit |
| Generic narrative voice | Skill-based narrative personalities |
| Linear flow | Player-driven skill perspective selection |

### Design Pillars
1. **Coherence**: LLM finds connections between avatar skills and environment
2. **Personality**: Each skill has unique narrative voice (cached system prompts)
3. **Agency**: Player chooses which skills to consult before acting
4. **Constraint**: Game system ensures outcomes fit world state

---

## Design Clarifications

This section documents key design decisions clarified during initial planning:

### Body Parts & Skills
- **Phase 6 Scope**: Body parts tracked but not used in skill calculations. Skill levels are static (1-10), used only for action skill checks.
- **Future Feature**: Body part levels will define skill level bounds and XP progression rates. Successful skill checks will grant XP toward level-ups.

### Humors
- **Phase 6 Scope**: Track humor changes from outcomes, no mechanical effects yet (placeholder).
- **Future Feature**: Humors will affect skill check probabilities, unlock/lock certain actions or thinking skills, and modify narrative tone availability.

### LLM Generation Logic
- **Outcome Selection**: When thinking skill generates 2-5 actions, LLM selects most coherent outcome match for each action from the keyword's possible outcomes list.
- **Action Skill Selection**: LLM can choose ANY of the avatar's action skills but must thoughtfully pick most appropriate skills (constrained to 2-5 total actions).
- **Failure Outcomes**: Predefined list of generic failures (humor changes). LLM evaluator scores each for coherence with the failed action. Highest scoring failure is selected (similar to existing CriticEvaluator pattern).

### Data Architecture
- **Skills as C# Classes**: data/skills.csv is deprecated. Each skill is a concrete C# class inheriting from abstract `Skill` base class.
- **Persona Prompts**: Hardcoded in Observation and Thinking skill classes. Action skills have no prompts (only used for skill checks).
- **Skill Registry**: Central `SkillRegistry.cs` provides queries by function, category, ID.

### Player Constraints
- **Exhausted Thinking Attempts**: When player uses all 3 thinking attempts, keywords become disabled. Player MUST pick one action from current thinking block (cannot abandon).
- **Non-Transition Outcomes**: When outcome is NOT a new narration node (e.g., acquire item only), thinking skill generates final narration from its persona POV, shows "Continue" button, exits to WorldView.

### Integration Strategy
- **Mode 5 Replacement**: Phase 6 completely replaces current LocationBlueprint-based Mode 5.
- **Acquired Resources**: 
  - Items & Companions: Placeholder lists in Avatar class, no immediate effects
  - Skills: Proper skill instances immediately available for next narration phase

### Error Handling
- **GBNF Constraints**: All LLM requests use GBNF to prevent malformed JSON. Action arrays constrained to 2-5 items minimum.
- **Keyword Fallback**: Two-tier approach:
  1. First attempt: Prompt LLM to use keywords, no GBNF constraint on narration text (natural)
  2. Fallback: If <3 keywords returned, use GBNF with keyword intro examples (simple strings like "There is moss") to force inclusion

### Outcome System
- **Outcome Representation**: Outcomes converted to natural language strings using predefined formats:
  - Learn skill: `"learn {skill.name}"`
  - Acquire item: `"acquire {item.name}"`
  - Transition: `"transition {node_id}"`
  - Humor change: `"increase {humor} by {amount}"`
- **Outcome Parsing**: System uses predefined format patterns to parse strings back into concrete actions
- **Shared Outcomes**: Multiple keywords can reference the same outcome
- **One Outcome Per Action**: Each generated action has exactly one preselected outcome

### Skill System Details
- **No Categories**: Skills do NOT have category property. Only functions (Observation/Thinking/Action) and body part associations matter.
- **Initial Levels**: Random distribution (1-10) for all learned skills
- **Multi-Function Skills**: Some skills have 2+ functions (e.g., Mycology = Observation + Thinking). Future may support all 3 functions on single skill.

### UI/UX Behavior
- **Keyword Interaction**: No keyword descriptions. Hover = subtle color highlight. Click = show thinking skill selection popup (single click).
- **Action Text Format**: LLM generates actions starting with "try to ..." (GBNF constrained). UI displays only the part after "try to ".
- **Observation Rendering**: Draw each observation block immediately as generated. Only highlight/enable keywords after ALL observation blocks complete. Scroll starts at top.

### Technical Details
- **Outcome Narration**: Thinking skill that generated the action narrates the outcome from its persona POV (not neutral Narrator).
- **Slot Persistence**: Skill slots persist across narration nodes. Only system prompts are cached, not conversation history.
- **Failure Evaluation**: Reuse existing CriticEvaluator code, adapt to new system. Same yes/no probability pattern, Slot 50.

---

## Core Concepts

### Narration Node
Represents a discrete narrative context (a location within a location, a specific scene).

**Properties**:
- **Keywords**: 5-10 notable elements in the scene (e.g., "rustling leaves", "moss-covered stone", "bird call")
- **Outcomes by Keyword**: Each keyword maps to 2-5 possible outcomes
- **Entry Node Flag**: Can this be the first node when entering a forest?
- **Transitions**: Which nodes can this lead to?

**Outcomes** include:
- Transition to another narration node
- Acquire item
- Learn new skill
- Gain animal companion
- Increase/decrease body humors (placeholder for now)

### Avatar
Defined by:
- **Body Parts**: 17 parts with levels (Lower Limbs, Eyes, Cerebrum, etc.)
  - Future feature: Body part levels will define skill level bounds and XP progression rates
  - Phase 6: Body parts tracked but not used in skill check calculations
- **Skills**: ~50 learned skills (from bank of 300)
  - ~10 Observation skills (generate perceptions)
  - ~20 Thinking skills (generate reasoning + actions)
  - ~20 Action skills (used for skill checks)
  - Each skill has individual level (1-10)
  - Future feature: Successful skill checks grant XP, level up at threshold
  - Phase 6: Skill levels static, only used for action skill checks
- **Humors**: 10 humors with quantities (placeholder for Phase 6)
  - Black Bile, Yellow Bile, Appetitus, Melancholia, Ether
  - Phlegm, Blood, Voluptas, Laetitia, Euphoria
  - Future feature: Affect skill check probabilities, unlock/lock actions, modify narrative availability
  - Phase 6: Track changes from outcomes, no mechanical effects yet
- **Inventory**: Placeholder list for acquired items
- **Companions**: Placeholder list for acquired companions

### Skills
Each skill has:
- **Name**: e.g., "Observation", "Algebraic Analysis", "Brute Force"
- **Function Tags**: observation, thinking, action (some skills have multiple)
- **Body Part Associations**: 1-2 body parts (from data/skills.csv)
- **Level**: Used for skill checks (action skills only)
- **Persona Prompt**: Unique system prompt defining skill's "voice" (cached in LLM)

**Example Skills**:
- **Observation** (Eyes + Ears): Perceives visual and auditory details
- **Algebraic Analysis** (Cerebrum + Anamnesis): Cold, pattern-obsessed reasoning
- **Brute Force** (Upper Limbs + Lower Limbs): Direct, physical solutions
- **Opportunism** (Cerebrum + Heart): Spots advantageous moments
- **Mycology** (Eyes + Nose): Specialized knowledge of fungi

---

## Data Structures

### NarrationNode
```csharp
public record NarrationNode(
    string NodeId,                                      // "forest_clearing_01"
    string NodeName,                                    // "A Sun-Dappled Clearing"
    string NeutralDescription,                          // Base scene description (no skill voice)
    List<string> Keywords,                              // ["rustling leaves", "moss", "bird call", ...]
    Dictionary<string, List<Outcome>> OutcomesByKeyword, // keyword → possible outcomes
    bool IsEntryNode,                                   // Can be first node in location?
    List<string> PossibleTransitions                    // Node IDs this can lead to
);

public record Outcome(
    OutcomeType Type,                                   // Transition, Item, Skill, Companion, Humor
    string TargetId,                                    // NodeId, ItemId, SkillId, CompanionId
    Dictionary<string, int>? HumorChanges               // Optional humor deltas
)
{
    // Convert outcome to natural language string for LLM
    public string ToNaturalLanguageString() => Type switch
    {
        OutcomeType.Transition => $"transition {TargetId}",
        OutcomeType.Item => $"acquire {TargetId}",
        OutcomeType.Skill => $"learn {TargetId}",
        OutcomeType.Companion => $"befriend {TargetId}",
        OutcomeType.Humor => string.Join(", ", HumorChanges!.Select(kvp => 
            $"{(kvp.Value > 0 ? "increase" : "decrease")} {kvp.Key} by {Math.Abs(kvp.Value)}")),
        _ => "unknown outcome"
    };
    
    // Parse natural language string back to outcome
    public static Outcome FromNaturalLanguageString(string outcomeStr, List<Outcome> possibleOutcomes)
    {
        // Match against possible outcomes by their string representation
        return possibleOutcomes.First(o => o.ToNaturalLanguageString() == outcomeStr);
    }
};

public enum OutcomeType
{
    Transition,      // Move to new narration node
    Item,            // Acquire item
    Skill,           // Learn new skill
    Companion,       // Gain animal companion
    Humor            // Only humor changes (no concrete outcome)
}
```

### Avatar
```csharp
public class Avatar
{
    public Dictionary<string, int> BodyPartLevels { get; init; }  // 17 body parts, level 1-10
    public List<Skill> LearnedSkills { get; set; }                // 50 skills max
    public Dictionary<string, int> Humors { get; set; }           // 10 humors, 0-100 range
    public Inventory Inventory { get; set; }                      // Items (future)
    public List<string> Companions { get; set; }                  // Animal companions (future)
    
    // Helper queries
    public List<Skill> GetObservationSkills() => 
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Observation)).ToList();
    
    public List<Skill> GetThinkingSkills() => 
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Thinking)).ToList();
    
    public List<Skill> GetActionSkills() => 
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Action)).ToList();
}
```

### Skill
```csharp
public abstract class Skill
{
    public abstract string SkillId { get; }           // "observation", "algebraic_analysis"
    public abstract string DisplayName { get; }       // "Observation", "Algebraic Analysis"
    public abstract SkillFunction[] Functions { get; } // Can have multiple functions (1-3)
    public abstract string[] BodyParts { get; }       // Associated body parts (1-2)
    public int Level { get; set; }                    // 1-10, used for skill checks (random initial)
    
    // Only for Observation and Thinking skills
    public virtual string? PersonaPrompt => null;     // Hardcoded LLM system prompt
}

// Example implementation
public class ObservationSkill : Skill
{
    public override string SkillId => "observation";
    public override string DisplayName => "Observation";
    public override SkillFunction[] Functions => new[] { SkillFunction.Observation };
    public override string[] BodyParts => new[] { "Eyes", "Ears" };
    
    public override string PersonaPrompt => @"
You are the inner voice of OBSERVATION, the avatar's ability to perceive their environment.

You are methodical, detail-oriented, and precise. You notice things others miss: the texture of surfaces, the direction of light, the exact distance between objects. You describe what you see in concrete, measurable terms. You count, measure, estimate. You note colors, shapes, sizes.

You do not interpret or theorize. You simply report what the eyes and ears detect. You are the foundation upon which other skills build their reasoning.

When narrating, you speak in clear, factual sentences. No flowery language. No metaphors. Just observations.";
}

public enum SkillFunction
{
    Observation,   // Generates perceptions of environment
    Thinking,      // Generates reasoning and actions
    Action         // Used for skill checks when executing actions
}
```

### NarrationState
```csharp
public record NarrationState(
    string CurrentNodeId,
    int ThinkingAttemptsRemaining,            // Starts at 3, decrements on keyword click
    string? SelectedKeyword,                   // Currently exploring keyword
    string? SelectedThinkingSkill,             // Currently consulting thinking skill
    List<ParsedAction>? GeneratedActions,      // Actions from thinking process
    List<NarrationBlock> NarrationHistory      // All narration blocks (for display)
);

public record NarrationBlock(
    NarrationBlockType Type,                   // Observation, Thinking, Action, Outcome
    string SkillName,                          // Which skill narrated this
    string Content,                            // The narration text
    List<string>? Keywords,                    // Highlighted keywords (if observation)
    List<ParsedAction>? Actions                // Clickable actions (if thinking)
);

public enum NarrationBlockType
{
    Observation,   // Skill perceives environment
    Thinking,      // Skill reasons about actions
    Action,        // Player-selected action
    Outcome        // Result of action (success/failure)
}
```

---

## LLM Integration Strategy

### Slot Management Architecture

**Key Principles**:
1. **One slot per skill** - Each skill gets dedicated LLM conversation slot
2. **Cached system prompts** - Skill persona prompts are cached (never change)
3. **No conversation history** - Each request is standalone (skill prompt + current request only)
4. **Stateless evaluation** - Yes/no evaluations use separate slot

**Slot Allocation**:
```
Slot 0-9:   Observation skills (10 max)
Slot 10-29: Thinking skills (20 max)
Slot 30-49: Action skills (20 max) [currently unused, reserved for future]
Slot 50:    Critic (yes/no evaluations)
Slot 51:    Narrator (outcome descriptions) [reuses existing Narrator]
```

**Benefits**:
- **Performance**: System prompts cached, only send new request each time
- **Consistency**: Each skill maintains personality across entire session
- **Scalability**: 50 skill slots << LlamaServer's slot limit
- **Memory**: No context bloat (no conversation history accumulation)
- **Persistence**: Slots persist across narration node transitions (only system prompts cached, not history)

### LLM Request Flow

#### Observation Generation
```csharp
// Select 2-3 observation skills randomly
var observationSkills = avatar.GetObservationSkills().OrderBy(_ => rng.Next()).Take(3);

// Generate and display blocks progressively
foreach (var skill in observationSkills)
{
    int slotId = skillToSlotMapping[skill.SkillId];
    
    // First time: Create instance with cached persona prompt
    if (!activeSlots.Contains(slotId))
    {
        await llamaServer.CreateInstanceAsync(skill.PersonaPrompt, slotId);
        activeSlots.Add(slotId);
    }
    
    // Build request (no conversation history, just this request)
    var observationRequest = BuildObservationRequest(
        neutralDescription: currentNode.NeutralDescription,
        keywords: currentNode.Keywords,
        avatarState: avatar
    );
    
    // Constrain output: must include 3-5 keywords from node's keyword list
    var schema = CreateObservationSchema(currentNode.Keywords);
    string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
    
    // Request observation
    var response = await llamaServer.ContinueRequestAsync(
        slotId,
        observationRequest,
        gbnfGrammar: gbnf
    );
    
    var observation = ParseObservationResponse(response);
    var block = new NarrationBlock(
        Type: NarrationBlockType.Observation,
        SkillName: skill.DisplayName,
        Content: observation.NarrationText,
        Keywords: observation.ExtractedKeywords,
        Actions: null
    );
    narrationState.AddBlock(block);
    
    // Immediately render this block (keywords not yet clickable)
    await ui.RenderNarrationBlock(block, keywordsEnabled: false);
}

// All observation blocks complete - now enable keyword interaction
await ui.EnableKeywords(narrationState.GetAllKeywords());
```

#### Thinking Generation
```csharp
// Player clicked keyword, selected thinking skill from popup
var thinkingSkill = selectedThinkingSkill;
var keyword = selectedKeyword;
int slotId = skillToSlotMapping[thinkingSkill.SkillId];

// First time: Create instance with cached persona prompt
if (!activeSlots.Contains(slotId))
{
    await llamaServer.CreateInstanceAsync(thinkingSkill.PersonaPrompt, slotId);
    activeSlots.Add(slotId);
}

// Get possible outcomes for this keyword
var possibleOutcomes = currentNode.OutcomesByKeyword[keyword];

// Build thinking request
var thinkingRequest = BuildThinkingRequest(
    keyword: keyword,
    node: currentNode,
    possibleOutcomes: possibleOutcomes,
    actionSkills: avatar.GetActionSkills(), // LLM can choose ANY, must pick most appropriate
    avatarState: avatar
);

// Instructions to LLM:
// - Generate 2-5 actions (enforced by GBNF minItems/maxItems)
// - For each action, select most coherent outcome from possibleOutcomes list
// - For each action, select most appropriate action skill from avatar's action skills
// - Actions should follow logically from CoT reasoning

// Constrain output: CoT reasoning + list of (action_skill, outcome, action_text)
var schema = CreateThinkingSchema(avatar.GetActionSkills(), possibleOutcomes);
string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

// Request thinking
var response = await llamaServer.ContinueRequestAsync(
    slotId,
    thinkingRequest,
    gbnfGrammar: gbnf
);

var thinking = ParseThinkingResponse(response);
narrationState.AddBlock(new NarrationBlock(
    Type: NarrationBlockType.Thinking,
    SkillName: thinkingSkill.DisplayName,
    Text: thinking.ReasoningText,
    Keywords: null,
    Actions: thinking.GeneratedActions // 2-5 actions, each with preselected outcome
));
```

#### Action Outcome Evaluation
```csharp
// Reuse existing Critic system (Slot 50)
// ActionScorer and ActionDifficultyEvaluator already handle this

var score = await actionScorer.ScoreActionAsync(selectedAction, context);
if (score < threshold) {
    // Action fails automatically (too incoherent)
    return FailureOutcome;
}

var difficulty = await actionDifficultyEvaluator.EvaluateDifficultyAsync(selectedAction, context);
bool success = RollSkillCheck(selectedAction.RelatedSkill.Level, difficulty);

if (success) {
    var outcome = selectedAction.PreselectedOutcome;
    // Thinking skill generates outcome narration from its persona POV
    var outcomeNarration = await GenerateOutcomeNarration(
        thinkingSkill: selectedAction.ThinkingSkill,
        action: selectedAction,
        outcome: outcome,
        context: context
    );
    return (outcome, outcomeNarration);
} else {
    var failureOutcome = await DetermineFailureOutcome(selectedAction, context);
    var failureNarration = await GenerateOutcomeNarration(
        thinkingSkill: selectedAction.ThinkingSkill,
        action: selectedAction,
        outcome: failureOutcome,
        context: context
    );
    return (failureOutcome, failureNarration);
}

// Failure outcome determination
async Task<Outcome> DetermineFailureOutcome(ParsedAction action, NarrationContext context)
{
    // Predefined list of generic failure outcomes
    var genericFailures = new List<Outcome>
    {
        new Outcome(OutcomeType.Humor, HumorChanges: new() { ["Black Bile"] = +3 }), // Frustration
        new Outcome(OutcomeType.Humor, HumorChanges: new() { ["Yellow Bile"] = +2 }), // Irritation
        new Outcome(OutcomeType.Humor, HumorChanges: new() { ["Phlegm"] = +2 }),      // Resignation
        new Outcome(OutcomeType.Humor, HumorChanges: new() { ["Melancholia"] = +1 }), // Disappointment
        // Add more generic failures as needed
    };
    
    // Use LLM evaluator to score each failure for coherence
    // Similar to existing CriticEvaluator pattern:
    // - Ask yes/no: "Is [failure] a coherent consequence of [action] in [context]?"
    // - Sum yes probabilities for each failure
    // - Return failure with highest yes probability average
    
    var failureScores = new Dictionary<Outcome, float>();
    foreach (var failure in genericFailures)
    {
        // Reuse existing CriticEvaluator pattern (Slot 50)
        // Ask yes/no: "Is [failure] coherent with [action] in [context]?"
        var coherenceScore = await criticEvaluator.EvaluateCoherenceAsync(action, failure, context);
        failureScores[failure] = coherenceScore;
    }
    
    return failureScores.OrderByDescending(kvp => kvp.Value).First().Key;
}

// Outcome narration from thinking skill's POV
async Task<string> GenerateOutcomeNarration(
    Skill thinkingSkill, 
    ParsedAction action, 
    Outcome outcome, 
    NarrationContext context)
{
    int slotId = skillToSlotMapping[thinkingSkill.SkillId];
    
    var narratorRequest = BuildOutcomeNarrationRequest(
        action: action,
        outcome: outcome,
        context: context,
        thinkingSkillPersona: thinkingSkill.DisplayName
    );
    
    var response = await llamaServer.ContinueRequestAsync(
        slotId,  // Use thinking skill's slot (with its cached persona prompt)
        narratorRequest,
        gbnfGrammar: SimpleStringSchema(minLength: 50, maxLength: 200)
    );
    
    return response.Trim();
}

// Note: Adapt existing CriticEvaluator to work with NarrationNode context instead of LocationBlueprint
```

### JSON Schemas

#### Observation Schema
```csharp
var observationSchema = new CompositeField("ObservationResponse", new JsonField[]
{
    new StringField("narration_text", minLength: 50, maxLength: 300),
    new ArrayField("highlighted_keywords", 
        new ChoiceField<string>("keyword", currentNode.Keywords.ToArray()),
        minLength: 3,
        maxLength: 5
    )
});

// Keyword constraint strategy:
// 1. First attempt: Prompt LLM to use keywords, but NO GBNF constraint on narration_text
//    - More natural narration
//    - LLM might fail to include keywords
// 2. Fallback (if first attempt returns 0 keywords): Use GBNF with keyword intro examples
//    - Each keyword has intro_example: "moss" => "There is moss"
//    - Constrain narration_text to start with one of the intro examples
//    - Forces keyword inclusion but less natural

async Task<ObservationResponse> GenerateObservationWithKeywordFallback(
    Skill observationSkill, 
    NarrationNode node)
{
    // First attempt: Natural narration, prompted but not constrained
    var response = await llamaServer.ContinueRequestAsync(
        slotId: GetSlotForSkill(observationSkill),
        prompt: BuildObservationPrompt(node, promptKeywordUsage: true),
        gbnfGrammar: GenerateGBNF(observationSchema) // Still constrains JSON structure
    );
    
    var parsed = JsonSerializer.Deserialize<ObservationResponse>(response);
    
    if (parsed.HighlightedKeywords.Count >= 3)
    {
        return parsed; // Success!
    }
    
    // Fallback: Use keyword intro examples to force inclusion
    var keywordIntros = node.Keywords.Select(k => node.KeywordIntroExamples[k]).ToList();
    var constrainedSchema = CreateObservationSchemaWithIntros(observationSchema, keywordIntros);
    
    response = await llamaServer.ContinueRequestAsync(
        slotId: GetSlotForSkill(observationSkill),
        prompt: BuildObservationPrompt(node, promptKeywordUsage: true),
        gbnfGrammar: GenerateGBNF(constrainedSchema) // Now constrains narration to start with intro
    );
    
    return JsonSerializer.Deserialize<ObservationResponse>(response);
}
```

**Example Output**:
```json
{
  "narration_text": "You notice the way light filters through the canopy, casting dappled shadows on the forest floor. The rustling leaves speak of wind patterns, of air currents moving through the branches. Moss clings to the north side of stones, a testament to moisture and shade.",
  "highlighted_keywords": ["rustling leaves", "moss", "dappled shadows"]
}
```

#### Thinking Schema
```csharp
var thinkingSchema = new CompositeField("ThinkingResponse", new JsonField[]
{
    new StringField("reasoning_text", minLength: 100, maxLength: 400),
    new ArrayField("actions",
        new CompositeField("action", new JsonField[]
        {
            new ChoiceField<string>("action_skill", actionSkillNames),
            new ChoiceField<string>("outcome_id", outcomeIds),
            new PrefixedStringField("action_description", prefix: "try to ", minLength: 30, maxLength: 160)
            // UI displays only the part after "try to "
        }),
        minLength: 2,  // GBNF enforces at least 2 actions
        maxLength: 5   // GBNF enforces at most 5 actions
    )
});
```

**Example Output**:
```json
{
  "reasoning_text": "The moss indicates a moist microclimate. Brute Force could tear it away to check for hidden crevices. Observation could examine the growth patterns for age and water flow. Mycology would identify edible species.",
  "actions": [
    {
      "action_skill": "brute_force",
      "outcome": "acquire rare_mushroom",
      "action_description": "try to tear away the moss to expose what lies beneath the stone"
    },
    {
      "action_skill": "observation",
      "outcome": "transition hidden_stream",
      "action_description": "try to follow the moss growth pattern downhill to trace water flow"
    },
    {
      "action_skill": "mycology",
      "outcome": "acquire rare_mushroom",
      "action_description": "try to carefully harvest the pale mushrooms growing in the moss"
    }
  ]
}
```

**Note**: UI displays actions without the "try to " prefix:
- `> [Brute Force] tear away the moss to expose what lies beneath the stone`
- `> [Observation] follow the moss growth pattern downhill to trace water flow`
- `> [Mycology] carefully harvest the pale mushrooms growing in the moss`

---

## Player Interaction Flow

### State Machine

```
┌─────────────────┐
│  ENTER NODE     │
│ (entry node or  │
│  from outcome)  │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────┐
│  OBSERVATION GENERATION     │
│ - Select 2-3 observation    │
│   skills randomly           │
│ - Generate narration blocks │
│ - Highlight keywords        │
│ - Set thinking attempts = 3 │
└────────┬────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  KEYWORD EXPLORATION        │
│ Player can:                 │
│ - Hover keywords (popup)    │
│ - Click keyword (if         │
│   attempts > 0)             │
│ - Scroll narration          │
└────────┬────────────────────┘
         │ keyword clicked
         ▼
┌─────────────────────────────┐
│  THINKING SKILL SELECTION   │
│ - Show popup with ~20       │
│   thinking skills           │
│ - Player selects one        │
│ - Decrement thinking        │
│   attempts                  │
└────────┬────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  THINKING GENERATION        │
│ - LLM generates CoT         │
│   reasoning + actions       │
│ - Display thinking block    │
│ - Show 2-5 clickable        │
│   actions                   │
└────────┬────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  ACTION SELECTION           │
│ Player can:                 │
│ - Click different keyword   │
│   (if attempts > 0)         │
│ - Click action to execute   │
│                             │
│ If attempts = 0:            │
│ - Keywords disabled         │
│ - MUST pick one action      │
│   from current thinking     │
└────────┬────────────────────┘
         │ action clicked
         ▼
┌─────────────────────────────┐
│  SKILL CHECK & EVALUATION   │
│ - ActionScorer (coherence)  │
│ - ActionDifficultyEvaluator │
│ - Roll skill check          │
│ - Determine outcome         │
└────────┬────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  OUTCOME NARRATION          │
│ - Narrator generates text   │
│ - Apply state changes       │
│   (items, humors, etc.)     │
│ - Display outcome block     │
└────────┬────────────────────┘
         │
         ▼
    Is outcome a
    transition to
    new node?
         │
    ┌────┴────┐
    │ YES     │ NO
    ▼         ▼
┌─────────────────┐  ┌─────────────────────────────┐
│  CLEAR UI       │  │  FINAL NARRATION            │
│  Loop to        │  │  - Thinking skill generates │
│  ENTER NODE     │  │    outcome narration in its │
│  (new node)     │  │    persona voice             │
└─────────────────┘  │  - Show "Continue" button   │
                     │  - Click → Exit to WorldView│
                     └─────────────────────────────┘
```

### Thinking Attempt Limit

**Visual Representation**:
```
Thinking Attempts: [██████] [██████] [██████]
                      3         2         1
```

After 3 keyword explorations, player must commit to one of the generated actions (cannot explore more keywords).

**UI Behavior**:
- Keywords become unclickable when attempts = 0
- Keywords show grayed-out styling
- Tooltip shows "No thinking attempts remaining"

---

## UI Specifications

### Main Terminal Layout (100x30 grid)

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│ Forest Exploration - The Clearing                                          Thinking: [██] [██] [██] │ Line 0-1 (Header)
├──────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                  │
│ [OBSERVATION]                                                                                    │
│ You notice the way light filters through the canopy, casting dappled shadows on the forest      │
│ floor. The rustling leaves speak of wind patterns, of air currents moving through the           │ Lines 2-16 (Scrollable Narration)
│ branches. Moss clings to the north side of stones.                                              │
│                                                                                                  │
│ [VISUAL ANALYSIS]                                                                                │
│ The clearing measures approximately 20 meters across. Seven distinct tree species form the      │
│ perimeter. A moss-covered stone sits at the center, roughly 60cm in diameter.                   │
│                                                                                                  │
│ [ALGEBRAIC ANALYSIS]                                                                             │
│ The moss indicates a moist microclimate. Variables: stone position (center), moss coverage      │
│ (north side), light patterns (dappled). Possible transformations: physical force on stone,      │
│ mycological extraction from moss, trajectory analysis following water flow.                      │
│                                                                                                  │
│   > [Brute Force] tear away the moss to expose what lies beneath the stone                     │
│   > [Observation] follow the moss growth pattern downhill to trace water flow                   │ Lines 17-28 (Actions)
│   > [Mycology] carefully harvest the pale mushrooms growing in the moss                         │
│                                                                                                  │
├──────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Hover keywords to highlight • Click keywords to think (3 attempts remaining)                    │ Line 29 (Status)
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

**Note**: Action text displayed without "try to " prefix (removed from LLM output)

### Color Scheme

| Element | Foreground | Background | Notes |
|---------|-----------|------------|-------|
| Skill headers | Yellow | Black | Bright, attention-grabbing |
| Narration text | Gray70 | Black | Readable, neutral |
| Keywords | Cyan | Black | Highlighted, clickable |
| Keywords (hover) | White | DarkGray | Obvious hover state |
| Keywords (disabled) | Gray30 | Black | Grayed out when attempts = 0 |
| Actions | White | Black | Clickable |
| Actions (hover) | Yellow | DarkGray | Matches current system |
| Action prefix `>` | Gray50 | Black | Visual separator |
| Skill in action | Green | Black | `[Brute Force]` |
| Status bar | Gray50 | Black | Subtle instructions |

### Keyword Hover Behavior

**Hover State**:
- Keyword color changes slightly (e.g., Cyan → White)
- No popup on hover
- Visual feedback only

**Click Behavior**:
- Single click on keyword → show thinking skill selection popup immediately
- No intermediate description display

### Popup Terminal (Thinking Skill Selection)

```
┌─────────────────────────────┐
│ Select Thinking Skill       │
│─────────────────────────────│
│ > Observation               │
│ > Algebraic Analysis        │
│ > Visual Analysis           │
│ > Mycology                  │
│ > Opportunism               │
│ > Logic                     │
│ > Intuition                 │
│ > Pattern Recognition       │
│   ...                       │
│                             │
│ [ESC to cancel]             │
└─────────────────────────────┘
```

**Behavior**:
- Appears when player clicks a keyword (single click)
- Shows all ~20 thinking skills
- Scrollable if needed (max 20 lines)
- Click skill → generate thinking narration, decrement thinking attempts
- ESC → cancel, no thinking attempt consumed
- **No keyword descriptions**: Keywords have no stored descriptions (not in node data)

### Keyword Interaction

**Hover State**:
- Keyword color changes from Cyan to White
- No popup on hover, only visual feedback
- Indicates keyword is clickable

**Click Behavior**:
- Single click → immediately show thinking skill selection popup
- No intermediate description step
- One-step interaction for better flow

### Scrolling Behavior

**Narration Area**: Lines 2-16 (15 lines visible)
- Each observation block rendered immediately as generated
- Keywords NOT clickable until all observation blocks complete
- If narration blocks exceed 15 lines, track scroll offset
- Mouse wheel scrolls narration
- New blocks always added at bottom
- Scroll position starts at top
- Auto-scroll to bottom when new block added (optional)

**Action Area**: Lines 17-28 (12 lines available)
- Actions from current thinking block only (2-5 actions)
- No scrolling needed (max 5 actions × 2 lines each = 10 lines)

---

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
**Goal**: Core data structures and skill definitions

**Tasks**:
1. Create `Avatar.cs` class
   - Body part levels (hardcoded initial values, 17 parts)
   - Skill collection with levels
   - Humor tracking (10 humors, placeholder)
   - Inventory placeholder (List<string>)
   - Companions placeholder (List<string>)
2. Create `Skill.cs` abstract class
   - Abstract base class with SkillId, DisplayName, Functions, BodyParts, Level, PersonaPrompt
   - Create concrete skill classes: ~10 Observation, ~20 Thinking, ~20 Action
   - Hardcode persona prompts in Observation and Thinking skill classes
   - Action skills have no persona prompts (only used for skill checks)
3. Create `SkillRegistry.cs`
   - Central registry of all available skills
   - Methods to query skills by function, category, ID
4. Create `NarrationNode.cs` record
   - NarrationNode, Outcome, OutcomeType
   - Add KeywordIntroExamples dictionary for fallback
5. Create `NarrationState.cs` record
   - NarrationState, NarrationBlock, enums
6. Create `ForestNarrationNodeGenerator.cs`
   - Generates 10 forest nodes with keywords and outcomes
   - Entry node selection logic

**Deliverable**: Data layer complete, can instantiate all core objects

**Note**: data/skills.csv is now deprecated and replaced by C# skill class definitions

---

### Phase 2: Observation System (Week 3-4)
**Goal**: LLM-generated observations with highlighted keywords

**Tasks**:
1. Create `ObservationPromptConstructor.cs`
   - Takes skill + node → builds observation request
   - Includes neutral description, keywords, avatar state
2. Create `ObservationExecutor.cs`
   - Manages observation skill slots (0-9)
   - Creates instances with cached persona prompts
   - Implements keyword fallback strategy (natural first, constrained if failed)
   - Parses JSON responses
3. Create `KeywordRenderer.cs`
   - Parse narration text, identify keywords
   - Store clickable regions (character-level spans)
   - Render with color coding
4. Update `TerminalLocationUI.cs`
   - Support inline clickable keywords (not just actions)
   - Track `List<KeywordRegion>` during rendering
   - Detect clicks on keywords

**Deliverable**: Enter node → see 2-3 observation narrations with clickable keywords

---

### Phase 3: Thinking System (Week 5-6)
**Goal**: Player selects thinking skill → LLM generates CoT + actions

**Tasks**:
1. Create `ThinkingSkillPopup.cs`
   - Shows popup at mouse position
   - Lists ~20 thinking skills
   - Returns selected skill or null (ESC)
2. Create `ThinkingPromptConstructor.cs`
   - Takes skill + keyword + outcomes + action skills → builds thinking request
   - Includes constraints (must pick from outcome list, action skill list)
3. Create `ThinkingExecutor.cs`
   - Manages thinking skill slots (10-29)
   - Creates instances with cached persona prompts
   - Sends thinking requests with GBNF constraints
   - Parses JSON responses (reasoning + actions)
4. Update `NarrationState.cs`
   - Track thinking attempts remaining (3)
   - Track selected keyword + thinking skill
5. Update `TerminalLocationUI.cs`
   - Render thinking attempt progress bar
   - Disable keywords when attempts = 0
   - Display thinking narration blocks
   - Display generated actions (clickable)

**Deliverable**: Click keyword → select skill → see CoT reasoning + 3-6 actions

---

### Phase 4: Action Execution (Week 7-8)
**Goal**: Execute actions with skill checks and outcomes

**Tasks**:
1. Integrate existing `ActionScorer.cs` and `ActionDifficultyEvaluator.cs`
   - Reuse Critic evaluation (Slot 50)
   - Adapt to NarrationNode context (instead of LocationBlueprint)
2. Create `OutcomeApplicator.cs`
   - Applies outcome to NarrationState and Avatar
   - Handles transitions, items, skills, companions, humors
3. Create `NarratorExecutor.cs` (or reuse existing Narrator)
   - Generates atmospheric outcome narration
   - Takes action + outcome → narrative description
4. Update `TerminalLocationUI.cs`
   - Clear narration blocks after action outcome (if new node)
   - Display outcome narration block
   - Show state changes (item acquired, etc.)

**Deliverable**: Click action → skill check → outcome narration → state change

---

### Phase 5: UI Polish (Week 9-10)
**Goal**: Smooth UX, animations, feedback

**Tasks**:
1. Implement scrolling for narration area
   - Track scroll offset
   - Mouse wheel support
   - Auto-scroll to new blocks
2. Create transition animations
   - Skill check progress bar (simulated dice roll)
   - Success/failure reveal (fade-in, color flash)
   - Typewriter effect for narration (optional)
3. Improve popup visuals
   - Box shadows (using layered terminals)
   - Better positioning (avoid screen edges)
   - Show skill descriptions in popup
4. Add loading indicators
   - Reuse existing loading animation system
   - Show spinner during LLM requests
   - "Thinking..." text with ellipsis animation
5. Add keyboard shortcuts
   - ESC to cancel popup
   - Number keys to select thinking skills (1-9)
   - Arrow keys to scroll narration

**Deliverable**: Polished, responsive UI with smooth feedback

---

### Phase 6: Content & Balancing (Week 11-12)
**Goal**: Create full forest location, tune difficulty

**Tasks**:
1. Create 10 forest narration nodes
   - Write neutral descriptions
   - Define 5-10 keywords per node
   - Map 2-5 outcomes per keyword
   - Define transitions (graph connectivity)
2. Write skill persona prompts
   - 10 observation skills (full prompts)
   - 20 thinking skills (full prompts)
   - 20 action skills (name + body parts only, no prompts needed)
3. Populate forest items/companions
   - Plants: mushrooms, berries, herbs
   - Companions: bird, fox, squirrel
   - Skills: Mycology, Botany, Tracking
4. Tune skill check probabilities
   - Test action difficulty evaluation
   - Adjust success rates (currently 40-95% based on difficulty)
   - Balance humor changes
5. Playtest and iterate
   - Test all node transitions
   - Verify keyword-outcome coherence
   - Check for dead ends or infinite loops

**Deliverable**: Fully playable forest location with 10 nodes

---

## Forest Content Examples

### Node 1: Sun-Dappled Clearing (Entry Node)

**Neutral Description**:
> A circular clearing opens in the dense forest, approximately twenty meters across. Sunlight streams through gaps in the canopy, creating pools of golden light on the leaf-strewn ground. Seven ancient oaks form the perimeter, their gnarled roots breaking through the earth. At the center rests a moss-covered stone, weathered smooth by centuries of rain.

**Keywords**:
1. `dappled shadows` - light patterns on ground
2. `moss-covered stone` - central feature
3. `ancient oaks` - perimeter trees
4. `leaf litter` - ground covering
5. `bird calls` - ambient sound
6. `rustling leaves` - canopy movement
7. `tree roots` - exposed root systems

**Outcomes by Keyword**:

`moss-covered stone`:
- **Transition** → "Hidden Stream" (following moss moisture gradient)
- **Item** → Rare Mushroom (mycological examination)
- **Companion** → Curious Squirrel (disturb stone, squirrel emerges from beneath)
- **Humor** → Increase Laetitia +5 (satisfaction from discovery)

`ancient oaks`:
- **Transition** → "Hollow Oak" (inspect tree, find entrance)
- **Skill** → Dendrology (study tree ring patterns, learn tree knowledge)
- **Item** → Oakwood Branch (useful for crafting)
- **Humor** → Increase Melancholia +3 (contemplate tree's age, your own mortality)

`bird calls`:
- **Transition** → "Berry Bramble" (follow birdsong to food source)
- **Companion** → Songbird (whistle in response, bird approaches)
- **Skill** → Ornithology (identify species by call)
- **Humor** → Increase Blood +5 (uplifted by song)

`tree roots`:
- **Transition** → "Rock Outcrop" (follow roots uphill to bedrock)
- **Item** → Grub Larvae (dig in roots, find protein)
- **Humor** → Increase Appetitus +2 (hunger from seeing food)

`leaf litter`:
- **Item** → Medicinal Herb (sift through leaves)
- **Humor** → Increase Phlegm +3 (meditative repetitive action)

**Possible Transitions**:
- Hidden Stream (following water)
- Hollow Oak (tree exploration)
- Berry Bramble (following birds)
- Rock Outcrop (uphill terrain)
- Dense Thicket (pushing through underbrush) - not keyword-linked, only as failure consequence

---

### Node 2: Hidden Stream

**Neutral Description**:
> A narrow stream cuts through the forest floor, its clear water flowing over smooth river stones. The banks are thick with ferns and reeds. Small fish dart between stones. The sound of flowing water fills the air, peaceful and constant. Dragonflies hover above the surface.

**Keywords**:
1. `flowing water` - stream current
2. `smooth stones` - riverbed
3. `darting fish` - aquatic life
4. `ferns and reeds` - bank vegetation
5. `dragonflies` - insects
6. `water clarity` - visibility of streambed

**Outcomes by Keyword**:

`flowing water`:
- **Transition** → "Pond" (follow stream downstream)
- **Skill** → Hydrodynamics (understand flow patterns)
- **Humor** → Increase Laetitia +4 (soothing sound)

`darting fish`:
- **Item** → Fresh Fish (catch with hands or improvised trap)
- **Companion** → Otter (observe fishing technique, otter appears)
- **Humor** → Increase Appetitus +3 (hunger watching fish)

`smooth stones`:
- **Item** → Skipping Stone (perfect flat stone)
- **Skill** → Geology (identify stone types)
- **Transition** → "Rock Outcrop" (follow stone trail uphill)

`ferns and reeds`:
- **Item** → Reed Bundle (useful for weaving)
- **Skill** → Botany (study wetland plants)

**Possible Transitions**:
- Pond (downstream)
- Rock Outcrop (upstream/uphill)
- Sun-Dappled Clearing (return)
- Dense Thicket (away from stream)

---

### Node 3: Hollow Oak

**Neutral Description**:
> One of the ancient oaks has a cavity in its trunk, large enough for a person to enter. Inside, the hollow extends upward into darkness, and downward into the root system. The wood is dry and papery. Scratch marks cover the interior walls. The air smells of old wood and animal musk.

**Keywords**:
1. `hollow cavity` - entrance
2. `scratch marks` - animal signs
3. `upward darkness` - climbing potential
4. `root chamber` - underground space
5. `animal musk` - scent clue
6. `papery wood` - decomposition

**Outcomes by Keyword**:

`scratch marks`:
- **Companion** → Raccoon (investigate, find sleeping raccoon)
- **Skill** → Tracking (identify animal from marks)
- **Humor** → Yellow Bile +2 (unease at predator signs)

`upward darkness`:
- **Transition** → "Canopy Overlook" (climb upward, emerge at canopy level)
- **Humor** → Ether +3 (vertigo, disorientation)

`root chamber`:
- **Item** → Buried Cache (dig in roots, find old traveler's stash)
- **Transition** → "Mushroom Circle" (roots connect to mycelial network)
- **Humor** → Black Bile +2 (confined space, darkness)

`animal musk`:
- **Companion** → Fox (follow scent, find den)
- **Skill** → Olfactory Analysis (develop scent-based tracking)

**Possible Transitions**:
- Canopy Overlook (upward)
- Mushroom Circle (root network)
- Sun-Dappled Clearing (exit hollow)
- Den (if following animal)

---

### Example Skill Persona Prompts

#### Observation (Observation Skill)
```
You are the inner voice of OBSERVATION, the avatar's ability to perceive their environment.

You are methodical, detail-oriented, and precise. You notice things others miss: the texture of surfaces, the direction of light, the exact distance between objects. You describe what you see in concrete, measurable terms. You count, measure, estimate. You note colors, shapes, sizes.

You do not interpret or theorize. You simply report what the eyes and ears detect. You are the foundation upon which other skills build their reasoning.

When narrating, you speak in clear, factual sentences. No flowery language. No metaphors. Just observations.
```

#### Algebraic Analysis (Thinking Skill)
```
You are the inner voice of ALGEBRAIC ANALYSIS, a cold, abstract, pattern-obsessed way of thinking.

You perceive the world as variables, constraints, systems, transformations, inputs and outputs. You do not care about emotions, beauty, or intent. You constantly try to reduce situations to symbolic relations, mappings, optimizations, equivalences, and edge cases.

When reasoning about actions, you explain how unrelated skills might still fit the same underlying mathematical structure. You enjoy forcing coherence where none is obvious. You find elegant solutions by treating everything as an optimization problem.

You speak in analytical, detached, slightly pedantic terms. You use words like "variable", "constraint", "transformation", "mapping", "optimization", "equivalence".
```

#### Brute Force (Action Skill)
```
You are the inner voice of BRUTE FORCE, the avatar's capacity for direct physical power.

You believe in simple solutions: push, pull, break, smash. Why think when you can act? Why plan when you can just do it? Obstacles are meant to be overcome through raw strength.

You do not reason or think. You are an action skill. Your only role is to be invoked by thinking skills when they generate actions. You have no persona prompt for observation or thinking.

[Note: Action skills do not generate narration. This prompt exists only for documentation.]
```

#### Mycology (Multi-Function Skill)
```
You are the inner voice of MYCOLOGY, specialized knowledge of fungi.

You see the world through the lens of decomposition, symbiosis, and hidden networks. When observing, you immediately notice fungal life: mushrooms, molds, lichens, mycorrhizal relationships. You recognize edible vs. poisonous species instantly.

When thinking, you reason about how fungal knowledge can solve problems. Mushrooms indicate soil quality, moisture, season. Mycelial networks connect distant parts of the forest. Some fungi are medicinal, others psychoactive.

You speak with quiet expertise. You use precise taxonomic language. You appreciate the beauty of decomposition, the elegance of symbiosis.
```

---

### Example Observation Generation

**Input**:
- Node: Sun-Dappled Clearing
- Skill: Observation
- Keywords: [dappled shadows, moss-covered stone, ancient oaks, leaf litter, bird calls, rustling leaves, tree roots]

**Observation Skill Prompt** (cached):
```
You are the inner voice of OBSERVATION...
[full prompt from above]
```

**Request** (not cached, sent each time):
```
You are in this environment:

A circular clearing opens in the dense forest, approximately twenty meters across. Sunlight streams through gaps in the canopy, creating pools of golden light on the leaf-strewn ground. Seven ancient oaks form the perimeter, their gnarled roots breaking through the earth. At the center rests a moss-covered stone, weathered smooth by centuries of rain.

Generate a narration of your observations. Your narration must include 3 to 5 of these notable elements:
- dappled shadows
- moss-covered stone
- ancient oaks
- leaf litter
- bird calls
- rustling leaves
- tree roots

Respond in JSON format:
{
  "narration_text": "string (50-300 characters)",
  "highlighted_keywords": ["keyword1", "keyword2", ...]
}
```

**LLM Response**:
```json
{
  "narration_text": "The clearing measures approximately twenty meters across, bordered by seven distinct oak specimens. At the center, a moss-covered stone sits partially embedded in the earth, roughly sixty centimeters in diameter. Dappled shadows shift across the leaf litter as wind moves through the canopy above.",
  "highlighted_keywords": ["moss-covered stone", "ancient oaks", "dappled shadows", "leaf litter"]
}
```

---

### Example Thinking Generation

**Input**:
- Keyword: "moss-covered stone"
- Thinking Skill: Algebraic Analysis
- Possible Outcomes: [Transition to Hidden Stream, Item: Rare Mushroom, Companion: Curious Squirrel, Humor: Laetitia +5]
- Action Skills: [Brute Force, Observation, Mycology, Athletics, Stealth, ...]

**Algebraic Analysis Skill Prompt** (cached):
```
You are the inner voice of ALGEBRAIC ANALYSIS...
[full prompt from above]
```

**Request** (not cached):
```
The avatar is focusing on: moss-covered stone

Context: A circular clearing opens in the dense forest, approximately twenty meters across. Sunlight streams through gaps in the canopy, creating pools of golden light on the leaf-strewn ground. Seven ancient oaks form the perimeter, their gnarled roots breaking through the earth. At the center rests a moss-covered stone, weathered smooth by centuries of rain.

Possible outcomes if actions succeed:
1. transition_to_stream: Following moisture gradient to water source
2. rare_mushroom: Discovering fungal growth in moss
3. curious_squirrel: Disturbing stone reveals squirrel den
4. satisfaction: Feeling of discovery

Available action skills:
- Brute Force
- Observation  
- Mycology
- Athletics
- Stealth
- Logic
- Investigation

Generate a brief chain-of-thought reasoning explaining how these action skills could achieve the possible outcomes, then propose 3-6 concrete actions.

Respond in JSON format:
{
  "reasoning_text": "string (100-400 characters)",
  "actions": [
    {
      "action_skill": "one of the available action skills",
      "outcome_id": "one of the outcome IDs above",
      "action_description": "string (20-150 characters) starting with 'You'"
    }
  ]
}
```

**LLM Response**:
```json
{
  "reasoning_text": "The moss-covered stone is a variable in a moisture-optimization problem. Moss growth indicates high humidity, directional water flow. Transformations available: physical displacement (Brute Force), detailed analysis (Observation), mycological sampling (Mycology). Each transformation maps to different outcome states in the system.",
  "actions": [
    {
      "action_skill": "Brute Force",
      "outcome_id": "curious_squirrel",
      "action_description": "You grip the stone's edges and wrench it upward, applying maximum force to the constraint."
    },
    {
      "action_skill": "Observation",
      "outcome_id": "transition_to_stream",
      "action_description": "You trace the moss growth vector downslope, following the moisture gradient to its source."
    },
    {
      "action_skill": "Mycology",
      "outcome_id": "rare_mushroom",
      "action_description": "You examine the moss-stone interface for fungal specimens, optimizing for edible species."
    },
    {
      "action_skill": "Investigation",
      "outcome_id": "satisfaction",
      "action_description": "You systematically document the stone's position, orientation, and moss distribution patterns."
    }
  ]
}
```

---

## Technical Considerations

### Performance Optimization

**LLM Slot Caching**:
- Each skill's persona prompt cached by llama.cpp
- Typical prompt size: 200-400 tokens
- Context size: 4096 tokens
- Request size: 500-800 tokens (description + constraints + JSON schema)
- Response size: 200-500 tokens
- **Total per request**: ~700-1300 tokens (well within context limit)

**Request Latency** (estimated):
- Observation generation: 3 skills × 5s = 15s
- Thinking generation: 1 skill × 8s = 8s
- Action evaluation: Critic × 3s = 3s
- Outcome narration: Narrator × 5s = 5s
- **Total per action cycle**: ~30s

**Mitigation Strategies**:
- Background LLM preloading (start Narrator while showing actions)
- Loading animations (already implemented in LocationTravelGameController)
- Streaming tokens for immediate visual feedback
- Player reads narration while LLM generates (parallel activities)

### Error Handling

**LLM Failures**:
- JSON parse errors → retry once, then fallback to hardcoded action
- Timeout (>30s) → show error message, offer retry or skip
- Invalid keyword selection → log warning, continue
- Invalid outcome ID → log warning, use random outcome

**State Corruption**:
- Always validate NarrationState transitions
- Track previous state for rollback
- Log all state changes for debugging

**Graph Issues**:
- Dead-end nodes → always have at least one "return to previous" outcome
- Disconnected nodes → validate graph connectivity on startup
- Missing outcomes → log error, use generic "nothing happens" outcome

### Memory Management

**Skill Slot Retention**:
- Keep observation/thinking slots alive for entire session (cached prompts)
- Total memory: 50 slots × 4096 context × 2 bytes/token ≈ 400KB
- Negligible compared to model size (2-4GB)

**Narration History**:
- Limit to last 50 blocks (roughly 10-15 actions)
- Older blocks trimmed automatically
- Full history saved to log file for debugging

### Testing Strategy

**Unit Tests**:
- NarrationNode graph connectivity validation
- Outcome application logic
- Keyword extraction and rendering
- JSON schema generation/parsing

**Integration Tests**:
- Full observation → thinking → action → outcome cycle
- LLM slot management (create, reuse, reset)
- UI interaction (click keyword, select skill, click action)

**Playtesting**:
- 5 complete playthroughs of forest location
- Test all 10 nodes, all transitions
- Verify all outcomes achievable
- Check for softlocks or dead ends

---

## Migration from LocationBlueprint

### Deprecated Systems
- `LocationBlueprint.cs` - replaced by `NarrationNode.cs`
- `LocationGenerator.cs` - replaced by `ForestNarrationNodeGenerator.cs`
- `StateCategory` - no longer needed (states handled by node graph)
- `LocationContent` - outcomes now embedded in nodes

### Preserved Systems
- `LlamaServerManager` - unchanged
- `ActionScorer` / `ActionDifficultyEvaluator` - adapted to NarrationNode context
- `TerminalLocationUI` - extended for inline keywords
- `PopupTerminalHUD` - new usage for skill selection
- `Critic` - unchanged (still used for evaluation)

### New Systems
- `NarrationNode` graph system
- `Avatar` with skills and humors
- `ObservationExecutor` / `ThinkingExecutor`
- `KeywordRenderer`
- `ThinkingSkillPopup`
- `OutcomeApplicator`
- `ForestNarrationNodeGenerator`

---

## Success Criteria

### Minimum Viable Product (End of Phase 4)
- ✅ Enter forest location
- ✅ See 2-3 observation narrations with different skill voices
- ✅ Click keyword, select thinking skill
- ✅ See CoT reasoning + 3-6 actions
- ✅ Click action, see skill check + outcome
- ✅ Transition to new node OR exit location
- ✅ Thinking attempt limit enforced (3 max)

### Full Feature Complete (End of Phase 6)
- ✅ 10 forest nodes with unique descriptions
- ✅ 50+ keywords across all nodes
- ✅ 30+ skill prompts (10 observation, 20 thinking)
- ✅ All transitions tested and working
- ✅ Scrolling, animations, polished UI
- ✅ Balanced difficulty (skill checks feel fair)
- ✅ No dead ends or softlocks

### Quality Benchmarks
- LLM coherence: 80%+ of generated actions are contextually appropriate (via playtesting)
- Performance: 90%+ of LLM requests complete within 15s
- UI responsiveness: All clicks/hovers respond within 100ms
- Graph coverage: All 10 nodes reachable from entry nodes

---

## Appendix: 10 Forest Nodes

1. **Sun-Dappled Clearing** (Entry) - Central meeting point
2. **Hidden Stream** - Water source, fishing opportunity
3. **Hollow Oak** - Vertical exploration, animal den
4. **Berry Bramble** - Food source, thorny obstacle
5. **Mushroom Circle** - Mycological focus, eerie atmosphere
6. **Rock Outcrop** - High ground, geology, vista
7. **Dense Thicket** - Difficult terrain, rare plants
8. **Pond** - Still water, reflection, aquatic life
9. **Canopy Overlook** - Aerial view, bird's-eye perspective
10. **Forest Edge** (Exit) - Transition to plains/road, leaving forest

Each node has 5-10 keywords, 2-5 outcomes per keyword, and connects to 2-4 other nodes.

---

**End of Design Document**
