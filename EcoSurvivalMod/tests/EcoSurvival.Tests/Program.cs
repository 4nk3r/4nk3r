using System;
using System.Collections.Generic;
using EcoSurvival.Core;

namespace EcoSurvival.Tests
{
    public static class Program
    {
        private static readonly List<string> Failures = new List<string>();
        private static int passed;

        public static int Main()
        {
            Run("Config: defaults are valid", ConfigDefaultsAreValid);
            Run("Config: invalid values are rejected", ConfigInvalidValuesAreRejected);

            Run("Hunger: predators gain hunger faster than herbivores", PredatorsGainHungerFaster);
            Run("Hunger: advance clamps at maximum", AdvanceClampsAtMax);
            Run("Hunger: feeding clamps at zero and reports removed amount", FeedClampsAtZero);
            Run("Hunger: stages follow thresholds", StagesFollowThresholds);
            Run("Hunger: ferocity is zero below onset and one at max", FerocityBounds);
            Run("Hunger: ferocity is monotonic in hunger", FerocityIsMonotonic);
            Run("Hunger: damage multiplier stays within configured range", DamageMultiplierRange);
            Run("Hunger: invalid arguments throw", InvalidArgumentsThrow);

            Run("Mind: satiated predator rests", SatiatedPredatorRests);
            Run("Mind: hungry predator without visible prey searches", HungryPredatorSearches);
            Run("Mind: lone prey is hunted directly", LonePreyIsHunted);
            Run("Mind: grouped prey with cover is ambushed", GroupedPreyWithCoverIsAmbushed);
            Run("Mind: ferocious predator ignores herd and hunts", FerociousPredatorIgnoresHerd);
            Run("Mind: herbivore flees from a threat", HerbivoreFleesFromThreat);
            Run("Mind: herd amplifies alert and makes herbivore flee earlier", HerdAmplifiesAlert);
            Run("Mind: hungry safe herbivore grazes, satiated one rests", HerbivoreGrazesWhenHungry);

            Run("Carcass: predator kill of herbivore is consumed", PredatorKillIsConsumed);
            Run("Carcass: delayed removal when configured", DelayedRemovalWhenConfigured);
            Run("Carcass: herbivore killed by player keeps corpse", PlayerKillKeepsCorpse);
            Run("Carcass: predator killed by herbivore is not special-cased", HerbivoreKillerDoesNotConsume);
            Run("Carcass: meal value scales with fraction", MealValueScales);

            Console.WriteLine();
            Console.WriteLine($"Passed: {passed}, failed: {Failures.Count}");
            foreach (var failure in Failures)
            {
                Console.WriteLine("  FAIL: " + failure);
            }

            return Failures.Count == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine("  ok   " + name);
            }
            catch (Exception ex)
            {
                Failures.Add($"{name}: {ex.Message}");
                Console.WriteLine("  FAIL " + name + " -> " + ex.Message);
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void CheckNear(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-9)
            {
                throw new Exception($"{message}: expected {expected}, got {actual}");
            }
        }

        // ---------- Config ----------

        private static void ConfigDefaultsAreValid()
        {
            new SurvivalConfig().Validate();
        }

        private static void ConfigInvalidValuesAreRejected()
        {
            AssertThrows<ArgumentOutOfRangeException>(() => new SurvivalConfig { MealNutritionFraction = 0 }.Validate());
            AssertThrows<ArgumentOutOfRangeException>(() => new SurvivalConfig { MealNutritionFraction = 1.5 }.Validate());
            AssertThrows<ArgumentOutOfRangeException>(() => new SurvivalConfig { FerocityOnsetHunger = 100 }.Validate());
            AssertThrows<ArgumentOutOfRangeException>(() => new SurvivalConfig { MaxDamageMultiplier = 0.5 }.Validate());
            AssertThrows<ArgumentOutOfRangeException>(() => new SurvivalConfig { PredatorHungerPerHour = -1 }.Validate());
            AssertThrows<ArgumentOutOfRangeException>(() => new SurvivalConfig { PredatorKillCorpseDelaySeconds = -5 }.Validate());
        }

        // ---------- Hunger ----------

        private static void PredatorsGainHungerFaster()
        {
            var cfg = new SurvivalConfig();
            var predator = new HungerState(Diet.Carnivore, cfg, 0);
            var herbivore = new HungerState(Diet.Herbivore, cfg, 0);
            predator.Advance(1);
            herbivore.Advance(1);
            Check(predator.Hunger > herbivore.Hunger, "predator should get hungrier faster");
            CheckNear(cfg.PredatorHungerPerHour, predator.Hunger, "predator hunger after one hour");
            CheckNear(cfg.HerbivoreHungerPerHour, herbivore.Hunger, "herbivore hunger after one hour");
        }

        private static void AdvanceClampsAtMax()
        {
            var state = new HungerState(Diet.Carnivore, new SurvivalConfig(), 95);
            state.Advance(100);
            CheckNear(HungerState.Max, state.Hunger, "hunger must clamp at max");
        }

        private static void FeedClampsAtZero()
        {
            var state = new HungerState(Diet.Carnivore, new SurvivalConfig(), 10);
            double removed = state.Feed(50);
            CheckNear(0, state.Hunger, "hunger must clamp at zero");
            CheckNear(10, removed, "removed amount must equal actual reduction");
        }

        private static void StagesFollowThresholds()
        {
            var cfg = new SurvivalConfig();
            Check(new HungerState(Diet.Carnivore, cfg, 39.9).Stage == HungerStage.Satiated, "39.9 is satiated");
            Check(new HungerState(Diet.Carnivore, cfg, 40).Stage == HungerStage.Hungry, "40 is hungry");
            Check(new HungerState(Diet.Carnivore, cfg, 70).Stage == HungerStage.Starving, "70 is starving");
            Check(new HungerState(Diet.Carnivore, cfg, 90).Stage == HungerStage.Ferocious, "90 is ferocious");
        }

        private static void FerocityBounds()
        {
            var cfg = new SurvivalConfig();
            CheckNear(0, new HungerState(Diet.Carnivore, cfg, 0).Ferocity, "ferocity at zero hunger");
            CheckNear(0, new HungerState(Diet.Carnivore, cfg, cfg.FerocityOnsetHunger).Ferocity, "ferocity at onset");
            CheckNear(1, new HungerState(Diet.Carnivore, cfg, HungerState.Max).Ferocity, "ferocity at max hunger");
        }

        private static void FerocityIsMonotonic()
        {
            var cfg = new SurvivalConfig();
            double previous = -1;
            for (double h = 0; h <= 100; h += 2.5)
            {
                double f = new HungerState(Diet.Carnivore, cfg, h).Ferocity;
                Check(f >= previous, $"ferocity decreased at hunger {h}");
                previous = f;
            }
        }

        private static void DamageMultiplierRange()
        {
            var cfg = new SurvivalConfig { MaxDamageMultiplier = 2.0, MaxDetectionBonus = 0.8 };
            CheckNear(1.0, new HungerState(Diet.Carnivore, cfg, 0).DamageMultiplier, "calm damage multiplier");
            CheckNear(2.0, new HungerState(Diet.Carnivore, cfg, 100).DamageMultiplier, "ferocious damage multiplier");
            CheckNear(1.8, new HungerState(Diet.Carnivore, cfg, 100).DetectionMultiplier, "ferocious detection multiplier");
            double mid = new HungerState(Diet.Carnivore, cfg, 77.5).DamageMultiplier;
            Check(mid > 1.0 && mid < 2.0, "intermediate damage multiplier must be between bounds");
        }

        private static void InvalidArgumentsThrow()
        {
            var state = new HungerState(Diet.Carnivore, new SurvivalConfig(), 10);
            AssertThrows<ArgumentOutOfRangeException>(() => state.Advance(-1));
            AssertThrows<ArgumentOutOfRangeException>(() => state.Feed(-1));
            AssertThrows<ArgumentNullException>(() => new HungerState(Diet.Carnivore, null));
        }

        // ---------- Mind ----------

        private static void SatiatedPredatorRests()
        {
            var cfg = new SurvivalConfig();
            var hunger = new HungerState(Diet.Carnivore, cfg, 10);
            var p = new Perception { PreyVisible = true, PreyHerdSize = 0, PreyDistance = 0.2 };
            Check(AnimalMind.Decide(Diet.Carnivore, hunger, p, cfg) == Intent.Rest, "satiated predator must rest");
        }

        private static void HungryPredatorSearches()
        {
            var cfg = new SurvivalConfig();
            var hunger = new HungerState(Diet.Carnivore, cfg, 60);
            var p = new Perception { PreyVisible = false };
            Check(AnimalMind.Decide(Diet.Carnivore, hunger, p, cfg) == Intent.Search, "no prey visible must search");
        }

        private static void LonePreyIsHunted()
        {
            var cfg = new SurvivalConfig();
            var hunger = new HungerState(Diet.Carnivore, cfg, 75);
            var p = new Perception { PreyVisible = true, PreyHerdSize = 0, PreyDistance = 0.4, CoverScore = 0.9 };
            Check(AnimalMind.Decide(Diet.Carnivore, hunger, p, cfg) == Intent.Hunt, "lone prey must be hunted");
        }

        private static void GroupedPreyWithCoverIsAmbushed()
        {
            var cfg = new SurvivalConfig();
            var hunger = new HungerState(Diet.Carnivore, cfg, 60);
            var p = new Perception { PreyVisible = true, PreyHerdSize = 3, PreyDistance = 0.6, CoverScore = 0.8 };
            Check(AnimalMind.Decide(Diet.Carnivore, hunger, p, cfg) == Intent.Ambush, "grouped prey near cover must be ambushed");
        }

        private static void FerociousPredatorIgnoresHerd()
        {
            var cfg = new SurvivalConfig();
            var hunger = new HungerState(Diet.Carnivore, cfg, 100);
            var p = new Perception { PreyVisible = true, PreyHerdSize = 4, PreyDistance = 0.6, CoverScore = 0.9 };
            Intent intent = AnimalMind.Decide(Diet.Carnivore, hunger, p, cfg);
            Check(intent == Intent.Hunt, $"ferocious predator must hunt, got {intent}");
        }

        private static void HerbivoreFleesFromThreat()
        {
            var cfg = new SurvivalConfig();
            var hunger = new HungerState(Diet.Herbivore, cfg, 50);
            var p = new Perception { ThreatLevel = 0.9, AllyHerdSize = 0 };
            Check(AnimalMind.Decide(Diet.Herbivore, hunger, p, cfg) == Intent.Flee, "herbivore must flee from a strong threat");
        }

        private static void HerdAmplifiesAlert()
        {
            var cfg = new SurvivalConfig();
            var hunger = new HungerState(Diet.Herbivore, cfg, 50);
            var weakThreat = new Perception { ThreatLevel = 0.25, AllyHerdSize = 0 };
            var sameInHerd = new Perception { ThreatLevel = 0.25, AllyHerdSize = 4 };
            Check(AnimalMind.Decide(Diet.Herbivore, hunger, weakThreat, cfg) != Intent.Flee, "lone herbivore should not flee at weak threat");
            Check(AnimalMind.Decide(Diet.Herbivore, hunger, sameInHerd, cfg) == Intent.Flee, "herd member should flee at the same threat");
        }

        private static void HerbivoreGrazesWhenHungry()
        {
            var cfg = new SurvivalConfig();
            var p = new Perception { ThreatLevel = 0 };
            Check(AnimalMind.Decide(Diet.Herbivore, new HungerState(Diet.Herbivore, cfg, 50), p, cfg) == Intent.Graze, "hungry safe herbivore must graze");
            Check(AnimalMind.Decide(Diet.Herbivore, new HungerState(Diet.Herbivore, cfg, 5), p, cfg) == Intent.Rest, "satiated herbivore must rest");
        }

        // ---------- Carcass ----------

        private static void PredatorKillIsConsumed()
        {
            var cfg = new SurvivalConfig { PredatorKillCorpseDelaySeconds = 0 };
            CarcassOutcome outcome = CarcassPolicy.Decide(Diet.Herbivore, true, Diet.Carnivore, cfg);
            Check(outcome == CarcassOutcome.ConsumedByPredator, $"expected ConsumedByPredator, got {outcome}");
            outcome = CarcassPolicy.Decide(Diet.Herbivore, true, Diet.Omnivore, cfg);
            Check(outcome == CarcassOutcome.ConsumedByPredator, $"omnivore kill expected ConsumedByPredator, got {outcome}");
        }

        private static void DelayedRemovalWhenConfigured()
        {
            var cfg = new SurvivalConfig { PredatorKillCorpseDelaySeconds = 120 };
            CarcassOutcome outcome = CarcassPolicy.Decide(Diet.Herbivore, true, Diet.Carnivore, cfg);
            Check(outcome == CarcassOutcome.RemovedAfterDelay, $"expected RemovedAfterDelay, got {outcome}");
        }

        private static void PlayerKillKeepsCorpse()
        {
            var cfg = new SurvivalConfig();
            CarcassOutcome outcome = CarcassPolicy.Decide(Diet.Herbivore, false, Diet.Carnivore, cfg);
            Check(outcome == CarcassOutcome.DefaultCorpse, $"non-animal kill must keep corpse, got {outcome}");
        }

        private static void HerbivoreKillerDoesNotConsume()
        {
            var cfg = new SurvivalConfig();
            CarcassOutcome outcome = CarcassPolicy.Decide(Diet.Herbivore, true, Diet.Herbivore, cfg);
            Check(outcome == CarcassOutcome.DefaultCorpse, $"herbivore killer must keep corpse, got {outcome}");
        }

        private static void MealValueScales()
        {
            var cfg = new SurvivalConfig { MealNutritionFraction = 0.5 };
            CheckNear(25, CarcassPolicy.MealValue(50, cfg), "meal value");
            AssertThrows<ArgumentOutOfRangeException>(() => CarcassPolicy.MealValue(-1, cfg));
        }

        // ---------- Helpers ----------

        private static void AssertThrows<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new Exception($"expected {typeof(T).Name}, got {ex.GetType().Name}");
            }
            throw new Exception($"expected {typeof(T).Name}, nothing thrown");
        }
    }
}
