"""The narration of the animated segments, keyed by scene and step. Every rule is the manual's
(docs/manual/06-narration.md and 02-memory.md on narration-map) or the design note's
(design/NARRATION_MAP.md). The segments over gameplay are written in build_storyboard.py, against
the takes' timelines."""

MANIM = {
    ("Principles", None): [
        "The old screen had its charm. Arriving at a place, a mind wrote a paragraph about it, some words "
        "in it lit up, and you clicked a word to look closer. It read a little like a book. But a book is "
        "perhaps a poor place to stand in. You could not quite see where things were, nor tell what a word "
        "would lead to, and what you read scrolled away, often just when it was needed again.",
        "Now the place is the page. Every area is a card, every way a line between them, and under each "
        "card stands a row of symbols: what there is to look at. Think of it as a mind map of the place, "
        "and the visit grows on it. You click one symbol, one thing, and the mind you choose looks at it. "
        "The symbol drops under its card and becomes a root, with the text the mind wrote. Think about "
        "it, and a branch grows: what the mind wants, and the act it proposes, waiting, with the sixes it "
        "needs. Carry it out, and the branch ends in what it came to, and what it stirred. The old page "
        "scrolled away as you read it. Here nothing does: every text stays where it grew, and as the "
        "visit goes on, trees fill the map, the whole visit readable at a glance. And where you have "
        "been, you walk again for free. A way not yet taken is an act like any other.",
    ],
    ("Legend", "kinds"): [
        "Every card on the map is an area, and under its name stands a row of symbols, one per thing "
        "in it, in four kinds. An arch is a way: a neighbouring area, a door, a stair, a path or a "
        "climb. A face is a person, or any creature that does not count you an enemy. A skull is one "
        "that does. A diamond is any other thing: a cart, a bush, a chest, a corpse. And a hollow "
        "diamond is something a container was seen to hold.",
    ],
    ("Legend", "states"): [
        "Everything in a place stands in one of three states, for each member of the party. Unknown: "
        "all you have is its kind and where it stands. You click the kind, and the mind you choose "
        "picks which one to look at. Known: remembered from an earlier visit, or seen inside a "
        "container. It has a name, and a click looks at that one. Observed: looked at on this visit. "
        "Only what is observed can be thought about, and it becomes the root of a tree.",
    ],
    ("Legend", "tree"): [
        "A tree is everything the visit wrote about one thing, where it grew. The root, and under it "
        "the text the observing mind wrote. A question mark, to think about it, while it still "
        "affords something. The thought, and the act it produced, waiting, with the number of sixes "
        "it needs. Click it whenever you like. Then the outcome: a tick, or a cross, and what the act "
        "stirred. A refusal, from any mind, for any reason, is a crossed circle. And a quotation mark "
        "is a thing spoken of to a companion. Hover any node to read its text again.",
    ],
    ("Chain", "steps"): [
        "Under the map, the rules of an act did not change. An act still takes three minds. An "
        "observing mind looks: you must stand in the area, and it costs a noetic point. A thinking "
        "mind wants something: it can think from anywhere in the place, and it costs a point. An "
        "acting mind carries it out: the body walks there first, and attempting it costs nothing more.",
        "Your noetic points are the encephalon's measure. A success refills the whole pool, there and "
        "then. A failure refills nothing. And with no points left you can still walk where you know "
        "the way, carry out the acts already waiting, and leave.",
    ],
    ("Chain", "dice"): [
        "The dice are the sum of the levels of every mind in the chain, the one that looked, the one "
        "that thought and the one that acts, and the level of a tool, if you combine one. The "
        "difficulty is the number of sixes needed. An eager mind needs one six fewer, a reluctant one, "
        "one more. An act harder than its pool cannot succeed, and an act of difficulty nought is not "
        "rolled at all.",
    ],
    ("Functions", None): [
        "So which mind is asked what? Every mind bears one to three functions, out of eight. It "
        "observes, it thinks, it acts, it speaks, it fights, it feels, it wants, or it knows a trade. "
        "Thinking and action are never in the same mind, and neither are action and emotion: to "
        "want a thing, to do it and to be moved by it are three minds, not one.",
        "The will is the one to understand. A will does not weigh options. It wants a few kinds of "
        "act, and it observes and thinks only toward them.",
    ],
    ("Properties", None): [
        "Besides its functions, a mind declares a handful of properties, and each of them changes how "
        "it behaves on the map. Here are three real ones: Beast Sense, which looks and thinks; Petty "
        "Thief, a will to steal that acts; and Diligence, a will to work that speaks and acts.",
        "Its sources, one region or two organs, set the ceiling its level can reach, and a wound "
        "lowers it. Its memory type says where it can be filed. Its moral level, low, medium or "
        "high, only matters when it thinks or acts. Its readiness to doubt says how many second "
        "thoughts it gets. And a discreet mind may act with someone in the room, and is not heard "
        "through a wall.",
    ],
    ("Rules", None): [
        "Two families of rule stand between you and an act. Rules of action judge an act already "
        "chosen: they reject it, in the acting mind's own voice, and it costs a point. That man is "
        "watching. That door needs a key. My arm is ruined. Rules of choice narrow what is offered "
        "in the first place: they withhold silently, and cost nothing. A principled mind is never "
        "offered a crime. A speaking mind turns to people. It simply never occurred to it.",
    ],
    ("Will", "course"): [
        "Take Petty Thief. It wants one thing: to steal. The game reckons its course, the shortest "
        "chain of acts from where you stand to a theft: open the shop door, walk to the shelf, steal. "
        "Asked to look, it looks only at what begins that course, and asked about anything else it "
        "declines, naming the first step. That refusal is a rule of its nature, and costs nothing. "
        "And when it does the act it wants, it is eager: one six fewer, and it never refuses.",
    ],
    ("Will", "candidates"): [
        "Thinking, a will is less strict. Any act that begins a course of at most three acts is a "
        "candidate. Of those, it proposes the shortest, and a second thought takes the next. A course "
        "longer than that is not considered, unless it is the shortest the place offers.",
    ],
    ("Will", "leave"): [
        "And when no course exists in the place at all, the will declines everything, and says it "
        "would have to leave to find what it wants. That is not a dead end: it is a hint. From the "
        "world map, a will can be sent to search: the places in range are tried nearest first, and "
        "the first where its course exists becomes the journey's end. The visit is then made for "
        "that want, however long the course.",
    ],
    ("Morality", None): [
        "Morality is read twice, because the goal and the means are two decisions. A high thinking "
        "mind is never offered a crime. A high acting mind refuses a crime unasked, before it is "
        "even offered. A low thinking mind, when a crime is on offer, is offered only crimes, and "
        "must choose one. And a low acting mind cannot refuse a crime at all. Which is why a "
        "scrupulous hand can still be chosen to carry out what a low mind decided.",
    ],
    ("Proximity", "raw"): [
        "Being seen is two separate questions. First: may I act at all? With someone in the room, an "
        "ordinary mind is refused, and that refusal is a rule of action, so it costs a point. A "
        "discreet mind may try. That permission is the whole of what discretion buys at close "
        "quarters, and a discreet mind may not back out of an act that only its discretion allows.",
    ],
    ("Proximity", "effective"): [
        "Second: what does failing cost? Three rungs. In the same room, a witness catches you in the "
        "act, and an enemy attacks. In the same section, through a wall, they come to look: they walk "
        "into your room, and now someone stands there. Further off, nothing. Discretion silences you "
        "through a wall, never in the same room.",
    ],
    ("Confronted", "closed"): [
        "An enemy in the room, or a witness with a reason to stop you, confronts you, and the map "
        "closes around them. You may answer them, explain yourself, which makes every check of that "
        "conversation one six easier, or carry out an act already waiting, if a discreet mind will. "
        "Thinking about them, a mind is offered only those answers, unless a discreet mind could act. "
        "You may not look at anything else, think about anything else, walk away, or speak to a "
        "companion. And somebody asleep confronts nobody.",
    ],
    ("Confronted", "exit"): [
        "The way out is one symbol. When nobody stops you, it reads Leave: every member walks out "
        "by known ways, for free, and a walk that meets someone who would confront you stops there. "
        "Confronted, it reads Runaway: the feet's dice, needing a single six, which the humors can "
        "bend. It is checked again at every area on the way, and failing it starts the fight, or the "
        "conversation.",
    ],
    ("Wages", None): [
        "A failure costs three things. The pool is not refilled. At most one social consequence: "
        "caught, attacked, or someone comes to look. And one penalty, drawn from what that act "
        "risks. A wound, for an attempt that can hurt you. The way lost, for finding a way: the visit "
        "ends and you stand outside. The hour wasted, for the hard and the slow. Or the thread lost, "
        "one more noetic point, for the senses, speech and waiting.",
    ],
    ("Party", None): [
        "The party changed the most. Each member is now its own body in the place. It stands where "
        "it was last taken, spends its own points, uses its own minds, and keeps its own trees. Click "
        "a token to switch member, for free. A member with a speaking mind can speak of something it "
        "observed to a companion nearby: that costs the speaker a point, and plants it on the "
        "listener's map, already observed. A beast is offered no conversation, no lock, no tool and "
        "no letters, and a fight takes only those standing where it breaks out.",
    ],
}


# The reasons behind each rule, said over each scene once its picture is complete: the scene's beats follow
# the MANIM parts (the storyboard's "core"), and these follow them. Each is drawn from the design note
# (design/NARRATION_MAP.md) or the rule's own comments in the code; said softly, never in the first person.
WHY = {
    ("Principles", None):
        "That is more or less the whole idea: a screen that looks like what happens underneath, minds at "
        "work on a place, leaving their marks where they worked. By the end of a visit, the map should "
        "read almost like an account of what happened there.",
    ("Legend", "kinds"):
        "Only four kinds, and that is deliberate. A symbol is meant to say what a click will look at, not "
        "what will be found. Finding out stays the mind's work.",
    ("Legend", "states"):
        "That last rule may be the important one. It is hard to plan around something nobody has looked "
        "at, so every tree starts with a mind paying attention, and paying for it.",
    ("Legend", "tree"):
        "Nothing in a tree is meant as decoration. Every node is something that happened, and it stays "
        "there, for as long as you remain in the place.",
    ("Chain", "steps"):
        "Those costs are what set the pace. Looking and thinking are where attention goes, so they cost "
        "something. And a success refills everything as a way of favouring doing over pondering: a visit "
        "that goes well tends to keep going, one that goes badly slowly runs out of breath.",
    ("Chain", "dice"):
        "The pool being the chain is perhaps the point. A good roll should come from matching the right "
        "mind to each step, rather than from one strong number.",
    ("Functions", None):
        "This split tries to mimic how a mind might work. The part of you that wants a drink is maybe not "
        "the part that pours it, nor the part that feels bad about it later. Here, each of those is a "
        "mind, and the one you listen to is up to you.",
    ("Properties", None):
        "None of these is really a number to push up. They are there so that two minds, facing the same "
        "thing, might behave differently, and so that you can tell why.",
    ("Rules", None):
        "The difference is meant to matter. A refusal at the moment of acting is the world pushing back, "
        "so it seems fair that it costs. A thing that never crosses a mind is simply who that mind is, and "
        "it would be odd to pay for it.",
    ("Will", "course"):
        "A will is single-minded because a want perhaps is. It does not weigh options; it sees what leads "
        "to it, and the rest is noise. Which makes a will a rather poor observer, and possibly a very good "
        "guide.",
    ("Will", "leave"):
        "This may be the way to find things now. Less by wandering until something happens, more by "
        "listening to what your minds want, and letting them lead.",
    ("Morality", None):
        "The idea is that morality belongs to the minds, not to the character. A character has no "
        "alignment as such. It has a scrupulous mind and a greedy one, and which one is heard, at which "
        "step, is a choice.",
    ("Proximity", "effective"):
        "Discretion is the one property that bends the rules of presence, so it is kept narrow. It buys "
        "the attempt, and probably nothing more: never a way out of what failing costs, face to face.",
    ("Confronted", "closed"):
        "Someone who has just caught you with a hand on his shelf is unlikely to leave you room to admire "
        "the rest of the shop. When you are cornered, the screen tries to corner you too.",
    ("Wages", None):
        "Each failure tries to cost something that fits it. A failed climb hurts. A failed search loses "
        "the way. A failed conversation loses the thread. Ideally, the price can be guessed before the "
        "roll.",
    ("Party", None):
        "This is probably the deepest change. Companions used to be something like extra minds for the "
        "protagonist. Now they are people standing somewhere else, and splitting the party becomes a real "
        "decision.",
}
