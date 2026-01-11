using UnityEngine;
using Assets.Scripts.Combat;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Central utility for applying damage through the pipeline.
    /// Handles resistance resolution and HP reduction.
    /// 
    /// DESIGN PRINCIPLE: All damage flows through a DamageResult.
    /// Even flat damage is modeled as "0 dice + flat bonus" for consistency.
    /// 
    /// LOGGING: Emits DamageEvent to CombatEventBus instead of logging directly.
    /// This allows aggregation of multiple damage sources in skill execution.
    /// 
    /// This is the SINGLE ENTRY POINT for all damage application:
    /// - Skills (via DamageEffect)
    /// - Status effects (DoT)
    /// - Environmental hazards (Stage)
    /// - Event cards
    /// </summary>
    public static class DamageApplicator
    {
        // ==================== PRIMARY API ====================
        
        /// <summary>
        /// Apply damage using a pre-calculated result.
        /// This is the CORE method - all other methods funnel through here.
        /// Emits DamageEvent for logging (aggregated by CombatEventBus).
        /// </summary>
        /// <param name="result">Pre-calculated damage with dice info</param>
        /// <param name="target">Entity receiving damage</param>
        /// <param name="attacker">Entity dealing damage (null for environmental)</param>
        /// <param name="causalSource">What caused this (for logging "Destroyed by X")</param>
        /// <param name="sourceType">Category of damage source</param>
        /// <returns>Updated result with resistance and final damage</returns>
        public static DamageResult Apply(
            DamageResult result,
            Entity target,
            Entity attacker = null,
            Object causalSource = null,
            DamageSource sourceType = DamageSource.Ability)
        {
            // Validate inputs
            if (target == null)
            {
                Debug.LogWarning("[DamageApplicator] Target is null, cannot apply damage");
                return result ?? CreateEmptyResult(DamageType.Physical);
            }
            
            if (result == null || result.RawTotal <= 0)
            {
                return result ?? CreateEmptyResult(DamageType.Physical);
            }
            
            // Get resistance level from target and apply it
            ResistanceLevel resistance = DamageCalculator.GetResistance(target, result.damageType);
            DamageCalculator.ApplyResistance(result, resistance);
            
            // Apply damage to target
            target.TakeDamage(result.finalDamage);
            
            // Emit event for logging (CombatEventBus handles aggregation)
            if (result.finalDamage > 0)
            {
                CombatEventBus.EmitDamage(result, attacker, target, causalSource, sourceType);
            }
            
            return result;
        }
        
        // ==================== CONVENIENCE METHODS (Create Result + Apply) ====================
        
        /// <summary>
        /// Apply flat damage from an attacker.
        /// Creates a result with no dice (0d0 + flat bonus).
        /// </summary>
        public static DamageResult ApplyFlat(
            int damage,
            DamageType damageType,
            Entity target,
            Entity attacker,
            Object causalSource,
            DamageSource sourceType = DamageSource.Ability)
        {
            var result = DamageCalculator.FromFlat(damage, damageType, causalSource?.name ?? "Unknown");
            return Apply(result, target, attacker, causalSource, sourceType);
        }
        
        /// <summary>
        /// Apply flat environmental damage (no attacker).
        /// Creates a result with no dice (0d0 + flat bonus).
        /// </summary>
        public static DamageResult ApplyEnvironmentalFlat(
            int damage,
            DamageType damageType,
            Entity target,
            Object causalSource,
            DamageSource sourceType = DamageSource.Effect)
        {
            var result = DamageCalculator.FromFlat(damage, damageType, causalSource?.name ?? "Environmental");
            return Apply(result, target, null, causalSource, sourceType);
        }
        
        /// <summary>
        /// Apply dice-based environmental damage (e.g., DoT with 1d6 fire per turn).
        /// Creates a result with dice information.
        /// </summary>
        public static DamageResult ApplyEnvironmentalDice(
            int diceCount,
            int dieSize,
            int bonus,
            DamageType damageType,
            Entity target,
            Object causalSource,
            DamageSource sourceType = DamageSource.Effect)
        {
            var result = DamageCalculator.FromDice(diceCount, dieSize, bonus, damageType, causalSource?.name ?? "Environmental");
            return Apply(result, target, null, causalSource, sourceType);
        }
        
        // ==================== RESULT CREATION HELPERS ====================
        
        /// <summary>
        /// Create an empty result for zero damage cases.
        /// </summary>
        private static DamageResult CreateEmptyResult(DamageType damageType)
        {
            var result = DamageResult.Create(damageType);
            DamageCalculator.ApplyResistance(result, ResistanceLevel.Normal);
            return result;
        }
    }
}
