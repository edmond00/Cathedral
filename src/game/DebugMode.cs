using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// The overall debug strategy chosen by the user at the start of an action execution.
/// </summary>
public enum DebugStrategy
{
    /// <summary>Auto-fail a plausibility check (answers "no" to first plausibility node).</summary>
    FailPlausibility,
    /// <summary>Auto-pass plausibility + difficulty, then force dice failure.</summary>
    FailDiceRoll,
    /// <summary>Auto-pass everything (plausibility, difficulty, dice roll).</summary>
    Succeed,
    /// <summary>Prompt each critic node individually (original behavior).</summary>
    Custom
}

/// <summary>
/// Global debug mode that overrides LLM and RNG decisions with console prompts.
/// Activated by the --debug CLI flag.
/// When active, all critic tree evaluations and dice rolls prompt the user
/// via the system console to choose the outcome.
/// </summary>
public static class DebugMode
{
    /// <summary>Whether debug mode is active.</summary>
    public static bool IsActive { get; set; } = false;

    /// <summary>Current strategy for the action being executed.</summary>
    public static DebugStrategy CurrentStrategy { get; private set; } = DebugStrategy.Custom;

    /// <summary>
    /// Print all available actions and their outcomes to the console.
    /// Called before the user clicks an action so they know what each one does.
    /// </summary>
    public static void PrintAvailableActions(List<ParsedNarrativeAction> actions)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine();
        Console.WriteLine("[DEBUG] ═══ Available Actions ═══");
        for (int i = 0; i < actions.Count; i++)
        {
            var a = actions[i];
            string outcomeType = a.PreselectedOutcome?.GetType().Name ?? "???";
            string outcomeDesc = a.PreselectedOutcome?.DisplayName ?? "unknown";
            string circuitous = a.IsCircuitous ? " [CIRCUITOUS]" : "";
            Console.WriteLine($"  {i + 1}) {a.ActionText}");
            Console.WriteLine($"     Modus Mentis: {a.ActionModusMentisId}  |  Outcome: {outcomeType} → {outcomeDesc}{circuitous}");
        }
        Console.WriteLine("[DEBUG] ═════════════════════════");
        Console.ResetColor();
    }

    /// <summary>
    /// Prompt the user to choose an overall debug strategy for the current action.
    /// Must be called at the start of ExecuteActionPhaseAsync.
    /// </summary>
    public static void PromptActionStrategy(string actionText, string outcomeSummary)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine($"[DEBUG] ═══ Action Strategy ═══");
        Console.WriteLine($"  Action:  {actionText}");
        Console.WriteLine($"  Outcome: {outcomeSummary}");
        Console.WriteLine();
        Console.WriteLine($"  1) Fail plausibility  (auto-reject at first plausibility check)");
        Console.WriteLine($"  2) Fail dice roll     (pass checks, then force dice failure)");
        Console.WriteLine($"  3) Succeed            (pass everything, dice succeeds)");
        Console.WriteLine($"  4) Custom             (answer each critic node individually)");
        Console.Write($"  Choice [1-4]: ");
        Console.ResetColor();

        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1":
                    CurrentStrategy = DebugStrategy.FailPlausibility;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  → Strategy: Fail Plausibility");
                    Console.ResetColor();
                    return;
                case "2":
                    CurrentStrategy = DebugStrategy.FailDiceRoll;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  → Strategy: Fail Dice Roll");
                    Console.ResetColor();
                    return;
                case "3":
                    CurrentStrategy = DebugStrategy.Succeed;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  → Strategy: Succeed");
                    Console.ResetColor();
                    return;
                case "4":
                    CurrentStrategy = DebugStrategy.Custom;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  → Strategy: Custom (per-node prompts)");
                    Console.ResetColor();
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"  Invalid. Enter 1-4: ");
                    Console.ResetColor();
                    break;
            }
        }
    }

    /// <summary>
    /// Prompt the user to choose yes or no for a critic node question.
    /// Returns (pYes, pNo, score) matching the user's choice.
    /// Only called when CurrentStrategy is Custom.
    /// </summary>
    public static (double pYes, double pNo, double score) PromptCriticNode(string nodeName, string question)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine($"[DEBUG] Critic Node: {nodeName}");
        Console.WriteLine($"  Question: {question}");
        Console.WriteLine($"  1) Yes");
        Console.WriteLine($"  2) No");
        Console.Write($"  Choice [1/2]: ");
        Console.ResetColor();

        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            if (input == "1" || input?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  → Yes (forced)");
                Console.ResetColor();
                return (0.95, 0.05, 0.95); // Strong yes
            }
            if (input == "2" || input?.Equals("no", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  → No (forced)");
                Console.ResetColor();
                return (0.05, 0.95, 0.05); // Strong no
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"  Invalid. Enter 1 or 2: ");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Prompt the user to choose whether a dice roll succeeds or fails.
    /// Returns true for success, false for failure.
    /// Only called when CurrentStrategy is Custom.
    /// </summary>
    public static bool PromptDiceRoll(string actionText, double successProbability)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"[DEBUG] Dice Roll");
        Console.WriteLine($"  Action: {actionText}");
        Console.WriteLine($"  Success probability: {successProbability:P0}");
        Console.WriteLine($"  1) Success");
        Console.WriteLine($"  2) Failure");
        Console.Write($"  Choice [1/2]: ");
        Console.ResetColor();

        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            if (input == "1" || input?.Equals("success", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  → Success (forced)");
                Console.ResetColor();
                return true;
            }
            if (input == "2" || input?.Equals("failure", StringComparison.OrdinalIgnoreCase) == true || 
                input?.Equals("fail", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  → Failure (forced)");
                Console.ResetColor();
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  Invalid. Enter 1 or 2: ");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Returns the yes/no override for a critic node based on the current strategy.
    /// For non-Custom strategies, returns the auto-answer without prompting.
    /// For Custom, prompts interactively.
    /// </summary>
    public static (double pYes, double pNo, double score) GetCriticOverride(string nodeName, string question, bool isPlausibilityNode)
    {
        switch (CurrentStrategy)
        {
            case DebugStrategy.FailPlausibility when isPlausibilityNode:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [AUTO] {nodeName}: No (fail plausibility strategy)");
                Console.ResetColor();
                return (0.05, 0.95, 0.05);

            case DebugStrategy.FailPlausibility:
            case DebugStrategy.FailDiceRoll:
            case DebugStrategy.Succeed:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [AUTO] {nodeName}: Yes (auto-pass)");
                Console.ResetColor();
                return (0.95, 0.05, 0.95);

            case DebugStrategy.Custom:
            default:
                return PromptCriticNode(nodeName, question);
        }
    }

    /// <summary>
    /// Returns the dice roll override based on the current strategy.
    /// For non-Custom strategies, returns the auto-answer without prompting.
    /// </summary>
    public static bool GetDiceRollOverride(string actionText, double successProbability)
    {
        switch (CurrentStrategy)
        {
            case DebugStrategy.FailDiceRoll:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [AUTO] Dice: Failure (fail dice strategy)");
                Console.ResetColor();
                return false;

            case DebugStrategy.Succeed:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [AUTO] Dice: Success (succeed strategy)");
                Console.ResetColor();
                return true;

            case DebugStrategy.FailPlausibility:
                // Shouldn't normally reach dice roll with this strategy, but auto-pass if we do
                return true;

            case DebugStrategy.Custom:
            default:
                return PromptDiceRoll(actionText, successProbability);
        }
    }
}
