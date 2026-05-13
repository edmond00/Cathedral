using System;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Reminescence;

namespace Cathedral.Game;

/// <summary>
/// Skip-childhood testing mode: when --skip-childhood is passed on the command line,
/// the ChildhoodReminescence + GetUp phases are bypassed and the protagonist is
/// populated as if those phases had run normally (first skills, first items, childhood
/// history) by random-walking the reminescence catalog. The run then drops directly
/// into WorldView.
/// </summary>
public static class SkipChildhoodMode
{
    public static bool IsActive { get; set; } = false;

    private static readonly Random _rng = new();

    /// <summary>
    /// Random-walks the reminescence catalog starting at the entry reminescence,
    /// applying each chosen fragment's outcome to <paramref name="protagonist"/>:
    /// skills are acquired at level 1, items are placed via AcquireItem, the childhood
    /// location is set on the very first fragment, and every visited fragment is
    /// recorded in the protagonist's ChildhoodHistory.
    /// </summary>
    public static void SimulateAndApply(Protagonist protagonist)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("*** --skip-childhood: simulating reminescence phase ***");
        Console.ResetColor();

        string currentId = ReminescenceRegistry.EntryReminescenceId;
        // Defensive cap: the catalog is a DAG with terminal sentinels, so a stuck
        // loop should never happen — but we don't want to hang the game on a bad edit.
        const int maxSteps = 32;

        for (int step = 0; step < maxSteps; step++)
        {
            if (ReminescenceRegistry.IsEnd(currentId)) break;

            var data = ReminescenceRegistry.Get(currentId);
            if (data == null)
            {
                Console.Error.WriteLine($"SkipChildhoodMode: unknown reminescence '{currentId}' — stopping.");
                return;
            }
            if (data.Fragments.Count == 0)
            {
                Console.Error.WriteLine($"SkipChildhoodMode: reminescence '{currentId}' has no fragments — stopping.");
                return;
            }

            var fragment = data.Fragments[_rng.Next(data.Fragments.Count)];
            var outcome  = fragment.Outcome;

            foreach (var skillId in outcome.SkillIds)
            {
                var template = ModusMentisRegistry.Instance.GetModusMentis(skillId);
                if (template == null)
                {
                    Console.WriteLine($"SkipChildhoodMode: skill '{skillId}' not registered — skipping.");
                    continue;
                }
                var instance = (ModusMentis)Activator.CreateInstance(template.GetType())!;
                instance.Level = 1;
                protagonist.AcquireModusMentis(instance);
            }

            foreach (var itemFactory in outcome.Items)
                protagonist.AcquireItem(itemFactory());

            if (outcome.SetChildhoodLocation != null)
                protagonist.ChildhoodHistory.Location = outcome.SetChildhoodLocation;

            protagonist.ChildhoodHistory.RecordFragment(
                currentId, fragment.Name, fragment.Summary, fragment.ContextSummary);

            Console.WriteLine($"  [{currentId}] → '{fragment.Name}' "
                + $"({outcome.SkillIds.Count} skill(s), {outcome.Items.Count} item(s))");

            if (outcome.IsTerminal) break;
            currentId = outcome.NextReminescenceId;
        }

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"*** --skip-childhood: done. Location='{protagonist.ChildhoodHistory.Location ?? "(unset)"}', "
            + $"{protagonist.ChildhoodHistory.RememberedFragments.Count} fragment(s) remembered ***");
        Console.ResetColor();
    }
}
