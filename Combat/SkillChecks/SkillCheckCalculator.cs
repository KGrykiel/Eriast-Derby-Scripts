using System.Collections.Generic;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Central calculator for all skill check logic.
    /// Gathers bonuses, rolls d20, builds complete result in one shot.
    /// </summary>
    public static class SkillCheckCalculator
    {
        /// <summary>
        /// Perform a skill check. Gathers all bonuses, rolls, evaluates, returns complete result.
        /// </summary>
        public static SkillCheckResult PerformSkillCheck(
            Entity entity,
            SkillCheckType checkType,
            int dc)
        {
            int baseRoll = RollUtility.RollD20();
            var bonuses = GatherBonuses(entity, checkType);
            int total = baseRoll + SumBonuses(bonuses);
            bool success = total >= dc;
            
            return new SkillCheckResult(baseRoll, checkType, bonuses, dc, success);
        }
        
        /// <summary>
        /// Gather all bonuses for a skill check as RollBonus entries.
        /// </summary>
        public static List<RollBonus> GatherBonuses(Entity entity, SkillCheckType checkType)
        {
            var bonuses = new List<RollBonus>();
            
            Entity sourceEntity = GetSourceEntityForCheck(entity, checkType);
            if (sourceEntity == null)
                return bonuses;
            
            // Intrinsic: base value from source entity
            int baseValue = GetBaseSkillValue(sourceEntity, checkType);
            if (baseValue != 0)
            {
                bonuses.Add(new RollBonus(GetBaseValueLabel(sourceEntity, checkType), baseValue));
            }
            
            // Applied: status effects and equipment on source entity
            Attribute attribute = SkillCheckTypeToAttribute(checkType);
            bonuses.AddRange(D20RollHelpers.GatherAppliedBonuses(sourceEntity, attribute));
            
            return bonuses;
        }
        
        // ==================== ROUTING ====================
        
        private static Entity GetSourceEntityForCheck(Entity entity, SkillCheckType checkType)
        {
            return checkType switch
            {
                SkillCheckType.Mobility => GetChassisFromEntity(entity),
                _ => null
            };
        }
        
        private static int GetBaseSkillValue(Entity entity, SkillCheckType checkType)
        {
            if (entity is ChassisComponent chassis)
            {
                return checkType switch
                {
                    SkillCheckType.Mobility => chassis.GetBaseMobility(),
                    _ => 0
                };
            }
            return 0;
        }
        
        private static string GetBaseValueLabel(Entity entity, SkillCheckType checkType)
        {
            return checkType switch
            {
                SkillCheckType.Mobility => entity.name ?? "Chassis Mobility",
                _ => entity.name ?? "Base"
            };
        }
        
        private static ChassisComponent GetChassisFromEntity(Entity entity)
        {
            if (entity is ChassisComponent chassis)
                return chassis;
            if (entity is VehicleComponent component)
            {
                Vehicle parentVehicle = EntityHelpers.GetParentVehicle(component);
                return parentVehicle?.chassis;
            }
            return null;
        }
        
        // ==================== HELPERS ====================
        
        public static Attribute SkillCheckTypeToAttribute(SkillCheckType checkType)
        {
            return checkType switch
            {
                SkillCheckType.Mobility => Attribute.Mobility,
                _ => Attribute.Mobility
            };
        }
        
        private static int SumBonuses(List<RollBonus> bonuses)
        {
            int sum = 0;
            foreach (var b in bonuses) sum += b.Value;
            return sum;
        }
    }
}

