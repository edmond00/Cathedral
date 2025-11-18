using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Glyph.Microworld.LocationSystem.Generators;

/// <summary>
/// Generates varied forest locations with natural environment features
/// Demonstrates the location blueprint system without NPC complexity
/// Creates rich hierarchical sublocations and environmental state systems
/// </summary>
public class ForestFeatureGenerator : LocationFeatureGenerator
{
    /// <summary>
    /// Generates natural language context for forest location
    /// Optimized for small LLMs with environmental details
    /// </summary>
    public override string GenerateContext(string locationId)
    {
        SetSeed(locationId);
        
        var canopyDensity = Rng.Next(3); // 0=sparse, 1=moderate, 2=dense
        var timeOfDay = Rng.Next(4); // 0=dawn, 1=morning, 2=afternoon, 3=dusk
        var weather = Rng.Next(4); // 0=clear, 1=misty, 2=drizzle, 3=storm
        var wildlifeActivity = Rng.Next(3); // 0=quiet, 1=active, 2=agitated
        var seasonalFeatures = Rng.Next(4); // 0=spring, 1=summer, 2=autumn, 3=winter
        
        return $"Ancient {GetTreeDescription(canopyDensity)} spread overhead, creating a {GetCanopyDescription(canopyDensity)} canopy. " +
               $"The {GetTimeDescription(timeOfDay)} light {GetLightFilterDescription(canopyDensity)} through the branches. " +
               $"The air is {GetWeatherDescription(weather)} and filled with the scent of {GetSeasonalScents(seasonalFeatures)}. " +
               $"Wildlife seems {GetWildlifeDescription(wildlifeActivity)}, {GetWildlifeSounds(wildlifeActivity)}. " +
               $"{GetSpecialEnvironmentalFeatures()}";
    }
    
    /// <summary>
    /// Generates complete forest blueprint with varied sublocations and environmental states
    /// Creates different forest configurations based on RNG seed
    /// </summary>
    public override LocationBlueprint GenerateBlueprint(string locationId)
    {
        SetSeed(locationId);
        
        // Generate varied forest characteristics
        var forestType = Rng.Next(4); // Different overall forest characteristics
        var hasWaterFeature = Rng.NextDouble() > 0.5;
        var hasElevationChange = Rng.NextDouble() > 0.4;
        var specialFeature = Rng.Next(5); // ancient_grove, fairy_ring, ruins, cave_system, sacred_site
        var canopyDensity = Rng.Next(3);
        
        // Create categorized state system for natural environment
        var stateCategories = GenerateForestStateCategories();
        
        // Generate hierarchical sublocations with variance
        var sublocations = GenerateVariedForestSublocations(forestType, hasWaterFeature, hasElevationChange, specialFeature, canopyDensity);
        
        // Create spatial connections between sublocations
        var connections = GenerateSublocationConnections(sublocations.Keys.ToList());
        
        // Generate content mappings for environmental interactions
        var contentMap = GenerateContentMap(sublocations);
        
        var blueprint = new LocationBlueprint(locationId, "forest", stateCategories, sublocations, connections, contentMap);
        
        if (!ValidateBlueprint(blueprint))
            throw new InvalidOperationException($"Generated invalid blueprint for forest {locationId}");
            
        return blueprint;
    }
    
    /// <summary>
    /// Creates environmental state categories specific to forest locations
    /// </summary>
    private Dictionary<string, StateCategory> GenerateForestStateCategories()
    {
        return new Dictionary<string, StateCategory>
        {
            ["time_of_day"] = new(
                "time_of_day", "Time of Day",
                new Dictionary<string, LocationState>
                {
                    ["dawn"] = new("dawn", "Dawn", "First light filters through the canopy, casting long shadows"),
                    ["morning"] = new("morning", "Morning", "Bright daylight illuminates the forest floor in dappled patterns"),
                    ["afternoon"] = new("afternoon", "Afternoon", "Warm afternoon sun creates shifting light and shadow"),
                    ["dusk"] = new("dusk", "Dusk", "Golden light fades as shadows lengthen between the trees"),
                    ["night"] = new("night", "Night", "Darkness envelops the forest, lit only by pale moonlight")
                },
                "morning", StateScope.Location),
                
            ["weather"] = new(
                "weather", "Weather Conditions",
                new Dictionary<string, LocationState>
                {
                    ["clear"] = new("clear", "Clear", "Bright sky visible through the canopy overhead"),
                    ["misty"] = new("misty", "Misty", "Soft fog drifts between the tree trunks, reducing visibility"),
                    ["drizzle"] = new("drizzle", "Light Rain", "Gentle rain patters on leaves, creating a steady rhythm"),
                    ["storm"] = new("storm", "Storm", "Heavy rain and wind shake the canopy violently overhead")
                },
                "clear", StateScope.Location),
                
            ["wildlife_state"] = new(
                "wildlife_state", "Wildlife Activity",
                new Dictionary<string, LocationState>
                {
                    ["calm"] = new("calm", "Calm", "Animals go about their normal activities peacefully"),
                    ["alert"] = new("alert", "Alert", "Wildlife is wary and watching, sensing potential danger"),
                    ["agitated"] = new("agitated", "Agitated", "Animals are disturbed and fleeing, leaving the area"),
                    ["hunting"] = new("hunting", "Hunting", "Predators are actively stalking through the underbrush")
                },
                "calm", StateScope.Location),
                
            ["path_visibility"] = new(
                "path_visibility", "Trail Condition",
                new Dictionary<string, LocationState>
                {
                    ["clear_trail"] = new("clear_trail", "Clear Trail", "Well-defined path is easy to follow"),
                    ["faint_trail"] = new("faint_trail", "Faint Trail", "Barely visible track requires careful attention"),
                    ["overgrown"] = new("overgrown", "Overgrown", "Path is heavily choked with vegetation"),
                    ["lost"] = new("lost", "Lost Path", "No visible trail remains, navigation is by landmarks only")
                },
                "clear_trail", StateScope.Sublocation),
                
            ["seasonal_state"] = new(
                "seasonal_state", "Seasonal Features",
                new Dictionary<string, LocationState>
                {
                    ["spring"] = new("spring", "Spring Growth", "New leaves and flowers bloom throughout the forest"),
                    ["summer"] = new("summer", "Summer Abundance", "Dense foliage and active wildlife fill the woods"),
                    ["autumn"] = new("autumn", "Autumn Colors", "Brilliant fall colors paint the canopy overhead"),
                    ["winter"] = new("winter", "Winter Dormancy", "Bare branches and dormant vegetation dominate")
                },
                "summer", StateScope.Location)
        };
    }
    
    /// <summary>
    /// Generates varied forest sublocations based on environmental features
    /// Creates different combinations for each forest instance
    /// </summary>
    private Dictionary<string, Sublocation> GenerateVariedForestSublocations(
        int forestType, bool hasWaterFeature, bool hasElevationChange, int specialFeature, int canopyDensity)
    {
        var sublocations = new Dictionary<string, Sublocation>
        {
            // Core forest areas present in all forests
            ["forest_edge"] = new("forest_edge", "Forest Edge", "Where open meadow meets the ancient woodland",
                null, new List<string> { "outer_grove", "main_path" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>()),
                
            ["main_path"] = new("main_path", "Main Forest Path", "Primary trail winding deeper into the woods",
                "forest_edge", new List<string> { "forest_edge", "path_fork", "fallen_log" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>
                {
                    ["path_visibility"] = "clear_trail"
                }),
                
            ["outer_grove"] = new("outer_grove", "Outer Grove", "Younger trees with more open spacing and scattered undergrowth",
                "forest_edge", new List<string> { "forest_edge", "main_path", "berry_patch" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>()),
                
            ["path_fork"] = new("path_fork", "Path Fork", "Where the main trail splits into multiple directions",
                "main_path", new List<string> { "main_path", "deep_woods", "hidden_trail" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>()),
                
            ["deep_woods"] = new("deep_woods", "Deep Woods", "Ancient trees with thick canopy and rich forest floor",
                "path_fork", new List<string> { "path_fork", "old_growth_area", "moss_covered_rocks" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>()),
                
            ["old_growth_area"] = new("old_growth_area", "Old Growth Area", 
                "Massive ancient trees that have stood for centuries",
                "deep_woods", new List<string> { "deep_woods" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>()),
                
            ["moss_covered_rocks"] = new("moss_covered_rocks", "Moss-Covered Rocks", 
                "Large boulders draped in thick, soft moss",
                "deep_woods", new List<string> { "deep_woods" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>())
        };
        
        // Add density-specific sublocations
        if (canopyDensity >= 2) // Dense forest
        {
            sublocations["dense_thicket"] = new("dense_thicket", "Dense Thicket", 
                "Tightly packed undergrowth and brambles create a natural barrier",
                "main_path", new List<string> { "main_path", "small_clearing" },
                new List<string>(), new List<string> { "storm" }, // Dangerous in storms
                new Dictionary<string, string>());
                
            sublocations["canopy_break"] = new("canopy_break", "Canopy Break", 
                "Fallen giant tree creates a gap in the dense canopy overhead",
                "deep_woods", new List<string> { "deep_woods", "sunlit_glade" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>());
        }
        
        // Add water features if present
        if (hasWaterFeature)
        {
            var waterType = Rng.Next(3);
            switch (waterType)
            {
                case 0: // Stream system
                    sublocations["stream_crossing"] = new("stream_crossing", "Stream Crossing", 
                        "Babbling brook with moss-covered stepping stones",
                        "path_fork", new List<string> { "path_fork", "stream_bank", "upstream_pool" },
                        new List<string>(), new List<string>(),
                        new Dictionary<string, string>());
                    sublocations["stream_bank"] = new("stream_bank", "Stream Bank", 
                        "Muddy shore lined with ferns and animal tracks",
                        "stream_crossing", new List<string> { "stream_crossing", "reed_bed" },
                        new List<string>(), new List<string>(),
                        new Dictionary<string, string>());
                    sublocations["upstream_pool"] = new("upstream_pool", "Upstream Pool", 
                        "Deep, crystal-clear pool perfect for drinking and reflection",
                        "stream_crossing", new List<string> { "stream_crossing" },
                        new List<string>(), new List<string>(),
                        new Dictionary<string, string>());
                    break;
                    
                case 1: // Hidden pond
                    sublocations["hidden_pond"] = new("hidden_pond", "Hidden Pond", 
                        "Still water surrounded by cattails and lily pads",
                        "deep_woods", new List<string> { "deep_woods", "pond_edge", "lily_pad_area" },
                        new List<string> { "faint_trail" }, // Hard to find
                        new List<string>(),
                        new Dictionary<string, string>());
                    sublocations["pond_edge"] = new("pond_edge", "Pond Edge", 
                        "Muddy shoreline with tracks from woodland creatures",
                        "hidden_pond", new List<string> { "hidden_pond" },
                        new List<string>(), new List<string>(),
                        new Dictionary<string, string>());
                    break;
                    
                case 2: // Waterfall
                    sublocations["waterfall_base"] = new("waterfall_base", "Waterfall Base", 
                        "Cascading water creates a misty, moss-covered grotto",
                        "deep_woods", new List<string> { "deep_woods", "behind_falls" },
                        new List<string>(), new List<string>(),
                        new Dictionary<string, string>());
                    break;
            }
        }
        
        // Add elevation changes if present
        if (hasElevationChange)
        {
            sublocations["hill_base"] = new("hill_base", "Hill Base", 
                "Gentle slope begins to rise among scattered boulders",
                "deep_woods", new List<string> { "deep_woods", "steep_climb", "rocky_outcrop" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>());
            sublocations["steep_climb"] = new("steep_climb", "Steep Climb", 
                "Challenging upward path through exposed roots and loose stones",
                "hill_base", new List<string> { "hill_base", "ridge_top" },
                new List<string>(), new List<string> { "storm" }, // Dangerous in weather
                new Dictionary<string, string>());
            sublocations["ridge_top"] = new("ridge_top", "Ridge Top", 
                "High vantage point offering views above the forest canopy",
                "steep_climb", new List<string> { "steep_climb", "overlook" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>());
            sublocations["rocky_outcrop"] = new("rocky_outcrop", "Rocky Outcrop", 
                "Weathered stone formation jutting from the hillside",
                "hill_base", new List<string> { "hill_base", "steep_climb" },
                new List<string>(), new List<string>(),
                new Dictionary<string, string>());
        }
        
        // Add special features based on generation
        switch (specialFeature)
        {
            case 0: // Ancient Grove
                sublocations["ancient_grove_entrance"] = new("ancient_grove_entrance", "Ancient Grove Entrance", 
                    "Circle of massive, gnarled oaks that seem to whisper ancient secrets",
                    "deep_woods", new List<string> { "deep_woods", "grove_center", "ritual_stones" },
                    new List<string>(), new List<string>(),
                    new Dictionary<string, string>());
                sublocations["grove_center"] = new("grove_center", "Grove Center", 
                    "Sacred heart of the ancient trees where time seems to stand still",
                    "ancient_grove_entrance", new List<string> { "ancient_grove_entrance", "spirit_tree" },
                    new List<string> { "calm" }, new List<string> { "agitated" }, // Requires calm wildlife
                    new Dictionary<string, string>());
                break;
                
            case 1: // Fairy Ring
                sublocations["fairy_ring"] = new("fairy_ring", "Fairy Ring", 
                    "Perfect circle of mushrooms in a moonlit clearing",
                    "deep_woods", new List<string> { "deep_woods", "mushroom_grove" },
                    new List<string> { "dusk", "night" }, new List<string>(), // Only visible in evening/night
                    new Dictionary<string, string>());
                sublocations["mushroom_grove"] = new("mushroom_grove", "Mushroom Grove", 
                    "Dense collection of exotic fungi growing on rotting logs",
                    "fairy_ring", new List<string> { "fairy_ring" },
                    new List<string>(), new List<string>(),
                    new Dictionary<string, string>());
                break;
                
            case 2: // Overgrown Ruins
                sublocations["moss_covered_ruins"] = new("moss_covered_ruins", "Moss-Covered Ruins", 
                    "Ancient worked stones slowly being reclaimed by nature",
                    "old_growth_area", new List<string> { "old_growth_area", "collapsed_structure", "vine_archway" },
                    new List<string>(), new List<string>(),
                    new Dictionary<string, string>());
                sublocations["collapsed_structure"] = new("collapsed_structure", "Collapsed Structure", 
                    "Broken foundation overgrown with roots and wild flowering vines",
                    "moss_covered_ruins", new List<string> { "moss_covered_ruins", "hidden_chamber" },
                    new List<string>(), new List<string>(),
                    new Dictionary<string, string>
                    {
                        ["access_state"] = "blocked" // Requires clearing
                    });
                break;
                
            case 3: // Cave System
                sublocations["cave_entrance"] = new("cave_entrance", "Cave Entrance", 
                    "Dark opening in a moss-covered hillside exhaling cool, damp air",
                    "rocky_outcrop", new List<string> { "rocky_outcrop", "entrance_chamber" },
                    new List<string>(), new List<string>(),
                    new Dictionary<string, string>());
                sublocations["entrance_chamber"] = new("entrance_chamber", "Cave Entrance Chamber", 
                    "Twilight zone where forest light gradually fades into darkness",
                    "cave_entrance", new List<string> { "cave_entrance", "deeper_tunnels" },
                    new List<string>(), new List<string> { "night" }, // Need light source at night
                    new Dictionary<string, string>());
                break;
                
            case 4: // Sacred Site
                sublocations["sacred_clearing"] = new("sacred_clearing", "Sacred Clearing", 
                    "Circular meadow surrounded by ancient stones and flowering trees",
                    "deep_woods", new List<string> { "deep_woods", "stone_circle", "meditation_spot" },
                    new List<string> { "calm" }, new List<string> { "hunting" }, // Requires peaceful atmosphere
                    new Dictionary<string, string>());
                break;
        }
        
        // Add small detail sublocations for granular exploration
        AddDetailSublocations(sublocations, canopyDensity);
        
        return sublocations;
    }
    
    /// <summary>
    /// Adds small-scale detail sublocations for granular forest exploration
    /// </summary>
    private void AddDetailSublocations(Dictionary<string, Sublocation> sublocations, int canopyDensity)
    {
        sublocations["fallen_log"] = new("fallen_log", "Fallen Log", 
            "Massive tree trunk lying across the forest floor, home to insects and small creatures",
            "main_path", new List<string> { "main_path", "log_interior" },
            new List<string>(), new List<string>(),
            new Dictionary<string, string>());
            
        sublocations["berry_patch"] = new("berry_patch", "Wild Berry Patch", 
            "Thorny bushes heavy with ripe berries attract birds and small animals",
            "outer_grove", new List<string> { "outer_grove" },
            new List<string>(), new List<string>(),
            new Dictionary<string, string>
            {
                ["seasonal_state"] = "summer" // Best in summer
            });
            
        sublocations["large_oak"] = new("large_oak", "Ancient Oak", 
            "Enormous oak tree with thick branches perfect for climbing",
            "outer_grove", new List<string> { "outer_grove", "tree_canopy" },
            new List<string>(), new List<string>(),
            new Dictionary<string, string>());
            
        // Only add animal_den if dense_thicket exists (canopyDensity >= 2)
        if (canopyDensity >= 2)
        {
            sublocations["animal_den"] = new("animal_den", "Animal Den", 
                "Small burrow or hollow showing signs of recent animal habitation",
                "dense_thicket", new List<string> { "dense_thicket" },
                new List<string> { "calm" }, new List<string> { "hunting" }, // Animals avoid when predators hunt
                new Dictionary<string, string>());
        }
        else
        {
            // For less dense forests, place animal den in outer grove
            sublocations["animal_den"] = new("animal_den", "Animal Den", 
                "Small burrow or hollow showing signs of recent animal habitation",
                "outer_grove", new List<string> { "outer_grove" },
                new List<string> { "calm" }, new List<string> { "hunting" }, // Animals avoid when predators hunt
                new Dictionary<string, string>());
        }
    }
    
    /// <summary>
    /// Creates spatial connections between forest sublocations
    /// Ensures logical navigation paths through the hierarchical structure
    /// </summary>
    protected override Dictionary<string, List<string>> GenerateSublocationConnections(List<string> sublocationIds)
    {
        var connections = new Dictionary<string, List<string>>();
        
        // Initialize empty connections for all sublocations
        foreach (var id in sublocationIds)
        {
            connections[id] = new List<string>();
        }
        
        // Define logical forest navigation patterns
        // Most connections are already defined in the sublocation DirectConnections
        // This method can add additional logical connections based on forest layout
        
        return connections;
    }
    
    /// <summary>
    /// Generates content mappings for environmental forest interactions
    /// Maps items, actions, and encounters to specific sublocation/state combinations
    /// </summary>
    protected override Dictionary<string, Dictionary<string, LocationContent>> GenerateContentMap(
        Dictionary<string, Sublocation> sublocations)
    {
        var contentMap = new Dictionary<string, Dictionary<string, LocationContent>>();
        
        foreach (var (sublocationId, sublocation) in sublocations)
        {
            contentMap[sublocationId] = new Dictionary<string, LocationContent>
            {
                ["default"] = GenerateDefaultContent(sublocationId, sublocation)
            };
            
            // Add state-specific content
            AddStateSpecificContent(contentMap[sublocationId], sublocationId, sublocation);
        }
        
        return contentMap;
    }
    
    /// <summary>
    /// Generates default content for a forest sublocation
    /// </summary>
    private LocationContent GenerateDefaultContent(string sublocationId, Sublocation sublocation)
    {
        var items = new List<string>();
        var actions = new List<string>();
        var companions = new List<string>();
        var quests = new List<string>();
        
        // Add location-appropriate items and actions
        switch (sublocationId)
        {
            case "berry_patch":
                items.AddRange(new[] { "wild_berries", "medicinal_herbs" });
                actions.AddRange(new[] { "forage_berries", "examine_plants", "check_for_thorns" });
                break;
                
            case "stream_crossing":
                items.AddRange(new[] { "clear_water", "smooth_stones" });
                actions.AddRange(new[] { "cross_stream", "follow_water_upstream", "search_banks" });
                break;
                
            case "fallen_log":
                items.AddRange(new[] { "bark_samples", "beetle_collection" });
                actions.AddRange(new[] { "examine_insects", "search_hollow", "climb_over" });
                break;
                
            case "ancient_oak":
                items.AddRange(new[] { "acorns", "bark_medicine" });
                actions.AddRange(new[] { "climb_tree", "examine_roots", "rest_in_shade" });
                break;
                
            case "cave_entrance":
                items.AddRange(new[] { "cave_minerals", "bat_guano" });
                actions.AddRange(new[] { "enter_cave", "examine_entrance", "listen_for_sounds" });
                break;
                
            default:
                // Generic forest actions
                actions.AddRange(new[] { "search_area", "listen_carefully", "examine_ground" });
                break;
        }
        
        // Add possible forest companions (woodland creatures, not NPCs)
        if (Rng.NextDouble() < 0.3) // 30% chance
        {
            companions.Add(GetRandomForestCompanion());
        }
        
        return new LocationContent(items, companions, quests, new List<string>(), actions);
    }
    
    /// <summary>
    /// Adds state-specific content variations to sublocation content map
    /// </summary>
    private void AddStateSpecificContent(Dictionary<string, LocationContent> sublocationContentMap, 
        string sublocationId, Sublocation sublocation)
    {
        // Add weather-specific content
        sublocationContentMap["storm"] = new LocationContent(
            new List<string> { "rain_water", "storm_debris" },
            new List<string>(), // No companions in storms
            new List<string>(),
            new List<string>(),
            new List<string> { "seek_shelter", "collect_rainwater", "wait_out_storm" });
            
        // Add time-specific content
        sublocationContentMap["night"] = new LocationContent(
            new List<string> { "nocturnal_herbs", "moonlight_flowers" },
            new List<string> { "night_owl", "firefly_swarm" },
            new List<string>(),
            new List<string>(),
            new List<string> { "navigate_by_stars", "listen_for_night_sounds", "make_camp" });
            
        // Add seasonal content
        sublocationContentMap["autumn"] = new LocationContent(
            new List<string> { "fallen_leaves", "nuts_and_seeds", "mushrooms" },
            new List<string>(),
            new List<string>(),
            new List<string>(),
            new List<string> { "gather_fallen_nuts", "track_animal_preparations", "enjoy_fall_colors" });
    }
    
    /// <summary>
    /// Returns a random forest companion (woodland creatures)
    /// </summary>
    private string GetRandomForestCompanion()
    {
        var companions = new[]
        {
            "curious_squirrel", "wise_old_owl", "friendly_deer", "forest_cat",
            "helpful_rabbit", "majestic_stag", "playful_fox", "gentle_badger"
        };
        return companions[Rng.Next(companions.Length)];
    }

    // Helper methods for context generation
    private string GetTreeDescription(int density) => density switch
    {
        0 => "scattered oaks and birches",
        1 => "mixed hardwood trees",
        2 => "dense forest giants",
        _ => "ancient woodland trees"
    };
    
    private string GetCanopyDescription(int density) => density switch
    {
        0 => "sparse",
        1 => "moderate",
        2 => "dense",
        _ => "thick"
    };
    
    private string GetLightFilterDescription(int density) => density switch
    {
        0 => "streams freely",
        1 => "filters down in patches",
        2 => "barely penetrates",
        _ => "struggles to reach"
    };
    
    private string GetTimeDescription(int time) => time switch
    {
        0 => "pale dawn",
        1 => "bright morning",
        2 => "warm afternoon", 
        3 => "golden dusk",
        _ => "deep night"
    };
    
    private string GetWeatherDescription(int weather) => weather switch
    {
        0 => "clear and crisp",
        1 => "soft and misty", 
        2 => "damp with light rain",
        3 => "heavy with storm clouds",
        _ => "still and humid"
    };
    
    private string GetWildlifeDescription(int activity) => activity switch
    {
        0 => "peaceful and undisturbed",
        1 => "active and alert",
        2 => "agitated and restless",
        _ => "calm"
    };
    
    private string GetWildlifeSounds(int activity) => activity switch
    {
        0 => "with only gentle rustling in the undergrowth",
        1 => "with bird calls echoing through the trees",
        2 => "with creatures fleeing and branches breaking",
        _ => "in comfortable silence"
    };
    
    private string GetSeasonalScents(int season) => season switch
    {
        0 => "fresh growth and flowering buds",
        1 => "rich earth and blooming wildflowers",
        2 => "fallen leaves and ripening fruit",
        3 => "crisp air and dormant earth",
        _ => "pine and moss"
    };
    
    private string GetSpecialEnvironmentalFeatures()
    {
        var features = new[]
        {
            "Shafts of sunlight create natural spotlights on the forest floor.",
            "The sound of distant water adds a gentle rhythm to the woodland symphony.",
            "Ancient trees bear scars of lightning strikes from storms long past.",
            "Moss-covered stones hint at structures built by long-forgotten hands.",
            "Natural clearings offer glimpses of sky through the leafy ceiling.",
            "Fallen logs create natural bridges and hiding places for woodland creatures.",
            "The air carries the scent of wildflowers blooming in hidden meadows."
        };
        return features[Rng.Next(features.Length)];
    }
}