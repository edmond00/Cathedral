using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Generation;

namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// One coherent quirk of a person, applied on top of the archetype's baseline to make an individual:
/// <c>Greedy</c>, <c>One-Eyed</c>, <c>Ox-Strong</c>. A trait is deliberately <b>two-sided</b> — it
/// changes gameplay content and it changes the words the game uses about the NPC — because a trait
/// that only did one would produce a character who reads one way and plays another.
///
/// <list type="bullet">
///   <item><see cref="ApplyGameplay"/> — modi mentis, organ scores, inventory, wounds.</item>
///   <item><see cref="ApplyText"/> — appearance, LLM persona prompt, dialogue-flavour fields.</item>
/// </list>
///
/// <para>
/// Both are <c>virtual</c> and both have a default implementation that consumes the declarative
/// fields below, so the ordinary trait is a ten-line object initialiser rather than a class. A trait
/// that needs to do something the fields cannot express subclasses and overrides the method it
/// needs — the declarative path is a convenience, not a ceiling.
/// </para>
///
/// <para>
/// Traits never roll their own dice from an ambient source: every random choice takes the
/// per-NPC <see cref="Random"/> handed in, which is seeded from the NPC's stable id. Same id, same
/// person, every time.
/// </para>
/// </summary>
public class PersonalityTrait
{
    /// <summary>Stable identifier, unique across the whole trait catalogue (e.g. "greedy").</summary>
    public required string TraitId { get; init; }

    /// <summary>Human-readable name, shown in the NPC audit (e.g. "Greedy").</summary>
    public required string DisplayName { get; init; }

    // ── Declarative gameplay payload ───────────────────────────────────────────

    /// <summary>
    /// Modus-mentis ids this trait grants. Granted at a level rolled the same way as the NPC's own
    /// skills (1–3, capped by the organ-derived maximum) and placed by the standard memory
    /// procedure. An id that is already held is skipped rather than duplicated.
    /// </summary>
    public string[] ModiMentis { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Organ-part score adjustments, applied after the archetype roll and clamped to the part's
    /// maximum: <c>("right_arm", +1)</c>. Negative deltas are allowed — a trait may cost something.
    /// </summary>
    public (string OrganPartId, int Delta)[] Organs { get; init; } = Array.Empty<(string, int)>();

    /// <summary>
    /// Extra belongings this trait implies. Factories rather than instances, because every NPC needs
    /// its own item objects.
    /// </summary>
    public Func<Item>[] Items { get; init; } = Array.Empty<Func<Item>>();

    /// <summary>
    /// Lasting injuries this trait implies — the mechanical half of "One-Eyed". A
    /// <see cref="WoundHandicap.High"/> wound genuinely disables the organ, so pair it with an
    /// <see cref="Appearance"/> clause or the player will meet a half-blind NPC with nothing to see.
    /// </summary>
    public Func<Wound>[] Wounds { get; init; } = Array.Empty<Func<Wound>>();

    // ── Declarative text payload ───────────────────────────────────────────────

    /// <summary>
    /// A clause appended to the observation hint — what this trait looks like from across a room.
    /// Lower case, no full stop: it is joined into the existing sentence.
    /// </summary>
    public string? Appearance { get; init; }

    /// <summary>
    /// A sentence or two appended to the NPC's LLM system prompt, in the same second-person voice as
    /// the archetype brief. This is what makes the trait audible in dialogue.
    /// </summary>
    public string? Persona { get; init; }

    /// <summary>
    /// Replacement opinions for the strengthen-relationship tree — where this trait would make the
    /// person answer differently from a typical member of their trade.
    /// </summary>
    public (DialogueTopic Topic, string Opinion)[] Opinions { get; init; }
        = Array.Empty<(DialogueTopic, string)>();

    /// <summary>Overrides <c>{npc:introduction}</c> when this trait dominates how they present themselves.</summary>
    public string? SelfIntroduction { get; init; }

    /// <summary>Overrides <c>{npc:labour}</c> when the trait changes how their working day actually goes.</summary>
    public string? DailyLabour { get; init; }

    // ── Application ────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies this trait's mechanical half. The default consumes the declarative fields; override
    /// for anything they cannot express.
    /// </summary>
    /// <param name="body">
    /// The NPC's body and belongings — organs, wounds, inventory, skills. This is the combatant
    /// rather than the <see cref="NpcEntity"/> because traits run <i>before</i> the entity exists:
    /// they help decide the text the entity is constructed with.
    /// </param>
    /// <param name="rng">The NPC's own generator, seeded from its stable id.</param>
    public virtual void ApplyGameplay(PartyMember body, Random rng)
    {
        foreach (var (organPartId, delta) in Organs)
        {
            var part = body.GetOrganPartById(organPartId);
            if (part == null)
                Console.Error.WriteLine(
                    $"PersonalityTrait '{TraitId}': no organ part '{organPartId}' on {body.DisplayName}.");
            else
                part.Score += delta;   // OrganPart.Score clamps to [0, MaxScore]
        }

        // HISTORICAL, not inflicted: a trait wound is part of who this person is — the shepherd's
        // scar, the smith's deafness — and must never heal off. Stamping these with the current day
        // instead would quietly erase every NPC's backstory after one long work stint.
        foreach (var wound in Wounds)
            body.Wounds.Add(WoundInstance.Historical(wound()));

        foreach (var item in Items)
            NpcBelongings.Give(body, item());

        // Skills last: their level cap reads the organ scores this trait may just have changed.
        foreach (var id in ModiMentis)
            NpcSkillGrant.Grant(body, id, rng);
    }

    /// <summary>
    /// Applies this trait's descriptive half. The default consumes the declarative fields; override
    /// for anything they cannot express.
    /// </summary>
    public virtual void ApplyText(NpcTextProfile text, Random rng)
    {
        text.AddAppearance(Appearance);
        text.AddPersona(Persona);

        foreach (var (topic, opinion) in Opinions)
            text.SetOpinion(topic, opinion);

        // First trait to claim a field keeps it — archetype traits are applied before global ones,
        // so the more specific voice wins.
        text.SelfIntroduction ??= SelfIntroduction;
        text.DailyLabour      ??= DailyLabour;
    }
}
