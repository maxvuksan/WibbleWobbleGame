using System.Collections.Generic;
using System.Linq;
using FixMath.NET;
using UnityEngine;
using Volatile;


/// <summary>
/// Extend the UserData property so we can get it as a specific type (e.g. our CustomPhysicsUserData struct)
/// </summary>
public static class VoltExtensions
{
    public static T GetUserData<T>(this VoltBody body)
        where T : class
    {
        return body.UserData as T;
    }
}

/// <summary>
/// A class to drive the Volatile physics simulation, this bridge the gap between Volatile and unity game objects
/// </summary>
public class CustomPhysicsSpace : MonoBehaviour
{
    [Header("Configuration")]
    public bool VisualInterpolation = true;
    private Fix64 _configWorldDampingFix64 = VoltConfig.DEFAULT_DAMPING;

    public Dictionary<ulong, CustomPhysicsBody> Bodies => _bodies;
    private Dictionary<ulong, CustomPhysicsBody> _bodies;


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

        _bodies = new Dictionary<ulong, CustomPhysicsBody>();

        InitWorld();

    }


    /// <summary>
    /// Initalize the physics simulation space
    /// </summary>
    private void InitWorld() 
    {
        _simulationSpace = new VoltWorld(_configWorldDampingFix64);
    }

    /// <summary>
    /// Sorts all the bodies by their assigned EntityIds
    /// </summary>
    public void SortWorld()
    {
        _simulationSpace.SortBodies();
    }

    public void RebuildBodyDictionary()
    {
        // assign body EntityId to new values
        CustomPhysicsBody[] newBodies = FindObjectsByType<CustomPhysicsBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        _bodies.Clear();

        for(int i = 0; i < newBodies.Length; i++)
        {
            newBodies[i].Construct();
            AddBody(newBodies[i]);
        }

        if(Configuration.Singleton.DebugMode){
            
            // Convert dictionary to sorted list for deterministic logging
            var sortedBodies = _bodies.OrderBy(kvp => kvp.Key).ToList();
            
            foreach (var body in sortedBodies)
            {
                string bodyInDict = "Body Dictionary Entry: " + body.Value.name + ", EntityId: " + body.Key + ", SimulationEntityId: " + body.Value.Body.EntityId;
                DeterminismLogger.LogExtraInfo(bodyInDict);
                Debug.Log(bodyInDict);
            }
        }
         
    }

    public CustomPhysicsBody GetBody(ulong entityId)
    {
        return _bodies[entityId];
    }

    public void AddBody(CustomPhysicsBody bodyComponent)
    {
        if (_bodies.ContainsKey(bodyComponent.Body.EntityId))
        {
            Debug.LogWarning("CustomPhysicsBody EntityId conflicts with an exisiting id in the dictionary, not adding to dictionary");
            return;
        }
        _bodies.Add(bodyComponent.Body.EntityId, bodyComponent);
    }
    
    public void RemoveBody(CustomPhysicsBody bodyComponent)
    {
        _bodies.Remove(bodyComponent.Body.EntityId);
    }

    /// <summary>
    /// Steps the physics simulation forward, this should only be called by CustomPhysics.PhysicsTick()
    /// </summary>
    public void UpdateSimulation(Fix64 deltaTime)
    {   
        _simulationSpace.DeltaTime = deltaTime;
        _simulationSpace.Update();

        foreach(var body in _bodies)
        {
            if(body.Value.BodyType != CustomBodyType.Static)
            {
                body.Value.ApplySimulationToGameObject();
            }
        }
    }

    /// <summary>
    /// Returns a snapshot of the physics simulation at the current tick
    /// </summary>
    public CustomSimulationSnapshot SerializeSimulationSnapshot()
    {
        CustomSimulationSnapshot snapshot = new();

        // TODO: I dont think this sorting is necassary, but i have included this just incase this would cause problems. 
        // Once we find the determinsm issue, test without this
        // ...
        // Sort by EntityId for deterministic order
        var sortedBodies = _bodies.OrderBy(kvp => kvp.Key);

        foreach(var body in sortedBodies)
        {
            VoltBody voltBody = body.Value.Body;

            CustomSimulationSnapshot.BodyState bodyState = new()
            {
                Velocity = voltBody.LinearVelocity,
                Angle = voltBody.Angle,
                AngularVelocity = voltBody.AngularVelocity,
                Position = voltBody.Position,
                BiasRotation = voltBody.BiasRotation,
                BiasVelocity = voltBody.BiasVelocity,

                BodyComponent = body.Value,
                CustomState = body.Value.CustomState?.Clone()
            };

            snapshot.Bodies.Add(bodyState);
        }

        return snapshot;
    }

    public void RestoreSimulationSnapshot(CustomSimulationSnapshot snapshot)
    {
        if (Configuration.Singleton.DebugMode)
        {
            DeterminismLogger.LogExtraInfo("RestoreSimulationSnapshot() called, restoring the state before tick " + snapshot.Tick + " ran\n");
        }

        // TODO: Currently snapshots are not taking into account if bodies are added/removed, this will likley cause issues
        foreach(CustomSimulationSnapshot.BodyState body in snapshot.Bodies)
        {

            if(body == null)
            {
                Debug.LogWarning("A body is being ignored because its component is null, was the object deleted? RestoreSimulationSnapshot()");
                continue;
            }

            VoltBody voltBody = body.BodyComponent.Body;

            voltBody.PartialReset();

            voltBody.BiasRotation = body.BiasRotation;
            voltBody.BiasVelocity = body.BiasVelocity;
            voltBody.LinearVelocity = body.Velocity;
            voltBody.AngularVelocity = body.AngularVelocity;
            voltBody.Set(body.Position, body.Angle);

            body.BodyComponent.CustomState = body.CustomState?.Clone();
        }

        // MAY NOT BE NEEDED, Test removing it after dsync issue is identified
        _simulationSpace.SortBodies();

        _simulationSpace.FreeManifolds();
    }

}
