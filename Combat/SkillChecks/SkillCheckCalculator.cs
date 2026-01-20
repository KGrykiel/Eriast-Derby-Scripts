using System.Collections.Generic;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Central calculator for all skill check logic.
    /// 
    /// Responsibilities:
    /// - Rolling d20 skill checks (uses RollUtility)
    /// - Gathering skill check modifiers from all sources
    /// - Evaluating success/failure
    /// 
    /// DESIGN: Follows same pattern as AttackCalculator and SaveCalculator.
    /// Characters/entities store base skill values, this gathers modifiers and computes finals.
    /// Returns breakdown data for tooltips.
    /// 
    /// Flow: Character rolls d20 + skill bonus vs DC
    /// Success = Total >= DC (character succeeds at the task)
    /// </summary>
    public static class SkillCheckCalculator
    {
        // ==================== SKILL CHECK ROLLING ====================
        
        /// <summary>
        /// Perform a skill check.
        /// This is the primary method for making skill checks.
        /// </summary>
        public static SkillCheckResult PerformSkillCheck(
            Entity entity,
            SkillCheckType checkType,
            int dc)
        {
            // Roll the d20
            var result = RollSkillCheck(checkType);
            
            // Gather and add skill modifiers
            var modifiers = GatherSkillCheckModifiers(entity, checkType);
            D20RollHelpers.AddModifiers(result, modifiers);
            
            // Evaluate: Success if Total >= DC
            D20RollHelpers.EvaluateAgainstTarget(result, dc);
            
            return result;
        }
        
        /// <summary>
        /// Roll a d20 skill check and create a result.
        /// </summary>
        public static SkillCheckResult RollSkillCheck(SkillCheckType checkType)
        {
            int roll = RollUtility.RollD20();
            return SkillCheckResult.FromD20(roll, checkType);
        }
        
        // ==================== SKILL MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather skill check modifiers from all sources.
        /// 
        /// Sources:
        /// - Intrinsic: Base skill value from appropriate component
        /// - Applied: Status effects and equipment on that component
        /// </summary>
        public static List<AttributeModifier> GatherSkillCheckModifiers(
            Entity entity,
            SkillCheckType checkType)
        {
            var modifiers = new List<AttributeModifier>();
            
            // Get the entity that owns this skill (e.g., chassis for Mobility)
            Entity sourceEntity = GetSourceEntityForCheck(entity, checkType);
            if (sourceEntity == null)
                return modifiers;
            
            // Get the corresponding attribute
            Attribute attribute = SkillCheckTypeToAttribute(checkType);
            
            // 1. Intrinsic: Base skill value from source entity
            float baseValue = GetBaseSkillValue(sourceEntity, checkType);
            if (baseValue != 0)
            {
                modifiers.Add(new AttributeModifier(
                    attribute,
                    ModifierType.Flat,
                    baseValue,
                    sourceEntity));
            }
            
            // 2. Applied: Status effects and equipment from source entity
            D20RollHelpers.GatherAppliedModifiers(sourceEntity, attribute, modifiers);
            
            return modifiers;
        }
        
        // ==================== MODIFIER SOURCES ====================
        
        /// <summary>
        /// Get the entity that should be the source for a skill check.
        /// Routes to appropriate component based on check type.
        /// 
        /// Current routing:
        /// - Mobility → Chassis (vehicle maneuverability)
        /// 
        /// Future routing examples:
        /// - Perception → Character or Sensor component
        /// - Mechanics → Drive component or Character
        /// - Electronics → Sensor component or Character
        /// </summary>
        private static Entity GetSourceEntityForCheck(Entity entity, SkillCheckType checkType)
        {
            return checkType switch
            {
                SkillCheckType.Mobility => GetChassisFromEntity(entity),
                _ => null
            };
        }
        
        /// <summary>
        /// Get base skill value from the source entity.
        /// 
        /// TEMPORARY: Only supports Mobility from chassis.
        /// When adding more skill types, add cases here to read from appropriate fields.
        /// </summary>
        private static float GetBaseSkillValue(Entity entity, SkillCheckType checkType)
        {
            if (entity is ChassisComponent chassis)
            {
                return checkType switch
                {
                    SkillCheckType.Mobility => chassis.baseMobility,
                    _ => 0f
                };
            }
            return 0f;
        }
        
        /// <summary>
        /// Helper: Get chassis from entity (handles both direct chassis and components).
        /// TEMPORARY: Currently only used for Mobility checks.
        /// </summary>
        private static ChassisComponent GetChassisFromEntity(Entity entity)
        {
            if (entity is ChassisComponent chassis)
            {
                return chassis;
            }
            else if (entity is VehicleComponent component)
            {
                Vehicle parentVehicle = EntityHelpers.GetParentVehicle(component);
                return parentVehicle?.chassis;
            }
            return null;
        }
        
        // ==================== HELPERS ====================
        
        /// <summary>
        /// Map SkillCheckType to corresponding Attribute for modifier gathering.
        /// Currently only Mobility exists (same stat used for saves and checks).
        /// </summary>
        public static Attribute SkillCheckTypeToAttribute(SkillCheckType checkType)
        {
            return checkType switch
            {
                SkillCheckType.Mobility => Attribute.Mobility,
                _ => Attribute.Mobility // Default fallback
            };
        }
    }
}

