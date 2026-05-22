using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeightedRandom
{
    // Picks an item using relative weights. Items with weight <= 0 are skipped.
    // Returns default(T) if no items have positive weight.
    public static T Pick<T>(IReadOnlyList<T> items, Func<T, float> getWeight)
    {
        if (items == null || items.Count == 0) return default;

        float total = 0f;
        foreach (var item in items)
        {
            float w = getWeight(item);
            if (w > 0f) total += w;
        }

        if (total <= 0f) return default;

        float roll = UnityEngine.Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var item in items)
        {
            float w = getWeight(item);
            if (w <= 0f) continue;
            cumulative += w;
            if (roll < cumulative) return item;
        }
        return items[items.Count - 1];
    }
}
