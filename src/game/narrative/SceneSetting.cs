using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The setting rules carried by every LLM request: the world's register (a medieval world, nothing
/// modern or real) and where the current scene is happening.
/// <para>
/// Ambient by design, like <see cref="NameFaking"/>. Prompts are assembled deep in
/// <see cref="PersonaRewriter"/>, which holds no world and no location of its own, and threading a
/// <see cref="WorldContext"/> through every rewrite call site — observation, thinking, action,
/// outcome, dialogue — to reach one closing sentence would be a wide change for a narrow need.
/// <see cref="SetPlace"/> is called when a scene opens; the value then applies to every request the
/// scene makes, on any slot.
/// </para>
/// </summary>
public static class SceneSetting
{
    /// <summary>
    /// Appended to every LLM instance's system prompt (see
    /// <c>LlamaServerManager.CreateInstanceAsync</c>), so it governs every request that slot will ever
    /// serve — personas, dialogue, critics alike.
    /// <para>
    /// Written flat and with named examples on purpose: a small model obeys "no bullets, no offices"
    /// where it ignores an abstraction like "avoid anachronisms". The examples are the drift actually
    /// observed in play (office, bullet, photo album), none of which the sanitizer's word lists catch.
    /// </para>
    /// </summary>
    public const string Rule =
        "Everything you write happens in a medieval world of iron, myth and hand labour. " +
        "Never write anything modern or from the real world: no machines, engines, guns, bullets, " +
        "photographs, albums, offices, factories, hospitals, schools, plastic, rubber, electricity, " +
        "chemistry, brand names, and no real country, city, people or date. " +
        "If a thing has no medieval name, do not mention the thing at all.";

    /// <summary>
    /// Where the current scene is, as a short noun phrase fit to drop into a prompt ("windswept
    /// plain", "forest", "village"). Empty before any scene opens — <see cref="Reminder"/> then omits
    /// the place clause rather than naming a place the scene is not in.
    /// </summary>
    public static string Place { get; private set; } = string.Empty;

    /// <summary>
    /// Records where the scene is happening. Pass what the scene's <see cref="WorldContext"/> calls
    /// the place (<see cref="WorldContext.GenerateContextDescription"/>); null or blank clears it.
    /// </summary>
    public static void SetPlace(string? place)
    {
        Place = (place ?? string.Empty).Trim().TrimEnd('.').Trim();
        if (Place.Length > 0)
            Console.WriteLine($"SceneSetting: scene place set to '{Place}'.");
    }

    /// <summary>
    /// The closing reminder appended to the end of every generation request (see the answer
    /// instructions in <c>Config.Narrative</c>). The system prompt already carries <see cref="Rule"/>,
    /// but on a 3B model a rule 1500 tokens up the context loses to the immediate wording of the
    /// request, so it is restated last — where instructions weigh most — and paired with the place, so
    /// the model does not furnish a forest with town streets.
    /// </summary>
    public static string Reminder()
    {
        const string setting = "Remember: a medieval world, nothing modern and nothing from the real world.";
        return Place.Length == 0
            ? setting
            : $"{setting} You are in {Article(Place)} {Place} — write only what belongs there.";
    }

    private static string Article(string noun) =>
        noun.Length > 0 && "aeiou".IndexOf(char.ToLowerInvariant(noun[0])) >= 0 ? "an" : "a";
}
