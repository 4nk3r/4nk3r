using System;

namespace EcoSurvival.Core
{
    /// <summary>What the animal currently wants to do.</summary>
    public enum Intent
    {
        Rest,
        Graze,
        Flee,
        Search,
        Hunt,
        Ambush,
    }

    /// <summary>
    /// Snapshot of what one animal can perceive this tick.
    /// Distances and cover are normalized to [0, 1] by the caller.
    /// </summary>
    public sealed class Perception
    {
        /// <summary>Strongest threat in range, in [0, 1]. Already scaled by the threat's ferocity.</summary>
        public double ThreatLevel { get; set; }

        /// <summary>Number of same-species animals nearby, excluding this animal.</summary>
        public int AllyHerdSize { get; set; }

        /// <summary>True if a huntable prey animal is in detection range.</summary>
        public bool PreyVisible { get; set; }

        /// <summary>Number of prey animals in the prey group. Zero if none is visible.</summary>
        public int PreyHerdSize { get; set; }

        /// <summary>Distance to the nearest visible prey, 0 = adjacent, 1 = edge of detection range.</summary>
        public double PreyDistance { get; set; } = 1.0;

        /// <summary>Quality of nearby cover for an ambush, in [0, 1].</summary>
        public double CoverScore { get; set; }
    }

    /// <summary>
    /// Chooses the intent of one animal from its hunger and perception.
    /// Pure function of its inputs: no Eco types, no hidden state, so it is unit-testable.
    ///
    /// Herbivores flee when the combined threat, amplified by herd alertness, exceeds a threshold.
    /// Predators weigh hunger against the defensive strength of the prey herd. Hungry predators
    /// ambush from cover when prey moves in a large group and hunt directly when prey is alone
    /// or when the predator is ferocious enough to ignore the herd.
    /// </summary>
    public static class AnimalMind
    {
        /// <summary>Threat score above which a herbivore flees.</summary>
        public const double FleeThreshold = 0.30;

        /// <summary>Hunt utility above which a predator commits to the prey.</summary>
        public const double HuntThreshold = 0.50;

        /// <summary>Herd size at which alert bonus and deterrence stop growing.</summary>
        public const int MaxHerdEffect = 4;

        public static Intent Decide(Diet diet, HungerState hunger, Perception perception, SurvivalConfig config)
        {
            if (hunger == null) throw new ArgumentNullException(nameof(hunger));
            if (perception == null) throw new ArgumentNullException(nameof(perception));
            if (config == null) throw new ArgumentNullException(nameof(config));

            if (diet == Diet.Herbivore)
            {
                return DecideHerbivore(hunger, perception);
            }

            return DecidePredator(hunger, perception, config);
        }

        private static Intent DecideHerbivore(HungerState hunger, Perception perception)
        {
            // Herds alert each other: the same threat is felt more strongly inside a group.
            double alert = 1.0 + 0.25 * Math.Min(perception.AllyHerdSize, MaxHerdEffect);
            double fleeScore = Clamp01(perception.ThreatLevel) * alert;

            if (fleeScore >= FleeThreshold)
            {
                return Intent.Flee;
            }

            // A hungry herbivore grazes when it is safe. Otherwise it rests.
            return hunger.Stage == HungerStage.Satiated ? Intent.Rest : Intent.Graze;
        }

        private static Intent DecidePredator(HungerState hunger, Perception perception, SurvivalConfig config)
        {
            if (hunger.Stage == HungerStage.Satiated)
            {
                return Intent.Rest;
            }

            if (!perception.PreyVisible)
            {
                return Intent.Search;
            }

            double hungerDrive = hunger.Hunger / HungerState.Max;

            // Large prey herds deter charging. Ferocity partly overrides the deterrence.
            int herdEffect = Math.Min(perception.PreyHerdSize, MaxHerdEffect);
            double deterrence = herdEffect * config.HerdDeterrencePerMember;
            double ferocityOverride = hunger.Ferocity * 0.5;
            double deterrenceAfterOverride = Math.Max(0.0, deterrence - ferocityOverride);

            // Waiting in cover costs nothing, so an ambush is chosen before the charge threshold.
            // A calm predator facing a group prefers cover. A ferocious one does not wait.
            bool groupedPrey = perception.PreyHerdSize >= 2;
            bool hasCover = Clamp01(perception.CoverScore) >= 0.6;
            bool notDesperate = hunger.Ferocity < 0.5;

            if (groupedPrey && hasCover && notDesperate)
            {
                return Intent.Ambush;
            }

            double huntUtility = hungerDrive * (1.0 - Math.Min(1.0, deterrenceAfterOverride)) + ferocityOverride;

            return huntUtility >= HuntThreshold ? Intent.Hunt : Intent.Search;
        }

        private static double Clamp01(double value)
        {
            if (double.IsNaN(value)) return 0.0;
            return value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
        }
    }
}
