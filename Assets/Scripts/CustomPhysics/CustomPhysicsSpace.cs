using System.Collections.Generic;
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
    [SerializeField] private float _configWorldDamping = (float)VoltConfig.DEFAULT_DAMPING;

    private Fix64 _configWorldDampingFix64;

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
        CustomPhysicsBody[] bodies = FindObjectsByType<CustomPhysicsBody>(FindObjectsSortMode.None);
        _bodies.Clear();

        for(int i = 0; i < bodies.Length; i++)
        {
            bodies[i].Construct();
            AddBody(bodies[i]);
        }

        if(Configuration.Singleton.DebugMode){
            
            foreach (var body in _bodies)
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


    public ulong PrintBodyOrderChecksum()
    {
        ulong checksum = 1469598103934665603UL;
        int index = 0;

        foreach (var body in _bodies)
        {
            ulong id = body.Value.Body.EntityId;

            checksum ^= id + (ulong)index;
            checksum *= 1099511628211UL;

            index++;
        }

        checksum ^= (ulong)index;

        Debug.Log(
            $"Body Order Checksum: {checksum}");

        return checksum;
    }

    /// <summary>
    /// Steps the physics simulation forward, this should only be called by CustomPhysics.PhysicsTick()
    /// </summary>
    public void UpdateSimulation(Fix64 deltaTime)
    {   
        // TODO: This is temporary code to calculate a checksum style value of the inserted bodies, this will tell us if the ordering of bodies is the same between machines
        if(CustomPhysics.Tick == 0){
            var checksum = PrintBodyOrderChecksum();
            DeterminismLogger.LogExtraInfo("body order checksum" + checksum);
        }



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

        foreach(var body in _bodies)
        {
            CustomSimulationSnapshot.BodyState bodyState = new()
            {
                Velocity = body.Value.LinearVelocity,
                Angle = body.Value.Angle,
                AngularVelocity = body.Value.AngularVelocity,
                Position = body.Value.Position,
                BodyComponent = body.Value,
                //BiasRotation = body.Value.Body.BiasRotation,
                //BiasVelocity = body.Value.Body.BiasVelocity,
                //Torque = body.Value.Body.Torque,
                //Force = body.Value.Body.Force,
                CustomState = body.Value.CustomState?.Clone()
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
            //body.BodyComponent.Body.Torque = body.Torque;
            //body.BodyComponent.Body.BiasRotation = body.BiasRotation;
            //body.BodyComponent.Body.BiasVelocity = body.BiasVelocity;
            //body.BodyComponent.Body.Force = body.Force;
            body.BodyComponent.CustomState = body.CustomState?.Clone();
            body.BodyComponent.Body.Set(body.Position, body.Angle);
        }
    }

}
