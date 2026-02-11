using UnityEngine;

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
        /// Always logs damage even if 0 (important for showing IMMUNE feedback).
        /// 
        /// CALLER RESPONSIBILITY: Both result and target must be non-null.
        /// Resistance has already been resolved by DamageCalculator.
        /// </summary>
        /// <param name="result">Pre-calculated damage result with resistance applied (must not be null)</param>
        /// <param name="target">Entity receiving damage (must not be null)</param>
        /// <param name="attacker">Entity dealing damage (null for environmental)</param>
        /// <param name="causalSource">What caused this (for logging "Destroyed by X")</param>
        /// <param name="sourceType">Category of damage source</param>
        /// <returns>The same result (for chaining/inspection)</returns>
        public static DamageResult Apply(
            DamageResult result,
            Entity target,
            Entity attacker = null,
            Object causalSource = null,
            DamageSource sourceType = DamageSource.Ability)
        {
            // ALWAYS emit event for logging, even if damage is 0
            // This is critical for showing IMMUNE/RESISTANT feedback to players
            CombatEventBus.EmitDamage(result, attacker, target, causalSource, sourceType);

            // Apply damage to target
            if (result.FinalDamage > 0)
            {
                target.TakeDamage(result.FinalDamage);
            }

            return result;
        }
    }
}
