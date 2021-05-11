using Unity.Burst;
using UnityEngine;


public static class Counters
{
    public struct CounterData
    {
        public int NotBurst;
        public int Total;
    }

    public static readonly SharedStatic<CounterData> _Counters = SharedStatic<CounterData>.GetOrCreate<CounterData>();

    public static void Reset()
    {
        _Counters.Data = default;
    }

    [BurstDiscard]
    static void BumpBursted()
    {
        _Counters.Data.NotBurst++;
    }

    public static void Bump(ref bool didRun)
    {
        if (didRun)
            return;
        didRun = true;
        BumpBursted();
        _Counters.Data.Total++;
    }
}