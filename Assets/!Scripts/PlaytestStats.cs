using System.Collections.Generic;
using UnityEngine;

public static class PlaytestStats
{
    public class Entry
    {
        public float damage;
        public float combatTime;
        public int hits;

        public float MeasuredDps => combatTime > 0.5f ? damage / combatTime : 0f;
    }

    private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => entries.Clear();

    public static void Reset() => entries.Clear();

    public static Entry Get(string key)
    {
        if (!entries.TryGetValue(key, out var e))
        {
            e = new Entry();
            entries[key] = e;
        }
        return e;
    }

    public static void AddDamage(string key, float amount)
    {
        var e = Get(key);
        e.damage += amount;
        e.hits++;
    }

    public static void AddCombatTime(string key, float dt)
    {
        Get(key).combatTime += dt;
    }
}
