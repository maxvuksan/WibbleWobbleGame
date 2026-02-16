using System.Collections.Generic;
using FixMath.NET;
using UnityEngine;
using Volatile;

/// <summary>
/// A class to drive the Volatile physics simulation, this bridge the gap between Volatile and unity game objects
/// </summary>
public class CustomPhysicsSpace : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float _configWorldDamping = (float)VoltConfig.DEFAULT_DAMPING;

    private Fix64 _configWorldDampingFix64;

    private List<CustomPhysicsBody> _bodies;


    public VoltWorld SimulationSpace
    {
        get => _simulationSpace;
    }
    
    private VoltWorld _simulationSpace;

    public static CustomPhysicsSpace Singleton;


    private void Awake() 
    {
        if(Singleton != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Singleton = this;

        _bodies = new List<CustomPhysicsBody>();

        InitFix64Constants();
        InitWorld();

    }



    /// <summary>
    /// Converts configuration variables to Fix64 variants
    /// </summary>
    private void InitFix64Constants()
    {
        _configWorldDampingFix64 = (Fix64)_configWorldDamping;
    }

    /// <summary>
    /// Initalize the physics simulation space
    /// </summary>
    private void InitWorld() 
    {
        _simulationSpace = new VoltWorld(_configWorldDampingFix64);
    }

    public void AddBody(CustomPhysicsBody body)
    {
        _bodies.Add(body);
        print("body added");
    }
    public void RemoveBody(CustomPhysicsBody body)
    {
        _bodies.Remove(body);
    }

    /// <summary>
    /// Steps the physics simulation forward, this should only be called by CustomPhysics.PhysicsTick()
    /// </summary>
    public void UpdateSimulation(Fix64 deltaTime)
    {
        _simulationSpace.DeltaTime = deltaTime;
        _simulationSpace.Update();

        foreach(CustomPhysicsBody body in _bodies)
        {
            if(body.BodyType != CustomBodyType.Static)
            {
                body.ApplySimulationToGameObject();
            }
        }
    }

    /// <summary>
    /// Returns a snapshot of the physics simulation at the current tick
    /// </summary>
    public CustomSimulationSnapshot SerializeSimulationSnapshot()
    {
        CustomSimulationSnapshot snapshot = new();

        foreach(CustomPhysicsBody body in _bodies)
        {

            CustomSimulationSnapshot.BodyState bodyState = new()
            {
                Velocity = body.LinearVelocity,
                Angle = body.Angle,
                AngularVelocity = body.AngularVelocity,
                Position = body.Position,
                BodyComponent = body,
            };

            snapshot.Bodies.Add(bodyState);
        }

        return snapshot;
    }

    public void RestoreSimulationSnapshot(CustomSimulationSnapshot snapshot)
    {
        // TODO: Currently snapshots are not taking into account if bodies are added/removed, this will likley cause issues
        foreach(CustomSimulationSnapshot.BodyState body in snapshot.Bodies)
        {
            if(body == null)
            {
                Debug.LogWarning("A body is being ignored because its component is null, was the object deleted? RestoreSimulationSnapshot()");
                continue;
            }

            body.BodyComponent.Body.LinearVelocity = body.Velocity;
            body.BodyComponent.Body.AngularVelocity = body.AngularVelocity;
            body.BodyComponent.Body.Set(body.Position, body.Angle);
        }
    }

}
