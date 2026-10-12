"""The generic cards a storyboard can use without writing a scene ("type": "title" / "card").

Thin subclasses, because manim only renders scenes defined in the file it is pointed at.
"""

from vidkit import manimkit


class TitleCard(manimkit.TitleCard):
    pass


class TextCard(manimkit.TextCard):
    pass
