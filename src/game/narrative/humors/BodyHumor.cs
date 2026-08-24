using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for all body humor instances stored inside a HumorQueue.
/// Each instance carries its own type identity plus UI presentation data and
/// gameplay-effect descriptors (VitalHeat, TransmutingVirtue).
///
/// Gameplay effects (consuming VitalHeat, applying TransmutingVirtue to dice)
/// are not yet executed — the public properties serve as data for future systems.
/// </summary>
public abstract class BodyHumor
{
    /// <summary>Human-readable humor type name (e.g. "Blood", "Black Bile").</summary>
    public abstract string Name { get; }

    /// <summary>Symbol character displayed in the humor queue UI.</summary>
    public abstract char Symbol { get; }

    /// <summary>Color used to render this humor's symbol in the terminal UI.</summary>
    public abstract Vector4 Color { get; }

    /// <summary>Background color used to render this humor's cell in the terminal UI.</summary>
    public virtual Vector4 BackgroundColor => new(0f, 0f, 0f, 1f);

    /// <summary>
    /// Energy delta when this humor provides vital heat.
    /// Positive = energy gain, Negative = energy drain.
    /// </summary>
    public abstract int VitalHeat { get; }

    /// <summary>
    /// Dice modification applied when this humor's transmuting virtue is invoked.
    /// Null when the humor has no transmuting effect.
    /// </summary>
    public abstract TransmutingVirtue? TransmutingVirtue { get; }

    /// <summary>
    /// One first-person sentence naming what carrying this humor feels like, written plainly.
    ///
    /// <para>This is the second half of every emotion narration: the neutral line handed to the
    /// emotion modus mentis is <c>&lt;what just happened&gt; &lt;what it makes me feel&gt;</c>, and this
    /// is that second clause. It exists because a humor is otherwise <b>cryptic</b> — "Laetitia" is a
    /// name, not a feeling, and an LLM asked to rewrite it invents whichever emotion the persona
    /// flatters. Written here, per humor, the feeling is fixed and the persona only colours it.</para>
    ///
    /// <para>Empty on every humor that is not a mind state: the secreted four, Juvenescence and the
    /// consumables are things a body carries, not things it feels. An emotion trigger naming one of
    /// those would produce a narration with nothing to say, which is why
    /// <see cref="Cathedral.Game.Narrative.ModusMentisRuleValidator"/> R15 refuses it.</para>
    /// </summary>
    public virtual string FeelsLike => "";

    /// <summary>
    /// When true this humor cannot be erased by normal queue removal.
    /// Black bile instances are pinned at the back of the queue; if removal is needed,
    /// the nearest non-black-bile item is removed instead.
    /// A queue entirely filled with black bile is critical (future: causes death).
    /// </summary>
    public virtual bool IsBlackBile => false;
}
