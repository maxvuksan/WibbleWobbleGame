using System;
using System.Collections.Generic;
using System.Linq;
using FixMath.NET;
using Unity.VisualScripting;
using UnityEngine;
using Volatile;


/// <summary>
/// An interface for interacting with the custom physics scripts. 
/// </summary>
public class CustomPhysics : MonoBehaviour
{


    /// <summary>
    /// How many times PhysicsTick() is called per second 
    /// </summary>
    private static readonly int s_ticksPerSecond = 120;

    /// <summary>
    /// Fixed delta time value, is the difference in time between each tick
    /// </summary>
    public static readonly Fix64 TimeBetweenTicks = Fix64.One / (Fix64)s_ticksPerSecond;

    /// <summary>
    /// How many ticks in the past are snapshots recorded for. This history would be useful for performing rollbacks
    /// </summary>
    public static readonly int HistoryLength = s_ticksPerSecond * 30;

    /// <summary>
    /// Is true if the physics simulation is resimulating (catching up ticks)
    /// </summary>
    public static bool Resimulating => _resimulatingDepth > 0;
    private static int _resimulatingDepth = 0;


    /// <summary>
    /// A list of simulation states in the past, index 0 represents the most recent past tick
    /// </summary>
    private static List<CustomSimulationSnapshot> _historyRingBuffer;
    private static int _historyRingBufferPointer = 0;

    /// <summary>
    /// The current tick of the physics simulation. How many ticks have passed
    /// </summary>
    public static long Tick { get; private set; } = 0;

    private static double _timeAccumulator;

    /// <summary>
    /// All physics operations should be performed in this callback
    /// </summary>
    public static Action OnPrePhysicsTick;
    public static Action OnPhysicsTick;
    public static Action OnPostPhysicsTick;
    public static Action OnTurnOffPhysicsSimulation;
    public static Action OnRecomputeEntityIds;

    private static long? _pendingRollbackTick = null;
    private static double _simulationStartTime = -1;
        
    void Awake()
    {
        ClearSnapshotHistoryRingBuffer();
    }

    public static void ScheduleStart(double startTime)
    {   
        if(_simulationStartTime != -1)
        {
            Debug.LogWarning("Ignoring simulation ScheduleStart, the simulation has already started");
            return;
        }
        
        _simulationStartTime = startTime;
    }

    /// <summary>
    /// Is called whenever physics ticks forward
    /// </summary>
    public static void PhysicsTick()
    {
        OnPrePhysicsTick?.Invoke();
        OnPhysicsTick?.Invoke();
        //Helpers.SafeInvoke(OnPrePhysicsTick, "OnPrePhysicsTick()");
        //Helpers.SafeInvoke(OnPhysicsTick, "OnPhysicsTick()");

        CustomPhysicsSpace.Singleton.UpdateSimulation(TimeBetweenTicks);

        Helpers.SafeInvoke(OnPostPhysicsTick, "OnPostPhysicsTick()");
        
        RecordSnapshotToHistory();
        
        Tick++;
    }

    public static void ResetTickToZero()
    {
        Tick = 0;
    }

    private static void ClearSnapshotHistoryRingBuffer()
    {
        _historyRingBuffer = new List<CustomSimulationSnapshot>();

        for(int i = 0; i < HistoryLength; i++)
        {
            _historyRingBuffer.Add(new());
        }
    }

    public static void TurnOffSimulation()
    {
        ResetTickToZero();
        _simulationStartTime = -1;
        ClearSnapshotHistoryRingBuffer();
        OnTurnOffPhysicsSimulation?.Invoke();

    }

    void Update()
    {
        if (_simulationStartTime < 0)
        {
            return;
        } 

        if (Time.realtimeSinceStartupAsDouble < _simulationStartTime)
        {
            return;
        }

        if(Tick == 0)
        {
            DeterminismLogger.ClearLog();
            
            OnRecomputeEntityIds?.Invoke(); 
            CustomPhysicsSpace.Singleton.RebuildBodyDictionary();
            CustomPhysicsSpace.Singleton.SortWorld();
        }

        long targetTick = (long)((Time.realtimeSinceStartupAsDouble - _simulationStartTime) * s_ticksPerSecond);

        ActOnPendingRollback(targetTick);
        PerformNecassaryPhysicsTicks(targetTick);
    }

    private static void PerformNecassaryPhysicsTicks(long targetTick)
    {
        while(Tick < targetTick)
        {
            PhysicsTick();
            targetTick = ActOnPendingRollback(targetTick);
        }
    }

    /// <returns>The updated targetTick variable</returns>
    private static long ActOnPendingRollback(long targetTick)
    {
        if (_pendingRollbackTick.HasValue)
        {
            long rollbackTo = _pendingRollbackTick.Value;
            _pendingRollbackTick = null;

            _resimulatingDepth++;
            Rollback(rollbackTo);
            _resimulatingDepth--;

            // expand futureTick if needed to ensure we still reach original target
            return Math.Max(targetTick, Tick);
        }
        return targetTick;
    }



    /// <summary>
    /// Schedules a rollback, can be called multiple times per tick. Rollsback to the minimum of the 'previousTick' argument
    /// </summary>
    public static void RequestRollbackAndResimulate(long previousTick)
    {
        if (_pendingRollbackTick == null || previousTick < _pendingRollbackTick.Value)
        {
            _pendingRollbackTick = previousTick;
        }
    }

    /// <summary>
    /// Serializes the physics space, adding the result to the history ring buffer
    /// </summary>
    private static void RecordSnapshotToHistory()
    {
        var snapshot = CustomPhysicsSpace.Singleton.SerializeSimulationSnapshot();
        snapshot.Tick = Tick;
        
        int index = (int)(Tick % HistoryLength);
        _historyRingBuffer[index] = snapshot;

        // If we are in DebugMode, record current frame to log
        if (Configuration.Singleton.DebugMode)
        {
            List<PlayerInputDriver> playerDrivers = new();

            for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
            {
                playerDrivers.Add(PlayerDataManager.Singleton.PlayerData[i].playerInputDriver);
            }

            DeterminismLogger.LogTick(Tick, snapshot, playerDrivers);
        }
    }

    /// <summary>
    /// Returns the physics simulation to a tick in the past, this can only be done to recently elapsed ticks 
    /// </summary>
    /// <param name="previousTick">The tick we wish to rollback to</param>
    private static void Rollback(long previousTick)
    {
        if (previousTick <= 0)
        {
            Debug.LogWarning("Cannot rollback to tick 0 or below, ignoring");
            return;
        }

        long tickDifference = Tick - previousTick;

        if(tickDifference > HistoryLength)
        {
            Debug.LogError("Cannot rollback to the provided tick because the history only stores recent ticks, this tick is too far in the future");
            return;
        }
        
        long snapshotTick = previousTick - 1; // restore state BEFORE previousTick ran;
        int shiftedIndex = (int)(snapshotTick % HistoryLength); 

        if(_historyRingBuffer[shiftedIndex].Tick != snapshotTick)
        {
            Debug.LogError("The tick in the history buffer does not match the desired tick to rollback to");
            return;
        }

        CustomPhysicsSpace.Singleton.RestoreSimulationSnapshot(_historyRingBuffer[shiftedIndex]);

        Tick = previousTick;
    }
    
    /// <summary>
    /// Simulates the 
    /// </summary>
    /// <param name="futureTick">The tick we wish to simulate up to</param>
    public static void SimulateFuture(long futureTick)
    {
        // TODO: Prevent death spiral, enforce a max resimulatingDepth
        _resimulatingDepth++;
        
        PerformNecassaryPhysicsTicks(futureTick);

        _resimulatingDepth--;
    }


    /// <summary>
    /// Shoots a ray in the CustomPhysics simulation space
    /// </summary>
    public static CustomPhysicsRayResult Raycast(VoltVector2 origin, VoltVector2 direction, Fix64 distance)
    {
        VoltRayCast ray = new VoltRayCast(origin, direction, distance);
        VoltRayResult result = new();

        CustomPhysicsRayResult customResult = new() { Hit = false};
        customResult.Hit = CustomPhysicsSpace.Singleton.SimulationSpace.RayCast(ref ray, ref result, null);
        customResult.Origin = origin;
        customResult.Direction = direction;

        if (!customResult.Hit)
        {
            customResult.Destination = ray.origin + ray.direction * ray.distance;
            return customResult;
        }

        customResult.Destination = result.ComputePoint(ref ray);
        customResult.Normal = result.normal;
        
        customResult.Body = CustomPhysicsSpace.Singleton.GetBody(result.Body.EntityId);
    
        return customResult;
    }

}
