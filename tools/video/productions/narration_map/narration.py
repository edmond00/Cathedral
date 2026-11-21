"""The narration of the animated segments, keyed by scene and step. Every rule is the manual's
(docs/manual/06-narration.md and 02-memory.md on narration-map) or the design note's
(design/NARRATION_MAP.md). The segments over gameplay are written in build_storyboard.py, against
the takes' timelines.

Written to be heard once: short sentences, the subject first, one idea each. The sentences of a MANIM
part keep their order, since a scene's beats follow them."""

MANIM = {
    ("Principles", None): [
        "The old screen had its charm. When you arrived somewhere, a mind wrote a paragraph about it. "
        "Some words lit up, and you clicked a word to look closer. It read a bit like a book. But a book "
        "is not a good place to stand in. You could not see where things were. You could not tell where "
        "a word would lead. And the text scrolled away, often just when you needed it again.",
        "Now the place is the page. Each area is a card, and each way is a line between two cards. Under "
        "each card is a row of symbols: the things you can look at. Think of it as a mind map of the "
        "place. The visit grows on it. You click one symbol, and the mind you choose looks at that thing. "
        "The symbol moves under its card and becomes a root, with the text the mind wrote. Think about "
        "it, and a branch grows. It shows what the mind wants, and the act it proposes, with the sixes "
        "it needs. Do the act, and the branch ends with the result, and how it made you feel. The old "
        "page scrolled away as you read. Here nothing does. Every text stays where it grew. As the "
        "visit goes on, trees fill the map, and you can read the whole visit at a glance. Walking back "
        "where you have been is free. A way you have never taken is an act, like any other.",
    ],
    ("Legend", "kinds"): [
        "Each card is an area. Under its name is a row of symbols, one for each thing in it. There are "
        "four kinds. An arch is a way out: a neighbouring area, a door, a stair, a path or a climb. A "
        "face is a person, or any creature that is not your enemy. A skull is an enemy. A diamond is "
        "any other thing: a cart, a bush, a chest, a body. A hollow diamond is something seen inside a "
        "container.",
    ],
    ("Legend", "states"): [
        "Each thing in a place is in one of three states, for each member of the party. Unknown: you "
        "only know its kind and where it is. You click the kind, and the mind you choose picks which "
        "one to look at. Known: you remember it from an earlier visit, or saw it inside a container. "
        "It has a name, and a click looks at that one. Observed: you looked at it on this visit. You "
        "can only think about what you observed, and it becomes the root of a tree.",
    ],
    ("Legend", "tree"): [
        "A tree holds everything the visit wrote about one thing. First the root, and under it the text "
        "the observing mind wrote. Then a question mark, to think about it, as long as there is "
        "something to do with it. Then the thought, and the act it proposed, with the number of sixes "
        "it needs. You can click it whenever you like. Then the result: a tick or a cross, and how it "
        "made you feel. A refusal, from any mind, for any reason, is a crossed circle. A quotation mark "
        "is something told to a companion. Hover over any node to read its text again.",
    ],
    ("Chain", "steps"): [
        "The rules of an act did not change. An act still takes three minds. One mind looks. You must "
        "be in the area, and it costs one noetic point. One mind thinks, and decides what it wants. It "
        "can think from anywhere in the place, and it costs one point. One mind acts. The body walks "
        "there first, and the act itself costs nothing more.",
        "Your noetic points come from your brain. A success refills all of them, right away. A failure "
        "refills nothing. With no points left, you can still walk where you know the way, do the acts "
        "already waiting, and leave.",
    ],
    ("Chain", "dice"): [
        "You roll one die for each level of each mind in the chain: the one that looked, the one that "
        "thought, and the one that acts. A tool adds its own level. The difficulty is how many sixes "
        "you need. An eager mind needs one six less. A reluctant mind needs one more. If you need more "
        "sixes than you have dice, the act cannot succeed. And an act of difficulty zero needs no roll.",
    ],
    ("Functions", None): [
        "So which mind does what? Each mind has one to three functions, out of eight. It can observe, "
        "think, act, speak, fight, feel, want, or know a trade. No mind both thinks and acts. No mind "
        "both acts and feels. Wanting something, doing it, and being moved by it are three minds, not "
        "one.",
        "The will is the one to understand. A will does not weigh options. It wants a few kinds of act, "
        "and it only looks and thinks in that direction.",
    ],
    ("Properties", None): [
        "Each mind also has a few properties, and each one changes how it behaves on the map. Here are "
        "three real minds. Beast Sense looks and thinks. Petty Thief is a will to steal, and it acts. "
        "Diligence is a will to work, and it speaks and acts.",
        "Its sources, a body region or two organs, set how high its level can go. A wound lowers it. "
        "Its memory type says where it can be stored. Its moral level, low, medium or high, only "
        "matters when it thinks or acts. Its doubt says how many second thoughts it gets. And a "
        "discreet mind can act with someone in the room, and is not heard through a wall.",
    ],
    ("Rules", None): [
        "Two kinds of rule can stop an act. Rules of action judge an act you already chose. They refuse "
        "it, in the acting mind's own voice, and that costs a point. That man is watching. That door "
        "needs a key. My arm is ruined. Rules of choice decide what is offered at all. They hide "
        "options silently, and cost nothing. A principled mind is never offered a crime. A speaking "
        "mind turns to people. The idea simply never occurs to it.",
    ],
    ("Will", "course"): [
        "Take Petty Thief. It wants one thing: to steal. The game works out its course. That is the "
        "shortest chain of acts from here to a theft: open the shop door, walk to the shelf, steal. "
        "Asked to look, it only looks at the first step of that course. Asked about anything else, it "
        "says no, and names the first step. That refusal is part of its nature, and costs nothing. And "
        "when it does what it wants, it is eager: it needs one six less, and it never refuses.",
    ],
    ("Will", "candidates"): [
        "When it thinks, a will is less strict. Any act that starts a course of three acts or fewer is "
        "a candidate. It proposes the shortest one. A second thought takes the next. Longer courses "
        "are ignored, unless the place has nothing shorter.",
    ],
    ("Will", "leave"): [
        "And when the place offers no course at all, the will refuses everything. It says it would "
        "have to leave to find what it wants. That is not a dead end. It is a hint. From the world "
        "map, you can send a will to search. It tries the nearest places first. The first place where "
        "its course exists becomes your destination. The visit is then made for that want, however "
        "long the course.",
    ],
    ("Morality", None): [
        "Morality is checked twice, because choosing a goal and choosing who does it are two decisions. "
        "A high-moral thinking mind is never offered a crime. A high-moral acting mind refuses a crime "
        "before it is even offered. A low-moral thinking mind, when a crime is possible, is offered "
        "only crimes, and must pick one. And a low-moral acting mind cannot refuse a crime at all. So "
        "an honest hand can still be chosen to carry out what a low mind decided.",
    ],
    ("Proximity", "raw"): [
        "Being seen raises two separate questions. First: can I act at all? With someone in the room, "
        "an ordinary mind is refused. That is a rule of action, so it costs a point. A discreet mind "
        "can try. That is all discretion gives you up close. And a discreet mind cannot back out of an "
        "act that only its discretion allows.",
    ],
    ("Proximity", "effective"): [
        "Second: what does failing cost? There are three levels. In the same room, a witness catches "
        "you, and an enemy attacks. Behind a wall, in the same building, they come to look. They walk "
        "into your room, and now someone is there. Further away, nothing happens. Discretion keeps you "
        "quiet through a wall, but never in the same room.",
    ],
    ("Confronted", "closed"): [
        "An enemy in the room confronts you. So does a witness with a reason to stop you. The map then "
        "closes around them. You can answer them. Explaining yourself makes every check of that "
        "conversation one six easier. Or you can do an act already waiting, if a discreet mind will. "
        "A thinking mind is only offered those answers, unless a discreet mind could act. You cannot "
        "look at anything else, think about anything else, walk away, or talk to a companion. And "
        "someone asleep confronts nobody.",
    ],
    ("Confronted", "exit"): [
        "The way out is one symbol. When nobody stops you, it says Leave. Every member walks out by "
        "known ways, for free. If a walk meets someone who would confront you, it stops there. When "
        "you are confronted, it says Runaway. You roll your feet's dice and need a single six. The "
        "humors can help. The roll is made again in every area on the way, and failing it starts the "
        "fight, or the conversation.",
    ],
    ("Wages", None): [
        "A failure costs three things. Your points are not refilled. There can be one social "
        "consequence: you are caught, attacked, or someone comes to look. And there is one penalty, "
        "based on what the act risked. A wound, for something that can hurt you. A lost way, for "
        "finding a way: the visit ends and you are outside. A lost hour, for hard and slow work. Or a "
        "lost thread, one more noetic point, for the senses, speech and waiting.",
    ],
    ("Party", None): [
        "The party changed the most. Each member is now a separate body in the place. It stays where "
        "you last took it. It spends its own points, uses its own minds, and keeps its own trees. "
        "Click a token to switch member, for free. A member with a speaking mind can tell a nearby "
        "companion about something it observed. That costs the speaker a point, and puts that thing "
        "on the listener's map, already observed. A beast gets no conversation, no lock, no tool and "
        "no letters. And a fight only involves the members standing where it starts.",
    ],
}


# The reasons behind each rule, said over each scene once its picture is complete: the scene's beats follow
# the MANIM parts (the storyboard's "core"), and these follow them. Each is drawn from the design note
# (design/NARRATION_MAP.md) or the rule's own comments in the code; said softly, never in the first person.
WHY = {
    ("Principles", None):
        "That is the whole idea. The screen should look like what happens underneath: minds working on a "
        "place, and leaving marks where they worked. At the end of a visit, the map should tell you what "
        "happened there.",
    ("Legend", "kinds"):
        "Only four kinds, on purpose. A symbol tells you what a click will look at, not what you will "
        "find. Finding out is still the mind's job.",
    ("Legend", "states"):
        "That last rule may be the most important. You cannot plan around something nobody has looked at. "
        "So every tree starts with a mind paying attention, and paying for it.",
    ("Legend", "tree"):
        "Nothing in a tree is decoration. Every node is something that happened. And it stays there as "
        "long as you are in the place.",
    ("Chain", "steps"):
        "These costs set the pace. Looking and thinking take attention, so they cost something. A success "
        "refills everything, to reward doing over thinking. A visit that goes well keeps going. One that "
        "goes badly slowly runs out of breath.",
    ("Chain", "dice"):
        "That may be the point of the chain. A good roll should come from picking the right mind for each "
        "step, not from one big number.",
    ("Functions", None):
        "This split tries to copy how a mind might work. The part of you that wants a drink may not be the "
        "part that pours it. And neither is the part that feels bad about it later. Here, each of those is "
        "a mind, and you choose which one to listen to.",
    ("Properties", None):
        "None of these is really a number to raise. They are there so that two minds can act differently "
        "with the same thing, and so you can see why.",
    ("Rules", None):
        "The difference matters. A refusal when you act is the world pushing back, so it seems fair that "
        "it costs. Something that never crosses a mind is just who that mind is. It would be strange to "
        "pay for that.",
    ("Will", "course"):
        "A will is single-minded because a want probably is. It does not compare options. It sees what "
        "leads to what it wants, and ignores the rest. That makes a will a poor observer, but maybe a very "
        "good guide.",
    ("Will", "leave"):
        "This may be the new way to find things. Not by wandering until something happens, but by "
        "listening to what your minds want, and letting them lead.",
    ("Morality", None):
        "The idea is that morality belongs to the minds, not to the character. Your character has no "
        "alignment. It has an honest mind and a greedy one. Which one you listen to, at which step, is "
        "your choice.",
    ("Proximity", "effective"):
        "Discretion is the only property that bends these rules, so it is kept narrow. It lets you try. "
        "It never saves you from what failing costs, face to face.",
    ("Confronted", "closed"):
        "Someone who just caught you stealing will not give you time to look around the shop. When you "
        "are cornered, the screen tries to corner you too.",
    ("Wages", None):
        "Each failure tries to cost something that fits it. A failed climb hurts. A failed search loses "
        "the way. A failed conversation loses the thread. Ideally, you can guess the price before you "
        "roll.",
    ("Party", None):
        "This is probably the biggest change. Companions used to be like extra minds for the hero. Now "
        "they are people standing somewhere else. Splitting the party becomes a real decision.",
}
