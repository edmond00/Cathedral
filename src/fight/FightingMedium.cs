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

    /// <summary>Factory for an organ medium.</summary>
    public static FightingMedium Organ(string organId) =>
        new() { Type = MediumType.OrganMedium, OrganId = organId };

    /// <summary>Factory for a weapon medium. Category membership is defined by <see cref="WeaponMediumRegistry"/>.</summary>
    public static FightingMedium Weapon =>
        new() { Type = MediumType.WeaponMedium };

    /// <summary>
    /// Returns the level of this medium for a given fighter.
    /// For an organ medium, this is the organ's current score.
    /// For a weapon medium, this is the <see cref="IWeaponItem.Level"/> of the first equipped weapon.
    /// </summary>
    public int GetLevel(Fighter f)
    {
        if (Type == MediumType.OrganMedium)
        {
            if (string.IsNullOrEmpty(OrganId)) return 0;
            return f.Member.GetOrganById(OrganId)?.Score ?? 0;
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
