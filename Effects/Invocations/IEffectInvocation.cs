using Assets.Scripts.Combat.Rolls.RollSpecs;

namespace Assets.Scripts.Effects.Invocations
{
    /// <summary>
    /// Pairs a typed effect with a typed resolver and executes it against resolved targets.
    /// Implemented by <see cref="EntityEffectInvocation"/>, <see cref="VehicleEffectInvocation"/>, and <see cref="SeatEffectInvocation"/>.
    /// </summary>
    public interface IEffectInvocation
    {
        void Execute(RollContext ctx, EffectContext effectContext);
    }
}
