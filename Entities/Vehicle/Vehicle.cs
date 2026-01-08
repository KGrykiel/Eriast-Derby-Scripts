using UnityEngine;
using TMPro;
using RacingGame.Events;
using System.Collections.Generic;
using System.Linq;
using EventType = RacingGame.Events.EventType;
using Assets.Scripts.Entities.Vehicle.VehicleComponents;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.Enums;

public enum ControlType
{
    Player,
    AI
}

/// <summary>
/// Vehicle is a CONTAINER/COORDINATOR for Entity components.
/// Vehicle itself is NOT an Entity - its components (chassis, weapons, etc.) ARE entities.
/// 
/// The vehicle aggregates stats from components and provides convenience properties.
/// Damage to "the vehicle" is actually damage to its chassis component.
/// 
/// MODIFIER SYSTEM: Modifiers are stored on individual components, not the vehicle.
/// When a modifier is applied to a vehicle, it's automatically routed to the correct component
/// based on attribute type (HP modifiers → Chassis, Speed modifiers → Drive, etc.)
/// Vehicle.GetActiveModifiers() aggregates modifiers from all components for UI display.
/// 
/// For targeting:
/// - Target Vehicle → actually targets chassis (the "body" of the vehicle)
/// - Target Component → targets specific component directly
/// </summary>
public class Vehicle : MonoBehaviour
{
    [Header("Vehicle Identity")]
    public string vehicleName;

    public ControlType controlType = ControlType.Player;
    [HideInInspector] public Stage currentStage;
    [HideInInspector] public float progress = 0f;
    [HideInInspector] public bool hasLoggedMovementWarningThisTurn = false;

    private TextMeshProUGUI nameLabel;

    [Header("Skills (Legacy - will be component-based)")]
    public System.Collections.Generic.List<Skill> skills = new System.Collections.Generic.List<Skill>();
    
    [Header("Vehicle Components")]
    [Tooltip("Chassis - MANDATORY structural component (stores HP and AC)")]
    public ChassisComponent chassis;
    
    [Tooltip("Power Core - MANDATORY power supply component (stores energy)")]
    public PowerCoreComponent powerCore;
    
    [Tooltip("Optional components (Drive, Weapons, Utilities, etc.)")]
    public List<VehicleComponent> optionalComponents = new List<VehicleComponent>();

    /// <summary>
    /// Get all active modifiers from all components (for UI display).
    /// Aggregates modifiers from chassis, power core, drive, weapons, etc.
    /// </summary>
    public List<AttributeModifier> GetActiveModifiers()
    {
        List<AttributeModifier> allModifiers = new List<AttributeModifier>();
        
        foreach (var component in AllComponents)
        {
            allModifiers.AddRange(component.GetModifiers());
        }
        
        return allModifiers;
    }
    
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

    // ==================== ENTITY ACCESS (for targeting systems) ====================
    
    /// <summary>
    /// Get the primary targetable entity for this vehicle (the chassis).
    /// When skills target "a vehicle", they actually target this entity.
    /// </summary>
    public Entity GetPrimaryTarget()
    {
        return chassis;
    }
    
    /// <summary>
    /// Get all targetable entities on this vehicle.
    /// This includes all non-destroyed components.
    /// </summary>
    public List<Entity> GetAllTargetableEntities()
    {
        return AllComponents
            .Where(c => !c.isDestroyed)
            .Cast<Entity>()
            .ToList();
    }

    /// <summary>
    /// Get all components (mandatory + optional).
    /// </summary>
    public List<VehicleComponent> AllComponents
    {
        get
        {
            List<VehicleComponent> all = new List<VehicleComponent>();
            if (chassis != null) all.Add(chassis);
            if (powerCore != null) all.Add(powerCore);
            if (optionalComponents != null) all.AddRange(optionalComponents);
            return all;
        }
    }

    void Awake()
    {
        // Initialize components first
        InitializeComponents();
        
        // Components self-initialize their values in their own Awake() methods
        // No need to manually set health or energy here

        var labelTransform = transform.Find("NameLabel");
        if (labelTransform != null)
        {
            nameLabel = labelTransform.GetComponent<TextMeshProUGUI>();
        }
    }
    
    void OnValidate()
    {
        // Validate component space usage
        if (chassis == null) return;
        
        int netSpace = CalculateNetComponentSpace();
        
        if (netSpace > 0)
        {
            Debug.LogError($"[Vehicle] {vehicleName} exceeds component space by {netSpace} units! " +
                          $"Remove components or upgrade chassis.");
        }
    }
    
    private int CalculateNetComponentSpace()
    {
        int total = 0;
        foreach (var component in AllComponents)
        {
            if (component != null)
            {
                total += component.componentSpace;
            }
        }
        return total;
    }
    
    // ==================== CONVENIENCE PROPERTIES (delegate to components) ====================
    
    /// <summary>
    /// Vehicle health IS chassis health.
    /// Reading this property returns chassis health.
    /// Writing to this property damages the chassis.
    /// Clamped between 0 and maxHealth.
    /// </summary>
    public int health
    {
        get
        {
            if (chassis == null)
                return 0; // No chassis = no health
            return chassis.health;
        }
        set
        {
            if (chassis == null)
                return; // Cannot set health without chassis
            
            // Clamp to valid range (use GetMaxHP for modifier-adjusted max)
            chassis.health = Mathf.Clamp(value, 0, chassis.GetMaxHP());
            
            // Check for destruction
            if (chassis.health <= 0 && !chassis.isDestroyed)
            {
                chassis.isDestroyed = true;
                DestroyVehicle();
            }
        }
    }
    
    /// <summary>
    /// Maximum health (chassis base HP + bonuses from other components + modifiers).
    /// Delegates to chassis.GetMaxHP() which applies modifiers.
    /// </summary>
    public int maxHealth
    {
        get
        {
            if (chassis == null) return 0;
            return chassis.GetMaxHP();
        }
    }
    
    /// <summary>
    /// Current energy (stored in power core).
    /// Clamped between 0 and maxEnergy.
    /// </summary>
    public int energy
    {
        get
        {
            if (powerCore == null) return 0;
            return powerCore.currentEnergy;
        }
        set
        {
            if (powerCore == null) return;
            // Clamp to valid range (use GetMaxEnergy for modifier-adjusted max)
            powerCore.currentEnergy = Mathf.Clamp(value, 0, powerCore.GetMaxEnergy());
        }
    }
    
    /// <summary>
    /// Maximum energy capacity (with modifiers applied).
    /// Delegates to powerCore.GetMaxEnergy() which applies modifiers.
    /// </summary>
    public int maxEnergy
    {
        get
        {
            if (powerCore == null) return 0;
            return powerCore.GetMaxEnergy();
        }
    }
    
    /// <summary>
    /// Energy regeneration rate (with modifiers applied).
    /// Delegates to powerCore.GetEnergyRegen() which applies modifiers.
    /// </summary>
    public float energyRegen
    {
        get
        {
            if (powerCore == null) return 0f;
            return powerCore.GetEnergyRegen();
        }
    }
    
    /// <summary>
    /// Vehicle speed (from drive component with modifiers applied).
    /// </summary>
    public float speed
    {
        get
        {
            var drive = optionalComponents.OfType<DriveComponent>().FirstOrDefault();
            if (drive != null && !drive.isDestroyed && !drive.isDisabled)
            {
                return drive.GetSpeed();
            }
            return 0f;
        }
    }
    
    /// <summary>
    /// Vehicle armor class (chassis base AC + bonuses + modifiers).
    /// Delegates to chassis.GetTotalAC() which applies modifiers.
    /// </summary>
    public int armorClass
    {
        get
        {
            if (chassis == null) return 10; // Default AC
            return chassis.GetTotalAC();
        }
    }
    
    /// <summary>
    /// Damage the vehicle. Actually damages the chassis component.
    /// Use TakeDamageToComponent() to damage specific components.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (chassis == null)
        {
            Debug.LogWarning($"[Vehicle] {vehicleName} has no chassis to damage!");
            return;
        }
        
        // Damage chassis directly (chassis is an Entity)
        chassis.TakeDamage(amount);
        
        // Check if chassis destroyed → vehicle destroyed
        if (chassis.isDestroyed)
        {
            DestroyVehicle();
        }
    }
    
    /// <summary>
    /// Get armor class for attack rolls.
    /// Returns modifier-adjusted AC from chassis.
    /// </summary>
    public int GetArmorClass()
    {
        return armorClass;
    }

    void Start()
    {
        TestFullIntegration();
        if (nameLabel != null)
        {
            nameLabel.text = vehicleName;
        }
        MoveToCurrentStage();
    }

    // ==================== MODIFIER SYSTEM (Component-Centric) ====================

    /// <summary>
    /// Add a modifier to the appropriate component.
    /// Automatically routes modifier based on attribute type.
    /// </summary>
    public void AddModifier(AttributeModifier modifier)
    {
        VehicleComponent targetComponent = ResolveModifierTarget(modifier.Attribute);
        
        if (targetComponent == null)
        {
            Debug.LogWarning($"[Vehicle] {vehicleName}: No component found to apply {modifier.Attribute} modifier!");
            return;
        }
        
        targetComponent.AddModifier(modifier);
    }

    /// <summary>
    /// Remove a specific modifier from all components.
    /// Searches all components for the modifier and removes it.
    /// </summary>
    public void RemoveModifier(AttributeModifier modifier)
    {
        foreach (var component in AllComponents)
        {
            component.RemoveModifier(modifier);
        }
    }

    /// <summary>
    /// Remove modifiers from all components by source.
    /// </summary>
    public void RemoveModifiersFromSource(UnityEngine.Object source, bool localOnly)
    {
        int totalRemoved = 0;
        
        foreach (var component in AllComponents)
        {
            var modifiers = component.GetModifiers();
            
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var mod = modifiers[i];
                bool shouldRemove = mod.Source == source && (!localOnly || mod.local);
                
                if (shouldRemove)
                {
                    component.RemoveModifier(mod);
                    totalRemoved++;
                }
            }
        }
        
        if (totalRemoved > 0)
        {
            string sourceText = source != null ? source.name : "unknown source";
            string localText = localOnly ? " (local only)" : "";
            
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Low,
                $"{vehicleName} lost {totalRemoved} modifier(s) from {sourceText}{localText}",
                currentStage,
                this
            ).WithMetadata("removedCount", totalRemoved)
             .WithMetadata("source", sourceText)
             .WithMetadata("localOnly", localOnly);
        }
    }
    
    /// <summary>
    /// Update all component modifiers (decrement durations, remove expired).
    /// Call at end of vehicle's turn.
    /// </summary>
    public void UpdateModifiers()
    {
        foreach (var component in AllComponents)
        {
            component.UpdateModifiers();
        }
    }
    
    /// <summary>
    /// Resolve which component should receive a modifier based on attribute type.
    /// CORE ROUTING LOGIC - determines component ownership of attributes.
    /// PUBLIC: Used by skills to route effects to correct components.
    /// </summary>
    public VehicleComponent ResolveModifierTarget(Attribute attribute)
    {
        return attribute switch
        {
            // Chassis attributes
            Attribute.MaxHealth => chassis,
            Attribute.ArmorClass => chassis,
            Attribute.MagicResistance => chassis,
            
            // Power Core attributes
            Attribute.MaxEnergy => powerCore,
            Attribute.EnergyRegen => powerCore,
            
            // Drive attributes
            Attribute.Speed => optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
            
            // Default: chassis (fallback for unknown attributes)
            _ => chassis
        };
    }
    
    /// <summary>
    /// Route an effect to the appropriate component based on effect type.
    /// Used by skills to resolve targeting before applying effects.
    /// 
    /// Routing rules:
    /// - DamageEffect → Chassis (the vehicle's "body")
    /// - AttributeModifierEffect → Component based on attribute (Speed→Drive, Energy→PowerCore, etc.)
    /// - Other effects → Chassis (default)
    /// 
    /// This allows easy extension: add new effect types or component types
    /// by adding new cases here without modifying effect classes.
    /// </summary>
    public Entity RouteEffectTarget(IEffect effect, Entity defaultTarget)
    {
        if (effect == null)
            return defaultTarget;
        
        // Route modifiers based on attribute type
        if (effect is AttributeModifierEffect modifierEffect)
        {
            VehicleComponent component = ResolveModifierTarget(modifierEffect.attribute);
            return component ?? defaultTarget;
        }
        
        // Route damage to chassis (the vehicle's structural component)
        if (effect is DamageEffect)
        {
            return chassis ?? defaultTarget;
        }
        
        // Route restoration effects based on resource type
        if (effect is ResourceRestorationEffect restorationEffect)
        {
            // Health restoration → Chassis
            // Energy restoration → Power Core
            // Future: Could check restorationEffect.resourceType if we add that field
            return chassis ?? defaultTarget;
        }
        
        // Default: use provided target (usually chassis)
        return defaultTarget;
    }

    // ==================== ATTRIBUTE SYSTEM ====================

    /// <summary>
    /// Get attribute value from components (with modifiers already applied).
    /// Components calculate their own values including modifiers.
    /// This method routes to the correct component property.
    /// </summary>
    public float GetAttribute(Attribute attr)
    {
        return attr switch
        {
            Attribute.Speed => speed,
            Attribute.ArmorClass => armorClass,
            Attribute.MagicResistance => 10, // TODO: Move to component
            Attribute.MaxHealth => maxHealth,
            Attribute.MaxEnergy => maxEnergy,
            Attribute.EnergyRegen => energyRegen,
            _ => 0f
        };
    }

    // ==================== STAGE MANAGEMENT ====================

    public void UpdateNameLabel()
    {
        if (nameLabel != null)
        {
            nameLabel.text = vehicleName;
        }
    }

    public void SetCurrentStage(Stage stage)
    {
        if (currentStage != null)
        {
            currentStage.TriggerLeave(this);
        }
        
        Stage previousStage = currentStage;
        currentStage = stage;
        MoveToCurrentStage();
        
        if (currentStage != null)
        {
            currentStage.TriggerEnter(this);
            
            // Stage transition logging removed - handled by TurnController
            // This prevents duplicate logs
            
            // Check for finish line crossing (unique event - keep this)
            if (stage.isFinishLine)
            {
                RaceHistory.Log(
                    EventType.FinishLine,
                    EventImportance.Critical,
                    $"[FINISH] {vehicleName} crossed the finish line!",
                    stage,
                    this
                );
            }
        }
    }

    private void MoveToCurrentStage()
    {
        if (currentStage != null)
        {
            Vector3 stagePos = currentStage.transform.position;
            transform.position = new Vector3(stagePos.x, stagePos.y, transform.position.z);
        }
    }

    // ==================== VEHICLE DESTRUCTION ====================

    public void DestroyVehicle()
    {
        if (Status == VehicleStatus.Destroyed) return; // Already destroyed
        
        VehicleStatus oldStatus = Status;
        Status = VehicleStatus.Destroyed;
        
        // Log destruction
        RaceHistory.Log(
            EventType.Destruction,
            EventImportance.Critical,
            $"[DEAD] {vehicleName} has been destroyed!",
            currentStage,
            this
        ).WithMetadata("previousStatus", oldStatus.ToString())
         .WithMetadata("finalHealth", health)
         .WithMetadata("finalStage", currentStage?.stageName ?? "None");
        
        bool wasLeading = DetermineIfLeading();
        if (wasLeading)
        {
            RaceHistory.Log(
                EventType.TragicMoment,
                EventImportance.High,
                $"[TRAGIC] {vehicleName} was destroyed while in the lead!",
                currentStage,
                this
            );
        }
        
        var turnController = FindFirstObjectByType<TurnController>();
        if (turnController != null)
            turnController.RemoveDestroyedVehicle(this);
    }
    
    /// <summary>
    /// Checks if this vehicle was in the lead when destroyed.
    /// Used for dramatic event detection.
    /// </summary>
    private bool DetermineIfLeading()
    {
        // Simple heuristic: check if progress is high
        // In a real implementation, you'd check against RaceLeaderboard
        return progress > 50f; // Placeholder logic
    }
    
    // ==================== ENERGY MANAGEMENT ====================
    
    /// <summary>
    /// Regenerates energy at the start of turn.
    /// Delegates to PowerCore component.
    /// Cannot regenerate if PowerCore is destroyed.
    /// </summary>
    public void RegenerateEnergy()
    {
        if (powerCore == null || powerCore.isDestroyed)
        {
            // No power regeneration without a functional power core
            if (powerCore != null && powerCore.isDestroyed)
            {
                RaceHistory.Log(
                    EventType.Resource,
                    EventImportance.Medium,
                    $"{vehicleName} cannot regenerate energy - Power Core destroyed!",
                    currentStage,
                    this
                ).WithMetadata("powerCoreDestroyed", true)
                 .WithMetadata("currentEnergy", 0);
            }
            return;
        }
        
        // Delegate to power core
        powerCore.RegenerateEnergy();
    }
    
    // ==================== COMPONENT SYSTEM METHODS ====================

    /// <summary>
    /// Discover and initialize all vehicle components.
    /// Called automatically in Awake().
    /// </summary>
    private void InitializeComponents()
    {
        // Find all VehicleComponent child objects
        var allFoundComponents = GetComponentsInChildren<VehicleComponent>();

        // Auto-categorize components
        foreach (var component in allFoundComponents)
        {
            // Initialize component with vehicle reference
            component.Initialize(this);

            // Auto-assign mandatory components if not manually set
            if (component is ChassisComponent && chassis == null)
            {
                chassis = component as ChassisComponent;
            }
            else if (component is PowerCoreComponent && powerCore == null)
            {
                powerCore = component as PowerCoreComponent;
            }
            else
            {
                // Add to optional components if not already there
                if (!optionalComponents.Contains(component))
                {
                    optionalComponents.Add(component);
                }
            }
        }

        // Validate mandatory components
        if (chassis == null)
        {
            Debug.LogWarning($"[Vehicle] {vehicleName} has no Chassis component! Vehicle stats will be incomplete.");
        }

        if (powerCore == null)
        {
            Debug.LogWarning($"[Vehicle] {vehicleName} has no Power Core component! Vehicle will have no power.");
        }

        // Log component discovery
        Debug.Log($"[Vehicle] {vehicleName} initialized with {AllComponents.Count} component(s)");
    }

    // ==================== ROLE DISCOVERY ====================

    /// <summary>
    /// Get all available roles on this vehicle (emergent from components).
    /// Roles are discovered dynamically based on which components enable them.
    /// Returns one VehicleRole struct per component (even if role names are the same).
    /// Example: Two weapons = two separate Gunner roles with different skills/characters.
    /// </summary>
    public List<VehicleRole> GetAvailableRoles()
    {
        List<VehicleRole> roles = new List<VehicleRole>();

        foreach (var component in AllComponents)
        {
            // Skip if component doesn't enable a role or is destroyed
            if (!component.enablesRole || component.isDestroyed)
                continue;

            roles.Add(new VehicleRole
            {
                roleName = component.roleName,
                sourceComponent = component,
                assignedCharacter = component.assignedCharacter,
                availableSkills = component.GetAllSkills()
            });
        }

        return roles;
    }

    /// <summary>
    /// Get all skills from all components with matching role name.
    /// WARNING: This aggregates skills from multiple components if they share a role name!
    /// For player UI, use GetAvailableRoles() instead to keep components separate.
    /// This is primarily for AI use where it doesn't care which weapon/component it uses.
    /// </summary>
    public List<Skill> GetSkillsForRole(string roleName)
    {
        return AllComponents
            .Where(c => c.enablesRole && c.roleName == roleName && c.CanProvideSkills())
            .SelectMany(c => c.GetAllSkills())
            .ToList();
    }

    /// <summary>
    /// Get all skills from all components (for legacy compatibility or AI use).
    /// Aggregates skills from all operational components.
    /// </summary>
    public List<Skill> GetAllComponentSkills()
    {
        return AllComponents
            .Where(c => c.CanProvideSkills())
            .SelectMany(c => c.GetAllSkills())
            .ToList();
    }

    // ==================== TURN MANAGEMENT ====================

    /// <summary>
    /// Check if all role-enabling components have acted this turn.
    /// Used to determine when player turn should end.
    /// Note: Skipping counts as acting (sets hasActedThisTurn = true).
    /// </summary>
    public bool AllComponentsActed()
    {
        return AllComponents
            .Where(c => c.enablesRole && !c.isDestroyed && !c.isDisabled)
            .All(c => c.hasActedThisTurn);
    }

    /// <summary>
    /// Reset all components for new turn.
    /// Call at start of each round.
    /// </summary>
    public void ResetComponentsForNewTurn()
    {
        hasLoggedMovementWarningThisTurn = false;
        
        foreach (var component in AllComponents)
        {
            component.ResetTurnState();
        }
    }

    // ==================== COMPONENT STAT AGGREGATION ====================

    /// <summary>
    /// Get aggregated stat from all components.
    /// Example: GetComponentStat("HP") returns total HP from all components.
    /// Used internally by GetAttribute() to add component bonuses.
    /// </summary>
    public float GetComponentStat(string statName)
    {
        float total = 0f;

        foreach (var component in AllComponents)
        {
            var modifiers = component.GetStatModifiers();
            total += modifiers.GetStat(statName);
        }

        return total;
    }

    // ==================== COMPONENT ACCESSIBILITY ====================

    /// <summary>
    /// Check if a component is currently accessible for targeting.
    /// External components always accessible.
    /// Protected/Shielded components require shield destruction.
    /// Internal components require chassis damage (threshold set per component).
    /// </summary>
    public bool IsComponentAccessible(VehicleComponent target)
    {
        if (target == null || target.isDestroyed)
            return false;
        
        // External components are always accessible
        if (target.exposure == ComponentExposure.External)
            return true;
        
        // Protected/Shielded components: check if shielding component is destroyed
        if ((target.exposure == ComponentExposure.Protected || target.exposure == ComponentExposure.Shielded) 
            && target.shieldedByComponent != null)
        {
            // Accessible if shield is destroyed
            return target.shieldedByComponent.isDestroyed;
        }
        
        // Internal components: requires chassis damage based on component's threshold
        if (target.exposure == ComponentExposure.Internal)
        {
            if (chassis == null) return true; // Fallback if no chassis
            
            // Calculate chassis damage percentage (1.0 = fully damaged, 0.0 = undamaged)
            float chassisDamagePercent = 1f - ((float)chassis.health / (float)chassis.maxHealth);
            
            // Accessible if chassis damage >= threshold
            return chassisDamagePercent >= target.internalAccessThreshold;
        }
        
        // Default: accessible
        return true;
    }
    
    /// <summary>
    /// Get the reason why a component cannot be accessed (for UI display).
    /// Returns null if component is accessible.
    /// </summary>
    public string GetInaccessibilityReason(VehicleComponent target)
    {
        if (target == null || target.isDestroyed)
            return "Component destroyed";
        
        if (IsComponentAccessible(target))
            return null; // Accessible
        
        // Protected/Shielded components
        if ((target.exposure == ComponentExposure.Protected || target.exposure == ComponentExposure.Shielded) 
            && target.shieldedByComponent != null)
        {
            if (!target.shieldedByComponent.isDestroyed)
                return $"Shielded by {target.shieldedByComponent.name}";
        }
        
        // Internal components
        if (target.exposure == ComponentExposure.Internal)
        {
            if (chassis != null)
            {
                float chassisDamagePercent = 1f - ((float)chassis.health / (float)chassis.maxHealth);
                if (chassisDamagePercent < target.internalAccessThreshold)
                {
                    int requiredDamagePercent = Mathf.RoundToInt(target.internalAccessThreshold * 100f);
                    return $"Chassis must be {requiredDamagePercent}% damaged";
                }
            }
        }
        
        return "Cannot target";
    }

    // ==================== OPERATIONAL STATUS ====================

    /// <summary>
    /// Check if this vehicle can still move/function.
    /// Requires operational chassis and power core.
    /// </summary>
    public bool IsOperational()
    {
        // Chassis destroyed = vehicle destroyed
        if (chassis == null || chassis.isDestroyed)
            return false;
        
        // Power core destroyed = no power, cannot function
        if (powerCore == null || powerCore.isDestroyed)
            return false;
        
        // Vehicle is in destroyed state
        if (Status == VehicleStatus.Destroyed)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Get reason why vehicle is non-operational (for UI/logging).
    /// Returns null if operational.
    /// </summary>
    public string GetNonOperationalReason()
    {
        if (chassis == null)
            return "No chassis installed";
        if (chassis.isDestroyed)
            return "Chassis destroyed";
        
        if (powerCore == null)
            return "No power core installed";
        if (powerCore.isDestroyed)
            return "Power core destroyed - no power";
        
        if (Status == VehicleStatus.Destroyed)
            return "Vehicle destroyed";
        
        return null; // Operational
    }

    /// <summary>
    /// Check if this vehicle can move between stages.
    /// Requires operational chassis, power core, AND drive component.
    /// </summary>
    public bool CanMove()
    {
        if (!IsOperational()) 
            return false;
        
        // Check if vehicle has an operational drive component
        var driveComponent = optionalComponents.FirstOrDefault(c => c is DriveComponent);
        if (driveComponent == null || driveComponent.isDestroyed || driveComponent.isDisabled)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Get reason why vehicle cannot move.
    /// Returns null if vehicle can move.
    /// </summary>
    public string GetCannotMoveReason()
    {
        string operationalReason = GetNonOperationalReason();
        if (operationalReason != null)
            return operationalReason;
        
        var driveComponent = optionalComponents.FirstOrDefault(c => c is DriveComponent);
        if (driveComponent == null)
            return "No drive system installed";
        if (driveComponent.isDestroyed)
            return "Drive system destroyed";
        if (driveComponent.isDisabled)
            return "Drive system disabled";
        
        return null;
    }

    /// <summary>
    /// Get AC for targeting a specific component.
    /// Returns modifier-adjusted AC if component is a ChassisComponent.
    /// </summary>
    public int GetComponentAC(VehicleComponent targetComponent)
    {
        if (targetComponent == null)
            return GetArmorClass(); // Fallback to chassis AC
        
        // If targeting chassis, use modifier-adjusted AC
        if (targetComponent is ChassisComponent chassis)
        {
            return chassis.GetTotalAC();
        }
        
        // For other components, use base AC (they don't have GetTotalAC yet)
        // TODO: Add GetTotalAC() to all VehicleComponents for modifier support
        return targetComponent.armorClass;
    }

    // ==================== DEBUG/TESTING ====================

    void TestFullIntegration()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"=== INTEGRATION TEST: {vehicleName} ===");
        sb.AppendLine($"MaxHP: {GetAttribute(Attribute.MaxHealth)} (expected: 300)");
        sb.AppendLine($"AC: {GetArmorClass()} (expected: 32)");
        sb.AppendLine($"PowerCapacity: {GetComponentStat("PowerCapacity")}");

        var roles = GetAvailableRoles();
        sb.AppendLine($"Roles: {roles.Count} (expected: 3)");
        foreach (var role in roles)
        {
            string charName = role.assignedCharacter?.characterName ?? "NONE";
            sb.AppendLine($"  - {role.roleName}: {charName} ({role.availableSkills.Count} skills)");
        }

        sb.AppendLine($"AllComponentsActed: {AllComponentsActed()} (expected: false)");
        sb.AppendLine($"=== END TEST ===");
        
        Debug.Log(sb.ToString());
    }
}
