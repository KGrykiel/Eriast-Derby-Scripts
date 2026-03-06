using System;
using SerializeReferenceEditor;

namespace Assets.Scripts.Combat.RollSpecs
{
    /// <summary>Opposed check — both actor and target roll, highest wins.</summary>
    [Serializable]
    [SRName("Opposed Check")]
    public class OpposedCheckRollSpec : IRollSpec
    {
        public SkillCheckSpec attackerSpec;
        public SkillCheckSpec defenderSpec;
    }
}
