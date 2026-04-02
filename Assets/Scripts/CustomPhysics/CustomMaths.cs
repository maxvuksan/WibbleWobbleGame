using FixMath.NET;
using UnityEngine;
using Volatile;

public static class CustomMaths
{

    public static Fix64 Clamp(Fix64 value, Fix64 min, Fix64 max)
    {
        if(value > max)
        {
            value = max;
        }
        if(value < min)
        {
            value = min;
        }
        return value;
    }

    /// <summary>
    /// 2D cross product: returns scalar z-component of (a × b)
    /// </summary>
    public static Fix64 CrossProduct(VoltVector2 a, VoltVector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    /// <summary>
    /// Rotates a point by the given angle (in radians)
    /// </summary>
    public static VoltVector2 RotatePoint(VoltVector2 point, Fix64 angle)
    {
        Fix64 cos = Fix64.Cos(angle);
        Fix64 sin = Fix64.Sin(angle);
        
        return new VoltVector2(
            point.x * cos - point.y * sin,
            point.x * sin + point.y * cos
        );
    }
    
    /// <summary>
    /// Calculates the velocity contribution from rotation at a local point
    /// v = ω × r = ω * perpendicular(r)
    /// </summary>
    public static VoltVector2 PerpendicularVelocity(VoltVector2 localPoint, Fix64 rotation, Fix64 angularVel)
    {
        // Rotate the local point to world orientation
        VoltVector2 rotatedPoint = RotatePoint(localPoint, rotation);
        
        // 2D cross product: ω × r = ω * (-r.y, r.x)
        return new VoltVector2(
            -rotatedPoint.y * angularVel,
            rotatedPoint.x * angularVel
        );
    }

    
}