using Assets.Scripts.Modifiers;

namespace Assets.Scripts.Conditions
{
    /// <summary>
    /// Abstract runtime base for all applied conditions/effects.
    /// Each subclass holds its own typed template field and exposes it via Template.
    /// </summary>
    public abstract class AppliedConditionBase : IModifierSource
    {
        public UnityEngine.Object applier;
        public int turnsRemaining;

        public bool IsIndefinite => turnsRemaining < 0;
        public bool IsExpired => turnsRemaining == 0;

        public string ModifierLabel => Template?.effectName ?? "";

        public abstract ConditionBase Template { get; }

        public bool PreventsActions => Template.behavioralEffects?.preventsActions ?? false;
        public bool PreventsMovement => Template.behavioralEffects?.preventsMovement ?? false;

        protected AppliedConditionBase(int baseDuration, UnityEngine.Object applier)
        {
            this.applier = applier;
            this.turnsRemaining = baseDuration;
        }

        public void DecrementDuration()
        {
            if (IsIndefinite) return;
            turnsRemaining--;
        }

        public void RefreshDuration()
        {
            turnsRemaining = Template.baseDuration;
        }
    }
}
