using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game;

/// <summary>
/// Memory-fill testing mode: when --mm is passed on the command line, every empty slot
/// of every memory module is filled (after the childhood reminescence phase ends) with a
/// modusMentis randomly sampled from those not yet held by the member. A slot is left empty
/// when no unheld modusMentis can fill it (typed modules only accept a matching MemoryType;
/// Working and Residual accept any). Each modusMentis is used at most once.
/// </summary>
public static class FillMemoryMode
{
    public static bool IsActive { get; set; } = false;

    private static readonly Random _rng = new();

    /// <summary>
    /// Fills every empty, usable, non-blocked slot of <paramref name="member"/>'s memory
    /// modules with a level-1 instance of a randomly chosen modusMentis the member does not
    /// already hold and which the module accepts. Newly placed modiMentis are registered in
    /// <see cref="PartyMember.ModiMentis"/>. Slots with no eligible candidate are left empty.
    /// </summary>
    public static void FillEmptySlots(PartyMember member)
    {
        if (member.MemoryModules.Count == 0) member.InitializeMemory();

        // Track every modusMentis already held so we never place a duplicate.
        var usedIds = new HashSet<string>(member.ModiMentis.Select(m => m.ModusMentisId));
        var allTemplates = ModusMentisRegistry.Instance.GetAllModiMentis();

        int filled = 0;
        foreach (var module in member.MemoryModules)
        {
            foreach (var slot in module.Slots)
            {
                if (slot.IsUnusable || slot.IsBlocked || slot.IsFilled) continue;

                // Candidates: not yet held, and accepted by this module (typed modules
                // require a matching MemoryType; Working/Residual accept anything).
                var candidates = allTemplates
                    .Where(t => !usedIds.Contains(t.ModusMentisId) && module.AcceptsModusMentis(t))
                    .ToList();
                if (candidates.Count == 0) continue; // no eligible modusMentis — leave empty

                var template = candidates[_rng.Next(candidates.Count)];
                var instance = (ModusMentis)Activator.CreateInstance(template.GetType())!;
                instance.Level = 1;

                slot.ModusMentis = instance;
                member.ModiMentis.Add(instance);
                usedIds.Add(template.ModusMentisId);
                filled++;
            }
        }

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"*** --mm: filled {filled} empty memory slot(s) with random unheld modiMentis ***");
        Console.ResetColor();
    }
}
