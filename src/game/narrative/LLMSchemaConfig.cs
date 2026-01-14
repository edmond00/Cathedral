using System.Collections.Generic;
using System.Linq;
using Cathedral.LLM.JsonConstraints;
using Cathedral.Glyph.Microworld.LocationSystem;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Centralized configuration for all LLM JSON constraint schemas.
/// Makes it easier to review and edit schema definitions for different LLM response types.
/// </summary>
public static class LLMSchemaConfig
{
    #region Observation Schemas
    
    /// <summary>
    /// Basic observation schema with natural narration.
    /// Used for initial observation attempts without keyword constraints.
    /// </summary>
    public static CompositeField CreateObservationSchema()
    {
        return new CompositeField("ObservationResponse",
            new StringField("narration_text", 
                MinLength: 50, 
                MaxLength: 600, 
                Hint: "A short description of what the avatar observes in the environment")
        );
    }
    
    /// <summary>
    /// Observation schema with keyword intro template for fallback strategy.
    /// Forces the LLM to start with a specific keyword intro to guarantee keyword inclusion.
    /// </summary>
    /// <param name="keywords">Available keywords for the current location</param>
    /// <returns>Schema with template-constrained intro using first keyword</returns>
    public static CompositeField CreateObservationSchemaWithIntros(List<string> keywords)
    {
        if (keywords.Count == 0)
        {
            // Fallback to basic schema if no keywords available
            return CreateObservationSchema();
        }
        
        // Build template with first keyword intro: "You notice {keyword}"
        // The <generated> placeholder tells the LLM where to continue generating
        var firstIntro = $"You notice {keywords.First()}";
        
        return new CompositeField("ObservationResponse",
            new TemplateStringField(
                "narration_text",
                Template: firstIntro + " <generated>",  // Fixed intro + placeholder for LLM generation
                MinGenLength: 50,      // LLM must generate at least 50 more chars after intro
                MaxGenLength: 550      // Up to 550 more chars
            )
        );
    }
    
    #endregion
    
    #region Thinking Schemas
    
    /// <summary>
    /// Schema for thinking/planning responses.
    /// Includes reasoning text and a list of actions with skills, outcomes, and descriptions.
    /// </summary>
    /// <param name="validActionSkills">List of action skill names the avatar can use</param>
    /// <param name="validOutcomes">List of valid outcome keywords for the current situation</param>
    public static CompositeField CreateThinkingSchema(List<string> validActionSkills, List<string> validOutcomes)
    {
        return new CompositeField("ThinkingResponse",
            new StringField("reasoning_text", 
                MinLength: 50, 
                MaxLength: 800, 
                Hint: "A short reasoning process the avatar used to decide on actions"),
            new ArrayField("actions",
                ElementType: new CompositeField("Action",
                    new ChoiceField<string>("action_skill", validActionSkills.ToArray()),
                    new ChoiceField<string>("outcome", validOutcomes.ToArray()),
                    new TemplateStringField("action_description", 
                        Template: "try to <generated>",
                        MinGenLength: 10,
                        MaxGenLength: 400,
                        Hint: "Describe in few words the action the avatar will take to achieve the outcome")
                ),
                MinLength: 2,
                MaxLength: 5
            )
        );
    }
    
    #endregion
    
    #region Outcome Narration Schemas
    
    /// <summary>
    /// Schema for outcome narration text.
    /// Simple narration describing the result of an action.
    /// </summary>
    public static CompositeField CreateOutcomeNarrationSchema()
    {
        return new CompositeField("OutcomeNarration",
            new StringField("narration", 
                MinLength: 50, 
                MaxLength: 800, 
                Hint: "A short narration text describing the outcome of the action")
        );
    }
    
    #endregion
    
    #region Action Outcome Schemas
    
    /// <summary>
    /// Schema for action outcome from Director LLM.
    /// Includes success status, narrative, state changes, sublocation changes, items gained, and interaction end flag.
    /// </summary>
    /// <param name="stateCategories">Available state categories and their possible states</param>
    /// <param name="accessibleSublocations">Sublocations the avatar can move to</param>
    /// <param name="availableItems">Items that can be gained from the action</param>
    public static CompositeField CreateActionOutcomeSchema(
        Dictionary<string, (string[] PossibleStates, string CurrentState)> stateCategories,
        string[] accessibleSublocations,
        string[] availableItems)
    {
        // State changes can be empty or specify a category/state change
        var stateChangeOptions = new List<CompositeField>
        {
            new CompositeField("no_change", 
                new ConstantStringField("category", "none"),
                new ConstantStringField("new_state", "none"))
        };

        // Add possible state changes for each category
        foreach (var (categoryId, data) in stateCategories)
        {
            if (data.PossibleStates.Length > 0)
            {
                stateChangeOptions.Add(new CompositeField($"change_{categoryId}",
                    new ConstantStringField("category", categoryId),
                    new ChoiceField<string>("new_state", data.PossibleStates)));
            }
        }

        // Build the outcome structure
        // Note: new_sublocation can be null or a string value
        // Use ChoiceField to allow "none" as the value (we'll handle null parsing)
        return new CompositeField("ActionOutcome",
            new BooleanField("success"),
            new StringField("narrative", 20, 300),
            new VariantField("state_changes", stateChangeOptions.ToArray()),
            new ChoiceField<string>("new_sublocation", accessibleSublocations.Concat(new[] { "none" }).ToArray()),
            new ArrayField("items_gained", new ChoiceField<string>("item", availableItems.Distinct().ToArray()), 0, 3),
            new BooleanField("ends_interaction")
        );
    }
    
    #endregion
}
