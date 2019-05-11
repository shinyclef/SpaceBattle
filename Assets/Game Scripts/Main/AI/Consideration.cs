using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Consideration : IComponentData
{
    public FactType FactType;
    public GraphType GraphType;
    public float Slope;
    public float Exp;
    public half XShift;
    public half YShift;

    public float Evaluate(float input)
    {
        switch (GraphType)
        {
            case GraphType.Constant:
                return YShift;
            case GraphType.Linear:
                return Slope * input + YShift;
            case GraphType.Exponential:
                return Slope * math.pow(input - XShift, Exp) + YShift;
            case GraphType.Sigmoid:
                float divisor = (1 + math.pow(2.718f, XShift + Slope * input));
                return divisor == 0f ? YShift : Exp / divisor + YShift;
            default:
                return 0;
        }
    }
}

[InternalBufferCapacity(5)]
public struct ConsiderationBuf : IBufferElementData
{
    public static implicit operator Consideration(ConsiderationBuf e) { return e.Value; }
    public static implicit operator ConsiderationBuf(Consideration e) { return new ConsiderationBuf { Value = e }; }

    public Consideration Value;
}

public class ConsiderationBufComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<ConsiderationBuf>(entity);
    }
}