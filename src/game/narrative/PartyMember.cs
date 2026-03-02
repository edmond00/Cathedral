using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for all party members (protagonist and companions).
/// Holds the shared physical and skill state: body parts, organs, humors, derived stats,
/// inventory, and the skill set used during narrative execution.
///
/// Subclasses add their own unique data:
///   - <see cref="Protagonist"/>: journal, companion list, current location
///   - <see cref="Companion"/>: display name, relationship to protagonist
/// </summary>
public abstract class PartyMember
{
    // ── Body ─────────────────────────────────────────────────────
    private List<BodyPart> _bodyParts;
    private List<BodyHumor> _humors;

    public List<BodyPart> BodyParts => _bodyParts;
    public List<BodyHumor> Humors => _humors;
    public List<DerivedStat> DerivedStats { get; private set; }

    /// <summary>Backward-compat dict: humor name → value.</summary>
    public Dictionary<string, int> HumorValues =>
        _humors.ToDictionary(h => h.Name, h => h.Value);

    // ── Skills ───────────────────────────────────────────────────
    public List<Skill> Skills { get; set; }
    /// <summary>Alias for Skills — kept for call-site compatibility.</summary>
    public List<Skill> LearnedSkills { get; set; }

    // ── Inventory ────────────────────────────────────────────────
    public List<string> Inventory { get; set; }

    // ── Display name (subclasses define this differently) ────────
    /// <summary>Human-readable name shown in the party panel.</summary>
    public abstract string DisplayName { get; }

    // ── Constructor ──────────────────────────────────────────────
    protected PartyMember()
    {
        _bodyParts = InitializeBodyParts();
        _humors = InitializeHumors();
        DerivedStats = InitializeDerivedStats();
        Skills = new List<Skill>();
        LearnedSkills = Skills; // same reference
        Inventory = new List<string>();
    }

    // ── Body initialisation ──────────────────────────────────────
    private static List<BodyPart> InitializeBodyParts()
    {
        var rng = new Random();
        var parts = new List<BodyPart>
        {
            new EncephalonBodyPart(),
            new VisageBodyPart(),
            new TrunkBodyPart(),
            new UpperLimbsBodyPart(),
            new LowerLimbsBodyPart()
        };
        foreach (var bp in parts)
            foreach (var organ in bp.Organs)
                foreach (var part in organ.Parts)
                    part.Score = rng.Next(1, 11);
        return parts;
    }

    private static List<BodyHumor> InitializeHumors() => new()
    {
        new BlackBile(),
        new YellowBile(),
        new Appetitus(),
        new Melancholia(),
        new Ether(),
        new Phlegm(),
        new Blood(),
        new Voluptas(),
        new Laetitia(),
        new Euphoria()
    };

    private static List<DerivedStat> InitializeDerivedStats() => new()
    {
        new PerceptionStat(),
        new EnduranceStat(),
        new IntellectStat(),
        new DexterityStat(),
        new WillpowerStat()
    };

    // ── Skill initialisation ─────────────────────────────────────
    /// <summary>
    /// Populate skills with a random selection from the registry and randomise their levels.
    /// </summary>
    public void InitializeSkills(SkillRegistry registry, int skillCount = 50)
    {
        var rng = new Random();
        var obs = registry.GetObservationSkills().OrderBy(_ => rng.Next()).Take(10);
        var think = registry.GetThinkingSkills().OrderBy(_ => rng.Next()).Take(20);
        var act = registry.GetActionSkills().OrderBy(_ => rng.Next()).Take(20);
        var selected = obs.Concat(think).Concat(act).Distinct().Take(skillCount).ToList();

        Skills.Clear();
        Skills.AddRange(selected);
        foreach (var skill in Skills)
            skill.Level = rng.Next(1, 11);
    }

    // ── Skill queries ────────────────────────────────────────────
    public List<Skill> GetObservationSkills() =>
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Observation)).ToList();
    public List<Skill> GetThinkingSkills() =>
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Thinking)).ToList();
    public List<Skill> GetActionSkills() =>
        LearnedSkills.Where(s => s.Functions.Contains(SkillFunction.Action)).ToList();
    public Skill? GetSkillById(string skillId) =>
        LearnedSkills.FirstOrDefault(s => s.SkillId == skillId);

    // ── Body hierarchy queries ───────────────────────────────────
    public BodyPart? GetBodyPartById(string id) =>
        _bodyParts.FirstOrDefault(bp => bp.Id == id);
    public Organ? GetOrganById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).FirstOrDefault(o => o.Id == id);
    public OrganPart? GetOrganPartById(string id) =>
        _bodyParts.SelectMany(bp => bp.Organs).SelectMany(o => o.Parts).FirstOrDefault(p => p.Id == id);

    /// <summary>Returns the primary organ score for a skill (used for skill checks).</summary>
    public int GetOrganScoreForSkill(Skill skill)
    {
        if (skill.Organs.Length == 0) return 0;
        return GetOrganById(skill.Organs[0])?.Score ?? 0;
    }
}
