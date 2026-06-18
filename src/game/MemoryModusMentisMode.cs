using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game;

/// <summary>
/// Memory-fill testing mode: when --mm is passed on the command line, every empty
/// usable slot of every memory module is filled with a random modusMentis once the
/// childhood reminescence phase ends (or is skipped via --skip-childhood).
///
/// Only modiMentis that the module accepts (see <see cref="MemoryModule.AcceptsModusMentis"/>)
/// and that the party member has not already memorized are considered. When no eligible
/// candidate remains for a slot, the slot is left empty.
/// </summary>
public static class MemoryModusMentisMode
{
    /// <summary>Whether memory-fill mode is active.</summary>
    public static bool IsActive { get; set; } = false;

    private static readonly Random _rng = new();

    /// <summary>
    /// Fills the empty slots of every memory module of <paramref name="member"/> with random,
    /// not-yet-memorized modiMentis the module accepts. Does nothing unless mode is active.
    /// Each placed modusMentis is registered in <see cref="PartyMember.ModiMentis"/> so it is not
    /// re-used for another slot and counts as memorized.
    /// </summary>
    public static void FillIfActive(PartyMember member)
    {
        if (!IsActive || member == null) return;
        if (member.MemoryModules.Count == 0) member.InitializeMemory();

        // The registry holds singleton templates; we instantiate fresh copies for placement.
        var templates = ModusMentisRegistry.Instance.GetAllModiMentis();
        int placed = 0;

        foreach (var module in member.MemoryModules)
        {
            foreach (var slot in module.Slots)
            {
                if (slot.IsUnusable || slot.IsBlocked || slot.IsFilled) continue;

                // Candidates: accepted by this module and not already memorized anywhere.
                var memorizedIds = member.ModiMentis.Select(m => m.ModusMentisId).ToHashSet();
                var candidates = templates
                    .Where(t => module.AcceptsModusMentis(t) && !memorizedIds.Contains(t.ModusMentisId))
                    .ToList();

                if (candidates.Count == 0) continue; // leave the slot empty

                var template = candidates[_rng.Next(candidates.Count)];
                var instance = (ModusMentis)Activator.CreateInstance(template.GetType())!;
                instance.Level = 1;
                instance.CurrentXp = 0;

                slot.ModusMentis = instance;
                member.ModiMentis.Add(instance);
                placed++;
            }
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"*** --mm: filled {placed} empty memory slot(s) with random modiMentis ***");
        Console.ResetColor();
    }
}
