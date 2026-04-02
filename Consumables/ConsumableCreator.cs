#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using System.Collections.Generic;
using Assets.Scripts.Characters;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Combat.Rolls.Targeting;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectTypes;
using Assets.Scripts.Effects.Targeting;
using Assets.Scripts.Skills;

namespace Assets.Scripts.Consumables
{
    /// <summary>
    /// Convenience editor class to create new Consumable and AmmunitionType assets with preset configurations.
    /// </summary>
    public static class ConsumableCreator
    {
        public const string ConsumablesFolder = "Assets/Content/Consumables";
        public const string AmmoFolder = "Assets/Content/Consumables/Ammo";

        [MenuItem("Assets/Racing/Regenerate All Consumables")]
        public static void RegenerateAllConsumables()
        {
            System.IO.Directory.CreateDirectory(ConsumablesFolder);
            System.IO.Directory.CreateDirectory(AmmoFolder);

            // Combat consumables
            RegenerateConsumable(DefineFragGrenade());
            RegenerateConsumable(DefineIncendiaryFlask());
            RegenerateConsumable(DefineShockCharge());
            RegenerateConsumable(DefineConcussionGrenade());
            RegenerateConsumable(DefineSmokeCanister());

            // Utility consumables
            RegenerateConsumable(DefineRepairKit());
            RegenerateConsumable(DefineEnergyCell());
            RegenerateConsumable(DefineFortifyingTonic());
            RegenerateConsumable(DefineStimulant());
            RegenerateConsumable(DefineArmourSealant());

            // Ammunition types
            RegenerateAmmo(DefineHollowPoint());
            RegenerateAmmo(DefineIncendiaryRounds());
            RegenerateAmmo(DefineShockRounds());
            RegenerateAmmo(DefineArmourPiercing());
            RegenerateAmmo(DefineExplosiveTips());

            AssetDatabase.SaveAssets();
            Debug.Log("[ConsumableCreator] All consumables regenerated.");
        }

        // ==================== COMBAT CONSUMABLES ====================

        // Pattern: attack roll, AoE bludgeoning — thrown explosive scattering shrapnel across the lane.
        private static CombatConsumable DefineFragGrenade()
            => MakeCombat("Frag Grenade",
                "A fragmentation grenade that scatters shrapnel across the target lane.",
                Attack(FX(Dmg(2, 6, 0, DamageType.Bludgeoning, OnLane))),
                TargetingMode.Enemy,
                ActionType.Action);

        // Pattern: save-based fire damage + Burning DoT — thrown flask that ignites on impact.
        private static CombatConsumable DefineIncendiaryFlask()
            => MakeCombat("Incendiary Flask",
                "A flask of combustible fluid. Failed Stability save: fire damage and Burning.",
                Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 13,
                    FX(Dmg(1, 6, 0, DamageType.Fire, OnTarget),
                       EntityStatus(LoadEntityCondition("Burning"), OnTarget))),
                TargetingMode.Enemy,
                ActionType.Action);

        // Pattern: attack roll, force damage + Overheating — electromagnetic shock device.
        private static CombatConsumable DefineShockCharge()
            => MakeCombat("Shock Charge",
                "An electromagnetic pulse device. On hit: force damage and Overheating.",
                Attack(FX(Dmg(1, 6, 0, DamageType.Force, OnTarget),
                           EntityStatus(LoadEntityCondition("Overheating"), OnTarget))),
                TargetingMode.EnemyComponent,
                ActionType.Action);

        // Pattern: save-based crowd control — concussive blast that Slows the target.
        private static CombatConsumable DefineConcussionGrenade()
            => MakeCombat("Concussion Grenade",
                "A concussive device. Failed Mobility save: Slowed.",
                Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 14,
                    FX(EntityStatus(LoadEntityCondition("Slowed"), OnTarget))),
                TargetingMode.Enemy,
                ActionType.BonusAction);

        // Pattern: check gates a character debuff — navigator makes a Perception check;
        // on success a follow-up node blinds the opposing navigator.
        private static CombatConsumable DefineSmokeCanister()
            => MakeCombat("Smoke Canister",
                "Dense smoke dispersal. Pass a Perception check DC 13 to blind the opposing navigator.",
                Check(SkillCheckSpec.ForCharacter(CharacterSkill.Perception, ComponentType.Sensors), 13,
                    FX(),
                    successChain: AlwaysApply(FX(CharacterStatus(LoadCharacterCondition("Blinded"), OnTarget)))),
                TargetingMode.Enemy,
                ActionType.BonusAction);

        // ==================== UTILITY CONSUMABLES ====================

        // Pattern: no roll, self-component repair — basic patch kit.
        private static UtilityConsumable DefineRepairKit()
            => MakeUtility("Repair Kit",
                "A basic patch kit. Restores health to the using component.",
                AlwaysApply(FX(Heal(8, OnSourceComp))),
                TargetingMode.SourceComponent,
                ActionType.BonusAction);

        // Pattern: no roll, restore energy — portable battery pack.
        private static UtilityConsumable DefineEnergyCell()
            => MakeUtility("Energy Cell",
                "A rechargeable cell. Restores energy to the vehicle.",
                AlwaysApply(FX(Energy(4, OnSelf))),
                TargetingMode.Self,
                ActionType.BonusAction);

        // Pattern: no roll, apply Fortified buff — structural reinforcement compound.
        private static UtilityConsumable DefineFortifyingTonic()
            => MakeUtility("Fortifying Tonic",
                "A structural sealant compound. Applies Fortified to the vehicle.",
                AlwaysApply(FX(EntityStatus(LoadEntityCondition("Fortified"), OnSelf))),
                TargetingMode.Self,
                ActionType.BonusAction);

        // Pattern: no roll, apply Regenerating HoT — nanite repair solution.
        private static UtilityConsumable DefineStimulant()
            => MakeUtility("Stimulant",
                "A nanite repair solution. Applies Regenerating to the vehicle.",
                AlwaysApply(FX(EntityStatus(LoadEntityCondition("Regenerating"), OnSelf))),
                TargetingMode.Self,
                ActionType.BonusAction);

        // Pattern: no roll, two effects — component heal and Fortified buff applied together.
        private static UtilityConsumable DefineArmourSealant()
            => MakeUtility("Armour Sealant",
                "Emergency spray-on armour. Heals the component and applies Fortified to the vehicle.",
                AlwaysApply(FX(
                    Heal(4, OnSourceComp),
                    EntityStatus(LoadEntityCondition("Fortified"), OnSelf))),
                TargetingMode.SourceComponent,
                ActionType.BonusAction);

        // ==================== AMMUNITION TYPES ====================

        // Pattern: bonus physical damage on hit — hollow-point rounds that punch deeper.
        private static AmmunitionType DefineHollowPoint()
            => MakeAmmo("Hollow Point",
                "Rounds designed to deform on impact, dealing bonus physical damage.",
                AlwaysApply(FX(Dmg(1, 4, 0, DamageType.Physical, OnTarget))),
                "Ranged", "Ballistic");

        // Pattern: fire DoT on hit — rounds coated in combustible compound.
        private static AmmunitionType DefineIncendiaryRounds()
            => MakeAmmo("Incendiary Rounds",
                "Combustible-tipped rounds. On hit: fire damage and Burning.",
                AlwaysApply(FX(
                    Dmg(1, 4, 0, DamageType.Fire, OnTarget),
                    EntityStatus(LoadEntityCondition("Burning"), OnTarget))),
                "Ranged", "Ballistic");

        // Pattern: save-based Overheating on hit — electromagnetic slugs.
        private static AmmunitionType DefineShockRounds()
            => MakeAmmo("Shock Rounds",
                "Electromagnetic slugs. On hit: target makes a Mobility save DC 13 or gains Overheating.",
                Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 13,
                    FX(EntityStatus(LoadEntityCondition("Overheating"), OnTarget))),
                "Ranged", "Ballistic");

        // Pattern: bonus piercing damage — hardened penetrator rounds.
        private static AmmunitionType DefineArmourPiercing()
            => MakeAmmo("Armour-Piercing",
                "Hardened penetrator rounds. On hit: bonus piercing damage.",
                AlwaysApply(FX(Dmg(1, 6, 2, DamageType.Piercing, OnTarget))),
                "Ranged", "Ballistic", "Heavy");

        // Pattern: AoE splash on hit — micro-explosive tips that detonate on contact.
        private static AmmunitionType DefineExplosiveTips()
            => MakeAmmo("Explosive Tips",
                "Micro-explosive rounds. On hit: bludgeoning splash to all vehicles in the target lane.",
                AlwaysApply(FX(Dmg(1, 4, 0, DamageType.Bludgeoning, OnLane))),
                "Ranged", "Ballistic");

        // ==================== REGENERATION HELPERS ====================

        private static void RegenerateConsumable(Consumable definition)
        {
            string path = $"{ConsumablesFolder}/{definition.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Consumable>(path);
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

        private static void RegenerateAmmo(AmmunitionType definition)
        {
            string path = $"{AmmoFolder}/{definition.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AmmunitionType>(path);
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

        private static EntityCondition LoadEntityCondition(string name)
            => AssetDatabase.LoadAssetAtPath<EntityCondition>($"{ConditionCreator.StatusEffectsFolder}/{name}.asset");

        private static CharacterCondition LoadCharacterCondition(string name)
            => AssetDatabase.LoadAssetAtPath<CharacterCondition>($"{ConditionCreator.ConditionsFolder}/{name}.asset");

        // ==================== FACTORY METHODS ====================

        private static CombatConsumable MakeCombat(
            string name,
            string description,
            RollNode onUseNode,
            TargetingMode targeting = TargetingMode.Enemy,
            ActionType actionCost = ActionType.Action,
            int bulkPerCharge = 1)
        {
            var item = ScriptableObject.CreateInstance<CombatConsumable>();
            item.name = name;
            item.description = description;
            item.onUseNode = onUseNode;
            item.targetingMode = targeting;
            item.actionCost = actionCost;
            item.bulkPerCharge = bulkPerCharge;
            return item;
        }

        private static UtilityConsumable MakeUtility(
            string name,
            string description,
            RollNode onUseNode,
            TargetingMode targeting = TargetingMode.Self,
            ActionType actionCost = ActionType.BonusAction,
            int bulkPerCharge = 1)
        {
            var item = ScriptableObject.CreateInstance<UtilityConsumable>();
            item.name = name;
            item.description = description;
            item.onUseNode = onUseNode;
            item.targetingMode = targeting;
            item.actionCost = actionCost;
            item.bulkPerCharge = bulkPerCharge;
            return item;
        }

        private static AmmunitionType MakeAmmo(
            string name,
            string description,
            RollNode onHitNode,
            params string[] compatible)
        {
            var ammo = ScriptableObject.CreateInstance<AmmunitionType>();
            ammo.name = name;
            ammo.description = description;
            ammo.onHitNode = onHitNode;
            ammo.compatibleWith = new List<string>(compatible);
            return ammo;
        }

        // ==================== BUILDER METHODS ====================

        private static IEffectTargetResolver OnTarget     => new SelectedTargetResolver();
        private static IEffectTargetResolver OnSelf       => new SourceVehicleResolver();
        private static IEffectTargetResolver OnSourceComp => new SourceComponentResolver();
        private static IEffectTargetResolver OnLane       => new AllVehiclesInLaneEffectResolver();

        private static List<EffectInvocation> FX(params EffectInvocation[] effects)
            => new List<EffectInvocation>(effects);

        private static EffectInvocation Dmg(
            int dice, int dieSize, int bonus = 0,
            DamageType type = DamageType.Physical,
            IEffectTargetResolver target = null)
            => new EffectInvocation
            {
                targetResolver = target ?? OnTarget,
                effect = new DamageEffect
                {
                    formulaProvider = new StaticFormulaProvider
                    {
                        formula = new DamageFormula { baseDice = dice, dieSize = dieSize, bonus = bonus, damageType = type }
                    }
                }
            };

        private static EffectInvocation Heal(int amount, IEffectTargetResolver target = null)
            => new EffectInvocation
            {
                targetResolver = target ?? OnSelf,
                effect = new ResourceRestorationEffect { formula = new RestorationFormula { resourceType = ResourceType.Health, isDrain = false, bonus = amount } }
            };

        private static EffectInvocation Energy(int amount, IEffectTargetResolver target = null)
            => new EffectInvocation
            {
                targetResolver = target ?? OnSelf,
                effect = new ResourceRestorationEffect { formula = new RestorationFormula { resourceType = ResourceType.Energy, isDrain = false, bonus = amount } }
            };

        private static EffectInvocation EntityStatus(EntityCondition condition, IEffectTargetResolver target = null)
            => new EffectInvocation
            {
                targetResolver = target ?? OnTarget,
                effect = new ApplyEntityConditionEffect { condition = condition }
            };

        private static EffectInvocation CharacterStatus(CharacterCondition condition, IEffectTargetResolver target = null)
            => new EffectInvocation
            {
                targetResolver = target ?? OnTarget,
                effect = new ApplyCharacterConditionEffect { condition = condition }
            };

        private static RollNode AlwaysApply(List<EffectInvocation> effects)
            => new RollNode { targetResolver = new CurrentTargetResolver(), successEffects = effects };

        private static RollNode Attack(
            List<EffectInvocation> onHit,
            List<EffectInvocation> onMiss = null,
            RollNode successChain = null)
            => new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = new AttackSpec(),
                successEffects = onHit ?? new List<EffectInvocation>(),
                failureEffects = onMiss ?? new List<EffectInvocation>(),
                onSuccessChain = successChain
            };

        private static RollNode Save(
            SaveSpec spec, int dc,
            List<EffectInvocation> onFail,
            List<EffectInvocation> onPass = null,
            RollNode failChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = spec,
                failureEffects = onFail ?? new List<EffectInvocation>(),
                successEffects = onPass ?? new List<EffectInvocation>(),
                onFailureChain = failChain
            };
        }

        private static RollNode Check(
            SkillCheckSpec spec, int dc,
            List<EffectInvocation> onSuccess,
            List<EffectInvocation> onFail = null,
            RollNode successChain = null,
            RollNode failChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = spec,
                successEffects = onSuccess ?? new List<EffectInvocation>(),
                failureEffects = onFail ?? new List<EffectInvocation>(),
                onSuccessChain = successChain,
                onFailureChain = failChain
            };
        }
    }
}
#endif
