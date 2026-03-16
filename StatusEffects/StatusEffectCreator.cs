#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Assets.Scripts.Entities;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Rolls.Advantage;

namespace Assets.Scripts.StatusEffects
{
    /// <summary>
    /// Convenience editor class to create new StatusEffect assets with preset configurations based on type.
    /// </summary>
    public static class StatusEffectCreator
    {
        private const string MenuPath = "Assets/Create/Racing/Status Effect/";

        [MenuItem(MenuPath + "Buff")]
        public static void CreateBuff()
        {
            CreateStatusEffectAsset("NewBuff", MakeBlankBuff());
        }

        [MenuItem(MenuPath + "Debuff")]
        public static void CreateDebuff()
        {
            CreateStatusEffectAsset("NewDebuff", MakeBlankDebuff());
        }

        [MenuItem(MenuPath + "Damage over Time")]
        public static void CreateDoT()
        {
            CreateStatusEffectAsset("NewDoT", MakeBlankDoT());
        }

        [MenuItem(MenuPath + "Healing over Time")]
        public static void CreateHoT()
        {
            CreateStatusEffectAsset("NewHoT", MakeBlankHoT());
        }

        private static void CreateStatusEffectAsset(string defaultName, StatusEffect definition)
        {
            string path = "Assets";

            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                    path = System.IO.Path.GetDirectoryName(path);
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{defaultName}.asset");

            AssetDatabase.CreateAsset(definition, assetPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = definition;
            EditorUtility.FocusProjectWindow();
        }

        private static StatusEffect MakeBlankBuff()
            => Make("New Buff", baseDuration: 3,
                modifiers: Mods(Mod(Attribute.Mobility, ModifierType.Flat, 2f)));

        private static StatusEffect MakeBlankDebuff()
            => Make("New Debuff", baseDuration: 3,
                modifiers: Mods(Mod(Attribute.Mobility, ModifierType.Flat, -2f)));

        private static StatusEffect MakeBlankDoT()
            => Make("New DoT", baseDuration: 3,
                periodicEffects: Periodics(
                    Periodic(PeriodicEffectType.Damage, new DamageFormula { baseDice = 1, dieSize = 6, bonus = 0, damageType = DamageType.Fire })));

        private static StatusEffect MakeBlankHoT()
            => Make("New HoT", baseDuration: 3,
                periodicEffects: Periodics(Periodic(PeriodicEffectType.Healing, 5)));

        #region named effect catalogue
        // ==================== NAMED STATUS EFFECT CATALOGUE ====================
        // Define all named effects here. Run "Assets/Racing/Regenerate All Status Effects" to write them to disk.
        // First run: drag generated assets to skill/card definitions once. Subsequent regenerations overwrite the
        // same .asset file (same GUID) so all references are preserved automatically.

        private const string StatusEffectsFolder = "Assets/Content/StatusEffects";

        [MenuItem("Assets/Racing/Regenerate All Status Effects")]
        public static void RegenerateAllStatusEffects()
        {
            System.IO.Directory.CreateDirectory(StatusEffectsFolder);

            RegenerateStatusEffect(DefineBurning());
            RegenerateStatusEffect(DefineBleeding());
            RegenerateStatusEffect(DefineOverheating());
            RegenerateStatusEffect(DefineStunned());
            RegenerateStatusEffect(DefineSlowed());
            RegenerateStatusEffect(DefineFortified());
            RegenerateStatusEffect(DefineInspired());
            RegenerateStatusEffect(DefineBlinded());
            RegenerateStatusEffect(DefineVulnerable());
            RegenerateStatusEffect(DefineRegenerating());

            AssetDatabase.SaveAssets();
            Debug.Log("[StatusEffectCreator] All status effects regenerated.");
        }

        // ---- Effect definitions ----

        // Pattern: DoT with feature requirement.
        private static StatusEffect DefineBurning()
            => Make("Burning", baseDuration: 3,
                required: EntityFeature.IsFlammable,
                periodicEffects: Periodics(
                    Periodic(PeriodicEffectType.Damage, new DamageFormula { baseDice = 1, dieSize = 6, bonus = 0, damageType = DamageType.Fire })));

        // Pattern: DoT, no feature requirement.
        private static StatusEffect DefineBleeding()
            => Make("Bleeding", baseDuration: 3,
                periodicEffects: Periodics(
                    Periodic(PeriodicEffectType.Damage, new DamageFormula { baseDice = 1, dieSize = 4, bonus = 0, damageType = DamageType.Physical })));

        // Pattern: modifier + periodic drain, requires electronic.
        private static StatusEffect DefineOverheating()
            => Make("Overheating", baseDuration: 3,
                required: EntityFeature.IsElectronic,
                modifiers: Mods(
                    Mod(Attribute.MaxSpeed, ModifierType.Flat, -20f),
                    Mod(Attribute.EnergyRegen, ModifierType.Flat, -2f)),
                periodicEffects: Periodics(
                    Periodic(PeriodicEffectType.EnergyDrain, 3)));

        // Pattern: behavioral — prevents all actions, short duration.
        private static StatusEffect DefineStunned()
            => Make("Stunned", baseDuration: 1,
                behavioral: Behavioral(preventsActions: true));

        // Pattern: modifier — reduces speed and mobility.
        private static StatusEffect DefineSlowed()
            => Make("Slowed", baseDuration: 2,
                modifiers: Mods(
                    Mod(Attribute.MaxSpeed, ModifierType.Multiplier, 0.5f),
                    Mod(Attribute.Mobility, ModifierType.Flat, -3f)));

        // Pattern: multi-modifier buff — armor and integrity bonus.
        private static StatusEffect DefineFortified()
            => Make("Fortified", baseDuration: 2,
                modifiers: Mods(
                    Mod(Attribute.ArmorClass, ModifierType.Flat, 3f),
                    Mod(Attribute.Integrity, ModifierType.Flat, 2f)));

        // Pattern: advantage grant on character checks.
        private static StatusEffect DefineInspired()
            => Make("Inspired", baseDuration: 2,
                advantageGrants: new List<AdvantageGrant>
                {
                    AdvGrant("Inspired", RollMode.Advantage, new CharacterCheckAdvantage())
                });

        // Pattern: disadvantage grant on attacks.
        private static StatusEffect DefineBlinded()
            => Make("Blinded", baseDuration: 2,
                advantageGrants: new List<AdvantageGrant>
                {
                    AdvGrant("Blinded", RollMode.Disadvantage, new AttackAdvantage())
                });

        // Pattern: behavioral — amplifies damage taken.
        private static StatusEffect DefineVulnerable()
            => Make("Vulnerable", baseDuration: 2,
                behavioral: Behavioral(damageAmplification: 1.5f));

        // Pattern: HoT — periodic healing per turn.
        private static StatusEffect DefineRegenerating()
            => Make("Regenerating", baseDuration: 3,
                periodicEffects: Periodics(
                    Periodic(PeriodicEffectType.Healing, 8)));

        private static void RegenerateStatusEffect(StatusEffect definition)
        {
            string path = $"{StatusEffectsFolder}/{definition.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<StatusEffect>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(definition, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(definition, path);
            }
        }

        // ==================== BUILDER METHODS ====================

        private static ModifierData Mod(Attribute attribute, ModifierType type, float value)
            => new ModifierData { attribute = attribute, type = type, value = value };

        private static List<ModifierData> Mods(params ModifierData[] mods)
            => new List<ModifierData>(mods);

        private static PeriodicEffectData Periodic(PeriodicEffectType type, DamageFormula formula)
            => new PeriodicEffectData { type = type, damageFormula = formula };

        private static PeriodicEffectData Periodic(PeriodicEffectType type, int amount)
            => new PeriodicEffectData { type = type, amount = amount };

        private static List<PeriodicEffectData> Periodics(params PeriodicEffectData[] effects)
            => new List<PeriodicEffectData>(effects);

        private static BehavioralEffectData Behavioral(
            bool preventsActions = false,
            bool preventsMovement = false,
            float damageAmplification = 1f)
            => new BehavioralEffectData
            {
                preventsActions = preventsActions,
                preventsMovement = preventsMovement,
                damageAmplification = damageAmplification
            };

        private static AdvantageGrant AdvGrant(string label, RollMode type, params IAdvantageTarget[] targets)
            => new AdvantageGrant
            {
                label = label,
                type = type,
                targets = new List<IAdvantageTarget>(targets)
            };

        private static StatusEffect Make(
            string name,
            int baseDuration = -1,
            EntityFeature required = EntityFeature.None,
            EntityFeature excluded = EntityFeature.None,
            List<ModifierData> modifiers = null,
            List<PeriodicEffectData> periodicEffects = null,
            BehavioralEffectData behavioral = null,
            List<AdvantageGrant> advantageGrants = null)
        {
            var effect = ScriptableObject.CreateInstance<StatusEffect>();
            effect.name = name;
            effect.effectName = name;
            effect.baseDuration = baseDuration;
            effect.requiredFeatures = required;
            effect.excludedFeatures = excluded;
            effect.modifiers = modifiers ?? new List<ModifierData>();
            effect.periodicEffects = periodicEffects ?? new List<PeriodicEffectData>();
            effect.behavioralEffects = behavioral ?? new BehavioralEffectData();
            effect.advantageGrants = advantageGrants ?? new List<AdvantageGrant>();
            return effect;
        }
        #endregion
    }
}
#endif
