using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Generates narration nodes for forest exploration.
/// Creates a graph of connected narrative scenes with keywords and outcomes.
/// </summary>
public class ForestNarrationNodeGenerator
{
    private readonly List<NarrationNode> _nodes = new();
    
    public ForestNarrationNodeGenerator()
    {
        GenerateForestNodes();
    }
    
    private void GenerateForestNodes()
    {
        // Node 1: Sun-Dappled Clearing (Entry Node)
        var clearingOutcomes = new Dictionary<string, List<Outcome>>
        {
            ["dappled shadows"] = new()
            {
                new Outcome(OutcomeType.Transition, "hollow_oak", null),
                new Outcome(OutcomeType.Skill, "perception", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Melancholia"] = +2 })
            },
            ["moss-covered stone"] = new()
            {
                new Outcome(OutcomeType.Transition, "hidden_stream", null),
                new Outcome(OutcomeType.Item, "rare_mushroom", null),
                new Outcome(OutcomeType.Companion, "curious_squirrel", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Laetitia"] = +5 })
            },
            ["ancient oaks"] = new()
            {
                new Outcome(OutcomeType.Transition, "hollow_oak", null),
                new Outcome(OutcomeType.Skill, "dendrology", null),
                new Outcome(OutcomeType.Item, "oakwood_branch", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Melancholia"] = +3 })
            },
            ["bird calls"] = new()
            {
                new Outcome(OutcomeType.Transition, "berry_bramble", null),
                new Outcome(OutcomeType.Companion, "songbird", null),
                new Outcome(OutcomeType.Skill, "ornithology", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Blood"] = +5 })
            },
            ["tree roots"] = new()
            {
                new Outcome(OutcomeType.Transition, "rock_outcrop", null),
                new Outcome(OutcomeType.Item, "grub_larvae", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Appetitus"] = +2 })
            },
            ["leaf litter"] = new()
            {
                new Outcome(OutcomeType.Item, "medicinal_herb", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Phlegm"] = +3 })
            }
        };
        
        _nodes.Add(new NarrationNode(
            NodeId: "clearing",
            NodeName: "Sun-Dappled Clearing",
            NeutralDescription: "A circular clearing opens in the dense forest, approximately twenty meters across. Sunlight streams through gaps in the canopy, creating pools of golden light on the leaf-strewn ground. Seven ancient oaks form the perimeter, their gnarled roots breaking through the earth. At the center rests a moss-covered stone, weathered smooth by centuries of rain.",
            Keywords: new() { "dappled shadows", "moss-covered stone", "ancient oaks", "leaf litter", "bird calls", "tree roots" },
            KeywordIntroExamples: new()
            {
                ["dappled shadows"] = "There are dappled shadows",
                ["moss-covered stone"] = "There is a moss-covered stone",
                ["ancient oaks"] = "There are ancient oaks",
                ["leaf litter"] = "There is leaf litter",
                ["bird calls"] = "There are bird calls",
                ["tree roots"] = "There are tree roots"
            },
            OutcomesByKeyword: clearingOutcomes,
            IsEntryNode: true,
            PossibleTransitions: new() { "hidden_stream", "hollow_oak", "berry_bramble", "rock_outcrop" }
        ));
        
        // Node 2: Hidden Stream
        var streamOutcomes = new Dictionary<string, List<Outcome>>
        {
            ["flowing water"] = new()
            {
                new Outcome(OutcomeType.Transition, "pond", null),
                new Outcome(OutcomeType.Skill, "hydrodynamics", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Laetitia"] = +4 })
            },
            ["darting fish"] = new()
            {
                new Outcome(OutcomeType.Item, "fresh_fish", null),
                new Outcome(OutcomeType.Companion, "otter", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Appetitus"] = +3 })
            },
            ["smooth stones"] = new()
            {
                new Outcome(OutcomeType.Item, "skipping_stone", null),
                new Outcome(OutcomeType.Skill, "geology", null),
                new Outcome(OutcomeType.Transition, "rock_outcrop", null)
            },
            ["ferns and reeds"] = new()
            {
                new Outcome(OutcomeType.Item, "reed_bundle", null),
                new Outcome(OutcomeType.Skill, "botany", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Phlegm"] = +2 })
            },
            ["dragonflies"] = new()
            {
                new Outcome(OutcomeType.Skill, "entomology", null),
                new Outcome(OutcomeType.Companion, "dragonfly", null),
                new Outcome(OutcomeType.Humor, "", new() { ["Blood"] = +3 })
            }
        };
        
        _nodes.Add(new NarrationNode(
            NodeId: "hidden_stream",
            NodeName: "Hidden Stream",
            NeutralDescription: "A narrow stream cuts through the forest floor, its clear water flowing over smooth river stones. The banks are thick with ferns and reeds. Small fish dart between stones. The sound of flowing water fills the air, peaceful and constant.",
            Keywords: new() { "flowing water", "smooth stones", "darting fish", "ferns and reeds", "dragonflies" },
            KeywordIntroExamples: new()
            {
                ["flowing water"] = "There is flowing water",
                ["smooth stones"] = "There are smooth stones",
                ["darting fish"] = "There are darting fish",
                ["ferns and reeds"] = "There are ferns and reeds",
                ["dragonflies"] = "There are dragonflies"
            },
            OutcomesByKeyword: streamOutcomes,
            IsEntryNode: false,
            PossibleTransitions: new() { "pond", "rock_outcrop", "clearing" }
        ));
        
        // Add more nodes as needed (hollow_oak, berry_bramble, rock_outcrop, pond, etc.)
        // For now, just these two demonstrate the structure
    }
    
    public NarrationNode? GetNode(string nodeId)
    {
        return _nodes.Find(n => n.NodeId == nodeId);
    }
    
    public NarrationNode GetRandomEntryNode()
    {
        var entryNodes = _nodes.FindAll(n => n.IsEntryNode);
        if (entryNodes.Count == 0)
            return _nodes[0]; // Fallback to first node
        
        var random = new System.Random();
        return entryNodes[random.Next(entryNodes.Count)];
    }
    
    public List<NarrationNode> GetAllNodes()
    {
        return new List<NarrationNode>(_nodes);
    }
}
