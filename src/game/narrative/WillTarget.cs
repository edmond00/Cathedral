using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative;

/// <summary>
/// What a modus mentis with the <see cref="ModusMentisFunction.Will"/> function wants to do: one
/// verb, a verb on one kind of object, or a group of verbs. Everything a will does — what it attends
/// to, what it proposes, when it refuses — is derived from this one declaration by
/// <see cref="ActionPathPlanner"/>; a will modus mentis never lists the steps that lead to it.
///
/// <para><b>Declared by type, never by name.</b> A verb is matched by its class (a base class names a
/// group: <see cref="DialogueVerb"/> is every conversation), an object by its point-of-interest or
/// element class, and a yield by its item class — the same rule that keeps every other content gate
/// a build error rather than a silent miss.</para>
///
/// <para><see cref="Purpose"/> is the one string, and it is narration, not identification: the plain
/// infinitive phrase the will is voiced with when it cannot be satisfied here ("pick a lock").</para>
/// </summary>
public sealed class WillTarget
{
    /// <summary>The verb classes that satisfy the will. A base class admits every subclass.</summary>
    public IReadOnlyList<Type> VerbTypes { get; }

    /// <summary>Extra conditions on the target, all of which must hold. Empty: any target the verb takes.</summary>
    private readonly IReadOnlyList<(Func<Element, bool> Test, string Label)> _conditions;

    /// <summary>What the will wants, as an infinitive phrase without "to" — "pick a lock".</summary>
    public string Purpose { get; }

    /// <summary>The element classes <see cref="On{TElement}"/> narrowed the target to — read by the audit.</summary>
    public IReadOnlyList<Type> OnTypes { get; }

    private WillTarget(IReadOnlyList<Type> verbTypes, IReadOnlyList<(Func<Element, bool>, string)> conditions, string purpose,
                       IReadOnlyList<Type>? onTypes = null)
    {
        VerbTypes   = verbTypes;
        _conditions = conditions;
        Purpose     = purpose;
        OnTypes     = onTypes ?? Array.Empty<Type>();
    }

    /// <summary>The will to perform <typeparamref name="TVerb"/> on anything it can be performed on.</summary>
    public static WillTarget To<TVerb>(string purpose) where TVerb : Verb
        => new(new[] { typeof(TVerb) }, Array.Empty<(Func<Element, bool>, string)>(), purpose);

    /// <summary>The will to perform any one of several verbs — a group with no common base class.</summary>
    public static WillTarget ToAny(string purpose, params Type[] verbTypes)
    {
        foreach (var t in verbTypes)
            if (!typeof(Verb).IsAssignableFrom(t))
                throw new ArgumentException($"WillTarget: {t.Name} is not a Verb");
        return new(verbTypes, Array.Empty<(Func<Element, bool>, string)>(), purpose);
    }

    /// <summary>Narrows the target to elements of type <typeparamref name="TElement"/> (a point of interest, an area…).</summary>
    public WillTarget On<TElement>() where TElement : Element
        => new(VerbTypes, _conditions.Append(((Func<Element, bool>)(e => e is TElement), $"on {typeof(TElement).Name}")).ToList(),
               Purpose, OnTypes.Append(typeof(TElement)).ToList());

    /// <summary>
    /// Narrows the target to what holds or is an item of type <typeparamref name="TItem"/>: an item
    /// element of that type, or a point of interest with one among its items — "mine gold ore" is
    /// mining an ore vein that yields it.
    /// </summary>
    public WillTarget Yielding<TItem>() where TItem : Item
        => With(e => e switch
            {
                ItemElement { Item: TItem }       => true,
                PointOfInterest poi               => poi.Items.Any(i => i.Item is TItem),
                _                                 => false,
            }, $"yielding {typeof(TItem).Name}");

    /// <summary>
    /// <see cref="Yielding{TItem}"/> for several item classes at once — herbs are both the
    /// <c>HerbItem</c> family and the plain <c>Herb</c>. <paramref name="label"/> names the set in the audit.
    /// </summary>
    public WillTarget YieldingAny(string label, params Type[] itemTypes)
    {
        bool Is(Item item) => itemTypes.Any(t => t.IsInstanceOfType(item));
        return With(e => e switch
            {
                ItemElement ie      => Is(ie.Item),
                PointOfInterest poi => poi.Items.Any(i => Is(i.Item)),
                _                   => false,
            }, $"yielding {label}");
    }

    /// <summary>
    /// Narrows the target by a test of its own, for a condition no type expresses (a person of a given
    /// species). The test must still gate on types, never on a name.
    /// </summary>
    public WillTarget Where(Func<Element, bool> test, string label) => With(test, label);

    private WillTarget With(Func<Element, bool> test, string label)
        => new(VerbTypes, _conditions.Append((test, label)).ToList(), Purpose, OnTypes);

    /// <summary>Whether <paramref name="verb"/> is one of this will's verbs, whatever it is aimed at.</summary>
    public bool Wants(Verb verb) => VerbTypes.Any(t => t.IsInstanceOfType(verb));

    /// <summary>Whether this concrete action is what the will wants.</summary>
    public bool Matches(VerbAction action)
        => Wants(action.Verb) && _conditions.All(c => c.Test(action.Target));

    /// <summary>
    /// Whether the will wants a conversation: any of its verbs opens a dialogue. Read by the audit,
    /// since such a will is expected to bear the Speaking function too; without it, it could open the
    /// conversation it wants and have nothing to say in it.
    /// </summary>
    public bool Converses => VerbRegistry.Instance.GetAll().OfType<DialogueVerb>().Any(Wants);

    /// <summary>For the audit: "UnlockDoorVerb", "MineVerb on OreVeinPointOfInterest yielding GoldOre".</summary>
    public string Describe()
    {
        var verbs = string.Join("|", VerbTypes.Select(t => t.Name));
        return _conditions.Count == 0 ? verbs : $"{verbs} {string.Join(" ", _conditions.Select(c => c.Label))}";
    }

    public override string ToString() => Describe();
}
