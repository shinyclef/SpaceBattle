using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(GameGroupLateSim))]
public class ClearTriggerInfoBufferSys : ComponentSystem
{
    private EntityQuery triggerInfoTagQuery;

    protected override void OnCreate()
    {
        triggerInfoTagQuery = GetEntityQuery(typeof(HasTriggerInfoTag));
    }

    protected override void OnUpdate()
    {
        EntityManager em = World.Active.EntityManager;
        em.RemoveComponent(triggerInfoTagQuery, typeof(HasTriggerInfoTag));
        NativeArray<Entity> entities = triggerInfoTagQuery.ToEntityArray(Allocator.TempJob);
        foreach (Entity e in entities)
        {
            DynamicBuffer<TriggerInfoBuf> buffer = em.GetBuffer<TriggerInfoBuf>(e);
            buffer.Clear();
        }

        entities.Dispose();
    }
}