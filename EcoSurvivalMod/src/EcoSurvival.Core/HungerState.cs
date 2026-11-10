using System;

namespace EcoSurvival.Core
{
    public enum Diet
    {
        Herbivore,
        Omnivore,
        Carnivore,
    }

    public enum HungerStage
    {
        Satiated,
        Hungry,
        Starving,
        Ferocious,
    }

    /// <summary>
    /// Hunger of a single animal. Hunger grows with time, is reduced by feeding,
    /// and drives the stage and the ferocity of the animal.
    /// </summary>
    public sealed class HungerState
    {
        /// <summary>Upper bound of the hunger scale.</summary>
        public const double Max = 100.0;

        /// <summary>Hunger at which the animal is Hungry.</summary>
        public const double HungryThreshold = 40.0;

        /// <summary>Hunger at which the animal is Starving.</summary>
        public const double StarvingThreshold = 70.0;

        /// <summary>Hunger at which the animal is Ferocious.</summary>
        public const double FerociousThreshold = 90.0;

        private readonly SurvivalConfig config;

        public HungerState(Diet diet, SurvivalConfig config, double initialHunger = 20.0)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            this.Diet = diet;
            this.config = config;
            this.Hunger = Clamp(initialHunger, 0.0, Max);
        }

        public Diet Diet { get; }

        /// <summary>Current hunger on the 0..100 scale.</summary>
        public double Hunger { get; private set; }

        /// <summary>Hunger gained per in-game hour for this diet.</summary>
        public double HungerPerHour => this.Diet == Diet.Herbivore
            ? this.config.HerbivoreHungerPerHour
            : this.config.PredatorHungerPerHour;

        public HungerStage Stage
        {
            get
            {
                if (this.Hunger >= FerociousThreshold) return HungerStage.Ferocious;
                if (this.Hunger >= StarvingThreshold) return HungerStage.Starving;
                if (this.Hunger >= HungryThreshold) return HungerStage.Hungry;
                return HungerStage.Satiated;
            }
        }

        /// <summary>
        /// Ferocity in [0, 1]. Zero below the onset level, one at maximum hunger.
        /// Linear between the onset and the maximum.
        /// </summary>
        public double Ferocity
        {
            get
            {
                double onset = this.config.FerocityOnsetHunger;
                return Clamp((this.Hunger - onset) / (Max - onset), 0.0, 1.0);
            }
        }

        /// <summary>Damage multiplier for predator attacks, in [1, MaxDamageMultiplier].</summary>
        public double DamageMultiplier =>
            1.0 + (this.config.MaxDamageMultiplier - 1.0) * this.Ferocity;

        /// <summary>Detection radius multiplier, in [1, 1 + MaxDetectionBonus].</summary>
        public double DetectionMultiplier =>
            1.0 + this.config.MaxDetectionBonus * this.Ferocity;

        /// <summary>Advances hunger by elapsed in-game time.</summary>
        public void Advance(double hours)
        {
            if (hours < 0) throw new ArgumentOutOfRangeException(nameof(hours), "must be >= 0");
            this.Hunger = Clamp(this.Hunger + this.HungerPerHour * hours, 0.0, Max);
        }

        /// <summary>Reduces hunger by a meal. Returns the hunger actually removed.</summary>
        public double Feed(double amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "must be >= 0");
            double before = this.Hunger;
            this.Hunger = Clamp(this.Hunger - amount, 0.0, Max);
            return before - this.Hunger;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (double.IsNaN(value)) throw new ArgumentException("value must be a number.", nameof(value));
            return value < min ? min : (value > max ? max : value);
        }
    }
}
