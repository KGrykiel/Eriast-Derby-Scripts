using Assets.Scripts.Consumables;
using Assets.Scripts.Core;
using Assets.Scripts.Combat.Damage;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes
{
    /// <summary>Damage-dealing system. enables Gunner role. Can have multiple per vehicle.</summary>
    public class WeaponComponent : VehicleComponent
    {
        [Header("Weapon Damage")]
        [Tooltip("Base damage formula (dice, bonus, damage type)")]
        public DamageFormula baseDamageFormula = new() { baseDice = 1, dieSize = 8, bonus = 0, damageType = DamageType.Physical };

        [Header("Weapon Stats")]
        [SerializeField]
        [Tooltip("Attack bonus (for to-hit rolls) (base value before modifiers)")]
        private int baseAttackBonus = 0;

        [Header("Ammunition")]
        [Tooltip("Weapon tags used for ammo compatibility checks, e.g. [\"Ranged\", \"Ballistic\"].")]
        public List<string> weaponTags = new();

        [Tooltip("Currently loaded ammunition type. Null = standard/none.")]
        public AmmunitionType loadedAmmunition;

        // ==================== STAT ACCESSORS ====================

        public int GetBaseAttackBonus() => baseAttackBonus;

        public DamageFormula GetDamageFormula() => baseDamageFormula;
        public int GetAttackBonus() => StatCalculator.GatherAttributeValue(this, EntityAttribute.AttackBonus);

        public override int GetBaseValue(EntityAttribute attribute)
        {
            return attribute switch
            {
                EntityAttribute.AttackBonus => baseAttackBonus,
                _ => base.GetBaseValue(attribute)
            };
        }

        /// <summary>
        /// Default values for convenience, to be edited manually.
        /// </summary>
        void Reset()
        {
            gameObject.name = "Weapon";
            componentType = ComponentType.Weapon;

            baseMaxHealth = 40;
            health = 40;
            baseArmorClass = 14;
            baseComponentSpace = 150;
            basePowerDrawPerTurn = 5;

            baseDamageFormula = new DamageFormula { baseDice = 1, dieSize = 8, bonus = 0, damageType = DamageType.Physical };
            baseAttackBonus = 0;
            roleType = RoleType.Gunner;
        }

        void Awake()
        {
            componentType = ComponentType.Weapon;
            roleType = RoleType.Gunner;
        }

        public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
        {
            var stats = new List<VehicleComponentUI.DisplayStat>();

            int dice = baseDamageFormula.baseDice;
            int dieSize = baseDamageFormula.dieSize;
            int bonus = baseDamageFormula.bonus;

            string dmgStr = bonus != 0
                ? $"{dice}d{dieSize}{bonus:+0;-0}"
                : $"{dice}d{dieSize}";
            stats.Add(VehicleComponentUI.DisplayStat.Simple("Damage", "DMG", dmgStr));

            int modifiedAttackBonus = GetAttackBonus();
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Attack Bonus", "HIT", EntityAttribute.AttackBonus, baseAttackBonus, modifiedAttackBonus));

            stats.AddRange(base.GetDisplayStats());

            return stats;
        }
    }
}