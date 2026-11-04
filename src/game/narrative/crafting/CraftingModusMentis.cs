using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Crafting;

/// <summary>
/// A modus mentis with the <see cref="ModusMentisFunction.Craft"/> function: the knowledge of one trade at
/// one station with one tool. It declares three things and everything else is derived from them:
///
/// <list type="bullet">
///   <item><see cref="Station"/> — the kind of point of interest it works at, a type carrying
///     <see cref="CraftingStationAttribute"/>. The station decides the material (<see cref="Material"/>).</item>
///   <item><see cref="Tool"/> — the implement it works with, an item type. <b>No two crafting modi mentis
///     share a station and a tool</b> (R21), so the implement combined with USE says which of them is at
///     work: hammer on the anvil is the blacksmith, tongs on the anvil the bladesmith.</item>
///   <item><see cref="Recipes"/> — one to <see cref="MaxRecipes"/> item types it can make there. A recipe
///     is paid for in the station's material, as many units as the made thing holds
///     (<see cref="ItemMaterials.Units(Item)"/>).</item>
/// </list>
///
/// <para><b>The function does two things.</b> It unlocks <see cref="UseStationVerb"/> on its station kind —
/// nobody without it is offered USE there — and it makes the modus mentis a will (<see cref="Will"/>):
/// it wants to work its station, to take up its material, to buy its material, to take up its tool, and
/// to study what teaches it. The authorization a master's workshop requires is not a target but a step
/// on the way (<c>ActionPathPlanner</c>), so a crafting mind that cannot speak still gets there: the
/// conversation is carried by whichever speaking mind the means judge picks.</para>
/// </summary>
public abstract class CraftingModusMentis : ModusMentis
{
    public const int MaxRecipes = 5;

    /// <summary>The station kind this trade works at: a <see cref="PointOfInterest"/> type with <see cref="CraftingStationAttribute"/>.</summary>
    public abstract Type Station { get; }

    /// <summary>The implement this trade works with: an <see cref="Item"/> type.</summary>
    public abstract Type Tool { get; }

    /// <summary>What it can make at its station: <see cref="Item"/> types, one to <see cref="MaxRecipes"/>.</summary>
    public abstract IReadOnlyList<Type> Recipes { get; }

    /// <summary>The material the station works, and the only one a recipe here is paid in.</summary>
    public CraftMaterial Material => CraftingStations.MaterialOf(Station)
        ?? throw new InvalidOperationException($"{ModusMentisId}: {Station.Name} is not a crafting station");

    /// <summary>The station named for sentences.</summary>
    public string StationNoun => CraftingStations.Noun(Station);

    /// <summary>The prototype of <see cref="Tool"/> from the item registry, or null when it is not an item there.</summary>
    public Item? ToolPrototype => ItemRegistry.Instance.All.FirstOrDefault(i => i.GetType() == Tool);

    /// <summary>The item id of the implement, or "" when it is not registered (R20 fails on that).</summary>
    public string ToolItemId => ToolPrototype?.ItemId ?? "";

    /// <summary>Prototypes of every recipe output, in declaration order.</summary>
    public IEnumerable<Item> RecipePrototypes
        => Recipes.Select(t => ItemRegistry.Instance.All.FirstOrDefault(i => i.GetType() == t)).OfType<Item>();

    /// <summary>What making one <paramref name="output"/> costs here, in <see cref="Material"/>.</summary>
    public static int Cost(Item output) => ItemMaterials.Units(output);

    public sealed override ModusMentisFunction[] Functions
        => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action, ModusMentisFunction.Craft };

    /// <summary>Every craft is done with the hands, which is what keeps beasts out of it.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override Memory.ModusMentisMemoryType MemoryType => Memory.ModusMentisMemoryType.Procedural;

    /// <summary>
    /// What a crafting mind wants, in this order: to work its station, to take up its material, to buy
    /// it, to take up its tool, and to study what teaches it. The targets do not read what the body
    /// carries, so the player can steer it toward any of them; the nearest one wins as for every will.
    /// </summary>
    public sealed override WillTargets? Will
    {
        get
        {
            var material = Material.Label();
            var tool     = ToolPrototype?.DisplayName.ToLowerInvariant() ?? Tool.Name.ToLowerInvariant();
            var id       = ModusMentisId;
            return WillTargets.Of(
                WillTarget.To<UseStationVerb>($"work at the {StationNoun}")
                    .OnType(Station)
                    .ForVariant(v => v is CraftingModusMentis mm && mm.ModusMentisId == id, $"as {id}"),
                WillTarget.ToAny($"take up {material} to work",
                        typeof(GatherVerb), typeof(GrabVerb), typeof(CutVerb), typeof(MineVerb), typeof(CutWoodVerb), typeof(DigVerb))
                    .YieldingMaterial(Material),
                WillTarget.To<ProposeToBuyVerb>($"buy {material}").SellingMaterial(Material),
                WillTarget.ToAny($"take up a {tool}", typeof(GatherVerb), typeof(GrabVerb))
                    .YieldingAny(tool, Tool),
                WillTarget.ToAny($"study the {StationNoun} and its work", typeof(ExamineVerb), typeof(ContemplateVerb)).TeachingOf(id));
        }
    }

    /// <summary>The places this trade belongs in. Each trade names its own (R19: area kinds only).</summary>
    public abstract override IReadOnlyList<Type> WillPlaces { get; }

    /// <summary>The crafting modi mentis <paramref name="member"/> holds for <paramref name="station"/>'s kind.</summary>
    public static IEnumerable<CraftingModusMentis> HeldFor(PartyMember member, Element station)
        => member.ModiMentis.OfType<CraftingModusMentis>().Where(mm => mm.Station.IsInstanceOfType(station));

    /// <summary>Every crafting modus mentis in the catalogue.</summary>
    public static IEnumerable<CraftingModusMentis> All
        => ModusMentisRegistry.Instance.GetAllModiMentis().OfType<CraftingModusMentis>();

    /// <summary>Whether <paramref name="item"/> is the implement of some crafting modus mentis.</summary>
    public static bool IsCraftTool(Item item) => All.Any(mm => mm.Tool == item.GetType());
}
