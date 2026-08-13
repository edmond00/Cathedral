# LLM Controlled JSON Output Framework (Design Document)

## Overview

This system enables the precise control of LLM-generated outputs for a game or simulation.  
It defines the structure and constraints of JSON outputs in C# using a **nested data definition model**,  
then automatically generates:

1. A **GBNF grammar file** enforcing structural and syntactic constraints for the LLM.
2. A **template JSON prompt** showing the expected output format, including annotated placeholders  
   describing what the LLM should generate.

This ensures that the LLM always produces valid and game-usable JSON outputs.
This code should be written into the `src/LLM` directory.

---

## High-Level Architecture

```
[C# Schema Definition]  
      ↓  
[Generator Module]  
      ↓ ↓  
  [GBNF Grammar]   [JSON Template Prompt]
```

### Key Components

- **`FieldDefinition` hierarchy**  
  A set of C# classes/records describing each field's type, constraints, and generation hints.

- **`JsonStructureDefinition`**  
  A root object defining the full JSON structure using nested fields.

- **`JsonConstraintGenerator`**  
  A static class that:
  - Traverses the C# structure.
  - Generates the GBNF file.
  - Generates the dummy JSON template.

---

## Core Concepts

### 1. Data Definition System

Each field in the JSON output is represented as an object inheriting from a common abstract type.

#### Base Type

```csharp
abstract record JsonField
{
    public string Name { get; init; }
}
```

#### Example Field Types

```csharp
record IntField(string Name, int Min, int Max) : JsonField;
record FloatField(string Name, double Min, double Max) : JsonField;
record StringField(string Name, int MinLength, int MaxLength) : JsonField;
record ChoiceField<T>(string Name, params T[] Options) : JsonField;
record CompositeField(string Name, params JsonField[] Fields) : JsonField;
record VariantField(string Name, params CompositeField[] Variants) : JsonField;
record TemplateStringField(string Name, string Template, int MinGenLength, int MaxGenLength) : JsonField;
```

This structure allows flexible composition of nested and branching JSON models.

---

### 2. GBNF Generation

The generator should:
- Create GBNF rules matching the structure of the `JsonField` tree.
- Define tokens and productions for each field type.
- Respect min/max lengths and enumerated choices.

#### Example

Given:
```csharp
var schema = new CompositeField("Character", new JsonField[]
{
    new StringField("name", 3, 20),
    new IntField("age", 0, 120),
    new ChoiceField<string>("class", "warrior", "mage", "rogue"),
    new CompositeField("stats", new JsonField[]
    {
        new IntField("strength", 1, 10),
        new IntField("dexterity", 1, 10),
        new IntField("intelligence", 1, 10),
    }),
});
```

Expected GBNF output (simplified):
```
root ::= object
object ::= "{" pair ("," pair)* "}"
pair ::= string ":" value

value ::= string | number | object
string ::= "\"" [a-zA-Z0-9 _]{3,20} "\""
number ::= int | float

Character ::= "{"
  "\"name\":" name ","
  "\"age\":" age ","
  "\"class\":" class ","
  "\"stats\":" stats
"}"

name ::= "\"" [a-zA-Z]{3,20} "\""
age ::= integer(0,120)
class ::= "\"warrior\"" | "\"mage\"" | "\"rogue\""
stats ::= "{" "\"strength\":" int(1,10) "," "\"dexterity\":" int(1,10) "," "\"intelligence\":" int(1,10) "}"
```

---

### 3. Dummy JSON Template Generation

The second output is a "template" JSON for the LLM prompt.

Given the same schema, the dummy template should look like:

```json
{
  "name": "<string of 3–20 characters>",
  "age": "<integer between 0–120>",
  "class": "<choice between [\"warrior\", \"mage\", \"rogue\"]>",
  "stats": {
    "strength": "<integer between 1–10>",
    "dexterity": "<integer between 1–10>",
    "intelligence": "<integer between 1–10>"
  }
}
```

If a `TemplateStringField` is used, for example:

```csharp
new TemplateStringField("description", "this is <generated>", 10, 100)
```

Then the JSON would include:
```json
"description": "this is <text of 10–100 characters>"
```

---

### 4. Branching and Variants

For situations where different structures may be chosen (e.g., event types):

```csharp
var eventSchema = new VariantField("event",
    new CompositeField("battle", new JsonField[] {
        new IntField("enemy_level", 1, 100),
        new StringField("terrain", 3, 15)
    }),
    new CompositeField("dialogue", new JsonField[] {
        new StringField("speaker", 3, 20),
        new StringField("text", 5, 200)
    })
);
```

Generated dummy JSON:
```json
"event": "<choose one of the following structures>
{
  \"battle\": { \"enemy_level\": <1–100>, \"terrain\": <string of 3–15 characters> }
}
OR
{
  \"dialogue\": { \"speaker\": <3–20 chars>, \"text\": <5–200 chars> }
}"
```

---

## Implementation Plan

1. **Define Field Classes**
   - Implement all field types (`IntField`, `ChoiceField`, etc.).
   - Support recursive nesting via `CompositeField` and `VariantField`.

2. **Implement `JsonConstraintGenerator`**
   - Traverse the field tree recursively.
   - Generate:
     - GBNF rules for each field type.
     - Corresponding JSON template entries.

3. **Add Exporters**
   - `string GenerateGBNF(JsonField root)`
   - `string GenerateTemplate(JsonField root)`

4. **Optional Utilities**
   - Validator to check that LLM JSON output matches schema.
   - Serializer/deserializer for saving schema definitions.

---

## Example Usage

```csharp
var schema = new CompositeField("SceneEvent", new JsonField[]
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
        })
    )
});

var gbnf = JsonConstraintGenerator.GenerateGBNF(schema);
var template = JsonConstraintGenerator.GenerateTemplate(schema);

File.WriteAllText("scene_event.gbnf", gbnf);
File.WriteAllText("scene_event_template.json", template);
```

---

## Expected Outputs

### GBNF (simplified)
```
SceneEvent ::= "{" "\"type\":" type "," "\"content\":" content "}"
type ::= "\"combat\"" | "\"dialogue\"" | "\"exploration\""
content ::= combat | dialogue
combat ::= "{" "\"enemyLevel\":" int(1,100) "," "\"enemyType\":" enemyType "}"
dialogue ::= "{" "\"speaker\":" string(3,20) "," "\"text\":" template_text(10,100) "}"
```

### JSON Template
```json
{
  "type": "<choice between [\"combat\", \"dialogue\", \"exploration\"]>",
  "content": "<choose one of the following structures>
{
  \"combat\": { \"enemyLevel\": <1–100>, \"enemyType\": <choice between [\"goblin\", \"dragon\", \"bandit\"]> }
}
OR
{
  \"dialogue\": { \"speaker\": <3–20 chars>, \"text\": \"says <text of 10–100 characters>\" }
}"
}
```

---

## Extension Ideas

- Allow probabilistic field weighting or optional fields.
- Add support for arrays (`ArrayField`).
- Export schema to JSON Schema format as well.
- Integrate runtime validation of LLM output using the same definitions.

---

## Summary

This design allows:

- **Declarative control** of LLM-generated JSON structures.
- **Automatic GBNF generation** to constrain the LLM.
- **Automatic template creation** to guide output semantics.
- **Nesting and branching** for complex game event structures.

By following this specification, a Copilot agent can implement a working generator system that ensures every JSON produced by the LLM adheres to the game’s strict format requirements.