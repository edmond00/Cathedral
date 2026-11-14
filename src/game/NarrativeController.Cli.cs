using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;

namespace Cathedral.Game;

// The script driver's seams (--cli, --record). They exist so an automated run can act on named
// things instead of cells: a symbol is found by what it stands for and clicked through the same
// routine the pointer uses.
public partial class NarrativeController
{
    /// <summary>What a --cli <c>state</c> reports of the narration.</summary>
    public (bool Busy, string Loading, bool DiceActive, bool DiceRolling, bool Result, int Noetic, int MaxNoetic,
            string? Error, int Observed, int Unobserved) CliSnapshot() => (
        _loading != null || (_previewSession.IsActive && !_previewSession.Snapshot().Complete),
        _loading ?? "",
        _dice.IsVisible,
        _dice.IsVisible && _dice.IsRolling,
        _result != null,
        Mind.Noetic,
        _active.MaxNoeticPoints,
        _error,
        Mind.ElementNodes.Count(),
        Unobserved(_pov.Where).Count());

    // ── Symbols by name ──────────────────────────────────────────────────────

    /// <summary>
    /// Every symbol of the frame last drawn, as a script names it, with whether a click would act.
    /// One line a handle, as it is typed: <c>click observe person in "Alehouse"</c>,
    /// <c>click think "the reeve"</c>, <c>click act 0 "I will greet him"</c>; an observed element is listed
    /// beside them, for what it offers.
    /// </summary>
    public IReadOnlyList<string> CliSymbols()
    {
        var lines = new List<string>();
        if (_overlay.Frame is not { } frame) return lines;
        string On(MapSymbol s) => IsEnabled(s) ? "" : " (disabled)";

        foreach (var card in frame.Cards)
        {
            foreach (var group in card.Tray.Where(t => t.Known == null).GroupBy(t => t.Kind))
                lines.Add($"click observe {ElementKinds.Word(group.Key)} in \"{card.Area.DisplayName}\" x{group.Count()}{On(group.First())}");
            foreach (var known in card.Tray.Where(t => t.Known != null))
                lines.Add($"click observe \"{NameOf(known.Known!)}\" in \"{card.Area.DisplayName}\"{On(known)}");
            foreach (var token in card.Tokens)
                lines.Add($"click member \"{token.DisplayName}\" in \"{card.Area.DisplayName}\"{(ReferenceEquals(token, _active) ? " (played)" : "")}");
        }
        foreach (var element in Mind.ElementNodes.Where(n => Locate(n) != null))
        {
            lines.Add($"element \"{NameOf(element)}\" in \"{Locate(element)!.Value.Where.DisplayName}\"");
            foreach (var part in ContentsOf(element))
                lines.Add($"click observe \"{NameOf(part)}\" in \"{NameOf(element)}\"{On(new ContentSymbol(element, part))}");
            foreach (var stub in StubsOf(element))
                lines.Add($"click {(stub == StubKind.Think ? "think" : "speak")} \"{NameOf(element)}\"{On(new StubSymbol(element, stub))}");
        }
        var acts = CliActs();
        for (int i = 0; i < acts.Count; i++)
            lines.Add($"click act {i} \"{acts[i].Action!.DisplayText}\" on \"{NameOf(acts[i].ElementNode)}\"{On(new NodeSymbol(acts[i]))}");
        if (Exploring) lines.Add($"click exit  ({ExitLabel})");
        return lines;
    }

    /// <summary>The acts waiting on the played member's trees, in the order they were proposed.</summary>
    private List<ThoughtNode> CliActs() => Mind.WaitingActs.Where(a => Locate(a.ElementNode) != null).ToList();

    /// <summary>
    /// Whether <paramref name="name"/> names something that answers to <paramref name="names"/>: the
    /// whole name first, then a whole word of one, then any part — the order <c>--observe-only</c>
    /// matches in, and for the same reason ("pig" is the pig, not the Pigsty Track).
    /// </summary>
    private static int MatchRank(IEnumerable<string> names, string name)
    {
        var all = names.ToList();
        if (all.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase))) return 0;
        if (all.Any(n => Regex.IsMatch(n, $@"\b{Regex.Escape(name)}\b", RegexOptions.IgnoreCase))) return 1;
        return all.Any(n => n.Contains(name, StringComparison.OrdinalIgnoreCase)) ? 2 : int.MaxValue;
    }

    private IEnumerable<string> NamesOf(NarrativeAnchor anchor)
        => ObservationController.TargetableNames(anchor).Append(NameOf(anchor));

    private IEnumerable<string> NamesOf(ThoughtNode element)
        => Locate(element) is { } at ? NamesOf(at.Anchor).Append(NameOf(element)) : new[] { NameOf(element) };

    private static T? Best<T>(IEnumerable<T> candidates, Func<T, int> rank) where T : class
        => candidates.Select(c => (c, r: rank(c))).Where(x => x.r != int.MaxValue).OrderBy(x => x.r).Select(x => x.c).FirstOrDefault();

    private Area? CliArea(string? name)
        => name == null ? null : Best(_scene.AllAreas, a => MatchRank(new[] { a.DisplayName }, name));

    /// <summary>
    /// The symbol a script names, or the reason there is none. <paramref name="what"/> is one of
    /// <c>observe</c>, <c>think</c>, <c>speak</c>, <c>act</c>, <c>member</c>; <paramref name="name"/> a
    /// kind (<c>observe person</c>), an element's name, an act's index or a word of its text, or a
    /// member's name; <paramref name="areaName"/> narrows an observation to an area, which is
    /// otherwise the one the played member stands in when it offers any.
    /// </summary>
    public (MapSymbol? Symbol, string? Error) CliFindSymbol(string what, string name, string? areaName = null,
                                                             bool playerOnly = false)
    {
        var frame = _overlay.Frame;
        if (frame == null) return (null, "the map has not been drawn yet");
        var area = CliArea(areaName);
        if (areaName != null && area == null) return (null, $"no area '{areaName}'");

        switch (what)
        {
            case "observe":
            {
                var trays = frame.Cards.Where(c => area == null || c.Area.Id == area.Id)
                                 .OrderBy(c => c.Area.Id == _pov.Where.Id ? 0 : 1).SelectMany(c => c.Tray).ToList();
                if (ElementKinds.Parse(name) is { } kind)
                    return trays.FirstOrDefault(t => t.Kind == kind && t.Known == null) is { } anonymous
                        ? (anonymous, null) : (null, $"nothing unobserved of kind '{name}'" + (area != null ? $" in {area.DisplayName}" : ""));
                if (Best(trays.Where(t => t.Known != null), t => MatchRank(NamesOf(t.Known!), name)) is { } known)
                    return (known, null);
                var contents = Mind.ElementNodes.SelectMany(n => ContentsOf(n).Select(p => new ContentSymbol(n, p)));
                if (Best(contents, c => MatchRank(NamesOf(c.Part), name)) is { } content) return (content, null);
                // Naming the holder asks for whatever it holds first: a memory's fragments are not
                // something a script on the real model can name ahead.
                if (Best(Mind.ElementNodes.Where(n => ContentsOf(n).Count > 0), n => MatchRank(NamesOf(n), name)) is { } holder)
                    return (new ContentSymbol(holder, ContentsOf(holder)[0]), null);

                // A script may name what a player cannot: something not yet known. The symbol is the
                // anonymous one of its kind, and the observation that follows is pinned to it — the
                // mind's choice made for it, as --observe-only makes it for a whole run.
                var unknown = trays.Where(t => t.Known == null).Select(t => t.Area).Distinct()
                    .SelectMany(a => Unobserved(a).Where(u => !u.Known).Select(u => (Area: a, u.Anchor, u.Kind)))
                    .Select(u => (u.Area, u.Anchor, u.Kind, Rank: MatchRank(NamesOf(u.Anchor), name)))
                    .Where(u => u.Rank != int.MaxValue).OrderBy(u => u.Rank).ToList();
                if (unknown.Count == 0) return (null, $"nothing unobserved named '{name}'");
                // A recording takes only what a player can do, and a player cannot name what they
                // have not learned: on camera the anonymous symbol is clicked by its kind.
                if (playerOnly) return (null, $"'{name}' is not known yet — observe by kind (way, person, hostile, thing)");
                _cliObservePin = unknown[0].Anchor;
                return (trays.First(t => t.Known == null && t.Kind == unknown[0].Kind && t.Area.Id == unknown[0].Area.Id), null);
            }
            case "think" or "speak":
            {
                // "last" is whatever this member's trees took in last: on the real model the mind
                // chooses what it looks at, and a script has no name to give.
                var element = name.Equals("last", StringComparison.OrdinalIgnoreCase)
                    ? _lastRooted.TryGetValue(_active, out var latest) && Locate(latest) != null ? latest : null
                    : Best(Mind.ElementNodes.Where(n => Locate(n) != null), n => MatchRank(NamesOf(n), name));
                if (element == null) return (null, $"no observed element named '{name}'");
                var stub = what == "think" ? StubKind.Think : StubKind.Speak;
                return StubsOf(element).Contains(stub) ? (new StubSymbol(element, stub), null)
                                                       : (null, $"'{NameOf(element)}' offers no '{what}'");
            }
            case "act":
            {
                var acts = CliActs();
                var act = int.TryParse(name, out int index)
                    ? (index >= 0 && index < acts.Count ? acts[index] : null)
                    : Best(acts, a => MatchRank(new[] { a.Action!.ActionText, a.Action.Verb.VerbId }, name));
                return act != null ? (new NodeSymbol(act), null) : (null, $"no waiting act '{name}' ({acts.Count} waiting)");
            }
            case "member":
            {
                var member = Best(_minds.Keys, m => MatchRank(new[] { m.DisplayName }, name));
                return member != null ? (new TokenSymbol(member), null) : (null, $"no member '{name}' in this place");
            }
        }
        return (null, $"unknown symbol kind '{what}'");
    }

    /// <summary>The unknown element a script named for the observation it is about to click (<see cref="CliFindSymbol"/>).</summary>
    private NarrativeAnchor? _cliObservePin;

    /// <summary>Clicks a symbol found by <see cref="CliFindSymbol"/>. Returns an error, or null.</summary>
    public string? CliClickSymbol(MapSymbol symbol)
    {
        if (Busy || IsPopupVisible) return "the map does not answer a click now";
        if (!IsEnabled(symbol)) return "that symbol is disabled";
        ClickSymbol(symbol);
        return null;
    }

    /// <summary>Clicks the way out: LEAVE, or RUNAWAY when somebody confronts the played member.</summary>
    public string? CliClickExit()
    {
        if (Busy || IsPopupVisible) return "the map does not answer a click now";
        if (!Exploring) return "there is no way out of this phase";
        OnExitClicked();
        return null;
    }

    /// <summary>What the way out would be now — LEAVE or RUNAWAY — or null where there is none.</summary>
    public string? CliExitLabel => Exploring ? ExitLabel : null;

    /// <summary>Walks the played member to an area by ways the party knows, as a click on its card does.</summary>
    public string? CliWalkTo(string areaName)
    {
        if (Busy || IsPopupVisible) return "the map does not answer a click now";
        if (CliArea(areaName) is not { } area) return $"no area '{areaName}'";
        if (area.Id == _pov.Where.Id) return null;
        if (ConfrontationOf(Mind) != null) return "confronted: only the way out leads away";
        if (!CanReach(area)) return $"no known way to '{area.DisplayName}'";
        WalkTo(area, then: () => { });
        return null;
    }

    /// <summary>
    /// The screen cell a symbol is drawn at, for a recorder that drives the real pointer: the map is
    /// panned to it first when it is off the view. Null when the frame does not hold it.
    /// </summary>
    public (int X, int Y)? CliSymbolCell(MapSymbol symbol)
    {
        if (_map.CellOf(tag => Equals(tag, symbol)) is not { } cell) return null;
        if (_map.ScreenCellOf(cell.X, cell.Y) == null) _map.CentreOnCell(cell.X, cell.Y);
        return _map.ScreenCellOf(cell.X, cell.Y);
    }

    /// <summary>The screen cell of the way out's glyph, panned into view; null where there is none.</summary>
    public (int X, int Y)? CliExitCell()
    {
        if (!Exploring || _map.ExitCellOf(_scene) is not { } cell) return null;
        if (_map.ScreenCellOf(cell.X, cell.Y) == null) _map.CentreOnCell(cell.X, cell.Y);
        return _map.ScreenCellOf(cell.X, cell.Y);
    }

    /// <summary>The screen cell of an area's card, panned into view; null when the map has no such card.</summary>
    public (int X, int Y)? CliAreaCell(string areaName)
    {
        if (CliArea(areaName) is not { } area) return null;
        _map.Reveal(area);
        return _map.ScreenCellOf(area);
    }

    // ── Boxes ────────────────────────────────────────────────────────────────

    /// <summary>The dice [ Continue ] button region, if the dice box is showing one.</summary>
    public (bool Present, int X, int Y, int Width) CliDiceContinue()
    {
        var r = _dice.ContinueButtonRegion;
        return (_dice.IsVisible && !_dice.IsRolling && r.Width > 0, r.X, r.Y, r.Width);
    }

    /// <summary>The narration's dice box, when a roll is on screen.</summary>
    public DiceRollComponent? CliDice => _dice.IsVisible ? _dice : null;

    /// <summary>The time box's state: none, choosing, passing (animating) or settled.</summary>
    public string CliTimeBoxState()
        => !_timeBox.IsVisible ? "none"
         : _timeBox.IsChoosing ? "choosing"
         : _timeBox.IsAnimating ? "passing" : "settled";

    /// <summary>The periods the time box offers while it asks, with their button regions.</summary>
    public IReadOnlyList<(TimePeriod Period, int X, int Y, int Width)> CliTimeBoxChoices() => _timeBox.PeriodRegions;

    /// <summary>The time box's [ Continue ] region, once the hours have passed.</summary>
    public (bool Present, int X, int Y, int Width) CliTimeBoxContinue()
    {
        var r = _timeBox.ContinueButtonRegion;
        return (_timeBox.IsSettled && r.Width > 0, r.X, r.Y, r.Width);
    }

    /// <summary>Whether somebody is walking on the map.</summary>
    public bool CliWalking => _walk != null;

    /// <summary>Whether the preview box is up, its title, its text, and whether it is complete.</summary>
    public (bool Active, string Title, string Text, bool Complete) CliPreview()
    {
        var s = _previewSession.Snapshot();
        return (s.Active, s.Title, s.DisplayText, s.Complete);
    }

    /// <summary>Whether the preview box is asking YES or NO (the doubt of a thinking) rather than offering CONTINUE.</summary>
    public bool CliPreviewAsking() => _previewSession.Snapshot().Asking;

    /// <summary>
    /// Whether a doubt waits on an answer anywhere in the preview queue — shown, or behind parts still
    /// to be continued past. The thinking is blocked on it, so the script driver counts it as idle.
    /// </summary>
    public bool CliPreviewAwaitsAnswer() => _previewSession.AwaitsAnswer;

    /// <summary>The preview box's [ YES ] or [ NO ] region, when the shown part asks.</summary>
    public (bool Present, int X, int Y, int Width) CliPreviewAnswer(bool yes)
    {
        var r = yes ? _previewAnswerRegions.Yes : _previewAnswerRegions.No;
        return (r.Width > 0, r.X, r.Y, r.Width);
    }

    /// <summary>The preview box's [ CONTINUE ] region, when generation is done.</summary>
    public (bool Present, int X, int Y, int Width) CliPreviewContinue() =>
        (_previewContinueRegion.Width > 0, _previewContinueRegion.X, _previewContinueRegion.Y, _previewContinueRegion.Width);

    /// <summary>The result box: its title and chips, and its [ CONTINUE ] region; null when no result is up.</summary>
    public (string Title, IReadOnlyList<string> Chips, int X, int Y, int Width)? CliResult()
        => _result == null ? null
         : (_result.Title, _result.Chips.Select(c => c.Text).ToList(),
            _resultContinueRegion.X, _resultContinueRegion.Y, _resultContinueRegion.Width);

    // ── Popups ───────────────────────────────────────────────────────────────

    /// <summary>Labels of the popup on screen, or null when none is up.</summary>
    public (string Kind, IReadOnlyList<string> Labels)? CliPopup()
    {
        if (_choicePopup.IsVisible)
            return ("choice", _choicePopup.Choices.Select((c, i) => _choicePopup.IsChoiceEnabled(i) ? c : $"{c} (disabled)").ToList());
        if (_modusMentisPopup.IsVisible)
            return ("modus-mentis", _modusMentisPopup.Choices.Select(m => m.DisplayName).ToList());
        if (_itemSelectionPopup.IsVisible)
            return ("item", _itemSelectionPopup.Choices.Select(i => i.DisplayName).ToList());
        return null;
    }

    /// <summary>The index of the modus mentis <paramref name="id"/> in the open modus mentis popup, or -1.</summary>
    public int CliModusMentisPopupIndex(string id)
        => _modusMentisPopup.IsVisible
            ? _modusMentisPopup.Choices.ToList().FindIndex(m => m.ModusMentisId.Equals(id, StringComparison.OrdinalIgnoreCase))
            : -1;

    /// <summary>
    /// Answers the popup on screen by index. Returns an error, or null. Deliberately not through the
    /// mouse path: a popup's hit-test reads the live OS cursor, which no injected coordinate moves.
    /// </summary>
    public string? CliChoosePopup(int index)
    {
        if (_choicePopup.IsVisible)
        {
            if (!_choicePopup.IsChoiceEnabled(index)) return $"choice {index} is disabled or out of range";
            _choicePopup.Hide();
            DispatchChoice(index);
            return null;
        }
        if (_modusMentisPopup.IsVisible)
        {
            var list = _modusMentisPopup.Choices;
            if (index < 0 || index >= list.Count) return $"modus-mentis {index} out of range";
            var chosen = list[index];
            _modusMentisPopup.Hide();
            ApplyModusMentisSelection(chosen);
            return null;
        }
        if (_itemSelectionPopup.IsVisible)
        {
            var list = _itemSelectionPopup.Choices;
            if (index < 0 || index >= list.Count) return $"item {index} out of range";
            var chosen = list[index];
            _itemSelectionPopup.Hide();
            var pending = _pending as ToolPending;
            _pending = null;
            if (pending != null) _ = CombineItemAsync(pending.Act, chosen);
            return null;
        }
        return "no popup is visible";
    }

    /// <summary>
    /// A screen pixel that picks choice <paramref name="index"/> of the popup on screen, found by asking
    /// that popup's own hit-test. Null when no popup is up or no pixel picks that choice.
    /// </summary>
    public OpenTK.Mathematics.Vector2? CliPopupChoicePixel(int index)
    {
        var bounds = _core.PopupTerminal?.GetScreenBounds(_core.ClientSize);
        if (bounds == null) return null;

        Func<float, float, OpenTK.Mathematics.Vector2i, float, int?>? hit =
              _choicePopup.IsVisible        ? _choicePopup.CliIndexAt
            : _itemSelectionPopup.IsVisible ? _itemSelectionPopup.CliIndexAt
            : _modusMentisPopup.IsVisible   ? _modusMentisPopup.CliIndexAt
            : null;
        if (hit == null) return null;

        float cell = CellPixels;
        var (left, top, right, bottom) = bounds.Value;
        float x = left + Math.Min(right - left, cell * 8f);
        for (float y = top; y <= bottom; y += cell * 0.25f)
            if (hit(x, y, _core.ClientSize, cell) == index)
            {
                // Aim at the middle of the row rather than its first hit pixel.
                float y2 = y;
                while (y2 + 1 <= bottom && hit(x, y2 + 1, _core.ClientSize, cell) == index) y2 += 1;
                return new OpenTK.Mathematics.Vector2(x, (y + y2) * 0.5f);
            }
        return null;
    }

    // ── Staging and reading ──────────────────────────────────────────────────

    /// <summary>
    /// The script driver's <c>hostile</c>: puts a person of this scene in <paramref name="areaName"/>
    /// for the rest of the visit (<see cref="Scene.Scene.DisplacedNpcs"/>, as a drawn-in witness is)
    /// and makes them count the protagonist an enemy. Returns an error, or null.
    /// </summary>
    public string? CliMakeHostile(string who, string areaName)
    {
        var area = _scene.AllAreas.FirstOrDefault(a => a.DisplayName.Equals(areaName, StringComparison.OrdinalIgnoreCase));
        if (area == null) return $"no area '{areaName}'";
        var sceneNpc = _scene.Npcs.FirstOrDefault(n => n.IsAlive && n.Entity is NpcEntity e
                           && (e.Archetype.ArchetypeId.Equals(who, StringComparison.OrdinalIgnoreCase)
                               || e.DisplayName.Equals(who, StringComparison.OrdinalIgnoreCase)));
        if (sceneNpc?.Entity is not NpcEntity npc) return $"no person '{who}' in this scene";
        _scene.DisplacedNpcs[sceneNpc.Id] = area;
        foreach (var member in _minds.Keys) npc.AffinityTable.SetEnemy(member.AffinityKey);
        ApplyTimePeriod(Period);
        return null;
    }

    /// <summary>
    /// The verb id of the last act put through execution, or null before any. Read by the script
    /// driver's <c>expect-verb</c>: the outcome banner is the same for every verb, so it is the only
    /// thing a verb test can assert that a wrong verb cannot satisfy.
    /// </summary>
    public string? CliLastExecutedVerbId() => _cliLastExecutedVerbId;
    private string? _cliLastExecutedVerbId;

    /// <summary>A stable name for what a will's first step acts on: a person by archetype id, anything else by name.</summary>
    private static string CliTargetId(Element? target) => target switch
    {
        SceneNpc { Entity: NpcEntity npc } => npc.Archetype.ArchetypeId,
        null => "-",
        _    => target.DisplayName.Replace(' ', '_'),
    };

    /// <summary>
    /// A flat, greppable dump of the game state an <see cref="Outcome"/> can change. Read by the
    /// script driver's <c>inspect</c>: a chip proves the player was told something happened, and this
    /// proves it did.
    ///
    /// <para>Everything is named by <b>stable id</b> — item ids, modus mentis ids, archetype ids —
    /// never by display name, because names come from content and a test that spells one breaks the
    /// day somebody rewrites it. One fact per line, prefixed by subject.</para>
    /// </summary>
    public IReadOnlyList<string> CliInspect(string subject)
    {
        var outp = new List<string>();
        bool All(string s) => subject is "all" or "" || subject == s;
        var actor = _active;

        if (All("items"))
            foreach (var i in actor.GetAllItems())
                outp.Add($"item {i.ItemId}");

        if (All("coins"))
            outp.Add($"coins gold={_protagonist.Party.Gold} silver={_protagonist.Party.Silver} "
                   + $"copper={_protagonist.Party.Copper}");

        // The phase's budget, and the acting body's implement band. Both appear in the `state` line
        // too, but `state` only emits — `expect` scans the rendered terminal and never sees it, so
        // this is the seam that makes them assertable, exactly as `inspect menu` is for a greyed
        // button. The budget is what proves a refused combination was CHARGED for: the refusal
        // narration reads much like any other failure, so nothing on screen distinguishes a
        // combination that cost a point from one that did not.
        if (All("noetic"))
            outp.Add($"noetic points={Mind.Noetic}/{actor.MaxNoeticPoints} "
                   + $"tool_proficiency={ToolUsageProficiencyStat.Of(actor)}");

        // The open choice popup, with each option's state. `regions` prints the same thing, but only
        // emits — and a greyed option differs from a live one by colour alone, so this is what lets a
        // script assert that a tool refusal leaves "Use Tool" as the only way forward.
        if (All("popup") && _choicePopup.IsVisible)
            outp.Add("popup options=[" + string.Join(",", _choicePopup.Choices.Select((c, i) =>
                         _choicePopup.IsChoiceEnabled(i) ? c : $"{c}(disabled)")) + "]");

        // trespass= is what the header's TRESPASSING tag shows, and the condition under which every act
        // is a crime — assertable, where the tag is only a colour on screen. confronted= is the kind of
        // confrontation the actor is in (ConfrontationSelector.Current), the one the exit button reads.
        // The time box: whether it asks, is passing, or waits on Continue, and between which hours.
        if (All("time"))
            outp.Add($"time box={CliTimeBoxState()} destination={(_timeBox.IsVisible ? _timeBox.Destination.ToString() : "-")}");

        // Every will the actor holds and its path from here (ActionPathPlanner): how many steps, the
        // first to take and the goal, which of its targets it reaches, or none when no target can be
        // reached in this place. What a will
        // observes and proposes is derived from exactly this, so it is the seam a script asserts on.
        if (All("will"))
            foreach (var mm in actor.LearnedModiMentis.Where(m => m.Will != null).OrderBy(m => m.ModusMentisId))
            {
                // What the will steers by here: the pursued target alone on a visit a will's search was
                // walked for (WillPriority.TargetsOf), else every target.
                var pursued = Cathedral.Game.Narrative.Rules.Choice.WillPriority.PursuedOf(mm, _scene, _pov, actor);
                var path = ActionPathPlanner.Plan(_scene, _pov, actor, pursued ?? mm.Will!);
                outp.Add(path == null
                    ? $"will {mm.ModusMentisId} steps=none"
                    : $"will {mm.ModusMentisId} steps={path.Steps.Count} first=\"{path.First.Verbatim}\" goal=\"{path.Goal.Verbatim}\""
                      + $" target={CliTargetId(path.First.Action.Target)}"
                      + (path.ToolStep >= 0 ? $" tool_step={path.ToolStep + 1}" : "")
                      // Which of the will's targets the path reaches, by declaration order (1 = first), and its purpose.
                      + (path.Target is { } aim ? $" aim={AimOf(mm.Will!, aim)}/{mm.Will!.Count} purpose=\"{aim.Purpose}\"" : "")
                      + (pursued != null ? " pursuit=yes" : ""));
            }

        // A will's targets are built afresh on every read, so the one a path reaches is found by what it is.
        static int AimOf(WillTargets will, WillTarget aim)
            => will.ToList().FindIndex(t => t.Purpose == aim.Purpose && t.Describe() == aim.Describe()) + 1;

        if (All("where"))
        {
            var confrontation = ConfrontationOf(Mind);
            outp.Add($"where area=\"{_pov.Where.DisplayName}\" period={_pov.When} "
                   + $"day={(int)Cathedral.Game.Narrative.GameClock.Days} trespass={(IsTrespassing ? "yes" : "no")} "
                   + $"confronted={(confrontation == null ? "none" : $"{confrontation.Kind}:{confrontation.Npc.Archetype.ArchetypeId}")}"
                   + (Exploring ? $" exit={ExitLabel}" : ""));
        }

        // What the location map knows: every area stood in, every way gone by (AreaVisits). Read from
        // the mental map of the place, so after a revisit this is what the last visit left behind —
        // or nothing, once the place is forgotten.
        if (All("map"))
        {
            foreach (var area in _scene.AllAreas.Where(a => _scene.VisitedAreas.Contains(a.StableKey)))
                outp.Add($"map visited {area.DisplayName}");
            foreach (var (a, b, key, connector) in AreaVisits.AllLinks(_scene).Where(l => _scene.TakenLinks.Contains(l.Key)))
                outp.Add(connector == null
                    ? $"map taken between {string.Join(" and ", new[] { a.DisplayName, b.DisplayName }.OrderBy(n => n, StringComparer.Ordinal))}"
                    : $"map taken {connector.DisplayName}");
            foreach (var area in _scene.AllAreas.Where(a => _scene.SeenAreas.Contains(a.StableKey)
                                                         && !_scene.VisitedAreas.Contains(a.StableKey)))
                outp.Add($"map seen {area.DisplayName}");
            if (_lastWalk != null) outp.Add($"map {_lastWalk}");
        }

        // Who is in the party, and for those in the place where each stands, what they have left, and
        // which of them is played.
        if (All("party"))
        {
            foreach (var c in _protagonist.CompanionParty)
                outp.Add($"companion \"{c.DisplayName}\" species=\"{c.PartyDescription}\"");
            foreach (var mind in _minds.Values)
                outp.Add($"member \"{mind.Member.DisplayName}\" area=\"{mind.Pov.Where.DisplayName}\" "
                       + $"noetic={mind.Noetic}/{mind.Member.MaxNoeticPoints} played={(ReferenceEquals(mind.Member, _active) ? "yes" : "no")}");
        }

        // What the map offers a click now, as `regions` lists it: the tray symbols of each card, the
        // stubs and the waiting acts. Asked for by name only, for an assertion on what is NOT offered.
        if (subject == "symbols")
            foreach (var line in CliSymbols())
                outp.Add($"symbol {line.Trim()}");

        // What every node of the played member's trees says: on screen a text is read once, in the
        // preview box, and after that only by hovering its symbol. Asked for by name only.
        if (subject == "texts")
            foreach (var node in Mind.AllNodes)
                if ((node.Block?.Text ?? node.Note) is { Length: > 0 } said)
                    outp.Add($"text {node.Kind.ToString().ToLowerInvariant()} mind={node.Mind?.ModusMentisId ?? "-"} \"{said}\"");

        // The played member's thought trees: every observed element with where it now is, and under
        // each every node in order. What the screen draws as symbols, as lines a script can assert on.
        if (All("trees"))
        {
            foreach (var root in Mind.Roots) Describe(root, 0);
            foreach (var (area, refusal) in Mind.Strays)
                outp.Add($"tree refusal area=\"{area.DisplayName}\" mind={refusal.Mind?.ModusMentisId ?? "-"}");

            void Describe(ThoughtNode node, int depth)
            {
                string pad = new string(' ', depth * 2);
                switch (node.Kind)
                {
                    case ThoughtNodeKind.Element:
                        var at = Locate(node);
                        outp.Add($"tree {pad}element \"{NameOf(node)}\" kind={ElementKinds.Word(KindOf(node))} "
                               + $"area=\"{at?.Where.DisplayName ?? "-"}\" observer={node.Mind?.ModusMentisId ?? "none"}");
                        break;
                    case ThoughtNodeKind.Act:
                        outp.Add($"tree {pad}act state={node.State.ToString().ToLowerInvariant()} verb={node.Action!.Verb.VerbId} "
                               + $"means={node.Action.ActionModusMentisId} difficulty={node.Action.DifficultyLevel} text=\"{node.Action.DisplayText}\"");
                        break;
                    default:
                        outp.Add($"tree {pad}{node.Kind.ToString().ToLowerInvariant()} mind={node.Mind?.ModusMentisId ?? "-"}");
                        break;
                }
                foreach (var child in node.Children) Describe(child, depth + 1);
            }
        }

        // What is remembered of the place's things and people (ElementMemory), by key.
        if (All("known"))
            foreach (var key in _scene.KnownElements.OrderBy(k => k, StringComparer.Ordinal))
                outp.Add($"known {key}");

        // Which conversation was last queued, by tree id — the confrontation's id carries the crime it
        // accuses you of (caught_red_handed_murderer), which nothing on screen states plainly.
        if (All("dialogue"))
            outp.Add($"dialogue last={_lastDialogueTreeId}");

        // Learned routines — see Routine.CliLine. Reported here rather than read off the routines panel
        // because recording happens with no UI open at all: the panel is where a player reads them,
        // not where they are made.
        if (All("routines"))
            foreach (var r in _protagonist.RecordedRoutines)
                outp.Add(r.CliLine());

        // Every workshop holding a crafting station: its master, and whether the leave is given.
        if (All("workshops"))
            foreach (var section in _scene.Sections)
            {
                var stations = section.Areas.SelectMany(ar => ar.PointsOfInterest)
                                            .Where(Cathedral.Game.Scene.CraftingStations.IsStation).ToList();
                if (stations.Count == 0) continue;
                var master = Cathedral.Game.Narrative.Crafting.CraftingAccess.MasterOf(_scene, section);
                outp.Add($"workshop \"{section.DisplayName}\" stations={string.Join(",", stations.Select(s => s.DisplayName.Replace(' ', '_')))} "
                       + $"master={master?.Archetype.ArchetypeId ?? "none"} "
                       + $"authorized={(Cathedral.Game.Narrative.Crafting.CraftingAccess.IsAuthorized(_scene, section) ? "yes" : "no")} "
                       // Which of them the acting member may lawfully USE from where it stands, and which it is
                       // offered only as a crime: the gates themselves, read directly, so a script need not get a
                       // thinking mind to choose it.
                       + $"usable={string.Join(",", stations.Where(st => st is Cathedral.Game.Scene.PointOfInterest p
                                       && Cathedral.Game.Scene.Verbs.VerbRegistry.Instance.Get("use") is { } use && use.IsPossible(_scene, _pov, p, actor)
                                       && !use.IsIllegal(_scene, _pov, p, actor))
                                     .Select(st => st.DisplayName.Replace(' ', '_')).DefaultIfEmpty("none"))} "
                       + $"crime={string.Join(",", stations.Where(st => st is Cathedral.Game.Scene.PointOfInterest p
                                       && Cathedral.Game.Scene.Verbs.VerbRegistry.Instance.Get("use") is { } use && use.IsPossible(_scene, _pov, p, actor)
                                       && use.IsIllegal(_scene, _pov, p, actor))
                                     .Select(st => st.DisplayName.Replace(' ', '_')).DefaultIfEmpty("none"))}");
            }

        if (All("wounds"))
            foreach (var w in actor.Wounds)
                outp.Add($"wound {w.WoundName} target={w.TargetId}");

        // The four humor queues by composition, newest first. This is the only assertable proof that
        // an emotion reached the spleen: the chip says "Voluptas × 4" and `expect` can scan for that,
        // but a chip proves what the player was TOLD — the queue is what actually moved. Reported as
        // counts per humor name rather than slot by slot, because a queue is 49 slots deep and a
        // script cares that N instances arrived, not where they landed.
        //
        // All four queues, not just the spleen, because the same line is what makes eating and the
        // travel drain assertable later, and a subject that answers only the question of the day
        // would need widening the first time anything else wanted it.
        if (All("humors"))
            foreach (var q in actor.HumorQueues.All)
            {
                var counts = q.Items.GroupBy(h => h.Name)
                                    .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                                    .Select(g => $"{g.Key}={g.Count()}");
                outp.Add($"humors {q.OrganId} {string.Join(" ", counts)}");
            }

        // xp as well as level: practice is the commonest thing a verb does and the slowest to show,
        // since a level takes several successes. Without the raw count a script cannot tell a modus
        // mentis that was paid for its work from one that was not — which is how the Get-Up phase
        // came to award none at all without a single test noticing.
        // xp as well as level, and `effective` as well as `level`: a wound caps what a modus mentis
        // may reach without touching the level it stored, so the stored number alone cannot say
        // whether the thing can still be used. `broken=` is the flag every test in this area asserts
        // — it is the whole of the rule, and deriving it in a script from two numbers would be a
        // paraphrase of BrokenModusMentis rather than a reading of it.
        if (All("skills"))
            foreach (var m in actor.ModiMentis)
                outp.Add($"skill {m.ModusMentisId} level={m.Level} xp={m.CurrentXp}"
                       + $" effective={actor.GetEffectiveModusMentisLevel(m)}"
                       + $" max={actor.GetMaxLevelForModusMentis(m)}"
                       + $" broken={actor.IsModusMentisBroken(m)}");
        // ALWAYS REJECT's ignore list: refused for good, never learned again.
        if (All("skills"))
            foreach (var id in actor.RejectedModiMentisIds.OrderBy(i => i, System.StringComparer.Ordinal))
                outp.Add($"skill-rejected {id}");

        if (All("npcs"))
            foreach (var npc in _scene.Npcs)
            {
                string key   = _protagonist.AffinityKey;
                string where = _scene.GetAreaOf(npc, _pov.When)?.DisplayName ?? "-";
                var ent = npc.Entity as Cathedral.Game.Npc.NpcEntity;
                outp.Add($"npc {npc.Entity.Archetype.ArchetypeId} alive={npc.Entity.IsAlive}"
                       + (ent != null ? $" affinity={ent.AffinityTable.GetLevel(key)}"
                                      + $" enemy={ent.AffinityTable.IsEnemy(key)}"
                                      // What a blow does to them. Hit points are derived from the
                                      // wound count, so the pair is one fact read two ways — but a
                                      // test asserting the first blow landed needs the count, and
                                      // one asserting it was withheld from a dying man needs the
                                      // hit points. Nothing else surfaces either.
                                      + $" hp={ent.Combatant.CurrentHp}/{ent.Combatant.MaxHp}"
                                      + $" wounds={ent.Combatant.Wounds.Count}"
                                      // What a conversation's outcomes write. None of it is visible
                                      // any other way, and it is the whole state change of half the
                                      // catalogue: join_party, alms, the trade and job menus, the
                                      // goad into a fight, the introduction.
                                      + $" roused={npc.Roused}"
                                      + $" join={ent.JoinRequested}"
                                      + $" alms={ent.AlmsGiven}"
                                      + $" trade={ent.TradeRequest}"
                                      + $" job={(ent.JobRequest != null)}"
                                      + $" goaded={ent.FightRequestedByDialogue}"
                                      + $" introduced={(ent.IntroductionGranted != null)}" : "")
                       + $" area=\"{where}\"");
            }

        if (All("pois"))
            foreach (var poi in _pov.Where.PointsOfInterest)
                outp.Add($"poi \"{poi.DisplayName}\" lemma={poi.ReferenceLemma} items={poi.Items.Count}"
                       + (poi is Cathedral.Game.Scene.Building.DoorPointOfInterest d ? $" door={d.DoorState}" : ""));

        return outp;
    }
}
