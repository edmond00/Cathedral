using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// Global traits about the <b>body</b>: how a person is built, and what has already happened to them.
/// These are the traits most likely to carry a wound, and every one of them shows in the appearance
/// clause — a disabled organ the player cannot see would be a mechanic with no fiction attached.
/// </summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterBodyTraits()
    {
        Add(new PersonalityTrait
        {
            TraitId     = "ox_strong",
            DisplayName = "Ox-Strong",
            Organs      = new[] { ("left_arm", 1), ("right_arm", 1), ("backbone", 1) },
            ModiMentis  = new[] { "brute_force", "hard_labor" },
            Appearance  = "built heavy through the shoulders, with forearms like knotted rope",
            Persona     = "You are unusually strong and quietly aware of it. You never threaten anyone — you have never needed to.",
            Opinions    = new[] { (DialogueTopic.Work, "I can do the work of two, so I'm given the work of three. That's how it goes") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "one_eyed",
            DisplayName = "One-Eyed",
            Wounds      = new Func<Wound>[] { () => new PiercedEyeLeftWound() },
            Items       = new Func<Item>[] { () => new EyePatch() },
            ModiMentis  = new[] { "vigilance" },
            Appearance  = "the left eye is gone — a leather patch on a greasy cord covers the socket",
            Persona     = "You lost your left eye years ago. You turn your head further than most people do to look at things, and you have stopped being embarrassed about it.",
            Opinions    = new[] { (DialogueTopic.Health, "I manage. You'd be surprised how little you need two of them for") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "hard_of_hearing",
            DisplayName = "Hard of Hearing",
            Wounds      = new Func<Wound>[] { () => new PerforatedEardrumRightWound() },
            Organs      = new[] { ("left_ear", 1) },
            Appearance  = "turns the left ear toward whoever is speaking, and watches mouths",
            Persona     = "You are deaf in the right ear. You ask people to repeat themselves and you speak louder than you think you do.",
            Opinions    = new[] { (DialogueTopic.Neighbours, "Half of what people say about me, I never hear. I've decided that's a mercy") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "lame_leg",
            DisplayName = "Lame",
            Wounds      = new Func<Wound>[] { () => new KneeFractureRightWound() },
            Items       = new Func<Item>[] { () => new WoodenCrutch() },
            ModiMentis  = new[] { "patience" },
            Appearance  = "moves with a heavy, rolling limp, leaning on a rag-padded crutch",
            Persona     = "Your right knee was broken years ago and set badly. Standing hurts, walking hurts more, and you do not want it remarked on.",
            Opinions    = new[]
            {
                (DialogueTopic.Health, "It aches before rain and it aches after. I've stopped keeping score"),
                (DialogueTopic.Roads,  "Roads are for people with two good legs. I stay where I am"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "scarred_face",
            DisplayName = "Scar-Faced",
            Wounds      = new Func<Wound>[] { () => new ScarWound() },
            ModiMentis  = new[] { "stoneface" },
            Appearance  = "a pale seam of old scar runs from the cheekbone down into the jaw",
            Persona     = "A long scar crosses your face. Strangers look at it and then look away, and you have learned to let the pause happen without filling it.",
            Opinions    = new[] { (DialogueTopic.Neighbours, "People here stopped seeing it years ago. It's strangers who make it a thing") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "missing_fingers",
            DisplayName = "Short of Fingers",
            Wounds      = new Func<Wound>[] { () => new FingersAmputeeLeftWound() },
            Organs      = new[] { ("right_hand", 1) },
            Appearance  = "two fingers are missing from the left hand, the stumps long healed over",
            Persona     = "You lost two fingers on your left hand to your own work. You do everything one-handed that you can, and you are quietly proud of how little it slows you.",
            Opinions    = new[] { (DialogueTopic.Work, "Took two fingers off me before I learned to respect it. Cheap, as lessons go") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "broken_nose",
            DisplayName = "Broken-Nosed",
            Wounds      = new Func<Wound>[] { () => new BrokenNoseWound() },
            ModiMentis  = new[] { "brawling" },
            Appearance  = "the nose has been broken and reset crooked, more than once by the look of it",
            Persona     = "Your nose has been broken twice. You are not a violent person now, but you were once, and it shows.",
            Opinions    = new[] { (DialogueTopic.Neighbours, "I was a worse man at twenty. Some here still hold it against me, and fairly") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "great_height",
            DisplayName = "Long-Limbed",
            Organs      = new[] { ("left_leg", 1), ("right_leg", 1) },
            ModiMentis  = new[] { "bearing" },
            Appearance  = "stands a full head above most folk and stoops out of habit under low beams",
            Persona     = "You are very tall. You stoop indoors without thinking and people always notice you first in a crowd, which you find tiresome.",
            Opinions    = new[] { (DialogueTopic.Kin, "Everyone in my family is built like this. Doorways were not made with us in mind") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "small_and_quick",
            DisplayName = "Small and Quick",
            Organs      = new[] { ("left_foot", 1), ("right_foot", 1), ("cerebellum", 1) },
            ModiMentis  = new[] { "finesse", "surefoot" },
            Appearance  = "small, wiry and never quite still",
            Persona     = "You are small and fast and you have spent your life being underestimated by larger people. You enjoy that more than you let on.",
            Opinions    = new[] { (DialogueTopic.Work, "There's places I can get into that nobody else here can. That's worth more than a big back") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "iron_gut",
            DisplayName = "Iron-Gutted",
            Organs      = new[] { ("paunch", 1), ("viscera", 1) },
            ModiMentis  = new[] { "iron_stomach", "trencherman" },
            Appearance  = "thick through the middle and entirely untroubled by it",
            Persona     = "You can eat anything and it has never once made you ill. It is a small thing to be proud of and you are proud of it anyway.",
            Opinions    = new[] { (DialogueTopic.Food, "I've eaten worse than you can imagine and slept fine after. It's a gift") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "weak_chested",
            DisplayName = "Weak-Chested",
            Organs      = new[] { ("pulmones", -1) },
            ModiMentis  = new[] { "patience", "herblore" },
            Appearance  = "pauses for breath more often than the work should require",
            Persona     = "Your chest has always been bad. You get through the day, but you do it slowly, and cold weather frightens you.",
            Opinions    = new[]
            {
                (DialogueTopic.Health,  "My chest. It's been my chest since I was a child and it'll be my chest at the end"),
                (DialogueTopic.Weather, "Cold damp air is the enemy. I can feel a bad winter coming a week before it arrives"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "keen_eyed",
            DisplayName = "Keen-Eyed",
            Organs      = new[] { ("left_eye", 1), ("right_eye", 1) },
            ModiMentis  = new[] { "scrutiny", "deadeye" },
            Appearance  = "has a habit of looking past you at something you have not noticed yet",
            Persona     = "Your eyesight is remarkable and you notice things other people miss. You mention them, sometimes to your cost.",
            Opinions    = new[] { (DialogueTopic.Neighbours, "I see who comes and goes. I don't go telling it about, but I see") },
        });
    }
}
