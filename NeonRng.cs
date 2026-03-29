using System;
using System.Diagnostics;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace NeonRandom
{
    [Serializable]
    public class NeonRng
    {
        #region Fields
        public uint State0;
        public uint State1;
        public uint State2;
        public uint State3;

        public long CallCount;
        public ulong StateHash;
        public bool IsInitialized;
        #endregion

        #region Initialization
        public NeonRng(string seed)
        {
            Init(seed);
        }

        /// <summary>
        /// Initializes or re-seeds the generator. Must be called before any RNG calls.
        /// </summary>
        public void Init(string seed)
        {
            CallCount = 0;
            State0 = ComputeHash(seed, 2166136261u);
            State1 = ComputeHash(seed, 2166136261u + 1u);
            State2 = ComputeHash(seed, 2166136261u + 2u);
            State3 = ComputeHash(seed, 2166136261u + 3u);

            if (State0 == 0 && State1 == 0 && State2 == 0 && State3 == 0)
            {
                State3 = 1;
            }

            IsInitialized = true;
        }
        #endregion

        #region RNG Methods
        /// <returns>A random UInt.</returns>
        public uint NextUInt()
        {
            uint x = State0;
            uint y = State1;
            uint t = x ^ (x << 11);
            State0 = State1;
            State1 = State2;
            State2 = State3;
            uint newW = State3 ^ (State3 >> 19) ^ t ^ (t >> 8);
            State3 = newW;
            
            CallCount++;
            return newW;
        }

        /// <returns>A random float between 0 and 1.</returns>
        public float NextFloat()
        {
            return NextUInt() / (float)uint.MaxValue;
        }
        
        /// <returns>A random int within the passed double-inclusive range.</returns>
        public int Range(int minInclusive, int maxInclusive, RngBias rngBias = default)
        {
            if (minInclusive > maxInclusive)
            {
                Debug.LogError("Min cannot be greater than max.");
                return maxInclusive;
            }

            if (!rngBias.IsDefined || !rngBias.IsPositional || rngBias.ClampedWeight == 0)
            {
                uint span = (uint)(maxInclusive - minInclusive + 1);
                uint r = NextUInt();
                uint offset = span != 0 ? r % span : r;
                return minInclusive + (int)offset;
            }

            int min = minInclusive;
            int max = maxInclusive;
            int count = max - min + 1;
            
            if (count <= 0)
            {
                return min;
            }

            int index = SelectPositionalIndex(count, rngBias.Kind, rngBias.ClampedWeight);
            if (index < 0 || index >= count)
            {
                index = Range(0, count - 1);
            }

            return min + index;
        }

        /// <summary>
        /// Double inclusive float range.
        /// </summary>
        public float Range(float minInclusive, float maxInclusive, RngBias rngBias = default)
        {
            if (minInclusive > maxInclusive)
            {
                Debug.LogError("Min cannot be greater than max.");
                return maxInclusive;
            }

            float min = minInclusive;
            float max = maxInclusive;

            if (min == max)
            {
                return min;
            }

            if (!rngBias.IsDefined || !rngBias.IsPositional || rngBias.ClampedWeight == 0)
            {
                return min + NextFloat() * (max - min);
            }

            int weight = rngBias.ClampedWeight;

            if (weight == 100)
            {
                return GetBiasedFloatTarget(min, max, rngBias.Kind);
            }

            if (weight == -100)
            {
                return GetBiasedFloatAvoid(min, max, rngBias.Kind);
            }

            const int bucketCount = 256;
            int index = SelectPositionalIndex(bucketCount, rngBias.Kind, weight);
            if (index < 0 || index >= bucketCount)
            {
                index = Range(0, bucketCount - 1);
            }

            float span = max - min;
            float bucketSize = span / bucketCount;
            float bucketStart = min + bucketSize * index;
            return bucketStart + NextFloat() * bucketSize;
        }

        /// <summary>
        /// Emulates UnityEngine.Random.Range(int, int) inclusivity.
        /// </summary>
        public int UnityRange(int minInclusive, int maxExclusive, RngBias rngBias = default)
        {
            if (minInclusive == maxExclusive) return minInclusive;
            if (minInclusive < maxExclusive) return Range(minInclusive, maxExclusive - 1);
            return Range(maxExclusive + 1, minInclusive, rngBias);
        }

        /// <summary>
        /// Emulates UnityEngine.Random.Range(float, float) inclusivity.
        /// </summary>
        public float UnityRange(float minInclusive, float maxInclusive, RngBias rngBias = default)
        {
            if (minInclusive > maxInclusive)
            {
                float temp = minInclusive;
                minInclusive = maxInclusive;
                maxInclusive = temp;
            }

            return Range(minInclusive, maxInclusive, rngBias);
        }

        /// <returns>Int +- amount (random range).</returns>
        public int PlusMinusFlat(int value, int amount, RngBias rngBias = default)
        {
            return Range(value - amount, value + amount, rngBias);
        }

        /// <returns>Int +- percentage (random range).</returns>
        public int PlusMinusPercent(int value, float percent, RngBias rngBias = default)
        {
            float range = value * (percent / 100f);
            int min = UnityEngine.Mathf.FloorToInt(value - range);
            int max = UnityEngine.Mathf.CeilToInt(value + range);
            return Range(min, max, rngBias);
        }

        /// <returns>Float +- amount (random range).</returns>
        public float PlusMinusFlat(float value, float amount, RngBias rngBias = default)
        {
            return Range(value - amount, value + amount, rngBias);
        }

        /// <returns>Float +- percentage (random range).</returns>
        public float PlusMinusPercent(float value, float percent, RngBias rngBias = default)
        {
            float range = value * (percent / 100f);
            return Range(value - range, value + range, rngBias);
        }
        
        /// <summary>
        /// 50/50 chance to return true or false.
        /// </summary>
        public bool CoinFlip()
        {
            return (NextUInt() & 1) == 0;
        }
        
        /// <param name="percent">Percent chance to return true.</param>
        public bool PercentChance(int percent)
        {
            if (percent <= 0) return false;
            if (percent >= 100) return true;
            int roll = (int)(((ulong)NextUInt() * 100UL) >> 32);
            return roll < percent;
        }

        /// <summary>
        /// Gets a single item from a collection.
        /// </summary>
        public T GetRandom<T>(IEnumerable<T> items, RngBias rngBias = default)
        {
            if (items == null)
            {
                return default;
            }

            List<T> list = new List<T>(items);
            int count = list.Count;
            if (count == 0)
            {
                return default;
            }

            if (!rngBias.IsDefined || rngBias.ClampedWeight == 0)
            {
                int indexUnbiased = Range(0, count - 1);
                return list[indexUnbiased];
            }

            try
            {
                list.Sort();
            }
            catch (Exception)
            {
                Debug.LogError($"GetRandom (biased): Type {typeof(T).Name} is not comparable. Falling back to unbiased selection.");
                int fallbackIndex = Range(0, count - 1);
                return list[fallbackIndex];
            }

            if (rngBias.IsFrequency)
            {
                bool[] isBiased = new bool[count];
                if (rngBias.Kind == BiasKind.Mode)
                {
                    MarkModeBiased(list, isBiased);
                }
                else
                {
                    MarkAntiModeBiased(list, isBiased);
                }

                int index = SelectBiasedIndex(count, isBiased, rngBias.ClampedWeight);
                if (index < 0 || index >= count)
                {
                    index = Range(0, count - 1);
                }

                return list[index];
            }

            if (rngBias.IsPositional)
            {
                int indexPositional = SelectPositionalIndex(count, rngBias.Kind, rngBias.ClampedWeight);
                if (indexPositional < 0 || indexPositional >= count)
                {
                    indexPositional = Range(0, count - 1);
                }

                return list[indexPositional];
            }

            int fallback = Range(0, count - 1);
            return list[fallback];
        }

        /// <summary>
        /// Gets the specified number of random items from the passed collection. No duplicates.
        /// </summary>
        /// <param name="rngBias">Optional bias for each pick.</param>
        /// <param name="flexCount">If true, will return as many items as possible when count > collection size.</param>
        public List<T> GetRandom<T>(IEnumerable<T> items, int count, RngBias rngBias = default, bool flexCount = false)
        {
            if (items == null)
            {
                return default;
            }
            if (count < 0)
            {
                return default;
            }

            List<T> buffer = new List<T>(items);
            int total = buffer.Count;
            if (count > total)
            {
                if (!flexCount)
                {
                    Debug.LogError($"GetRandom: passed count ({count}) cannot be greater than the number of items in the collection");
                    return default;
                }

                count = total;
            }

            if (!rngBias.IsDefined || rngBias.ClampedWeight == 0)
            {
                List<T> resultUnbiased = new List<T>(count);
                for (int i = 0; i < count; i++)
                {
                    int index = Range(i, total - 1);
                    T selected = buffer[index];
                    buffer[index] = buffer[i];
                    buffer[i] = selected;
                    resultUnbiased.Add(selected);
                }

                return resultUnbiased;
            }

            List<T> result = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                if (buffer.Count == 0)
                {
                    break;
                }

                T selected = GetRandom(buffer, rngBias);
                result.Add(selected);
                buffer.Remove(selected);
            }

            return result;
        }

        /// <summary>
        /// Returns a random int from within the passed NumRangeInt.
        /// </summary>
        public int GetRandom(NumRangeInt range, RngBias rngBias = default)
        {
            int min = range.Min;
            int max = range.Max;

            if (min > max)
            {
                Debug.LogError("GetRandom(NumRangeInt, Bias): Min cannot be greater than max.");
                return max;
            }

            return Range(min, max, rngBias);
        }

        /// <summary>
        /// Returns a random float from within the passed NumRangeFloat.
        /// </summary>
        public float GetRandom(NumRangeFloat range, RngBias rngBias = default)
        {
            float min = range.Min;
            float max = range.Max;

            if (min > max)
            {
                Debug.LogError("GetRandom(NumRangeFloat, Bias): Min cannot be greater than max.");
                return max;
            }

            return Range(min, max, rngBias);
        }
        
        /// <summary>
        /// Gets a random item from a WeightedCollection, according to the collection's defined item weights.
        /// </summary>
        public T GetRandomWeighted<T>(WeightedCollection<T> collection)
        {
            if (collection == null || collection.Count == 0 || collection.TotalWeight <= 0)
            {
                return default;
            }

            int roll = Range(1, collection.TotalWeight);
            int cumulative = 0;

            foreach (KeyValuePair<T, int> entry in collection)
            {
                int weight = entry.Value;
                if (weight <= 0)
                {
                    continue;
                }

                cumulative += weight;
                if (roll <= cumulative)
                {
                    return entry.Key;
                }
            }

            return default;
        }

        /// <summary>
        /// Inserts a single item at a random list index, optionally biased over the index range.
        /// </summary>
        public void RandomInsert<T>(IList<T> list, T item, RngBias rngBias = default)
        {
            int index = Range(0, list.Count, rngBias);
            list.Insert(index, item);
        }

        /// <summary>
        /// Inserts multiple items into a list, each at its own random index, optionally biased over the index range.
        /// </summary>
        public void RandomInsert<T>(IList<T> list, IEnumerable<T> items, RngBias rngBias = default)
        {
            foreach (T currentItem in items)
            {
                int index = Range(0, list.Count, rngBias);
                list.Insert(index, currentItem);
            }
        }

        /// <summary>
        /// Shuffles a list in-place.
        /// </summary>
        public void Shuffle<T>(IList<T> list)
        {
            ShuffleList(list);
        }

        /// <returns>A shuffled shallow copy of the passed list.</returns>
        public List<T> ToShuffledList<T>(IList<T> original)
        {
            List<T> list = new List<T>(original);
            ShuffleList(list);
            return list;
        }

        /// <summary>
        /// Shuffles a queue in-place by copying to a temporary list, applying Fisher–Yates, and rebuilding the queue.
        /// </summary>
        public void Shuffle<T>(Queue<T> queue)
        {
            List<T> list = new List<T>(queue);
            ShuffleList(list);

            queue.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                queue.Enqueue(list[i]);
            }
        }
        
        /// <summary>
        /// Shuffles a stack in-place by copying to a temporary list, applying Fisher–Yates, and rebuilding the stack.
        /// </summary>
        public void Shuffle<T>(Stack<T> stack)
        {
            List<T> list = new List<T>(stack);
            ShuffleList(list);

            stack.Clear();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                stack.Push(list[i]);
            }
        }

        private void ShuffleList<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Range(0, n);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        #endregion

        #region Helper Functions
        private static uint ComputeHash(string str, uint seed)
        {
            unchecked
            {
                uint hash = seed;
                for (int i = 0; i < str.Length; i++)
                {
                    hash = (hash ^ (uint)str[i]) * 16777619u;
                }

                return hash;
            }
        }

        public ulong GetStateHash()
        {
            ulong p1 = ((ulong)State0 << 32) | State1;
            ulong p2 = ((ulong)State2 << 32) | State3;
            return p1 ^ p2 ^ (ulong)CallCount;
        }

        /// <summary>
        /// Positional selector for Min/Max/Extremes/Median.
        /// </summary>
        private int SelectPositionalIndex(int count, BiasKind kind, int weight)
        {
            if (count <= 0)
            {
                return -1;
            }

            if (count == 1)
            {
                return 0;
            }

            if (kind == BiasKind.Mode || kind == BiasKind.Antimode || kind == BiasKind.None)
            {
                return Range(0, count - 1);
            }

            if (weight < -100) weight = -100;
            if (weight > 100) weight = 100;

            if (weight == 0)
            {
                return Range(0, count - 1);
            }

            if (weight == 100)
            {
                switch (kind)
                {
                    case BiasKind.Min:
                        return 0;
                    case BiasKind.Max:
                        return count - 1;
                    case BiasKind.Extremes:
                        return CoinFlip() ? 0 : count - 1;
                    case BiasKind.Median:
                        if (count == 2)
                        {
                            return Range(0, 1);
                        }

                        if (count % 2 == 1)
                        {
                            return count / 2;
                        }
                        else
                        {
                            int rightMedian = count / 2;
                            int leftMedian = rightMedian - 1;
                            return Range(0, 1) == 0 ? leftMedian : rightMedian;
                        }
                }

                return Range(0, count - 1);
            }

            bool[] forbiddenIndices = null;
            if (weight == -100)
            {
                forbiddenIndices = new bool[count];
                switch (kind)
                {
                    case BiasKind.Min:
                        forbiddenIndices[0] = true;
                        break;
                    case BiasKind.Max:
                        forbiddenIndices[count - 1] = true;
                        break;
                    case BiasKind.Extremes:
                        forbiddenIndices[0] = true;
                        forbiddenIndices[count - 1] = true;
                        break;
                    case BiasKind.Median:
                        if (count == 2)
                        {
                            forbiddenIndices[0] = true;
                            forbiddenIndices[1] = true;
                        }
                        else if (count % 2 == 1)
                        {
                            int mid = count / 2;
                            forbiddenIndices[mid] = true;
                        }
                        else
                        {
                            int right = count / 2;
                            int left = right - 1;
                            forbiddenIndices[left] = true;
                            forbiddenIndices[right] = true;
                        }
                        break;
                }
            }

            float strength = Math.Abs(weight) / 100f;
            bool towards = weight > 0;

            float[] weights = new float[count];
            float total = 0f;

            for (int i = 0; i < count; i++)
            {
                float w;

                if (forbiddenIndices != null && forbiddenIndices[i])
                {
                    w = 0f;
                }
                else
                {
                    float closeness = GetPositionalCloseness(i, count, kind);
                    float centered = (closeness - 0.5f) * 2f;
                    float sign = towards ? 1f : -1f;
                    w = 1f + sign * strength * centered;

                    if (w < 0.0001f)
                    {
                        w = 0.0001f;
                    }
                }

                weights[i] = w;
                total += w;
            }

            if (total <= 0f)
            {
                return Range(0, count - 1);
            }

            float roll = NextFloat() * total;
            float cumulative = 0f;
            for (int i = 0; i < count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                {
                    return i;
                }
            }

            return count - 1;
        }

        /// <summary>
        /// Returns a 0..1 "closeness" to the positional bias target for the given index.
        /// Higher means closer to the biased region.
        /// </summary>
        private float GetPositionalCloseness(int index, int count, BiasKind kind)
        {
            if (count <= 1)
            {
                return 1f;
            }

            switch (kind)
            {
                case BiasKind.Min:
                {
                    float denom = (float)(count - 1);
                    if (denom <= 0f) return 1f;
                    return 1f - (float)index / denom;
                }
                case BiasKind.Max:
                {
                    float denom = (float)(count - 1);
                    if (denom <= 0f) return 1f;
                    return (float)index / denom;
                }
                case BiasKind.Extremes:
                {
                    int edgeDistance = Math.Min(index, count - 1 - index);
                    float maxDistance = (count - 1) * 0.5f;
                    if (maxDistance <= 0f) return 1f;
                    return 1f - (float)edgeDistance / maxDistance;
                }
                case BiasKind.Median:
                {
                    if (count <= 2)
                    {
                        return 1f;
                    }

                    float center = (count - 1) * 0.5f;
                    float maxDistance = center;
                    float distance = Math.Abs(index - center);
                    if (maxDistance <= 0f) return 1f;
                    return 1f - distance / maxDistance;
                }
                default:
                    return 0.5f;
            }
        }

        /// <summary>
        /// Frequency-based selector for Mode/Antimode.
        /// </summary>
        private int SelectBiasedIndex(int count, bool[] isBiased, int weight)
        {
            if (count <= 0)
            {
                return -1;
            }

            if (weight < -100) weight = -100;
            if (weight > 100) weight = 100;

            int biasedCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (isBiased[i])
                {
                    biasedCount++;
                }
            }

            if (biasedCount == 0 || biasedCount == count || weight == 0)
            {
                return Range(0, count - 1);
            }

            if (weight > 0)
            {
                int biasPercent = weight;
                if (PercentChance(biasPercent))
                {
                    int targetIndex = Range(0, biasedCount - 1);
                    int seen = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (!isBiased[i]) continue;
                        if (seen == targetIndex) return i;
                        seen++;
                    }
                }

                return Range(0, count - 1);
            }
            else
            {
                int antiPercent = -weight;
                int nonBiasedCount = count - biasedCount;

                if (nonBiasedCount <= 0)
                {
                    return Range(0, count - 1);
                }

                if (PercentChance(antiPercent))
                {
                    int targetIndex = Range(0, nonBiasedCount - 1);
                    int seen = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (isBiased[i]) continue;
                        if (seen == targetIndex) return i;
                        seen++;
                    }
                }

                return Range(0, count - 1);
            }
        }

        private void MarkModeBiased<T>(List<T> list, bool[] isBiased)
        {
            int count = list.Count;
            if (count == 0)
            {
                return;
            }

            int bestCount = 0;
            int i = 0;

            while (i < count)
            {
                int j = i + 1;
                while (j < count && EqualityComparer<T>.Default.Equals(list[j], list[i]))
                {
                    j++;
                }

                int groupSize = j - i;
                if (groupSize > bestCount)
                {
                    bestCount = groupSize;
                }

                i = j;
            }

            if (bestCount <= 1)
            {
                return;
            }

            i = 0;
            while (i < count)
            {
                int j = i + 1;
                while (j < count && EqualityComparer<T>.Default.Equals(list[j], list[i]))
                {
                    j++;
                }

                int groupSize = j - i;
                if (groupSize == bestCount)
                {
                    for (int k = i; k < j; k++)
                    {
                        isBiased[k] = true;
                    }
                }

                i = j;
            }
        }

        private void MarkAntiModeBiased<T>(List<T> list, bool[] isBiased)
        {
            int count = list.Count;
            if (count == 0)
            {
                return;
            }

            int minCount = int.MaxValue;
            int i = 0;

            while (i < count)
            {
                int j = i + 1;
                while (j < count && EqualityComparer<T>.Default.Equals(list[j], list[i]))
                {
                    j++;
                }

                int groupSize = j - i;
                if (groupSize < minCount)
                {
                    minCount = groupSize;
                }

                i = j;
            }

            if (minCount == int.MaxValue)
            {
                return;
            }

            i = 0;
            while (i < count)
            {
                int j = i + 1;
                while (j < count && EqualityComparer<T>.Default.Equals(list[j], list[i]))
                {
                    j++;
                }

                int groupSize = j - i;
                if (groupSize == minCount)
                {
                    for (int k = i; k < j; k++)
                    {
                        isBiased[k] = true;
                    }
                }

                i = j;
            }
        }

        private float GetBiasedFloatTarget(float min, float max, BiasKind kind)
        {
            switch (kind)
            {
                case BiasKind.Min:
                    return min;
                case BiasKind.Max:
                    return max;
                case BiasKind.Extremes:
                    return CoinFlip() ? min : max;
                case BiasKind.Median:
                    return (min + max) * 0.5f;
                default:
                    return Range(min, max);
            }
        }

        private float GetBiasedFloatAvoid(float min, float max, BiasKind kind)
        {
            NumRangeFloat range = new NumRangeFloat(min, max);
            float value = GetRandom(range);

            if (min == max)
            {
                return min;
            }

            int safety = 0;
            const int maxIterations = 32;

            switch (kind)
            {
                case BiasKind.Min:
                    while (value == min && safety < maxIterations)
                    {
                        value = GetRandom(range);
                        safety++;
                    }
                    break;
                case BiasKind.Max:
                    while (value == max && safety < maxIterations)
                    {
                        value = GetRandom(range);
                        safety++;
                    }
                    break;
                case BiasKind.Extremes:
                    while ((value == min || value == max) && safety < maxIterations)
                    {
                        value = GetRandom(range);
                        safety++;
                    }
                    break;
                case BiasKind.Median:
                    float median = (min + max) * 0.5f;
                    while (Math.Abs(value - median) < float.Epsilon && safety < maxIterations)
                    {
                        value = GetRandom(range);
                        safety++;
                    }
                    break;
            }

            return value;
        }
        #endregion
    }
}
