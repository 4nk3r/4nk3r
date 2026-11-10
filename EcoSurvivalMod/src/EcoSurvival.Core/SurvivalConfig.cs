using System;

namespace EcoSurvival.Core
{
    /// <summary>
    /// Tunable rules of the survival model. All rates are per in-game hour.
    /// Hunger is expressed on a 0..100 scale.
    /// </summary>
    public sealed class SurvivalConfig
    {
        /// <summary>Hunger gained per in-game hour by carnivores and omnivores.</summary>
        public double PredatorHungerPerHour { get; set; } = 8.0;

        /// <summary>Hunger gained per in-game hour by herbivores.</summary>
        public double HerbivoreHungerPerHour { get; set; } = 4.0;

        /// <summary>Hunger removed by one meal, as a fraction of the prey body value (0, 1].</summary>
        public double MealNutritionFraction { get; set; } = 0.6;

        /// <summary>Hunger level at which ferocity starts to rise.</summary>
        public double FerocityOnsetHunger { get; set; } = 55.0;

        /// <summary>Maximum multiplier applied to predator damage at full ferocity (1 = no change).</summary>
        public double MaxDamageMultiplier { get; set; } = 2.0;

        /// <summary>Maximum extra detection radius at full ferocity, as a fraction of the base radius.</summary>
        public double MaxDetectionBonus { get; set; } = 0.8;

        /// <summary>Hunt utility penalty per prey herd member. Larger herds deter hunting.</summary>
        public double HerdDeterrencePerMember { get; set; } = 0.12;

        /// <summary>
        /// Seconds after which a corpse of a herbivore killed by an animal is removed.
        /// 0 means the predator consumes the body at the moment of death and no corpse is spawned.
        /// </summary>
        public int PredatorKillCorpseDelaySeconds { get; set; } = 0;

        /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> if any value is outside its valid range.</summary>
        public void Validate()
        {
            Require(PredatorHungerPerHour >= 0, nameof(PredatorHungerPerHour), "must be >= 0");
            Require(HerbivoreHungerPerHour >= 0, nameof(HerbivoreHungerPerHour), "must be >= 0");
            Require(MealNutritionFraction > 0 && MealNutritionFraction <= 1, nameof(MealNutritionFraction), "must be in (0, 1]");
            Require(FerocityOnsetHunger > 0 && FerocityOnsetHunger < HungerState.Max, nameof(FerocityOnsetHunger), "must be in (0, 100)");
            Require(MaxDamageMultiplier >= 1, nameof(MaxDamageMultiplier), "must be >= 1");
            Require(MaxDetectionBonus >= 0, nameof(MaxDetectionBonus), "must be >= 0");
            Require(HerdDeterrencePerMember >= 0, nameof(HerdDeterrencePerMember), "must be >= 0");
            Require(PredatorKillCorpseDelaySeconds >= 0, nameof(PredatorKillCorpseDelaySeconds), "must be >= 0");
        }

        private static void Require(bool condition, string name, string message)
        {
            if (!condition)
            {
                throw new ArgumentOutOfRangeException(name, $"{name} {message}.");
            }
        }
    }
}
