using UnityEngine;
using TMPro;
using RacingGame.Events;
using System.Collections.Generic;
using System.Linq;
using EventType = RacingGame.Events.EventType;

public enum ControlType
{
    Player,
    AI
}

public class Vehicle : Entity
{
    public string vehicleName;

    [Header("Base Attributes (editable per vehicle)")]
    public float speed = 4f;
    public int magicResistance = 10; // Temporary
    public int maxEnergy = 50;
    public float energyRegen = 1f;

    [HideInInspector] public int energy; // Current energy, can be modified by skills or events

    // Active modifiers
    private List<AttributeModifier> activeModifiers = new List<AttributeModifier>();

    public ControlType controlType = ControlType.Player;
    [HideInInspector] public Stage currentStage;
    [HideInInspector] public float progress = 0f;

    private TextMeshPro nameLabel;

    [Header("Skills")]
    public System.Collections.Generic.List<Skill> skills = new System.Collections.Generic.List<Skill>();
    
    [Header("Vehicle Components")]
    [Tooltip("Chassis - MANDATORY structural component")]
    public ChassisComponent chassis;
    
    [Tooltip("Power Core - MANDATORY power supply component")]
    public PowerCoreComponent powerCore;
    
    [Tooltip("Optional components (Drive, Weapons, Utilities, etc.)")]
    public List<VehicleComponent> optionalComponents = new List<VehicleComponent>();

    public List<AttributeModifier> GetActiveModifiers()
    {
        return activeModifiers;
    }
    
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

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
        
        // Set health and energy to max at start
        health = maxHealth;
        energy = maxEnergy;

        var labelTransform = transform.Find("NameLabel");
        if (labelTransform != null)
        {
            nameLabel = labelTransform.GetComponent<TextMeshPro>();
        }
    }

    void Start()
    {
        if (nameLabel != null)
        {
            nameLabel.text = vehicleName;
        }
        MoveToCurrentStage();
    }

    public void AddModifier(AttributeModifier modifier)
    {
        activeModifiers.Add(modifier);
        
        // Log modifier addition
        string durText = modifier.DurationTurns > 0 ? $" for {modifier.DurationTurns} turns" : " (permanent)";
        string sourceText = modifier.Source != null ? $" from {modifier.Source.name}" : "";
        
        RaceHistory.Log(
            EventType.Modifier,
            EventImportance.Low,
            $"{vehicleName} gained {modifier.Type} {modifier.Attribute} {modifier.Value:+0;-0}{durText}{sourceText}",
            currentStage,
            this
        ).WithMetadata("modifierType", modifier.Type.ToString())
         .WithMetadata("attribute", modifier.Attribute.ToString())
         .WithMetadata("value", modifier.Value)
         .WithMetadata("duration", modifier.DurationTurns);
    }

    public void RemoveModifier(AttributeModifier modifier)
    {
        if (activeModifiers.Remove(modifier))
        {
            // Log modifier removal
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Low,
                $"{vehicleName} lost {modifier.Type} {modifier.Attribute} {modifier.Value:+0;-0} modifier",
                currentStage,
                this
            ).WithMetadata("modifierType", modifier.Type.ToString())
             .WithMetadata("attribute", modifier.Attribute.ToString())
             .WithMetadata("removed", true);
        }
    }

    public void RemoveModifiersFromSource(UnityEngine.Object source, bool localOnly)
    {
        int removedCount = 0;
        
        if (localOnly)
        {
            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                if (activeModifiers[i].Source == source && activeModifiers[i].local)
                {
                    activeModifiers.RemoveAt(i);
                    removedCount++;
                }
            }
        }
        else
        {
            // Remove all modifiers from the source, regardless of local flag
            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                if (activeModifiers[i].Source == source)
                {
                    activeModifiers.RemoveAt(i);
                    removedCount++;
                }
            }
        }
        
        // Log bulk removal if any were removed
        if (removedCount > 0)
        {
            string sourceText = source != null ? source.name : "unknown source";
            string localText = localOnly ? " (local only)" : "";
            
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Low,
                $"{vehicleName} lost {removedCount} modifier(s) from {sourceText}{localText}",
                currentStage,
                this
            ).WithMetadata("removedCount", removedCount)
             .WithMetadata("source", sourceText)
             .WithMetadata("localOnly", localOnly);
        }
    }
    
    public void UpdateModifiers()
    {
        int expiredCount = 0;
        
        for (int i = activeModifiers.Count - 1; i >= 0; i--)
        {
            var mod = activeModifiers[i];
            if (mod.DurationTurns == 0)
            {
                activeModifiers.RemoveAt(i);
                expiredCount++;
                continue;
            }
            if (mod.DurationTurns > 0)
                mod.DurationTurns--;
        }
        
        // Log if modifiers expired (low importance, won't clutter feed)
        if (expiredCount > 0)
        {
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Debug,
                $"{vehicleName}: {expiredCount} modifier(s) expired",
                currentStage,
                this
            ).WithMetadata("expiredCount", expiredCount);
        }
    }

    public float GetAttribute(Attribute attr)
    {
        float baseValue = 0f;
        switch (attr)
        {
            case Attribute.Speed: 
                baseValue = speed;
                // Add component speed bonuses
                baseValue += GetComponentStat(VehicleStatModifiers.StatNames.Speed);
                break;
            case Attribute.ArmorClass: 
                baseValue = armorClass;
                // Add component AC bonuses
                baseValue += GetComponentStat(VehicleStatModifiers.StatNames.AC);
                break;
            case Attribute.MagicResistance: 
                baseValue = magicResistance; 
                break;
            case Attribute.MaxHealth: 
                baseValue = maxHealth;
                // Add component HP bonuses
                baseValue += GetComponentStat(VehicleStatModifiers.StatNames.HP);
                break;
            case Attribute.MaxEnergy: 
                baseValue = maxEnergy; 
                break;
            case Attribute.EnergyRegen: 
                baseValue = energyRegen; 
                break;
            default: return 0f;
        }

        float flatBonus = 0f;
        float percentMultiplier = 1f;

        foreach (var mod in activeModifiers)
        {
            if (mod.Attribute != attr) continue;
            if (mod.Type == ModifierType.Flat)
                flatBonus += mod.Value;
            else if (mod.Type == ModifierType.Percent)
                percentMultiplier *= (1f + mod.Value / 100f);
        }

        return (baseValue + flatBonus) * percentMultiplier;
    }

    public override int GetArmorClass()
    {
        // Use the attribute system for dynamic armor class
        return Mathf.RoundToInt(GetAttribute(Attribute.ArmorClass));
    }

    /// <summary>
    /// Override TakeDamage - NO LOGGING, just apply damage.
    /// Logging is handled by the caller (Skill, Stage hazard, etc.)
    /// </summary>
    public override void TakeDamage(int amount)
    {
        int oldHealth = health;
        base.TakeDamage(amount); // Call parent implementation
        
        // NO LOGGING HERE - prevents duplicates
        // Status changes (Bloodied, Critical, Destroyed) are logged by the caller
    }

    protected override void OnEntityDestroyed()
    {
        DestroyVehicle();
    }

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

    public void DestroyVehicle()
    {
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
    
    /// <summary>
    /// Regenerates energy at the start of turn.
    /// Call this from TurnController or GameManager.
    /// </summary>
    public void RegenerateEnergy()
    {
        int oldEnergy = energy;
        int maxEnergyValue = (int)GetAttribute(Attribute.MaxEnergy);
        float regenRate = GetAttribute(Attribute.EnergyRegen);
        
        energy = Mathf.Min(energy + (int)regenRate, maxEnergyValue);
        
        int regenAmount = energy - oldEnergy;
        
        if (regenAmount > 0)
        {
            RaceHistory.Log(
                EventType.Resource,
                EventImportance.Debug,
                $"{vehicleName} regenerated {regenAmount} energy ({energy}/{maxEnergyValue})",
                currentStage,
                this
            ).WithMetadata("regenAmount", regenAmount)
             .WithMetadata("currentEnergy", energy)
             .WithMetadata("maxEnergy", maxEnergyValue);
        }
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
        foreach (var component in AllComponents)
        {
            component.ResetTurnState();
        }
    }

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

    // ==================== COMBAT STUBS (Phase 6 - Deferred) ====================

    /// <summary>
    /// STUB: Component-targeted damage (Phase 6).
    /// TODO: Implement two-stage hit rolls:
    ///   1. Roll vs Component AC (harder)
    ///   2. If miss, roll vs Vehicle AC (easier) → damages vehicle pool
    /// TODO: Define chassis/power core destruction = vehicle unusable
    /// TODO: Implement damage distribution between component HP and vehicle HP
    /// </summary>
    public void TakeDamageToComponent(VehicleComponent targetComponent, int damage)
    {
        // STUB: For now, just damage the component
        if (targetComponent != null)
        {
            targetComponent.TakeDamage(damage);

            // TODO: Also damage vehicle HP pool? Or only on critical components?
            // TODO: Check if chassis or power core destroyed → DestroyVehicle()
        }
    }

    /// <summary>
    /// STUB: Get AC for targeting a specific component (Phase 6).
    /// TODO: Account for component location/exposure (hidden power core vs exposed cannon)
    /// TODO: Apply modifiers based on vehicle orientation, cover, etc.
    /// </summary>
    public int GetComponentAC(VehicleComponent targetComponent)
    {
        // STUB: For now, just return component's AC
        return targetComponent != null ? targetComponent.componentAC : GetArmorClass();
    }
}
