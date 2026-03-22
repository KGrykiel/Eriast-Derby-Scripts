using System;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    /// <summary>Opposed check — both actor and target roll, highest wins.</summary>
    [Serializable]
    [SRName("Opposed Check")]
    public class OpposedCheckRollSpec : IRollSpec
    {
        [SerializeReference, SR]
        public SkillCheckSpec attackerSpec;

        [SerializeReference, SR]
        public SkillCheckSpec defenderSpec;
    }
}
