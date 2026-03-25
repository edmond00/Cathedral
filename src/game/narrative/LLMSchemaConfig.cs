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
                Template: "I <generated>.",
                MinGenLength: 20,
                MaxGenLength: 300)
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
                Template: "<generated>.",
                MinGenLength: 20,
                MaxGenLength: 300)
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
        
        // Build template forcing "I notice {keyword}" intro
        var firstIntro = $"I notice {keywords.First()}";
        
        return new CompositeField("ObservationResponse",
            new TemplateStringField(
                "what_do_i_feel_and_observe",
                Template: firstIntro + " <generated>.",
                MinGenLength: 10,
                MaxGenLength: 280
            )
        );
    }
    
    #endregion
    
    #region Thinking Schemas

    /// <summary>
    /// Schema for the reasoning-only first call of the batched thinking pipeline.
    /// </summary>
    public static CompositeField CreateReasoningSchema()
    {
        return new CompositeField("ReasoningResponse",
            new TemplateStringField("what_do_i_think",
                Template: "I <generated>.",
                MinGenLength: 50,
                MaxGenLength: 400)
        );
    }

    /// <summary>
    /// Schema for the intermediate skill-selection call (step 3a).
    /// The outcome is hardcoded; the LLM reasons about which skill fits best, then picks one.
    /// Field order: outcome (hardcoded) → how_my_skills_could_help → which_skill_and_why → selected_skill
    /// </summary>
    public static CompositeField CreateSkillSelectionSchema(List<string> validSkills, string hardcodedOutcome)
    {
        return new CompositeField("SkillSelection",
            new ChoiceField<string>("outcome", new[] { hardcodedOutcome }),
            new TemplateStringField("how_my_skills_could_help",
                Template: "<generated>.",
                MinGenLength: 20,
                MaxGenLength: 300),
            new TemplateStringField("which_skill_and_why",
                Template: "<generated>.",
                MinGenLength: 20,
                MaxGenLength: 300),
            new ChoiceField<string>("selected_skill", validSkills.ToArray())
        );
    }

    /// <summary>
    /// Schema for the action-description call (step 3b).
    /// Both outcome and skill are hardcoded; the LLM only writes the action description.
    /// Field order: outcome (hardcoded) → skill (hardcoded) → action_description
    /// </summary>
    public static CompositeField CreateSingleActionSchema(string hardcodedOutcome, string hardcodedSkill)
    {
        return new CompositeField("Action",
            new ChoiceField<string>("outcome", new[] { hardcodedOutcome }),
            new ChoiceField<string>("skill", new[] { hardcodedSkill }),
            new TemplateStringField("action_description",
                Template: "try to <generated>.",
                MinGenLength: 10,
                MaxGenLength: 200,
                Hint: "Describe in few words the action the protagonist will try to take to achieve the outcome")
        );
    }

    /// <summary>
    /// Schema for thinking/planning responses.
    /// Includes reasoning text and a list of actions with modiMentis, outcomes, and descriptions.
    /// </summary>
    /// <param name="validActionModiMentis">List of action modusMentis names the protagonist can use</param>
    /// <param name="validOutcomes">List of valid outcome keywords for the current situation</param>
    public static CompositeField CreateThinkingSchema(List<string> validActionModiMentis, List<string> validOutcomes)
    {
        return new CompositeField("ThinkingResponse",
            new TemplateStringField("what_do_i_think",
                Template: "I <generated>.",
                MinGenLength: 50,
                MaxGenLength: 498),
            new ArrayField("actions",
                ElementType: new CompositeField("Action",
                    new ChoiceField<string>("action_modusMentis", validActionModiMentis.ToArray()),
                    new ChoiceField<string>("outcome", validOutcomes.ToArray()),
                    new TemplateStringField("action_description", 
                        Template: "try to <generated>.",
                        MinGenLength: 10,
                        MaxGenLength: 200,
                        Hint: "Describe in few words the action the protagonist will try to take to achieve the outcome")
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
            new TemplateStringField("what_happened",
                Template: "I <generated>.",
                MinGenLength: 50,
                MaxGenLength: 350)
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
