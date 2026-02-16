using System.Collections.Generic;
using FixMath.NET;
using UnityEngine;
using Volatile;

/// <summary>
/// Stores the tick of a specific state
/// </summary>
[System.Serializable]
public class CustomSimulationSnapshot
{
    public long Tick;
    public List<BodyState> Bodies;

    public CustomSimulationSnapshot()
    {
        Bodies = new List<BodyState>();
    }

    public class BodyState
    {
        public VoltVector2 Position;
        public VoltVector2 Velocity;
        public Fix64 Angle;
        public Fix64 AngularVelocity;
        public CustomPhysicsBody BodyComponent; 
    } 
}