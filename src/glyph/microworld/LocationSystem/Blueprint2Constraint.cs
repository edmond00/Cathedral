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
    /// </summary>
    /// <param name="blueprint">The location blueprint defining structure and rules</param>
    /// <param name="currentSublocation">Player's current sublocation ID</param>
    /// <param name="currentStates">Current active states mapped by category ID</param>
    /// <param name="numberOfActions">Number of action choices to generate (default: 7)</param>
    /// <returns>Array field defining valid action choices structure</returns>
    public static JsonField GenerateActionConstraints(
        LocationBlueprint blueprint,
        string currentSublocation,
        Dictionary<string, string> currentStates,
        int numberOfActions = 7)
    {
        if (blueprint == null)
            throw new ArgumentNullException(nameof(blueprint));
        if (string.IsNullOrEmpty(currentSublocation))
            throw new ArgumentException("Current sublocation cannot be null or empty", nameof(currentSublocation));
        if (currentStates == null)
            throw new ArgumentNullException(nameof(currentStates));
        if (!blueprint.Sublocations.ContainsKey(currentSublocation))
            throw new ArgumentException($"Sublocation '{currentSublocation}' not found in blueprint", nameof(currentSublocation));
        if (numberOfActions < 1 || numberOfActions > 20)
            throw new ArgumentException("Number of actions must be between 1 and 20", nameof(numberOfActions));

        // Define the structure of a single action
        var singleActionField = new CompositeField("Action",
            new TemplateStringField("action_text", "try to <generated>", 10, 280),  // LLM-generated action with "try to" prefix
            GenerateSuccessConstraints(blueprint, currentSublocation, currentStates),
            GenerateFailureConstraints(),                    // LLM-generated failure consequences
            new ChoiceField<string>("related_skill", GetAvailableSkills()),
            new ChoiceField<int>("difficulty", 1, 2, 3, 4, 5)
        );

        // Wrap in an array field that generates exactly the specified number of actions
        return new CompositeField("ActionChoices",
            new ArrayField("actions", singleActionField, numberOfActions, numberOfActions)
        );
    }

    /// <summary>
    /// Generates constraints for successful action consequences
    /// </summary>
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

    /// <summary>
    /// Generates constraints for failure consequences (LLM has creative freedom here)
    /// </summary>
    private static JsonField GenerateFailureConstraints()
    {
        return new CompositeField("failure_consequences",
            new ChoiceField<string>("type", "damage", "lost", "injured", "startled_wildlife", "equipment_loss", "exhaustion", "none"),
            new StringField("description", 5, 50)
        );
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
            return new OptionalField("state_changes",
                new VariantField("state_change", availableStateChanges.ToArray()));
        }
        else
        {
            return new OptionalField("state_changes", new ConstantStringField("no_change", "none"));
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
            ? new OptionalField("sublocation_change",
                new ChoiceField<string>("sublocation_change", accessibleSublocations.ToArray()))
            : new OptionalField("sublocation_change", new ConstantStringField("no_movement", "none"));
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
            ? new OptionalField("item_gained",
                new ChoiceField<string>("item_gained", availableItems))
            : new OptionalField("item_gained", new ConstantStringField("no_item", "none"));
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
            ? new OptionalField("companion_gained",
                new ChoiceField<string>("companion_gained", availableCompanions))
            : new OptionalField("companion_gained", new ConstantStringField("no_companion", "none"));
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

        return availableQuests.Any()
            ? new OptionalField("quest_gained",
                new ChoiceField<string>("quest_gained", availableQuests))
            : new OptionalField("quest_gained", new ConstantStringField("no_quest", "none"));
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
    /// Returns available skills for action constraints
    /// </summary>
    private static string[] GetAvailableSkills()
    {
        return new[]
        {
            "strength", "dexterity", "constitution",
            "intelligence", "wisdom", "charisma",
            "athletics", "stealth", "perception",
            "survival", "nature_lore", "tracking",
            "navigation", "climbing", "swimming"
        };
    }
}

/// <summary>
/// Simple constant string field implementation for Blueprint2Constraint
/// </summary>
public record ConstantStringField(string Name, string Value) : JsonField(Name)
{
    public string Value { get; init; } = Value ?? throw new ArgumentNullException(nameof(Value));
}