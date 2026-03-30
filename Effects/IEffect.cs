namespace Assets.Scripts.Effects
{
    public interface IEffect
    {
        void Apply(IEffectTarget target, EffectContext context);
    }
}
