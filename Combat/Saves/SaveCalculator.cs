using System.Collections.Generic;

namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Central calculator for all saving throw logic.
    /// Gathers bonuses, rolls d20, builds complete result in one shot.
    /// </summary>
    public static class SaveCalculator
    {
        /// <summary>
        /// Perform a saving throw for a skill. Calculates DC from skill and user.
        /// </summary>
        public static SaveResult PerformSavingThrow(Entity target, Skill skill, Entity dcSource)
        {
            int dc = CalculateSaveDC(skill, dcSource);
            return PerformSavingThrow(target, skill.saveType, dc);
        }
        
        /// <summary>
        /// Perform a saving throw with explicit DC.
        /// Gathers all bonuses, rolls, evaluates, returns complete result.
        /// </summary>
        public static SaveResult PerformSavingThrow(Entity target, SaveType saveType, int dc)
        {
            int baseRoll = RollUtility.RollD20();
            var bonuses = GatherBonuses(target, saveType);
            int total = baseRoll + SumBonuses(bonuses);
            bool success = total >= dc;
            
            return new SaveResult(baseRoll, saveType, bonuses, dc, success);
        }
        
        /// <summary>
        /// Gather all bonuses for a saving throw as RollBonus entries.
        /// </summary>
        public static List<RollBonus> GatherBonuses(Entity target, SaveType saveType)
        {
            var bonuses = new List<RollBonus>();
            
            // Intrinsic: base save value from the appropriate entity
            Entity sourceEntity = GetSourceEntityForSave(target, saveType);
            if (sourceEntity != null)
            {
                int baseValue = GetBaseSaveValue(sourceEntity, saveType);
                if (baseValue != 0)
                {
                    bonuses.Add(new RollBonus(sourceEntity.name ?? saveType.ToString(), baseValue));
                }
            }
            
            // Applied: status effects and equipment
            if (sourceEntity != null)
            {
                Attribute attribute = SaveTypeToAttribute(saveType);
                bonuses.AddRange(D20RollHelpers.GatherAppliedBonuses(sourceEntity, attribute));
            }
            
            return bonuses;
        }
        
        // ==================== ROUTING ====================
        
        private static Entity GetSourceEntityForSave(Entity target, SaveType saveType)
        {
            return saveType switch
            {
                SaveType.Mobility => GetChassisFromEntity(target),
                // Future: Stability → drive, Systems → power core, etc.
                _ => null
            };
        }
        
        private static int GetBaseSaveValue(Entity entity, SaveType saveType)
        {
            if (entity is ChassisComponent chassis)
            {
                return saveType switch
                {
                    SaveType.Mobility => chassis.GetBaseMobility(),
                    _ => 0
                };
            }
            return 0;
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
        
        // ==================== DC CALCULATION ====================
        
        public static int CalculateSaveDC(Skill skill, Entity user)
        {
            return skill.saveDCBase;
        }
        
        // ==================== HELPERS ====================
        
        public static Attribute SaveTypeToAttribute(SaveType saveType)
        {
            return saveType switch
            {
                SaveType.Mobility => Attribute.Mobility,
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
