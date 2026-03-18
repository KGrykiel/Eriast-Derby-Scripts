using System;
using UnityEngine;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using SerializeReferenceEditor;

namespace Assets.Scripts.StatusEffects
{
    public interface IPeriodicEffect { }

    [Serializable]
    [SRName("Damage (DoT)")]
    public class PeriodicDamageEffect : IPeriodicEffect
    {
        [Tooltip("Damage formula and type")]
        public DamageFormula damageFormula = new() { baseDice = 1, dieSize = 6, bonus = 0, damageType = DamageType.Fire };
    }

    [Serializable]
    [SRName("Restoration (HoT/Energy)")]
    public class PeriodicRestorationEffect : IPeriodicEffect
    {
        [Tooltip("Restoration formula defining resource type, dice, and bonus")]
        public RestorationFormula formula = new();
    }
}
