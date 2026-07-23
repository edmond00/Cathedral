using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Maps a conversation's roles ("you" = speaking party member, "npc" = interlocutor) onto the
/// scene-wide false-name registry (<see cref="NameFaking"/>). The local LLM only ever sees the scene's
/// simple, sanitizer-safe placeholder names; the real in-world names are restored on output.
///
/// <para>This is a thin adapter — the single source of false names is the scene registry built when the
/// scene begins, so a PC and every NPC across the whole scene share one collision-free, allow-listed
/// name set. Kept as a class (rather than calling <see cref="NameFaking"/> directly at every call site)
/// so <see cref="DialogueTemplate"/>'s <c>{role:name}</c> tokens and the replica writers keep their
/// existing <c>Placeholder</c>/<c>ToPlaceholder</c>/<c>ToReal</c> API.</para>
/// </summary>
public class DialogueNameTable
{
    private readonly Dictionary<string, string> _roleReal; // role → real display name

    private DialogueNameTable(Dictionary<string, string> roleReal) => _roleReal = roleReal;

    /// <summary>Builds the table for a party-member ↔ NPC conversation (the two core roles).</summary>
    public static DialogueNameTable Build(PartyMember pc, NpcEntity npc)
        => new(new(StringComparer.OrdinalIgnoreCase)
        {
            ["you"] = pc.DisplayName,
            ["npc"] = npc.DisplayName,
        });

    /// <summary>
    /// The scene false name for a role, or null when the role is not registered. Falls back to the real
    /// name if the character somehow isn't in the scene registry (e.g. a conversation outside a scene).
    /// </summary>
    public string? Placeholder(string role)
        => _roleReal.TryGetValue(role, out var real) ? (NameFaking.Current?.FalseFor(real) ?? real) : null;

    /// <summary>Real → false: apply to any text handed to the LLM (e.g. the NPC persona prompt).</summary>
    public string ToPlaceholder(string text) => NameFaking.Fake(text);

    /// <summary>False → real: apply to every generated replica before it is shown to the player.</summary>
    public string ToReal(string text) => NameFaking.Real(text);
}
