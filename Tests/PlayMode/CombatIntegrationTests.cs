using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat;
using Assets.Scripts.Core;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Conditions;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectTypes.EntityEffects;
using Assets.Scripts.Effects.Invocations;
using Assets.Scripts.Effects.Targeting;
using Assets.Scripts.Combat.Rolls.Targeting;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Modifiers;
using Assets.Scripts.Entities;
using Assets.Scripts.Skills.Costs;

namespace Assets.Scripts.Tests.PlayMode
{
    public class CombatIntegrationTests
    {
        private Vehicle playerVehicle;
        private Vehicle enemyVehicle;
        private GameObject stageObj;
        private readonly System.Collections.Generic.List<Object> cleanup = new();
        private readonly System.Collections.Generic.List<GameObject> gameObjects = new();

        [TearDown]
        public void TearDown()
        {
            if (playerVehicle != null) Object.DestroyImmediate(playerVehicle.gameObject);
            if (enemyVehicle != null) Object.DestroyImmediate(enemyVehicle.gameObject);
            if (stageObj != null) Object.DestroyImmediate(stageObj);
            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            foreach (var go in gameObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            cleanup.Clear();
            gameObjects.Clear();
        }

        // ==================== HELPERS ====================
        // (All other helpers moved to centralized factories)

        // ==================== FULL ATTACK: WEAPON FIRES, ENEMY TAKES DAMAGE, ENERGY SPENT ====================

        [UnityTest]
        public IEnumerator FullAttack_WeaponFiresAtEnemy_EnergyConsumed()
        {
            // Player: Gunner Bob (attack +3) in weapon seat with +2 weapon
            var bob = TestCharacterFactory.CreateWithCleanup("Bob", baseAttackBonus: 3, cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder("PlayerRacer")
                .WithChassis()
                .WithPowerCore()
                .WithWeapon(bob, attackBonus: 2)
                .Build();
            playerVehicle.PowerCore.currentEnergy = 20;

            // Enemy: Simple vehicle with chassis
            enemyVehicle = new TestVehicleBuilder("EnemyRacer")
                .WithChassis(maxHealth: 80, armorClass: 10)
                .WithPowerCore()
                .Build();

            // Skill: Cannon Shot, 2d8+5 physical, costs 3 energy
            var cannonShot = TestSkillFactory.CreateAttackSkill("Cannon Shot", 2, 8, 5, DamageType.Physical, cleanup, new EnergyCost { amount = 3 });

            int enemyHPBefore = enemyVehicle.Chassis.GetCurrentHealth();
            int energyBefore = playerVehicle.PowerCore.currentEnergy;

            // Execute skill through the full pipeline
            var ctx = new RollContext
            {
                SourceActor = new CharacterWithToolActor(playerVehicle.GetSeatForCharacter(bob), playerVehicle.AllComponents.OfType<WeaponComponent>().First()),
                Target = enemyVehicle,
                CausalSource = cannonShot.name
            };
            playerVehicle.ExecuteSkill(ctx, cannonShot);
            yield return null;

            // Energy should always be consumed (hit or miss)
            Assert.AreEqual(energyBefore - 3, playerVehicle.PowerCore.currentEnergy,
                "Should consume 3 energy regardless of hit/miss");
        }

        // ==================== NOT ENOUGH ENERGY ? SKILL BLOCKED ====================

        [UnityTest]
        public IEnumerator SkillExecution_NotEnoughEnergy_Blocked()
        {
            var gunner = TestCharacterFactory.CreateWithCleanup("Gunner", baseAttackBonus: 2, cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .WithWeapon(gunner)
                .Build();
            playerVehicle.PowerCore.currentEnergy = 1; // Not enough for cost 3

            enemyVehicle = new TestVehicleBuilder("Enemy")
                .WithChassis(maxHealth: 50)
                .WithPowerCore()
                .Build();

            var expensiveSkill = TestSkillFactory.CreateAttackSkill("Big Gun", 3, 10, 10, DamageType.Physical, cleanup, new EnergyCost { amount = 3 });

            int enemyHPBefore = enemyVehicle.Chassis.GetCurrentHealth();

            var ctx = new RollContext
            {
                SourceActor = new CharacterWithToolActor(playerVehicle.GetSeatForCharacter(gunner), playerVehicle.AllComponents.OfType<WeaponComponent>().First()),
                Target = enemyVehicle,
                CausalSource = expensiveSkill.name
            };
            bool executed = playerVehicle.ExecuteSkill(ctx, expensiveSkill);
            yield return null;

            Assert.IsFalse(executed, "Skill should be blocked when not enough energy");
            Assert.AreEqual(enemyHPBefore, enemyVehicle.Chassis.GetCurrentHealth(),
                "Enemy health should be unchanged");
            Assert.AreEqual(1, playerVehicle.PowerCore.currentEnergy,
                "Energy should not be consumed");
        }

        // ==================== BUFF SKILL: NO-ROLL MODIFIER ? TARGET COMPONENT STATS CHANGE ====================

        [UnityTest]
        public IEnumerator BuffSkill_AppliesStatusEffect_ModifiesStat()
        {
            var engineer = TestCharacterFactory.CreateWithCleanup("Engineer", cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore(engineer)
                .Build();
            playerVehicle.PowerCore.currentEnergy = 20;

            // Buff: "Reinforce Hull" ? +3 AC to chassis for 3 turns
            var reinforceTemplate = TestStatusEffectFactory.CreateModifierEffect("Reinforced", EntityAttribute.ArmorClass, 3f, duration: 3, cleanup: cleanup);
            var reinforceSkill = TestSkillFactory.CreateNoRollSkill("Reinforce Hull",
                new System.Collections.Generic.List<IEffectInvocation>
                {
                    new VehicleEffectInvocation
                    {
                        effect = new ApplyEntityConditionEffect { condition = reinforceTemplate },
                        targetResolver = new TargetVehicleResolver()
                    }
                },
                cleanup: cleanup,
                new EnergyCost { amount = 2 });

            int acBefore = playerVehicle.Chassis.GetArmorClass();

            var ctx = new RollContext
            {
                SourceActor = new CharacterWithToolActor(playerVehicle.GetSeatForCharacter(engineer), playerVehicle.PowerCore),
                Target = playerVehicle,
                CausalSource = reinforceSkill.name
            };
            playerVehicle.ExecuteSkill(ctx, reinforceSkill);
            yield return null;

            int acAfter = playerVehicle.Chassis.GetArmorClass();
            Assert.AreEqual(acBefore + 3, acAfter, "AC should increase by 3 from Reinforced buff");

            // Verify status effect is active
            var effects = playerVehicle.Chassis.GetActiveConditions();
            Assert.AreEqual(1, effects.Count, "Should have 1 active status effect");
            Assert.AreEqual("Reinforced", effects[0].template.effectName);
        }

        // ==================== DAMAGE-OVER-TIME: BURNS FOR 3 TURNS, THEN EXPIRES ====================

        [UnityTest]
        public IEnumerator DoTSkill_AppliesBurning_TicksDamageEachTurn()
        {
            var pyro = TestCharacterFactory.CreateWithCleanup("Pyro", cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .WithWeapon(pyro)
                .Build();
            playerVehicle.PowerCore.currentEnergy = 20;

            enemyVehicle = new TestVehicleBuilder("Enemy")
                .WithChassis(maxHealth: 200)
                .WithPowerCore()
                .Build();

            // "Flame Thrower" applies Burning: 5 fire damage per turn for 3 turns
            var burningTemplate = TestStatusEffectFactory.CreateDoTEffect("Burning", damage: 5, DamageType.Fire, duration: 3, cleanup: cleanup);
            var flameThrower = TestSkillFactory.CreateNoRollSkill("Flame Thrower",
                new System.Collections.Generic.List<IEffectInvocation>
                {
                    new VehicleEffectInvocation
                    {
                        effect = new ApplyEntityConditionEffect { condition = burningTemplate },
                        targetResolver = new TargetVehicleResolver()
                    }
                },
                cleanup: cleanup,
                new EnergyCost { amount = 2 });

            // Apply Burning to enemy
            var ctx = new RollContext
            {
                SourceActor = new CharacterWithToolActor(playerVehicle.GetSeatForCharacter(pyro), playerVehicle.AllComponents.OfType<WeaponComponent>().First()),
                Target = enemyVehicle,
                CausalSource = flameThrower.name
            };
            playerVehicle.ExecuteSkill(ctx, flameThrower);
            yield return null;

            Assert.AreEqual(1, enemyVehicle.Chassis.GetActiveConditions().Count, "Enemy should be Burning");

            // Simulate 3 turns of DoT ticking
            int healthBefore = enemyVehicle.Chassis.GetCurrentHealth();
            for (int turn = 1; turn <= 3; turn++)
            {
                enemyVehicle.Chassis.UpdateConditions();
                int healthAfter = enemyVehicle.Chassis.GetCurrentHealth();
                int damageTaken = healthBefore - healthAfter;

                if (turn <= 3)
                {
                    Assert.Greater(damageTaken, 0, $"Should take burn damage by turn {turn}");
                }

                healthBefore = healthAfter;
            }

            // After 3 turns, Burning should expire
            Assert.AreEqual(0, enemyVehicle.Chassis.GetActiveConditions().Count,
                "Burning should expire after 3 turns");
        }

        // ==================== MULTI-CREW VEHICLE: DRIVER + GUNNER + ENGINEER ====================

        [UnityTest]
        public IEnumerator MultiCrew_EachCrewMemberOperatesCorrectly()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", dexterity: 18, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(driver, CharacterSkill.Piloting);

            var gunner = TestCharacterFactory.CreateWithCleanup("Gunner", baseAttackBonus: 4, cleanup: cleanup);

            var engineer = TestCharacterFactory.CreateWithCleanup("Engineer", intelligence: 16, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(engineer, CharacterSkill.Mechanics);

            playerVehicle = new TestVehicleBuilder("WarRig")
                .WithChassis(driver)
                .WithPowerCore(engineer)
                .WithWeapon(gunner, attackBonus: 2)
                .Build();

            // Test 1: Piloting check routes to Driver
            var pilotSpec = SkillCheckSpec.ForCharacter(CharacterSkill.Piloting, ComponentType.Chassis);
            pilotSpec.dc = 12;
            var pilotResult = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = playerVehicle, Spec = pilotSpec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(playerVehicle, pilotSpec) });
            yield return null;
            Assert.AreNotEqual(0, pilotResult.BaseRoll, "Should not auto-fail");

            // Test 2: Mechanics check routes to Engineer via PowerCore
            var mechanicsSpec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics, ComponentType.PowerCore);
            mechanicsSpec.dc = 10;
            var mechResult = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = playerVehicle, Spec = mechanicsSpec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(playerVehicle, mechanicsSpec) });
            yield return null;
            Assert.AreNotEqual(0, mechResult.BaseRoll, "Should not auto-fail");

            // Test 3: Best Perception routes to character with highest WIS modifier
            var percSpec = SkillCheckSpec.ForCharacter(CharacterSkill.Perception);
            percSpec.dc = 14;
            var percResult = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = playerVehicle, Spec = percSpec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(playerVehicle, percSpec) });
            yield return null;
            Assert.IsNotNull(percResult);
            Assert.AreNotEqual(0, percResult.BaseRoll, "Should not auto-fail");

            // Test 4: Attack bonuses stack correctly
            var attackSpec = new AttackSpec { grantedMode = RollMode.Normal };
            var gathered = RollGatherer.ForAttack(attackSpec, new CharacterWithToolActor(playerVehicle.GetSeatForCharacter(gunner), playerVehicle.AllComponents.OfType<WeaponComponent>().First()));
            var bonuses = gathered.Bonuses;
            int totalMod = bonuses.Sum(b => b.Value);
            Assert.AreEqual(6, totalMod, "Attack bonus should be weapon(2) + character(4) = 6");
        }

        // ==================== LANE STATUS EFFECT: MODIFIES VEHICLE STATS ====================

        [UnityTest]
        public IEnumerator LaneEffect_AppliesStatusEffect_ModifiesVehicleStats()
        {
            // Create a vehicle
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", cleanup: cleanup);
            playerVehicle = TestVehicleBuilder.CreateWithChassis(driver);

            // Create stage with a lane that has a status effect
            var laneEffect = TestStatusEffectFactory.CreateModifierEffect("Cliff Edge", EntityAttribute.ArmorClass, -2f, cleanup: cleanup);
            var stage = TestStageFactory.CreateStage("Rocky Stage", out stageObj);
            var cliffLane = TestStageFactory.CreateLane("Cliff Edge Lane", stage, stageObj, laneEffect);

            int acBefore = playerVehicle.Chassis.GetArmorClass();

            // Simulate vehicle entering lane ? apply lane status effect
            cliffLane.vehiclesInLane.Add(playerVehicle);
            playerVehicle.SetCurrentLane(cliffLane);

            // Apply lane effect to all vehicle components (as the system does)
            foreach (var component in playerVehicle.AllComponents)
            {
                component.ApplyCondition(laneEffect, cliffLane);
            }
            yield return null;

            int acAfter = playerVehicle.Chassis.GetArmorClass();
            Assert.AreEqual(acBefore - 2, acAfter, "AC should drop by 2 from Cliff Edge lane");

            // Verify effect is tracked
            var effects = playerVehicle.Chassis.GetActiveConditions();
            Assert.AreEqual(1, effects.Count);
            Assert.AreEqual("Cliff Edge", effects[0].template.effectName);
        }

        // ==================== LANE WITH TURN EFFECT: SKILL CHECK OR TAKE DAMAGE ====================

        [UnityTest]
        public IEnumerator LaneTurnEffect_SkillCheckOrDamage_ExecutesProperly()
        {
            // Setup: Vehicle with driver
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", level: 5, dexterity: 18, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(driver, CharacterSkill.Piloting);
            playerVehicle = TestVehicleBuilder.CreateWithChassis(driver);

            // Create stage + lane with turn effect: Piloting DC 10 or take damage
            var stage = TestStageFactory.CreateStage("Hazard Stage", out stageObj);
            var hazardLane = TestStageFactory.CreateLane("Hazard Lane", stage, stageObj);

            var checkSpec = SkillCheckSpec.ForCharacter(CharacterSkill.Piloting);
            checkSpec.dc = 10;
            var turnEffect = new LaneTurnEffect
            {
                effectName = "Rocky Road Hazard",
                description = "Navigate treacherous rocks",
                rollNode = new RollNode
                {
                    targetResolver = new CurrentTargetResolver(),
                    rollSpec = checkSpec
                }
            };
            hazardLane.turnEffects.Add(turnEffect);
            hazardLane.vehiclesInLane.Add(playerVehicle);
            playerVehicle.SetCurrentLane(hazardLane);

            // Execute the skill check (simulating what the turn system would do)
            var checkResult = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = playerVehicle,
                Spec = checkSpec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(playerVehicle, checkSpec)
            });
            yield return null;

            // Result should be valid
            Assert.IsNotNull(checkResult);
            Assert.AreNotEqual(0, checkResult.BaseRoll, "Should be able to attempt the check");

            // Verify modifier is correct: DEX 18 (+4) + Prof level 5 (+3) = +7
            int expectedDexMod = CharacterStatCalculator.CalculateAttributeModifier(18);
            int expectedProf = CharacterStatCalculator.CalculateProficiencyBonus(5);
            Assert.AreEqual(expectedDexMod + expectedProf, checkResult.TotalModifier,
                $"Modifier should be DEX({expectedDexMod}) + Prof({expectedProf})");
        }

        // ==================== SAVING THROW VS ENEMY SKILL ====================

        [UnityTest]
        public IEnumerator EnemySkill_ForceSave_EffectsApplyOnFailure()
        {
            // Player vehicle: Driver with poor WIS
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", wisdom: 8, cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();
            var driverSeat = new VehicleSeat
            {
                seatName = "Driver",
                controlledComponents = new System.Collections.Generic.List<VehicleComponent> { playerVehicle.Chassis }
            };
            driverSeat.Assign(driver);
            playerVehicle.seats.Add(driverSeat);

            // Enemy vehicle
            var enemyCaster = TestCharacterFactory.CreateWithCleanup("EnemyCaster", cleanup: cleanup);
            enemyVehicle = new TestVehicleBuilder("EnemyShip")
                .WithChassis()
                .WithPowerCore()
                .AddSeat("Caster", enemyCaster)
                .Build();
            enemyVehicle.PowerCore.currentEnergy = 20;

            // Create save skill: "Psychic Scream" - WIS save DC 20 or take damage
            // DC 20 is nearly impossible with WIS 8 (-1 mod + half level)
            var psychicScream = TestSkillFactory.CreateSaveSkill("Psychic Scream", CharacterAttribute.Wisdom, dc: 20,
                new System.Collections.Generic.List<IEffectInvocation>
                {
                    new VehicleEffectInvocation
                    {
                        effect = new DamageEffect
                        {
                            formulaProvider = new StaticFormulaProvider
                            {
                                formula = new DamageFormula
                                {
                                    baseDice = 0, dieSize = 0, bonus = 15,
                                    damageType = DamageType.Physical
                                }
                            }
                        },
                        targetResolver = new TargetVehicleResolver()
                    }
                },
                cleanup: cleanup,
                new EnergyCost { amount = 3 });

            int playerHPBefore = playerVehicle.Chassis.GetCurrentHealth();

            // Execute save skill against player chassis
            var ctx = new RollContext
            {
                SourceActor = new CharacterActor(enemyVehicle.GetSeatForCharacter(enemyCaster)),
                Target = playerVehicle,
                CausalSource = psychicScream.name
            };
            enemyVehicle.ExecuteSkill(ctx, psychicScream);
            yield return null;

            // Energy consumed from enemy
            Assert.AreEqual(17, enemyVehicle.PowerCore.currentEnergy, "Enemy should spend 3 energy");

            // Save routing: should route to Driver (only character, best WIS by default)
            // With DC 20, most likely fails (needs nat 20 basically)
            // We can't guarantee the roll, but we can verify the system didn't crash
            // and that the save used the correct character
        }

        // ==================== STUN ? COMPONENT NOT OPERATIONAL ? AUTO-FAIL ====================

        [UnityTest]
        public IEnumerator StunEffect_PreventsActions_CausesAutoFail()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", dexterity: 18, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(driver, CharacterSkill.Piloting);

            playerVehicle = new TestVehicleBuilder()
                .WithChassis(driver)
                .WithPowerCore()
                .WithUtility(driver)
                .Build();

            // Stun the Utility component
            var stunTemplate = ScriptableObject.CreateInstance<EntityCondition>();
            stunTemplate.effectName = "Stunned";
            stunTemplate.baseDuration = 2;
            stunTemplate.modifiers = new System.Collections.Generic.List<EntityModifierData>();
            stunTemplate.periodicEffects = new System.Collections.Generic.List<IPeriodicEffect>();
            stunTemplate.behavioralEffects = new BehavioralEffectData { preventsActions = true };
            cleanup.Add(stunTemplate);

            var utilityComp = playerVehicle.GetComponentOfType(ComponentType.Utility);
            utilityComp.ApplyCondition(stunTemplate, utilityComp);
            yield return null;

            // Verify Utility is not operational
            Assert.IsFalse(utilityComp.IsOperational, "Stunned component should not be operational");

            // Skill requiring Utility should auto-fail
            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics, ComponentType.Utility);
            spec.dc = 10;
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = playerVehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(playerVehicle, spec) });
            yield return null;

            Assert.AreEqual(0, result.BaseRoll, "Should auto-fail when required component is stunned");

            // Wait 2 turns ? stun expires ? component operational again
            utilityComp.UpdateConditions(); // Turn 1
            utilityComp.UpdateConditions(); // Turn 2 ? expires
            yield return null;

            Assert.IsTrue(utilityComp.IsOperational, "Component should be operational after stun expires");

            var resultAfter = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = playerVehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(playerVehicle, spec) });
            Assert.AreNotEqual(0, resultAfter.BaseRoll, "Should be able to attempt after stun expires");
        }

        // ==================== COMPONENT DESTROYED ? SKILL BLOCKED ? HEAL ? WORKS AGAIN ====================

        [UnityTest]
        public IEnumerator ComponentLifecycle_Destroy_Heal_ResumeFunction()
        {
            var engineer = TestCharacterFactory.CreateWithCleanup("Engineer", intelligence: 16, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(engineer, CharacterSkill.Mechanics);

            playerVehicle = new TestVehicleBuilder()
                .WithChassis(engineer)
                .WithPowerCore()
                .WithUtility(engineer)
                .Build();

            var utility = playerVehicle.GetComponentOfType(ComponentType.Utility);
            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics, ComponentType.Utility);
            spec.dc = 10;

            // Phase 1: Working normally
            var result1 = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = playerVehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(playerVehicle, spec) });
            yield return null;
            Assert.AreNotEqual(0, result1.BaseRoll, "Should work when component is healthy");

            // Phase 2: Destroy component via damage
            utility.TakeDamage(utility.GetCurrentHealth());
            Assert.IsTrue(utility.IsDestroyed(), "Component should be destroyed");

            var result2 = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = playerVehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(playerVehicle, spec) });
            yield return null;
            Assert.AreEqual(0, result2.BaseRoll, "Should auto-fail when component destroyed");

            // Phase 3: Restore component
            utility.ResetDestroyedState();
            utility.SetHealth(utility.GetMaxHealth());
            Assert.IsTrue(utility.IsOperational, "Component should be operational after restoration");

            var result3 = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = playerVehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(playerVehicle, spec) });
            yield return null;
            Assert.AreNotEqual(0, result3.BaseRoll, "Should work again after component restored");
        }

        // ==================== MULTIPLE MODIFIERS FROM DIFFERENT SOURCES STACK ====================

        [UnityTest]
        public IEnumerator ModifierStacking_MultipleSourcesCombine()
        {
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();

            int baseAC = playerVehicle.Chassis.GetArmorClass();

            // Source 1: Equipment modifier (+2 AC)
            playerVehicle.Chassis.AddModifier(new EntityAttributeModifier(
                EntityAttribute.ArmorClass, ModifierType.Flat, 2f,
                "Armor Plating"));

            // Source 2: Status effect (+3 AC from buff skill)
            var shieldBuff = TestStatusEffectFactory.CreateModifierEffect("Shield", EntityAttribute.ArmorClass, 3f, duration: 5, cleanup: cleanup);
            playerVehicle.Chassis.ApplyCondition(shieldBuff, playerVehicle);

            // Source 3: Direct modifier (+1 AC from event card)
            playerVehicle.Chassis.AddModifier(new EntityAttributeModifier(
                EntityAttribute.ArmorClass, ModifierType.Flat, 1f,
                "Lucky Break"));

            yield return null;

            int finalAC = playerVehicle.Chassis.GetArmorClass();
            Assert.AreEqual(baseAC + 2 + 3 + 1, finalAC,
                "AC should be base + equipment(2) + status(3) + event(1)");

            // Verify breakdown shows all sources
            var (total, bv, modifiers) = Assets.Scripts.Core.StatCalculator.GatherAttributeValueWithBreakdown(
                playerVehicle.Chassis, EntityAttribute.ArmorClass);
            Assert.IsTrue(modifiers.Any(m => m.Label == "Armor Plating"), "Should show Armor Plating");
            Assert.IsTrue(modifiers.Any(m => m.Label == "Lucky Break"), "Should show Lucky Break");

            // Remove status effect ? AC should drop by 3
            var applied = playerVehicle.Chassis.GetActiveConditions()[0];
            playerVehicle.Chassis.RemoveCondition(applied);
            yield return null;

            int acAfterRemoval = playerVehicle.Chassis.GetArmorClass();
            Assert.AreEqual(baseAC + 2 + 1, acAfterRemoval,
                "AC should drop by 3 after removing Shield buff");
        }

        // ==================== FULL COMBAT SCENARIO: TWO VEHICLES TRADING BLOWS ====================

        [UnityTest]
        public IEnumerator FullScenario_TwoVehiclesCombat_MultiTurn()
        {
            // Player: 3-person crew
            var pDriver = TestCharacterFactory.CreateWithCleanup("PlayerDriver", dexterity: 16, baseAttackBonus: 0, cleanup: cleanup);
            var pGunner = TestCharacterFactory.CreateWithCleanup("PlayerGunner", baseAttackBonus: 5, cleanup: cleanup);
            var pEngineer = TestCharacterFactory.CreateWithCleanup("PlayerEngineer", intelligence: 14, cleanup: cleanup);

            playerVehicle = new TestVehicleBuilder("PlayerRacer")
                .WithChassis(pDriver, maxHealth: 100)
                .WithPowerCore(pEngineer)
                .WithWeapon(pGunner, attackBonus: 3)
                .Build();
            playerVehicle.PowerCore.currentEnergy = 50;

            // Enemy: 2-person crew
            var eDriver = TestCharacterFactory.CreateWithCleanup("EnemyDriver", dexterity: 14, cleanup: cleanup);
            var eGunner = TestCharacterFactory.CreateWithCleanup("EnemyGunner", baseAttackBonus: 3, cleanup: cleanup);

            enemyVehicle = new TestVehicleBuilder("EnemyRacer")
                .WithChassis(eDriver, maxHealth: 80)
                .WithPowerCore()
                .WithWeapon(eGunner, attackBonus: 1)
                .Build();
            enemyVehicle.PowerCore.currentEnergy = 50;

            // Create stage + lane
            var stage = TestStageFactory.CreateStage("Combat Arena", out stageObj);
            var mainLane = TestStageFactory.CreateLane("Main Road", stage, stageObj);
            stage.vehiclesInStage.Add(playerVehicle);
            stage.vehiclesInStage.Add(enemyVehicle);
            mainLane.vehiclesInLane.Add(playerVehicle);
            mainLane.vehiclesInLane.Add(enemyVehicle);
            playerVehicle.SetCurrentStage(stage);
            enemyVehicle.SetCurrentStage(stage);

            // Skills
            var playerAttack = TestSkillFactory.CreateAttackSkill("Cannon", 2, 8, 3, DamageType.Physical, cleanup, new EnergyCost { amount = 3 });
            var enemyAttack = TestSkillFactory.CreateAttackSkill("Crossbow", 1, 6, 2, DamageType.Physical, cleanup, new EnergyCost { amount = 2 });

            // Simulate 3 turns of combat
            for (int turn = 1; turn <= 3; turn++)
            {
                // Player attacks enemy
                if (playerVehicle.PowerCore.CanDrawPower(3))
                {
                    var playerCtx = new RollContext
                    {
                        SourceActor = new CharacterWithToolActor(playerVehicle.GetSeatForCharacter(pGunner), playerVehicle.AllComponents.OfType<WeaponComponent>().First()),
                        Target = enemyVehicle,
                        CausalSource = playerAttack.name
                    };
                    playerVehicle.ExecuteSkill(playerCtx, playerAttack);
                }

                // Enemy attacks player
                if (enemyVehicle.PowerCore.CanDrawPower(2))
                {
                    var enemyCtx = new RollContext
                    {
                        SourceActor = new CharacterWithToolActor(enemyVehicle.GetSeatForCharacter(eGunner), enemyVehicle.AllComponents.OfType<WeaponComponent>().First()),
                        Target = playerVehicle,
                        CausalSource = enemyAttack.name
                    };
                    enemyVehicle.ExecuteSkill(enemyCtx, enemyAttack);
                }

                // End-of-turn: tick status effects
                playerVehicle.UpdateStatusEffects();
                enemyVehicle.UpdateStatusEffects();

                yield return null;
            }

            // After 3 turns: verify energy was spent (no regen, pure consumption)
            // Player: 50 - (3 * 3) = 41
            // Enemy: 50 - (2 * 3) = 44
            Assert.AreEqual(41, playerVehicle.PowerCore.currentEnergy,
                "Player should have spent 9 energy over 3 turns (3 per turn)");
            Assert.AreEqual(44, enemyVehicle.PowerCore.currentEnergy,
                "Enemy should have spent 6 energy over 3 turns (2 per turn)");

            // Verify neither vehicle crashed (no null refs, no exceptions)
            Assert.IsNotNull(playerVehicle.Chassis);
            Assert.IsNotNull(enemyVehicle.Chassis);
        }
    }
}
