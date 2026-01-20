//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using Assets.Scripts.Effects;
//using Assets.Scripts.Combat.SkillChecks;
//using Assets.Scripts.Entities.Vehicle.VehicleComponents;

//namespace Assets.Scripts.Events.EventCard.EventCardTypes
//{
//    /// <summary>
//    /// Multi-role challenge card that engages all 5 players simultaneously.
//    /// Creates dramatic team coordination moments.
//    /// ~30% of stage decks.
//    /// 
//    /// Example: "Sudden Rockslide!"
//    /// - Driver: Swerve to dodge (Mobility DC 15)
//    /// - Navigator: Predict pattern (Perception DC 14)
//    /// - Technician: Reinforce hull (Mechanics DC 13)
//    /// - Gunner 1: Shoot rocks (Attack vs AC 18)
//    /// - Gunner 2: Covering fire (Attack vs AC 16)
//    /// - Collaboration: 3+ successes = crew gains "Coordinated" buff
//    /// </summary>
//    [CreateAssetMenu(fileName = "New Multi-Role Card", menuName = "Racing/Event Cards/Multi-Role Card")]
//    public class MultiRoleCard : EventCard
//    {
//        [Header("Role Challenges")]
//        [Tooltip("Individual challenges for each role")]
//        public List<RoleChallenge> roleChallenges = new List<RoleChallenge>();
        
//        [Header("Collaboration")]
//        [Tooltip("How many successes needed for collaboration bonus")]
//        public int requiredSuccesses = 3;
        
//        [Tooltip("Bonus effects if enough roles succeed")]
//        public List<EffectInvocation> collaborationBonus = new List<EffectInvocation>();
        
//        [Tooltip("Effects if too few roles succeed")]
//        public List<EffectInvocation> totalFailureEffects = new List<EffectInvocation>();
        
//        public override CardResolutionResult Resolve(Vehicle vehicle, Stage stage)
//        {
//            var results = new List<RoleChallengeResult>();
//            int successes = 0;
            
//            // Each role attempts their challenge
//            foreach (var challenge in roleChallenges)
//            {
//                Entity roller = GetRollerForRole(vehicle, challenge.targetRole);
                
//                if (roller == null)
//                {
//                    Debug.LogWarning($"[MultiRoleCard] Vehicle {vehicle.vehicleName} has no {challenge.targetRole} component!");
//                    continue;
//                }
                
//                // Perform skill check
//                var checkResult = SkillCheckCalculator.PerformSkillCheck(roller, challenge.checkType, challenge.dc);
                
//                // Apply individual success/failure effects
//                if (checkResult.Succeeded == true)
//                {
//                    successes++;
//                    ApplyEffects(challenge.successEffects, vehicle);
//                }
//                else
//                {
//                    ApplyEffects(challenge.failureEffects, vehicle);
//                }
                
//                // Track result for logging
//                results.Add(new RoleChallengeResult(challenge.targetRole, checkResult));
//            }
            
//            // Check collaboration threshold
//            bool collaborationSuccess = successes >= requiredSuccesses;
            
//            if (collaborationSuccess)
//            {
//                ApplyEffects(collaborationBonus, vehicle);
//                return new CardResolutionResult(
//                    true, 
//                    $"Crew coordination successful! ({successes}/{roleChallenges.Count} roles succeeded)", 
//                    results);
//            }
//            else
//            {
//                ApplyEffects(totalFailureEffects, vehicle);
//                return new CardResolutionResult(
//                    false, 
//                    $"Crew failed to coordinate! (Only {successes}/{requiredSuccesses} succeeded)", 
//                    results);
//            }
//        }
        
//        /// <summary>
//        /// Gets the appropriate component/entity for a given role.
//        /// </summary>
//        private Entity GetRollerForRole(Vehicle vehicle, RoleType role)
//        {
//            return role switch
//            {
//                RoleType.Driver => vehicle.chassis,
//                RoleType.Navigator => vehicle.chassis,
//                RoleType.Technician => vehicle.powerCore,
//                RoleType.Gunner => vehicle.chassis,
//                _ => vehicle.chassis
//            };
//        }
        
//        public override CardResolutionResult AutoResolve(Vehicle vehicle, Stage stage)
//        {
//            // NPCs use same resolution - each role rolls independently
//            return Resolve(vehicle, stage);
//        }
//    }
    
//    /// <summary>
//    /// Represents a challenge for a single role within a MultiRoleCard.
//    /// </summary>
//    [Serializable]
//    public class RoleChallenge
//    {
//        [Tooltip("Which role faces this challenge")]
//        public RoleType targetRole = RoleType.Driver;
        
//        [Tooltip("Description of what this role must do")]
//        public string challengeDescription = "Make a check";
        
//        [Header("Skill Check")]
//        [Tooltip("Type of check required")]
//        public SkillCheckType checkType = SkillCheckType.Mobility;
        
//        [Tooltip("Difficulty class")]
//        public int dc = 15;
        
//        [Header("Effects")]
//        [Tooltip("Effects applied if this role succeeds")]
//        public List<EffectInvocation> successEffects = new List<EffectInvocation>();
        
//        [Tooltip("Effects applied if this role fails")]
//        public List<EffectInvocation> failureEffects = new List<EffectInvocation>();
//    }
//}
