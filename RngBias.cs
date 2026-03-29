using System;

namespace NeonRandom
{
    /// <summary>
    /// Defines a positional or frequency-based bias for random selection.
    /// Min, Max, Extremes, and Median use a sliding positional bias.
    /// Weight range is -100..100. Negative values exist for convenience and effectively make Min --> Max and vice versa.
    /// -100 = maximum negative bias (strongest tendency away from the target; exact target is never chosen where possible).
    ///   0 = no bias (uniform selection).
    /// 100 = maximum positive bias (strongest tendency toward the target; exact target is always chosen where possible).
    /// Mode/Antimode modify the chance of getting the most/least common values from an IEnumerable using the same weight semantics.
    /// </summary>
    [Serializable]
    public readonly struct RngBias
    {
        internal readonly BiasKind Kind;
        internal readonly int Weight;

        public bool IsDefined
        {
            get { return Kind != BiasKind.None; }
        }

        public int ClampedWeight
        {
            get { return Weight; }
        }

        internal bool IsPositional
        {
            get
            {
                return Kind == BiasKind.Min || Kind == BiasKind.Max || Kind == BiasKind.Extremes ||
                       Kind == BiasKind.Median;
            }
        }

        internal bool IsFrequency
        {
            get { return Kind == BiasKind.Mode || Kind == BiasKind.Antimode; }
        }

        private RngBias(BiasKind kind, int weight)
        {
            if (weight < -100) weight = -100;
            else if (weight > 100) weight = 100;
            Kind = kind;
            Weight = weight;
        }

        /// <summary>
        /// Bias towards the minimum value in a collection.
        /// </summary>
        /// <param name="weight">Valid weights are -100 to 100.</param>
        public static RngBias Min(int weight)
        {
            return new RngBias(BiasKind.Min, weight);
        }

        /// <summary>
        /// Bias towards the maximum value in a collection.
        /// </summary>
        /// <param name="weight">Valid weights are -100 to 100.</param>
        public static RngBias Max(int weight)
        {
            return new RngBias(BiasKind.Max, weight);
        }

        /// <summary>
        /// Bias towards the extremes of a collection (min and max, avoiding the center).
        /// </summary>
        /// <param name="weight">Valid weights are -100 to 100.</param>
        public static RngBias Extremes(int weight)
        {
            return new RngBias(BiasKind.Extremes, weight);
        }

        /// <summary>
        /// Bias towards the median (center) of a collection.
        /// </summary>
        /// <param name="weight">Valid weights are -100 to 100.</param>
        public static RngBias Median(int weight)
        {
            return new RngBias(BiasKind.Median, weight);
        }

        /// <summary>
        /// Bias towards the most common/frequently occuring value in a collection.
        /// </summary>
        /// <param name="weight">Valid weights are -100 to 100.</param>
        public static RngBias Mode(int weight)
        {
            return new RngBias(BiasKind.Mode, weight);
        }

        /// <summary>
        /// Bias towards the least common/frequently occuring value in a collection.
        /// </summary>
        /// <param name="weight">Valid weights are -100 to 100.</param>
        public static RngBias Antimode(int weight)
        {
            return new RngBias(BiasKind.Antimode, weight);
        }
    }

    internal enum BiasKind
    {
        None,
        Min,
        Max,
        Extremes,
        Median,
        Mode,
        Antimode
    }
}