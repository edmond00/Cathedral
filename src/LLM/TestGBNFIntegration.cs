using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.LLM
{
    public static class TestGBNFIntegration
    {
        public static async Task RunTest()
        {
            Console.WriteLine("🧪 Testing GBNF Integration...");
            
            var llmManager = new LlamaServerManager("http://localhost:8080");
            
            try
            {
                // Test 1: Create a simple schema
                var nameSchema = new CompositeField("character", new JsonField[]
                {
                    new StringField("name", 1, 50),
                    new IntField("age", 1, 150),
                    new ChoiceField<string>("occupation", "wizard", "warrior", "rogue", "cleric")
                });
                
                var gbnf = JsonConstraintGenerator.GenerateGBNF(nameSchema);
                
                Console.WriteLine($"📝 Generated GBNF Grammar:");
                Console.WriteLine(gbnf);
                Console.WriteLine();
                
                // Test 2: Initialize slot and test with GBNF
                var slotId = await llmManager.CreateInstanceAsync("gbnf-test");
                Console.WriteLine($"✅ Initialized slot: {slotId}");
                
                var responseReceived = false;
                var fullResponse = "";
                
                await llmManager.ContinueRequestAsync(
                    slotId,
                    "Create a character for a fantasy RPG. Return only valid JSON with name, age, and occupation fields.",
                    onTokenStreamed: (token, slotId) =>
                    {
                        Console.Write(token);
                        fullResponse += token;
                    },
                    onCompleted: (slotId, response, wasCancelled) =>
                    {
                        responseReceived = true;
                        Console.WriteLine($"\n✅ Response completed. Cancelled: {wasCancelled}");
                    },
                    gbnfGrammar: gbnf
                );
                
                // Wait for completion
                while (!responseReceived)
                {
                    await Task.Delay(100);
                }
                
                // Test 3: Validate the response
                Console.WriteLine("\n🔍 Validating JSON response...");
                var isValid = JsonValidator.ValidateJson(fullResponse.Trim(), nameSchema, out var errors);
                
                if (isValid)
                {
                    Console.WriteLine("✅ GBNF Integration Test PASSED! Response is valid JSON matching schema.");
                }
                else
                {
                    Console.WriteLine("❌ GBNF Integration Test FAILED! Validation errors:");
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }
                
                // Cleanup
                llmManager.ResetInstance(slotId);
                Console.WriteLine("🧹 Cleaned up test slot");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GBNF Integration Test FAILED with exception: {ex.Message}");
                throw;
            }
            
            Console.WriteLine("🎯 GBNF Integration Test Complete!");
        }
    }
}