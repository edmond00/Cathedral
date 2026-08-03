using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Narrative;

public enum OutcomeReportSeverity { Positive, Negative, Neutral }

/// <summary>
/// A discrete outcome that both describes itself for UI display (chip below the narration block)
/// and applies its own game-state change via <see cref="Apply"/>.
/// Concrete types live either here (narrative-only) or in Cathedral.Game.Scene (scene state).
/// </summary>
public abstract class OutcomeReport
{
    public string Text { get; }
    public OutcomeReportSeverity Severity { get; }

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

    protected OutcomeReport(string text, OutcomeReportSeverity severity, string verbatim)
    {
        Text     = text;
        Severity = severity;
        Verbatim = verbatim ?? string.Empty;
    }

    /// <summary>Apply the concrete game-state change carried by this report.</summary>
    public virtual void Apply(PartyMember protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov) { }
}

// ── Narrative-only concrete types (no scene dependency) ──────────────────────

/// <summary>Grants a new modus mentis to the protagonist (fresh level-1 instance).</summary>
public sealed class SkillAcquisitionOutcome : OutcomeReport
{
    private readonly ModusMentis _template;

    public SkillAcquisitionOutcome(ModusMentis template)
        : base($"Skill acquired: {template.DisplayName}", OutcomeReportSeverity.Positive,
               $"learned {template.DisplayName}")
    {
        _template = template;
    }

    public override void Apply(PartyMember protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
    {
        var instance = (ModusMentis)Activator.CreateInstance(_template.GetType())!;
        instance.Level = 1;
        protagonist.AcquireModusMentis(instance);
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
/// <para>Only a newly-learned modus mentis shows in the UI or narrates. The ordinary case — acting
/// with something you already know — is silent, or every action would end with a chip saying you got
/// slightly better at walking.</para>
/// </summary>
public sealed class ModusMentisGrantOutcome : OutcomeReport
{
    private readonly ModusMentis _template;
    private readonly bool        _alreadyKnown;

    private ModusMentisGrantOutcome(ModusMentis template, bool alreadyKnown)
        : base(alreadyKnown ? string.Empty : $"Skill learned: {template.DisplayName}",
               OutcomeReportSeverity.Positive,
               alreadyKnown ? string.Empty : $"learned {template.DisplayName}")
    {
        _template     = template;
        _alreadyKnown = alreadyKnown;
    }

    public override bool ShowInUI => !_alreadyKnown;

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

        return new ModusMentisGrantOutcome(template, actor.GetModusMentisById(modusMentisId) != null);
    }

    public override void Apply(PartyMember protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
    {
        var known = protagonist.GetModusMentisById(_template.ModusMentisId);
        if (known != null)
        {
            protagonist.AwardModusMentisXp(known);
            Console.WriteLine($"ModusMentisGrant: {protagonist.DisplayName} already knows {_template.DisplayName} — awarded XP");
            return;
        }

        var instance = (ModusMentis)Activator.CreateInstance(_template.GetType())!;
        instance.Level = 1;
        var dropped = protagonist.LearnModusMentis(instance);
        Console.WriteLine($"ModusMentisGrant: {protagonist.DisplayName} learned {_template.DisplayName}"
                        + (dropped != null ? $" (evicted {dropped.DisplayName} from working memory)" : ""));
    }
}

/// <summary>Grants an item that was created outside the scene (e.g. reminescence grants).</summary>
public sealed class ItemGrantOutcome : OutcomeReport
{
    private readonly Item _item;

    public ItemGrantOutcome(Item item)
        : base($"Item received: {item.DisplayName}", OutcomeReportSeverity.Positive,
               $"obtained {item.WithArticle()}")
    {
        _item = item;
    }

    public override void Apply(PartyMember protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
        => protagonist.AcquireItem(_item);
}

/// <summary>
/// Credits coins to the shared party wallet (never the inventory). Used by reminescence
/// grants such as "a gold coin you stole from a rich traveller".
/// </summary>
public sealed class CoinGrantOutcome : OutcomeReport
{
    private readonly CoinType _coin;
    private readonly int      _amount;

    public CoinGrantOutcome(CoinType coin, int amount)
        : base($"Coins received: {amount} {coin}", OutcomeReportSeverity.Positive,
               $"gained {amount} {coin.ToString().ToLowerInvariant()} coin{(amount == 1 ? "" : "s")}")
    {
        _coin   = coin;
        _amount = amount;
    }

    public override void Apply(PartyMember protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
    {
        if (protagonist is Protagonist proto)
            proto.Party.Add(_coin, _amount);
    }
}

/// <summary>Inflicts a wound on the protagonist. Produced by the LLM failure critic.</summary>
public sealed class WoundInflictionOutcome : OutcomeReport
{
    public Wound Wound { get; }

    public WoundInflictionOutcome(Wound wound)
        : base(FormatText(wound), OutcomeReportSeverity.Negative, FormatVerbatim(wound))
    {
        Wound = wound;
    }

    private static string WoundLocation(Wound w)
        => (w.TargetId.Length > 0 ? w.TargetId : w.WildcardZoneHint ?? "body").Replace('_', ' ');

    private static string FormatText(Wound w)
        => $"Wound: {w.WoundName} — {WoundLocation(w)}";

    private static string FormatVerbatim(Wound w)
        => $"suffered {w.WoundName.ToLowerInvariant()} to my {WoundLocation(w)}";

    public override void Apply(PartyMember protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
        => protagonist.Wounds.Add(Wound);
}

/// <summary>Modifies a humor score. Apply is a no-op until HumorQueue routing is implemented.</summary>
public sealed class HumorChangeOutcome : OutcomeReport
{
    public HumorChangeOutcome(string humorName, int amount)
        : base($"{humorName} {(amount > 0 ? "+" : "")}{amount}",
               amount > 0 ? OutcomeReportSeverity.Positive : OutcomeReportSeverity.Negative,
               $"felt my {humorName.ToLowerInvariant()} {(amount > 0 ? "rise" : "fall")}")
    { }

    // TODO: route into HumorQueue once implemented
}

/// <summary>
/// Internal: records a childhood-reminescence fragment in the protagonist's history.
/// Does not appear as a UI chip.
/// </summary>
public sealed class ChildhoodHistoryOutcome : OutcomeReport
{
    private readonly string  _originId;
    private readonly string  _fragmentName;
    private readonly string  _summary;
    private readonly string  _contextSummary;
    private readonly string? _setLocation;

    public override bool ShowInUI => false;

    public ChildhoodHistoryOutcome(string originId, string fragmentName, string summary,
        string contextSummary = "", string? setLocation = null)
        : base(string.Empty, OutcomeReportSeverity.Neutral, verbatim: string.Empty)
    {
        _originId       = originId;
        _fragmentName   = fragmentName;
        _summary        = summary;
        _contextSummary = contextSummary;
        _setLocation    = setLocation;
    }

    public override void Apply(PartyMember protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
    {
        // Childhood history is protagonist-only and only produced during the (solo) reminescence
        // phase, so the acting member is always the protagonist here.
        if (protagonist is not Protagonist proto) return;
        if (_setLocation != null)
            proto.ChildhoodHistory.Location = _setLocation;
        proto.ChildhoodHistory.RecordFragment(_originId, _fragmentName, _summary, _contextSummary);
    }
}
