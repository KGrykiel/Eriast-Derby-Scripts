using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Effects.Targeting;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.Invocations
{
    /// <summary>
    /// Invocation for effects that operate on Vehicle targets.
    /// The SR picker constrains both fields to the correct typed interfaces.
    /// </summary>
    [System.Serializable]
    [SRName("Vehicle")]
    public class VehicleEffectInvocation : IEffectInvocation
    {
        [SerializeReference, SR]
        public IVehicleEffect effect;

        [SerializeReference, SR]
        public IVehicleEffectResolver targetResolver;

        public void Execute(RollContext ctx, EffectContext effectContext)
        {
            if (effect == null || targetResolver == null) return;
            foreach (var target in targetResolver.Resolve(ctx))
            {
                if (target != null)
                    effect.Apply(target, effectContext);
            }
        }
    }
}
