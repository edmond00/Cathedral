using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game;

/// <summary>
/// Debug-only: grants named modi mentis at a chosen level, from
/// <c>--grant-mm &lt;id[,id…]&gt;[:level]</c>. Inert unless the flag is passed.
///
/// <para>
/// This exists because fighting skills are gated behind their modi mentis, and a run that does not
/// happen to roll <c>rage</c> or <c>athletics</c> can never reach the skills that need them. It is
/// also the only way to exercise both ends of a buff's vital-heat curve — cost falls as level rises
/// (<see cref="Fight.FightingSkill.VitalHeatCostFor"/>), so a level-1 grant and a level-9 grant are
/// the two cases worth checking, and neither is reachable by playing normally in a test script.
/// </para>
///
/// <para>
/// A granted modus mentis is placed in a memory module that accepts it, so it behaves exactly like
/// one learned in play. When every acceptable module is full the modus mentis is still added to the
/// member — the point is reachability, not a faithful simulation of memory pressure — and that is
/// reported, so a script author can tell the two situations apart.
/// </para>
/// </summary>
public static class GrantModiMentisMode
{
    /// <summary>Grant whatever <c>--grant-mm</c> asked for. No-op when the flag was not passed.</summary>
    public static void GrantIfActive(PartyMember member)
    {
        var spec = Config.Debug.GrantModiMentis;
        if (spec == null) return;

        var (ids, level) = spec.Value;
        if (member.MemoryModules.Count == 0) member.InitializeMemory();

        var all = ModusMentisRegistry.Instance.GetAllModiMentis();
        var granted  = new List<string>();
        var raised   = new List<string>();
        var unknown  = new List<string>();
        var unslotted = new List<string>();

        foreach (var id in ids)
        {
            // Already held: just raise it. Re-granting would duplicate the entry.
            var held = member.ModiMentis.FirstOrDefault(m => m.ModusMentisId == id);
            if (held != null)
            {
                held.Level = level;
                raised.Add(id);
                continue;
            }

            var template = all.FirstOrDefault(t => t.ModusMentisId == id);
            if (template == null) { unknown.Add(id); continue; }

            var instance = (ModusMentis)Activator.CreateInstance(template.GetType())!;
            instance.Level = level;

            var slot = member.MemoryModules
                .Where(mod => mod.AcceptsModusMentis(template))
                .SelectMany(mod => mod.Slots)
                .FirstOrDefault(s => !s.IsUnusable && !s.IsBlocked && !s.IsFilled);

            if (slot != null) slot.ModusMentis = instance;
            else              unslotted.Add(id);

            member.ModiMentis.Add(instance);
            granted.Add(id);
        }

        Console.ForegroundColor = ConsoleColor.Magenta;
        if (granted.Count > 0)
            Console.WriteLine($"*** --grant-mm: granted {string.Join(", ", granted)} at level {level} ***");
        if (raised.Count > 0)
            Console.WriteLine($"*** --grant-mm: raised {string.Join(", ", raised)} to level {level} ***");
        if (unslotted.Count > 0)
            Console.WriteLine($"*** --grant-mm: no free memory slot for {string.Join(", ", unslotted)} — held anyway ***");
        if (unknown.Count > 0)
            Console.Error.WriteLine($"*** --grant-mm: no such modus mentis: {string.Join(", ", unknown)} ***");
        Console.ResetColor();
    }
}
