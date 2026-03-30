namespace Cathedral.Game.Narrative;

/// <summary>
/// A body location (body part or organ part) that is eligible to receive a wildcard wound
/// from the narrative failure outcome critic tree.
/// <para>
/// TargetId is used as the choice id in the critic tree (e.g. "trunk", "left_arm").
/// DisplayName is the human-readable label shown in the critic prompt.
/// ZoneHint is stored on the wound instance after selection so that BodyArtViewer can
/// constrain the glyph placement to cells belonging to this location.
/// </para>
/// </summary>
public record WildcardCandidate(string TargetId, string DisplayName, string ZoneHint);
