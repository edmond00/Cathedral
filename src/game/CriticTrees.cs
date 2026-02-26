using System;
using System.Collections.Generic;

namespace Cathedral.Game;

/// <summary>
/// Factory class for building the standard Critic evaluation trees.
/// </summary>
public static class CriticTrees
{
    #region Plausibility Tree
    
    /// <summary>
    /// Builds the plausibility tree - a linear chain where all checks must pass.
    /// Failure returns an error message to send to the narrator.
    /// </summary>
    public static CriticNode BuildPlausibilityTree(string actionText, string contextDescription)
    {
        // Node 5: Not Contradictory (innermost)
        var notContradictory = new CriticNode(
            name: "NotContradictory",
            question: $"In the following context : {contextDescription}\nThe {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nIs this action consistent with what just happened and doesn't contradict recent events?",
            yesIsSuccess: true,
            threshold: 0.5,
            errorMessage: "This contradicts what just occurred"
        );
        
        // Node 4: Has Required Elements
        var hasRequiredElements = new CriticNode(
            name: "HasRequiredElements",
            question: $"In the following context : {contextDescription}\nThe {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nAre the objects, people, or elements needed for this action available or present?",
            yesIsSuccess: true,
            threshold: 0.5,
            errorMessage: "You don't have what's needed to do this"
        );
        hasRequiredElements.SuccessBranch = notContradictory;
        
        // Node 3: Context Appropriate
        var contextAppropriate = new CriticNode(
            name: "ContextAppropriate",
            question: $"In the following context : {contextDescription}\nThe {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nDoes this action make sense given the current location and situation?",
            yesIsSuccess: true,
            threshold: 0.5,
            errorMessage: "This action doesn't fit the current situation"
        );
        contextAppropriate.SuccessBranch = hasRequiredElements;
        
        // Node 2: Reasonable Timeframe
        var reasonableTimeframe = new CriticNode(
            name: "ReasonableTimeframe",
            question: $"In the following context : {contextDescription}\nThe {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nIs this action short enough to be completed in less than one hour?",
            yesIsSuccess: true,
            threshold: 0.5,
            errorMessage: "This action would take too long to complete"
        );
        reasonableTimeframe.SuccessBranch = contextAppropriate;
        
        // Node 1: Physically Possible (root)
        var physicallyPossible = new CriticNode(
            name: "PhysicallyPossible",
            question: $"In the following context : {contextDescription}\nThe {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nIs this action physically possible for a human to attempt?",
            yesIsSuccess: true,
            threshold: 0.5,
            errorMessage: "This action is physically impossible"
        );
        physicallyPossible.SuccessBranch = reasonableTimeframe;
        
        return physicallyPossible;
    }
    
    #endregion
    
    #region Difficulty Tree
    
    /// <summary>
    /// Builds the difficulty tree - all 10 questions are asked (threshold=0).
    /// YES = hard, NO = easy for all questions.
    /// Returns the tree root; use CalculateDifficultyFromResult to get the difficulty score.
    /// </summary>
    public static CriticNode BuildDifficultyTree(string actionText, string contextDescription)
    {
        // All questions have threshold=0 so they always "pass" to the next one
        // YES = hard (high score), NO = easy (low score)
        
        var contextPrefix = $"In the following context : {contextDescription}\n";
        
        var questions = new (string name, string question)[]
        {
            ("RequiresStrength", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nDoes this action require physical strength beyond average?"),
            
            ("RequiresTraining", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nDoes this action require specialized training or expertise?"),
            
            ("RequiresTools", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nDoes this action require tools, equipment, or materials?"),
            
            ("RequiresTime", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nDoes this action take significant time or sustained effort?"),
            
            ("RequiresPrecision", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nDoes this action require precise coordination or dexterity?"),
            
            ("RequiresFocus", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nDoes this action require mental focus or concentration?"),
            
            ("HasRisks", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nDoes this action carry risk of injury or negative consequences?"),
            
            ("EnvironmentFactor", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nWould environmental conditions typically make this action harder?"),
            
            ("HardToUndo", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nIs this action difficult to undo or correct if done wrong?"),
            
            ("ExpertChallenge", 
             $"{contextPrefix}The {Config.Narrative.PlayerName} is considering the following action : \"{actionText}\",\nWould even a trained professional find this challenging?")
        };
        
        // Build the chain - all nodes lead to the next via SuccessBranch AND FailureBranch
        // This ensures all questions are asked regardless of answer
        CriticNode? previous = null;
        CriticNode? root = null;
        
        for (int i = questions.Length - 1; i >= 0; i--)
        {
            var (name, question) = questions[i];
            var node = new CriticNode(
                name: name,
                question: question,
                yesIsSuccess: true,  // YES means "hard" 
                threshold: 0.0,      // Threshold 0 = always continue to next
                errorMessage: ""     // No error messages for difficulty checks
            );
            
            // Both branches lead to the next question
            if (previous != null)
            {
                node.SuccessBranch = previous;
                node.FailureBranch = previous;
            }
            
            previous = node;
            if (i == 0) root = node;
        }
        
        return root!;
    }
    
    /// <summary>
    /// Calculates the difficulty score (0.0 to 1.0) from the difficulty tree result.
    /// Uses the average of all YES probabilities (Score field).
    /// </summary>
    public static double CalculateDifficultyFromResult(CriticTreeResult result)
    {
        if (result.Trace.Count == 0)
            return 0.5;
        
        double totalScore = 0;
        foreach (var node in result.Trace)
        {
            // Score = P(yes) / (P(yes) + P(no))
            // Higher score = more likely YES = harder
            totalScore += node.Score;
        }
        
        return totalScore / result.Trace.Count;
    }
    
    /// <summary>
    /// Converts a 0.0-1.0 difficulty to an integer 1-10 scale.
    /// </summary>
    public static int DifficultyToScale(double difficulty)
    {
        return Math.Clamp((int)Math.Ceiling(difficulty * 10), 1, 10);
    }
    
    #endregion
    
    #region Failure Outcome Tree
    
    /// <summary>
    /// Represents a failure outcome with its associated humor/consequence.
    /// </summary>
    public class FailureOutcomeType
    {
        public string Name { get; set; } = "";
        public string HumorAffected { get; set; } = "";
        public int HumorAmount { get; set; } = 1;
        public string Description { get; set; } = "";
        public string NarratorHint { get; set; } = "";
    }
    
    // Pre-defined failure outcomes
    public static readonly FailureOutcomeType LimbInjury = new()
    {
        Name = "LimbInjury",
        HumorAffected = "Yellow Bile",
        HumorAmount = 3,
        Description = "Injury to arms, hands, legs, or feet",
        NarratorHint = "The character has injured a limb (arm, hand, leg, or foot)"
    };
    
    public static readonly FailureOutcomeType HeadTrauma = new()
    {
        Name = "HeadTrauma",
        HumorAffected = "Black Bile",
        HumorAmount = 4,
        Description = "Head or face injury, possible concussion",
        NarratorHint = "The character has hit their head or face, possibly dazed or disoriented"
    };
    
    public static readonly FailureOutcomeType TrunkInjury = new()
    {
        Name = "TrunkInjury",
        HumorAffected = "Yellow Bile",
        HumorAmount = 3,
        Description = "Injury to trunk, ribs, or back",
        NarratorHint = "The character has injured their trunk, ribs, or back"
    };
    
    public static readonly FailureOutcomeType GeneralSevereInjury = new()
    {
        Name = "GeneralSevereInjury",
        HumorAffected = "Yellow Bile",
        HumorAmount = 4,
        Description = "Serious physical injury",
        NarratorHint = "The character has suffered a serious physical injury"
    };
    
    public static readonly FailureOutcomeType MinorInjury = new()
    {
        Name = "MinorInjury",
        HumorAffected = "Yellow Bile",
        HumorAmount = 1,
        Description = "Bruise, scrape, or muscle strain",
        NarratorHint = "The character has a minor injury like a bruise, scrape, or muscle strain"
    };
    
    public static readonly FailureOutcomeType PhysicalExhaustion = new()
    {
        Name = "PhysicalExhaustion",
        HumorAffected = "Phlegm",
        HumorAmount = 2,
        Description = "Fatigue and breathlessness",
        NarratorHint = "The character is exhausted, fatigued, or out of breath"
    };
    
    public static readonly FailureOutcomeType SocialHumiliation = new()
    {
        Name = "SocialHumiliation",
        HumorAffected = "Black Bile",
        HumorAmount = 4,
        Description = "Reputation or relationship damage",
        NarratorHint = "The character has been publicly humiliated, damaging their reputation"
    };
    
    public static readonly FailureOutcomeType PublicEmbarrassment = new()
    {
        Name = "PublicEmbarrassment",
        HumorAffected = "Black Bile",
        HumorAmount = 2,
        Description = "Temporary public shame",
        NarratorHint = "The character is embarrassed in front of others"
    };
    
    public static readonly FailureOutcomeType Frustration = new()
    {
        Name = "Frustration",
        HumorAffected = "Black Bile",
        HumorAmount = 2,
        Description = "Self-directed frustration and criticism",
        NarratorHint = "The character is frustrated with themselves and their failure"
    };
    
    public static readonly FailureOutcomeType MildDisappointment = new()
    {
        Name = "MildDisappointment",
        HumorAffected = "Melancholia",
        HumorAmount = 1,
        Description = "Mild disappointment",
        NarratorHint = "The character feels mildly disappointed but not deeply affected"
    };
    
    public static readonly FailureOutcomeType SignificantMaterialLoss = new()
    {
        Name = "SignificantMaterialLoss",
        HumorAffected = "Black Bile",
        HumorAmount = 3,
        Description = "Valuable item destroyed or lost",
        NarratorHint = "Something valuable has been lost or destroyed"
    };
    
    public static readonly FailureOutcomeType MinorMaterialDamage = new()
    {
        Name = "MinorMaterialDamage",
        HumorAffected = "Yellow Bile",
        HumorAmount = 1,
        Description = "Minor item damage",
        NarratorHint = "An item has been slightly damaged"
    };
    
    public static readonly FailureOutcomeType WastedEffort = new()
    {
        Name = "WastedEffort",
        HumorAffected = "Phlegm",
        HumorAmount = 1,
        Description = "Time and effort wasted, must restart",
        NarratorHint = "The character's effort was wasted and they may need to start over"
    };
    
    public static readonly FailureOutcomeType MinorSetback = new()
    {
        Name = "MinorSetback",
        HumorAffected = "Phlegm",
        HumorAmount = 1,
        Description = "Small setback, easily recoverable",
        NarratorHint = "A minor setback that won't have lasting consequences"
    };
    
    public static readonly FailureOutcomeType NeutralFailure = new()
    {
        Name = "NeutralFailure",
        HumorAffected = "Melancholia",
        HumorAmount = 1,
        Description = "Action simply doesn't work, no lasting effect",
        NarratorHint = "The action simply didn't work, with no particular consequence"
    };
    
    /// <summary>
    /// Builds the failure outcome tree - binary branching to determine consequence type.
    /// The tree navigates based on YES/NO answers to find the most appropriate failure outcome.
    /// </summary>
    public static CriticNode BuildFailureOutcomeTree(string actionText, string contextDescription)
    {
        var contextPrefix = $"In the following context : {contextDescription}\n";
        // === LEAF NODES (Outcomes) ===
        // These are terminal nodes that will be identified by their names
        
        // Physical injury leaves
        var limbInjuryNode = CreateOutcomeLeaf("Outcome_LimbInjury", actionText, contextPrefix,
            "Would failing specifically risk injury to arms, hands, legs, or feet?");
        
        var headInjuryNode = CreateOutcomeLeaf("Outcome_HeadTrauma", actionText, contextPrefix,
            "Would failing specifically risk injury to the head or face?");
        
        var trunkInjuryNode = CreateOutcomeLeaf("Outcome_TrunkInjury", actionText, contextPrefix,
            "Would failing risk injury to the trunk, ribs, or back?");
        // TrunkInjury is default for "specific body part but not limb or head"
        
        var generalSevereNode = CreateOutcomeLeaf("Outcome_GeneralSevereInjury", actionText, contextPrefix,
            "Could the injury affect multiple body parts or the whole body?");
        
        var minorInjuryNode = CreateOutcomeLeaf("Outcome_MinorInjury", actionText, contextPrefix,
            "Would it cause minor pain like a bruise, scrape, or muscle strain?");
        
        var exhaustionNode = CreateOutcomeLeaf("Outcome_PhysicalExhaustion", actionText, contextPrefix,
            "Would failure mainly cause fatigue or exhaustion rather than injury?");
        
        // Mental/emotional leaves
        var humiliationNode = CreateOutcomeLeaf("Outcome_SocialHumiliation", actionText, contextPrefix,
            "Could the public failure damage reputation or important relationships?");
        
        var embarrassmentNode = CreateOutcomeLeaf("Outcome_PublicEmbarrassment", actionText, contextPrefix,
            "Would the embarrassment be temporary without lasting social damage?");
        
        var frustrationNode = CreateOutcomeLeaf("Outcome_Frustration", actionText, contextPrefix,
            "Would you blame yourself and feel frustrated about the failure?");
        
        var disappointmentNode = CreateOutcomeLeaf("Outcome_MildDisappointment", actionText, contextPrefix,
            "Would the emotional impact be mild and pass quickly?");
        
        // Material leaves
        var significantLossNode = CreateOutcomeLeaf("Outcome_SignificantMaterialLoss", actionText, contextPrefix,
            "Are valuable or irreplaceable items at risk of being destroyed?");
        
        var minorDamageNode = CreateOutcomeLeaf("Outcome_MinorMaterialDamage", actionText, contextPrefix,
            "Would only minor or easily replaceable items be affected?");
        
        var wastedEffortNode = CreateOutcomeLeaf("Outcome_WastedEffort", actionText, contextPrefix,
            "Would the main consequence be having to start over or redo work?");
        
        var minorSetbackNode = CreateOutcomeLeaf("Outcome_MinorSetback", actionText, contextPrefix,
            "Is this just a small delay with no real consequence?");
        
        var neutralFailureNode = CreateOutcomeLeaf("Outcome_NeutralFailure", actionText, contextPrefix,
            "Would the action simply not work, with no particular negative effect?");
        
        // === BRANCH NODES ===
        
        // Head injury check
        var headCheck = new CriticNode(
            name: "HeadInjuryCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nIf this action fails and causes injury, is the head or face at risk?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        headCheck.SuccessBranch = headInjuryNode;
        headCheck.FailureBranch = trunkInjuryNode; // Default to trunk if not head
        
        // Limb injury check
        var limbCheck = new CriticNode(
            name: "LimbInjuryCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nIf this action fails and causes injury, are the arms, hands, legs, or feet at risk?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        limbCheck.SuccessBranch = limbInjuryNode;
        limbCheck.FailureBranch = headCheck;
        
        // Specific body part check
        var specificBodyPart = new CriticNode(
            name: "SpecificBodyPartCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nIf this action fails and causes serious injury, is a specific body part particularly at risk?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        specificBodyPart.SuccessBranch = limbCheck;
        specificBodyPart.FailureBranch = generalSevereNode;
        
        // Minor pain check
        var minorPainCheck = new CriticNode(
            name: "MinorPainCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nIf this action fails, would it cause minor pain, strain, or discomfort rather than serious injury?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        minorPainCheck.SuccessBranch = minorInjuryNode;
        minorPainCheck.FailureBranch = exhaustionNode;
        
        // Severe injury check
        var severeInjuryCheck = new CriticNode(
            name: "SevereInjuryCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nCould failing this action cause serious injury such as broken bones, deep cuts, or significant pain?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        severeInjuryCheck.SuccessBranch = specificBodyPart;
        severeInjuryCheck.FailureBranch = minorPainCheck;
        
        // Reputation damage check
        var reputationCheck = new CriticNode(
            name: "ReputationDamageCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nIf others witness this failure, could it damage your reputation or relationships?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        reputationCheck.SuccessBranch = humiliationNode;
        reputationCheck.FailureBranch = embarrassmentNode;
        
        // Self-blame check
        var selfBlameCheck = new CriticNode(
            name: "SelfBlameCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nIf this fails privately, would you blame yourself and feel frustrated?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        selfBlameCheck.SuccessBranch = frustrationNode;
        selfBlameCheck.FailureBranch = disappointmentNode;
        
        // Public failure check
        var publicFailureCheck = new CriticNode(
            name: "PublicFailureCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nWould others witness or know about this failure?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        publicFailureCheck.SuccessBranch = reputationCheck;
        publicFailureCheck.FailureBranch = selfBlameCheck;
        
        // High value check
        var highValueCheck = new CriticNode(
            name: "HighValueCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nAre valuable or important items at risk of being lost or destroyed?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        highValueCheck.SuccessBranch = significantLossNode;
        highValueCheck.FailureBranch = minorDamageNode;
        
        // Wasted effort check
        var wastedEffortCheck = new CriticNode(
            name: "WastedEffortCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nWould failure mainly mean wasted time or effort?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        wastedEffortCheck.SuccessBranch = wastedEffortNode;
        wastedEffortCheck.FailureBranch = minorSetbackNode;
        
        // Material consequence check
        var materialCheck = new CriticNode(
            name: "MaterialConsequenceCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nCould failing cause loss or damage to objects or resources?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        materialCheck.SuccessBranch = highValueCheck;
        materialCheck.FailureBranch = wastedEffortCheck;
        
        // Mental consequence check
        var mentalCheck = new CriticNode(
            name: "MentalConsequenceCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nCould failing cause emotional or psychological distress?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        mentalCheck.SuccessBranch = publicFailureCheck;
        mentalCheck.FailureBranch = materialCheck;
        
        // Physical consequence check
        var physicalCheck = new CriticNode(
            name: "PhysicalConsequenceCheck",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nCould failing this action cause physical harm or bodily discomfort?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        physicalCheck.SuccessBranch = severeInjuryCheck;
        physicalCheck.FailureBranch = mentalCheck;
        
        // ROOT: Has any consequences?
        var root = new CriticNode(
            name: "HasConsequences",
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\nDoes failing this action have any negative consequences beyond simply not succeeding?",
            yesIsSuccess: true,
            threshold: 0.5
        );
        root.SuccessBranch = physicalCheck;
        root.FailureBranch = neutralFailureNode;
        
        return root;
    }
    
    /// <summary>
    /// Creates a leaf node for an outcome (terminal node).
    /// </summary>
    private static CriticNode CreateOutcomeLeaf(string outcomeName, string actionText, string contextPrefix, string confirmQuestion)
    {
        return new CriticNode(
            name: outcomeName,
            question: $"{contextPrefix}The {Config.Narrative.PlayerName} failed at the following action : \"{actionText}\",\n{confirmQuestion}",
            yesIsSuccess: true,
            threshold: 0.5,
            errorMessage: ""
        );
    }
    
    /// <summary>
    /// Gets the failure outcome based on the final node name from the tree trace.
    /// </summary>
    public static FailureOutcomeType GetFailureOutcomeFromResult(CriticTreeResult result)
    {
        if (result.Trace.Count == 0)
            return NeutralFailure;
        
        // Get the last node evaluated
        var lastNode = result.Trace[^1];
        var nodeName = lastNode.NodeName;
        
        return nodeName switch
        {
            "Outcome_LimbInjury" => LimbInjury,
            "Outcome_HeadTrauma" => HeadTrauma,
            "Outcome_TrunkInjury" => TrunkInjury,
            "Outcome_GeneralSevereInjury" => GeneralSevereInjury,
            "Outcome_MinorInjury" => MinorInjury,
            "Outcome_PhysicalExhaustion" => PhysicalExhaustion,
            "Outcome_SocialHumiliation" => SocialHumiliation,
            "Outcome_PublicEmbarrassment" => PublicEmbarrassment,
            "Outcome_Frustration" => Frustration,
            "Outcome_MildDisappointment" => MildDisappointment,
            "Outcome_SignificantMaterialLoss" => SignificantMaterialLoss,
            "Outcome_MinorMaterialDamage" => MinorMaterialDamage,
            "Outcome_WastedEffort" => WastedEffort,
            "Outcome_MinorSetback" => MinorSetback,
            "Outcome_NeutralFailure" => NeutralFailure,
            _ => NeutralFailure
        };
    }
    
    #endregion
}
