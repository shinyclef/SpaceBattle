public enum GraphType
{
    Linear,         // utility = Slope * input + YShift
    Exponential,    // utility = Slope * pow((input - XShift), E) + YShift
    Sigmoid         // utility = (E / (1 + pow(2.718, XShift + Slope * input))) + YShift 
}