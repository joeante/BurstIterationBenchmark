using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

class BootStrap : ICustomBootstrap
{
    public bool Initialize(string defaultWorldName)
    {
        IterationPerformance.SetTime("Timer-WorldInitializeBegin");
        
        var world = new World(defaultWorldName, WorldFlags.Game);
        World.DefaultGameObjectInjectionWorld = world;

        var systemList = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default, false);
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systemList);

#if !UNITY_DOTSRUNTIME
        ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);
#endif

        IterationPerformance.SetTime("Timer-WorldInitializeEnd");
        return true;
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
class MyTest : SystemBase
{
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

            var total = IterationPerformance.GetTime("Timer-TriggerBegin", "Timer-3Frame");
            var compile = IterationPerformance.GetTime("Timer-TriggerBegin", "Timer-BeforeAssemblyReload");
            var domainReload = IterationPerformance.GetTime("Timer-BeforeAssemblyReload", "Timer-WorldInitializeBegin");
            var worldCreate= IterationPerformance.GetTime("Timer-WorldInitializeBegin", "Timer-WorldInitializeEnd");
            var firstFrame= IterationPerformance.GetTime("Timer-WorldInitializeEnd", "Timer-0Frame");
            var secondFrame= IterationPerformance.GetTime("Timer-0Frame", "Timer-1Frame");
            var thirdFrame = IterationPerformance.GetTime("Timer-1Frame", "Timer-2Frame");
            var fourthFrame = IterationPerformance.GetTime("Timer-2Frame", "Timer-3Frame");
            
            Debug.Log($"Total: {total} Compile: {compile} DomainReload: {domainReload} WorldCreate: {worldCreate} PrepareFrame0: {firstFrame} OnUpdate0: {secondFrame} OnUpdate1: {thirdFrame} OnUpdate2: {fourthFrame}");
            Debug.Log($"Frame1: {_Counters[0].Bursted} of {_Counters[0].Total}, Frame1: {_Counters[1].Bursted} of {_Counters[1].Total}, Frame2: {_Counters[2].Bursted} of {_Counters[2].Total} Frame3: {_Counters[3].Bursted} of {_Counters[3].Total}");
        }
        Frame++;
    }
}

public class IterationPerformance : MonoBehaviour
{
    static int GeneratedSystemCount = 1000;
    static int GeneratedSystemsPerAsmDef = 50;

    static int ChangedSystems = 2;
    static int ChangedAsmDef = 2;
    static bool ChangeBurstCode = true;
    const string kBaseScriptName = "Test_ISystemBase";

    
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
        
        AssemblyReloadEvents.beforeAssemblyReload += delegate { IterationPerformance.SetTime("Timer-BeforeAssemblyReload"); };
    }

    [MenuItem("Iteration/TriggerChange")]
    static void Trigger()
    {
        for (int i = 0; i != ChangedSystems; i++)
        {
            int asmDef = i % ChangedAsmDef;
            int indexInAsmDef = i / ChangedAsmDef;
            int csIndex = indexInAsmDef + asmDef * GeneratedSystemsPerAsmDef;

            var changeFile =$"Assets/IterationTest/Gen{asmDef}/{kBaseScriptName}-{csIndex}.cs";
        
            var src = File.ReadAllText(changeFile);
            if (ChangeBurstCode)
            {
                File.ReadAllText(changeFile);

                var marker = "const int kExpectedVersion = ";
                var index = src.IndexOf(marker) + marker.Length;
                
                var endIndex = src.IndexOf(';', index);
                var version = int.Parse(src.Substring(index, endIndex - index));
                version++;
                src = src.Remove(index, endIndex - index);
                src = src.Insert(index, version.ToString());
            }
            else
            {
                src = File.ReadAllText(changeFile);
                src = src + " ";
            }

            File.WriteAllText(changeFile, src);
        }

        IterationPerformance.SetTime("Timer-TriggerBegin");

        AssetDatabase.Refresh();
        EditorApplication.EnterPlaymode();
    }
    
    [MenuItem("Iteration/Rebuild Scale Systems")]
    static void GenerateScripts()
    {
        int asmDefs = GeneratedSystemCount / GeneratedSystemsPerAsmDef;

        // Generate asmdefs
        for (int i = 0; i != 1000; i++)
        {
            var dir = $"Assets/IterationTest/Gen{i}";
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
         
        for (int i = 0; i != asmDefs; i++)
        {
            var dir = $"Assets/IterationTest/Gen{i}";
            Directory.CreateDirectory(dir);
            var asmdef = File.ReadAllText("Assets/IterationTest/GenXXX/GenXXX.asmdef");

            asmdef = asmdef.Replace("XXX", i.ToString());
            File.WriteAllText($"Assets/IterationTest/Gen{i}/Gen{i}.asmdef", asmdef);
        }
        
        // Generate systems
        var src = File.ReadAllText($"Assets/IterationTest/GenXXX/{kBaseScriptName}.cs");
        for (int i = 0; i != GeneratedSystemCount; i++)
        {
            int asmDefIndex = i / GeneratedSystemsPerAsmDef;
            
            var unique = src.Replace("XXX", $"{i}");
            File.WriteAllText($"Assets/IterationTest/Gen{asmDefIndex}/{kBaseScriptName}-{i}.cs", unique);
        }

        AssetDatabase.Refresh();
    }
/*
    [MenuItem("Iteration/Rebuild Scale Scene")]

    static void GenerateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        SceneManager.SetActiveScene(scene);

        for (int i = 0; i != GeneratedSystemCount; i++)
        {
            AssetDatabase.FindAssets("Script", "")  
        }
    }
*/
}
