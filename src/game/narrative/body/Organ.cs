using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for organs. An organ belongs to one body part and contains one or more organ parts.
/// For single-part organs, the organ part has the same id as the organ.
/// Score is the sum of its organ part scores.
/// </summary>
public abstract class Organ
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string BodyPartId { get; }
    public abstract List<OrganPart> Parts { get; }

    /// <summary>
    /// Encyclopedia-style flavour text describing the organ's overall purpose and the
    /// disciplines it lends to. Shown in the creation and body menus, in the band between
    /// the organ list and the derived-stat detail, when the organ is hovered.
    /// </summary>
    public virtual string Description => "";
    
    /// <summary>
    /// When true, each organ part of this organ appears as a candidate target for wildcard
    /// wounds in the narrative failure outcome critic tree.
    /// </summary>
    public virtual bool AcceptsWildcardWounds => false;

    /// <summary>
    /// Combat granularity: does each organ part act as its OWN independent fighting medium?
    ///
    /// <para>When <c>true</c> (and the organ has more than one part), the fight menu shows one
    /// tab per part and each part strikes independently — e.g. hands yield a Left Hand and a
    /// Right Hand tab, so Punch can be thrown once with each hand in a single turn, and each
    /// punch rolls dice from that hand's own score.</para>
    ///
    /// <para>When <c>false</c> (the default), the whole organ is a single fighting medium: the
    /// fight menu shows one tab for the organ, each action is usable once per turn, and the
    /// dice level is the organ's total score (the sum of its parts). This suits organs whose
    /// parts act together — e.g. legs (you run/jump/dodge with both legs at once).</para>
    ///
    /// Single-part organs are always whole-organ mediums regardless of this flag.
    /// </summary>
    public virtual bool PartsAreIndependentMediums => false;

    /// <summary>
    /// Score is the sum of all organ part scores.
    /// </summary>
    public int Score => Parts.Sum(p => p.Score);

    /// <summary>Maximum possible score = the sum of all organ part max scores (species-adjusted).</summary>
    public int MaxScore => Parts.Sum(p => p.MaxScore);
}
