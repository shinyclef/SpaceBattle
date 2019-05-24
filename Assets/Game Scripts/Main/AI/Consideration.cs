using Unity.Entities;
using Unity.Mathematics;

public struct Consideration : IComponentData
{
    public FactType FactType;
    public GraphType GraphType;
    public float Slope;
    public float Exp;
    public half XShift;
    public half YShift;
    public float InputMin;
    public float InputMax;

    public float GetNormalizedInput(float input)
    {
        return math.clamp((input - InputMin) / (InputMax - InputMin), 0f, 1f);
    }

    public float Evaluate(float input)
    {
        switch (GraphType)
        {
            case GraphType.Constant:
                return math.clamp(YShift, 0f, 1f);
            case GraphType.Linear:
                return math.clamp(Slope * input + YShift, 0f, 1f);
            case GraphType.Exponential:
                return math.clamp(Slope * math.pow(input - XShift, Exp) + YShift, 0f, 1f);
            case GraphType.Sigmoid:
                float divisor = (1 + math.pow(2.718f, XShift + Slope * input));
                return math.clamp(divisor == 0f ? YShift : Exp / divisor + YShift, 0f, 1f);
            default:
                return 0;
        }
    }
}