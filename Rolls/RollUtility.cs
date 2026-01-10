using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central utility class for ALL dice rolling in the game.
/// All random dice rolls MUST go through this class for consistency.
/// </summary>
public static class RollUtility
{
    // ==================== CORE DICE ROLLING ====================
    
    /// <summary>
    /// Roll a single die of the specified size (e.g., d6, d8, d20).
    /// </summary>
    public static int RollDie(int dieSize)
    {
        return Random.Range(1, dieSize + 1);
    }
    
    /// <summary>
    /// Roll multiple dice and return the sum.
    /// Example: RollDice(2, 6) rolls 2d6 and returns the total.
    /// </summary>
    public static int RollDice(int diceCount, int dieSize)
    {
        if (diceCount <= 0 || dieSize <= 0) return 0;
        
        int total = 0;
        for (int i = 0; i < diceCount; i++)
        {
            total += Random.Range(1, dieSize + 1);
        }
        return total;
    }
    
    /// <summary>
    /// Roll dice with a flat bonus.
    /// Example: RollDiceWithBonus(2, 6, 3) rolls 2d6+3.
    /// </summary>
    public static int RollDiceWithBonus(int diceCount, int dieSize, int bonus)
    {
        return RollDice(diceCount, dieSize) + bonus;
    }
    
    // ==================== ATTACK ROLLS (D20) ====================
    
    /// <summary>
    /// Roll to hit an entity with full breakdown tracking.
    /// </summary>
    public static RollBreakdown RollToHitWithBreakdown(Vehicle user, Entity target, TargetNumberType targetType, List<RollModifier> modifiers = null, string contextName = null)
    {
        // Get target vehicle from entity (if it's a component)
        Vehicle vehicleTarget = GetParentVehicle(target);
        
        // Roll the d20
        int baseRoll = RollDie(20);
        
        // Create breakdown
        RollBreakdown breakdown = RollBreakdown.D20(baseRoll, RollCategory.Attack);
        
        // Add all modifiers
        if (modifiers != null)
        {
            foreach (var mod in modifiers)
            {
                breakdown.WithModifier(mod.name, mod.value, mod.source);
            }
        }
        
        // Determine target value
        int targetValue = 0;
        string targetName = "AC";
        
        if (vehicleTarget != null)
        {
            switch (targetType)
            {
                case TargetNumberType.ArmorClass:
                    if (target is VehicleComponent component)
                    {
                        // Use modifier-adjusted AC for components
                        targetValue = vehicleTarget.GetComponentAC(component);
                    }
                    else
                    {
                        targetValue = vehicleTarget.armorClass;
                    }
                    targetName = "AC";
                    break;
                case TargetNumberType.MagicResistance:
                    targetValue = 10; // TODO: Move to component
                    targetName = "MR";
                    break;
            }
        }
        
        // Set target and determine success
        breakdown.Against(targetValue, targetName);
     
        return breakdown;
    }
    
    // ==================== DAMAGE ROLLS ====================
    
    /// <summary>
    /// Roll damage dice and return a DamageBreakdown with full details.
    /// This is the SINGLE SOURCE OF TRUTH for damage rolling.
    /// </summary>
    public static DamageBreakdown RollDamageWithBreakdown(int diceCount, int dieSize, int bonus, string componentName, DamageType damageType, string source = null)
    {
        // Roll dice using core method
        int rolled = RollDice(diceCount, dieSize);
        
        var breakdown = DamageBreakdown.Create(damageType)
            .AddComponent(componentName, diceCount, dieSize, bonus, rolled, source)
            .WithResistance(ResistanceLevel.Normal);
           
        return breakdown;
    }
    
    /// <summary>
    /// Roll damage from a weapon component with full breakdown.
    /// </summary>
    public static DamageBreakdown RollWeaponDamageWithBreakdown(WeaponComponent weapon)
    {
        if (weapon == null)
        {
            return DamageBreakdown.Create(DamageType.Physical).WithResistance(ResistanceLevel.Normal);
        }
        
        return RollDamageWithBreakdown(
            weapon.damageDice, 
            weapon.damageDieSize, 
            weapon.damageBonus, 
            "Weapon",
            weapon.damageType,
            weapon.name
        );
    }
    
    // ==================== SKILL CHECKS ====================

    /// <summary>
    /// Performs a skill check with full breakdown.
    /// </summary>
    public static RollBreakdown SkillCheckWithBreakdown(List<RollModifier> modifiers, int difficulty, string checkName = "Skill Check")
    {
        int baseRoll = RollDie(20);
        
        var breakdown = RollBreakdown.D20(baseRoll, RollCategory.SkillCheck);
        
        if (modifiers != null)
        {
            foreach (var mod in modifiers)
            {
                breakdown.WithModifier(mod.name, mod.value, mod.source);
            }
        }
        
        breakdown.Against(difficulty, "DC");
        
        return breakdown;
    }
    
    // ==================== HELPERS ====================
    
    /// <summary>
    /// Get parent vehicle from an entity (if it's a VehicleComponent).
    /// </summary>
    private static Vehicle GetParentVehicle(Entity entity)
    {
        if (entity is VehicleComponent component)
        {
            return component.ParentVehicle;
        }
        return null;
    }

    /// <summary>
    /// Create a modifier list builder for fluent API.
    /// </summary>
    public static ModifierListBuilder BuildModifiers()
    {
        return new ModifierListBuilder();
    }
}

/// <summary>
/// Fluent builder for creating modifier lists.
/// </summary>
public class ModifierListBuilder
{
    private List<RollModifier> modifiers = new List<RollModifier>();
    
    public ModifierListBuilder Add(string name, int value, string source = null)
    {
        if (value != 0)
        {
            modifiers.Add(new RollModifier(name, value, source));
        }
        return this;
    }
    
    public ModifierListBuilder AddIf(bool condition, string name, int value, string source = null)
    {
        if (condition && value != 0)
        {
            modifiers.Add(new RollModifier(name, value, source));
        }
        return this;
    }
    
    public List<RollModifier> Build()
    {
        return modifiers;
    }
    
    public static implicit operator List<RollModifier>(ModifierListBuilder builder)
    {
        return builder.Build();
    }
}

/// <summary>
/// Defines what target number to roll against.
/// Used internally by roll methods - skills should use SkillRollType instead.
/// </summary>
public enum TargetNumberType
{
    ArmorClass,       // Roll vs target's AC
    MagicResistance,  // Roll vs target's MR
    DifficultyClass   // Roll vs fixed DC (future: saving throws, skill checks)
}
