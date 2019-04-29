using System;
using Unity.Entities;
using Unity.Physics;

[Serializable]
public struct RayCastReqEvent : IComponentData
{
    public Entity Reguester;
    public short ReqId;
    public RaycastInput RaycastInput;

    public RayCastReqEvent(Entity reguester, short reqId, RaycastInput raycastInput)
    {
        Reguester = reguester;
        ReqId = reqId;
        RaycastInput = raycastInput;
    }
}