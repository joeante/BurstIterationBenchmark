using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using UnityEditor;
using UnityEngine;


[UpdateInGroup(typeof(InitializationSystemGroup))]
class MyTest : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        IterationPerformance.SetTime("Timer-OnCreate");
        Counters.Reset();
    }

    struct BurstCounter
    {
        public int Bursted;
        public int Total;
    }
    List<BurstCounter> _Counters = new List<BurstCounter>();
    
    void ExtractAndResetCounters()
    {
        _Counters.Add(new BurstCounter
        {
            Bursted = Counters._Counters.Data.Total - Counters._Counters.Data.NotBurst,
            Total = Counters._Counters.Data.Total
        });
        Counters.Reset();
    }

    private int Frame = 0;

    protected override void OnUpdate()
    {
        if (Frame == 0)
        {
            IterationPerformance.SetTime("Timer-0Frame");
            ExtractAndResetCounters();
        }

        if (Frame == 1)
        {
            IterationPerformance.SetTime("Timer-1Frame");
            ExtractAndResetCounters();
        }
        
        if (Frame == 2)
        {
            IterationPerformance.SetTime("Timer-2Frame");
            ExtractAndResetCounters();
        }
            
        if (Frame == 3)
        {
            IterationPerformance.SetTime("Timer-3Frame");
            ExtractAndResetCounters();

            var total = IterationPerformance.GetTime("Timer-TriggerBegin", "Timer-SecondFrame");
            var domainReload = IterationPerformance.GetTime("Timer-TriggerBegin", "Timer-DomainReload");
            var onCreate= IterationPerformance.GetTime("Timer-DomainReload", "Timer-OnCreate");
            var firstFrame= IterationPerformance.GetTime("Timer-OnCreate", "Timer-0Frame");
            var secondFrame= IterationPerformance.GetTime("Timer-0Frame", "Timer-1Frame");
            var thirdFrame = IterationPerformance.GetTime("Timer-1Frame", "Timer-2Frame");
            var fourthFrame = IterationPerformance.GetTime("Timer-2Frame", "Timer-3Frame");
            
            Debug.Log($"Total: {total} DomainReload: {domainReload} OnCreate: {onCreate} OnUpdate0 {firstFrame} OnUpdate1: {secondFrame} OnUpdate2: {thirdFrame} OnUpdate3: {fourthFrame}");
            Debug.Log($"Frame1: {_Counters[1].Bursted} of {_Counters[1].Total}, Frame2: {_Counters[2].Bursted} of {_Counters[2].Total} Frame3: {_Counters[3].Bursted} of {_Counters[3].Total}");
        }
        Frame++;
    }
}

public class IterationPerformance : MonoBehaviour
{
    public static void SetTime(string name)
    {
        EditorPrefs.SetString(name, DateTime.Now.ToBinary().ToString());
    }
    
    public static int GetTime(string previous, string name)
    {
        var sP = EditorPrefs.GetString(previous);
        var p = DateTime.FromBinary(long.Parse(sP));

        var sN = EditorPrefs.GetString(name);
        var n = DateTime.FromBinary(long.Parse(sN));

        var diff = n.Subtract(p);
        return (int)diff.TotalMilliseconds;
    }


    [InitializeOnLoadMethod]
    static void CompletedDomainReload()
    {        
        IterationPerformance.SetTime("Timer-DomainReload");
    }
    
    [MenuItem("Iteration/TriggerChange")]
    static void Trigger()
    {
        var changeFile = "Assets/IterationTest/Test_ForEach.cs";
        
        var src = File.ReadAllText(changeFile);
        File.WriteAllText(changeFile, src + " ");

        IterationPerformance.SetTime("Timer-TriggerBegin");

        AssetDatabase.Refresh();
        EditorApplication.EnterPlaymode();
    }
    
    [MenuItem("Iteration/Rebuild Scale Systems")]
    static void GenerateScripts()
    {
        int count = 500;
        var baseName = "Test_Job";
        
        
        if (Directory.Exists("Assets/IterationTest/Gen"))
            Directory.Delete("Assets/IterationTest/Gen", true);
        Directory.CreateDirectory("Assets/IterationTest/Gen");
        var src = File.ReadAllText($"Assets/IterationTest/{baseName}.cs");
        for (int i = 0; i != count; i++)
        {
            var unique = src.Replace("XXX", $"{i}");
            File.WriteAllText($"Assets/IterationTest/Gen/{baseName}-{i}.cs", unique);
        }
        
        AssetDatabase.Refresh();
    }
}
