using System.Collections.Generic;
using Core;

namespace Combat.Saves
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
            AddModifiers(result, modifiers);
            
            // Evaluate: Success if Total >= DC
            EvaluateAgainstDC(result, dc);
            
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
            
            // 2. Applied: Status effects and equipment
            if (target != null)
            {
                GatherAppliedModifiers(target, saveType, modifiers);
            }
            
            return modifiers;
        }
        
        // ==================== MODIFIER SOURCES ====================
        
        /// <summary>
        /// Intrinsic: Chassis's base save value (vehicle design).
        /// </summary>
        private static void GatherBaseSaveBonus(Entity target, SaveType saveType, List<AttributeModifier> modifiers)
        {
            float baseSave = GetEntityBaseSave(target, saveType);
            
            if (baseSave != 0)
            {
                modifiers.Add(new AttributeModifier(
                    SaveTypeToAttribute(saveType),
                    ModifierType.Flat,
                    baseSave,
                    target));
            }
        }
        
        /// <summary>
        /// Applied: Status effects and equipment modifiers already on the entity.
        /// Delegates to StatCalculator (single source of truth).
        /// </summary>
        private static void GatherAppliedModifiers(Entity target, SaveType saveType, List<AttributeModifier> modifiers)
        {
            var (_, _, allModifiers) = StatCalculator.GatherAttributeValueWithBreakdown(
                target, 
                SaveTypeToAttribute(saveType), 
                0f);
            
            foreach (var mod in allModifiers)
            {
                if (mod.Value != 0)
                {
                    modifiers.Add(mod);
                }
            }
        }
        
        // ==================== RESULT HELPERS ====================
        
        private static void AddModifiers(SaveResult result, List<AttributeModifier> modifiers)
        {
            foreach (var mod in modifiers)
            {
                if (mod.Value != 0)
                {
                    result.modifiers.Add(mod);
                }
            }
        }
        
        private static void EvaluateAgainstDC(SaveResult result, int dc)
        {
            result.targetValue = dc;
            result.success = result.Total >= dc;
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
        /// Get base save value from entity based on save type.
        /// </summary>
        private static float GetEntityBaseSave(Entity entity, SaveType saveType)
        {
            if (entity is ChassisComponent chassis)
            {
                return saveType switch
                {
                    SaveType.Mobility => chassis.baseMobility,
                    _ => 0f
                };
            }
            return 0f;
        }
        
        /// <summary>
        /// Map SaveType to corresponding Attribute for modifier gathering.
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
