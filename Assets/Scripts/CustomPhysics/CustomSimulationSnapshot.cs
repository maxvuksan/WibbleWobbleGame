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
        //public VoltVector2 Force;
        //public Fix64 Torque;
        //public VoltVector2 BiasVelocity;
        //public Fix64 BiasRotation;
        public CustomPhysicsBody BodyComponent; 
        public ICustomTickState CustomState;
    } 
}

/// <summary>
/// Implement this interface for state that needs to be account for when rolling back and resimulating
/// </summary>
public interface ICustomTickState
{
    ICustomTickState Clone();
}

public interface ICustomTickState<T> : ICustomTickState where T : ICustomTickState<T>
{
    new T Clone();

    // Route the non-generic call to the typed one automatically
    ICustomTickState ICustomTickState.Clone() => Clone();
}