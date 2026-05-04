using FixMath.NET;
using UnityEngine;
using Volatile;


/// <summary>
/// 2D IntHundredth, containg an X and Y parameter
/// </summary>
[System.Serializable]
public struct IntHundredthVector2
{
    public IntHundredth X;
    public IntHundredth Y;

    public static IntHundredthVector2 CreateFromHundrethValues(int xHundreths, int yHundreths)
    {
        IntHundredthVector2 vec2 = new();

        vec2.X = new IntHundredth(){ValueHundredths = xHundreths};
        vec2.Y = new IntHundredth(){ValueHundredths = yHundreths};

        return vec2;
    }

    public IntHundredthVector2(IntHundredth _x, IntHundredth _y)
    {
        X = _x;
        Y = _y;
    }

    public IntHundredthVector2(Fix64 _x, Fix64 _y)
    {
        X = new IntHundredth(_x);
        Y = new IntHundredth(_y);
    }
    /// <summary>
    /// Converts and casts the internal value to a VoltVector2 type, this removes the hundredth scaler
    /// </summary>
    public VoltVector2 AsVoltVector2()
    {
        return new VoltVector2((Fix64)X, (Fix64)Y);
    }

    /// <summary>
    /// Converts and casts the internal value to a Vector2 type, this removes the hundredth scaler
    /// </summary>
    public Vector2 AsVector2()
    {
        return new Vector2(X.AsFloat(), Y.AsFloat());
    }

    /// <summary>
    /// Equivilant to calling .AsVoltVector2()
    /// </summary>
    public static implicit operator VoltVector2(IntHundredthVector2 h) => h.AsVoltVector2();
}


/// <summary>
/// Primitive structure to store ints with decimal precision similar to floats, this is done by treating the value x100 larger than it is.
/// This can then be converted to our Fix64 value when necassary 
/// </summary>
[System.Serializable]
public struct IntHundredth
{
    [HideInInspector] public int ValueHundredths;

    /// <summary>
    /// Constructs the IntHundredth type from an unscaled integer value. This constructor scales and stores the provided value by 100
    /// </summary>
    public IntHundredth(int wholeValue)
    {
        ValueHundredths = wholeValue * 100;
    }

    public IntHundredth(Fix64 trueValue)
    {
        ValueHundredths = (int)(trueValue * (Fix64)100);
    }

    /// <summary>
    /// Converts and casts the internal value to a Fix64 type, this removes the hundredth scaler
    /// </summary>
    public Fix64 AsFix64()
    {
        return (Fix64)ValueHundredths / (Fix64)100;
    }

    /// <summary>
    /// Converts and casts the internal value to a float type, this removes the hundredth scaler,
    /// Note: This operation performs floating point arithmitic, we should only use AsFloat() for visualizations
    /// </summary>
    public float AsFloat()
    {
        return ValueHundredths / 100.0f;
    }
    
    public static IntHundredth operator +(IntHundredth a, IntHundredth b)
    {
        IntHundredth result;
        result.ValueHundredths = a.ValueHundredths + b.ValueHundredths;
        return result;
    }

    public static IntHundredth operator -(IntHundredth a, IntHundredth b)
    {
        IntHundredth result;
        result.ValueHundredths = a.ValueHundredths - b.ValueHundredths;
        return result;
    }

    /// <summary>
    /// Equivilant to calling .AsFix64()
    /// </summary>
    public static implicit operator Fix64(IntHundredth h) => h.AsFix64();

    /// <summary>
    /// Allows the structure to be initalized with an unscaled integer value 
    /// </summary>
    public static implicit operator IntHundredth(int wholeValue) => new IntHundredth(wholeValue);
}



