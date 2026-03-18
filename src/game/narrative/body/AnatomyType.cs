namespace Cathedral.Game.Narrative;

/// <summary>
/// Identifies the anatomical layout used by a species.
/// Each anatomy type has a distinct set of body parts, organs, wounds.
/// </summary>
public enum AnatomyType
{
    /// <summary>Bipedal humanoid: encephalon, visage, trunk, upper_limbs, lower_limbs.</summary>
    Human,
    /// <summary>Quadruped beast: encephalon, muzzle, trunk, limbs.</summary>
    Beast
}
