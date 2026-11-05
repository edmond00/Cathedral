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
    Beast,
    /// <summary>
    /// Great reptile: the beast's four regions, without claws, with horns on the muzzle and a
    /// thagomizer on the trunk. Only the giant kinds — a lizard you could step on stays shallow.
    /// </summary>
    Reptile
}
