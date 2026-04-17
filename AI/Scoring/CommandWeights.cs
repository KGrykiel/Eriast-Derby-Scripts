namespace Assets.Scripts.AI.Scoring
{
    /// <summary>
    /// Weight vector describing how strongly a seat wants each kind of action
    /// right now. Produced in the Scoring stage from tracker signals × personality.
    /// Skills are scored against this vector via dot product against a matching
    /// per-skill utility vector produced by <see cref="SkillScorer"/>.
    ///
    /// Axes are not mutually exclusive — they coexist. A mixed-role skill earns
    /// score on every axis it contributes to.
    /// </summary>
    public struct CommandWeights
    {
        /// <summary>Damage enemies, pressure their components.</summary>
        public float attack;

        /// <summary>Restore own/ally HP or energy; remove harmful conditions.</summary>
        public float heal;

        /// <summary>Debuff enemies, slow them, push them back, interrupt plans.</summary>
        public float disrupt;

        /// <summary>Escape pressure — lane changes away from threat, speed boosts.</summary>
        public float flee;
    }
}
