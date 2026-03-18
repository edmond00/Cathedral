namespace Cathedral.Game.Narrative;

public sealed class HumanSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Human;
    public override string DisplayName => "Human";
    public override string ArtFolderPath => "assets/art/body/human";
}
