using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Traits;

namespace Cathedral.Game.Npc.Generation;

/// <summary>
/// Turns a bare <see cref="EnemyCombatant"/> into an individual: rolls its body, its skills, its
/// belongings, and the personality traits that make it distinct from every other member of its
/// trade. Called once from <see cref="NamedNpcArchetype.Spawn"/>.
///
/// <para><b>Determinism.</b> Every roll here comes from one <see cref="Random"/> seeded from the
/// NPC's <i>stable</i> id (<c>{archetype}_{name}</c>) — not <c>NpcId</c>, which for non-persistent
/// NPCs contains a fresh random number and would make the same person different on every visit. Same
/// id, same person, and <c>--seed</c> replays the whole village exactly.</para>
///
/// <para><b>Order matters</b>, and the pipeline is written in dependency order:</para>
/// <list type="number">
///   <item>organs — everything downstream reads their scores;</item>
///   <item>sex — the genitories score is set from it, so it must follow the organ roll;</item>
///   <item>memory modules — sized from the (now rolled) brain organs;</item>
///   <item>skills — level-capped by the organs, filed into the (now sized) memory;</item>
///   <item>belongings — the archetype's working kit and clothes;</item>
///   <item>traits — applied last, so they modify a complete person rather than a half-built one.</item>
/// </list>
/// </summary>
public static class NpcContentGenerator
{
    /// <summary>Fewest points sprinkled over the body above the baseline of 1 per organ part.</summary>
    private const int MinOrganPoints = 10;

    /// <summary>Most points sprinkled over the body.</summary>
    private const int MaxOrganPoints = 30;

    /// <summary>
    /// Share of organ points that go to the archetype's signature organs rather than anywhere.
    /// High enough that a smith reliably has arms and a shepherd reliably has eyes; low enough that
    /// two smiths are still visibly different people.
    /// </summary>
    private const double EmphasisShare = 0.6;

    /// <summary>The organ whose score <i>is</i> the NPC's sex; never touched by the random roll.</summary>
    private const string GenitoriesId = "genitories";

    /// <summary>
    /// Generates all content for one NPC and returns the traits it was dealt, so the caller can build
    /// the matching <see cref="NpcTextProfile"/> from the same three traits that shaped its body.
    /// </summary>
    public static IReadOnlyList<PersonalityTrait> Generate(
        NamedNpcArchetype archetype,
        EnemyCombatant    body,
        string            stableId,
        bool              isHuman)
    {
        // One generator for the whole NPC, seeded from its stable id and the run's master seed.
        var rng = GameRng.For($"npc_content_{stableId}");

        RollOrgans(archetype, body, rng);
        if (isHuman) AlignGenitoriesToSex(body, rng);
        StampAge(archetype, body, rng);

        body.InitializeMemory();
        GrantArchetypeSkills(archetype, body, rng);
        GiveBelongings(archetype, body, rng);

        var traits = PersonalityTraitRegistry.Instance.Deal(archetype.ArchetypeId, rng);
        foreach (var trait in traits)
            trait.ApplyGameplay(body, rng);

        EnsureBornAlive(body);
        SettleSkillLevels(body);
        return traits;
    }

    /// <summary>
    /// Guarantees the NPC is alive when generation finishes, by dropping historical wounds until at
    /// least one hit point remains.
    ///
    /// <para><b>An NPC could be born dead.</b> <c>CurrentHp</c> is <c>MaxHp − Wounds.Count</c> and
    /// <c>MaxHp</c> is the trunk score, while traits both add historical wounds and adjust organ
    /// scores — including downwards. Deal a low-trunk body two or three wound-bearing traits and the
    /// arithmetic reaches zero, which makes <see cref="Npc.NpcEntity.IsAlive"/> false before anyone
    /// has touched them. They stay in <c>Scene.Npcs</c> and keep their schedule, but
    /// <c>Scene.GetNpcsAt</c> filters the dead out, so <b>every</b> verb refuses them: a person
    /// standing in a room, described in prose, whom the player cannot talk to, rob, or strike. It is
    /// rare and seed-dependent, which is why it surfaced as an intermittent <c>--verb-audit</c>
    /// warning rather than as a reproducible complaint.</para>
    ///
    /// <para>Trimming the wounds rather than raising the trunk keeps the body the archetype rolled:
    /// the scar that has to go is the cheapest thing to lose, and the ones that remain are still the
    /// person's history. Historical wounds only (<c>InflictedOnDay == null</c>) — nothing generated
    /// here was inflicted in play, but the filter states the rule rather than assuming it.</para>
    /// </summary>
    private static void EnsureBornAlive(EnemyCombatant body)
    {
        if (body.CurrentHp > 0) return;

        int dropped = 0;
        while (body.CurrentHp <= 0)
        {
            int i = body.Wounds.FindLastIndex(w => w.InflictedOnDay == null);
            if (i < 0) break;   // nothing left to give back — see the floor below
            body.Wounds.RemoveAt(i);
            dropped++;
        }

        Console.Error.WriteLine(
            $"NpcContentGenerator: {body.DisplayName} was generated with no hit points "
            + $"(trunk {body.MaxHp}, {body.Wounds.Count + dropped} trait wounds) — dropped {dropped} "
            + $"historical wound(s) so they are alive. An NPC nobody can interact with is not content.");
    }

    /// <summary>Composes the NPC's text from the archetype's defaults plus the same dealt traits.</summary>
    public static NpcTextProfile BuildTextProfile(
        string observationHint,
        string personaPrompt,
        IReadOnlyList<PersonalityTrait> traits,
        string stableId)
    {
        // A separate stream from the content roll: adding a trait that draws differently must not
        // shift the appearance of every other NPC in the village.
        var rng     = GameRng.For($"npc_text_{stableId}");
        var profile = new NpcTextProfile(observationHint, personaPrompt, traits.Select(t => t.TraitId).ToList());

        foreach (var trait in traits)
            trait.ApplyText(profile, rng);

        return profile;
    }

    // ── 1. Organs ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sprinkles 10–30 points over the body, which starts flat at 1 per organ part. Points land on
    /// the archetype's signature organs <see cref="EmphasisShare"/> of the time and anywhere the rest
    /// of the time; a point aimed at a maxed-out part is re-aimed rather than wasted, so a saturated
    /// body does not quietly swallow the roll.
    /// </summary>
    private static void RollOrgans(NamedNpcArchetype archetype, PartyMember body, Random rng)
    {
        var parts = body.BodyParts
            .SelectMany(bp => bp.Organs)
            .SelectMany(o => o.Parts)
            .Where(p => p.Id != GenitoriesId)      // sex is decided, not rolled
            .ToList();
        if (parts.Count == 0) return;

        var emphasis = parts.Where(p => archetype.OrganEmphasis.Contains(p.Id)).ToList();

        int points = rng.Next(MinOrganPoints, MaxOrganPoints + 1);
        for (int i = 0; i < points; i++)
        {
            // A handful of attempts to find a part with headroom; give up on this point if the body
            // really is full, and stop entirely if nothing anywhere can take another point.
            bool placed = false;
            for (int attempt = 0; attempt < 8 && !placed; attempt++)
            {
                var pool   = emphasis.Count > 0 && rng.NextDouble() < EmphasisShare ? emphasis : parts;
                var target = pool[rng.Next(pool.Count)];
                if (target.Score >= target.MaxScore) continue;
                target.Score++;
                placed = true;
            }

            if (!placed && parts.All(p => p.Score >= p.MaxScore)) break;
        }
    }

    // ── 2. Sex ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Makes the genitories score agree with the sex the NPC was already given.
    ///
    /// <para>
    /// Sex itself is decided back in <see cref="NamedNpcArchetype.Spawn"/>, before the name is
    /// generated — the name has to know whether it is naming a man or a woman, and the stable id this
    /// generator seeds from is built <i>from</i> that name, so sex cannot be decided here without
    /// arguing in a circle. What happens here is the coupling: a woman scores 0 genitories, a man at
    /// least 1, so <c>GenderStat</c> gives the same answer whichever of its two sources it reads.
    /// </para>
    ///
    /// <para>
    /// The coupling is not free. Genitories feed <c>AttackBonusStat</c> — bonus dice on every
    /// offensive roll — and the level cap of the modi mentis drawing on that organ, so a woman
    /// genuinely gives up those dice. That is the intended design here, not an oversight.
    /// </para>
    /// </summary>
    private static void AlignGenitoriesToSex(PartyMember body, Random rng)
    {
        var genitories = body.GetOrganPartById(GenitoriesId);
        if (genitories == null || body.BiologicalSexMale is not bool male) return;

        genitories.Score = male ? rng.Next(1, genitories.MaxScore + 1) : 0;
    }

    /// <summary>
    /// Stamps the NPC's age from its archetype's band. Runs after the organ roll because
    /// <see cref="PartyMember.SetAgeAtCreation"/> clamps against the heart-derived lifetime — roll a
    /// weak heart and the upper end of a wide band is quietly pulled back, so nobody is born already
    /// past their death date.
    /// </summary>
    private static void StampAge(NamedNpcArchetype archetype, PartyMember body, Random rng)
    {
        int lo = Math.Min(archetype.MinAgeDays, archetype.MaxAgeDays);
        int hi = Math.Max(archetype.MinAgeDays, archetype.MaxAgeDays);
        body.SetAgeAtCreation(rng.Next(lo, hi + 1));
    }

    // ── 3–4. Skills ────────────────────────────────────────────────────────────

    /// <summary>
    /// Samples this archetype's skills, spread evenly across the perceiving, thinking and acting
    /// pools by round-robin.
    ///
    /// <para>
    /// The even spread is deliberate: the old shared sampler concatenated the three pools and took
    /// from the front, so an NPC holding eight or fewer skills drew nothing but observation ones and
    /// could never receive any of the action-only or thinking-only modi mentis at all.
    /// </para>
    /// </summary>
    private static void GrantArchetypeSkills(NamedNpcArchetype archetype, PartyMember body, Random rng)
    {
        var registry = ModusMentisRegistry.Instance;

        // Filtered to what this body can hold before anything is sampled, not after. Sampling first
        // and refusing later would silently shorten the roster — a wolf drawing six skills off the
        // global pool would keep only the two its anatomy allows — so the anatomy narrows the pool and
        // the archetype still gets the count it asked for.
        List<ModusMentis> Pool(IEnumerable<ModusMentis> source)
            => Shuffled(ModusMentisAnatomy.LearnableBy(source, body), rng);

        var pools = new[]
        {
            Pool(registry.GetObservationModiMentis()),
            Pool(registry.GetThinkingModiMentis()),
            Pool(registry.GetActionModiMentis()),
        };

        var ordered = new List<ModusMentis>();
        for (int depth = 0; ordered.Count < archetype.ModiMentisCount; depth++)
        {
            bool any = false;
            foreach (var pool in pools)
            {
                if (depth >= pool.Count) continue;
                ordered.Add(pool[depth]);
                any = true;
                if (ordered.Count >= archetype.ModiMentisCount) break;
            }
            if (!any) break;                       // every pool exhausted
        }

        // Start from an empty sheet: the base constructor leaves ModiMentis empty, but a caller may
        // have populated it, and a doubled skill list would silently overfill memory.
        body.ModiMentis.Clear();
        foreach (var template in ordered.DistinctBy(m => m.ModusMentisId))
            NpcSkillGrant.Grant(body, template.ModusMentisId, rng);
    }

    private static List<ModusMentis> Shuffled(IEnumerable<ModusMentis> source, Random rng)
        => source.OrderBy(_ => rng.Next()).ToList();

    /// <summary>
    /// Re-clamps every skill to what the finished body can sustain.
    ///
    /// <para>
    /// Skills are rolled before traits, and a trait can blind an eye, break a back or spend an organ
    /// point — all of which lower the organ-derived level cap after the fact. For a character who is
    /// injured <i>during play</i> the game deliberately lets the level stand and simply stops
    /// awarding XP, which is the right answer there: you keep what you learned. But an NPC is being
    /// created already carrying that injury, and someone born half-blind should not start above what
    /// a half-blind body supports. So the cap is applied once here, at the end, when the body is
    /// finally settled.
    /// </para>
    /// </summary>
    private static void SettleSkillLevels(PartyMember body)
    {
        foreach (var mm in body.ModiMentis)
        {
            int cap = Math.Max(1, body.GetMaxLevelForModusMentis(mm));
            if (mm.Level > cap) mm.Level = cap;
        }
    }

    // ── 5. Belongings ──────────────────────────────────────────────────────────

    /// <summary>
    /// Gives the NPC the archetype's working kit and clothes, plus a random handful of its optional
    /// extras — the variation that stops every miller carrying an identical pack.
    /// </summary>
    private static void GiveBelongings(NamedNpcArchetype archetype, PartyMember body, Random rng)
    {
        foreach (var factory in archetype.Loadout)
            NpcBelongings.Give(body, factory());

        var optional = archetype.OptionalLoadout;
        if (optional.Count == 0) return;

        int take = rng.Next(0, Math.Min(optional.Count, 3) + 1);
        foreach (var factory in optional.OrderBy(_ => rng.Next()).Take(take))
            NpcBelongings.Give(body, factory());
    }
}
