using UnityEngine;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

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
            SetBaseField(chassis, "baseMaxHealth", maxHealth);
            chassis.SetHealth(maxHealth);
            SetBaseField(chassis, "baseArmorClass", armorClass);
            return chassis;
        }

        public static DriveComponent CreateDrive(
            GameObject parent,
            int maxHealth = 60)
        {
            var obj = new GameObject("Drive");
            obj.transform.SetParent(parent.transform);
            var drive = obj.AddComponent<DriveComponent>();
            SetBaseField(drive, "baseMaxHealth", maxHealth);
            drive.SetHealth(maxHealth);
            return drive;
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
            weapon.roleType = RoleType.Gunner;
            SetBaseField(weapon, "baseMaxHealth", maxHealth);
            weapon.SetHealth(maxHealth);
            SetBaseField(weapon, "baseAttackBonus", attackBonus);
            return weapon;
        }

        public static TechnicianComponent CreateUtility(
            GameObject parent,
            int maxHealth = 50)
        {
            var obj = new GameObject("Utility");
            obj.transform.SetParent(parent.transform);
            var utility = obj.AddComponent<TechnicianComponent>();
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
