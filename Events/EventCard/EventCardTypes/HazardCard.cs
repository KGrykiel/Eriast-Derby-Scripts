using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Effects;
using Assets.Scripts.Combat.Saves;

namespace Assets.Scripts.Events.EventCard.EventCardTypes
{
    /// <summary>
    /// Environmental hazard card that the vehicle must resist via saving throw.
    /// Most common card type (~40% of stage decks).
    /// 
    /// Resolution:
    /// 1. Vehicle's chassis makes a saving throw (passive resistance)
    /// 2. Success: Apply success effects + narrative
    /// 3. Failure: Apply failure effects + narrative
    /// 
    /// Hazards are REACTIVE (you're resisting something happening to you)
    /// vs Skills which are ACTIVE (you're actively doing something).
    /// 
    /// Examples:
    /// - "Rockslide!" - Mobility save DC 15 or take 3d6 bludgeoning damage
    /// - "Icy Patch" - Mobility save DC 14 or lose turn progress
    /// - "Magical Surge" - (Future) Systems save DC 13 or take 2d6 force damage
    /// </summary>
    [CreateAssetMenu(fileName = "New Hazard Card", menuName = "Racing/Event Cards/Hazard Card")]
    public class HazardCard : EventCard
    {
        [Header("Saving Throw")]
        [Tooltip("Type of save required to resist this hazard")]
        public SaveType saveType = SaveType.Mobility;
        
        [Tooltip("Difficulty class for the save")]
        public int dc = 15;
        
        [Header("Effects")]
        [Tooltip("Effects applied on successful save")]
        public List<EffectInvocation> successEffects = new();
        
        [Tooltip("Effects applied on failed save")]
        public List<EffectInvocation> failureEffects = new();
        
        [Header("Narrative")]
        [Tooltip("Narrative text on success")]
        public string successNarrative = "The vehicle evades the hazard!";
        
        [Tooltip("Narrative text on failure")]
        public string failureNarrative = "The vehicle is struck!";
        
        public override CardResolutionResult Resolve(Vehicle vehicle, Stage stage)
        {
            // Chassis represents the vehicle body and makes all saves
            if (vehicle.chassis == null)
            {
                Debug.LogError($"[HazardCard] Vehicle {vehicle.vehicleName} has no chassis!");
                return new CardResolutionResult(false, "No chassis to make save!");
            }
            
            // Perform saving throw
            var saveResult = SaveCalculator.PerformSavingThrow(vehicle.chassis, saveType, dc);
            
            // Apply effects based on result
            if (saveResult.Succeeded == true)
            {
                ApplyEffects(successEffects, vehicle);
                return new CardResolutionResult(true, successNarrative);
            }
            else
            {
                ApplyEffects(failureEffects, vehicle);
                return new CardResolutionResult(false, failureNarrative);
            }
        }
        
        public override CardResolutionResult AutoResolve(Vehicle vehicle, Stage stage)
        {
            // Hazards work identically for NPCs - just roll the save
            return Resolve(vehicle, stage);
        }
    }
}
