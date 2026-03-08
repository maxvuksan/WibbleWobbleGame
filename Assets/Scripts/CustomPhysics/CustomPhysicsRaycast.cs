using UnityEngine;
using Volatile;


/// <summary>
/// A struct representing the result of a raycast
/// </summary>
[System.Serializable]
public struct CustomPhysicsRayResult
{
    public bool Hit;
    public CustomPhysicsBody? Body;
    public VoltVector2 Normal;
    public VoltVector2 Origin;
    public VoltVector2 Direction;

    public VoltVector2 HitPoint // more relevant variable name for rays which hit (returns Destination)
    {
        get => Destination;
    }

    public VoltVector2 Destination; 
}
