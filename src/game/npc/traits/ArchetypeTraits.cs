namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// The reserved trait pools — six per archetype, drawn on only by NPCs of that trade. Where a global
/// trait says something any villager could be, these say something only a smith, or only a shepherd,
/// could be: the burn-scarred hands, the ewe that follows them home, the toll everyone swears is short.
/// </summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterArchetypeTraits()
    {
        RegisterWorkshopTraits();     // blacksmith, baker, brewer, carpenter, cooper, miller, weaver, apprentice
        RegisterFieldTraits();        // reeve, hayward, plowman, reaper, bondman
        RegisterFarmTraits();         // farmer, farmhand, shepherd, swineherd, dairymaid, poultry keeper
        RegisterWildernessTraits();   // woodcutter, charcoal burner, fisherman, miner
        RegisterSolitaryTraits();     // druid, hermit, savage
    }
}
