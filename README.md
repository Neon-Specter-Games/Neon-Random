# Neon Random

Neon Random is a deterministic, seedable, serializable RNG solution for Unity.

Available for free on the Unity Asset Store or on [GitHub](https://github.com/Neon-Specter-Games/Neon-Random)

NOTE: If you want to use this for a non-Unity project, you can easily make it engine-agnostic by replacing a couple of math and logging calls.

---

## Features

* Deterministic RNG that can survive game loads or restarts
* Prevent RNG cross-contamination with decoupled RNG states
* Documented source code
* Simple, easy to use, consistent API
* Quickly get any number of random items from collections or number ranges
* Weighted collections with a definable probability for getting each item
* Lots of other useful methods like: PlusMinusFlat/Percent, PercentChance, CoinFlip, RandomInsert, Shuffle, and more
* All RNG methods can be biased to skew the output probabilities (towards the median, extremes, mode, etc.)

---

## Getting Started

1. Import the package using Unity Package Manager
2. Add `using NeonRandom` to your script
3. Create and seed a NeonRng instance:

   ```csharp
   NeonRng rng = new NeonRng("my seed");
   ```

---

## Seeding The RNG

* If you use a fixed seed like in the above example, you will get the same RNG every time. This is intentional and allows for deterministic RNG across loads and restarts.
* If you want different RNG every time (e.g. for quick testing), you can seed the RNG with:

  ```csharp
  Guid.NewGuid().ToString()
  ```
* Using a Guid is perfectly fine for initializing the RNG; but if you want human readable seeds, you'll need to build or find your own seed generator.

---

## Usage Examples

**NOTE:** All range definitions and method parameters are **double inclusive** (as opposed to Unity's inconsistent implementation). UnityRange methods that emulate `UnityEngine.Random` inclusivity are provided for easy migration.

Assuming we have a NeonRng instance called `rng`:

### Generating Numbers

* Get a random float:

  ```csharp
  rng.NextFloat();
  ```

* Get a random int from 1 to 10:

  ```csharp
  rng.Range(1, 10);
  ```

* Get an int plus or minus 5:

  ```csharp
  rng.PlusMinusFlat(10, 5); // Returns 5–15
  ```

* Get 2.5 plus or minus 60%:

  ```csharp
  rng.PlusMinusPercent(2.5f, 60); // Returns 1.0–4.0
  ```

* 70% chance to do something:

  ```csharp
  if (rng.PercentChance(70)) { }
  ```

---

### Working with Collections and NumRanges

* Get a random int from a pre-defined NumRange:

  ```csharp
  NumRangeInt range = new NumRangeInt(2, 5);
  rng.GetRandom(range);
  ```

* Get a random int with a 70% bias towards the median:

  ```csharp
  rng.GetRandom(range, RngBias.Median(70));
  ```

* Get a random KVP from a Dictionary:

  ```csharp
  rng.GetRandom(myDict);
  ```

* Get 3 random items from a List with a bias:

  ```csharp
  rng.GetRandom(myList, 3, RngBias.AntiMode(30));
  ```

* Shuffle a list:

  ```csharp
  rng.Shuffle(myList);
  ```

* Insert a collection into a list at random indices:

  ```csharp
  rng.RandomInsert(myList, collectionToInsert, RngBias.Extremes(80));
  ```

---

### Working with Weighted Collections

Implement a drop table using a Weighted Collection:

```csharp
WeightedCollection<string> lichDrops = new WeightedCollection<string>()
{
    {"Tattered Cloth", 45},
    {"Silver Coin", 45},
    {"Lich Blade", 10}
};
```

Get a random item:

```csharp
rng.GetRandomWeighted(lichDrops);
```

This has:

* 45% chance for "Tattered Cloth"
* 45% chance for "Silver Coin"
* 10% chance for "Lich Blade"

**Notes:**

1. Weighted collections are based on total weight, not percent. If weights don’t sum to 100, probability = `item weight / total weight`.
2. `WeightedCollection` is generic and supports any type (strings used here for simplicity).

---

## Preventing RNG Cross-Contamination

Every RNG call advances the RNG state. This means results depend on how many calls have been made—even with the same seed.

To avoid unintended coupling, use multiple RNG instances:

```csharp
public void InitRng(string seed)
{
    MasterRng = new NeonRng(seed);
    PlayerRng = new NeonRng(MasterRng.NextUInt().ToString());
    EnemyRng = new NeonRng(MasterRng.NextUInt().ToString());
    DropRng = new NeonRng(MasterRng.NextUInt().ToString());
}
```

Each system now has an isolated RNG state, preventing cross-contamination.

---

## Saving & Loading RNG States

Saving and loading RNG is straightforward. Since the entire RNG state is contained within the `NeonRng` instance, simply serialize and deserialize it like any other object.
