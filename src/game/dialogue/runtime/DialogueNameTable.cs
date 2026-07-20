using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Maps conversation <b>roles</b> to a short, gender-appropriate <b>placeholder</b> name and the
/// character's real display name. The local LLM only ever sees placeholders (which it handles far
/// more reliably than generated names like "Godric Reeve"); real names are substituted back into the
/// finished replica before it is shown.
///
/// <para>
/// Roles are open-ended and documented here:
/// <list type="bullet">
///   <item><c>you</c> — the speaking party member (placeholder <c>Bob</c>/<c>Alice</c>).</item>
///   <item><c>npc</c> — the interlocutor (placeholder <c>Jack</c>/<c>Emma</c>).</item>
///   <item><c>third:*</c> — reserved for future third parties a tree might mention; each draws the
///         next free name from the gendered pool so placeholders never collide.</item>
/// </list>
/// Add a role with <see cref="AddRole"/>; reference it in templates via <c>{role:name}</c>.
/// </para>
/// </summary>
public class DialogueNameTable
{
    public sealed record Entry(string Role, string Placeholder, string Real, bool Male);

    private readonly List<Entry> _entries = new();

    /// <summary>Fixed placeholder names for the two core roles, per gender (per design).</summary>
    private static readonly Dictionary<string, (string male, string female)> RoleNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["you"] = ("Bob",  "Alice"),
            ["npc"] = ("Jack", "Emma"),
        };

    /// <summary>Fallback names for additional roles (e.g. a mentioned third person).</summary>
    private static readonly string[] MalePool   = { "Tom",  "Will", "Ned",  "Hal",  "Sam"  };
    private static readonly string[] FemalePool = { "Kate", "Rose", "Mary", "Beth", "Joan" };

    public IReadOnlyList<Entry> Entries => _entries;

    /// <summary>Builds the table for a party-member ↔ NPC conversation (the two core roles).</summary>
    public static DialogueNameTable Build(PartyMember pc, NpcEntity npc)
    {
        var t = new DialogueNameTable();
        t.AddRole("you", NpcLabelResolver.GenderIsMale(pc),            pc.DisplayName);
        t.AddRole("npc", NpcLabelResolver.GenderIsMale(npc.Combatant), npc.DisplayName);
        return t;
    }

    /// <summary>Registers a role with a gender-appropriate placeholder, avoiding collisions.</summary>
    public void AddRole(string role, bool male, string realName)
        => _entries.Add(new Entry(role, ResolvePlaceholder(role, male), realName ?? "", male));

    private string ResolvePlaceholder(string role, bool male)
    {
        var used = _entries.Select(e => e.Placeholder).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (RoleNames.TryGetValue(role, out var pair))
        {
            var name = male ? pair.male : pair.female;
            if (!used.Contains(name)) return name;
        }
        foreach (var n in male ? MalePool : FemalePool)
            if (!used.Contains(n)) return n;
        return male ? "Man" : "Woman";
    }

    /// <summary>The placeholder for a role, or null if the role is not registered.</summary>
    public string? Placeholder(string role) =>
        _entries.FirstOrDefault(e => string.Equals(e.Role, role, StringComparison.OrdinalIgnoreCase))?.Placeholder;

    /// <summary>Real → placeholder: apply to any text handed to the LLM (persona prompt, context).</summary>
    public string ToPlaceholder(string text) => Replace(text, fromReal: true);

    /// <summary>Placeholder → real: apply to every generated replica before it is shown to the player.</summary>
    public string ToReal(string text) => Replace(text, fromReal: false);

    private string Replace(string text, bool fromReal)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        // Longest source first so a multi-word real name is replaced before its first token would be.
        foreach (var e in _entries.Where(e => !string.IsNullOrWhiteSpace(e.Real))
                                   .OrderByDescending(e => (fromReal ? e.Real : e.Placeholder).Length))
        {
            string from = fromReal ? e.Real : e.Placeholder;
            string to   = fromReal ? e.Placeholder : e.Real;
            if (from.Length == 0) continue;
            text = Regex.Replace(text, $@"\b{Regex.Escape(from)}\b", to.Replace("$", "$$"), RegexOptions.IgnoreCase);
        }
        return text;
    }
}
