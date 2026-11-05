"""Writes the corpus playthroughs (tools/prompts/scripts/corpus_*.cli) and the flags each runs with.

A playthrough boots, enters the location underfoot and then plays a list of phases. Every phase is a
block of ordinary CLI commands copied from known-good scripts (cli/outcome/emotion/success.cli,
cli/system/companion_handoff.cli, cli/system/dialogue_success.cli). With the real model nothing is
pinned — the judge picks the goal and the means — so the blocks are tolerant: a command that does
not apply in the state the model left (no dialogue opened, no action offered) reports an error and
the script moves on. The point is a corpus of real requests, not an assertion.

    python tools/prompts/make_scripts.py      # writes the scripts and runs.tsv
"""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
W = 1500  # seconds a single wait may take on CPU

BOOT = f"""wait
wait mode MainMenu 900
click menu New
wait mode ProtagonistCreation 60
click continue
wait mode WorldView 300
travel here
wait mode LocationInteraction 900
advance 12 {W}
"""


def think(mm, keyword="0"):
    """Think about something with `mm`, act on what it proposes, read the outcome and any emotion,
    and play out a conversation if the act opened one."""
    return f"""click keyword {keyword}
choose 0
choose {mm}
advance 12 {W}
click action 0
choose 0
advance 12 {W}
click continue
advance 12 {W}
advance 12 {W}
{dialogue()}"""


def dialogue():
    return f"""wait 30
advance 20 {W}
click option 0
advance 20 {W}
click option 1
advance 20 {W}
click option 0
advance 20 {W}
click continue
advance 20 {W}
"""


def observe(mm, keyword="1"):
    return f"""click keyword {keyword}
choose 1
choose {mm}
advance 12 {W}
"""


def speak_about(keyword="0"):
    return f"""click keyword {keyword}
choose 2
choose 0
choose 0
advance 12 {W}
"""


def next_phase():
    """Closes the segment. When the thinking proposed nothing the footer is LEAVE and the party walks
    out — then it walks straight back in (`travel here`, a fresh arrival), and in narration that
    command is a harmless error."""
    return f"""click button
wait {W}
travel here
wait mode LocationInteraction {W}
advance 12 {W}
"""


RUNS = [
    # conversations: opened in the room where a tradesman stands, attention narrowed to them
    ("talk_blacksmith", "--seed 31 --start-at village --start-area Forge --npc-static --observe-only blacksmith --period morning --grant-mm rhetoric,pride,flattery,cruelty",
     [think("rhetoric"), next_phase(), think("pride")]),
    ("talk_miller", "--seed 32 --start-at village --start-area Mill --npc-static --observe-only miller --period evening --grant-mm courtesy,mythomania,plain_dealing,insolence",
     [think("courtesy"), next_phase(), think("mythomania")]),
    ("talk_farm", "--seed 33 --start-at farm --npc-static --period noon --grant-mm homily,disputation,humility,gossip",
     [think("homily"), next_phase(), think("disputation")]),
    # name, flags, phases
    ("village_dawn", "--seed 21 --start-at village --period dawn --grant-mm poetry,rhetoric,cutpurse,avarice,keen_ear",
     [think("poetry"), next_phase(), observe("keen_ear"), think("rhetoric"), next_phase(), think("avarice")]),
    ("farm_night", "--seed 22 --start-at farm --period night --grant-mm arithmetic_logic,courtesy,butchery,dread,husbandry",
     [think("arithmetic_logic"), next_phase(), think("dread"), next_phase(), observe("husbandry"), think("courtesy")]),
    ("forest_evening", "--seed 23 --start-at forest --period evening --grant-mm woodcraft,caution,temerity,hunt,mycology,superstition",
     [think("caution"), next_phase(), observe("woodcraft"), think("superstition"), next_phase(), think("temerity")]),
    ("village_noon_party", "--seed 24 --start-at village --period noon --fill-party --grant-mm mercy,flattery,prosaic_grammar,physiognomy,brute_force",
     [think("prosaic_grammar"), next_phase(), think("mercy"), next_phase(), observe("physiognomy"), speak_about()]),
    ("farm_dusk", "--seed 25 --start-at farm --period dusk --grant-mm misanthropy,pride,plain_dealing,introspection,stoneface",
     [think("pride"), next_phase(), observe("introspection"), think("misanthropy"), next_phase(), think("pride")]),
    ("cave_morning", "--seed 26 --start-at cave --period morning --grant-mm tactics,sense_of_humor,ruination,nightwalking,clambering",
     [think("tactics"), next_phase(), think("sense_of_humor"), next_phase(), observe("nightwalking"), think("ruination")]),
    ("village_evening", "--seed 27 --start-at village --period evening --grant-mm mythomania,philosophy,streetwise,petty_thief,sloth,zeal",
     [think("mythomania"), next_phase(), observe("streetwise"), think("philosophy"), next_phase(), think("zeal")]),
    ("plain_afternoon_party", "--seed 28 --start-at plain --period afternoon --fill-party --grant-mm sangfroid,impatience,courtesy,architecture,dance",
     [think("impatience"), next_phase(), think("courtesy"), next_phase(), observe("architecture"), speak_about()]),
]


def main():
    os.makedirs(os.path.join(HERE, "scripts"), exist_ok=True)
    with open(os.path.join(HERE, "scripts", "runs.tsv"), "w") as idx:
        for name, flags, phases in RUNS:
            path = os.path.join(HERE, "scripts", f"corpus_{name}.cli")
            with open(path, "w") as f:
                f.write(f"# corpus playthrough {name}. FLAGS: --skip-childhood --debug {flags}\n")
                f.write(BOOT + "".join(phases) + "state\nquit\n")
            idx.write(f"{name}\t--skip-childhood --debug {flags}\n")
    print("wrote", len(RUNS), "scripts")


if __name__ == "__main__":
    main()
