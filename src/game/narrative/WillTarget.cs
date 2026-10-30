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

    /// <summary>
    /// The modus mentis whose own lesson this target is bound to (<see cref="Teaching{TModusMentis}"/>),
    /// or null. Read by <c>--will-audit</c>: such a target reaches only what the content already says
    /// teaches the will, so it is not how a desire grows into a general mind.
    /// </summary>
    public string? Teaches { get; }

    private WillTarget(IReadOnlyList<Type> verbTypes, IReadOnlyList<(Func<Element, bool>, string)> conditions, string purpose,
                       IReadOnlyList<Type>? onTypes = null, string? teaches = null)
    {
        VerbTypes   = verbTypes;
        _conditions = conditions;
        Purpose     = purpose;
        OnTypes     = onTypes ?? Array.Empty<Type>();
        Teaches     = teaches;
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
               Purpose, OnTypes.Append(typeof(TElement)).ToList(), Teaches);

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
    /// Narrows the target to somebody hostile: a kind that is an enemy on sight (a wolf, a bear), or a
    /// person who already counts somebody an enemy. A fighting will wants a fight, not a crime: struck
    /// at in a city street, a peaceful passer-by was a refusal waiting at the click (a witness, the
    /// watch), and the will idled in every town for want of anybody else.
    /// </summary>
    public WillTarget OnHostile()
        => With(e => e is SceneNpc { Entity: Cathedral.Game.Npc.NpcEntity npc }
                     && (npc.Archetype.DefaultEnemy || npc.AffinityTable.HasEnemies), "on somebody hostile");

    /// <summary>
    /// Narrows the target to a beast: an animal of some size, wild or kept — never a person, and never
    /// the small life that is caught rather than hunted. What a hunter stalks.
    /// </summary>
    public WillTarget OnBeast()
        => With(e => e is SceneNpc { Entity: var entity } && entity switch
            {
                Cathedral.Game.Npc.NpcEntity npc                  => !npc.CanSpeak,
                Cathedral.Game.Npc.ShallowNpcEntity { Archetype: Cathedral.Game.Npc.ShallowNpcArchetype { IsTiny: false } } => true,
                _                                                 => false,
            }, "on a beast");

    /// <summary>
    /// Narrows the target to what holds or is an item of <paramref name="kind"/> — a family that cuts
    /// across classes (<see cref="ItemKind"/>): every mushroom, every blade, every holy thing.
    /// </summary>
    public WillTarget YieldingKind(ItemKind kind)
        => With(e => e switch
            {
                ItemElement ie      => ie.Item.Kinds.Contains(kind),
                PointOfInterest poi => poi.Items.Any(i => i.Item.Kinds.Contains(kind)),
                _                   => false,
            }, $"yielding {kind}");

    /// <summary>
    /// Narrows the target to somebody whose wares include an item of <paramref name="kind"/> — a will
    /// to buy a blade is a proposal to buy made to a trader who sells one. Read off the trader's
    /// <c>BuyCatalog</c>, which is what the purchase would offer.
    /// </summary>
    public WillTarget Selling(ItemKind kind)
        => With(e => e is SceneNpc { Entity: Cathedral.Game.Npc.NpcEntity npc }
                     && npc.BuyCatalog?.Offers.Any(o => o.Prototype.Kinds.Contains(kind)) == true,
                $"selling {kind}");

    /// <summary>
    /// Narrows a will to PLAY to one or more kinds of play (<see cref="PlayActivity"/> subclasses): a
    /// cheat wants a game of knucklebones, a slider a slope. <paramref name="label"/> names the set in
    /// the audit. A kind of play is a type, so this is as safe as <see cref="On{TElement}"/>.
    /// </summary>
    public WillTarget Playing(string label, params Type[] playTypes)
    {
        foreach (var t in playTypes)
            if (!typeof(PlayActivity).IsAssignableFrom(t))
                throw new ArgumentException($"WillTarget: {t.Name} is not a PlayActivity");
        return With(e => e is IPlayable { Play: { } play } && playTypes.Any(t => t.IsInstanceOfType(play)), $"playing {label}");
    }

    /// <summary>
    /// Narrows the target to what declares <typeparamref name="TModusMentis"/> its own lesson for one of
    /// this target's verbs (<see cref="IVerbModusMentisSource"/>, a <c>VerbModiMentis</c> entry) — the
    /// object that taught the will. Without it the mind an object taught refuses that very object: the
    /// petroglyphs teach archeology by being examined, and archeology, wanting only to dig, had no
    /// interest in them. Call it after the verbs are named; <c>--will-lesson-audit</c> lists every object
    /// that teaches a will which refuses it.
    /// </summary>
    public WillTarget Teaching<TModusMentis>() where TModusMentis : ModusMentis, new()
    {
        var id = new TModusMentis().ModusMentisId;
        var verbTypes = VerbTypes;
        var verbIds = new Lazy<string[]>(() => VerbRegistry.Instance.GetAll()
            .Where(v => verbTypes.Any(t => t.IsInstanceOfType(v))).Select(v => v.VerbId).ToArray());
        return new(VerbTypes, _conditions.Append(((Func<Element, bool>)(e => e is IVerbModusMentisSource source
                                                                            && verbIds.Value.Any(v => source.ModusMentisFor(v) == id)),
                                                  $"teaching {id}")).ToList(),
                   Purpose, OnTypes, id);
    }

    /// <summary>
    /// Narrows the target by a test of its own, for a condition no type expresses (a person of a given
    /// species). The test must still gate on types, never on a name.
    /// </summary>
    public WillTarget Where(Func<Element, bool> test, string label) => With(test, label);

    private WillTarget With(Func<Element, bool> test, string label)
        => new(VerbTypes, _conditions.Append((test, label)).ToList(), Purpose, OnTypes, Teaches);

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

/// <summary>
/// Everything a will wants: one to <see cref="Max"/> <see cref="WillTarget"/>s, each its own goal with
/// its own purpose, in the order the modus mentis declares them. The will wants <i>any</i> of them and
/// goes for the nearest (<see cref="ActionPathPlanner"/>); between two equally near, the one declared
/// first. A single target converts implicitly, so a one-target will still reads
/// <c>Will => WillTarget.To&lt;…&gt;(…)</c>.
/// </summary>
public sealed class WillTargets : IReadOnlyList<WillTarget>
{
    /// <summary>The most targets a will may declare (rule R17). Past five it stops being a desire.</summary>
    public const int Max = 5;

    private readonly IReadOnlyList<WillTarget> _targets;

    private WillTargets(IReadOnlyList<WillTarget> targets)
    {
        _targets = targets;
        Key      = string.Join(" ; ", targets.Select(t => $"{t.Purpose}: {t.Describe()}"));
    }

    /// <summary>The targets in order of preference — the first is the will's chief want.</summary>
    public static WillTargets Of(params WillTarget[] targets) => new(targets);

    public static implicit operator WillTargets(WillTarget target) => new(new[] { target });

    /// <summary>
    /// What identifies this will's wants, for the planner's cache: a modus mentis builds its targets
    /// afresh on every read, so the instance is never the same twice.
    /// </summary>
    public string Key { get; }

    public int Count => _targets.Count;
    public WillTarget this[int index] => _targets[index];
    public IEnumerator<WillTarget> GetEnumerator() => _targets.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Whether <paramref name="verb"/> is one of any target's verbs.</summary>
    public bool Wants(Verb verb) => _targets.Any(t => t.Wants(verb));

    /// <summary>Whether this concrete action is something the will wants — any of its targets.</summary>
    public bool Matches(VerbAction action) => _targets.Any(t => t.Matches(action));

    /// <summary>The first declared target the action satisfies, or null.</summary>
    public WillTarget? TargetOf(VerbAction action) => _targets.FirstOrDefault(t => t.Matches(action));

    /// <summary>The declaration index of the first target the action satisfies, or -1.</summary>
    public int IndexOf(VerbAction action)
    {
        for (int i = 0; i < _targets.Count; i++) if (_targets[i].Matches(action)) return i;
        return -1;
    }

    /// <summary>Whether any target is a conversation.</summary>
    public bool Converses => _targets.Any(t => t.Converses);

    /// <summary>
    /// Every purpose as one phrase — "fight somebody or buy a weapon" — for the sentence a will says when
    /// nothing it wants can be done here. A path names its own target's purpose instead.
    /// </summary>
    public string Purpose => _targets.Count switch
    {
        1 => _targets[0].Purpose,
        _ => string.Join(", ", _targets.Take(_targets.Count - 1).Select(t => t.Purpose)) + " or " + _targets[^1].Purpose,
    };

    /// <summary>The purpose a path is narrated with: its own target's, or every purpose when there is no path.</summary>
    public string PurposeOf(ActionPath? path) => path?.Target?.Purpose ?? Purpose;

    /// <summary>For the audit and the startup rules: every target, " ; " between them.</summary>
    public string Describe() => string.Join(" ; ", _targets.Select(t => t.Describe()));

    public override string ToString() => Describe();
}
