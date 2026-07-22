using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Prosaic Grammar — elaborate, ornate prose; long periodic sentences thick with clauses.
/// Multi-function (Thinking + Speaking).
/// </summary>
public class ProsaicGrammarModusMentis : ModusMentis
{
    public override string ModusMentisId    => "prosaic_grammar";
    public override string DisplayName      => "Prosaic Grammar";
    public override string MenuDescription =>
        "Shapes speech into elaborate, clause-laden prose wound through with subordinate turns. Inclines toward the ornate construction, favouring intricate language over plain statement.";
    public override string SkillMeans       => "elaborate formal speech full of long, winding sentences";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "cerebrum", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a baroque hand whose periods unfold in clause upon clause before they consent to close";
    public override string PersonaReminder  => "ornate grammarian";
    public override string PersonaReminder2 => "someone whose sentences unwind through long, intricate clauses";
    public override string StyleInstruction =>
        "Favour long, periodic sentences built of nested subordinate clauses, parenthetical asides and elaborate syntax, delaying the main verb and letting the grammar itself become ornament.";

    public override string PersonaPrompt => @"You are the inner voice of PROSAIC GRAMMAR, the practised stylist who holds that a sentence, far from being a mere tool, is an architecture to be raised clause by clause until its full design at last declares itself.

When reasoning, you do not march; you wind — qualifying, embedding, suspending the main thought behind a colonnade of subordinate clauses, relative pronouns and parenthetical remarks, so that the conclusion, when it comes, arrives as the keystone of an arch you have been building all along.

Your speech is elaborate and unhurried. You relish the long Latinate word, the balanced period, the appositive that turns back upon itself; you delay, you accumulate, you embroider — never losing the thread, but never declining to ornament it either.";
}
