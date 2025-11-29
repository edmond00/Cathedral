namespace Cathedral.LLM.JsonConstraints;

/// <summary>
/// Base record for all JSON field definitions in the constraint system
/// </summary>
public abstract record JsonField(string Name, string? Hint = null)
{
    public string Name { get; init; } = Name ?? throw new ArgumentNullException(nameof(Name));
    public string? Hint { get; init; } = Hint;
    
    /// <summary>
    /// Optional custom rule name for GBNF generation.
    /// If null, uses Name. Allows multiple fields with same JSON name but different GBNF rules.
    /// </summary>
    public string? RuleName { get; init; } = null;
}

/// <summary>
/// Represents a numeric field with a fixed number of digits (0-padded)
/// </summary>
public record DigitField(string Name, int DigitCount = 1, string? Hint = null) : JsonField(Name, Hint)
{
    public int DigitCount { get; init; } = DigitCount > 0 ? DigitCount : throw new ArgumentException("DigitCount must be greater than 0");
}

/// <summary>
/// Represents a constant integer field with a fixed value
/// </summary>
public record ConstantIntField(string Name, int Value, string? Hint = null) : JsonField(Name, Hint)
{
    public int Value { get; init; } = Value;
}

/// <summary>
/// Represents a constant floating-point field with a fixed value
/// </summary>
public record ConstantFloatField(string Name, double Value, string? Hint = null) : JsonField(Name, Hint)
{
    public double Value { get; init; } = Value;
}

/// <summary>
/// Represents a string field with length constraints
/// </summary>
public record StringField(string Name, int MinLength, int MaxLength, string? Hint = null) : JsonField(Name, Hint)
{
    public int MinLength { get; init; } = MinLength >= 0 ? MinLength : throw new ArgumentException("MinLength cannot be negative");
    public int MaxLength { get; init; } = MaxLength >= MinLength ? MaxLength : throw new ArgumentException("MinLength cannot be greater than MaxLength");
}

/// <summary>
/// Represents a field that must be one of a predefined set of values
/// </summary>
public record ChoiceField<T>(string Name, T[] Options, string? Hint = null) : JsonField(Name, Hint)
{
    public T[] Options { get; init; } = Options?.Length > 0 ? Options : throw new ArgumentException("Options cannot be null or empty");
    
    // Convenience constructor that accepts params array
    public ChoiceField(string Name, params T[] Options) : this(Name, Options, null) { }
}

/// <summary>
/// Represents a composite object with nested fields
/// </summary>
public record CompositeField(string Name, JsonField[] Fields, string? Hint = null) : JsonField(Name, Hint)
{
    public JsonField[] Fields { get; init; } = Fields ?? throw new ArgumentNullException(nameof(Fields));
    
    // Convenience constructor that accepts params array
    public CompositeField(string Name, params JsonField[] Fields) : this(Name, Fields, null) { }
}

/// <summary>
/// Represents a field that can be one of several different composite structures
/// </summary>
public record VariantField(string Name, CompositeField[] Variants, string? Hint = null) : JsonField(Name, Hint)
{
    public CompositeField[] Variants { get; init; } = Variants?.Length > 0 ? Variants : throw new ArgumentException("Variants cannot be null or empty");
    
    // Convenience constructor that accepts params array
    public VariantField(string Name, params CompositeField[] Variants) : this(Name, Variants, null) { }
}

/// <summary>
/// Represents a string field with a template and variable generation length
/// </summary>
public record TemplateStringField(string Name, string Template, int MinGenLength, int MaxGenLength, string? Hint = null) : JsonField(Name, Hint)
{
    public string Template { get; init; } = !string.IsNullOrEmpty(Template) ? Template : throw new ArgumentException("Template cannot be null or empty");
    public int MinGenLength { get; init; } = MinGenLength >= 0 ? MinGenLength : throw new ArgumentException("MinGenLength cannot be negative");
    public int MaxGenLength { get; init; } = MaxGenLength >= MinGenLength ? MaxGenLength : throw new ArgumentException("MinGenLength cannot be greater than MaxGenLength");
}

/// <summary>
/// Represents an array field with element constraints
/// </summary>
public record ArrayField(string Name, JsonField ElementType, int MinLength, int MaxLength, string? Hint = null) : JsonField(Name, Hint)
{
    public JsonField ElementType { get; init; } = ElementType ?? throw new ArgumentNullException(nameof(ElementType));
    public int MinLength { get; init; } = MinLength >= 0 ? MinLength : throw new ArgumentException("MinLength cannot be negative");
    public int MaxLength { get; init; } = MaxLength >= MinLength ? MaxLength : throw new ArgumentException("MinLength cannot be greater than MaxLength");
}

/// <summary>
/// Represents a boolean field
/// </summary>
public record BooleanField(string Name, string? Hint = null) : JsonField(Name, Hint);

/// <summary>
/// Represents an optional field that may or may not be present
/// </summary>
public record OptionalField(string Name, JsonField InnerField, string? Hint = null) : JsonField(Name, Hint)
{
    public JsonField InnerField { get; init; } = InnerField ?? throw new ArgumentNullException(nameof(InnerField));
}

/// <summary>
/// Represents a fixed-length array where each position has a specific type (heterogeneous tuple)
/// Used when each array element must have different constraints (e.g., different skills per action)
/// </summary>
public record TupleField(string Name, JsonField[] Elements, string? Hint = null) : JsonField(Name, Hint)
{
    public JsonField[] Elements { get; init; } = Elements?.Length > 0 ? Elements : throw new ArgumentException("Elements cannot be null or empty");
    
    // Convenience constructor that accepts params array
    public TupleField(string Name, params JsonField[] Elements) : this(Name, Elements, null) { }
}

/// <summary>
/// Represents a constant string field that generates its value inline in the parent rule
/// rather than creating a separate reusable rule. Use when the same field name needs different values.
/// </summary>
public record InlineConstantStringField(string Name, string Value, string? Hint = null) : JsonField(Name, Hint)
{
    public string Value { get; init; } = Value ?? throw new ArgumentNullException(nameof(Value));
}