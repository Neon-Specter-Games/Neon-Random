using System;
using System.Collections;
using System.Collections.Generic;

namespace NeonRandom
{
    /// <summary>
    /// Extended dictionary of <object, weight>. Can be passed to BetterRandom.GetRandomWeighted to get a random item from the dictionary based on the weights. 
    /// </summary>
    public class WeightedCollection<T> : IDictionary<T, int>
    {
        private readonly Dictionary<T, int> InnerDictionary;
        public int TotalWeight;

        public WeightedCollection()
        {
            InnerDictionary = new Dictionary<T, int>();
            TotalWeight = 0;
        }

        public void Add(T key, int value)
        {
            if (InnerDictionary.ContainsKey(key))
            {
                throw new ArgumentException("An element with the same key already exists in the WeightedCollection.");
            }

            int clamped = value < 0 ? 0 : value;
            InnerDictionary.Add(key, clamped);
            TotalWeight += clamped;
        }

        public bool Remove(T key)
        {
            int existingValue;
            if (!InnerDictionary.TryGetValue(key, out existingValue))
            {
                return false;
            }

            bool removed = InnerDictionary.Remove(key);
            if (removed)
            {
                TotalWeight -= existingValue;
            }

            return removed;
        }

        public bool ContainsKey(T key)
        {
            return InnerDictionary.ContainsKey(key);
        }

        public bool TryGetValue(T key, out int value)
        {
            return InnerDictionary.TryGetValue(key, out value);
        }

        public int this[T key]
        {
            get { return InnerDictionary[key]; }
            set
            {
                int clamped = value < 0 ? 0 : value;

                int oldValue;
                if (InnerDictionary.TryGetValue(key, out oldValue))
                {
                    TotalWeight -= oldValue;
                    InnerDictionary[key] = clamped;
                    TotalWeight += clamped;
                }
                else
                {
                    InnerDictionary[key] = clamped;
                    TotalWeight += clamped;
                }
            }
        }

        public ICollection<T> Keys
        {
            get { return InnerDictionary.Keys; }
        }

        public ICollection<int> Values
        {
            get { return InnerDictionary.Values; }
        }

        public int Count
        {
            get { return InnerDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(KeyValuePair<T, int> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            InnerDictionary.Clear();
            TotalWeight = 0;
        }

        public bool Contains(KeyValuePair<T, int> item)
        {
            if (!InnerDictionary.TryGetValue(item.Key, out int existingValue))
            {
                return false;
            }

            return EqualityComparer<int>.Default.Equals(existingValue, item.Value);
        }

        public void CopyTo(KeyValuePair<T, int>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<T, int>>)InnerDictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<T, int> item)
        {
            if (!Contains(item))
            {
                return false;
            }

            return Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<T, int>> GetEnumerator()
        {
            return InnerDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InnerDictionary.GetEnumerator();
        }
    }
}
