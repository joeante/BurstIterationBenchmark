//#define USE_PHYSICS_QUERY

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

namespace Test_Job_ISystemBase
{
    struct TestData_XXX : IComponentData
    {
        public float3 pos;
        public int   output;
    }

    [BurstCompile]
    public struct Test_Job_ISystem_XXX : ISystemBase
    {
        const int kExpectedVersion = 3;
        private EntityQuery _Query;
        private ComponentTypeHandle<TestData_XXX> _typeHandle;
        
        [BurstCompile(OptimizeFor = OptimizeFor.FastCompilation)]
        unsafe struct TestJob : IJobEntityBatchNonJob
        {
            public ComponentTypeHandle<TestData_XXX> TestData;
            public CollisionWorld QueryWorld;
            public int Version;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                if (Version != kExpectedVersion)
                    UnityEngine.Debug.LogError("Compiled version doesn't match");

                Counters.Bump();
                
                var arr = batchInChunk.GetNativeArray(TestData);
                var ptr = (TestData_XXX*)arr.GetUnsafePtr();
                
                for (int i = 0 ; i < arr.Length; i++)
                {
                    ref var data = ref ptr[i];

                    var res = false;
                    if (data.output == -1)
                         res = QueryWorld.CheckSphere(data.pos, 100, CollisionFilter.Default);
                    if (res)
                        data.output++;
                }
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.FastCompilation, DisableDirectCall = true)]
        unsafe public void OnCreate(ref SystemState systemState)
        {
           //physics = World.GetExistingSystem<BuildPhysicsWorld>();

           var entity = systemState.EntityManager.CreateEntity();
           systemState.EntityManager.AddComponentData(entity, new TestData_XXX{});

           _typeHandle = systemState.GetComponentTypeHandle<TestData_XXX>();
           
           var queryDescBuilder = new EntityQueryDescBuilder(Allocator.Temp);
           queryDescBuilder.AddAll(ComponentType.ReadWrite<TestData_XXX>());
           queryDescBuilder.FinalizeQuery();
           
           //@TODO: Using systemstate crashes right now
           _Query = systemState.EntityManager.CreateEntityQuery(queryDescBuilder);
           
           queryDescBuilder.Dispose();
  
        }
        
        //@TODO: DisableDirectCall has a significant perf impact in avoiding auto registration of function pointers
        [BurstCompile(OptimizeFor = OptimizeFor.FastCompilation, DisableDirectCall = true)]
        public void OnDestroy(ref SystemState systemState)
        {
        }

        [BurstCompile(OptimizeFor = OptimizeFor.FastCompilation, DisableDirectCall = true)]
        public void OnUpdate(ref SystemState systemState)
        {
            _typeHandle.Update(ref systemState);
            
            var job = new TestJob
            {
                TestData = _typeHandle, 
                Version = kExpectedVersion
            };

            job.RunWithoutJobs(_Query);
        }
    }
}
