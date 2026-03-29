using System;
using System.Collections.Generic;
using Assets.Scripts.Characters;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.Advantage
{
    public interface IAdvantageTarget { }

    [Serializable]
    [SRName("Attacks")]
    public class AttackAdvantage : IAdvantageTarget { }

    [Serializable]
    [SRName("Vehicle Checks")]
    public class VehicleCheckAdvantage : IAdvantageTarget
    {
        [Tooltip("Restrict to specific attributes. Leave empty for all vehicle checks.")]
        public List<VehicleCheckAttribute> limitTo;
    }

    [Serializable]
    [SRName("Character Checks")]
    public class CharacterCheckAdvantage : IAdvantageTarget
    {
        [Tooltip("Restrict to specific skills. Leave empty for all character checks.")]
        public List<CharacterSkill> limitTo;
    }

    [Serializable]
    [SRName("Vehicle Saves")]
    public class VehicleSaveAdvantage : IAdvantageTarget
    {
        [Tooltip("Restrict to specific attributes. Leave empty for all vehicle saves.")]
        public List<VehicleCheckAttribute> limitTo;
    }

    [Serializable]
    [SRName("Character Saves")]
    public class CharacterSaveAdvantage : IAdvantageTarget
    {
        [Tooltip("Restrict to specific attributes. Leave empty for all character saves.")]
        public List<CharacterAttribute> limitTo;
    }

    [Serializable]
    public class AdvantageGrant
    {
        public string label;
        public RollMode type;

        [SerializeReference, SR]
        [Tooltip("What types of rolls does this grant apply to? Add entries for each category.")]
        public List<IAdvantageTarget> targets;

        [NonSerialized]
        public object Source;
    }

    /// <summary>
    /// Labeled advantage/disadvantage source for breakdowns and tooltips.
    /// Parallel to RollBonus but for the advantage axis.
    /// Used both as a serialised grant on RollNode and as a runtime source on outcomes.
    /// Default (Type = Normal) means no grant — used by RollNode when unconfigured.
    /// </summary>
    [Serializable]
    public struct AdvantageSource
    {
        public string Label;
        public RollMode Type;

        public AdvantageSource(string label, RollMode type)
        {
            Label = label;
            Type = type;
        }
    }

    /// <summary>
    /// Groups all advantage-related outcome data for a single d20 roll.
    /// Stored on D20RollOutcome, consumed by CombatFormatter.
    /// </summary>
    public struct AdvantageResult
    {
        public RollMode Mode;
        public int? DroppedRoll;
        public List<AdvantageSource> Sources;

        public AdvantageResult(RollMode mode, int? droppedRoll, List<AdvantageSource> sources)
        {
            Mode = mode;
            DroppedRoll = droppedRoll;
            Sources = sources ?? new List<AdvantageSource>();
        }
    }
}
