using System.Collections.Generic;

namespace Assets.Scripts.Events.EventCard
{
    /// <summary>
    /// Tracks the outcome of an event card resolution.
    /// Used for logging, UI display, and determining narrative flow.
    /// 
    /// Created by card Resolve() and AutoResolve() methods.
    /// Passed to EventCardLogger for RaceHistory integration.
    /// </summary>
    public class CardResolutionResult
    {
        /// <summary>
        /// Whether the card resolution was successful overall.
        /// For hazards: Did the skill check succeed?
        /// For choices: Was the choice successfully executed?
        /// For multi-role: Did the crew pass the collaboration check?
        /// </summary>
        public bool success;
        
        /// <summary>
        /// Narrative description of what happened.
        /// This is what the DM narrates to the players.
        /// </summary>
        public string narrativeOutcome;
        
        /// <summary>
        /// Detailed results for multi-role challenges.
        /// Tracks which roles succeeded/failed their individual checks.
        /// Null for simple hazard cards.
        /// </summary>
        public List<RoleChallengeResult> roleResults;
        
        /// <summary>
        /// Simple constructor for single-outcome cards (hazards, choices).
        /// </summary>
        public CardResolutionResult(bool success, string narrative)
        {
            this.success = success;
            this.narrativeOutcome = narrative;
            this.roleResults = null;
        }
        
        /// <summary>
        /// Full constructor for multi-role cards with per-role results.
        /// </summary>
        public CardResolutionResult(bool success, string narrative, List<RoleChallengeResult> roleResults)
        {
            this.success = success;
            this.narrativeOutcome = narrative;
            this.roleResults = roleResults;
        }
        
        /// <summary>
        /// Checks if this result is dramatic enough to log to RaceHistory.
        /// </summary>
        public bool IsDramatic()
        {
            // Always log failures (they're interesting)
            if (!success) return true;
            
            // Multi-role cards are always dramatic (5-player coordination)
            if (roleResults != null && roleResults.Count > 0) return true;
            
            // Simple successes are less dramatic (don't spam the log)
            return false;
        }
    }
    
    
    
    /// <summary>
    /// Tracks the result of a single role's challenge in a multi-role card.
    /// Used by MultiRoleCard to record per-role outcomes.
    /// </summary>
    public class RoleChallengeResult
    {
        public RoleType roleType;
        public Combat.SkillChecks.SkillCheckResult checkResult;
        
        public RoleChallengeResult(
            RoleType roleType, 
            Combat.SkillChecks.SkillCheckResult checkResult)
        {
            this.roleType = roleType;
            this.checkResult = checkResult;
        }
    }
}
