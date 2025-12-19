using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Applies outcomes to the game state: learns skills, adds items, transitions nodes, etc.
/// </summary>
public class OutcomeApplicator
{
    /// <summary>
    /// Applies an outcome to the avatar and game state.
    /// </summary>
    public void ApplyOutcome(Outcome outcome, Avatar avatar)
    {
        switch (outcome.Type)
        {
            case OutcomeType.Skill:
                ApplySkillOutcome(outcome, avatar);
                break;

            case OutcomeType.Item:
                ApplyItemOutcome(outcome, avatar);
                break;

            case OutcomeType.Companion:
                ApplyCompanionOutcome(outcome, avatar);
                break;

            case OutcomeType.Humor:
                ApplyHumorOutcome(outcome, avatar);
                break;

            case OutcomeType.Transition:
                // Transition is handled externally by the narrative controller
                // Just log it for now
                Console.WriteLine($"OutcomeApplicator: Transition to {outcome.TargetId}");
                break;

            default:
                Console.WriteLine($"OutcomeApplicator: Unknown outcome type {outcome.Type}");
                break;
        }

        // Apply humor changes if present
        if (outcome.HumorChanges != null)
        {
            ApplyHumorChanges(outcome.HumorChanges, avatar);
        }
    }

    private void ApplySkillOutcome(Outcome outcome, Avatar avatar)
    {
        // Check if skill is already known
        if (avatar.Skills.Any(s => s.SkillId == outcome.TargetId))
        {
            Console.WriteLine($"OutcomeApplicator: Skill {outcome.TargetId} already known");
            return;
        }

        // Get skill from registry
        var skill = SkillRegistry.Instance.GetSkill(outcome.TargetId);
        if (skill != null)
        {
            avatar.Skills.Add(skill);
            Console.WriteLine($"OutcomeApplicator: Learned skill {skill.DisplayName}");
        }
        else
        {
            Console.WriteLine($"OutcomeApplicator: Skill {outcome.TargetId} not found in registry");
        }
    }

    private void ApplyItemOutcome(Outcome outcome, Avatar avatar)
    {
        // Add item to inventory
        avatar.Inventory.Add(outcome.TargetId);
        Console.WriteLine($"OutcomeApplicator: Acquired item {outcome.TargetId}");
    }

    private void ApplyCompanionOutcome(Outcome outcome, Avatar avatar)
    {
        // Add companion
        if (!avatar.Companions.Contains(outcome.TargetId))
        {
            avatar.Companions.Add(outcome.TargetId);
            Console.WriteLine($"OutcomeApplicator: Befriended companion {outcome.TargetId}");
        }
    }

    private void ApplyHumorOutcome(Outcome outcome, Avatar avatar)
    {
        // Pure humor changes - already handled by ApplyHumorChanges below
        Console.WriteLine($"OutcomeApplicator: Applying humor-only outcome");
    }

    private void ApplyHumorChanges(Dictionary<string, int> humorChanges, Avatar avatar)
    {
        foreach (var change in humorChanges)
        {
            if (avatar.Humors.TryGetValue(change.Key, out int currentValue))
            {
                int newValue = Math.Clamp(currentValue + change.Value, 0, 100);
                avatar.Humors[change.Key] = newValue;
                
                string direction = change.Value > 0 ? "increased" : "decreased";
                Console.WriteLine($"OutcomeApplicator: {change.Key} {direction} by {Math.Abs(change.Value)} to {newValue}");
            }
            else
            {
                Console.WriteLine($"OutcomeApplicator: Unknown humor '{change.Key}'");
            }
        }
    }
}
