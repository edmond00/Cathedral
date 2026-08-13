using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Save;

/// <summary>
/// Everything about the party that survives a run — the party's counterpart to
/// <see cref="Cathedral.Game.LocationInstanceState"/>, and the single place the answer is written down.
///
/// <para><b>Why this is a snapshot and not the live objects.</b> A location's state gets away with
/// being plain data held by reference: <c>AttachTo</c> hands its dictionaries to the scene, gameplay
/// writes land in the saved object, and there is no save step at all. The party cannot work that way.
/// It is a live behavioural graph — polymorphic (items, modi mentis, humors, wounds), reference-shared
/// (a modus mentis is one object in two places), and full of get-only collections. Flattening it would
/// gut it. So the party is <i>captured</i> instead, and the domain keeps no knowledge of the save
/// format.</para>
///
/// <para><b>The contract for anything added here</b> — short on purpose, because a save file reads and
/// writes exactly these fields:</para>
///
/// <list type="number">
/// <item><b>Plain data only.</b> Ids, enums, primitives and lists of them. No domain object
/// references — they would serialise the graph twice and rebuild it wrong (see rule 3).</item>
/// <item><b>Keyed by a stable id.</b> A registry id, a type name, or an enum. Never a <c>Guid</c>,
/// never an index into something that gets rebuilt. Type names are used where no id exists, because a
/// rename is a deliberate act that a version bump can cover, where reflection order is not.</item>
/// <item><b>Captured, not shared.</b> The inverse of the location rule. <see cref="Rebuild"/>
/// constructs a brand-new graph; anything held by reference here would alias the dead run into the
/// new one.</item>
/// <item><b>Wired in one place.</b> A new field on a party type means one line in
/// <see cref="Capture"/>, one line in <see cref="Rebuild"/>, and one line in the save coverage audit.
/// Nothing else should reach into the party to persist it.</item>
/// </list>
///
/// <para><b>Nothing derivable is stored.</b> Derived stats are formula objects rediscovered by
/// reflection; current HP is <c>MaxHp − Wounds.Count</c>; age is <c>GameClock.Days − BirthTimeDays</c>;
/// memory capacities come from organ scores. Storing any of them would let a save contradict the body
/// it belongs to.</para>
/// </summary>
public sealed class PartyState
{
    // ── The party ─────────────────────────────────────────────────────────────
    public MemberDto Protagonist { get; set; } = new();
    public List<MemberDto> Companions { get; set; } = new();

    // ── Protagonist-only facts ────────────────────────────────────────────────
    // Flat rather than a ProtagonistDto subclass: polymorphic serialisation would need a
    // discriminator and buys nothing, since there is exactly one protagonist.
    public string? CharacterName { get; set; }
    public int CurrentLocationId { get; set; }
    public List<string> Journal { get; set; } = new();
    public int Gold { get; set; }
    public int Silver { get; set; }
    public int Copper { get; set; }
    public ChildhoodDto Childhood { get; set; } = new();

    /// <summary>Recorded routines. Already plain data with public setters, so stored as-is.</summary>
    public List<Routine> Routines { get; set; } = new();

    // ═════════════════════════════════════════════════════════════════════════
    // Capture
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Snapshots <paramref name="protagonist"/> and the whole party.</summary>
    public static PartyState Capture(Protagonist protagonist)
    {
        if (protagonist == null) throw new ArgumentNullException(nameof(protagonist));

        return new PartyState
        {
            Protagonist       = CaptureMember(protagonist),
            Companions        = protagonist.CompanionParty.Select(CaptureMember).ToList(),
            CharacterName     = protagonist.CharacterName,
            CurrentLocationId = protagonist.CurrentLocationId,
            Journal           = new List<string>(protagonist.JournalEntries),
            Gold              = protagonist.Party.Gold,
            Silver            = protagonist.Party.Silver,
            Copper            = protagonist.Party.Copper,
            Childhood         = CaptureChildhood(protagonist.ChildhoodHistory),
            Routines          = new List<Routine>(protagonist.RecordedRoutines),
        };
    }

    private static MemberDto CaptureMember(PartyMember m)
    {
        var dto = new MemberDto
        {
            SpeciesId         = SpeciesRegistry.IdOf(m.Species),
            DisplayName       = m.DisplayName,
            BiologicalSexMale = m.BiologicalSexMale,
            BirthTimeDays     = m.BirthTimeDays,
            ArchetypeId       = (m as EnemyCombatant)?.ArchetypeId ?? "",
            PersistentId      = (m as EnemyCombatant)?.PersistentId ?? "",
        };

        // Organ scores. The body-part/organ tree is structural and rebuilt from the anatomy factory;
        // only the leaf scores are the character.
        foreach (var part in m.BodyParts.SelectMany(bp => bp.Organs).SelectMany(o => o.Parts))
            dto.OrganScores[part.Id] = part.Score;

        // Modi mentis, stored ONCE. Memory slots below refer back by id, never inline — writing the
        // object in both places would rebuild two of them, and every reference-equality check in the
        // memory system (remove, archive, consolidate) would then silently no-op.
        foreach (var mm in m.ModiMentis)
            dto.ModiMentis.Add(new ModusMentisDto
            {
                Id = mm.ModusMentisId, Level = mm.Level, Xp = mm.CurrentXp,
            });

        foreach (var module in m.MemoryModules)
            dto.Memory[module.Type] = module.Slots
                .Select(s => s.ModusMentis?.ModusMentisId)
                .ToList();

        dto.Humors = new HumorsDto
        {
            CycleIndex = m.HumorQueues.CycleIndex,
            Hepar      = CaptureQueue(m.HumorQueues.Hepar),
            Paunch     = CaptureQueue(m.HumorQueues.Paunch),
            Pulmones   = CaptureQueue(m.HumorQueues.Pulmones),
            Spleen     = CaptureQueue(m.HumorQueues.Spleen),
        };

        foreach (var w in m.Wounds)
            dto.Wounds.Add(new WoundDto
            {
                Type           = w.Template.GetType().Name,
                InflictedOnDay = w.InflictedOnDay,
                ArtX           = w.ArtX,
                ArtY           = w.ArtY,
                ZoneHint       = w.WildcardZoneHint,
            });

        dto.Inventory = m.Inventory.Select(CaptureItem).ToList();
        foreach (var (anchor, items) in m.EquippedItems)
            dto.Equipped[anchor] = items.Select(CaptureItem).ToList();

        return dto;
    }

    private static List<string> CaptureQueue(HumorQueue q)
        => q.Items.Select(h => h.GetType().Name).ToList();

    private static ItemDto CaptureItem(Item item)
    {
        var dto = new ItemDto { ItemId = item.ItemId };

        // Only capture a composition that has actually been drawn. Reading Composition would force the
        // lazy draw, advancing the run-long item-composition stream as a side effect of saving.
        if (item is ConsumableItem { HasComposition: true } c)
            dto.Composition = c.Composition.Select(h => h.GetType().Name).ToList();

        if (item is IContainer container && container.Contents.Count > 0)
            dto.Contents = container.Contents.Select(CaptureItem).ToList();

        return dto;
    }

    private static ChildhoodDto CaptureChildhood(ChildhoodHistory h) => new()
    {
        Location = h.Location,
        Visited  = new List<string>(h.VisitedReminescences),
        Fragments = h.RememberedFragments
            .Select(f => new FragmentDto
            {
                ReminescenceId = f.ReminescenceId,
                FragmentName   = f.FragmentName,
                Summary        = f.FragmentSummary,
                Context        = f.ContextSummary,
            })
            .ToList(),
    };

    // ═════════════════════════════════════════════════════════════════════════
    // Rebuild
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds a fresh protagonist from this snapshot, or null when the save names content this build
    /// does not have.
    ///
    /// <para><b>Fails closed.</b> An unresolvable species, item or modus mentis aborts the whole
    /// rebuild rather than skipping the entry. Since the version gate confines a save to one build, an
    /// unknown id means corruption — and a partly-restored character is worse than a refused load in a
    /// game with one save and no going back.</para>
    ///
    /// <para><b>The caller must not install the result until it returns non-null.</b> That is what
    /// keeps a failed load from leaving a half-built protagonist in play.</para>
    /// </summary>
    public Protagonist? Rebuild()
    {
        var protagonist = new Protagonist();
        if (!RestoreMember(protagonist, Protagonist)) return null;

        protagonist.RestoreCharacterName(CharacterName);
        protagonist.CurrentLocationId = CurrentLocationId;

        protagonist.JournalEntries.Clear();
        protagonist.JournalEntries.AddRange(Journal);

        // Party is get-only and starts at zero on a fresh protagonist, so crediting is a restore.
        if (Gold   > 0) protagonist.Party.Add(CoinType.Gold,   Gold);
        if (Silver > 0) protagonist.Party.Add(CoinType.Silver, Silver);
        if (Copper > 0) protagonist.Party.Add(CoinType.Copper, Copper);

        RestoreChildhood(protagonist.ChildhoodHistory, Childhood);

        protagonist.RecordedRoutines.Clear();
        protagonist.RecordedRoutines.AddRange(Routines);

        protagonist.CompanionParty.Clear();
        foreach (var dto in Companions)
        {
            var species = SpeciesRegistry.ById(dto.SpeciesId);
            if (species == null)
            {
                Console.Error.WriteLine($"PartyState: unknown species '{dto.SpeciesId}' for companion '{dto.DisplayName}'.");
                return null;
            }

            var companion = new EnemyCombatant(dto.DisplayName, species)
            {
                ArchetypeId  = dto.ArchetypeId,
                PersistentId = dto.PersistentId,
            };
            if (!RestoreMember(companion, dto)) return null;
            protagonist.CompanionParty.Add(companion);
        }

        return protagonist;
    }

    /// <summary>
    /// Restores one body. <b>The order below is load-bearing and must not be rearranged:</b> memory
    /// capacities are derived from organ scores, so <c>InitializeMemory</c> has to run after the scores
    /// are in, or the modules come out the wrong size and every saved slot index lands somewhere else.
    /// </summary>
    private static bool RestoreMember(PartyMember m, MemberDto dto)
    {
        // 1. Organ scores (the body tree itself was built by the constructor from the species).
        foreach (var part in m.BodyParts.SelectMany(bp => bp.Organs).SelectMany(o => o.Parts))
            if (dto.OrganScores.TryGetValue(part.Id, out int score))
                part.Score = score;

        // 2. Identity and age.
        m.BiologicalSexMale = dto.BiologicalSexMale;
        m.RestoreBirthTime(dto.BirthTimeDays);

        // 3. Memory modules, sized from the scores just restored.
        m.InitializeMemory();

        // 4. Modi mentis — one instance each, held in a map so the slots below can point at the very
        //    same objects rather than at copies.
        var byId = new Dictionary<string, ModusMentis>(StringComparer.Ordinal);
        m.ModiMentis.Clear();
        foreach (var mmDto in dto.ModiMentis)
        {
            var instance = CreateModusMentis(mmDto.Id, m);
            if (instance == null)
            {
                Console.Error.WriteLine($"PartyState: unknown modus mentis '{mmDto.Id}'.");
                return false;
            }
            instance.Level     = mmDto.Level;
            instance.CurrentXp = mmDto.Xp;
            m.ModiMentis.Add(instance);
            byId[mmDto.Id] = instance;
        }

        // 5. Re-alias. ModiMentis and LearnedModiMentis are the SAME list on a live member; the
        //    queries read one and the mutations write the other, so two lists here would mean a member
        //    whose skills never change as far as anything reading them is concerned.
        m.LearnedModiMentis = m.ModiMentis;

        // 6. Memory placement, by index, using the instances from step 4.
        foreach (var (moduleType, slotIds) in dto.Memory)
        {
            var module = m.GetMemoryModule(moduleType);
            if (module == null) continue;
            for (int i = 0; i < slotIds.Count && i < module.Slots.Count; i++)
            {
                string? id = slotIds[i];
                module.Slots[i].ModusMentis = id != null && byId.TryGetValue(id, out var mm) ? mm : null;
            }
        }

        // 7. Humors. Order within a queue is the queue's meaning, so it is restored verbatim.
        if (dto.Humors != null)
        {
            if (!RestoreQueue(m.HumorQueues.Hepar,    dto.Humors.Hepar))    return false;
            if (!RestoreQueue(m.HumorQueues.Paunch,   dto.Humors.Paunch))   return false;
            if (!RestoreQueue(m.HumorQueues.Pulmones, dto.Humors.Pulmones)) return false;
            if (!RestoreQueue(m.HumorQueues.Spleen,   dto.Humors.Spleen))   return false;
            m.HumorQueues.CycleIndex = dto.Humors.CycleIndex;
        }

        // 8. Wounds, resolved against this body's own catalogue — wound ids collide across anatomies,
        //    so the type name is matched within the anatomy rather than globally.
        m.Wounds.Clear();
        var catalogue = WoundRegistry.ForAnatomy(m).ToDictionary(w => w.GetType().Name, w => w);
        foreach (var wDto in dto.Wounds)
        {
            if (!catalogue.TryGetValue(wDto.Type, out var template))
            {
                Console.Error.WriteLine($"PartyState: unknown wound '{wDto.Type}' for {m.DisplayName}.");
                return false;
            }
            m.Wounds.Add(new WoundInstance(template, wDto.InflictedOnDay, wDto.ZoneHint)
            {
                ArtX = wDto.ArtX,
                ArtY = wDto.ArtY,
            });
        }

        // 9. Items.
        m.Inventory.Clear();
        foreach (var itemDto in dto.Inventory)
        {
            var item = RestoreItem(itemDto);
            if (item == null) return false;
            m.Inventory.Add(item);
        }

        foreach (var (anchor, itemDtos) in dto.Equipped)
        {
            if (!m.EquippedItems.TryGetValue(anchor, out var slot)) continue;
            slot.Clear();
            foreach (var itemDto in itemDtos)
            {
                var item = RestoreItem(itemDto);
                if (item == null) return false;
                slot.Add(item);
            }
        }

        return true;
    }

    private static bool RestoreQueue(HumorQueue queue, List<string> typeNames)
    {
        if (typeNames.Count != HumorQueue.Capacity)
        {
            Console.Error.WriteLine(
                $"PartyState: humor queue '{queue.OrganId}' has {typeNames.Count} places, expected {HumorQueue.Capacity}.");
            return false;
        }

        var humors = new List<BodyHumor>(typeNames.Count);
        foreach (var name in typeNames)
        {
            var humor = CreateHumor(name);
            if (humor == null)
            {
                Console.Error.WriteLine($"PartyState: unknown humor '{name}'.");
                return false;
            }
            humors.Add(humor);
        }

        queue.RestoreAll(humors);
        return true;
    }

    private static Item? RestoreItem(ItemDto dto)
    {
        var item = ItemRegistry.GetById(dto.ItemId);
        if (item == null)
        {
            Console.Error.WriteLine($"PartyState: unknown item '{dto.ItemId}'.");
            return null;
        }

        if (dto.Composition != null && item is ConsumableItem consumable)
        {
            var humors = new List<BodyHumor>(dto.Composition.Count);
            foreach (var name in dto.Composition)
            {
                var humor = CreateHumor(name);
                if (humor == null)
                {
                    Console.Error.WriteLine($"PartyState: unknown humor '{name}' in item '{dto.ItemId}'.");
                    return null;
                }
                humors.Add(humor);
            }
            consumable.RestoreComposition(humors);
        }

        if (dto.Contents != null)
        {
            if (item is not IContainer container)
            {
                Console.Error.WriteLine($"PartyState: item '{dto.ItemId}' holds contents but is not a container.");
                return null;
            }
            foreach (var childDto in dto.Contents)
            {
                var child = RestoreItem(childDto);
                if (child == null) return null;
                // Through TryAdd, so the containment rules re-run — a vessel still refuses a second
                // kind of liquid, and a storage still refuses liquids at all.
                if (!container.TryAdd(child))
                {
                    Console.Error.WriteLine(
                        $"PartyState: '{dto.ItemId}' refused '{childDto.ItemId}' on restore.");
                    return null;
                }
            }
        }

        return item;
    }

    private static void RestoreChildhood(ChildhoodHistory h, ChildhoodDto dto)
    {
        h.Location = dto.Location;
        h.VisitedReminescences.Clear();
        h.VisitedReminescences.AddRange(dto.Visited);
        h.RememberedFragments.Clear();
        foreach (var f in dto.Fragments)
            h.RememberedFragments.Add((f.ReminescenceId, f.FragmentName, f.Summary, f.Context));
    }

    // ── Content resolution ────────────────────────────────────────────────────

    /// <summary>
    /// A fresh modus mentis for <paramref name="id"/>, or null when nothing claims it.
    ///
    /// <para><c>childhood_memory</c> is special-cased: it takes a constructor argument and so is
    /// deliberately absent from the registry. It is rebuilt from the childhood history, which the
    /// caller has already restored on the protagonist — which is why this takes the member.</para>
    /// </summary>
    private static ModusMentis? CreateModusMentis(string id, PartyMember member)
    {
        if (id == ChildhoodMemoryId)
            return member is Protagonist p
                ? new Narrative.ModiMentis.ChildhoodMemoryModusMentis(p.ChildhoodHistory.ToExperienceSummary())
                : null;

        var template = ModusMentisRegistry.Instance.GetModusMentis(id);
        return template == null ? null : (ModusMentis)Activator.CreateInstance(template.GetType())!;
    }

    /// <summary>
    /// The one modus mentis the registry cannot hand out: it takes a constructor argument, so the
    /// reflection registry skips it deliberately. Matched by id here rather than by type so the
    /// special case is visible at the call site.
    /// </summary>
    private const string ChildhoodMemoryId = "childhood_memory";

    private static Dictionary<string, Type>? _humorTypes;

    /// <summary>
    /// A humor from its type name. Humors carry no per-instance state at all — every property is an
    /// abstract getter — so the type name is a complete key and reconstruction is a bare
    /// <c>Activator.CreateInstance</c>.
    /// </summary>
    private static BodyHumor? CreateHumor(string typeName)
    {
        _humorTypes ??= Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BodyHumor)) && !t.IsAbstract
                     && t.GetConstructor(Type.EmptyTypes) != null)
            .ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);

        return _humorTypes.TryGetValue(typeName, out var type)
            ? (BodyHumor)Activator.CreateInstance(type)!
            : null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // The data
    // ═════════════════════════════════════════════════════════════════════════

    public sealed class MemberDto
    {
        public string SpeciesId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool? BiologicalSexMale { get; set; }
        public double BirthTimeDays { get; set; }

        /// <summary>Empty for the protagonist; set for a recruited companion.</summary>
        public string ArchetypeId { get; set; } = "";
        public string PersistentId { get; set; } = "";

        public Dictionary<string, int> OrganScores { get; set; } = new();
        public List<ModusMentisDto> ModiMentis { get; set; } = new();

        /// <summary>Slot contents by module, positionally. Null entries are empty slots, and the
        /// position matters — the working and residual modules evict from the far end.</summary>
        public Dictionary<MemoryModuleType, List<string?>> Memory { get; set; } = new();

        public HumorsDto? Humors { get; set; }
        public List<WoundDto> Wounds { get; set; } = new();
        public List<ItemDto> Inventory { get; set; } = new();
        public Dictionary<EquipmentAnchor, List<ItemDto>> Equipped { get; set; } = new();
    }

    public sealed class ModusMentisDto
    {
        public string Id { get; set; } = "";
        public int Level { get; set; }
        public int Xp { get; set; }
    }

    public sealed class HumorsDto
    {
        public int CycleIndex { get; set; }
        public List<string> Hepar { get; set; } = new();
        public List<string> Paunch { get; set; } = new();
        public List<string> Pulmones { get; set; } = new();
        public List<string> Spleen { get; set; } = new();
    }

    public sealed class WoundDto
    {
        public string Type { get; set; } = "";
        /// <summary>Null means historical — a scar carried in, which never heals.</summary>
        public double? InflictedOnDay { get; set; }
        public int? ArtX { get; set; }
        public int? ArtY { get; set; }
        public string? ZoneHint { get; set; }
    }

    public sealed class ItemDto
    {
        public string ItemId { get; set; } = "";
        /// <summary>Set only once the lazy draw has happened; null keeps the item undrawn.</summary>
        public List<string>? Composition { get; set; }
        public List<ItemDto>? Contents { get; set; }
    }

    public sealed class ChildhoodDto
    {
        public string? Location { get; set; }
        public List<string> Visited { get; set; } = new();
        public List<FragmentDto> Fragments { get; set; } = new();
    }

    /// <summary>
    /// A remembered fragment. The domain holds these as a named tuple in a get-only list, which
    /// System.Text.Json can neither name nor populate — hence a real type.
    /// </summary>
    public sealed class FragmentDto
    {
        public string ReminescenceId { get; set; } = "";
        public string FragmentName { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Context { get; set; } = "";
    }
}
