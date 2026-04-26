namespace Cathedral.Audio;

/// <summary>A single note event in a generated phrase, with pre-computed timing.</summary>
public struct NoteEvent
{
    public int ScaleIdx;
    public int MidiNote;
    public int DurationMs;
    /// <summary>0 = legato (no gap before next note).</summary>
    public int RestAfterMs;
    /// <summary>True for the first note of a phrase; triggers a velocity accent in the track loop.</summary>
    public bool IsAccented;
    /// <summary>Per-note velocity multiplier from the phrase dynamic arc (crescendo/decrescendo/arch).
    /// Applied in the track loop: rawVel * VelocityMult.</summary>
    public float VelocityMult;
}

/// <summary>
/// Stateless procedural music composition helpers.
/// All methods are pure functions — pass <see cref="Random"/> explicitly.
/// </summary>
public static class ProceduralMidiComposer
{
    // GM patch numbers (0-based, as sent in ProgramChangeEvent)
    public const int PatchChurchOrgan    = 19; // #20 — Pipe Organ
    public const int PatchChoirAahs      = 52; // #53 — Choir Aahs
    public const int PatchViolinStrings  = 48; // #49 — String Ensemble 1
    public const int PatchTremoloStrings = 44; // #45 — Tremolo Strings — creepy/tense
    public const int PatchFlute          = 73; // #74 — Flute
    public const int PatchOboe           = 68; // #69 — Oboe — mournful/urgent
    public const int PatchHarpsichord    = 6;  // #7  — Harpsichord — bright/tavern
    public const int PatchPadBowed       = 92; // #93 — Pad 5 (bowed) — ethereal/mystery

    /// <summary>
    /// Picks the next MIDI note using a weighted random walk within [minIdx, maxIdx].
    /// Weighted step probabilities create natural melodic motion with occasional organum-style leaps.
    /// </summary>
    public static int GetNextNote(int[] scale, int lastNoteIdx, Random rng, float sadness,
        int minIdx, int maxIdx, int droneNote = -1)
    {
        // Build move weights: step ±1, skip ±2, leap (4th = ~2-3 steps), repeat
        // Sadness increases preference for descending motion
        double ascendBias  = 1.0 + (1.0 - sadness) * 0.35 - sadness * 0.4; // 1.35 (dance) → 0.6 (lament)
        double descendBias = 1.0 + sadness * 0.4;                            // 1.0 → 1.4

        var candidates = new List<(int idx, double weight)>();

        void Add(int idx, double w)
        {
            if (idx >= minIdx && idx <= maxIdx)
                candidates.Add((idx, w));
        }

        Add(lastNoteIdx - 1, 30.0 * descendBias); // step down
        Add(lastNoteIdx + 1, 30.0 * ascendBias);  // step up
        Add(lastNoteIdx - 2, 12.0 * descendBias); // skip down
        Add(lastNoteIdx + 2, 12.0 * ascendBias);  // skip up
        Add(lastNoteIdx,      6.0);                // repeat
        Add(lastNoteIdx - 3,  4.0 * descendBias); // small leap down (organum)
        Add(lastNoteIdx + 3,  4.0 * ascendBias);  // small leap up  (organum)
        Add(lastNoteIdx - 4,  2.0 * descendBias); // 4th down
        Add(lastNoteIdx + 4,  2.0 * ascendBias);  // 4th up

        // Consonance boost: weight candidates toward stable intervals with the drone
        if (droneNote >= 0)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                int interval = (scale[candidates[i].idx] - droneNote + 144) % 12;
                double boost = interval switch {
                    0 or 12 => 2.0, // unison / octave
                    7       => 1.9, // perfect fifth
                    3 or 4  => 1.6, // minor / major third
                    8 or 9  => 1.3, // minor / major sixth
                    _       => 1.0, // dissonant — no boost
                };
                candidates[i] = (candidates[i].idx, candidates[i].weight * boost);
            }
        }

        if (candidates.Count == 0)
        {
            // Fallback: pick random note in range
            return rng.Next(minIdx, maxIdx + 1);
        }

        double totalWeight = candidates.Sum(c => c.weight);
        double pick = rng.NextDouble() * totalWeight;
        double acc  = 0;
        foreach (var (idx, w) in candidates)
        {
            acc += w;
            if (pick <= acc)
                return idx;
        }
        return candidates[^1].idx;
    }

    // ── Phrase-based generation ─────────────────────────────────────────────

    /// <summary>
    /// Generates a melodic phrase with mood-driven contour and rhythmic variety.
    /// <para>Contours: Arch (balanced), Descent (sadness/lament), Ascending (dance/hope).</para>
    /// <para>Cadence: 50% tonic, 30% dominant, 20% mediant — avoids always resolving to root.</para>
    /// <para>Fear: shorter phrases, broken rhythms, chromatic wrong-notes.</para>
    /// <para>Mystery: occasional within-phrase silences (breath).</para>
    /// </summary>
    public static NoteEvent[] GenerateMelodyPhrase(
        int[] scale, int startIdx, int minIdx, int maxIdx,
        float sadness, float fear, float mystery, double bpm, Random rng,
        int[]? motifContour = null)
    {
        double beatMs = 60_000.0 / bpm;
        // Fear shortens phrases (frantic/broken); sadness also shortens (contemplative)
        int maxLen    = fear > 0.6f ? 5 : (sadness > 0.5f ? 6 : 8);
        int minLen    = fear > 0.6f ? 2 : 4;
        int phraseLen = rng.Next(minLen, maxLen + 1);
        var rhythm = PickRhythmPattern(phraseLen, sadness, fear, rng);

        // ── Contour selection ─────────────────────────────────────────────────
        // Weights are mood-driven: sadness favours Descent, low-sadness favours Ascending.
        int rangeSize  = maxIdx - minIdx;
        double descentW   = sadness * 1.5;
        double ascendingW = (1.0 - sadness) * 1.2 * (1.0 - fear * 0.8);
        double archW      = 1.0;
        double totalCW    = archW + descentW + ascendingW;
        double cPick      = rng.NextDouble() * totalCW;

        int arcStart, peakIdx, cadenceIdx, peakPos;

        if (cPick < descentW)
        {
            // Descent: start near top, fall all the way to cadence
            arcStart   = Math.Clamp(maxIdx - rng.Next(0, rangeSize / 4 + 1), minIdx, maxIdx);
            peakIdx    = arcStart;
            cadenceIdx = PickCadenceNote(minIdx, maxIdx, rng);
            peakPos    = 0;
        }
        else if (cPick < descentW + ascendingW)
        {
            // Ascending: start near bottom, climb — cadence stays high (dominant or peak)
            // so the phrase doesn't crash back down after building tension.
            arcStart   = minIdx + rng.Next(0, rangeSize / 4 + 1);
            peakIdx    = Math.Clamp(arcStart + rng.Next(4, 7), minIdx, maxIdx);
            // 60% land on the peak itself, 40% on dominant — both sound like arriving high
            cadenceIdx = rng.NextDouble() < 0.60
                ? peakIdx
                : Math.Clamp(minIdx + 4, minIdx, maxIdx);  // dominant (~5th)
            peakPos    = phraseLen - 2; // leave room for cadence note after the peak
        }
        else
        {
            // Arch: low-mid start, rise to peak, fall to cadence
            arcStart   = minIdx + rng.Next(0, Math.Max(1, rangeSize / 3));
            arcStart   = Math.Clamp(arcStart, minIdx, maxIdx);
            peakIdx    = Math.Clamp(arcStart + rng.Next(2, 5), minIdx, maxIdx);
            cadenceIdx = PickCadenceNote(minIdx, maxIdx, rng);
            peakPos    = 1 + rng.Next(0, Math.Max(1, phraseLen / 2));
        }

        // Smooth phrase start: clamp arcStart within ~1/4 range of where the voice
        // left off (startIdx), so consecutive phrases connect rather than teleport.
        int maxStartJump = Math.Max(2, rangeSize / 4);
        arcStart = Math.Clamp(arcStart, startIdx - maxStartJump, startIdx + maxStartJump);
        arcStart = Math.Clamp(arcStart, minIdx, maxIdx);
        // Ensure peakIdx is still reachable from the adjusted start
        peakIdx  = Math.Clamp(peakIdx, minIdx, maxIdx);

        var noteIndices = new int[phraseLen];
        int cur = arcStart;
        for (int i = 0; i < phraseLen; i++)
        {
            int target = (i <= peakPos) ? peakIdx : cadenceIdx;
            int dir = Math.Sign(target - cur);
            if (dir != 0)
            {
                // Clamp step to distance remaining so we never overshoot the target.
                // Overshooting causes oscillation that sounds shaky near peaks and cadences.
                int dist = Math.Abs(target - cur);
                int step = rng.NextDouble() < 0.82 ? 1 : 2;
                step = Math.Min(step, dist);
                cur = Math.Clamp(cur + dir * step, minIdx, maxIdx);
            }
            else if (i < phraseLen - 1 && rng.NextDouble() < 0.35)
            {
                cur = Math.Clamp(cur + (rng.NextDouble() < 0.5 ? 1 : -1), minIdx, maxIdx);
            }
            noteIndices[i] = cur;
        }
        noteIndices[phraseLen - 1] = cadenceIdx;

        // Motif replay: ~30% chance to reuse the previous phrase's interval contour
        // (shifted to the new arcStart). Creates a sense of theme and development.
        if (motifContour != null && motifContour.Length > 0 && rng.NextDouble() < 0.30)
        {
            int replayCur = arcStart;
            noteIndices[0] = replayCur;
            int replayLen = Math.Min(phraseLen - 1, motifContour.Length);
            for (int i = 0; i < replayLen; i++)
            {
                replayCur = Math.Clamp(replayCur + motifContour[i], minIdx, maxIdx);
                noteIndices[i + 1] = replayCur;
            }
            // Tail: stepwise neighbor walk instead of flat repetition
            for (int i = replayLen + 1; i < phraseLen - 1; i++)
            {
                int prev = noteIndices[i - 1];
                noteIndices[i] = Math.Clamp(prev + (rng.NextDouble() < 0.5 ? 1 : -1), minIdx, maxIdx);
            }
            noteIndices[phraseLen - 1] = cadenceIdx; // always land on cadence
        }

        // Phrase dynamic arc: crescendo, decrescendo, or arch shape applied per note
        float[] envelope = PickVelocityEnvelope(phraseLen, sadness, fear, rng);
        float durationFactor = (1.0f + sadness * 0.5f) * (1.0f - fear * 0.55f);
        var events = new NoteEvent[phraseLen];
        for (int i = 0; i < phraseLen; i++)
        {
            int durMs = (int)(beatMs * rhythm[i % rhythm.Length] * durationFactor);
            // Fear adds a staccato rest after inner notes
            int restMs = (i < phraseLen - 1 && fear > 0.35f)
                ? (int)(beatMs * fear * 0.22)
                : (i == phraseLen - 2 && rng.NextDouble() < 0.5 ? (int)(beatMs * 0.12) : 0);
            // Mystery: occasional within-phrase breath (sudden silence)
            if (mystery > 0.5f && i > 0 && i < phraseLen - 1 && rng.NextDouble() < (mystery - 0.5f) * 0.6f)
                restMs = Math.Max(restMs, (int)(beatMs * (0.5 + rng.NextDouble() * (double)mystery)));
            events[i] = new NoteEvent
            {
                ScaleIdx     = noteIndices[i],
                MidiNote     = scale[noteIndices[i]],
                DurationMs   = Math.Max(durMs, 80),
                RestAfterMs  = restMs,
                IsAccented   = (i == 0),
                VelocityMult = envelope[i],
            };
        }
        // Fermata on the cadence note (~25% chance): extends it 40–100% longer.
        // At high sadness the chance rises to ~45%. Creates a sense of phrase arrival.
        float fermataChance = 0.25f + sadness * 0.20f;
        if (events.Length > 0 && rng.NextDouble() < fermataChance)
        {
            ref var last = ref events[events.Length - 1];
            last.DurationMs = (int)(last.DurationMs * (1.4 + rng.NextDouble() * 0.6));
        }

        // High fear: chromatic "wrong notes" — inject ±1 semitone dissonance
        if (fear > 0.65f)
        {
            for (int i = 0; i < events.Length; i++)
            {
                if (rng.NextDouble() < (fear - 0.65f) * 1.1)
                {
                    int displace = rng.NextDouble() < 0.5 ? 1 : -1;
                    events[i].MidiNote = Math.Clamp(events[i].MidiNote + displace, 21, 108);
                }
            }
        }
        return events;
    }

    /// <summary>
    /// Generates a faster, ornamental counter-melody phrase.
    /// <para>Tension: very fast, staccato. Mystery: wide leaps, spare.</para>
    /// </summary>
    public static NoteEvent[] GenerateCounterPhrase(
        int[] scale, int startIdx, int minIdx, int maxIdx,
        float sadness, float fear, float mystery, double bpm, Random rng,
        int melodyDirection = 0)
    {
        double beatMs = 60_000.0 / bpm;
        int phraseLen = fear > 0.6f ? rng.Next(3, 7) : rng.Next(5, 10);
        var noteIndices = new int[phraseLen];
        int cur = Math.Clamp(startIdx, minIdx, maxIdx);
        int rangeSize = maxIdx - minIdx;
        // Phrase-end target in contrary motion to the melody: adds direction and counterpoint feel
        int counterCadence = melodyDirection > 0
            ? minIdx + rng.Next(0, rangeSize / 3 + 1)             // melody went up → counter resolves low
            : melodyDirection < 0
                ? maxIdx - rng.Next(0, rangeSize / 3 + 1)         // melody went down → counter resolves high
                : (minIdx + maxIdx) / 2;                           // neutral → mid-range
        counterCadence = Math.Clamp(counterCadence, minIdx, maxIdx);
        for (int i = 0; i < phraseLen; i++)
        {
            double r = rng.NextDouble();
            // Mystery prefers wider leaps; fear prefers fast stepwise runs
            int step = mystery > 0.5f
                ? (r < 0.3 ? 1 : r < 0.55 ? -1 : r < 0.72 ? 3 : r < 0.88 ? -3 : 5) // wide leaps
                : (r < 0.4 ? 1 : r < 0.70 ? -1 : r < 0.85 ? 2 : -2);                // stepwise
            // Contrary motion: with 55% probability, flip a step that moves in the same
            // direction as the melody, pushing the counter in the opposite direction
            if (melodyDirection != 0 && Math.Sign(step) == melodyDirection && rng.NextDouble() < 0.55)
                step = -step;
            // Last 2 notes: steer toward counterCadence for a proper phrase landing
            if (i >= phraseLen - 2)
                step = Math.Sign(counterCadence - cur);
            cur = Math.Clamp(cur + step, minIdx, maxIdx);
            noteIndices[i] = cur;
        }
        float durationFactor = (1.0f + sadness * 0.4f) * (1.0f - fear * 0.6f);
        var rhythm = PickRhythmPattern(phraseLen, sadness, fear, rng);
        float[] envelope = PickVelocityEnvelope(phraseLen, sadness, fear, rng);
        var events = new NoteEvent[phraseLen];
        for (int i = 0; i < phraseLen; i++)
        {
            int durMs = (int)(beatMs * rhythm[i % rhythm.Length] * durationFactor);
            int restMs = fear > 0.4f ? (int)(beatMs * fear * 0.18) : 0;
            events[i] = new NoteEvent
            {
                ScaleIdx     = noteIndices[i],
                MidiNote     = scale[noteIndices[i]],
                DurationMs   = Math.Max(durMs, 55),
                RestAfterMs  = restMs,
                IsAccented   = (i == 0),
                VelocityMult = envelope[i],
            };
        }
        return events;
    }

    /// <summary>
    /// Generates a quick arpeggio figure over chord tones in the scale.
    /// Texture track uses this in place of plain sustained notes.
    /// The figure ascends or descends through root / 3rd / 5th scale degrees.
    /// </summary>
    public static NoteEvent[] GenerateArpeggioPhrase(
        int[] scale, int minIdx, int maxIdx,
        float sadness, float fear, float mystery, double bpm, Random rng,
        int melodyHintIdx = -1)
    {
        double beatMs = 60_000.0 / bpm;

        // Chord tone offsets: root (0), 3rd (+2), 5th (+4), optionally 7th (+6) at high mystery
        int[] offsets = mystery > 0.4f && rng.NextDouble() < mystery
            ? new[] { 0, 2, 4, 6 }
            : new[] { 0, 2, 4 };

        // Anchor position: tracks melody height loosely when available
        int rangeSpan = maxIdx - minIdx;
        int baseIdx;
        if (melodyHintIdx >= minIdx && melodyHintIdx <= maxIdx)
            // Texture hovers within 1/3 range of the current melody position
            baseIdx = Math.Clamp(melodyHintIdx - rng.Next(0, Math.Max(1, rangeSpan / 4)), minIdx, maxIdx - offsets[^1]);
        else
            baseIdx = minIdx + rng.Next(0, Math.Max(1, rangeSpan / 3));

        // Ascending or descending figure (randomised each phrase)
        bool descend = rng.NextDouble() < 0.5;
        var idxList = new List<int>();
        foreach (int offset in offsets)
            idxList.Add(Math.Clamp(baseIdx + offset, minIdx, maxIdx));
        if (descend) idxList.Reverse();

        // Note duration: shorter at high fear, longer with sadness
        float noteBeatsFactor = (0.35f + sadness * 0.2f) * (1.0f - fear * 0.45f);
        var events = new NoteEvent[idxList.Count];
        for (int i = 0; i < idxList.Count; i++)
        {
            int durMs  = Math.Max((int)(beatMs * noteBeatsFactor), 40);
            int restMs = fear > 0.4f ? (int)(beatMs * fear * 0.12) : 0;
            events[i] = new NoteEvent
            {
                ScaleIdx    = idxList[i],
                MidiNote    = scale[idxList[i]],
                DurationMs  = durMs,
                RestAfterMs = restMs,
            };
        }
        return events;
    }

    /// <summary>Picks a melodically varied cadence target.
    /// 50% tonic, 30% dominant (approx. 5th scale degree), 20% mediant (3rd).
    /// Avoids always resolving to the bottom root note.</summary>
    private static int PickCadenceNote(int minIdx, int maxIdx, Random rng)
    {
        double r = rng.NextDouble();
        if (r < 0.50) return minIdx;                                    // tonic
        if (r < 0.80) return Math.Clamp(minIdx + 4, minIdx, maxIdx);   // dominant (~5th scale degree)
        return Math.Clamp(minIdx + 2, minIdx, maxIdx);                  // mediant (3rd degree)
    }

    /// <summary>
    /// Returns a per-note velocity multiplier array shaping the phrase dynamically.
    /// Sadness → decrescendo (soft fade); dance/ascending → arch; fear → flat (unpredictable).
    /// </summary>
    private static float[] PickVelocityEnvelope(int len, float sadness, float fear, Random rng)
    {
        var env = new float[len];
        // Fear bypasses the arc for unpredictability
        if (fear > 0.55f || len <= 1)
        {
            for (int i = 0; i < len; i++) env[i] = 1.0f;
            return env;
        }
        double crescW  = (1.0 - sadness) * 0.4;
        double decresW = sadness * 0.5;
        double archW   = 0.6;
        double pick    = rng.NextDouble() * (crescW + decresW + archW);
        if (pick < decresW) // decrescendo: loud start, soft end
            for (int i = 0; i < len; i++) env[i] = 1.2f - 0.45f * i / (len - 1);
        else if (pick < decresW + crescW) // crescendo: soft start, loud end
            for (int i = 0; i < len; i++) env[i] = 0.75f + 0.45f * i / (len - 1);
        else // arch: soft-loud-soft following the melodic arc
            for (int i = 0; i < len; i++)
                env[i] = 0.78f + 0.42f * (float)Math.Sin((float)i / (len - 1) * Math.PI);
        return env;
    }

    // Rhythm patterns in beat units (quarter note = 1.0)
    private static readonly float[][] SlowRhythms = {
        new[] { 3f, 1f, 2f },
        new[] { 2f, 2f, 2f },
        new[] { 4f, 2f },
        new[] { 2f, 1f, 1f, 2f },
    };
    private static readonly float[][] LivelyRhythms = {
        new[] { 1f, 1f, 2f, 1f, 1f },
        new[] { 1.5f, 0.5f, 1f, 1f, 2f },
        new[] { 1f, 2f, 1f, 2f },
        new[] { 2f, 1f, 1f, 2f },
        new[] { 1f, 1f, 1f, 3f },
    };
    // Tense/staccato: short punchy notes, big rests
    private static readonly float[][] StaccatoRhythms = {
        new[] { 0.35f, 0.35f, 0.35f, 0.7f },
        new[] { 0.5f, 0.25f, 0.5f, 0.25f, 0.5f },
        new[] { 0.3f, 0.3f, 0.3f, 0.3f, 1.0f },
        new[] { 0.5f, 0.5f, 0.5f, 1.5f },
    };
    // Celtic/folk dance: compound-triple jig and dotted reel patterns
    private static readonly float[][] JigRhythms = {
        new[] { 0.5f, 0.25f, 0.25f, 0.5f, 0.25f, 0.25f }, // compound-triple jig
        new[] { 0.75f, 0.25f, 0.75f, 0.25f, 0.75f, 0.25f }, // dotted hornpipe
        new[] { 0.33f, 0.33f, 0.34f, 0.33f, 0.33f, 0.34f }, // even jig quavers
        new[] { 0.5f, 0.5f, 0.25f, 0.25f, 0.5f, 0.5f },    // reel with skip
    };
    // Broken/disturbing: stutter then sudden silence
    private static readonly float[][] BrokenRhythms = {
        new[] { 0.12f, 0.12f, 0.12f, 1.2f, 0.04f },
        new[] { 0.1f, 0.6f, 0.1f, 0.1f, 0.8f },
        new[] { 0.25f, 0.08f, 0.08f, 0.35f, 0.9f },
        new[] { 0.05f, 0.05f, 0.05f, 0.05f, 1.4f },
    };
    private static float[] PickRhythmPattern(int phraseLen, float sadness, float fear, Random rng)
    {
        if (fear > 0.65f) return BrokenRhythms[rng.Next(BrokenRhythms.Length)];
        if (fear > 0.45f) return StaccatoRhythms[rng.Next(StaccatoRhythms.Length)];
        if (sadness < 0.25f) return JigRhythms[rng.Next(JigRhythms.Length)];
        if (sadness > 0.50f) return SlowRhythms[rng.Next(SlowRhythms.Length)];
        return LivelyRhythms[rng.Next(LivelyRhythms.Length)];
    }

    // ── Per-note timing ───────────────────────────────────────────────────────

    /// <summary>
    /// Maps sadness/fear → BPM.
    /// Sadness slows (108→30), Fear speeds (+0 to +37).
    /// sadness=0 produces jig/reel tempos (~108–145 BPM); sadness=1 produces slow lament (~25–67 BPM).
    /// </summary>
    public static double GetTempoBpm(float sadness, float fear) =>
        Math.Clamp(108.0 - sadness * 78.0 + fear * 37.0, 25.0, 145.0);

    /// <summary>Returns note duration in milliseconds based on mood, BPM, and track role.
    /// Fear makes Texture staccato; Drone always sustains.</summary>
    public static int GetNoteDurationMs(float sadness, float fear, double bpm, TrackRole role)
    {
        double beatMs = 60_000.0 / bpm;
        double beats = role switch
        {
            TrackRole.Drone   => 7.0 + sadness * 5.0,
            TrackRole.Melody  => 1.2 + sadness * 0.8,
            TrackRole.Counter => 0.7 + sadness * 0.3,
            TrackRole.Texture => (0.4 + sadness * 0.2) * (1.0 - fear * 0.65),
            _                 => 1.0,
        };
        return Math.Max((int)(beatMs * beats), 60);
    }

    /// <summary>Returns rest duration in milliseconds after a note.</summary>
    public static int GetRestMs(float sadness, float fear, double bpm, TrackRole role, Random rng)
    {
        double beatMs = 60_000.0 / bpm;
        double beats = role switch
        {
            TrackRole.Drone   => 0.04,
            TrackRole.Melody  => 0.5 + sadness * 1.0,
            TrackRole.Counter => 0.2 + sadness * 0.3,
            TrackRole.Texture => (0.15 + rng.NextDouble() * 0.3) * (1.0 - fear * 0.55),
            _                 => 0.5,
        };
        return Math.Max((int)(beatMs * beats), 0);
    }

    /// <summary>Returns MIDI velocity (0–127) for a note.
    /// Sadness softens. High fear creates lurching bimodal dynamics — sudden ff or pp accents.</summary>
    public static int GetVelocity(float sadness, float fear, TrackRole role, Random rng)
    {
        int baseVelocity = role switch
        {
            TrackRole.Drone   => 52,
            TrackRole.Melody  => 65,
            TrackRole.Counter => 40, // softer obligato — plays beneath melody
            TrackRole.Texture => 45,
            _                 => 60,
        };
        // High fear: lurching bimodal dynamics (sudden accent or sudden dropout)
        if (fear > 0.65f && rng.NextDouble() < (fear - 0.65f) * 1.3)
            return rng.NextDouble() < 0.55
                ? Math.Clamp(baseVelocity + 35, 15, 115)
                : Math.Clamp(baseVelocity - 28,  8, 115);
        int moodShift = -(int)(sadness * 18) + (int)(fear * 25);
        int swing     = (int)(4 + fear * 20);
        return Math.Clamp(baseVelocity + moodShift + rng.Next(-swing, swing + 1), 8, 115);
    }

    /// <summary>
    /// Returns the GM patch number for a track role.
    /// Uses all three mood axes for a dramatically different instrument palette:
    ///   Bright+Calm → Harpsichord/Flute (tavern).
    ///   Tense       → Oboe/TremoloStrings (urgent).
    ///   Dark+Mysterious → ChoirAahs/PadBowed (dungeon).
    /// </summary>
    public static int GetInstrumentPatch(TrackRole role, float sadness, float fear, float mystery) =>
        role switch
        {
            TrackRole.Drone =>
                fear >= 0.60f && sadness < 0.50f ? PatchTremoloStrings  // tense
                : sadness < 0.35f                ? PatchViolinStrings    // bright tavern bass
                : sadness < 0.70f                ? PatchChurchOrgan      // mid
                :                                  PatchChoirAahs,        // dark/haunting

            TrackRole.Melody =>
                sadness < 0.28f && fear < 0.45f  ? PatchFlute            // bright tavern melody
                : sadness < 0.28f                ? PatchOboe             // urgent/chase
                : sadness < 0.60f                ? PatchViolinStrings    // lament
                :                                  PatchOboe,             // mournful

            TrackRole.Counter =>
                sadness < 0.32f                  ? PatchHarpsichord      // lively tavern runs
                : mystery >= 0.65f               ? PatchPadBowed         // ethereal dungeon
                :                                  PatchFlute,            // general

            TrackRole.Texture =>
                fear >= 0.60f || mystery >= 0.68f ? PatchTremoloStrings  // creepy/tense shimmer
                : sadness < 0.40f                 ? PatchHarpsichord     // bright decoration
                :                                   PatchFlute,           // soft texture

            _ => 0,
        };

    /// <summary>Returns the MIDI channel (0-indexed) for a track role. Channel 9 is reserved for drums.</summary>
    public static int GetChannel(TrackRole role) =>
        role switch
        {
            TrackRole.Drone   => 0,
            TrackRole.Melody  => 1,
            TrackRole.Counter => 2,
            TrackRole.Texture => 3,
            _                 => 0,
        };

    /// <summary>Dedicated SFX channel (0-indexed). Uses ch 14 (displayed as ch 15).</summary>
    public const int SfxChannel = 14;
}
