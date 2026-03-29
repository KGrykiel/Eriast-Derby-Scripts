using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Rolls.RollSpecs;

namespace Assets.Scripts.Effects
{
    /// <summary>
    /// Context for applying an effect, for example to determine if the attack was a critical hit to double the damage effect.
    /// Can be extended with more fields as needed, but should be kept separate from SkillContext to avoid coupling effects to skills.
    /// </summary>
    public struct EffectContext
    {
        public bool IsCriticalHit;
        public RollActor SourceActor;
        public string CausalSource;

        public static EffectContext Default => new();

        public static EffectContext FromRollContext(RollContext ctx, bool isCriticalHit = false)
        {
            return new EffectContext
            {
                IsCriticalHit = isCriticalHit,
                SourceActor = ctx.SourceActor
            };
        }
    }
}
