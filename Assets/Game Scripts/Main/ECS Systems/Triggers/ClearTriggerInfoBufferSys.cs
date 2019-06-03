using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(LateSimGameGroup))]
public class ClearTriggerInfoBufferSys : JobComponentSystem
{
    private EntityQuery query;

    protected override void OnCreate()
    {
        query = GetEntityQuery(typeof(HasTriggerInfoTag));
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityManager em = World.Active.EntityManager;
        query = GetEntityQuery(typeof(HasTriggerInfoTag));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; i++)
        {
            em.GetBuffer<TriggerInfoBuf>(entities[i]).Clear();
        }

        em.RemoveComponent(query, typeof(HasTriggerInfoTag));
        entities.Dispose();
        return inputDeps;
    }
}