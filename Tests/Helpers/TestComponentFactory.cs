using UnityEngine;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;

namespace Assets.Scripts.Tests.Helpers
{
    public static class TestComponentFactory
    {
        public static ChassisComponent CreateChassis(
            GameObject parent,
            int maxHealth = 100,
            int armorClass = 10)
        {
            var obj = new GameObject("Chassis");
            obj.transform.SetParent(parent.transform);
            var chassis = obj.AddComponent<ChassisComponent>();
            chassis.componentType = ComponentType.Chassis;
            SetBaseField(chassis, "baseMaxHealth", maxHealth);
            chassis.SetHealth(maxHealth);
            SetBaseField(chassis, "baseArmorClass", armorClass);
            return chassis;
        }

        public static WeaponComponent CreateWeapon(
            GameObject parent,
            string weaponName = "TestWeapon",
            int attackBonus = 0,
            int maxHealth = 50)
        {
            var obj = new GameObject(weaponName);
            obj.transform.SetParent(parent.transform);
            var weapon = obj.AddComponent<WeaponComponent>();
            weapon.componentType = ComponentType.Weapon;
            weapon.roleType = RoleType.Gunner;
            SetBaseField(weapon, "baseMaxHealth", maxHealth);
            weapon.SetHealth(maxHealth);
            SetBaseField(weapon, "baseAttackBonus", attackBonus);
            return weapon;
        }

        public static CustomComponent CreateUtility(
            GameObject parent,
            int maxHealth = 50)
        {
            var obj = new GameObject("Utility");
            obj.transform.SetParent(parent.transform);
            var utility = obj.AddComponent<CustomComponent>();
            utility.componentType = ComponentType.Utility;
            SetBaseField(utility, "baseMaxHealth", maxHealth);
            utility.SetHealth(maxHealth);
            return utility;
        }

        public static PowerCoreComponent CreatePowerCore(
            GameObject parent,
            int maxHealth = 50)
        {
            var obj = new GameObject("PowerCore");
            obj.transform.SetParent(parent.transform);
            var powerCore = obj.AddComponent<PowerCoreComponent>();
            powerCore.componentType = ComponentType.PowerCore;
            SetBaseField(powerCore, "baseMaxHealth", maxHealth);
            powerCore.SetHealth(maxHealth);
            return powerCore;
        }

        private static void SetBaseField(Entity entity, string fieldName, int value)
        {
            var field = typeof(Entity).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(entity, value);
                return;
            }
            var componentField = entity.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            componentField?.SetValue(entity, value);
        }
    }
}
