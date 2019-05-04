using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[DisableAutoCreation]
public class BehaviourTreeConcept : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem cmdBufferSystem;

    protected override void OnCreate()
    {
        cmdBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    public struct Job : IJobForEachWithEntity<Node>
    {
        [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
        [ReadOnly] public ComponentDataFromEntity<Node> Node;
        [ReadOnly] public ComponentDataFromEntity<Translation> Tran;
        [ReadOnly] public ComponentDataFromEntity<Rotation> Rot;
        [ReadOnly] public ComponentDataFromEntity<NearestEnemy> NearestEnemy;

        //public NativeHashMap<Entity>

        public void Execute(Entity e, int index, [ReadOnly] ref Node rootNode)
        {
            // execute root node to put the entity into a particular 'state'
            // this should result on various tags being applied
            NodeResult r = rootNode.DoTask(e, Node[e], Tran[e], Rot[e], NearestEnemy[e]);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            CommandBuffer = cmdBufferSystem.CreateCommandBuffer().ToConcurrent(),
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        cmdBufferSystem.AddJobHandleForProducer(jh);
        return jh;
    }
}

public struct NodeComponents
{
    public ComponentDataFromEntity<SequenceNode> Sequence;
    public ComponentDataFromEntity<MoveNode> Move;
}