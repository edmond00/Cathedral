using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Management;

/// <summary>
/// Everything a fight reads off one party member that no other management tab shows, gathered for
/// the Bellic tab. Read-only: it builds a throwaway <see cref="Fighter"/> around the member and asks
/// it the same questions <c>SkillAction</c>, <c>FightResolver</c> and <c>FightModeAdapter</c> ask,
/// so the numbers on the tab are the numbers the dice get.
///
/// <para><b>What is left out, and where it is shown instead.</b> Organ scores and wounds are the
/// Anatomy tab; hit points too. A garment's own defence and a weapon's own level are the Inventory
/// tab's item panel; the vital heat a buff is paid from is the Humors tab. Two things a fight reads
/// are left out because they do not apply to a party member: the AI personality (enemies only) and
/// the status effects (they exist only on a <see cref="Fighter"/> while a fight runs).</para>
///
/// <para><b>Anatomy shows the stats raw, this shows them as the fight sees them.</b> The Anatomy
/// tab's hovered-organ block calls <c>GetRawValue</c>, which ignores wounds; a fight calls
/// <c>GetValue</c>, which does not. A wounded stat is flagged here, so a player can see what a
/// wound has cost them at the table rather than in the body.</para>
/// </summary>
public sealed class BellicProfile
{
    public enum SkillState { Ready, Broken, LimbDisabled, NextToLearn }

    /// <summary>One line of a stat section: what, how much, from which organ, and a note.</summary>
    public sealed record Stat(string Label, string Value, string Source, bool Wounded, string? Note = null);

    /// <summary>One skill under one medium — the same skill can appear under two hands.</summary>
    public sealed record SkillRow(FightingSkill Skill, SkillState State, string Roll, string Cost, string Reach,
                                  Group Owner);

    /// <summary>
    /// A medium the member can strike through: an organ, one part of an organ that strikes on its
    /// own (each hand), a whole region, or a weapon in one hand. The fight's action list has one
    /// tab per such medium, and so does this.
    /// </summary>
    public sealed class Group
    {
        public required string Key { get; init; }
        public required string Title { get; init; }
        public required string Subtitle { get; init; }
        public required int Level { get; init; }
        public string? OrganPartId { get; init; }
        public string? OrganId { get; init; }
        public string? BodyPartId { get; init; }
        public List<SkillRow> Skills { get; } = new();
    }

    public PartyMember Member { get; }
    public List<Stat> Turn { get; } = new();
    public List<Stat> Escape { get; } = new();
    public List<Stat> Striking { get; } = new();
    public List<Stat> Defence { get; } = new();
    public List<Group> Groups { get; } = new();

    /// <summary>The slip chances a fight shows when a move crosses bad ground, from equilibrium.</summary>
    public int SlipTreacherousPct { get; }
    public int SlipDangerousPct { get; }

    private readonly Fighter _probe;

    public BellicProfile(PartyMember member)
    {
        Member = member;
        _probe = new Fighter(member, 0, 0, isPlayerControlled: true, FighterFaction.Party);
        // A fighter's turn opens with a full pool; the learnable list is filtered on it.
        _probe.CurrentCineticPoints = Math.Max(1, _probe.MaxCineticPoints);

        SlipTreacherousPct = FightResolver.EstimateSlipRiskPct(TerrainType.TreacherousTerrain, _probe.EquilibriumValue);
        SlipDangerousPct   = FightResolver.EstimateSlipRiskPct(TerrainType.DangerousTerrain,   _probe.EquilibriumValue);

        BuildStats();
        BuildGroups();
    }

    // ── Stats ────────────────────────────────────────────────────

    private void BuildStats()
    {
        Turn.Add(StatOf<CineticPointsStat>("Cinetic points", v => $"{v} CP"));
        Turn.Add(StatOf<MoveSpeedStat>("Move speed", _ => $"{_probe.EffectiveMoveSpeed} tile/CP"));
        Turn.Add(StatOf<InitiativeStat>("Initiative", v => $"1d6 + {v}"));
        Turn.Add(StatOf<EquilibriumStat>("Equilibrium", v => $"{v}",
            $"slips {SlipTreacherousPct}% on treacherous, {SlipDangerousPct}% on dangerous ground"));

        Escape.Add(StatOf<RunawayDiceStat>("Running away", Dice, "one six gets you out"));
        Escape.Add(StatOf<KnockdownRecoveryStat>("Getting up", Dice, "one six, when knocked down"));
        Escape.Add(StatOf<FightLearningStat>("Learning a skill", Dice, "sixes needed rise down the list"));

        Striking.Add(StatOf<NaturalAttackStat>("Natural attack", v => "+" + Dice(v), "on every blow at someone else"));
        // A paw holds nothing it could fight with, so a beast is spared two rows of "empty".
        bool hands = Member.Can(AnatomyCapability.Handcraft);
        foreach (var (anchor, label) in new[] { (EquipmentAnchor.RightHold, "Right hand"), (EquipmentAnchor.LeftHold, "Left hand") })
        {
            if (!Member.EquippedItems.TryGetValue(anchor, out var held)) continue;
            if (held.Count == 0 && !hands) continue;
            string names = string.Join(", ", held.Select(i => i.DisplayName).Distinct());
            if (held.Count == 0)
                Striking.Add(new Stat(label, "empty", "", false));
            else if (Member.WieldedWeapons(anchor).FirstOrDefault() is { } w)
                Striking.Add(new Stat(label, $"lv {w.Level}", WeaponMediumRegistry.GetById(w.WeaponCategory)?.DisplayName.ToLowerInvariant() ?? "weapon",
                                      false, names));
            else if (held.OfType<IWeaponItem>().Any())
                Striking.Add(new Stat(label, "too heavy", "the arm cannot wield it", false, names));
            else
                Striking.Add(new Stat(label, "no weapon", "", false, names));
        }
        if (Striking.Count == 1)
            Striking.Add(new Stat("Weapons", "none", "", false, "no hand to hold one with"));

        // Every weapon blow reads the FIRST weapon held, right hand before left (FightingMedium.GetLevel).
        var weapons = HeldWeapons().ToList();
        if (weapons.Select(w => w.Weapon.Level).Distinct().Count() > 1)
            Striking.Add(new Stat("", "", "", false,
                $"every weapon blow uses lv {weapons[0].Weapon.Level}, the {weapons[0].Item.DisplayName.ToLowerInvariant()}'s"));

        var natDef = StatOf<NaturalDefenseStat>("Natural defence", Dice, "against every blow");
        Defence.Add(natDef);
        int baseDef = _probe.NaturalDefense;
        foreach (var bp in Member.BodyParts)
        {
            int armour = Member.ArmorDiceForSection(bp.Id);
            var garments = WornOver(bp.Id).ToList();
            Defence.Add(new Stat($"  {bp.DisplayName}", Dice(baseDef + armour),
                armour > 0 ? $"+{armour} armour" : "", false,
                garments.Count > 0 ? string.Join(", ", garments) : null));
        }
    }

    /// <summary>"1 die", "3 dice".</summary>
    public static string Dice(int n) => n == 1 ? "1 die" : $"{n} dice";

    private Stat StatOf<T>(string label, Func<int, string> format, string? note = null) where T : DerivedStat
    {
        var stat = Member.DerivedStats.OfType<T>().First();
        int value = stat.GetValue(Member);
        bool wounded = value < stat.GetRawValue(Member);
        return new Stat(label, format(value), SourceName(stat), wounded, note);
    }

    private string SourceName(DerivedStat stat)
    {
        var src = stat.SourceOf(Member);
        // An organ this body lacks leaves the stat at its floor: say so, rather than name a missing organ.
        if (src.OrganPartId != null) return Member.GetOrganPartById(src.OrganPartId)?.DisplayName.ToLowerInvariant() ?? "no organ";
        if (src.OrganId     != null) return Member.GetOrganById(src.OrganId)?.DisplayName.ToLowerInvariant() ?? "no organ";
        if (src.BodyPartId  != null) return Member.GetBodyPartById(src.BodyPartId)?.DisplayName.ToLowerInvariant() ?? "no organ";
        return "";
    }

    private IEnumerable<string> WornOver(string bodyPartId)
    {
        foreach (var (anchor, items) in Member.EquippedItems)
            foreach (var item in items)
                if (item is WearableItem worn && worn.Slot.AnchorsTo(anchor)
                    && ArmorSections.SectionOf(worn.Slot) == bodyPartId && worn.DefenseDice > 0)
                    yield return item.DisplayName.ToLowerInvariant();
    }

    /// <summary>Weapons wielded, right before left — the order the fight reads them in, and only what
    /// it reads: a weapon too heavy for its arm is held and not listed. One item held in two places is
    /// listed once.</summary>
    private IEnumerable<(Item Item, IWeaponItem Weapon, string Hand)> HeldWeapons()
    {
        var seen = new HashSet<Item>(ReferenceEqualityComparer.Instance);
        foreach (var (anchor, hand) in new[] { (EquipmentAnchor.RightHold, "right hand"), (EquipmentAnchor.LeftHold, "left hand") })
            foreach (var w in Member.WieldedWeapons(anchor))
                if (w is Item item && seen.Add(item)) yield return (item, w, hand);
    }

    // ── Skills ───────────────────────────────────────────────────

    private void BuildGroups()
    {
        var registry = FightingSkillRegistry.Instance;

        foreach (var cat in OrganMediumRegistry.GetAll())
        {
            var organ = Member.GetOrganById(cat.OrganId);
            if (organ == null) continue;
            if (organ.PartsAreIndependentMediums && organ.Parts.Count > 1)
                foreach (var part in organ.Parts)
                    AddGroup(new Group { Key = "organpart:" + part.Id, Title = part.DisplayName, Subtitle = "strikes on its own",
                                         Level = part.Score, OrganPartId = part.Id, OrganId = organ.Id }, cat.SkillIds);
            else
                AddGroup(new Group { Key = "organ:" + organ.Id, Title = organ.DisplayName, Subtitle = "",
                                     Level = organ.Score, OrganId = organ.Id }, cat.SkillIds);
        }

        foreach (var cat in BodyPartMediumRegistry.GetAll())
        {
            var bp = Member.GetBodyPartById(cat.BodyPartId);
            if (bp == null) continue;
            AddGroup(new Group { Key = "bodypart:" + bp.Id, Title = bp.DisplayName, Subtitle = "the whole region",
                                 Level = bp.Score, BodyPartId = bp.Id }, cat.SkillIds);
        }

        int weaponLevel = FightingMedium.Weapon.GetLevel(_probe);
        int n = 0;
        foreach (var (item, weapon, hand) in HeldWeapons())
        {
            var cat = WeaponMediumRegistry.GetById(weapon.WeaponCategory);
            if (cat == null) continue;
            AddGroup(new Group { Key = $"weapon:{n++}", Title = item.DisplayName,
                                 Subtitle = $"{cat.DisplayName.ToLowerInvariant()}, {hand}", Level = weaponLevel }, cat.SkillIds);
        }

        void AddGroup(Group g, IReadOnlyList<string> skillIds)
        {
            bool nextShown = false;
            foreach (var id in skillIds)
            {
                var skill = registry.GetById(id);
                if (skill == null) continue;
                SkillState state;
                if (Knows(skill))
                    state = !skill.IsAnyMediumAvailable(_probe) ? SkillState.LimbDisabled
                          : skill.IsBrokenFor(_probe)          ? SkillState.Broken
                          : SkillState.Ready;
                else if (!nextShown) { state = SkillState.NextToLearn; nextShown = true; }
                else continue;   // behind the next one: unreachable until it is learned
                g.Skills.Add(new SkillRow(skill, state, RollText(skill, g), $"{skill.CineticPointsCost} CP", ReachText(skill), g));
            }
            if (g.Skills.Count > 0) Groups.Add(g);
        }
    }

    /// <summary>Known through its primary or any secondary modus mentis — <c>FightingSkill</c>'s own test.</summary>
    public bool Knows(FightingSkill skill) =>
        Member.LearnedModiMentis.Any(m => m.ModusMentisId == skill.RequiredModusMentisId
                                       || skill.SecondaryModusMentisIds.Contains(m.ModusMentisId));

    /// <summary>The medium a skill rolls through under this group — the fight resolves it the same way per tab.</summary>
    public static FightingMedium? ActiveMedium(FightingSkill skill, Group g) =>
        g.BodyPartId != null ? skill.GetMediumForBodyPartId(g.BodyPartId)
      : g.OrganId    != null ? skill.GetMediumForOrganId(g.OrganId)
      : null;

    private string RollText(FightingSkill skill, Group g)
    {
        var medium = ActiveMedium(skill, g);
        return skill.EffectType switch
        {
            FightingSkillEffect.Buff => $"{skill.VitalHeatCostFor(_probe, g.OrganPartId, medium)} heat",
            FightingSkillEffect.Defense or FightingSkillEffect.DefensePosture
                => $"+{skill.TotalDice(_probe, g.OrganPartId, medium)} def",
            _ => Dice(skill.TotalDice(_probe, g.OrganPartId, medium) + (skill.IsSelfTargeting ? 0 : _probe.NaturalAttack)),
        };
    }

    private static string ReachText(FightingSkill skill) =>
        skill.IsSelfTargeting      ? "self"
      : skill.ChargeDistance > 0   ? $"run {skill.ChargeDistance}"
      : skill.MinRange > 1         ? $"{skill.MinRange}-{skill.Range}"
      : skill.Range.ToString();

    // ── Detail (select-to-expand) ────────────────────────────────

    public enum Tone { Title, Text, Dim, Number, Warn, Learn }

    /// <summary>
    /// The expanded account of one skill row: what it does, and every term behind its number, each
    /// named by where it comes from. Same arithmetic as <see cref="FightingSkill.TotalLevel"/> —
    /// effective modus mentis levels, so a wound shows up here as it does in the roll.
    /// </summary>
    public List<(string Text, Tone Tone)> Detail(SkillRow row)
    {
        var s = row.Skill;
        var g = row.Owner;
        var lines = new List<(string, Tone)>();
        var medium = ActiveMedium(s, g) ?? s.Medium;

        string kind = s.EffectType switch
        {
            FightingSkillEffect.Attack         => "Attack",
            FightingSkillEffect.Defense        => "Guard, against the next blow",
            FightingSkillEffect.DefensePosture => "Stance, against every blow until your next turn",
            FightingSkillEffect.Buff           => "Buff, paid in vital heat",
            FightingSkillEffect.SpecialMovement => "Movement",
            _                                  => "Manoeuvre",
        };
        lines.Add(($"{s.DisplayName.ToUpperInvariant()}  ·  {kind}", Tone.Title));
        lines.Add((s.Description, Tone.Text));
        if (s.EffectType == FightingSkillEffect.Attack)
            lines.Add(($"Lands on: {LandsOn(s)}", Tone.Dim));
        lines.Add(("", Tone.Text));

        int mediumLevel = medium.GetLevel(_probe, g.OrganPartId);
        // A weapon blow reads the first weapon held, whichever weapon's block it was chosen from.
        string mediumName = g.Title.ToLowerInvariant();
        if (medium.Type == MediumType.WeaponMedium && HeldWeapons().First().Item.DisplayName is { } first
            && first != g.Title)
            mediumName = $"{first.ToLowerInvariant()}, held first";
        bool isBuff = s.EffectType == FightingSkillEffect.Buff;
        lines.Add((isBuff ? "Level, which makes it cheaper" : "Where the dice come from", Tone.Dim));
        lines.Add((Term(mediumName, mediumLevel, s.MediumLevelMultiplicator), Tone.Number));
        bool anyMm = false;
        foreach (var mm in Member.LearnedModiMentis.Where(m => m.ModusMentisId == s.RequiredModusMentisId
                                                            || s.SecondaryModusMentisIds.Contains(m.ModusMentisId)))
        {
            anyMm = true;
            int eff = Member.GetEffectiveModusMentisLevel(mm);
            string name = mm.DisplayName.ToLowerInvariant();
            lines.Add((Term(name, eff, s.SkillLevelMultiplicator) + (eff != mm.Level ? $"   (wounds: lv {mm.Level} held)" : ""),
                       eff != mm.Level ? Tone.Warn : Tone.Number));
        }
        if (!anyMm)
            lines.Add(($"  no modus mentis yet: {MmName(s.RequiredModusMentisId)}"
                     + string.Concat(s.SecondaryModusMentisIds.Select(id => $", {MmName(id)}")), Tone.Dim));
        if (s.BaseDice > 0) lines.Add(($"  base                        +{s.BaseDice}", Tone.Number));
        int total = s.TotalDice(_probe, g.OrganPartId, ActiveMedium(s, g));
        if (isBuff)
        {
            int level = s.TotalLevel(_probe, g.OrganPartId, ActiveMedium(s, g));
            lines.Add(($"  heat = {FightingSkill.MaxBuffVitalHeat} - level {level}, at least {FightingSkill.MinBuffVitalHeat}"
                     + $"  = {s.VitalHeatCostFor(_probe, g.OrganPartId, ActiveMedium(s, g))} heat", Tone.Title));
        }
        else if (s.IsSelfTargeting)
            lines.Add(($"  = {Dice(total)}" + (s.EffectType is FightingSkillEffect.Defense or FightingSkillEffect.DefensePosture
                                                 ? " added to your defence" : ""), Tone.Title));
        else
        {
            lines.Add(($"  natural attack ({SourceName(Member.DerivedStats.OfType<NaturalAttackStat>().First())})".PadRight(30)
                     + $"+{_probe.NaturalAttack}", Tone.Number));
            lines.Add(($"  = {Dice(total + _probe.NaturalAttack)} against the target's defence", Tone.Title));
        }
        lines.Add(("", Tone.Text));

        var after = new List<string> { $"{s.CineticPointsCost} CP", $"reach {ReachText(s)}" };
        if (!isBuff && s.VitalHeatCost > 0) after.Add($"{s.VitalHeatCost} vital heat");
        lines.Add((string.Join("  ·  ", after), Tone.Text));
        if (s.CreateBuffEffect(_probe) is { } buff) lines.Add((buff.Description, Tone.Text));
        if (s.SpecialEffects.Length > 0)
            lines.Add(("On a hit: " + string.Join(", ", s.SpecialEffects.Select(e => e.DisplayName)), Tone.Text));
        if (s.GuardBreaksOnDamage) lines.Add(("Beaten aside by the first blow that gets through.", Tone.Text));
        if (s.RequiresSuccessfulDefense) lines.Add(("Only after turning a melee blow aside, until your turn ends.", Tone.Text));
        if (s.ChargeDistance > 0) lines.Add(($"Runs up to {s.ChargeDistance} cells to the target, then strikes.", Tone.Text));

        switch (row.State)
        {
            case SkillState.Broken:
                lines.Add(("Broken: wounds have pushed what this body can hold of it below zero.", Tone.Warn));
                break;
            case SkillState.LimbDisabled:
                lines.Add(($"Out of reach: a severe wound disables the {g.Title.ToLowerInvariant()}.", Tone.Warn));
                break;
            case SkillState.NextToLearn:
                int pos = g.OrganId != null ? s.GetMediumPositionForOrganId(g.OrganId)
                        : g.BodyPartId != null ? s.GetMediumPositionForBodyPartId(g.BodyPartId)
                        : s.MediumPosition;
                int need = Math.Max(0, pos - 1) + 1;
                int dice = Math.Max(1, _probe.FightLearningStat);
                lines.Add(($"Not known. Tried in a fight: {need} six{(need > 1 ? "es" : "")} on {Dice(dice)}"
                         + $" ({SourceName(Member.DerivedStats.OfType<FightLearningStat>().First())}).", Tone.Learn));
                break;
        }
        return lines;
    }

    private static string Term(string name, int level, int mult)
    {
        string left = $"  {name}".PadRight(Math.Max(22, name.Length + 4)) + $"lv {level}";
        string term = mult != 1 ? $"{left} x{mult}" : left;
        return term.PadRight(Math.Max(30, term.Length + 2)) + $"+{level * mult}";
    }

    private static string MmName(string id) =>
        ModusMentisRegistry.Instance.GetModusMentis(id)?.DisplayName.ToLowerInvariant() ?? id.Replace('_', ' ');

    private static string LandsOn(FightingSkill s) => s.WoundTargetMode switch
    {
        WoundTargetMode.PlayerChooses => "wherever you aim",
        WoundTargetMode.FixedBodyPart when s.TargetBodyPartId is { } ids
            => string.Join(" or ", ids.Split(',').Select(i => i.Trim().Replace('_', ' ')).Distinct()),
        _ => "anywhere on the body",
    };
}
