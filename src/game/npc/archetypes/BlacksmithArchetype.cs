using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village blacksmith — master of the forge, brave, owns his shop.</summary>
public class BlacksmithArchetype : CraftsmanArchetype
{
    public override string ArchetypeId  => "blacksmith";
    public override int    ModiMentisCount => 10;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string[] NamePool => new[]
    {
        "Aelric Smith", "Brand Coalheart", "Edric Iron", "Godfrey Hammer",
        "Hild Smith", "Ranulf Forge", "Walter Anvil", "Wulfric Smith",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a heavy-shouldered figure looks up from the anvil, soot streaking his arms — {name}, the village blacksmith";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village blacksmith. You forge iron into tools — saws, axes, sickles, ploughshares — that everyone in the village and the surrounding country depends on. Your forge is hot, your hands are scarred, and your patience for fools is short.

You speak in clipped, certain sentences. You measure people the way you measure iron: by what they're useful for. You are proud of your craft and protective of your workshop. You distrust strangers but will warm to anyone who shows respect for honest labour.

You know who in the village owes you money for tools, who's about to need a new ploughshare, and which farmers can be trusted to pay. Your voice carries the rumble of the bellows.";
}
