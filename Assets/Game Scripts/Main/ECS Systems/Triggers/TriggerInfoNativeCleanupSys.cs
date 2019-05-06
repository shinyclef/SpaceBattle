using Unity.Entities;

[UpdateInGroup(typeof(TriggerGameGroup))]
[UpdateAfter(typeof(TriggerInfoApplySys))]
public class TriggerInfoNativeCleanupSys : ComponentSystem
{
    private TriggerInfoPrepareSys triggerPrepSys;

    protected override void OnCreate()
    {
        triggerPrepSys = World.GetOrCreateSystem<TriggerInfoPrepareSys>();
    }

    protected override void OnUpdate()
    {
        triggerPrepSys.TriggerMap.Dispose();
        triggerPrepSys.TriggerKeys.Dispose();
    }
}