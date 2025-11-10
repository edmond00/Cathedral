using Cathedral.LLM.JsonConstraints;

namespace Cathedral.LLM.JsonConstraints;

/// <summary>
/// Demonstrates usage of the JsonConstraintGenerator system with practical examples
/// </summary>
public static class JsonConstraintDemo
{
    /// <summary>
    /// Creates the SceneEvent schema from the design document
    /// </summary>
    public static CompositeField CreateSceneEventSchema()
    {
        return new CompositeField("SceneEvent", new JsonField[]
        {
            new ChoiceField<string>("type", "combat", "dialogue", "exploration"),
            new VariantField("content",
                new CompositeField("combat", new JsonField[]
                {
                    new IntField("enemyLevel", 1, 100),
                    new ChoiceField<string>("enemyType", "goblin", "dragon", "bandit")
                }),
                new CompositeField("dialogue", new JsonField[]
                {
                    new StringField("speaker", 3, 20),
                    new TemplateStringField("text", "says <generated>", 10, 100)
                }),
                new CompositeField("exploration", new JsonField[]
                {
                    new StringField("location", 5, 30),
                    new StringField("description", 20, 200),
                    new BooleanField("hasLoot")
                })
            )
        });
    }

    /// <summary>
    /// Creates a character creation schema example
    /// </summary>
    public static CompositeField CreateCharacterSchema()
    {
        return new CompositeField("Character", new JsonField[]
        {
            new StringField("name", 3, 20),
            new IntField("age", 0, 120),
            new ChoiceField<string>("class", "warrior", "mage", "rogue", "archer"),
            new CompositeField("stats", new JsonField[]
            {
                new IntField("strength", 1, 10),
                new IntField("dexterity", 1, 10),
                new IntField("intelligence", 1, 10),
                new IntField("constitution", 1, 10)
            }),
            new ArrayField("skills", new StringField("skill", 3, 15), 0, 5),
            new OptionalField("backstory", new StringField("backstory", 50, 500)),
            new FloatField("health", 1.0, 100.0)
        });
    }

    /// <summary>
    /// Creates a quest system schema example
    /// </summary>
    public static CompositeField CreateQuestSchema()
    {
        return new CompositeField("Quest", new JsonField[]
        {
            new StringField("title", 5, 50),
            new StringField("description", 20, 300),
            new ChoiceField<string>("difficulty", "easy", "medium", "hard", "legendary"),
            new ArrayField("objectives", 
                new CompositeField("objective", new JsonField[]
                {
                    new StringField("description", 10, 100),
                    new BooleanField("completed"),
                    new OptionalField("reward", new IntField("gold", 0, 1000))
                }), 1, 5),
            new VariantField("questType",
                new CompositeField("killQuest", new JsonField[]
                {
                    new StringField("targetType", 3, 20),
                    new IntField("targetCount", 1, 50)
                }),
                new CompositeField("fetchQuest", new JsonField[]
                {
                    new StringField("itemName", 3, 30),
                    new IntField("quantity", 1, 20),
                    new StringField("location", 5, 40)
                }),
                new CompositeField("escortQuest", new JsonField[]
                {
                    new StringField("npcName", 3, 25),
                    new StringField("destination", 5, 40),
                    new BooleanField("npcSurvived")
                })
            )
        });
    }

    /// <summary>
    /// Demonstrates generating GBNF and templates for all example schemas
    /// </summary>
    public static void RunDemonstration()
    {
        Console.WriteLine("=== JSON Constraint Generator Demonstration ===\n");

        // Scene Event Example
        var sceneEventSchema = CreateSceneEventSchema();
        Console.WriteLine("1. Scene Event Schema");
        Console.WriteLine("GBNF Grammar:");
        Console.WriteLine(JsonConstraintGenerator.GenerateGBNF(sceneEventSchema));
        Console.WriteLine("\nJSON Template:");
        Console.WriteLine(JsonConstraintGenerator.GenerateTemplate(sceneEventSchema));
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        // Character Example
        var characterSchema = CreateCharacterSchema();
        Console.WriteLine("2. Character Schema");
        Console.WriteLine("GBNF Grammar:");
        Console.WriteLine(JsonConstraintGenerator.GenerateGBNF(characterSchema));
        Console.WriteLine("\nJSON Template:");
        Console.WriteLine(JsonConstraintGenerator.GenerateTemplate(characterSchema));
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        // Quest Example
        var questSchema = CreateQuestSchema();
        Console.WriteLine("3. Quest Schema");
        Console.WriteLine("GBNF Grammar:");
        Console.WriteLine(JsonConstraintGenerator.GenerateGBNF(questSchema));
        Console.WriteLine("\nJSON Template:");
        Console.WriteLine(JsonConstraintGenerator.GenerateTemplate(questSchema));
    }

    /// <summary>
    /// Saves generated GBNF and template files to disk
    /// </summary>
    public static void SaveExamplesToFiles()
    {
        var outputDir = Path.Combine(Environment.CurrentDirectory, "JsonConstraintExamples");
        Directory.CreateDirectory(outputDir);

        var examples = new[]
        {
            ("SceneEvent", CreateSceneEventSchema()),
            ("Character", CreateCharacterSchema()),
            ("Quest", CreateQuestSchema())
        };

        foreach (var (name, schema) in examples)
        {
            var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
            var template = JsonConstraintGenerator.GenerateTemplate(schema);

            File.WriteAllText(Path.Combine(outputDir, $"{name}.gbnf"), gbnf);
            File.WriteAllText(Path.Combine(outputDir, $"{name}_template.json"), template);

            Console.WriteLine($"Generated {name}.gbnf and {name}_template.json");
        }

        Console.WriteLine($"\nFiles saved to: {outputDir}");
    }
}

/// <summary>
/// Utility class for validating LLM-generated JSON against schemas
/// </summary>
public static class JsonValidator
{
    /// <summary>
    /// Basic validation that checks if a JSON string could match a schema structure
    /// This is a simplified validator - a full implementation would be more comprehensive
    /// </summary>
    public static bool ValidateBasicStructure(string jsonString, JsonField schema)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(jsonString);
            return ValidateElement(document.RootElement, schema);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static bool ValidateElement(System.Text.Json.JsonElement element, JsonField field)
    {
        return field switch
        {
            IntField intField => element.ValueKind == System.Text.Json.JsonValueKind.Number &&
                               element.TryGetInt32(out var intVal) &&
                               intVal >= intField.Min && intVal <= intField.Max,
            
            FloatField floatField => element.ValueKind == System.Text.Json.JsonValueKind.Number &&
                                   element.TryGetDouble(out var doubleVal) &&
                                   doubleVal >= floatField.Min && doubleVal <= floatField.Max,
            
            StringField stringField => element.ValueKind == System.Text.Json.JsonValueKind.String &&
                                     element.GetString()!.Length >= stringField.MinLength &&
                                     element.GetString()!.Length <= stringField.MaxLength,
            
            BooleanField => element.ValueKind == System.Text.Json.JsonValueKind.True ||
                           element.ValueKind == System.Text.Json.JsonValueKind.False,
            
            ChoiceField<string> stringChoice => element.ValueKind == System.Text.Json.JsonValueKind.String &&
                                              stringChoice.Options.Contains(element.GetString()),
            
            ChoiceField<int> intChoice => element.ValueKind == System.Text.Json.JsonValueKind.Number &&
                                        element.TryGetInt32(out var choiceIntVal) &&
                                        intChoice.Options.Contains(choiceIntVal),
            
            CompositeField composite => element.ValueKind == System.Text.Json.JsonValueKind.Object &&
                                      composite.Fields.All(childField =>
                                          element.TryGetProperty(childField.Name, out var childElement) &&
                                          ValidateElement(childElement, childField)),
            
            ArrayField arrayField => element.ValueKind == System.Text.Json.JsonValueKind.Array &&
                                   element.GetArrayLength() >= arrayField.MinLength &&
                                   element.GetArrayLength() <= arrayField.MaxLength &&
                                   element.EnumerateArray().All(item => ValidateElement(item, arrayField.ElementType)),
            
            _ => true // For unsupported types, assume valid
        };
    }
}