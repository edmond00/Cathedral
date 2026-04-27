using System;
using System.Collections.Generic;

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

    /// <summary>False for internal bookkeeping outcomes that should not appear as UI chips.</summary>
    public virtual bool ShowInUI => true;

    protected OutcomeReport(string text, OutcomeReportSeverity severity)
    {
        Text     = text;
        Severity = severity;
    }

    /// <summary>Apply the concrete game-state change carried by this report.</summary>
    public virtual void Apply(Protagonist protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov) { }
}

// ── Narrative-only concrete types (no scene dependency) ──────────────────────

/// <summary>Grants a new modus mentis to the protagonist (fresh level-1 instance).</summary>
public sealed class SkillAcquisitionOutcome : OutcomeReport
{
    private readonly ModusMentis _template;

    public SkillAcquisitionOutcome(ModusMentis template)
        : base($"Skill acquired: {template.DisplayName}", OutcomeReportSeverity.Positive)
    {
        _template = template;
    }

    public override void Apply(Protagonist protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
    {
        var instance = (ModusMentis)Activator.CreateInstance(_template.GetType())!;
        instance.Level = 1;
        protagonist.AcquireModusMentis(instance);
    }
}

/// <summary>Grants an item that was created outside the scene (e.g. reminescence grants).</summary>
public sealed class ItemGrantOutcome : OutcomeReport
{
    private readonly Item _item;

    public ItemGrantOutcome(Item item)
        : base($"Item received: {item.DisplayName}", OutcomeReportSeverity.Positive)
    {
        _item = item;
    }

    public override void Apply(Protagonist protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
        => protagonist.AcquireItem(_item);
}

/// <summary>Inflicts a wound on the protagonist. Produced by the LLM failure critic.</summary>
public sealed class WoundInflictionOutcome : OutcomeReport
{
    public Wound Wound { get; }

    public WoundInflictionOutcome(Wound wound)
        : base(FormatText(wound), OutcomeReportSeverity.Negative)
    {
        Wound = wound;
    }

    private static string FormatText(Wound w)
    {
        var loc = (w.TargetId.Length > 0 ? w.TargetId : w.WildcardZoneHint ?? "body").Replace('_', ' ');
        return $"Wound: {w.WoundName} — {loc}";
    }

    public override void Apply(Protagonist protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
        => protagonist.Wounds.Add(Wound);
}

/// <summary>Modifies a humor score. Apply is a no-op until HumorQueue routing is implemented.</summary>
public sealed class HumorChangeOutcome : OutcomeReport
{
    public HumorChangeOutcome(string humorName, int amount)
        : base($"{humorName} {(amount > 0 ? "+" : "")}{amount}",
               amount > 0 ? OutcomeReportSeverity.Positive : OutcomeReportSeverity.Negative)
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
    private readonly string? _setLocation;

    public override bool ShowInUI => false;

    public ChildhoodHistoryOutcome(string originId, string fragmentName, string summary, string? setLocation = null)
        : base(string.Empty, OutcomeReportSeverity.Neutral)
    {
        _originId     = originId;
        _fragmentName = fragmentName;
        _summary      = summary;
        _setLocation  = setLocation;
    }

    public override void Apply(Protagonist protagonist, Cathedral.Game.Scene.Scene? scene, Cathedral.Game.Scene.PoV? pov)
    {
        if (_setLocation != null)
            protagonist.ChildhoodHistory.Location = _setLocation;
        protagonist.ChildhoodHistory.RecordFragment(_originId, _fragmentName, _summary);
    }
}
