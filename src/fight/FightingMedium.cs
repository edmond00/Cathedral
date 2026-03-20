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
/// </summary>
public record FightingMedium
{
    public MediumType Type { get; init; }

    /// <summary>Organ id required for <see cref="MediumType.OrganMedium"/> (e.g. "hands", "feet", "fangs", "claws").</summary>
    public string? OrganId { get; init; }

    /// <summary>
    /// True when this medium is an organ medium for <paramref name="organId"/>.
    /// </summary>
    public static FightingMedium Organ(string organId) =>
        new() { Type = MediumType.OrganMedium, OrganId = organId };

    /// <summary>True when this medium requires a weapon item (<see cref="IWeaponItem"/>) in a hold slot.</summary>
    public static FightingMedium Weapon =>
        new() { Type = MediumType.WeaponMedium };
}
