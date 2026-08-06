namespace Cathedral.Game.Narrative;

/// <summary>
/// One wound on one body — a <see cref="Wound"/> template plus everything that is true of this
/// particular injury on this particular person.
///
/// <para>
/// <b>Why this type exists.</b> <see cref="WoundRegistry.All"/> holds singletons, and the fight
/// path used to append those shared objects straight into <c>PartyMember.Wounds</c>. Anything
/// written onto a wound therefore leaked across every character in the process — and across the
/// whole run, since the registry is initialised once. Art positions already had this bug; adding a
/// healing timestamp would have made two characters share one recovery. Templates are now immutable
/// and every per-body fact lives here.
/// </para>
///
/// <para>
/// It forwards the template's whole read surface, so code that only asks a wound what it is
/// (<see cref="Handicap"/>, <see cref="TargetId"/>, <c>AffectsOrgan</c>…) needs no changes.
/// </para>
/// </summary>
public sealed class WoundInstance
{
    /// <summary>The catalogue entry this injury is an instance of.</summary>
    public Wound Template { get; }

    /// <summary>
    /// The day this wound was inflicted, or <b>null</b> for a wound that is part of who the
    /// character is rather than something that happened to them during the run — an NPC's
    /// trait scars, the protagonist's starting injuries.
    ///
    /// <para>
    /// Null means <em>historical</em>, and historical wounds never heal. Without this distinction
    /// one long work stint would quietly erase every NPC's backstory: the one-eyed blacksmith would
    /// grow the eye back. See <see cref="PartyMember.HealWounds"/>.
    /// </para>
    /// </summary>
    public double? InflictedOnDay { get; }

    /// <summary>Resolved glyph cell on the body art, or null until it has been placed.</summary>
    public int? ArtX { get; set; }
    /// <inheritdoc cref="ArtX"/>
    public int? ArtY { get; set; }

    /// <summary>
    /// For a wildcard wound, the body-part or organ-part id its glyph should be placed within
    /// (chosen by the narrative failure critic). Null means anywhere on the body.
    /// </summary>
    public string? WildcardZoneHint { get; set; }

    public WoundInstance(Wound template, double? inflictedOnDay = null, string? wildcardZoneHint = null)
    {
        Template         = template;
        InflictedOnDay   = inflictedOnDay;
        WildcardZoneHint = wildcardZoneHint;
    }

    /// <summary>A wound suffered right now — stamped with the current day, and so able to heal.</summary>
    public static WoundInstance Inflicted(Wound template, string? wildcardZoneHint = null)
        => new(template, GameClock.Days, wildcardZoneHint);

    /// <summary>A wound the character already carried — permanent, and exempt from healing.</summary>
    public static WoundInstance Historical(Wound template, string? wildcardZoneHint = null)
        => new(template, null, wildcardZoneHint);

    // ── Template passthrough ──────────────────────────────────────────────────

    public char WoundId                => Template.WoundId;
    public string WoundName            => Template.WoundName;
    public WoundHandicap Handicap      => Template.Handicap;
    public WoundTargetKind TargetKind  => Template.TargetKind;
    public string TargetId             => Template.TargetId;
    public string Description          => Template.Description;

    public bool AffectsOrganPart(string organPartId, string organId, string bodyPartId)
        => Template.AffectsOrganPart(organPartId, organId, bodyPartId);
    public bool AffectsOrgan(string organId, string bodyPartId)
        => Template.AffectsOrgan(organId, bodyPartId);
    public bool AffectsBodyPart(string bodyPartId)
        => Template.AffectsBodyPart(bodyPartId);

    // ── Healing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Whether this wound is the kind that can close at all. High-handicap wounds are permanent
    /// (a severed hand does not grow back), and so are historical ones.
    /// </summary>
    public bool CanHeal => InflictedOnDay.HasValue && Handicap != WoundHandicap.High;

    /// <summary>Days elapsed since this wound was inflicted, or 0 for a historical one.</summary>
    public double DaysOld => InflictedOnDay.HasValue
        ? System.Math.Max(0, GameClock.Days - InflictedOnDay.Value)
        : 0;

    /// <summary>
    /// How far along this wound is toward closing, 0..1, given the owner's
    /// <c>wound_healing</c> stat. Returns 0 for anything that cannot heal.
    ///
    /// <para>
    /// Derived from the clock rather than ticked, the same way
    /// <see cref="PartyMember.GetAgeDays"/> derives age from a stored birth day: there is no
    /// counter to keep in sync and the figure is correct whenever it happens to be read.
    /// </para>
    /// </summary>
    public double HealProgress(int healDurationDays)
    {
        if (!CanHeal || healDurationDays <= 0) return 0;
        return System.Math.Clamp(DaysOld / healDurationDays, 0, 1);
    }
}
