using System;
using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Narrative;

public enum OutcomeSeverity { Positive, Negative, Neutral }

/// <summary>
/// A discrete outcome that both describes itself for UI display (chip below the narration block)
/// and applies its own game-state change via <see cref="Apply"/>.
/// Concrete types live either here (narrative-only) or in Cathedral.Game.Scene (scene state).
/// </summary>
public readonly record struct OutcomeContext(
    PartyMember?                     Actor,
    Cathedral.Game.Scene.Scene?      Scene         = null,
    Cathedral.Game.Scene.PoV?        PoV           = null,
    Cathedral.Game.Npc.NpcEntity?    Npc           = null,
    string?                          PartyMemberId = null)
{
    /// <summary>Everything a scene-side consequence needs.</summary>
    public static OutcomeContext For(PartyMember actor, Cathedral.Game.Scene.Scene? scene,
                                     Cathedral.Game.Scene.PoV? pov)
        => new(actor, scene, pov);

    /// <summary>What a conversation's consequence needs: who was spoken to, and by whom.</summary>
    public static OutcomeContext ForDialogue(Cathedral.Game.Npc.NpcEntity npc, string partyMemberId,
                                             PartyMember? actor = null)
        => new(actor, Npc: npc, PartyMemberId: partyMemberId);
}

public abstract class Outcome : INarratable
{
    /// <summary>
    /// <see cref="Text"/>, so a consequence can be handed straight to the outcome narrator.
    ///
    /// <para>This is what removes a whole family of duplicate classes. Every consequence used to
    /// exist twice — once as the chip the player sees and once as a narratable the LLM is told
    /// about, with the two written separately and free to disagree. A wound was a
    /// <c>WoundInflictionOutcome</c> and a <c>WoundOutcome</c>; a fight is still a
    /// <c>FightTriggerOutcome</c> and a <c>FightTriggerOutcome</c>. One class, two renderings
    /// (<see cref="Text"/> for the chip, <see cref="Verbatim"/> for the prompt), is enough.</para>
    /// </summary>
    public string DisplayName => Text;

    /// <inheritdoc/>
    public string ToNaturalLanguageString() => Verbatim;

    /// <summary>
    /// The chip's line of text, and how it is coloured. Settable by subclasses because a few reports
    /// only learn their final shape when they run — practising a modus mentis reads differently when
    /// the practice happened to level it. Every site that renders a chip does so <i>after</i>
    /// <see cref="Apply"/>, so a report may safely rewrite these from there; nothing else may.
    /// </summary>
    public string Text { get; protected set; }
    public OutcomeSeverity Severity { get; protected set; }

    /// <summary>
    /// A short first-person verb phrase describing this outcome, written so it reads grammatically
    /// after "I " when the outcome narrator lists an action's consequences (e.g. "obtained a gold
    /// coin", "moved to the courtyard", "suffered a broken arm to my left arm"). Internal
    /// bookkeeping reports (state capture, phase transitions) carry an empty string so they drop out
    /// of the narrated list.
    /// </summary>
    public string Verbatim { get; }

    /// <summary>False for internal bookkeeping outcomes that should not appear as UI chips.</summary>
    public virtual bool ShowInUI => true;

    /// <summary>
    /// What this effect does to a routine being recorded — see <see cref="RoutineChainEffect"/>.
    ///
    /// <para><b>Declare this on any new report that moves the point of view — in space or in time —
    /// or hands off to another phase.</b> The default (<see cref="RoutineChainEffect.None"/>) says
    /// "a routine chain around this is still valid", which is right for ordinary state changes and
    /// wrong for those. The value is a flag set, so a report that does two of these things declares
    /// both (see <c>DoorUnlockOutcome</c>, which moves you and leaves a door unlocked). A forgotten
    /// <see cref="RoutineChainEffect.Movement"/> or <see cref="RoutineChainEffect.TimeShift"/> is
    /// caught at runtime — the narration controller compares the area and the period before and
    /// after applying reports and logs an error when either moved with nothing declaring it — but
    /// the routine recorded in that session is already wrong, so declare it here rather than relying
    /// on the warning.</para>
    /// </summary>
    public virtual RoutineChainEffect RoutineChainEffect => RoutineChainEffect.None;

    protected Outcome(string text, OutcomeSeverity severity, string verbatim)
    {
        Text     = text;
        Severity = severity;
        Verbatim = verbatim ?? string.Empty;
    }

    /// <summary>Apply the concrete game-state change carried by this report.</summary>
    /// <summary>
    /// Carries the consequence out. Everything an outcome could need arrives in one context, which
    /// is what let the conversation-side effects become ordinary outcomes: they wanted an NPC and a
    /// party-member id where the scene-side ones wanted a scene and a point of view, and two
    /// incompatible signatures were the only thing keeping them in a separate hierarchy.
    /// </summary>
    public virtual void Apply(OutcomeContext ctx) { }

    /// <summary>
    /// True once <see cref="Report"/> has settled this outcome's wording — i.e. something observable
    /// actually happened. Conversation outcomes used to signal that by returning null from Apply;
    /// as ordinary outcomes they signal it by never reporting, and <see cref="ShowInUI"/> follows.
    /// </summary>
    /// <summary>
    /// Stable id for this kind of consequence, e.g. <c>item_acquisition</c>. Derived from the type
    /// name so a new outcome is catalogued the moment it is written — the same trick
    /// <c>ItemRegistry</c> uses, and the reason there is no list to keep in step. Override only if a
    /// class must keep an id its name no longer matches.
    /// </summary>
    public virtual string OutcomeId
    {
        get
        {
            var name = GetType().Name;
            if (name.EndsWith("Outcome", System.StringComparison.Ordinal))
                name = name[..^"Outcome".Length];
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(name[i]));
            }
            return sb.ToString();
        }
    }

    protected bool Reported { get; private set; }

    /// <summary>Settles the chip text at Apply time, for outcomes whose wording depends on what changed.</summary>
    protected void Report(string text)
    {
        Text     = text;
        Reported = true;
    }

    /// <summary>
    /// Reports an affinity move, and stays silent when the level did not actually change — a step at
    /// the clamp boundary is a no-op and should leave no chip.
    /// </summary>
    protected void ReportAffinity(Cathedral.Game.Npc.NpcEntity npc,
                                  Cathedral.Game.Dialogue.Affinity.AffinityLevel before,
                                  Cathedral.Game.Dialogue.Affinity.AffinityLevel after)
    {
        if (before == after) return;

        // Severity follows BonusDice, not the raw enum order: Suspicious sorts highest numerically
        // (6, off the ladder) but grants nothing, so a move into it must read as a loss.
        int delta = after.BonusDice() - before.BonusDice();
        Severity = delta > 0 ? OutcomeSeverity.Positive
                 : delta < 0 ? OutcomeSeverity.Negative
                             : OutcomeSeverity.Neutral;

        Report($"{npc.DisplayName}: {before.ToShortLabel()} → {after.ToShortLabel()}");
    }
}

// ── Narrative-only concrete types (no scene dependency) ──────────────────────

/// <summary>Grants a new modus mentis to the protagonist (fresh level-1 instance).</summary>
public sealed class SkillAcquisitionOutcome : Outcome
{
    private readonly ModusMentis _template;

    public SkillAcquisitionOutcome(ModusMentis template)
        : base($"Modus mentis acquired: {template.DisplayName}", OutcomeSeverity.Positive,
               $"learned {template.DisplayName}")
    {
        _template = template;
    }

    public override void Apply(OutcomeContext ctx)
    {
        var instance = (ModusMentis)Activator.CreateInstance(_template.GetType())!;
        instance.Level = 1;
        ctx.Actor!.AcquireModusMentis(instance);
    }
}

/// <summary>
/// The lesson a successful verb teaches. Doing a thing is how the thing is learned: if the actor has
/// no modus mentis of this kind they acquire it at level 1, and if they already have one it earns
/// experience instead.
///
/// <para>This is deliberately <b>not</b> <see cref="SkillAcquisitionOutcome"/>. That one belongs to
/// the childhood reminescence, always grants a fresh level-1 instance, and places it with
/// <c>AcquireModusMentis</c> — which pushes through the typed long-term module and can permanently
/// drop something out of Residual. A verb fires on every success, so it uses
/// <c>LearnModusMentis</c> (working memory, FIFO eviction) and re-learning a known modus mentis
/// would reset it to level 1, which is why the known case awards experience and nothing else.</para>
///
/// <para><b>All three cases show a chip, and only the first narrates.</b> Learning reads as
/// "Modus mentis learned: Metalcraft"; practising something known is worded by
/// <see cref="ModusMentisXpAward.Describe"/>, the same formatter the chain and the fight log use. A
/// modus mentis already at its organ-derived ceiling earns nothing, so it shows nothing — otherwise
/// every action would end with a chip about how you cannot get any better at walking. The practice
/// chips are decided in <see cref="Apply"/> rather than in the constructor, because until the XP is
/// awarded there is no telling which of the three it is.</para>
///
/// <para>Only the learning case carries a <see cref="Outcome.Verbatim"/>. The narrator lists
/// verbatims as the sentence of what the action came to, and "I gained a point of experience" is not
/// a thing that happens in the fiction.</para>
/// </summary>
public sealed class ModusMentisGrantOutcome : Outcome
{
    private readonly ModusMentis _template;
    private readonly bool        _alreadyKnown;

    /// <summary>Set by <see cref="Apply"/> when practice actually moved the bar — see <see cref="ShowInUI"/>.</summary>
    private bool _practiceLanded;

    private ModusMentisGrantOutcome(ModusMentis template, bool alreadyKnown)
        : base(alreadyKnown ? string.Empty : $"Modus mentis learned: {template.DisplayName}",
               OutcomeSeverity.Positive,
               alreadyKnown ? string.Empty : $"learned {template.DisplayName}")
    {
        _template     = template;
        _alreadyKnown = alreadyKnown;
    }

    public override bool ShowInUI => !_alreadyKnown || _practiceLanded;

    /// <summary>
    /// Builds the grant for <paramref name="modusMentisId"/>, or null when the id is blank or does
    /// not resolve in the registry. A null return is the silent-failure case <c>--verb-audit</c>
    /// exists to catch: the verb declared a lesson nobody can learn.
    /// </summary>
    public static ModusMentisGrantOutcome? For(PartyMember actor, string? modusMentisId)
    {
        if (string.IsNullOrWhiteSpace(modusMentisId)) return null;

        var template = ModusMentisRegistry.Instance.GetModusMentis(modusMentisId);
        if (template == null)
        {
            Console.Error.WriteLine(
                $"ModusMentisGrantOutcome: no modus mentis registered as '{modusMentisId}' — " +
                "the verb or target declaring it teaches nothing. Check the id against ModusMentisRegistry.");
            return null;
        }

        // A body that cannot hold the lesson learns nothing from it. This is reachable in ordinary
        // play: a companion acts, the verb teaches the skill it always teaches, and the acting member
        // may be a beast. Granting anyway would file a skill capped at level 1 forever, since an
        // absent organ contributes nothing to the cap.
        if (!ModusMentisAnatomy.IsLearnableBy(template, actor))
        {
            Console.WriteLine(
                $"ModusMentisGrantOutcome: {actor.DisplayName} ({actor.AnatomyType}) cannot learn "
                + $"'{modusMentisId}' — the action teaches them nothing.");
            return null;
        }

        return new ModusMentisGrantOutcome(template, actor.GetModusMentisById(modusMentisId) != null);
    }

    public override void Apply(OutcomeContext ctx)
    {
        var known = ctx.Actor!.GetModusMentisById(_template.ModusMentisId);
        if (known != null)
        {
            // Observe the award rather than re-deriving its rules: AwardModusMentisXp is a no-op at
            // the organ-derived max level, rolls the bar over into a level when it fills, and reports
            // which of the three happened — so none of that logic is duplicated here, and the chip is
            // worded by the same formatter every other experience message uses.
            var award = ctx.Actor!.AwardModusMentisXp(known);

            _practiceLanded = award.Landed;
            if (award.Landed)
            {
                Text     = award.Describe();
                Severity = award.Levelled ? OutcomeSeverity.Positive : OutcomeSeverity.Neutral;
            }

            Console.WriteLine($"ModusMentisGrant: {ctx.Actor!.DisplayName} already knows {_template.DisplayName}"
                            + (_practiceLanded
                                ? $" — awarded XP ({award.CurrentXp}/{award.Threshold}, level {award.Level})"
                                : " — at max level, no XP awarded"));
            return;
        }

        var instance = (ModusMentis)Activator.CreateInstance(_template.GetType())!;
        instance.Level = 1;
        var dropped = ctx.Actor!.LearnModusMentis(instance);
        Console.WriteLine($"ModusMentisGrant: {ctx.Actor!.DisplayName} learned {_template.DisplayName}"
                        + (dropped != null ? $" (evicted {dropped.DisplayName} from working memory)" : ""));
    }
}

/// <summary>
/// One point of experience for a modus mentis the actor <b>already knows</b> — the lesson the dice
/// themselves teach, as opposed to <see cref="ModusMentisGrantOutcome"/>'s, which is the verb's.
///
/// <para>Every modus mentis that fed the roll earns it: the observation that surfaced the object,
/// the thinking that chose the goal, the action that carried it out, and (in conversation) the
/// speaking modus mentis that voiced the reply. Each of those used to be awarded in a silent loop,
/// so the player watched their memory menu change with nothing anywhere saying why. Routing them
/// through a report means each one shows its own chip, worded by
/// <see cref="ModusMentisXpAward.Describe"/> like every other experience message.</para>
///
/// <para>The chip is decided in <see cref="Apply"/>, because until the XP is awarded there is no
/// telling whether it practised, levelled, or hit the ceiling and did nothing. Nothing is narrated:
/// <see cref="Outcome.Verbatim"/> stays empty because "I gained a point of experience" is not
/// a thing that happens in the fiction.</para>
/// </summary>
public sealed class ModusMentisPracticeOutcome : Outcome
{
    private readonly string _modusMentisId;
    private bool            _landed;

    private ModusMentisPracticeOutcome(string modusMentisId)
        : base(string.Empty, OutcomeSeverity.Neutral, verbatim: string.Empty)
    {
        _modusMentisId = modusMentisId;
    }

    public override bool ShowInUI => _landed;

    /// <summary>
    /// The practice report for <paramref name="modusMentis"/>, or null when the actor does not know
    /// it. Held by id rather than by instance so the XP always lands on the actor's own copy — the
    /// chain can carry a modus mentis resolved elsewhere, and awarding a detached instance would
    /// change nothing the player can ever see.
    /// </summary>
    public static ModusMentisPracticeOutcome? For(PartyMember actor, ModusMentis? modusMentis)
    {
        if (modusMentis == null) return null;
        if (actor.GetModusMentisById(modusMentis.ModusMentisId) == null) return null;
        return new ModusMentisPracticeOutcome(modusMentis.ModusMentisId);
    }

    public override void Apply(OutcomeContext ctx)
    {
        var known = ctx.Actor!.GetModusMentisById(_modusMentisId);
        if (known == null) return;

        var award = ctx.Actor!.AwardModusMentisXp(known);
        _landed = award.Landed;
        if (!award.Landed) return;

        Text     = award.Describe();
        Severity = award.Levelled ? OutcomeSeverity.Positive : OutcomeSeverity.Neutral;
    }
}

/// <summary>Grants an item that was created outside the scene (e.g. reminescence grants).</summary>
public sealed class ItemGrantOutcome : Outcome
{
    private readonly Item _item;

    public ItemGrantOutcome(Item item)
        : base($"Item received: {item.DisplayName}", OutcomeSeverity.Positive,
               $"obtained {item.WithArticle()}")
    {
        _item = item;
    }

    public override void Apply(OutcomeContext ctx)
        => ctx.Actor!.AcquireItem(_item);
}

/// <summary>
/// Credits coins to the shared party wallet (never the inventory). Used by reminescence
/// grants such as "a gold coin you stole from a rich traveller".
/// </summary>
public sealed class CoinGrantOutcome : Outcome
{
    private readonly CoinType _coin;
    private readonly int      _amount;

    public CoinGrantOutcome(CoinType coin, int amount)
        : base($"Coins received: {amount} {coin}", OutcomeSeverity.Positive,
               $"gained {amount} {coin.ToString().ToLowerInvariant()} coin{(amount == 1 ? "" : "s")}")
    {
        _coin   = coin;
        _amount = amount;
    }

    public override void Apply(OutcomeContext ctx)
    {
        if (ctx.Actor! is Protagonist proto)
            proto.Party.Add(_coin, _amount);
    }
}

/// <summary>
/// Inflicts a wound on the protagonist. Produced by the LLM failure critic.
/// Carries a <see cref="WoundInstance"/> rather than a bare template so the wildcard zone hint —
/// where on the body the critic decided the blow landed — survives as far as the body art.
/// </summary>
public sealed class WoundInflictionOutcome : Outcome
{
    public WoundInstance Wound { get; }

    public WoundInflictionOutcome(WoundInstance wound)
        : base(FormatText(wound), OutcomeSeverity.Negative, FormatVerbatim(wound))
    {
        Wound = wound;
    }

    private static string WoundLocation(WoundInstance w)
        => (w.TargetId.Length > 0 ? w.TargetId : w.WildcardZoneHint ?? "body").Replace('_', ' ');

    private static string FormatText(WoundInstance w)
        => $"Wound: {w.WoundName} — {WoundLocation(w)}";

    private static string FormatVerbatim(WoundInstance w)
        => $"suffered {w.WoundName.ToLowerInvariant()} to my {WoundLocation(w)}";

    public override void Apply(OutcomeContext ctx)
        => ctx.Actor!.Wounds.Add(Wound);
}

/// <summary>Modifies a humor score. Apply is a no-op until HumorQueue routing is implemented.</summary>
public sealed class HumorChangeOutcome : Outcome
{
    public HumorChangeOutcome(string humorName, int amount)
        : base($"{humorName} {(amount > 0 ? "+" : "")}{amount}",
               amount > 0 ? OutcomeSeverity.Positive : OutcomeSeverity.Negative,
               $"felt my {humorName.ToLowerInvariant()} {(amount > 0 ? "rise" : "fall")}")
    { }

    // TODO: route into HumorQueue once implemented
}

/// <summary>
/// Internal: records a childhood-reminescence fragment in the protagonist's history.
/// Does not appear as a UI chip.
/// </summary>
public sealed class ChildhoodHistoryOutcome : Outcome
{
    private readonly string  _originId;
    private readonly string  _fragmentName;
    private readonly string  _summary;
    private readonly string  _contextSummary;
    private readonly string? _setLocation;

    public override bool ShowInUI => false;

    public ChildhoodHistoryOutcome(string originId, string fragmentName, string summary,
        string contextSummary = "", string? setLocation = null)
        : base(string.Empty, OutcomeSeverity.Neutral, verbatim: string.Empty)
    {
        _originId       = originId;
        _fragmentName   = fragmentName;
        _summary        = summary;
        _contextSummary = contextSummary;
        _setLocation    = setLocation;
    }

    public override void Apply(OutcomeContext ctx)
    {
        // Childhood history is ctx.Actor!-only and only produced during the (solo) reminescence
        // phase, so the acting member is always the ctx.Actor! here.
        if (ctx.Actor! is not Protagonist proto) return;
        if (_setLocation != null)
            proto.ChildhoodHistory.Location = _setLocation;
        proto.ChildhoodHistory.RecordFragment(_originId, _fragmentName, _summary, _contextSummary);
    }
}
