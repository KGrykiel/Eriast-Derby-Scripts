using Assets.Scripts.Entities;

namespace Assets.Scripts.Effects
{
    public interface IEffect
    {
        void Apply(Entity target, EffectContext context);
    }
}
