using UnityEngine;
using Assets.Scripts.Combat;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Central utility for applying damage through the pipeline.
    /// Handles resistance resolution and HP reduction.
    /// 
    /// DESIGN PRINCIPLE: All damage flows through a DamageBreakdown.
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
        /// Apply damage using a pre-calculated breakdown.
        /// This is the CORE method - all other methods funnel through here.
        /// Emits DamageEvent for logging (aggregated by CombatEventBus).
        /// </summary>
        /// <param name="breakdown">Pre-calculated damage with dice info</param>
        /// <param name="target">Entity receiving damage</param>
        /// <param name="attacker">Entity dealing damage (null for environmental)</param>
        /// <param name="causalSource">What caused this (for logging "Destroyed by X")</param>
        /// <param name="sourceType">Category of damage source</param>
        /// <returns>Updated breakdown with resistance and final damage</returns>
        public static DamageBreakdown Apply(
            DamageBreakdown breakdown,
            Entity target,
            Entity attacker = null,
            Object causalSource = null,
            DamageSource sourceType = DamageSource.Ability)
        {
            // Validate inputs
            if (target == null)
            {
                Debug.LogWarning("[DamageApplicator] Target is null, cannot apply damage");
                return breakdown ?? CreateEmptyBreakdown(DamageType.Physical);
            }
            
            if (breakdown == null || breakdown.rawTotal <= 0)
            {
                return breakdown ?? CreateEmptyBreakdown(DamageType.Physical);
            }
            
            // Create damage packet for resolution
            DamagePacket packet;
            if (attacker != null)
            {
                packet = DamagePacket.FromAttacker(
                    breakdown.rawTotal, 
                    breakdown.damageType, 
                    attacker, 
                    causalSource, 
                    sourceType);
            }
            else
            {
                packet = DamagePacket.Environmental(
                    breakdown.rawTotal, 
                    breakdown.damageType, 
                    causalSource, 
                    sourceType);
            }
            
            // Resolve damage (applies resistances)
            var (resolvedDamage, resistance) = DamageResolver.ResolveDamageWithResistance(packet, target);
            
            // Update breakdown with resolution results
            breakdown.WithResistance(resistance);
            breakdown.finalDamage = resolvedDamage;
            
            // Apply damage to target
            target.TakeDamage(resolvedDamage);
            
            // Emit event for logging (CombatEventBus handles aggregation)
            if (resolvedDamage > 0)
            {
                CombatEventBus.EmitDamage(breakdown, attacker, target, causalSource, sourceType);
            }
            
            return breakdown;
        }
        
        // ==================== CONVENIENCE METHODS (Create Breakdown + Apply) ====================
        
        /// <summary>
        /// Apply flat damage from an attacker.
        /// Creates a breakdown with no dice (0d0 + flat bonus).
        /// </summary>
        public static DamageBreakdown ApplyFlat(
            int damage,
            DamageType damageType,
            Entity target,
            Entity attacker,
            Object causalSource,
            DamageSource sourceType = DamageSource.Ability)
        {
            var breakdown = CreateFlatBreakdown(damage, damageType, causalSource?.name ?? "Unknown");
            return Apply(breakdown, target, attacker, causalSource, sourceType);
        }
        
        /// <summary>
        /// Apply flat environmental damage (no attacker).
        /// Creates a breakdown with no dice (0d0 + flat bonus).
        /// </summary>
        public static DamageBreakdown ApplyEnvironmentalFlat(
            int damage,
            DamageType damageType,
            Entity target,
            Object causalSource,
            DamageSource sourceType = DamageSource.Effect)
        {
            var breakdown = CreateFlatBreakdown(damage, damageType, causalSource?.name ?? "Environmental");
            return Apply(breakdown, target, null, causalSource, sourceType);
        }
        
        /// <summary>
        /// Apply dice-based environmental damage (e.g., DoT with 1d6 fire per turn).
        /// Creates a breakdown with dice information.
        /// </summary>
        public static DamageBreakdown ApplyEnvironmentalDice(
            int diceCount,
            int dieSize,
            int bonus,
            DamageType damageType,
            Entity target,
            Object causalSource,
            DamageSource sourceType = DamageSource.Effect)
        {
            // Roll dice
            int rolled = RollUtility.RollDice(diceCount, dieSize);
            
            // Create breakdown with dice info
            var breakdown = DamageBreakdown.Create(damageType);
            breakdown.AddComponent(
                causalSource?.name ?? "Environmental",
                diceCount,
                dieSize,
                bonus,
                rolled,
                causalSource?.name ?? "Environmental");
            
            return Apply(breakdown, target, null, causalSource, sourceType);
        }
        
        // ==================== BREAKDOWN CREATION HELPERS ====================
        
        /// <summary>
        /// Create a flat damage breakdown (0 dice + bonus).
        /// Models flat damage as "0d0+X" for consistency.
        /// </summary>
        private static DamageBreakdown CreateFlatBreakdown(int damage, DamageType damageType, string sourceName)
        {
            var breakdown = DamageBreakdown.Create(damageType);
            
            if (damage != 0)
            {
                // Model as 0d0 + flat bonus for consistency
                breakdown.AddComponent(sourceName, 0, 0, damage, 0, sourceName);
            }
            
            return breakdown;
        }
        
        /// <summary>
        /// Create an empty breakdown for zero damage cases.
        /// </summary>
        private static DamageBreakdown CreateEmptyBreakdown(DamageType damageType)
        {
            var breakdown = DamageBreakdown.Create(damageType);
            breakdown.WithResistance(ResistanceLevel.Normal);
            breakdown.finalDamage = 0;
            return breakdown;
        }
    }
}
