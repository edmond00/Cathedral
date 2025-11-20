using System.Text;
using System.Text.Json;
using System.Linq;
using Cathedral.Glyph.Microworld.LocationSystem;

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
        
        // Add all the generated rules, but deduplicate by rule name
        var rulesByName = new Dictionary<string, string>();
        foreach (var rule in processedRules)
        {
            var parts = rule.Split(new[] { " ::= " }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                var ruleName = parts[0];
                if (!rulesByName.ContainsKey(ruleName))
                {
                    rulesByName[ruleName] = rule;
                }
            }
        }
        
        foreach (var rule in rulesByName.Values.OrderBy(r => r))
        {
            builder.AppendLine(rule);
        }
        
        // Add basic JSON primitives
        AppendBasicJsonRules(builder, processedRules);
        
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
            DigitField digitField => GenerateDigitFieldRule(digitField, processedRules),
            ConstantIntField constIntField => GenerateConstantIntFieldRule(constIntField, processedRules),
            ConstantFloatField constFloatField => GenerateConstantFloatFieldRule(constFloatField, processedRules),
            ConstantStringField constStringField => GenerateConstantStringFieldRule(constStringField, processedRules),
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
    
    private static string GenerateDigitFieldRule(DigitField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        
        // Generate GBNF rule for exactly N digits as a quoted string (to allow leading zeros)
        var digitPattern = string.Join(" ", Enumerable.Repeat("[0-9]", field.DigitCount));
        var rule = $"{ruleName} ::= \"\\\"\" {digitPattern} \"\\\"\"";
        
        processedRules.Add(rule);
        
        return ruleName;
    }

    private static string GenerateConstantIntFieldRule(ConstantIntField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var rule = $"{ruleName} ::= \"{field.Value}\"";
        
        // Constant fields generate a literal numeric value with quotes in GBNF rule
        processedRules.Add(rule);
        
        return ruleName;
    }

    private static string GenerateConstantFloatFieldRule(ConstantFloatField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var rule = $"{ruleName} ::= \"{field.Value}\"";
        
        // Constant fields generate a literal numeric value with quotes in GBNF rule
        processedRules.Add(rule);
        
        return ruleName;
    }

    private static string GenerateConstantStringFieldRule(ConstantStringField field, HashSet<string> processedRules)
    {
        var ruleName = SanitizeRuleName(field.Name);
        var escapedValue = field.Value.Replace("\"", "\\\""); // Escape quotes in the string value
        var rule = $"{ruleName} ::= \"\\\"{escapedValue}\\\"\"";
        
        // Constant string fields generate a literal quoted string in GBNF rule
        processedRules.Add(rule);
        
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
        var choices = field.Options.Select(opt => $"\"{opt}\"");
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
        rule.Append(ruleName + " ::= \"[\" ");
        
        if (field.MinLength == 0)
        {
            // For arrays that can be empty, allow either empty array or array starting with element
            rule.Append("(");
            if (field.MaxLength > 0)
            {
                // First element (no leading comma)
                rule.Append(elementRuleName);
                
                // Additional optional elements (with leading comma)
                for (int i = 1; i < field.MaxLength; i++)
                {
                    rule.Append(" (\",\" " + elementRuleName + ")?");
                }
            }
            rule.Append(")?");
        }
        else
        {
            // For arrays with required elements, start with required elements
            for (int i = 0; i < field.MinLength; i++)
            {
                if (i > 0) rule.Append(" \",\" ");
                rule.Append(elementRuleName);
            }
            
            // Add optional elements beyond the minimum
            if (field.MaxLength > field.MinLength)
            {
                var optionalCount = field.MaxLength - field.MinLength;
                for (int i = 0; i < optionalCount; i++)
                {
                    rule.Append(" (\",\" " + elementRuleName + ")?");
                }
            }
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
            DigitField digitField => $"<{digitField.DigitCount}-digit string>",
            ConstantIntField constIntField => constIntField.Value,
            ConstantFloatField constFloatField => constFloatField.Value,
            ConstantStringField constStringField => constStringField.Value,
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
    
    private static string SanitizeRuleName(string name)
    {
        // Replace invalid characters with hyphens (GBNF standard)
        return new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
    }
    
    private static void AppendBasicJsonRules(StringBuilder builder, HashSet<string> processedRules)
    {
        builder.AppendLine();
        builder.AppendLine("# Basic JSON primitives");
        
        // Only add basic rules that haven't been added by field-specific generation
        if (!processedRules.Any(r => r.StartsWith("ws ::=")))
            builder.AppendLine("ws ::= [ \\t\\n\\r]*");
        if (!processedRules.Any(r => r.StartsWith("string ::=")))
            builder.AppendLine("string ::= \"\\\"\" [^\"\\\\]* \"\\\"\"");
        if (!processedRules.Any(r => r.StartsWith("number ::=")))
            builder.AppendLine("number ::= [\"-\"]? [0-9]+ (\".\" [0-9]+)?");
        if (!processedRules.Any(r => r.StartsWith("integer ::=")))
            builder.AppendLine("integer ::= [\"-\"]? [0-9]+");
        if (!processedRules.Any(r => r.StartsWith("boolean ::=")))
            builder.AppendLine("boolean ::= \"true\" | \"false\"");
        if (!processedRules.Any(r => r.StartsWith("null ::=")))
            builder.AppendLine("null ::= \"null\"");
    }
}