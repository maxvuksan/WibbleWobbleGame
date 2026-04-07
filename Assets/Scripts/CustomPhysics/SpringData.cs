

using FixMath.NET;
using Volatile;
using static CustomMaths;

[System.Serializable]
public struct SpringData
{
    public IntHundredth restLength;
    public IntHundredth springConstant;
    public IntHundredth dampingConstant;
}

public struct SpringForceResult
{
    public VoltVector2 linearForce;  // Force to apply to linear velocity
    public Fix64 torque1;             // Torque to apply to body 1
    public Fix64 torque2;             // Torque to apply to body 2
}

public static class Spring
{
    /// <summary>
    /// Computes the force from point1 to point2 given a spring between said points
    /// </summary>
    /// <returns>The force of the spring</returns>
    public static VoltVector2 CalculateForce(
        VoltVector2 p1,
        VoltVector2 p2,
        VoltVector2 v1,
        VoltVector2 v2,
        SpringData springData)
    {
        // Cast to Fix64
        Fix64 restLength = springData.restLength;
        Fix64 springConstant = springData.springConstant;
        Fix64 dampingConstant = springData.dampingConstant;

        VoltVector2 delta = p2 - p1;
        Fix64 distance = delta.magnitude;
        
        Fix64 epsilon = (Fix64)1 / (Fix64)10000; // Small threshold
        if (distance < epsilon)
        {
            return VoltVector2.zero;
        }
        
        VoltVector2 direction = new VoltVector2(delta.x / distance, delta.y / distance);
        Fix64 displacement = distance - restLength;
        
        // Spring force (Hooke's Law: F = k * x)
        Fix64 springForce = springConstant * displacement;
        
        // Damping force (F = -c * v)
        VoltVector2 relativeVelocity = v2 - v1;
        Fix64 velocityAlongSpring = VoltVector2.Dot(relativeVelocity, direction);
        Fix64 dampingForce = dampingConstant * velocityAlongSpring;
        
        // Total force magnitude
        Fix64 totalForceMagnitude = springForce + dampingForce;
        
        return direction * totalForceMagnitude;
    }

    /// <summary>
    /// Simplified version when p2 is a static anchor point (velocity = zero)
    /// </summary>
    public static VoltVector2 CalculateForce(
        VoltVector2 p1,
        VoltVector2 p2,
        VoltVector2 v1,
        SpringData springData)
    {
        return CalculateForce(p1, p2, v1, VoltVector2.zero, springData);
    }
    
}