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
        // ==================== SAVING THROWS ====================
        
        /// <summary>
        /// Perform a saving throw. Target rolls d20 + save bonus vs DC.
        /// Returns SaveResult with full breakdown for logging/tooltips.
        /// </summary>
        public static SaveResult PerformSavingThrow(
            Entity target,
            SaveType saveType,
            int dc,
            string sourceName = "Effect")
        {
            // Roll d20
            int roll = RollUtility.RollD20();
            
            // Create result
            var result = SaveResult.FromD20(roll, saveType);
            result.targetValue = dc;
            
            // Gather save modifiers (single source of truth)
            var modifiers = GatherSaveModifiers(target, saveType);
            
            // Add modifiers to result
            foreach (var mod in modifiers)
            {
                result.modifiers.Add(mod);
            }
            
            // Evaluate: Success if Total >= DC
            result.success = result.Total >= dc;
            
            return result;
        }
        
        // ==================== SAVE MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather ALL save modifiers from all sources.
        /// This is the SINGLE SOURCE OF TRUTH for save bonuses.
        /// Returns list of AttributeModifiers for the save attribute.
        /// </summary>
        public static List<AttributeModifier> GatherSaveModifiers(
            Entity target,
            SaveType saveType)
        {
            var modifiers = new List<AttributeModifier>();
            
            // 1. Base save from chassis
            GatherBaseSaveModifiers(target, saveType, modifiers);
            
            // 2. Entity modifiers (status effects + equipment bonuses from components)
            if (target != null)
            {
                GatherEntitySaveModifiers(target, saveType, modifiers);
            }
            
            return modifiers;
        }
        
        // ==================== MODIFIER SOURCES ====================
        
        private static void GatherBaseSaveModifiers(Entity target, SaveType saveType, List<AttributeModifier> modifiers)
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
        
        private static void GatherEntitySaveModifiers(Entity target, SaveType saveType, List<AttributeModifier> modifiers)
        {
            // Delegate to StatCalculator - single source of truth for modifier gathering
            // Returns ALL modifiers on the entity for this save (status effects + equipment)
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
