using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateAfter(typeof(LateSimGameGroup))]
[AlwaysUpdateSystem]
public class StructureSyncSys : ComponentSystem
{
    private EntityQuery destroyedShipsQuery;
    private EntityQuery hasTriggerInfoTagQuery;

    private JobHandle dependencies;

    protected override void OnCreate()
    {
        destroyedShipsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ShipSpawnerOwnerSsShC) },
            None = new ComponentType[] { typeof(SpawnTime) }
        });

        hasTriggerInfoTagQuery = GetEntityQuery(typeof(HasTriggerInfoTag));
    }

    public void AddJobHandleForProducer(JobHandle producerJob)
    {
        dependencies = JobHandle.CombineDependencies(dependencies, producerJob);
    }

    protected override void OnUpdate()
    {
        dependencies.Complete();
        dependencies = new JobHandle();

        // 2. Remove destroyed ship system shared components
        World.Active.EntityManager.RemoveComponent(destroyedShipsQuery, typeof(ShipSpawnerOwnerSsShC));

        // 3. Remove trigger buffers
        World.Active.EntityManager.RemoveComponent(hasTriggerInfoTagQuery, typeof(HasTriggerInfoTag));
    }
}