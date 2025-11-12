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
                    new DigitField("enemyLevel", 3),  // 3-digit enemy level (000-999)
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
            new DigitField("age", 3),    // 3-digit age (000-999)
            new ChoiceField<string>("class", "warrior", "mage", "rogue", "archer"),
            new CompositeField("stats", new JsonField[]
            {
                new DigitField("strength", 2),     // 2-digit strength (00-99)
                new DigitField("dexterity", 2),    // 2-digit dexterity (00-99)
                new DigitField("intelligence", 2), // 2-digit intelligence (00-99)
                new DigitField("constitution", 2)  // 2-digit constitution (00-99)
            }),
            new ArrayField("skills", new StringField("skill", 3, 15), 0, 5),
            new OptionalField("backstory", new StringField("backstory", 50, 500)),
            new DigitField("health", 3)  // 3-digit health (000-999)
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
                    new OptionalField("reward", new DigitField("gold", 4))  // 4-digit gold (0000-9999)
                }), 1, 5),
            new VariantField("questType",
                new CompositeField("killQuest", new JsonField[]
                {
                    new StringField("targetType", 3, 20),
                    new DigitField("targetCount", 2)  // 2-digit target count (00-99)
                }),
                new CompositeField("fetchQuest", new JsonField[]
                {
                    new StringField("itemName", 3, 30),
                    new DigitField("quantity", 2),    // 2-digit quantity (00-99)
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
/// Comprehensive utility class for validating LLM-generated JSON against schemas
/// </summary>
public static class JsonValidator
{
    /// <summary>
    /// Validates a JSON string against a JsonField schema with detailed error reporting
    /// </summary>
    /// <param name="jsonString">The JSON string to validate</param>
    /// <param name="schema">The schema to validate against</param>
    /// <param name="errors">List of validation errors found</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateJson(string jsonString, JsonField schema, out List<string> errors)
    {
        errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            errors.Add("JSON string is null or empty");
            return false;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(jsonString);
            return ValidateElement(document.RootElement, schema, "", errors);
        }
        catch (System.Text.Json.JsonException ex)
        {
            errors.Add($"Invalid JSON format: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Simple validation that returns only true/false
    /// </summary>
    public static bool ValidateJson(string jsonString, JsonField schema)
    {
        return ValidateJson(jsonString, schema, out _);
    }

    private static bool ValidateElement(System.Text.Json.JsonElement element, JsonField field, string path, List<string> errors)
    {
        var currentPath = string.IsNullOrEmpty(path) ? field.Name : $"{path}.{field.Name}";
        
        try
        {
            return field switch
            {
                DigitField digitField => ValidateDigitField(element, digitField, currentPath, errors),
                ConstantIntField constIntField => ValidateConstantIntField(element, constIntField, currentPath, errors),
                ConstantFloatField constFloatField => ValidateConstantFloatField(element, constFloatField, currentPath, errors),
                StringField stringField => ValidateStringField(element, stringField, currentPath, errors),
                BooleanField boolField => ValidateBooleanField(element, boolField, currentPath, errors),
                ChoiceField<string> stringChoice => ValidateStringChoiceField(element, stringChoice, currentPath, errors),
                ChoiceField<int> intChoice => ValidateIntChoiceField(element, intChoice, currentPath, errors),
                TemplateStringField templateField => ValidateTemplateStringField(element, templateField, currentPath, errors),
                ArrayField arrayField => ValidateArrayField(element, arrayField, currentPath, errors),
                CompositeField compositeField => ValidateCompositeField(element, compositeField, currentPath, errors),
                VariantField variantField => ValidateVariantField(element, variantField, currentPath, errors),
                OptionalField optionalField => ValidateOptionalField(element, optionalField, currentPath, errors),
                _ => AddError(errors, currentPath, $"Unsupported field type: {field.GetType().Name}")
            };
        }
        catch (Exception ex)
        {
            AddError(errors, currentPath, $"Validation exception: {ex.Message}");
            return false;
        }
    }

    private static bool ValidateDigitField(System.Text.Json.JsonElement element, DigitField field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            AddError(errors, path, $"Expected string, got {element.ValueKind}");
            return false;
        }

        var value = element.GetString()!;
        
        // Check if it has the exact expected number of digits
        if (value.Length != field.DigitCount)
        {
            AddError(errors, path, $"Expected {field.DigitCount}-digit string, got {value.Length} characters: {value}");
            return false;
        }

        // Validate all characters are digits
        if (!value.All(char.IsDigit))
        {
            AddError(errors, path, $"Expected all digits, got non-digit characters in: {value}");
            return false;
        }

        return true;
    }

    private static bool ValidateConstantIntField(System.Text.Json.JsonElement element, ConstantIntField field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Number)
        {
            AddError(errors, path, $"Expected integer, got {element.ValueKind}");
            return false;
        }

        if (!element.TryGetInt32(out var value))
        {
            AddError(errors, path, "Value is not a valid 32-bit integer");
            return false;
        }

        if (value != field.Value)
        {
            AddError(errors, path, $"Expected constant value {field.Value}, got {value}");
            return false;
        }

        return true;
    }

    private static bool ValidateConstantFloatField(System.Text.Json.JsonElement element, ConstantFloatField field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Number)
        {
            AddError(errors, path, $"Expected number, got {element.ValueKind}");
            return false;
        }

        if (!element.TryGetDouble(out var value))
        {
            AddError(errors, path, "Value is not a valid number");
            return false;
        }

        if (Math.Abs(value - field.Value) > 0.0001) // Use epsilon for float comparison
        {
            AddError(errors, path, $"Expected constant value {field.Value}, got {value}");
            return false;
        }

        return true;
    }

    private static bool ValidateStringField(System.Text.Json.JsonElement element, StringField field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            AddError(errors, path, $"Expected string, got {element.ValueKind}");
            return false;
        }

        var value = element.GetString() ?? "";
        if (value.Length < field.MinLength || value.Length > field.MaxLength)
        {
            AddError(errors, path, $"String length {value.Length} is outside range [{field.MinLength}, {field.MaxLength}]");
            return false;
        }

        return true;
    }

    private static bool ValidateBooleanField(System.Text.Json.JsonElement element, BooleanField field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.True && 
            element.ValueKind != System.Text.Json.JsonValueKind.False)
        {
            AddError(errors, path, $"Expected boolean, got {element.ValueKind}");
            return false;
        }

        return true;
    }

    private static bool ValidateStringChoiceField(System.Text.Json.JsonElement element, ChoiceField<string> field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            AddError(errors, path, $"Expected string choice, got {element.ValueKind}");
            return false;
        }

        var value = element.GetString();
        if (!field.Options.Contains(value))
        {
            AddError(errors, path, $"Value '{value}' is not in allowed choices: [{string.Join(", ", field.Options)}]");
            return false;
        }

        return true;
    }

    private static bool ValidateIntChoiceField(System.Text.Json.JsonElement element, ChoiceField<int> field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Number)
        {
            AddError(errors, path, $"Expected integer choice, got {element.ValueKind}");
            return false;
        }

        if (!element.TryGetInt32(out var value))
        {
            AddError(errors, path, "Value is not a valid integer");
            return false;
        }

        if (!field.Options.Contains(value))
        {
            AddError(errors, path, $"Value {value} is not in allowed choices: [{string.Join(", ", field.Options)}]");
            return false;
        }

        return true;
    }

    private static bool ValidateTemplateStringField(System.Text.Json.JsonElement element, TemplateStringField field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            AddError(errors, path, $"Expected string, got {element.ValueKind}");
            return false;
        }

        var value = element.GetString() ?? "";
        
        // For template strings, we validate that it follows the template pattern
        if (field.Template.Contains("<generated>"))
        {
            var beforeMarker = field.Template.Substring(0, field.Template.IndexOf("<generated>"));
            var afterMarker = field.Template.Substring(field.Template.IndexOf("<generated>") + "<generated>".Length);
            
            if (!value.StartsWith(beforeMarker) || !value.EndsWith(afterMarker))
            {
                AddError(errors, path, $"Template string does not match pattern. Expected format: '{field.Template.Replace("<generated>", "[generated text]")}'");
                return false;
            }
            
            var generatedPart = value.Substring(beforeMarker.Length, value.Length - beforeMarker.Length - afterMarker.Length);
            if (generatedPart.Length < field.MinGenLength || generatedPart.Length > field.MaxGenLength)
            {
                AddError(errors, path, $"Generated portion length {generatedPart.Length} is outside range [{field.MinGenLength}, {field.MaxGenLength}]");
                return false;
            }
        }
        else
        {
            // Exact match for non-template strings
            if (value != field.Template)
            {
                AddError(errors, path, $"Expected exact value '{field.Template}', got '{value}'");
                return false;
            }
        }

        return true;
    }

    private static bool ValidateArrayField(System.Text.Json.JsonElement element, ArrayField field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            AddError(errors, path, $"Expected array, got {element.ValueKind}");
            return false;
        }

        var length = element.GetArrayLength();
        if (length < field.MinLength || length > field.MaxLength)
        {
            AddError(errors, path, $"Array length {length} is outside range [{field.MinLength}, {field.MaxLength}]");
            return false;
        }

        var isValid = true;
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var itemPath = $"{path}[{index}]";
            if (!ValidateElement(item, field.ElementType, itemPath.Substring(0, itemPath.LastIndexOf('.')), errors))
            {
                isValid = false;
            }
            index++;
        }

        return isValid;
    }

    private static bool ValidateCompositeField(System.Text.Json.JsonElement element, CompositeField field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            AddError(errors, path, $"Expected object, got {element.ValueKind}");
            return false;
        }

        var isValid = true;

        // Check that all required fields are present
        foreach (var childField in field.Fields)
        {
            if (element.TryGetProperty(childField.Name, out var childElement))
            {
                if (!ValidateElement(childElement, childField, path, errors))
                {
                    isValid = false;
                }
            }
            else if (!(childField is OptionalField))
            {
                AddError(errors, $"{path}.{childField.Name}", "Required field is missing");
                isValid = false;
            }
        }

        return isValid;
    }

    private static bool ValidateVariantField(System.Text.Json.JsonElement element, VariantField field, string path, List<string> errors)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            AddError(errors, path, $"Expected object for variant, got {element.ValueKind}");
            return false;
        }

        // Try to match against any of the variants
        var variantErrors = new List<List<string>>();
        
        foreach (var variant in field.Variants)
        {
            var variantSpecificErrors = new List<string>();
            if (ValidateElement(element, variant, path, variantSpecificErrors))
            {
                return true; // Found a matching variant
            }
            variantErrors.Add(variantSpecificErrors);
        }

        // If no variant matched, add errors for all variants
        AddError(errors, path, $"Value does not match any of the {field.Variants.Length} possible variants:");
        for (int i = 0; i < field.Variants.Length; i++)
        {
            errors.Add($"  Variant {i + 1} ({field.Variants[i].Name}) errors:");
            foreach (var error in variantErrors[i])
            {
                errors.Add($"    {error}");
            }
        }

        return false;
    }

    private static bool ValidateOptionalField(System.Text.Json.JsonElement element, OptionalField field, string path, List<string> errors)
    {
        // For optional fields, we validate the inner field if the element exists
        return ValidateElement(element, field.InnerField, path, errors);
    }

    private static bool AddError(List<string> errors, string path, string message)
    {
        errors.Add($"{path}: {message}");
        return false;
    }
}