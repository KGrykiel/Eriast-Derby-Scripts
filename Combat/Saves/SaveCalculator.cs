using System.Collections.Generic;

namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Central calculator for all saving throw logic.
    /// 
    /// Responsibilities:
    /// - Rolling d20 saving throws (uses RollUtility)
    /// - Gathering save modifiers from all sources
    /// - Calculating Save DC for skills
    /// - Evaluating success/failure
    /// 
    /// DESIGN: Entities store raw base values (e.g., chassis.baseMobility).
    /// This calculator gathers modifiers via StatCalculator and computes final values.
    /// Returns breakdown data for tooltips.
    /// 
    /// Flow: Target rolls d20 + save bonus vs DC
    /// Success = Total >= DC (target resists the effect)
    /// </summary>
    public static class SaveCalculator
    {
        // ==================== SAVE ROLLING ====================
        
        /// <summary>
        /// Perform a saving throw for a skill. Calculates DC from skill and user.
        /// This is the primary method for skill-based saves.
        /// </summary>
        public static SaveResult PerformSavingThrow(Entity target, Skill skill, Entity dcSource)
        {
            int dc = CalculateSaveDC(skill, dcSource);
            return PerformSavingThrow(target, skill.saveType, dc);
        }
        
        /// <summary>
        /// Perform a saving throw with explicit DC.
        /// Use this for non-skill saves (environmental hazards, traps with fixed DC).
        /// </summary>
        public static SaveResult PerformSavingThrow(Entity target, SaveType saveType, int dc)
        {
            // Roll the d20
            var result = RollSavingThrow(saveType);
            
            // Gather and add save modifiers
            var modifiers = GatherSaveModifiers(target, saveType);
            D20RollHelpers.AddModifiers(result, modifiers);
            
            // Evaluate: Success if Total >= DC
            D20RollHelpers.EvaluateAgainstTarget(result, dc);
            
            return result;
        }
        
        /// <summary>
        /// Roll a d20 saving throw and create a save result.
        /// </summary>
        public static SaveResult RollSavingThrow(SaveType saveType)
        {
            int roll = RollUtility.RollD20();
            return SaveResult.FromD20(roll, saveType);
        }
        
        // ==================== SAVE MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather save modifiers from all sources.
        /// 
        /// Sources:
        /// - Intrinsic (chassis): Base save value from chassis design
        /// - Applied (buffs, debuffs): Pre-existing modifiers on entity
        /// </summary>
        public static List<AttributeModifier> GatherSaveModifiers(
            Entity target,
            SaveType saveType)
        {
            var modifiers = new List<AttributeModifier>();
            
            // 1. Intrinsic: Base save from chassis
            GatherBaseSaveBonus(target, saveType, modifiers);
            
            // 2. Applied: Status effects and equipment (shared helper)
            if (target != null)
            {
                D20RollHelpers.GatherAppliedModifiers(target, SaveTypeToAttribute(saveType), modifiers);
            }
            
            return modifiers;
        }
        
        // ==================== MODIFIER SOURCES ====================
        
        /// <summary>
        /// Intrinsic: Chassis's base mobility value (used for both saves and checks).
        /// </summary>
        private static void GatherBaseSaveBonus(Entity target, SaveType saveType, List<AttributeModifier> modifiers)
        {
            // Source mobility directly from chassis
            if (target is ChassisComponent chassis)
            {
                int mobility = chassis.GetBaseMobility();
                if (mobility != 0)
                {
                    modifiers.Add(new AttributeModifier(
                        Attribute.Mobility,
                        ModifierType.Flat,
                        mobility,
                        target));
                }
            }
        }

        // ==================== DC CALCULATION ====================
        
        /// <summary>
        /// Calculate the Save DC for a skill.
        /// DC = Skill's base DC + User's DC bonus (future).
        /// </summary>
        public static int CalculateSaveDC(Skill skill, Entity user)
        {
            int baseDC = skill.saveDCBase;
            
            // Future: Add user's DC bonus based on relevant attribute
            // int userBonus = GetUserDCBonus(user, skill.saveDCAttribute);
            // return baseDC + userBonus;
            
            return baseDC;
        }
        
        // ==================== HELPERS ====================
        
        /// <summary>
        /// Map SaveType to corresponding Attribute for modifier gathering.
        /// Currently only Mobility exists (same stat used for saves and checks).
        /// </summary>
        public static Attribute SaveTypeToAttribute(SaveType saveType)
        {
            return saveType switch
            {
                SaveType.Mobility => Attribute.Mobility,
                _ => Attribute.Mobility // Default fallback
            };
        }
    }
}
