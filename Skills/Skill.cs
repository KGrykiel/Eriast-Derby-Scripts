using UnityEngine;
using System.Collections.Generic;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;
using System.Linq;
using System.Text;

public abstract class Skill : ScriptableObject
{
    public string description;
    public int energyCost = 1;

    [Header("Roll Configuration")]
    [Tooltip("Does this skill require an attack roll to hit?")]
    public bool requiresAttackRoll = true;
    
    [Tooltip("Type of roll (AC for attacks, etc.)")]
    public RollType rollType = RollType.ArmorClass;

    [Header("Effects")]
    [SerializeField]
    public List<EffectInvocation> effectInvocations = new List<EffectInvocation>();
    
    [Header("Component Targeting")]
    [Tooltip("Can this skill target specific vehicle components?")]
    public bool allowsComponentTargeting = false;
    
    [Tooltip("Target component name (leave empty to target chassis/vehicle)")]
    public string targetComponentName = "";
    
    [Tooltip("Penalty when targeting components (applied only to chassis fallback roll)")]
    [Range(0, 10)]
    public int componentTargetingPenalty = 2;

    /// <summary>
    /// Uses the skill without a weapon. For spells and non-weapon abilities.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget)
    {
        return Use(user, mainTarget, null);
    }

    /// <summary>
    /// Uses the skill with an optional weapon. Applies all effect invocations and logs the results.
    /// Supports component targeting for vehicles.
    /// NEW: Skill-level roll, then apply all effects on hit.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget, WeaponComponent weapon)
    {
        // Check for null target
        if (mainTarget == null)
        {
            EventImportance importance = user.controlType == ControlType.Player
                ? EventImportance.Medium
                : EventImportance.Low;

            RaceHistory.Log(
                EventType.SkillUse,
                importance,
                $"{user.vehicleName} attempted to use {name} but there was no valid target",
                user.currentStage,
                user
            ).WithMetadata("skillName", name)
                .WithMetadata("failed", true)
                .WithMetadata("reason", "NoTarget");

            return false;
        }

        // Check for no effects configured
        if (effectInvocations == null || effectInvocations.Count == 0)
        {
            EventImportance importance = user.controlType == ControlType.Player
                ? EventImportance.Medium
                : EventImportance.Low;

            RaceHistory.Log(
                EventType.SkillUse,
                importance,
                $"{user.vehicleName} attempted to use {name} but it has no effects configured",
                user.currentStage,
                user
            ).WithMetadata("skillName", name)
                .WithMetadata("failed", true)
                .WithMetadata("reason", "NoEffects");

            return false;
        }
        
        // Check for component targeting
        // If skill allows component targeting and a target component was selected,
        // use the specialized component targeting logic
        if (allowsComponentTargeting && !string.IsNullOrEmpty(targetComponentName))
        {
            return UseComponentTargeted(user, mainTarget, weapon);
        }

        // Standard skill resolution with SKILL-LEVEL ROLL
        Entity userEntity = user.chassis;
        Entity targetEntity = mainTarget.chassis;
        
        if (userEntity == null || targetEntity == null)
        {
            Debug.LogWarning($"[Skill] {name}: User or target has no chassis!");
            return false;
        }
        
        // Build modifier list for attack roll
        var modifiers = BuildAttackModifiers(user, weapon);
        
        // SKILL-LEVEL ROLL TO HIT (if required)
        RollBreakdown attackRoll = null;
        if (requiresAttackRoll)
        {
            attackRoll = RollUtility.RollToHitWithBreakdown(
                user,
                targetEntity,
                rollType,
                modifiers,
                name
            );
            
            // If miss, log and return false
            if (attackRoll.success != true)
            {
                string attackerName = GetAttackerDisplayName(user);
                string weaponText = weapon != null ? $" with {weapon.name}" : "";
                
                var missEvt = RaceHistory.Log(
                    EventType.Combat,
                    EventImportance.Medium,
                    $"{attackerName} attacks {mainTarget.vehicleName}{weaponText}. <color=#FF4444>Miss</color>",
                    user.currentStage,
                    user, mainTarget
                );
                
                missEvt.WithMetadata("skillName", name)
                       .WithMetadata("result", "miss")
                       .WithMetadata("rollBreakdown", attackRoll.ToDetailedString());
                
                return false;
            }
            
            // Hit! Log attack success
            string attackerNameHit = GetAttackerDisplayName(user);
            string weaponTextHit = weapon != null ? $" with {weapon.name}" : "";
            
            var hitEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                $"{attackerNameHit} attacks {mainTarget.vehicleName}{weaponTextHit}. <color=#44FF44>Hit</color>",
                user.currentStage,
                user, mainTarget
            );
            
            hitEvt.WithMetadata("skillName", name)
                  .WithMetadata("result", "hit")
                  .WithMetadata("rollBreakdown", attackRoll.ToDetailedString());
        }
        
        // Apply ALL effects (they all share the same hit/success)
        bool anyApplied = false;
        Dictionary<Entity, List<DamageBreakdown>> damageByTarget = new Dictionary<Entity, List<DamageBreakdown>>();
        
        foreach (var invocation in effectInvocations)
        {
            if (invocation.effect == null) continue;
            
            // Get actual targets for this effect based on target mode
            List<Entity> targets = BuildTargetList(invocation.targetMode, userEntity, targetEntity, user.currentStage);
            
            foreach (var target in targets)
            {
                // DEBUG: Log which target we're applying to
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                string targetName = targetVehicle != null ? targetVehicle.vehicleName : "Unknown";
                Debug.Log($"[Skill] Applying {invocation.effect.GetType().Name} to {targetName} (targetMode: {invocation.targetMode})");
                
                // Apply effect
                invocation.effect.Apply(userEntity, target, user.currentStage, weapon);
                anyApplied = true;
                
                // Track damage breakdowns by target
                if (invocation.effect is DamageEffect damageEffect && damageEffect.LastBreakdown != null)
                {
                    if (!damageByTarget.ContainsKey(target))
                    {
                        damageByTarget[target] = new List<DamageBreakdown>();
                    }
                    damageByTarget[target].Add(damageEffect.LastBreakdown);
                    
                    // DEBUG: Log damage tracking
                    Debug.Log($"[Skill] Tracked {damageEffect.LastBreakdown.finalDamage} damage to {targetName}");
                }
            }
        }
        
        // Log damage for each target separately
        foreach (var kvp in damageByTarget)
        {
            Entity target = kvp.Key;
            List<DamageBreakdown> breakdowns = kvp.Value;
            
            if (breakdowns.Count > 0)
            {
                int totalDamage = breakdowns.Sum(d => d.finalDamage);
                string attackerNameDmg = GetAttackerDisplayName(user);
                
                // Determine target name
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                string targetName = targetVehicle != null ? targetVehicle.vehicleName : EntityHelpers.GetEntityDisplayName(target);
                
                // Check if this is self-targeting (healing/self-damage)
                bool isSelfTarget = target == userEntity || targetVehicle == user;
                string actionVerb = isSelfTarget ? "takes" : "deals";
                string preposition = isSelfTarget ? "" : $" to {targetName}";
                
                var damageEvt = RaceHistory.Log(
                    EventType.Combat,
                    EventImportance.High,
                    $"{attackerNameDmg} {actionVerb} <color=#FFA500>{totalDamage}</color> damage{preposition}",
                    user.currentStage,
                    user, targetVehicle
                );
                
                damageEvt.WithMetadata("skillName", name)
                        .WithMetadata("damage", totalDamage)
                        .WithMetadata("damageSource", name)
                        .WithMetadata("isSelfTarget", isSelfTarget);
                
                // Build combined damage breakdown
                string combinedBreakdown = BuildCombinedDamageBreakdown(breakdowns, name);
                damageEvt.WithMetadata("damageBreakdown", combinedBreakdown);
            }
        }

        return anyApplied;
    }
    
    /// <summary>
    /// Applies a single effect invocation to appropriate targets based on target mode.
    /// DEPRECATED: Use inline application with target tracking instead.
    /// </summary>
    private bool ApplyEffectInvocation(EffectInvocation invocation, Entity user, Entity mainTarget, Stage context, Object source)
    {
        if (invocation.effect == null) return false;
        
        List<Entity> targets = BuildTargetList(invocation.targetMode, user, mainTarget, context);
        
        bool anyApplied = false;
        foreach (var target in targets)
        {
            invocation.effect.Apply(user, target, context, source);
            anyApplied = true;
        }
        
        return anyApplied;
    }
    
    /// <summary>
    /// Build the list of targets based on target mode.
    /// </summary>
    private List<Entity> BuildTargetList(EffectTargetMode targetMode, Entity user, Entity mainTarget, Stage context)
    {
        List<Entity> targets = new List<Entity>();
        
        switch (targetMode)
        {
            case EffectTargetMode.User:
                targets.Add(user);
                break;
            case EffectTargetMode.Target:
                targets.Add(mainTarget);
                break;
            case EffectTargetMode.Both:
                targets.Add(user);
                targets.Add(mainTarget);
                break;
            case EffectTargetMode.AllInStage:
                Stage stage = context;
                if (stage == null && user is VehicleComponent userComp && userComp.ParentVehicle != null)
                {
                    stage = userComp.ParentVehicle.currentStage;
                }
                
                if (stage != null && stage.vehiclesInStage != null)
                {
                    Vehicle userVehicle = EntityHelpers.GetParentVehicle(user);
                    foreach (var vehicle in stage.vehiclesInStage)
                    {
                        if (vehicle != userVehicle && vehicle.chassis != null)
                        {
                            targets.Add(vehicle.chassis);
                        }
                    }
                }
                break;
        }
        
        return targets;
    }

    /// <summary>
    /// Build the list of attack modifiers with proper source tracking.
    /// </summary>
    protected virtual List<RollModifier> BuildAttackModifiers(Vehicle user, WeaponComponent weapon)
    {
        var builder = RollUtility.BuildModifiers();
        
        // Add caster/vehicle bonus (future: from character stats)
        int casterBonus = GetCasterToHitBonus(user, rollType);
        builder.AddIf(casterBonus != 0, "Caster Bonus", casterBonus, user.vehicleName);
        
        // Add weapon attack bonus
        if (weapon != null && weapon.attackBonus != 0)
        {
            builder.Add("Weapon Attack Bonus", weapon.attackBonus, weapon.name);
        }
        
        // Subclasses can override to add more modifiers
        return builder.Build();
    }

    /// <summary>
    /// Utility: Get caster's to-hit bonus based on roll type.
    /// </summary>
    protected int GetCasterToHitBonus(Vehicle caster, RollType rollType)
    {
        if (rollType == RollType.None)
            return 0;

        // Future: Pull from vehicle/character attributes
        return 0;
    }
    
    /// <summary>
    /// Two-stage component-targeted attack resolution with Pathfinder-style logging.
    /// Creates separate log entries for: component attack, chassis attack (if needed), and damage.
    /// </summary>
    private bool UseComponentTargeted(Vehicle user, Vehicle mainTarget, WeaponComponent weapon)
    {
        VehicleComponent targetComponent = mainTarget.AllComponents
            .FirstOrDefault(c => c.name == targetComponentName);

        // Get display name once at the start
        string attackerName = GetAttackerDisplayName(user);
        string weaponText = weapon != null ? $" with {weapon.name}" : "";

        if (targetComponent == null || targetComponent.isDestroyed)
        {
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{attackerName} tried to target {mainTarget.vehicleName}'s {targetComponentName}, but it's unavailable",
                user.currentStage,
                user, mainTarget
            ).WithMetadata("skillName", name)
             .WithMetadata("targetComponent", targetComponentName)
             .WithMetadata("failed", true)
             .WithMetadata("reason", "ComponentUnavailable");
            
            return false;
        }

        if (!mainTarget.IsComponentAccessible(targetComponent))
        {
            string reason = mainTarget.GetInaccessibilityReason(targetComponent);
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{attackerName} cannot target {mainTarget.vehicleName}'s {targetComponentName}: {reason}",
                user.currentStage,
                user, mainTarget
            ).WithMetadata("skillName", name)
             .WithMetadata("targetComponent", targetComponentName)
             .WithMetadata("failed", true)
             .WithMetadata("reason", "ComponentInaccessible")
             .WithMetadata("accessibilityReason", reason);
            
            return false;
        }

        // Calculate damage and apply effects (respecting targetMode)
        Dictionary<Entity, List<DamageBreakdown>> damageByTarget = new Dictionary<Entity, List<DamageBreakdown>>();
        Entity userEntity = user.chassis;
        Entity targetEntity = targetComponent; // Component is the primary target
        
        foreach (var invocation in effectInvocations)
        {
            if (invocation.effect is DamageEffect damageEffect)
            {
                // Get actual targets based on targetMode
                List<Entity> targets = BuildTargetList(invocation.targetMode, userEntity, targetEntity, user.currentStage);
                
                foreach (var target in targets)
                {
                    var breakdown = damageEffect.formula.ComputeDamageWithBreakdown(weapon);
                    if (breakdown != null && breakdown.rawTotal > 0)
                    {
                        // Apply damage to the correct target
                        DamagePacket packet = DamagePacket.Create(breakdown.rawTotal, breakdown.damageType, user.chassis);
                        int resolved = DamageResolver.ResolveDamage(packet, target);
                        target.TakeDamage(resolved);
                        
                        // Update breakdown with actual resistance info from target
                        ResistanceLevel resistance = target.GetResistance(breakdown.damageType);
                        breakdown.WithResistance(resistance);
                        breakdown.finalDamage = resolved;
                        
                        // Track for logging
                        if (!damageByTarget.ContainsKey(target))
                        {
                            damageByTarget[target] = new List<DamageBreakdown>();
                        }
                        damageByTarget[target].Add(breakdown);
                    }
                }
            }
        }

        if (damageByTarget.Count == 0)
        {
            Debug.LogWarning($"[Skill] {name}: Component targeting skill has no damage effects!");
            return false;
        }

        // Build modifiers for component targeting (NO PENALTY - just base bonuses)
        var componentModifiers = RollUtility.BuildModifiers()
            .AddIf(weapon != null && weapon.attackBonus != 0, "Weapon Attack Bonus", weapon?.attackBonus ?? 0, weapon?.name)
            .Build();

        // Build modifiers for chassis fallback (WITH PENALTY)
        var chassisModifiers = RollUtility.BuildModifiers()
            .AddIf(weapon != null && weapon.attackBonus != 0, "Weapon Attack Bonus", weapon?.attackBonus ?? 0, weapon?.name)
            .Add("Component Targeting Penalty", -componentTargetingPenalty, name)
            .Build();

        // Stage 1: Roll vs Component AC (NO PENALTY)
        int componentAC = mainTarget.GetComponentAC(targetComponent);
        var componentRoll = RollBreakdown.D20(Random.Range(1, 21), RollCategory.Attack);
        foreach (var mod in componentModifiers) componentRoll.WithModifier(mod.name, mod.value, mod.source);
        componentRoll.Against(componentAC, "Component AC");

        if (componentRoll.success == true)
        {
            // HIT THE COMPONENT - Log attack with result
            var attackEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                $"{attackerName} attacks {mainTarget.vehicleName}'s {targetComponentName}{weaponText}. <color=#44FF44>Hit</color>",
                user.currentStage,
                user, mainTarget
            );
            
            attackEvt.WithMetadata("skillName", name)
                    .WithMetadata("targetComponent", targetComponentName)
                    .WithMetadata("result", "hit")
                    .WithMetadata("rollBreakdown", componentRoll.ToDetailedString());
            
            // Log damage for each target separately
            foreach (var kvp in damageByTarget)
            {
                Entity target = kvp.Key;
                List<DamageBreakdown> breakdowns = kvp.Value;
                int totalDamage = breakdowns.Sum(d => d.finalDamage);
                
                // Determine target name
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                string targetName = targetVehicle != null ? targetVehicle.vehicleName : EntityHelpers.GetEntityDisplayName(target);
                
                // Check if this is self-targeting
                bool isSelfTarget = targetVehicle == user;
                
                // Build appropriate log message
                string damageLog;
                if (isSelfTarget)
                {
                    damageLog = $"{attackerName} takes <color=#FFA500>{totalDamage}</color> damage";
                }
                else if (targetVehicle == mainTarget)
                {
                    damageLog = $"{attackerName} deals <color=#FFA500>{totalDamage}</color> damage to {mainTarget.vehicleName}'s {targetComponentName}";
                }
                else
                {
                    damageLog = $"{attackerName} deals <color=#FFA500>{totalDamage}</color> damage to {targetName}";
                }
                
                var damageEvt = RaceHistory.Log(
                    EventType.Combat,
                    EventImportance.High,
                    damageLog,
                    user.currentStage,
                    user, targetVehicle ?? mainTarget
                );
                
                damageEvt.WithMetadata("skillName", name)
                        .WithMetadata("targetComponent", target == targetComponent ? targetComponentName : "other")
                        .WithMetadata("damage", totalDamage)
                        .WithMetadata("damageSource", name)
                        .WithMetadata("isSelfTarget", isSelfTarget);
                
                string combinedBreakdown = BuildCombinedDamageBreakdown(breakdowns, name);
                damageEvt.WithMetadata("damageBreakdown", combinedBreakdown);
            }
            
            return true;
        }

        // MISSED COMPONENT - Log component attack miss
        var componentMissEvt = RaceHistory.Log(
            EventType.Combat,
            EventImportance.Medium,
            $"{attackerName} attacks {mainTarget.vehicleName}'s {targetComponentName}{weaponText}. <color=#FF4444>Miss</color>",
            user.currentStage,
            user, mainTarget
        );
        
        componentMissEvt.WithMetadata("skillName", name)
                       .WithMetadata("targetComponent", targetComponentName)
                       .WithMetadata("result", "miss")
                       .WithMetadata("rollBreakdown", componentRoll.ToDetailedString());

        // Stage 2: Try chassis (WITH PENALTY)
        int chassisAC = mainTarget.GetArmorClass();
        var chassisRoll = RollBreakdown.D20(Random.Range(1, 21), RollCategory.Attack);
        foreach (var mod in chassisModifiers) chassisRoll.WithModifier(mod.name, mod.value, mod.source);
        chassisRoll.Against(chassisAC, "Chassis AC");

        if (chassisRoll.success == true)
        {
            // HIT CHASSIS - Log chassis attack with result
            var chassisAttackEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.Medium,
                $"{attackerName} attacks {mainTarget.vehicleName}'s chassis{weaponText}. <color=#44FF44>Hit</color>",
                user.currentStage,
                user, mainTarget
            );
            
            chassisAttackEvt.WithMetadata("skillName", name)
                           .WithMetadata("targetComponent", "chassis")
                           .WithMetadata("result", "hit")
                           .WithMetadata("rollBreakdown", chassisRoll.ToDetailedString());
            
            // Log damage for each target separately
            foreach (var kvp in damageByTarget)
            {
                Entity target = kvp.Key;
                List<DamageBreakdown> breakdowns = kvp.Value;
                int totalDamage = breakdowns.Sum(d => d.finalDamage);
                
                // Determine target name
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
                string targetName = targetVehicle != null ? targetVehicle.vehicleName : EntityHelpers.GetEntityDisplayName(target);
                
                // Check if this is self-targeting
                bool isSelfTarget = targetVehicle == user;
                
                // Build appropriate log message
                string damageLog;
                if (isSelfTarget)
                {
                    damageLog = $"{attackerName} takes <color=#FFA500>{totalDamage}</color> damage";
                }
                else if (targetVehicle == mainTarget)
                {
                    damageLog = $"{attackerName} deals <color=#FFA500>{totalDamage}</color> damage to {mainTarget.vehicleName}'s chassis";
                }
                else
                {
                    damageLog = $"{attackerName} deals <color=#FFA500>{totalDamage}</color> damage to {targetName}";
                }
                
                var damageEvt = RaceHistory.Log(
                    EventType.Combat,
                    EventImportance.Medium,
                    damageLog,
                    user.currentStage,
                    user, targetVehicle ?? mainTarget
                );
                
                damageEvt.WithMetadata("skillName", name)
                        .WithMetadata("targetComponent", "chassis")
                        .WithMetadata("damage", totalDamage)
                        .WithMetadata("damageSource", name)
                        .WithMetadata("isSelfTarget", isSelfTarget);
                
                string combinedBreakdown = BuildCombinedDamageBreakdown(breakdowns, name);
                damageEvt.WithMetadata("damageBreakdown", combinedBreakdown);
            }
            
            return true;
        }

        // MISSED BOTH - Log chassis attack miss
        var chassisMissEvt = RaceHistory.Log(
            EventType.Combat,
            EventImportance.Medium,
            $"{attackerName} attacks {mainTarget.vehicleName}'s chassis{weaponText}. <color=#FF4444>Miss</color>",
            user.currentStage,
            user, mainTarget
        );
        
        chassisMissEvt.WithMetadata("skillName", name)
                     .WithMetadata("targetComponent", "chassis")
                     .WithMetadata("result", "miss")
                     .WithMetadata("rollBreakdown", chassisRoll.ToDetailedString());
        
        return false;
    }

    /// <summary>
    /// Gets display name for attacker in format: "CharacterName (VehicleName)" or just "VehicleName"
    /// </summary>
    private string GetAttackerDisplayName(Vehicle vehicle)
    {
        // TODO: Get character name from vehicle's driver/pilot component
        // For now, just return vehicle name
        // Future: return $"{characterName} ({vehicle.vehicleName})"
        return vehicle.vehicleName;
    }

    /// <summary>
    /// Builds a combined damage breakdown string from multiple damage effects.
    /// Format: (4d6+5) (×0.5) = 12 fire (resistant)
    /// </summary>
    private string BuildCombinedDamageBreakdown(List<DamageBreakdown> breakdowns, string sourceName)
    {
        if (breakdowns == null || breakdowns.Count == 0)
            return "";
        
        var sb = new StringBuilder();
        int totalDamage = breakdowns.Sum(d => d.finalDamage);
        
        sb.AppendLine($"Damage Result: {totalDamage}");
        sb.AppendLine($"Damage Source: {sourceName}");
        sb.AppendLine();
        
        // List each damage breakdown with inline resistance
        foreach (var breakdown in breakdowns)
        {
            // Show each component in the breakdown
            foreach (var comp in breakdown.components)
            {
                string diceStr = "";
                if (comp.diceCount > 0)
                {
                    diceStr = $"({comp.ToDiceString()})";
                }
                else if (comp.bonus != 0)
                {
                    string sign = comp.bonus >= 0 ? "+" : "";
                    diceStr = $"({sign}{comp.bonus})";
                }
                
                // Add resistance multiplier if applicable
                string resistMod = "";
                string resistLabel = "";
                if (breakdown.resistanceLevel != ResistanceLevel.Normal)
                {
                    resistMod = breakdown.resistanceLevel switch
                    {
                        ResistanceLevel.Vulnerable => " (×2)",
                        ResistanceLevel.Resistant => " (×0.5)",
                        ResistanceLevel.Immune => " (×0)",
                        _ => ""
                    };
                    resistLabel = $" ({breakdown.resistanceLevel.ToString().ToLower()})";
                }
                
                // Format: (4d6+5) (×0.5) = 12 fire (resistant)
                sb.AppendLine($"{diceStr}{resistMod} = {breakdown.finalDamage} {breakdown.damageType.ToString().ToLower()}{resistLabel}");
            }
        }
        
        return sb.ToString().Trim();
    }
}