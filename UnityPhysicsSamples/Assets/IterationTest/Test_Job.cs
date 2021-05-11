using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Test_Job
{
    
    struct TestData_XXX : IComponentData
    {
        public float3 pos;
        public int   output;
    }

    public class TestSystem_XXX : SystemBase
    {
        private BuildPhysicsWorld physics;

        private EntityQuery _Query;
        
        [BurstCompile(OptimizeFor = OptimizeFor.FastCompilation)]
        unsafe struct TestJob : IJobEntityBatch
        {
            public ComponentTypeHandle<TestData_XXX> TestData;
            public CollisionWorld QueryWorld;
            public bool DidCheck;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                Counters.Bump(ref DidCheck);
                
                var arr = batchInChunk.GetNativeArray(TestData);
                var ptr = (TestData_XXX*)arr.GetUnsafePtr();

                for (int i = 0; i != arr.Length; i++)
                {
                    ref var data = ref ptr[i];

                    var res = QueryWorld.CheckSphere(data.pos, 100, CollisionFilter.Default);
                    if (res)
                        data.output++;
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            physics = World.GetExistingSystem<BuildPhysicsWorld>();

            var arch = EntityManager.CreateArchetype(typeof(TestData_XXX));
            EntityManager.CreateEntity(arch, 1);

            _Query = GetEntityQuery(typeof(TestData_XXX));
        }

        protected override void OnUpdate()
        {
            var query = physics.PhysicsWorld.CollisionWorld;

            var job = new TestJob {QueryWorld = query, TestData = GetComponentTypeHandle<TestData_XXX>()};
            job.Run(_Query);
        }
    }
}
          