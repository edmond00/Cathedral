using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

/// <summary>
/// Describes what physical medium a fighting skill uses — either a body-part organ
/// (hands, feet, fangs, claws) or a hand-held weapon.
/// </summary>
public enum MediumType
{
    /// <summary>Uses a body organ directly (hands, feet, fangs, claws).</summary>
    OrganMedium,
    /// <summary>Uses a weapon held in RightHold or LeftHold equipment slot.</summary>
    WeaponMedium,
    /// <summary>Uses a whole body-part region directly (e.g. upper_limbs); level is the region's total score.</summary>
    BodyPartMedium,
}

/// <summary>
/// Value-type representing the physical medium required to use a fighting skill.
/// Weapon skills declare only <see cref="MediumType.WeaponMedium"/>; the categories that
/// include a skill are defined by <see cref="WeaponMediumRegistry"/>.
/// </summary>
public record FightingMedium
{
    public MediumType Type { get; init; }

    /// <summary>Organ id required for <see cref="MediumType.OrganMedium"/> (e.g. "hands", "feet", "fangs", "claws").</summary>
    public string? OrganId { get; init; }

    /// <summary>Body-part id required for <see cref="MediumType.BodyPartMedium"/> (e.g. "upper_limbs").</summary>
    public string? BodyPartId { get; init; }

    /// <summary>Factory for an organ medium.</summary>
    public static FightingMedium Organ(string organId) =>
        new() { Type = MediumType.OrganMedium, OrganId = organId };

    /// <summary>
    /// Factory for a body-part medium. The medium level is the body part's total score
    /// (the sum of its organs' scores). Category membership is defined by <see cref="BodyPartMediumRegistry"/>.
    /// </summary>
    public static FightingMedium BodyPart(string bodyPartId) =>
        new() { Type = MediumType.BodyPartMedium, BodyPartId = bodyPartId };

    /// <summary>Factory for a weapon medium. Category membership is defined by <see cref="WeaponMediumRegistry"/>.</summary>
    public static FightingMedium Weapon =>
        new() { Type = MediumType.WeaponMedium };

    /// <summary>
    /// Plain label for this medium, independent of any fighter — "hands", "upper limbs", "weapon".
    /// The info panel's own <c>MediumLabelFor</c> is richer (it can name the specific organ part or
    /// the equipped weapon); this is the fighter-free fallback used by
    /// <see cref="FightingSkill.LevelBreakdown"/>.
    /// </summary>
    public string DisplayName => Type switch
    {
        MediumType.OrganMedium    => (OrganId    ?? "organ").Replace('_', ' '),
        MediumType.BodyPartMedium => (BodyPartId ?? "region").Replace('_', ' '),
        _                         => "weapon",
    };

    /// <summary>
    /// Returns the level of this medium for a given fighter.
    /// For an organ medium, this is the organ's current score — or, when
    /// <paramref name="organPartId"/> is supplied and matches one of the organ's parts,
    /// that single part's score (so a left-hand attack uses only the left hand's level).
    /// For a weapon medium, this is the <see cref="IWeaponItem.Level"/> of the first equipped weapon.
    /// </summary>
    public int GetLevel(Fighter f, string? organPartId = null)
    {
        if (Type == MediumType.OrganMedium)
        {
            if (string.IsNullOrEmpty(OrganId)) return 0;
            var organ = f.Member.GetOrganById(OrganId);
            if (organ == null) return 0;
            if (!string.IsNullOrEmpty(organPartId))
            {
                var part = organ.Parts.FirstOrDefault(p => p.Id == organPartId);
                if (part != null) return part.Score;
            }
            return organ.Score;
        }
        else if (Type == MediumType.BodyPartMedium)
        {
            // Whole body-part region: level is the region's total score (sum of its organs).
            // organPartId is ignored — a body-part medium has no independent sub-parts.
            if (string.IsNullOrEmpty(BodyPartId)) return 0;
            return f.Member.GetBodyPartById(BodyPartId)?.Score ?? 0;
        }
        else // WeaponMedium
        {
            var weapon = f.Member.EquippedItems[EquipmentAnchor.RightHold]
                .Concat(f.Member.EquippedItems[EquipmentAnchor.LeftHold])
                .OfType<IWeaponItem>()
                .FirstOrDefault();
            return weapon?.Level ?? 0;
        }
    }
}
