using UnityEngine;
using TMPro;
using RacingGame.Events;
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
    private System.Collections.Generic.List<AttributeModifier> activeModifiers = new System.Collections.Generic.List<AttributeModifier>();

    public ControlType controlType = ControlType.Player;
    [HideInInspector] public Stage currentStage;
    [HideInInspector] public float progress = 0f;

    private TextMeshPro nameLabel;
    [Header("Skills")]
    public System.Collections.Generic.List<Skill> skills = new System.Collections.Generic.List<Skill>();

    public System.Collections.Generic.List<AttributeModifier> GetActiveModifiers()
    {
        return activeModifiers;
    }
    
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

    void Awake()
    {
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
            case Attribute.Speed: baseValue = speed; break;
            case Attribute.ArmorClass: baseValue = armorClass; break;
            case Attribute.MagicResistance: baseValue = magicResistance; break;
            case Attribute.MaxHealth: baseValue = maxHealth; break;
            case Attribute.MaxEnergy: baseValue = maxEnergy; break;
            case Attribute.EnergyRegen: baseValue = energyRegen; break;
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
    /// Override TakeDamage to log damage events.
    /// </summary>
    public override void TakeDamage(int amount)
    {
        int oldHealth = health;
        base.TakeDamage(amount); // Call parent implementation
        int actualDamage = oldHealth - health;
        
        // Determine importance based on amount and status
        EventImportance importance = DetermineDamageImportance(actualDamage);
        
        // Build description
        string description = $"{vehicleName} took {actualDamage} damage ({health}/{maxHealth} HP)";
        
        // Add context
        if (health <= 0)
        {
            description += " - DESTROYED!";
            importance = EventImportance.Critical;
        }
        else if (health <= maxHealth * 0.25f && oldHealth > maxHealth * 0.25f)
        {
            description += " - CRITICAL HEALTH!";
            if (importance > EventImportance.High)
                importance = EventImportance.High;
        }
        else if (health <= maxHealth * 0.5f && oldHealth > maxHealth * 0.5f)
        {
            description += " - Bloodied!";
        }
        
        // Log damage event
        RaceHistory.Log(
            EventType.Combat,
            importance,
            description,
            currentStage,
            this
        ).WithMetadata("damage", actualDamage)
         .WithMetadata("oldHealth", oldHealth)
         .WithMetadata("newHealth", health)
         .WithMetadata("maxHealth", maxHealth)
         .WithMetadata("healthPercent", (float)health / maxHealth);
    }
    
    /// <summary>
    /// Determines importance of damage event based on amount and vehicle status.
    /// </summary>
    private EventImportance DetermineDamageImportance(int damage)
    {
        // Player taking damage is always important
        if (controlType == ControlType.Player)
        {
            if (damage > 20 || health <= maxHealth * 0.3f)
                return EventImportance.High;
            return EventImportance.Medium;
        }
        
        // NPC damage
        if (damage > 30)
            return EventImportance.High;
        
        if (damage > 15)
            return EventImportance.Medium;
        
        return EventImportance.Low;
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
            
            // Log stage transition (already logged in TurnController, so keep this Debug level)
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Debug,
                $"{vehicleName} entered {stage.stageName}",
                stage,
                this
            ).WithMetadata("previousStage", previousStage?.stageName ?? "None")
             .WithMetadata("newStage", stage.stageName);
            
            // Check for finish line crossing
            if (stage.isFinishLine)
            {
                RaceHistory.Log(
                    EventType.FinishLine,
                    EventImportance.Critical,
                    $"[FINISH] {vehicleName} crossed the finish line!",
                    stage,
                    this
                );
                
                SimulationLogger.LogEvent($"{vehicleName} crossed the finish line!");
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
        
        SimulationLogger.LogEvent($"{vehicleName} has been destroyed!");
        
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
        
        // Remove from turn order
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
}
