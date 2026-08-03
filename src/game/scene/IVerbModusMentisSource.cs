namespace Cathedral.Game.Scene;

/// <summary>
/// Implemented by scene elements that want to decide, per verb, which modus mentis a successful
/// execution teaches. This is the <i>target override</i> half of the grant rule: a verb declares a
/// sensible default (EXAMINE teaches scrutiny), and the thing being acted on may say better
/// (examining a mushroom cluster teaches mycology; examining a statue teaches iconography).
///
/// <para>Returning null means "no opinion" — the verb's own
/// <see cref="Verbs.Verb.GrantedModusMentisId"/> stands. Ids are matched against
/// <c>ModusMentisRegistry</c>; a typo grants nothing, silently, which is why <c>--verb-audit</c>
/// resolves every declared id.</para>
/// </summary>
public interface IVerbModusMentisSource
{
    /// <summary>The modus mentis id this element teaches for <paramref name="verbId"/>, or null.</summary>
    string? ModusMentisFor(string verbId);
}
