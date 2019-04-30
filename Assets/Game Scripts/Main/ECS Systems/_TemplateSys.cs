//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Transforms;

//[DisableAutoCreation]
//public class TemplateSys : JobComponentSystem
//{
//    private BeginInitializationEntityCommandBufferSystem cmdBufferSystem;

//    protected override void OnCreate()
//    {
//        cmdBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
//    }

//    [BurstCompile]
//    private struct Job : IJobForEachWithEntity<Translation>
//    {
//        public EntityCommandBuffer.Concurrent CommandBuffer;

//        public void Execute(Entity entity, int index, [ReadOnly] ref Translation tran)
//        {
//        }
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        var job = new Job()
//        {
//            CommandBuffer = cmdBufferSystem.CreateCommandBuffer().ToConcurrent(),
//        };

//        JobHandle jh = job.Schedule(this, inputDeps);
//        cmdBufferSystem.AddJobHandleForProducer(jh);
//        return jh;
//    }
//}