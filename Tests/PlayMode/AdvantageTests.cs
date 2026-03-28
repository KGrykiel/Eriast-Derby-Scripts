using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Entities.Vehicle.VehicleComponents;
using Assets.Scripts.Conditions;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Conditions.EntityConditions;

namespace Assets.Scripts.Tests.PlayMode
{
    public class AdvantageTests
    {
        private Vehicle vehicle;
        private readonly List<Object> cleanup = new();

        [TearDown]
        public void TearDown()
        {
            if (vehicle != null)
                Object.DestroyImmediate(vehicle.gameObject);
            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            cleanup.Clear();
        }

        // ==================== HELPERS ====================

        private EntityCondition CreateAdvantageStatusEffect(
            string name,
            RollMode mode,
            List<IAdvantageTarget> targets,
            string grantLabel = null)
        {
            var template = ScriptableObject.CreateInstance<EntityCondition>();
            template.effectName = name;
            template.baseDuration = -1;
            template.modifiers = new List<EntityModifierData>();
            template.periodicEffects = new List<IPeriodicEffect>();
            template.behavioralEffects = new BehavioralEffectData();
            template.advantageGrants = new List<AdvantageGrant>
            {
                new AdvantageGrant
                {
                    label = grantLabel,
                    type = mode,
                    targets = targets
                }
            };

            cleanup.Add(template);
            return template;
        }

        // ==================== D20Calculator Structural ====================

        [Test]
        public void Roll_Normal_NoDroppingRoll()
        {
            var gathered = new GatheredRoll(new List<RollBonus>(), null);
            var outcome = D20Calculator.Roll(gathered, 10);

            Assert.AreEqual(RollMode.Normal, outcome.Advantage.Mode);
            Assert.IsNull(outcome.Advantage.DroppedRoll,
                "Normal rolls should not have a dropped roll");
        }

        [Test]
        public void Roll_Advantage_HasDroppedRoll()
        {
            var sources = new List<AdvantageSource> { new AdvantageSource("Test", RollMode.Advantage) };
            var gathered = new GatheredRoll(new List<RollBonus>(), sources);

            var outcome = D20Calculator.Roll(gathered, 10);

            Assert.AreEqual(RollMode.Advantage, outcome.Advantage.Mode);
            Assert.IsNotNull(outcome.Advantage.DroppedRoll,
                "Advantage rolls should have a dropped roll");
        }

        [Test]
        public void Roll_Disadvantage_HasDroppedRoll()
        {
            var sources = new List<AdvantageSource> { new AdvantageSource("Test", RollMode.Disadvantage) };
            var gathered = new GatheredRoll(new List<RollBonus>(), sources);

            var outcome = D20Calculator.Roll(gathered, 10);

            Assert.AreEqual(RollMode.Disadvantage, outcome.Advantage.Mode);
            Assert.IsNotNull(outcome.Advantage.DroppedRoll,
                "Disadvantage rolls should have a dropped roll");
        }

        [Test]
        public void Roll_Advantage_KeepsHigherRoll()
        {
            var sources = new List<AdvantageSource> { new AdvantageSource("Test", RollMode.Advantage) };
            var gathered = new GatheredRoll(new List<RollBonus>(), sources);

            var outcome = D20Calculator.Roll(gathered, 10);

            Assert.GreaterOrEqual(outcome.BaseRoll, outcome.Advantage.DroppedRoll.Value,
                "With advantage, the kept roll should be >= the dropped roll");
        }

        [Test]
        public void Roll_Disadvantage_KeepsLowerRoll()
        {
            var sources = new List<AdvantageSource> { new AdvantageSource("Test", RollMode.Disadvantage) };
            var gathered = new GatheredRoll(new List<RollBonus>(), sources);

            var outcome = D20Calculator.Roll(gathered, 10);

            Assert.LessOrEqual(outcome.BaseRoll, outcome.Advantage.DroppedRoll.Value,
                "With disadvantage, the kept roll should be <= the dropped roll");
        }

        [Test]
        public void Roll_SourcesPropagatedToOutcome()
        {
            var sources = new List<AdvantageSource>
            {
                new AdvantageSource("Bless", RollMode.Advantage),
                new AdvantageSource("Flanking", RollMode.Advantage)
            };
            var gathered = new GatheredRoll(new List<RollBonus>(), sources);

            var outcome = D20Calculator.Roll(gathered, 10);

            Assert.AreEqual(2, outcome.Advantage.Sources.Count);
            Assert.AreEqual("Bless", outcome.Advantage.Sources[0].Label);
            Assert.AreEqual("Flanking", outcome.Advantage.Sources[1].Label);
        }

        [Test]
        public void Roll_NullSources_ProducesEmptyArray()
        {
            var gathered = new GatheredRoll(new List<RollBonus>(), null);
            var outcome = D20Calculator.Roll(gathered, 10);

            Assert.IsNotNull(outcome.Advantage.Sources);
            Assert.AreEqual(0, outcome.Advantage.Sources.Count);
        }

        // ==================== GatherAdvantageSources ====================

        [Test]
        public void GatherAdvantageSources_NoEntityNoGrant_ReturnsEmpty()
        {
            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Piloting);

            var sources = RollGatherer.GatherAdvantageSources(null, null, spec, default);

            Assert.AreEqual(0, sources.Count);
        }

        [Test]
        public void GatherAdvantageSources_GrantedSourceIncluded()
        {
            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Piloting);
            var granted = new AdvantageSource("Spec Grant", RollMode.Advantage);

            var sources = RollGatherer.GatherAdvantageSources(null, null, spec, granted);

            Assert.AreEqual(1, sources.Count);
            Assert.AreEqual("Spec Grant", sources[0].Label);
            Assert.AreEqual(RollMode.Advantage, sources[0].Type);
        }

        [Test]
        public void GatherAdvantageSources_DefaultGrantExcluded()
        {
            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Piloting);

            var sources = RollGatherer.GatherAdvantageSources(null, null, spec, default);

            Assert.AreEqual(0, sources.Count,
                "Default AdvantageSource (Type=Normal) should not be added");
        }

        // ==================== Status Effect Grants ====================

        [UnityTest]
        public IEnumerator StatusEffect_MatchingCheckGrant_IncludedInSources()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            var targets = new List<IAdvantageTarget> { new CharacterCheckAdvantage() };
            var effect = CreateAdvantageStatusEffect("Inspired", RollMode.Advantage, targets);

            var utility = vehicle.GetComponentOfType(ComponentType.Utility);
            utility.ApplyCondition(effect, null);

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Piloting, requiredComponent: ComponentType.Utility, dc: 10);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = vehicle,
                Spec = spec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(vehicle, spec)
            });
            yield return null;

            Assert.AreEqual(RollMode.Advantage, result.Advantage.Mode,
                "Character check advantage status effect should produce advantage mode");
            Assert.IsNotNull(result.Advantage.DroppedRoll);
        }

        [UnityTest]
        public IEnumerator StatusEffect_NonMatchingGrant_NotIncluded()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            var targets = new List<IAdvantageTarget> { new AttackAdvantage() };
            var effect = CreateAdvantageStatusEffect("Attack Boost", RollMode.Advantage, targets);

            var utility = vehicle.GetComponentOfType(ComponentType.Utility);
            utility.ApplyCondition(effect, null);

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Piloting, requiredComponent: ComponentType.Utility, dc: 10);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = vehicle,
                Spec = spec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(vehicle, spec)
            });
            yield return null;

            Assert.AreEqual(RollMode.Normal, result.Advantage.Mode,
                "Attack advantage should not apply to skill checks");
            Assert.IsNull(result.Advantage.DroppedRoll);
        }

        [UnityTest]
        public IEnumerator StatusEffect_LimitToSkill_OnlyMatchesTargetedSkill()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            var targets = new List<IAdvantageTarget>
            {
                new CharacterCheckAdvantage { limitTo = new List<CharacterSkill> { CharacterSkill.Mechanics } }
            };
            var effect = CreateAdvantageStatusEffect("Mechanics Focus", RollMode.Advantage, targets);

            var utility = vehicle.GetComponentOfType(ComponentType.Utility);
            utility.ApplyCondition(effect, null);

            var pilotSpec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Piloting, requiredComponent: ComponentType.Utility, dc: 10);
            var pilotResult = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = vehicle,
                Spec = pilotSpec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(vehicle, pilotSpec)
            });
            yield return null;

            Assert.AreEqual(RollMode.Normal, pilotResult.Advantage.Mode,
                "limitTo [Mechanics] should not apply to Piloting checks");

            var mechSpec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredComponent: ComponentType.Utility, dc: 10);
            var mechResult = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = vehicle,
                Spec = mechSpec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(vehicle, mechSpec)
            });
            yield return null;

            Assert.AreEqual(RollMode.Advantage, mechResult.Advantage.Mode,
                "limitTo [Mechanics] should apply to Mechanics checks");
        }

        // ==================== Component Grants ====================

        [UnityTest]
        public IEnumerator ComponentGrant_Operational_IncludedInSources()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            var utility = vehicle.GetComponentOfType(ComponentType.Utility);
            utility.providedAdvantageGrants = new List<ComponentAdvantageGrantData>
            {
                new ComponentAdvantageGrantData
                {
                    grant = new AdvantageGrant
                    {
                        label = "Enhanced Sensors",
                        type = RollMode.Advantage,
                        targets = new List<IAdvantageTarget> { new CharacterCheckAdvantage() }
                    }
                }
            };
            utility.ApplyProvidedAdvantageGrants(vehicle);

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Perception, requiredComponent: ComponentType.Utility, dc: 10);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = vehicle,
                Spec = spec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(vehicle, spec)
            });
            yield return null;

            Assert.AreEqual(RollMode.Advantage, result.Advantage.Mode,
                "Operational component advantage grant should be active");
        }

        [UnityTest]
        public IEnumerator ComponentGrant_Destroyed_ExcludedFromSources()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            var utility = vehicle.GetComponentOfType(ComponentType.Utility);
            utility.providedAdvantageGrants = new List<ComponentAdvantageGrantData>
            {
                new ComponentAdvantageGrantData
                {
                    grant = new AdvantageGrant
                    {
                        label = "Enhanced Sensors",
                        type = RollMode.Advantage,
                        targets = new List<IAdvantageTarget> { new CharacterCheckAdvantage() }
                    }
                }
            };
            utility.ApplyProvidedAdvantageGrants(vehicle);

            utility.TakeDamage(utility.GetCurrentHealth());

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Perception, requiredComponent: ComponentType.Chassis, dc: 10);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = vehicle,
                Spec = spec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(vehicle, spec)
            });
            yield return null;

            Assert.AreEqual(RollMode.Normal, result.Advantage.Mode,
                "Destroyed component's advantage grants should not apply");
        }

        // ==================== Spec grantedMode ====================

        [UnityTest]
        public IEnumerator SpecGrantedMode_SkillCheck_ProducesAdvantage()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(character);

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Piloting, requiredComponent: ComponentType.Chassis, dc: 10);
            spec.grantedMode = RollMode.Advantage;

            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = vehicle,
                Spec = spec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(vehicle, spec)
            });
            yield return null;

            Assert.AreEqual(RollMode.Advantage, result.Advantage.Mode,
                "Spec grantedMode should propagate to the roll");
            Assert.IsNotNull(result.Advantage.DroppedRoll);
        }

        [UnityTest]
        public IEnumerator SpecGrantedMode_Disadvantage_ProducesDisadvantage()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(character);

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Piloting, requiredComponent: ComponentType.Chassis, dc: 10);
            spec.grantedMode = RollMode.Disadvantage;

            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = vehicle,
                Spec = spec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(vehicle, spec)
            });
            yield return null;

            Assert.AreEqual(RollMode.Disadvantage, result.Advantage.Mode);
            Assert.IsNotNull(result.Advantage.DroppedRoll);
        }

        // ==================== Cancellation Through Performer ====================

        [UnityTest]
        public IEnumerator SpecAdvantage_PlusStatusDisadvantage_CancelsToNormal()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            var targets = new List<IAdvantageTarget> { new CharacterCheckAdvantage() };
            var effect = CreateAdvantageStatusEffect("Cursed", RollMode.Disadvantage, targets);

            var utility = vehicle.GetComponentOfType(ComponentType.Utility);
            utility.ApplyCondition(effect, null);

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredComponent: ComponentType.Utility, dc: 10);
            spec.grantedMode = RollMode.Advantage;

            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext
            {
                Vehicle = vehicle,
                Spec = spec,
                CausalSource = null,
                Routing = CheckRouter.RouteSkillCheck(vehicle, spec)
            });
            yield return null;

            Assert.AreEqual(RollMode.Normal, result.Advantage.Mode,
                "Spec advantage + status disadvantage should cancel to Normal");
            Assert.IsNull(result.Advantage.DroppedRoll,
                "Cancelled mode should not produce a dropped roll");
            Assert.GreaterOrEqual(result.Advantage.Sources.Count, 2,
                "Both sources should still be recorded");
        }

        // ==================== Vehicle Check Advantage with limitTo ====================

        [UnityTest]
        public IEnumerator VehicleCheckAdvantage_LimitToAttribute_OnlyMatchesTargeted()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(character);

            var targets = new List<IAdvantageTarget>
            {
                new VehicleCheckAdvantage { limitTo = new List<VehicleCheckAttribute> { VehicleCheckAttribute.Mobility } }
            };
            var effect = CreateAdvantageStatusEffect("Mobility Focus", RollMode.Advantage, targets);

            vehicle.chassis.ApplyCondition(effect, null);

            var handlingSpec = SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Mobility);
            handlingSpec.dc = 10;

            var handlingSources = RollGatherer.GatherAdvantageSources(vehicle.chassis, null, handlingSpec, default);

            Assert.AreEqual(1, handlingSources.Count,
                "Should match vehicle check with Mobility attribute");
            Assert.AreEqual(RollMode.Advantage, handlingSources[0].Type);

            var speedSpec = SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Stability);
            speedSpec.dc = 10;

            var speedSources = RollGatherer.GatherAdvantageSources(vehicle.chassis, null, speedSpec, default);

            Assert.AreEqual(0, speedSources.Count,
                "limitTo [Mobility] should not match Stability checks");
            yield return null;
        }

        // ==================== AdvantageResult Struct ====================

        [Test]
        public void AdvantageResult_DefaultMode_IsNormal()
        {
            var result = default(AdvantageResult);

            Assert.AreEqual(RollMode.Normal, result.Mode);
            Assert.IsNull(result.DroppedRoll);
            Assert.IsNull(result.Sources);
        }

        [Test]
        public void AdvantageSource_Constructor_SetsProperties()
        {
            var source = new AdvantageSource("Bless", RollMode.Advantage);

            Assert.AreEqual("Bless", source.Label);
            Assert.AreEqual(RollMode.Advantage, source.Type);
        }

        [Test]
        public void AdvantageSource_Default_IsNormalType()
        {
            var source = default(AdvantageSource);

            Assert.AreEqual(RollMode.Normal, source.Type);
        }
    }
}
