using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Glyph.Microworld.LocationSystem;

/// <summary>
/// Converts location blueprints into JSON constraints for LLM action generation
/// Analyzes current game state to generate appropriate action possibilities
/// </summary>
public static class Blueprint2Constraint
{
    /// <summary>
    /// Generates JSON field constraints for LLM action generation based on current game state
    /// Each action gets a pre-determined success consequence and 5 skill candidates to choose from
    /// </summary>
    /// <param name="blueprint">The location blueprint defining structure and rules</param>
    /// <param name="currentSublocation">Player's current sublocation ID</param>
    /// <param name="currentStates">Current active states mapped by category ID</param>
    /// <param name="skillCandidates">Array of 5-skill arrays, one set of candidates per action</param>
    /// <param name="numberOfActions">Number of action choices to generate (default: 7)</param>
    /// <returns>Array field defining valid action choices structure</returns>
    public static JsonField GenerateActionConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates,
        string[][] skillCandidates,
        int numberOfActions = 7)
    {
        if (blueprint == null)
            throw new ArgumentNullException(nameof(blueprint));
        if (string.IsNullOrEmpty(currentSublocation))
            throw new ArgumentException("Current sublocation cannot be null or empty", nameof(currentSublocation));
        if (currentStates == null)
            throw new ArgumentNullException(nameof(currentStates));
        if (skillCandidates == null || skillCandidates.Length == 0)
            throw new ArgumentException("Skill candidates array cannot be null or empty", nameof(skillCandidates));
        if (skillCandidates.Length != numberOfActions)
            throw new ArgumentException($"Skill candidates array must have exactly {numberOfActions} elements", nameof(skillCandidates));
        if (skillCandidates.Any(sc => sc == null || sc.Length != 5))
            throw new ArgumentException("Each skill candidate set must contain exactly 5 skills", nameof(skillCandidates));
        if (!blueprint.Sublocations.ContainsKey(currentSublocation))
            throw new ArgumentException($"Sublocation '{currentSublocation}' not found in blueprint", nameof(currentSublocation));
        if (numberOfActions < 1 || numberOfActions > 20)
            throw new ArgumentException("Number of actions must be between 1 and 20", nameof(numberOfActions));

        // Generate all possible success consequences once
        var allSuccessConsequences = GenerateAllSuccessConsequences(blueprint, currentSublocation, currentStates);
        
        // Create individual action fields with pre-determined outcomes
        var actionFields = new JsonField[numberOfActions];
        var rng = new Random();
        
        for (int i = 0; i < numberOfActions; i++)
        {
            // Sample a random success consequence for this action (simple string label)
            var consequenceIndex = rng.Next(allSuccessConsequences.Count);
            var consequenceLabel = allSuccessConsequences[consequenceIndex];
            
            // Format skill candidates as a readable string for hints
            string skillCandidatesStr = string.Join(", ", skillCandidates[i]);
            
            actionFields[i] = new CompositeField($"action_{i + 1}",
                // 1. Success consequence (constant string, not chosen by LLM)
                new InlineConstantStringField("success_consequence", consequenceLabel, 
                    $"what happens if the action succeeds")
                { 
                    RuleName = $"success_consequence_{i + 1}" // Unique GBNF rule per action
                },
                
                // 2. Related skill (LLM chooses from candidates)
                // CRITICAL: Keep JSON field name as "related_skill" but use unique GBNF rule name
                new ChoiceField<string>("related_skill", skillCandidates[i], "choose the most appropriate skill that could achieve this consequence") 
                { 
                    RuleName = $"related_skill_{i + 1}" // Unique GBNF rule per action
                },
                
                // 3. Action text (LLM generates based on consequence and skill)
                new TemplateStringField("action_text", "try to <generated>", 5, 100, 
                    "Generate a concrete action (6-12 words) using the chosen skill that could lead to the consequence. Must be possible in the current location. Write in 2nd person.")
            );
        }

        // Use TupleField to create a fixed-length array where each position has a specific skill
        return new CompositeField("ActionChoices",
            new TupleField("actions", actionFields)
        );
    }

    /// <summary>
    /// Generates a list of all possible success consequence labels (simple strings)
    /// Each consequence is returned as a simple string label (1-3 words)
    /// </summary>
    private static List<string> GenerateAllSuccessConsequences(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var consequences = new List<string>();

        // State change consequences - use the Label from LocationState
        foreach (var (categoryId, category) in blueprint.StateCategories)
        {
            if (CanInfluenceStateCategory(blueprint, currentSublocation, categoryId))
            {
                var possibleStates = GetAccessibleStates(category, currentStates);
                foreach (var stateId in possibleStates)
                {
                    var state = category.PossibleStates[stateId];
                    consequences.Add(state.Label);
                }
            }
        }

        // Movement consequences - use "enter {name}" or "reach {name}"
        var currentSubLocation = blueprint.Sublocations[currentSublocation];
        var accessibleSublocations = new List<string>();
        
        if (blueprint.SublocationConnections.ContainsKey(currentSublocation))
        {
            foreach (var connectedId in blueprint.SublocationConnections[currentSublocation])
            {
                var connected = blueprint.Sublocations[connectedId];
                if (CanAccessSublocation(connected, currentStates))
                {
                    accessibleSublocations.Add(connectedId);
                }
            }
        }
        
        foreach (var (sublocationId, sublocation) in blueprint.Sublocations)
        {
            if (sublocation.ParentSublocationId == currentSublocation &&
                CanAccessSublocation(sublocation, currentStates))
            {
                accessibleSublocations.Add(sublocationId);
            }
        }
        
        if (currentSubLocation.ParentSublocationId != null)
        {
            accessibleSublocations.Add(currentSubLocation.ParentSublocationId);
        }
        
        foreach (var sublocationId in accessibleSublocations)
        {
            var sublocation = blueprint.Sublocations[sublocationId];
            // Create a short label like "enter grove" or "reach path"
            var shortName = SimplifyLocationName(sublocation.Name);
            consequences.Add($"enter {shortName}");
        }

        // Item gain consequences - use "gain {item}"
        var availableItems = GetAvailableContent(blueprint, currentSublocation, currentStates)
            .SelectMany(content => content.AvailableItems)
            .Distinct()
            .ToList();
        
        foreach (var item in availableItems)
        {
            consequences.Add($"gain {item}");
        }

        // Companion gain consequences - use "meet {companion}"
        var availableCompanions = GetAvailableContent(blueprint, currentSublocation, currentStates)
            .SelectMany(content => content.AvailableCompanions)
            .Distinct()
            .ToList();
        
        foreach (var companion in availableCompanions)
        {
            consequences.Add($"meet {companion}");
        }

        // Quest gain consequences - use "discover {quest}"
        var availableQuests = GetAvailableContent(blueprint, currentSublocation, currentStates)
            .SelectMany(content => content.AvailableQuests)
            .Distinct()
            .ToList();
        
        foreach (var quest in availableQuests)
        {
            consequences.Add($"discover {quest}");
        }

        // Always include "nothing happens" option
        consequences.Add("nothing happens");

        return consequences;
    }

    /// <summary>
    /// Simplifies a location name to 1-2 words for consequence labels
    /// Examples: "Ancient Forest Path" -> "path", "Main Grove" -> "grove"
    /// </summary>
    private static string SimplifyLocationName(string name)
    {
        // Take the last word if multi-word, or the full name if single word
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? words[^1].ToLower() : name.ToLower();
    }

    /// <summary>
    /// Gets all possible failure consequence types for LLM to choose from
    /// </summary>
    private static string[] GetFailureConsequenceOptions()
    {
        return new[] { 
            "injured", 
            "lost", 
            "equipment_loss", 
            "exhaustion", 
            "attacked", 
            "disease", 
        };
    }

    /// <summary>
    /// OLD METHOD - Generates constraints for successful action consequences
    /// Returns a VariantField where LLM must choose ONE consequence type
    /// KEPT FOR REFERENCE - NOT USED ANYMORE
    /// </summary>
    private static JsonField GenerateSuccessConstraints_OLD(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var variants = new List<CompositeField>();

        // State change consequence
        var stateChangeField = GenerateCategorizedStateChangeConstraints(blueprint, currentSublocation, currentStates);
        if (stateChangeField is VariantField stateVariant && stateVariant.Variants.Length > 1)
        {
            // If there are actual state change options (not just "none"), add them as separate variants
            foreach (var variant in stateVariant.Variants)
            {
                if (variant.Name != "no_state_change")
                {
                    variants.Add(new CompositeField($"success_{variant.Name}",
                        new InlineConstantStringField("consequence_type", variant.Name, "identifies which type of consequence occurred"),
                        variant.Fields[0], // category field
                        variant.Fields[1]  // new_state field
                    ));
                }
            }
        }

        // Movement consequence
        var sublocationField = GenerateHierarchicalSublocationChangeConstraints(blueprint, currentSublocation, currentStates);
        if (sublocationField is ChoiceField<string> sublocationChoice && sublocationChoice.Options.Length > 1)
        {
            variants.Add(new CompositeField("success_movement",
                new InlineConstantStringField("consequence_type", "movement", "identifies which type of consequence occurred"),
                sublocationField
            ));
        }

        // Item gained consequence
        var itemField = GenerateItemGainConstraints(blueprint, currentSublocation, currentStates);
        if (itemField is ChoiceField<string> itemChoice && itemChoice.Options.Length > 1)
        {
            variants.Add(new CompositeField("success_item",
                new InlineConstantStringField("consequence_type", "item_gained", "identifies which type of consequence occurred"),
                itemField
            ));
        }

        // Companion gained consequence
        var companionField = GenerateCompanionGainConstraints(blueprint, currentSublocation, currentStates);
        if (companionField is ChoiceField<string> companionChoice && companionChoice.Options.Length > 1)
        {
            variants.Add(new CompositeField("success_companion",
                new InlineConstantStringField("consequence_type", "companion_gained", "identifies which type of consequence occurred"),
                companionField
            ));
        }

        // Quest gained consequence
        var questField = GenerateQuestGainConstraints(blueprint, currentSublocation, currentStates);
        if (questField is ChoiceField<string> questChoice && questChoice.Options.Length > 1)
        {
            variants.Add(new CompositeField("success_quest",
                new InlineConstantStringField("consequence_type", "quest_gained", "identifies which type of consequence occurred"),
                questField
            ));
        }

        // Always add a "none" option
        variants.Add(new CompositeField("success_none",
            new InlineConstantStringField("consequence_type", "none", "identifies which type of consequence occurred")
        ));

        return new VariantField("success_consequences", variants.ToArray(), "what happens if this action succeeds - choose ONE consequence type among: state change, movement, item gained, companion gained, quest gained, or none");
    }



    /// <summary>
    /// Generates state change constraints based on what state categories can be influenced
    /// from the current sublocation and state combination
    /// </summary>
    private static JsonField GenerateCategorizedStateChangeConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var availableStateChanges = new List<CompositeField>();

        // Find state categories that can be changed from current sublocation
        foreach (var (categoryId, category) in blueprint.StateCategories)
        {
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

        if (availableStateChanges.Any())
        {
            // Add a "no change" option to the variants
            availableStateChanges.Add(new CompositeField("no_state_change",
                new ConstantStringField("category", "none"),
                new ConstantStringField("new_state", "none")
            ));
            
            return new VariantField("state_changes", availableStateChanges.ToArray());
        }
        else
        {
            // If no state changes are available, return a simple "none" field
            return new CompositeField("state_changes",
                new ConstantStringField("category", "none"),
                new ConstantStringField("new_state", "none"));
        }
    }

    /// <summary>
    /// Generates sublocation movement constraints based on hierarchical connections
    /// and current state requirements
    /// </summary>
    private static JsonField GenerateHierarchicalSublocationChangeConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var currentSubLocation = blueprint.Sublocations[currentSublocation];
        var accessibleSublocations = new List<string>();

        // Add directly connected sublocations
        if (blueprint.SublocationConnections.ContainsKey(currentSublocation))
        {
            foreach (var connectedId in blueprint.SublocationConnections[currentSublocation])
            {
                var connected = blueprint.Sublocations[connectedId];
                if (CanAccessSublocation(connected, currentStates))
                {
                    accessibleSublocations.Add(connectedId);
                }
            }
        }

        // Add child sublocations (one level down in hierarchy)
        foreach (var (sublocationId, sublocation) in blueprint.Sublocations)
        {
            if (sublocation.ParentSublocationId == currentSublocation &&
                CanAccessSublocation(sublocation, currentStates))
            {
                accessibleSublocations.Add(sublocationId);
            }
        }

        // Add parent sublocation (moving back up hierarchy)
        if (currentSubLocation.ParentSublocationId != null)
        {
            accessibleSublocations.Add(currentSubLocation.ParentSublocationId);
        }

        return accessibleSublocations.Any()
            ? new ChoiceField<string>("sublocation_change", 
                accessibleSublocations.Concat(new[] { "none" }).ToArray())
            : new ConstantStringField("sublocation_change", "none");
    }

    /// <summary>
    /// Generates item gain constraints based on available content in current sublocation/state
    /// </summary>
    private static JsonField GenerateItemGainConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var availableItems = GetAvailableContent(blueprint, currentSublocation, currentStates)
            .SelectMany(content => content.AvailableItems)
            .Distinct()
            .ToArray();

        return availableItems.Any()
            ? new ChoiceField<string>("item_gained", 
                availableItems.Concat(new[] { "none" }).ToArray())
            : new ConstantStringField("item_gained", "none");
    }

    /// <summary>
    /// Generates companion gain constraints based on available content
    /// </summary>
    private static JsonField GenerateCompanionGainConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var availableCompanions = GetAvailableContent(blueprint, currentSublocation, currentStates)
            .SelectMany(content => content.AvailableCompanions)
            .Distinct()
            .ToArray();

        return availableCompanions.Any()
            ? new ChoiceField<string>("companion_gained", 
                availableCompanions.Concat(new[] { "none" }).ToArray())
            : new ConstantStringField("companion_gained", "none");
    }

    /// <summary>
    /// Generates quest gain constraints based on available content
    /// </summary>
    private static JsonField GenerateQuestGainConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var availableQuests = GetAvailableContent(blueprint, currentSublocation, currentStates)
            .SelectMany(content => content.AvailableQuests)
            .Distinct()
            .ToArray();

        var questOptions = availableQuests.ToList();
        questOptions.Add("none");
        return new ChoiceField<string>("quest_gained", questOptions.ToArray());
    }

    /// <summary>
    /// Determines if a state category can be influenced from the current sublocation
    /// </summary>
    private static bool CanInfluenceStateCategory(
        LocationBlueprint blueprint,
        string currentSublocation,
        string categoryId)
    {
        // Check if this is a location-wide state that can be influenced
        var category = blueprint.StateCategories[categoryId];
        
        if (category.Scope == StateScope.Location)
        {
            // Location-wide states can typically be influenced from most sublocations
            // Add specific logic here based on location type and category
            return true;
        }
        else
        {
            // Sublocation-specific states can only be influenced if we're in the right sublocation
            var sublocation = blueprint.Sublocations[currentSublocation];
            return sublocation.LocalStates.ContainsKey(categoryId);
        }
    }

    /// <summary>
    /// Gets the list of states accessible from a category given current game state
    /// </summary>
    private static List<string> GetAccessibleStates(
        StateCategory category,
        Dictionary<string, string> currentStates)
    {
        var accessibleStates = new List<string>();
        var currentStateInCategory = currentStates.GetValueOrDefault(category.CategoryId, category.DefaultStateId);

        foreach (var (stateId, state) in category.PossibleStates)
        {
            // Don't allow transitioning to the same state
            if (stateId == currentStateInCategory)
                continue;

            // Check if all required states are active
            var requiredSatisfied = state.RequiredStates.All(required => 
                currentStates.Values.Contains(required));

            // Check if no forbidden states are active
            var forbiddenSatisfied = !state.ForbiddenStates.Any(forbidden => 
                currentStates.Values.Contains(forbidden));

            if (requiredSatisfied && forbiddenSatisfied)
            {
                accessibleStates.Add(stateId);
            }
        }

        return accessibleStates;
    }

    /// <summary>
    /// Checks if a sublocation can be accessed given the current state
    /// </summary>
    private static bool CanAccessSublocation(
        Sublocation sublocation,
        Dictionary<string, string> currentStates)
    {
        // Check required states
        var requiredSatisfied = sublocation.RequiredStates.All(required => 
            currentStates.Values.Contains(required));

        // Check forbidden states
        var forbiddenSatisfied = !sublocation.ForbiddenStates.Any(forbidden => 
            currentStates.Values.Contains(forbidden));

        return requiredSatisfied && forbiddenSatisfied;
    }

    /// <summary>
    /// Gets available content for the current sublocation and state combination
    /// </summary>
    private static List<LocationContent> GetAvailableContent(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        var content = new List<LocationContent>();

        if (blueprint.ContentMap.ContainsKey(currentSublocation))
        {
            var sublocationContent = blueprint.ContentMap[currentSublocation];
            
            // Add default content
            if (sublocationContent.ContainsKey("default"))
            {
                content.Add(sublocationContent["default"]);
            }

            // Add state-specific content
            foreach (var state in currentStates.Values)
            {
                if (sublocationContent.ContainsKey(state))
                {
                    content.Add(sublocationContent[state]);
                }
            }
        }

        return content;
    }

    /// <summary>
    /// Returns available skills for action constraints as array
    /// </summary>
    public static string[] GetAvailableSkills()
    {
        return new[]
        {
            "smithing", "crafting", "running",
            "mathematics", "information inquiring", "charm",
            "athletics", "stealth", "perception",
            "survival", "nature_lore", "tracking",
            "navigation", "climbing", "swimming",
            "poetry", "history_lore", "equitation",
            "astronomy", "body language", "mining",
            "poisoning", "coprophilia", "politics",
            "intimidation", "joking", "singing",
            "economy", "business", "management",
        };
    }

    /// <summary>
    /// Returns available skills for action constraints as list (for easier manipulation)
    /// </summary>
    public static List<string> GetAvailableSkillsList()
    {
        return new List<string>(GetAvailableSkills());
    }
}

/// <summary>
/// Simple constant string field implementation for Blueprint2Constraint
/// </summary>
public record ConstantStringField(string Name, string Value) : JsonField(Name)
{
    public string Value { get; init; } = Value ?? throw new ArgumentNullException(nameof(Value));
}