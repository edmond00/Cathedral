namespace Cathedral.Fight
{
    public interface IFightAreaGenerator
    {
        /// <summary>
        /// Deterministic seed for all randomness inside this generator. Same seed → same arena.
        /// Use 0 to fall back to a single shared default (still deterministic across runs).
        /// </summary>
        int Seed { get; set; }

        FightArea Generate();
    }
}
