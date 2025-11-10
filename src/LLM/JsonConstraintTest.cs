using Cathedral.LLM.JsonConstraints;

namespace Cathedral.LLM;

/// <summary>
/// Simple test program to verify the JsonConstraintGenerator implementation
/// </summary>
public class JsonConstraintTest
{
    public static void TestJsonConstraintGeneration()
    {
        Console.WriteLine("Testing JSON Constraint Generation System...\n");

        try
        {
            // Create a simple schema for testing
            var testSchema = new CompositeField("TestEvent", new JsonField[]
            {
                new StringField("name", 3, 20),
                new IntField("level", 1, 50),
                new ChoiceField<string>("type", "fire", "water", "earth", "air"),
                new BooleanField("isActive")
            });

            // Generate GBNF
            var gbnf = JsonConstraintGenerator.GenerateGBNF(testSchema);
            Console.WriteLine("Generated GBNF:");
            Console.WriteLine(gbnf);
            Console.WriteLine();

            // Generate Template
            var template = JsonConstraintGenerator.GenerateTemplate(testSchema);
            Console.WriteLine("Generated Template:");
            Console.WriteLine(template);
            Console.WriteLine();

            // Test the demo examples
            Console.WriteLine("Running full demonstration...");
            JsonConstraintDemo.RunDemonstration();

            Console.WriteLine("JSON Constraint Generation test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during testing: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}