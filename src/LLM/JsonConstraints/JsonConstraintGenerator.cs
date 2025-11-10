using System.Text;
using System.Text.Json;

namespace Cathedral.LLM.JsonConstraints;

/// <summary>
/// Static class for generating GBNF grammars and JSON templates from JsonField definitions
/// </summary>
public static class JsonConstraintGenerator
{
    /// <summary>
    /// Generates a GBNF grammar string from a JsonField structure
    /// </summary>
    /// <param name="root">The root field definition</param>
    /// <returns>A GBNF grammar string</returns>
    public static string GenerateGBNF(JsonField root)
    {
        var builder = new StringBuilder();
        var processedRules = new HashSet<string>();
        
        // Start with the root rule
        builder.AppendLine("root ::= " + GenerateFieldRule(root, processedRules));
        builder.AppendLine();
        
        // Add all the generated rules
        foreach (var rule in processedRules.OrderBy(r => r))
        {
            builder.AppendLine(rule);
        }
        
        // Add basic JSON primitives
        AppendBasicJsonRules(builder);
        
        return builder.ToString();
    }
    
    /// <summary>
    /// Generates a JSON template string from a JsonField structure
    /// </summary>
    /// <param name="root">The root field definition</param>
    /// <returns>A JSON template string with descriptive placeholders</returns>
    public static string GenerateTemplate(JsonField root)
    {
        var templateObject = GenerateTemplateObject(root);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(templateObject, options);
        
        // Clean up Unicode escapes and other artifacts
        json = CleanJsonTemplate(json);
        
        return json;
    }

    private static string CleanJsonTemplate(string json)
    {
        return json.Replace("\\u0022", "\"")     // quotes
                   .Replace("\\u002D", "-")      // hyphen
                   .Replace("\\u2013", "–")      // en dash
                   .Replace("\\u201C", "\"")     // left double quote  
                   .Replace("\\u201D", "\"")     // right double quote
                   .Replace("\\u003C", "<")      // less than
                   .Replace("\\u003E", ">")      // greater than
                   .Replace("\\n", "\n")         // actual newlines
                   .Replace("\\\\", "\\")        // double backslashes
                   .Replace("\\\"", "\"")        // escaped quotes
                   .Replace("\\–", "–");         // escaped en dash
    }
    
    private static string GenerateFieldRule(JsonField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        
        return field switch
        {
            IntField intField => GenerateIntFieldRule(intField, processedRules),
            FloatField floatField => GenerateFloatFieldRule(floatField, processedRules),
            StringField stringField => GenerateStringFieldRule(stringField, processedRules),
            BooleanField boolField => GenerateBooleanFieldRule(boolField, processedRules),
            ChoiceField<string> stringChoice => GenerateStringChoiceFieldRule(stringChoice, processedRules),
            ChoiceField<int> intChoice => GenerateIntChoiceFieldRule(intChoice, processedRules),
            TemplateStringField templateField => GenerateTemplateStringFieldRule(templateField, processedRules),
            ArrayField arrayField => GenerateArrayFieldRule(arrayField, processedRules),
            CompositeField compositeField => GenerateCompositeFieldRule(compositeField, processedRules),
            VariantField variantField => GenerateVariantFieldRule(variantField, processedRules),
            OptionalField optionalField => GenerateOptionalFieldRule(optionalField, processedRules),
            _ => throw new ArgumentException($"Unsupported field type: {field.GetType()}")
        };
    }
    
    private static string GenerateIntFieldRule(IntField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var rule = $"{ruleName} ::= integer-{field.Min}-{field.Max}";
        
        // Generate the specific integer range rule
        var rangeRuleName = $"integer-{field.Min}-{field.Max}";
        if (!processedRules.Any(r => r.StartsWith(rangeRuleName)))
        {
            processedRules.Add($"{rangeRuleName} ::= {GenerateIntegerRange(field.Min, field.Max)}");
        }
        
        return ruleName;
    }
    
    private static string GenerateFloatFieldRule(FloatField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var rule = $"{ruleName} ::= float-{field.Min}-{field.Max}";
        
        // For simplicity, we'll use a general float pattern and rely on the LLM to respect bounds
        var rangeRuleName = $"float-{field.Min}-{field.Max}";
        if (!processedRules.Any(r => r.StartsWith(rangeRuleName)))
        {
            processedRules.Add($"{rangeRuleName} ::= [\"-\"]? [0-9]+ \".\" [0-9]+");
        }
        
        return ruleName;
    }
    
    private static string GenerateStringFieldRule(StringField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var stringRuleName = $"string-{field.MinLength}-{field.MaxLength}";
        
        if (!processedRules.Any(r => r.StartsWith(stringRuleName)))
        {
            var charPattern = field.MinLength == field.MaxLength 
                ? $"[a-zA-Z0-9 _]{{{field.MinLength}}}"
                : $"[a-zA-Z0-9 _]{{{field.MinLength},{field.MaxLength}}}";
            processedRules.Add(stringRuleName + " ::= \"\\\"\" " + charPattern + " \"\\\"\"");
        }
        
        return stringRuleName;
    }
    
    private static string GenerateBooleanFieldRule(BooleanField field, HashSet<string> processedRules)
    {
        if (!processedRules.Any(r => r.StartsWith("boolean")))
        {
            processedRules.Add("boolean ::= \"true\" | \"false\"");
        }
        
        return "boolean";
    }
    
    private static string GenerateStringChoiceFieldRule(ChoiceField<string> field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var choices = field.Options.Select(opt => "\"\\\"" + opt + "\\\"\"");
        var rule = $"{ruleName} ::= {string.Join(" | ", choices)}";
        processedRules.Add(rule);
        
        return ruleName;
    }
    
    private static string GenerateIntChoiceFieldRule(ChoiceField<int> field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var choices = field.Options.Select(opt => opt.ToString());
        var rule = $"{ruleName} ::= {string.Join(" | ", choices)}";
        processedRules.Add(rule);
        
        return ruleName;
    }
    
    private static string GenerateTemplateStringFieldRule(TemplateStringField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        // For template strings, we need to identify the <generated> placeholder
        var template = field.Template;
        var generatedMarker = "<generated>";
        
        if (template.Contains(generatedMarker))
        {
            var before = template.Substring(0, template.IndexOf(generatedMarker));
            var after = template.Substring(template.IndexOf(generatedMarker) + generatedMarker.Length);
            
            var charPattern = field.MinGenLength == field.MaxGenLength
                ? $"[a-zA-Z0-9 _]{{{field.MinGenLength}}}"
                : $"[a-zA-Z0-9 _]{{{field.MinGenLength},{field.MaxGenLength}}}";
            
            var rule = ruleName + " ::= \"\\\"" + before + "\" " + charPattern + " \"" + after + "\\\"\"";
            processedRules.Add(rule);
        }
        else
        {
            // Fallback to regular string if no placeholder found
            var rule = ruleName + " ::= \"\\\"" + template + "\\\"\"";
            processedRules.Add(rule);
        }
        
        return ruleName;
    }
    
    private static string GenerateArrayFieldRule(ArrayField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var elementRuleName = GenerateFieldRule(field.ElementType, processedRules);
        
        var rule = new StringBuilder();
        rule.Append(ruleName + " ::= \"[\"");
        
        if (field.MinLength == 0)
        {
            rule.Append(" (");
        }
        else
        {
            rule.Append(" ");
        }
        
        // Generate pattern for required elements
        for (int i = 0; i < field.MinLength; i++)
        {
            if (i > 0) rule.Append(" \",\" ");
            rule.Append(elementRuleName);
        }
        
        // Generate pattern for optional elements
        if (field.MaxLength > field.MinLength)
        {
            var optionalCount = field.MaxLength - field.MinLength;
            for (int i = 0; i < optionalCount; i++)
            {
                rule.Append(" (\",\" " + elementRuleName + ")?");
            }
        }
        
        if (field.MinLength == 0)
        {
            rule.Append(")?");
        }
        
        rule.Append(" \"]\"");
        
        processedRules.Add(rule.ToString());
        return ruleName;
    }
    
    private static string GenerateCompositeFieldRule(CompositeField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var rule = new StringBuilder();
        rule.Append(ruleName + " ::= \"{\"");
        
        for (int i = 0; i < field.Fields.Length; i++)
        {
            var childField = field.Fields[i];
            var childRuleName = GenerateFieldRule(childField, processedRules);
            
            if (i > 0) rule.Append(" \",\"");
            rule.Append(" \"\\\"" + childField.Name + "\\\":\" " + childRuleName);
        }
        
        rule.Append(" \"}\"");
        processedRules.Add(rule.ToString());
        
        return ruleName;
    }
    
    private static string GenerateVariantFieldRule(VariantField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var variants = field.Variants.Select(variant => GenerateFieldRule(variant, processedRules));
        var rule = $"{ruleName} ::= {string.Join(" | ", variants)}";
        processedRules.Add(rule);
        
        return ruleName;
    }
    
    private static string GenerateOptionalFieldRule(OptionalField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var innerRuleName = GenerateFieldRule(field.InnerField, processedRules);
        var rule = $"{ruleName} ::= {innerRuleName}?";
        processedRules.Add(rule);
        
        return ruleName;
    }
    
    private static object GenerateTemplateObject(JsonField field)
    {
        return field switch
        {
            IntField intField => $"<integer between {intField.Min}–{intField.Max}>",
            FloatField floatField => $"<float between {floatField.Min}–{floatField.Max}>",
            StringField stringField => $"<string of {stringField.MinLength}–{stringField.MaxLength} characters>",
            BooleanField => "<boolean: true or false>",
            ChoiceField<string> stringChoice => $"<choice between {JsonSerializer.Serialize(stringChoice.Options)}>",
            ChoiceField<int> intChoice => $"<choice between {JsonSerializer.Serialize(intChoice.Options)}>",
            TemplateStringField templateField => GenerateTemplateStringPlaceholder(templateField),
            ArrayField arrayField => GenerateArrayTemplatePlaceholder(arrayField),
            CompositeField compositeField => GenerateCompositeTemplatePlaceholder(compositeField),
            VariantField variantField => GenerateVariantTemplatePlaceholder(variantField),
            OptionalField optionalField => $"<optional: {GenerateTemplateObject(optionalField.InnerField)}>",
            _ => $"<unsupported field type: {field.GetType().Name}>"
        };
    }
    
    private static string GenerateTemplateStringPlaceholder(TemplateStringField field)
    {
        var generatedMarker = "<generated>";
        if (field.Template.Contains(generatedMarker))
        {
            return field.Template.Replace(generatedMarker, 
                $"<text of {field.MinGenLength}–{field.MaxGenLength} characters>");
        }
        return field.Template;
    }
    
    private static string GenerateArrayTemplatePlaceholder(ArrayField field)
    {
        var elementPlaceholder = GenerateTemplateObject(field.ElementType);
        return $"<array of {field.MinLength}–{field.MaxLength} elements, each element should be: {FormatPlaceholder(elementPlaceholder)}>";
    }
    
    private static Dictionary<string, object> GenerateCompositeTemplatePlaceholder(CompositeField field)
    {
        var result = new Dictionary<string, object>();
        foreach (var childField in field.Fields)
        {
            result[childField.Name] = GenerateTemplateObject(childField);
        }
        return result;
    }
    
    private static string GenerateVariantTemplatePlaceholder(VariantField field)
    {
        var variants = field.Variants.Select(v => 
        {
            var composite = GenerateCompositeTemplatePlaceholder(v);
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var cleanJson = JsonSerializer.Serialize(composite, options);
            return CleanJsonTemplate(cleanJson);
        });
        return $"<choose one of the following structures>\n{string.Join("\nOR\n", variants)}";
    }

    private static string FormatPlaceholder(object placeholder)
    {
        if (placeholder is string str)
        {
            return str;
        }
        else if (placeholder is Dictionary<string, object> dict)
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(dict, options);
            return CleanJsonTemplate(json);
        }
        else
        {
            return placeholder?.ToString() ?? "<unknown>";
        }
    }
    
    private static string GenerateIntegerRange(int min, int max)
    {
        // For simplicity, use a basic integer pattern
        // In a full implementation, you'd want more sophisticated range handling
        if (min >= 0)
        {
            return "[0-9]+";
        }
        else
        {
            return "[\"-\"]? [0-9]+";
        }
    }
    
    private static string SanitizeRuleName(string name)
    {
        // Replace invalid characters with underscores
        return new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
    }
    
    private static void AppendBasicJsonRules(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("# Basic JSON primitives");
        builder.AppendLine("ws ::= [ \\t\\n\\r]*");
        builder.AppendLine("string ::= \"\\\"\" [^\"\\\\]* \"\\\"\"");
        builder.AppendLine("number ::= [\"-\"]? [0-9]+ (\".\" [0-9]+)?");
        builder.AppendLine("integer ::= [\"-\"]? [0-9]+");
        builder.AppendLine("boolean ::= \"true\" | \"false\"");
        builder.AppendLine("null ::= \"null\"");
    }
}