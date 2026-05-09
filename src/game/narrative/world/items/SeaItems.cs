using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Herring : SeaFoodItem
{
    public override string ItemId      => "herring";
    public override string DisplayName => "Herring";
    public override string Description => "A silver-flanked herring, eyes still bright";
}

public sealed class Cod : SeaFoodItem
{
    public override string ItemId      => "cod";
    public override string DisplayName => "Cod";
    public override string Description => "A fat cod, mottled grey-green along its back";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 1.0f;
}

public sealed class Mackerel : SeaFoodItem
{
    public override string ItemId      => "mackerel";
    public override string DisplayName => "Mackerel";
    public override string Description => "A streamlined mackerel banded with iridescent green-blue";
}

public sealed class Crab : SeaFoodItem
{
    public override string ItemId      => "crab";
    public override string DisplayName => "Crab";
    public override string Description => "A scuttling brown crab, claws still snapping";
}

public sealed class Mussel : SeaFoodItem
{
    public override string ItemId      => "mussel";
    public override string DisplayName => "Mussel";
    public override string Description => "A fistful of black-shelled mussels clamped tight";
}

public sealed class Shell : Item
{
    public override string ItemId      => "shell";
    public override string DisplayName => "Shell";
    public override string Description => "A pearly fragment of seashell, edges worn smooth by tide";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.05f;
}

public sealed class Seaweed : Item
{
    public override string ItemId      => "seaweed";
    public override string DisplayName => "Seaweed";
    public override string Description => "A heavy strand of brown seaweed, cool and slick";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.2f;
}

public sealed class Driftwood : Item
{
    public override string ItemId      => "driftwood";
    public override string DisplayName => "Driftwood";
    public override string Description => "A water-pale piece of driftwood, salt-cracked";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 0.8f;
}

public sealed class RopeFragment : Item
{
    public override string ItemId      => "rope_fragment";
    public override string DisplayName => "Rope Fragment";
    public override string Description => "A frayed length of tarred rope cut by a tide-rock";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.2f;
}
