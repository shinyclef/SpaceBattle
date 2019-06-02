using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(CombatTargetSys))]
public class DebugRaysSys : ComponentSystem
{
    public static DebugRaysSys I;

    protected override void OnCreate()
    {
        I = this;
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((ref LocalToWorld l2w, ref MoveDestination moveDestination) =>
        {
            Debug.DrawLine(l2w.Position, new float3(moveDestination.Value, l2w.Position.z), Color.green);
            Debug.DrawLine(l2w.Position, new float3(l2w.Position.xy + l2w.Up.xy, l2w.Position.z), Color.white);
        });
    }
}