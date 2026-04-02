using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Effects.Targeting;

namespace Assets.Scripts.Effects
{
    /// <summary>
    /// The full wrapper that is used in the Unity editor. Allows to route each effect in a skill/event to different targets;
    /// for example, damage to selected target but speed buff to self. Each effect's Apply handles its own internal routing.
    /// </summary>
    [System.Serializable]
    public class EffectInvocation
    {
        [SerializeReference, SR]
        public IEffect effect;

        [SerializeReference, SR]
        public IEffectTargetResolver targetResolver;
    }
}