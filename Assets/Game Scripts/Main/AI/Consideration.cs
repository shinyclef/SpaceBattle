using Unity.Mathematics;

public struct Consideration
{
    public FactType FactType;
    public GraphType GraphType;
    public float Slope;
    public float Exp;
    public float XShift;
    public float YShift;

    public float Evaluate(float input)
    {
        switch (GraphType)
        {
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