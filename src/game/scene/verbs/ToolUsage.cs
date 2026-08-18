namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// How a verb stands towards implements. Every verb declares one; the default is
/// <see cref="Optional"/>, so a new verb behaves as every verb did before this existed.
///
/// <para>The line between the three is drawn by what a combined item mechanically <i>does</i>: it
/// joins the die chain as its leaf. So the question a category answers is "can holding a thing
/// change how well this goes?" — not "would it read well".</para>
/// </summary>
public enum ToolUsage
{
    /// <summary>
    /// Nothing carried can help. Speech (the tree decides what follows, and cannot see the item),
    /// the senses (no state change at all — the narration is the outcome), thought and phase
    /// transitions, waiting, walking on the flat, swimming, and the acts of persuasion turned upon
    /// an animal, where showing a beast an axe is the opposite of the goal.
    ///
    /// <para>A combination attempted here fails at once, without a critic call, and costs a noetic
    /// point — unless the implement carries an explicit exception for this very verb.</para>
    /// </summary>
    Excluded,

    /// <summary>
    /// Bare hands will do, and an implement may help. The item-use critic judges whatever was
    /// combined against bare hands, and the body's proficiency decides how generous a judgement it
    /// needs. This is the default and by far the largest group.
    /// </summary>
    Optional,

    /// <summary>
    /// The verb cannot be attempted bare-handed at all: <c>ReferenceToolIds</c> names what it wants,
    /// <c>RequiredToolRule</c> refuses the attempt when nothing is combined, and the critic judges a
    /// substitute against the named tool rather than against bare hands.
    ///
    /// <para>Implies <see cref="AnatomyCapability.Handcraft"/> — see
    /// <c>Verb.EffectiveCapabilities</c> — so a body that cannot hold a tool is never offered the
    /// verb rather than being charged for attempting it.</para>
    /// </summary>
    Required,
}
