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
            new TemplateStringField("what_do_i_feel_and_observe",
                Template: "I <generated>",
                MinGenLength: 20,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }

    /// <summary>
    /// Continuation observation schema — no forced 'I ' prefix.
    /// Used for transition and focus sentences after the first sentence in a batch.
    /// </summary>
    public static CompositeField CreateContinuationObservationSchema()
    {
        return new CompositeField("ObservationResponse",
            new TemplateStringField("what_do_i_feel_and_observe",
                Template: "<generated>",
                MinGenLength: 20,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }
    
    #endregion
    
    #region Thinking Schemas

    /// <summary>
    /// Call 1 (WHY): thinking modusMentis explains why observing the keyword makes it want the outcome.
    /// </summary>
    public static CompositeField CreateWhySchema()
    {
        return new CompositeField("WhyResponse",
            new TemplateStringField("what_do_i_think",
                Template: "I <generated>",
                MinGenLength: 20,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }

    /// <summary>
    /// Call 2 (HOW): thinking modusMentis picks which action skill to use and briefly explains.
    /// </summary>
    /// <param name="validMeans">List of "with X" approach strings the LLM can choose from</param>
    public static CompositeField CreateHowSchema(List<string> validMeans)
    {
        return new CompositeField("HowResponse",
            new TemplateStringField("how_could_i_do_it",
                Template: "<generated>",
                MinGenLength: 20,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 100),
            new ChoiceField<string>("selected_approach", validMeans.ToArray())
        );
    }

    /// <summary>
    /// Call 3 (WHAT): action modusMentis describes concretely what it will attempt.
    /// </summary>
    public static CompositeField CreateWhatSchema()
    {
        return new CompositeField("WhatResponse",
            new TemplateStringField("action_description",
                Template: "try to <generated>.",
                MinGenLength: 10,
                MaxGenLength: 300,
                Hint: "In a few words, what exactly will you try to do?")
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
            new TemplateStringField("what_happened",
                Template: "I <generated>",
                MinGenLength: 30,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }
    
    #endregion
    
    #region Action Outcome Schemas
    
    /// <summary>
    /// Schema for action outcome from Director LLM.
    /// Includes success status, narrative, state changes, sublocation changes, items gained, and interaction end flag.
    /// </summary>
    /// <param name="stateCategories">Available state categories and their possible states</param>
    /// <param name="accessibleSublocations">Sublocations the protagonist can move to</param>
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
