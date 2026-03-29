using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Observations.Forest;

/// <summary>
/// Owl Pellet Site observation — a collection of regurgitated owl pellets.
/// Two sub-outcomes: TinyBones, MatPellet.
/// </summary>
public class OwlPelletSiteObservation : ObservationObject
{
    public override string ObservationId => "owl_pellet_site";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("a grey oval <pellet> on the ground below"),
        KeywordInContext.Parse("some tiny <bone>s pressed into the pellet surface"),
        KeywordInContext.Parse("a mat of compacted <fur> around the bones"),
        KeywordInContext.Parse("the clear <evidence> of a raptor roosting above"),
    };

    private static readonly string[] Moods = { "dry", "scattered", "compressed", "aged", "numerous", "grey", "informative", "preserved" };

    public OwlPelletSiteObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new TinyBones(),
            new MatPellet(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} owl pellet site";
    }

    public sealed class TinyBones : Item
    {
        public override string ItemId => "pellet_tiny_bones";
        public override string DisplayName => "Tiny Bones";
        public override string Description => "Small rodent bones from dissected owl pellets";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("a tiny <bone> from a small rodent"),
            KeywordInContext.Parse("a minuscule <skull> intact inside the pellet"),
        };
    }

    public sealed class MatPellet : Item
    {
        public override string ItemId => "owl_pellet_mat_pellet";
        public override string DisplayName => "Matted Pellet";
        public override string Description => "A compressed pellet of fur and bone";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("a compact <bolus> of compressed fur and bone"),
            KeywordInContext.Parse("the matted <fur> wrapping the indigestible parts"),
        };
    }
}
