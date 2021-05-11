using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Test_ForEach
{
    struct TestData_XXX : IComponentData
    {
        public float3 pos;
        public int   output;
    }

    public class TestSystem_XXX : SystemBase
    {
        private BuildPhysicsWorld physics;

        protected override void OnCreate()
        {
            base.OnCreate();
            physics = World.GetExistingSystem<BuildPhysicsWorld>();

            var arch = EntityManager.CreateArchetype(typeof(TestData_XXX));
            EntityManager.CreateEntity(arch, 1);
        }

        protected override void OnUpdate()
        {
            var query = physics.PhysicsWorld.CollisionWorld;

            Entities.WithoutBurst().ForEach((ref TestData_XXX data) =>
            {
                var res = query.CheckSphere(data.pos, 100, CollisionFilter.Default);
                if (res)
                    data.output++;
            
            }).Run();
        }
    }
}
 