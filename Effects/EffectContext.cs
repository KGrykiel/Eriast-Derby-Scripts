using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Skills.Helpers;

namespace Assets.Scripts.Effects
{
    /// <summary>
    /// Context for applying an effect, for example to determine if the attack was a critical hit to double the damage effect.
    /// Can be extended with more fields as needed, but should be kept separate from SkillContext to avoid coupling effects to skills.
    /// </summary>
    public struct EffectContext
    {
        public bool IsCriticalHit;
        public Entity SourceEntity;
        public DamageSource? DamageSourceOverride;

        public static EffectContext Default => new();

        public static EffectContext FromSkillContext(SkillContext ctx)
        {
            return new EffectContext
            {
                IsCriticalHit = ctx.IsCriticalHit,
                SourceEntity = ctx.SourceEntity
            };
        }
    }
}
