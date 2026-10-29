# The tutorial video: direction

Decided with the developer after the scripted run was done (BRIEF.md covers the run; this covers the
video made from it). Where this and the `video` skill's house rules differ, this wins.

- **For** new players first (how to play the first hour), polished enough to share as a showcase.
- **Length**: no limit. Each system is explained properly rather than named in passing.
- **Voice**: a plain, clear instructor, not the skill's old-man narrator. Piper `en_GB-alan-medium`
  (first `en_US-ryan-high`; the developer preferred alan).
- **Depth**: the rules explained properly, like the manual read aloud: how a die pool is built, what
  each organ group feeds, how humors are spent, wages, routines. Every claim from the code at HEAD.
- **Staging**: not mentioned. The run is shown as a game (no word about the debug flags behind it).
- **The language model** is explained as a system, by what makes it particular, and only once it has
  written something on screen (the first childhood memory), not in the opening: it runs fully
  locally, on the CPU (no claim about GPUs: not verified on the shipped build); it is integrated with coded gameplay (it decides what is observed and
  which goal a way of thinking proposes, while rules, dice and outcomes are code); each modus mentis
  is a persona the model roleplays, reasoning step by step in its voice (a chain of thought made of
  minds).
- **Every passage the game wrote is read aloud** by the narrator, in a delivery clearly different
  from the explanations, so a listener always knows whether a mechanism is being explained or the
  game's text is being read. The texts come from the take's LLM session logs
  (`logs/llm_session_*/slot_*/request_*/llm_response.txt`, timestamped), aligned to the timeline.
  Same voice (alan) for both, chosen by audition ("slower and rougher" was too close to the
  explanation, "pitched down with a stone-room echo" too far). The chosen reading, R4, as storyboard
  settings:
  `"voice": {"name": "en_GB-alan-medium", "speed": 1.0, "grit": 0.15}` for the explanation, and
  `"voice_styles": {"reading": {"speed": 0.88, "grit": 0, "noise_scale": 0.9, "noise_w": 1.1,
  "pitch": 0.97, "room": 0.16}}` for the game's text: a looser Piper delivery, slightly lower, in a
  small room. A segment's `vo` is then a list of parts, `{"text": …, "style": "reading"}` for a read
  passage (`vidkit.build.voice_segment`, `vidkit.tts.speak`).
- **Conversations**: every reply offered is read, each named by the mind that wrote it.
- **No captions.**
- **Diagrams (manim)**: the organs → systems map; one narration turn (observe → keyword → think
  with a modus mentis → goal and means → act → dice → outcome, and where noetic points go); dice and
  humors (the pool, a success, what each humor does to a die, the viscera limit, attack against
  defence in a fight). Work, trade and routines are shown in footage only.
- **Footage**: the final take, `build/rec_tutorial` (1440×1080 @ 30, with `music.wav`: the game's
  own music and sound effects; the take's real audio, not `music.mid` through the stand-in synth).
