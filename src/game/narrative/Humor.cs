namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents a body humor that can be affected by outcomes.
/// Each humor has an associated question for the Critic to evaluate
/// whether an action should increase this humor.
/// </summary>
public class Humor
{
    public string Name { get; init; }
    
    /// <summary>
    /// Question to ask the Critic LLM to determine if an action increases this humor.
    /// Example: "Can this make you happy?" for Voluptas (pleasure/joy)
    /// </summary>
    public string CriticQuestion { get; init; }
    
    public Humor(string name, string criticQuestion)
    {
        Name = name;
        CriticQuestion = criticQuestion;
    }
    
    /// <summary>
    /// Registry of all available humors with their Critic evaluation questions.
    /// </summary>
    public static class Registry
    {
        public static readonly Humor BlackBile = new("Black Bile", 
            "Does this action cause frustration, anger, or self-criticism?");
        
        public static readonly Humor YellowBile = new("Yellow Bile", 
            "Does this action cause irritation, impatience, or agitation?");
        
        public static readonly Humor Phlegm = new("Phlegm", 
            "Does this action cause calmness, resignation, or emotional detachment?");
        
        public static readonly Humor Melancholia = new("Melancholia", 
            "Does this action cause sadness, disappointment, or contemplation of mortality?");
        
        public static readonly Humor Ether = new("Ether", 
            "Does this action cause confusion, disorientation, or a dreamlike state?");
        
        public static readonly Humor Blood = new("Blood", 
            "Does this action cause excitement, energy, or heightened awareness?");
        
        public static readonly Humor Appetitus = new("Appetitus", 
            "Does this action cause hunger, desire, or physical craving?");
        
        public static readonly Humor Voluptas = new("Voluptas", 
            "Does this action cause pleasure, joy, or sensory satisfaction?");
        
        public static readonly Humor Laetitia = new("Laetitia", 
            "Does this action cause contentment, satisfaction, or peaceful happiness?");
        
        public static readonly Humor Euphoria = new("Euphoria", 
            "Does this action cause intense joy, elation, or transcendent bliss?");
        
        /// <summary>
        /// Gets all available humors.
        /// </summary>
        public static Humor[] All => new[]
        {
            BlackBile, YellowBile, Phlegm, Melancholia, Ether,
            Blood, Appetitus, Voluptas, Laetitia, Euphoria
        };
    }
}
