---
hide:
  - navigation
---

# Neon Random

Neon Random is a deterministic, seedable, serializable RNG solution for Unity.
<br>Available for free on the Unity Asset Store or at: https://github.com/Neon-Specter-Games/Neon-Random

## Features
* Deterministic RNG that can survive game loads or restarts.
* Prevent RNG cross-contamination with decoupled RNG states.
* Documented source code.
* Simple, easy to use, consistent API.
* Quickly get any number of random items from collections or number ranges.
* Weighted collections with a definable probability for getting each item.
* Lots of other useful methods like:  PlusMinusFlat/Percent, PercentChance, CoinFlip, RandomInsert, Shuffle, and more.
* All RNG methods can be biased to skew the output probabilities (towards the median, extremes, mode, etc.)

## Getting Started

1. Import the package using Unity Package Manager.
2. Add `using NeonRandom` to your script.
3. Create and seed a NeonRng instance:  `NeonRng rng = new NeonRng("my seed");`

## Seeding The RNG

* If you use a fixed seed like in the above example, you will get the same RNG every time. This is intentional and allows for deterministic RNG across loads and restarts.
* If you want different RNG every time (e.g. for quick testing), you can seed the RNG with `Guid.NewGuid().ToString()`
* Using a Guid is perfectly fine for initializing the RNG; but if you want human readable seeds, you'll need to build or find your own seed generator.

## Usage Examples

NOTE: All range definitions and method parameters are <b>double inclusive</b> (as opposed to Unity's inconsistent implementation). UnityRange methods that emulate UnityEngine.Random inclusivity are provided for easy migration.

Assuming we have a NeonRng instance called `rng`:

<h5>Generating Numbers</h5>
* Get a random float:  `rng.NextFloat();`
* Get a random int from 1 to 10:  `rng.Range(1, 10);`
* Get an int plus or minus 5:  `rng.PlusMinusFlat(10, 5);` This yields an int from 5 to 15.
* Get 2.5 plus or minus 60%:  `rng.PlusMinusPercent(2.5f, 60);` This yields a float 1.0 to 4.0.
* 70% chance to do something:  `if (rng.PercentChance(70)) ...`

<h5>Working with collections and NumRanges</h5>
* Get a random int from a pre-defined NumRange: `NumRangeInt range = new NumRangeInt(2, 5);`<br> then: `rng.GetRandom(range);`
* Get a random int from a NumRange, with a 70% bias towards the median:  `rng.GetRandom(range, RngBias.Median(70));`
* Get a random KVP from a Dictionary:  `rng.GetRandom(myDict);`
* Get 3 random items from a List, with a 30% bias towards the least common items:<br> `rng.GetRandom(myList, 3, RngBias.AntiMode(30));`
* Shuffle a list:  `rng.Shuffle(myList);`
* Insert a collection of items into a list at random indices, with an 80% bias towards the extremes (avoiding the middle of the list):<br>`rng.RandomInsert(myList, collectionToInsert, RngBias.Extremes(80))`

<h5>Working with Weighted Collections</h5>
Implement a drop table using a Weighted Collection:
    
    WeightedCollection<string> lichDrops = new WeightedCollection<string>()
    {
        {"Tattered Cloth", 45},
        {"Silver Coin", 45},
        {"Lich Blade", 10}
    };
    
Then to get a random item from this collection: `rng.GetRandomWeighted(lichDrops);`<br> This has a 45% chance to return "Tattered Cloth" or "Silver Coin" and a 10% chance to return "Lich Blade".

Note:<br>
1. Weighted collections are based on total weight, not percent. In the example above, the weights add up to 100, so it's effectively percentage-based. If the weights don't add up to 100, the probability of getting an item will be:  <i>item weight</i> / <i>total weight</i>.<br>
2. WeightedCollection is generic - it supports any object type. The above example uses strings for simplicity, but you would probably want to use a factory or enum instead.

## Preventing RNG Cross-Contamination

Every time an RNG call is made to a NeonRng instance, it progresses the RNG state. This means that RNG outcomes will vary depending on the number of calls that have been made to that NeonRng instance (even if the seed is the same).

To avoid unintentional progression of the RNG state, you can use multiple NeonRng instances, each with its own dedicated role. Since each instance has its own state, RNG calls made to one instance will not affect the others.

Here's an example implementation that initializes several instances, each with their own role:

```csharp
public void InitRng(string seed)
{
    MasterRng = new NeonRng(seed);
    PlayerRng = new NeonRng(MasterRng.NextUInt().ToString());
    EnemyRng = new NeonRng(MasterRng.NextUInt().ToString());
    DropRng = new NeonRng(MasterRng.NextUInt().ToString());
}
```

In this example, we would use PlayerRng for anything that can be triggered by the player. Now our RNG states are decoupled, so the player won't be able to manipulate enemy actions or drops by reloading and taking different actions.

## Saving & Loading RNG States

Saving and loading RNG is simple. Since the entire RNG state is contained within the NeonRng instance, all you have to do is serialize and deserialize your RNG instances like you would with any other object.