using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonRandom
{
    /// <summary>
    /// Double–inclusive range for ints.
    /// </summary>
    [Serializable]
    public struct NumRangeInt : IEnumerable<int>
    {
        public int Min;
        public int Max;

        public NumRangeInt(int min, int max)
        {
            if (min > max)
            {
                UnityEngine.Debug.LogWarning("Min cannot be greater than max - swapping values.");
                int temp = min;
                min = max;
                max = temp;
            }
            
            Min = min;
            Max = max;
        }
        
        public int Count()
        {
            return Max - Min + 1;
        }

        public bool Contains(int value)
        {
            return value >= Min && value <= Max;
        }

        public bool IsDefault()
        {
            return Min == 0 && Max == 0;
        }
        
        public override string ToString()
        {
            return $"NumRangeInt({Min}, {Max})";
        }

        /// <summary>
        /// Returns a new array containing every integer in the range from Min to Max inclusive.
        /// </summary>
        public int[] ToArray()
        {
            int length = Max - Min + 1;
            if (length <= 0)
            {
                return new int[0];
            }

            int[] result = new int[length];
            int index = 0;

            for (int value = Min; value <= Max; value++)
            {
                result[index] = value;
                index++;
            }

            return result;
        }

        /// <summary>
        /// Returns a new list containing every integer in the range from Min to Max inclusive.
        /// </summary>
        public List<int> ToList()
        {
            int length = Max - Min + 1;
            if (length <= 0)
            {
                return new List<int>();
            }

            List<int> list = new List<int>(length);
            for (int value = Min; value <= Max; value++)
            {
                list.Add(value);
            }
            
            return list;
        }

        /// <summary>
        /// Enumerator that yields all integer values from Min to Max (inclusive).
        /// </summary>
        public struct Enumerator : IEnumerator<int>
        {
            private readonly int min;
            private readonly int max;
            private int current;  // Camel case to match .net enumerator implementation

            public Enumerator(int minVal, int maxVal)
            {
                min = minVal;
                max = maxVal;
                current = minVal - 1;
            }

            public int Current
            {
                get { return current; }
            }

            object IEnumerator.Current
            {
                get { return current; }
            }

            public bool MoveNext()
            {
                current++;
                return current <= max;
            }

            public void Reset()
            {
                current = min - 1;
            }

            public void Dispose()
            {
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(Min, Max);
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return new Enumerator(Min, Max);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(Min, Max);
        }

        /// <summary>
        /// Finds the value in the passed list that is closest to this range.
        /// If one or more values fall inside the range, the value closest to the median of the range is chosen.
        /// If no values fall inside the range, the value closest to Min or Max is chosen.
        /// </summary>
        /// <returns>False if the closest value was unique or if the input was invalid; true if there was a tie.</returns>
        public bool FindNearest(List<int> values, out int nearest)
        {
            nearest = 0;

            if (values == null || values.Count == 0)
            {
                Debug.LogWarning("NumRangeInt.FindNearest: values list is null or empty.");
                return false;
            }

            int median = Min + (Max - Min) / 2;
            bool anyInside = false;
            
            for (int i = 0; i < values.Count; i++)
            {
                if (Contains(values[i]))
                {
                    anyInside = true;
                    break;
                }
            }

            int bestDistance = int.MaxValue;
            bool tie = false;

            if (anyInside)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    int value = values[i];
                    if (!Contains(value))
                    {
                        continue;
                    }

                    int distance = Math.Abs(value - median);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearest = value;
                        tie = false;
                    }
                    else if (distance == bestDistance)
                    {
                        tie = true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < values.Count; i++)
                {
                    int value = values[i];

                    int distanceToMin = Math.Abs(value - Min);
                    int distanceToMax = Math.Abs(value - Max);
                    int distance = distanceToMin < distanceToMax ? distanceToMin : distanceToMax;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearest = value;
                        tie = false;
                    }
                    else if (distance == bestDistance)
                    {
                        tie = true;
                    }
                }
            }

            if (bestDistance == int.MaxValue)
            {
                Debug.LogWarning("NumRangeInt.FindNearest: no candidate values matched criteria.");
                return false;
            }

            return tie;
        }
    }

    /// <summary>
    /// Double–inclusive range for floats. Floats are treated like ints (step is 1.0).
    /// </summary>
    [Serializable]
    public struct NumRangeFloat
    {
        public float Min;
        public float Max;

        public NumRangeFloat(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(float value)
        {
            return value >= Min && value <= Max;
        }

        /// <summary>
        /// Finds the value in the passed list that is closest to this range.
        /// If one or more values fall inside the range, the value closest to the median of the range is chosen.
        /// If no values fall inside the range, the value closest to Min or Max is chosen.
        /// </summary>
        /// <returns>False if the closest value was unique or if the input was invalid; true if there was a tie.</returns>
        public bool FindNearest(List<float> values, out float nearest)
        {
            nearest = 0f;

            if (values == null || values.Count == 0)
            {
                Debug.LogWarning("NumRangeFloat.FindNearest: values list is null or empty.");
                return false;
            }

            float median = (Min + Max) * 0.5f;

            bool rangeContainsValue = false;
            for (int i = 0; i < values.Count; i++)
            {
                if (Contains(values[i]))
                {
                    rangeContainsValue = true;
                    break;
                }
            }

            float bestDistance = float.PositiveInfinity;
            bool tie = false;

            if (rangeContainsValue)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    float value = values[i];
                    if (!Contains(value))
                    {
                        continue;
                    }

                    float distance = Mathf.Abs(value - median);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearest = value;
                        tie = false;
                    }
                    else if (Mathf.Approximately(distance, bestDistance))
                    {
                        tie = true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < values.Count; i++)
                {
                    float value = values[i];

                    float distanceToMin = Mathf.Abs(value - Min);
                    float distanceToMax = Mathf.Abs(value - Max);
                    float distance = distanceToMin < distanceToMax ? distanceToMin : distanceToMax;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearest = value;
                        tie = false;
                    }
                    else if (Mathf.Approximately(distance, bestDistance))
                    {
                        tie = true;
                    }
                }
            }

            if (float.IsPositiveInfinity(bestDistance))
            {
                Debug.LogWarning("NumRangeFloat.FindNearest: no candidate values matched criteria.");
                return false;
            }

            return tie;
        }
        
        public override string ToString()
        {
            return $"NumRangeFloat({Min}, {Max})";
        }
    }
}
