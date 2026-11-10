using System;

namespace EcoSurvival.Core
{
    /// <summary>What happens to the body of a killed animal.</summary>
    public enum CarcassOutcome
    {
        /// <summary>The body stays as a normal corpse on the ground.</summary>
        DefaultCorpse,

        /// <summary>The predator consumes the body at the moment of death. No corpse is spawned.</summary>
        ConsumedByPredator,

        /// <summary>A corpse is spawned and removed after <see cref="SurvivalConfig.PredatorKillCorpseDelaySeconds"/>.</summary>
        RemovedAfterDelay,
    }

    public static class CarcassPolicy
    {
        /// <summary>
        /// Herbivores killed by animals that can hunt them do not remain on the ground.
        /// Every other death (environment, player, predator killed by a player) keeps the default corpse.
        /// </summary>
        public static CarcassOutcome Decide(
            Diet victimDiet,
            bool killedByAnimal,
            Diet killerDiet,
            SurvivalConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            bool predatorKill = killedByAnimal
                && victimDiet == Diet.Herbivore
                && (killerDiet == Diet.Carnivore || killerDiet == Diet.Omnivore);

            if (!predatorKill)
            {
                return CarcassOutcome.DefaultCorpse;
            }

            return config.PredatorKillCorpseDelaySeconds == 0
                ? CarcassOutcome.ConsumedByPredator
                : CarcassOutcome.RemovedAfterDelay;
        }

        /// <summary>
        /// Hunger removed from the predator by a consumed body.
        /// The prey body value is in hunger units and is scaled by the meal fraction.
        /// </summary>
        public static double MealValue(double preyBodyValue, SurvivalConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (preyBodyValue < 0) throw new ArgumentOutOfRangeException(nameof(preyBodyValue), "must be >= 0");
            return preyBodyValue * config.MealNutritionFraction;
        }
    }
}
