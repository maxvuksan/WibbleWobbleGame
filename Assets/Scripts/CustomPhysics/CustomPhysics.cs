using System;
using System.Collections.Generic;
using System.Linq;
using FixMath.NET;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
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
    /// The real time the last physics tick occured
    /// </summary>
    public static double LastPhysicsTickTime { get; private set; }

    /// <summary>
    /// Fixed delta time value, is the difference in time between each tick
    /// </summary>
    public static readonly Fix64 TimeBetweenTicks = Fix64.One / (Fix64)s_ticksPerSecond;

    /// <summary>
    /// How many ticks in the past are snapshots recorded for. This history would be useful for performing rollbacks
    /// </summary>
    public static readonly int HistoryLength = s_ticksPerSecond * 150;

    /// <summary>
    /// Is true if the physics simulation is resimulating (catching up ticks)
    /// </summary>
    public static bool Resimulating => _resimulatingDepth > 0;

    /// <summary>
    /// After a rollback simulates the future at the same rate the game ticks at normally, this parameter is used for testing the correctness of rollbacks and resimulation
    /// </summary>
    public static bool SimulateFutureAtRegularTickRate = false;
    public static long SimuluateFutureAtRegularTickRateStartTick = long.MaxValue; 
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
    public static Action OnStartPhysicsSimulation;
    public static Action OnRecomputeEntityIds;
    public static Action OnPostRecomputeEntityIds;

    private static long? _pendingRollbackTick = null;
    private static double _simulationStartTime = -1;
    private static bool _recomputeEntityIdsRequired = false;
        
    void Awake()
    {
        CustomConstraintSolver.Initialize();
        ClearSnapshotHistoryRingBuffer();
    }
    void OnDestroy()
    {
        CustomConstraintSolver.Cleanup();
    }

    public static void BeginRollbackDebug()
    {
        SimuluateFutureAtRegularTickRateStartTick = Tick;
        SimulateFutureAtRegularTickRate = true;       
        DeterminismLogger.ClearLog();
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

    public static void RecomputeEntityIds()
    {
        _recomputeEntityIdsRequired = true;
    }

    /// <summary>
    /// Is called whenever physics ticks forward
    /// </summary>
    public static void PhysicsTick()
    {
        LastPhysicsTickTime = Time.realtimeSinceStartupAsDouble;

        RecordSnapshotToHistory();

        Helpers.SafeInvoke(OnPrePhysicsTick, "OnPrePhysicsTick");
        Helpers.SafeInvoke(OnPhysicsTick, "OnPhysicsTick");
        
        CustomConstraintSolver.SolveAllConstraints();
        CustomPhysicsSpace.Singleton.UpdateSimulation(TimeBetweenTicks);

        Helpers.SafeInvoke(OnPostPhysicsTick, "OnPostPhysicsTick");

        LogTick();
        Tick++;

    }

    private static void LogTick(string title="")
    {
        if (Configuration.Singleton.DebugMode)
        {
            List<PlayerInputDriver> playerDrivers = new();

            if(PlayerDataManager.Singleton != null){
                for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
                {
                    playerDrivers.Add(PlayerDataManager.Singleton.PlayerData[i].playerInputDriver);
                }
            }

            int index = (int)(Tick % HistoryLength);
            DeterminismLogger.LogTick(Tick, _historyRingBuffer[index], playerDrivers, title);
        }
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
        // the simulation is already off
        if(_simulationStartTime == -1)
        {
            return;
        }

        OnTurnOffPhysicsSimulation?.Invoke();
        ResetTickToZero();
        _simulationStartTime = -1;
        ClearSnapshotHistoryRingBuffer();
        SimulateFutureAtRegularTickRate = false;
        SimuluateFutureAtRegularTickRateStartTick = long.MaxValue;
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
            OnStartPhysicsSimulation?.Invoke();
        }

        if (_recomputeEntityIdsRequired)
        {
            DeterminismLogger.ClearLog();
            OnRecomputeEntityIds?.Invoke(); 

            CustomPhysicsSpace.Singleton.RebuildBodyDictionary();
            CustomPhysicsSpace.Singleton.SortWorld();
            
            OnPostRecomputeEntityIds?.Invoke();

            _recomputeEntityIdsRequired = false;
        }

        long targetTick = (long)((Time.realtimeSinceStartupAsDouble - _simulationStartTime) * s_ticksPerSecond);

        PerformNecassaryPhysicsTicks(targetTick);
    }

    private static void PerformNecassaryPhysicsTicks(long targetTick)
    {
        while(Tick < targetTick)
        {
            PhysicsTick();
            var currTick = ActOnPendingRollback(targetTick);

            if (SimulateFutureAtRegularTickRate)
            {
                return;
            }
            else
            {
                targetTick = currTick;
            }
        }
    }

    /// <returns>The updated targetTick variable</returns>
    private static long ActOnPendingRollback(long targetTick)
    {
        if (_pendingRollbackTick.HasValue)
        {
            long rollbackTo = _pendingRollbackTick.Value;
            _pendingRollbackTick = null;

            Rollback(rollbackTo);

            if (SimulateFutureAtRegularTickRate)
            {
                return rollbackTo;     
            }
            else
            {
                // expand futureTick if needed to ensure we still reach original target
                SimulateFuture(targetTick);           
            }
            return targetTick;
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
    }

    /// <summary>
    /// Returns the physics simulation to a tick in the past, this can only be done to recently elapsed ticks 
    /// </summary>
    /// <param name="previousTick">The tick we wish to rollback to</param>
    private static void Rollback(long previousTick)
    {
        Debug.Log("Rollback occured");

        if (previousTick <= 0)
        {
            Debug.LogWarning("Cannot rollback to tick less than 0, ignoring");
            return;
        }

        long tickDifference = Tick - previousTick;

        if(tickDifference > HistoryLength)
        {
            Debug.LogError("Cannot rollback to the provided tick because the history only stores recent ticks, this tick is too far in the future");
            return;
        }
        
        long snapshotTick = previousTick; // was previousTick - 1; // restore state BEFORE previousTick ran;
        int shiftedIndex = (int)(snapshotTick % HistoryLength); 

        if(_historyRingBuffer[shiftedIndex].Tick != snapshotTick)
        {
            Debug.LogError("The tick in the history buffer does not match the desired tick to rollback to");
            return;
        }

        if (Configuration.Singleton.DebugMode)
        {
            DeterminismLogger.LogExtraInfo("Performing Rollback, CurrentTick: " + Tick + " RollbackSnapshotTick: " + snapshotTick);
        }

        CustomPhysicsSpace.Singleton.RestoreSimulationSnapshot(_historyRingBuffer[shiftedIndex]);
        
        Tick = previousTick; // restore simulate -1 so the next tick we process previousTick
    }
    
    /// <summary>
    /// Simulates the future to return to where we were up to
    /// </summary>
    /// <param name="futureTick">The tick we wish to simulate up to</param>
    public static void SimulateFuture(long futureTick)
    {
        if (Configuration.Singleton.DebugMode)
        {
            DeterminismLogger.LogExtraInfo("Simulating to future tick: " + futureTick);
        }

        // TODO: Prevent death spiral, enforce a max resimulatingDepth
        _resimulatingDepth++;
        
        PerformNecassaryPhysicsTicks(futureTick);

        _resimulatingDepth--;
    }


    /// <summary>
    /// Queries and returns the bodies which overlap a defined circle in the simulation space
    /// </summary>
    /// <param name="origin">The centre of the circle</param>
    /// <param name="radius">The radius of the circle</param>
    /// <returns>The result of the operation</returns>
    public static CustomPhyiscsOverlapResult OverlapCircle(VoltVector2 origin, Fix64 radius)
    {
        CustomPhyiscsOverlapResult customResult = new() {Hit = false};
        var outputVoltBuffer = CustomPhysicsSpace.Singleton.SimulationSpace.QueryCircle(origin, radius, (body) => true);

        if(outputVoltBuffer.Count != 0)
        {
            customResult.Hit = true;
        }

        customResult.Bodies = new List<CustomPhysicsBody>();

        foreach(var body in outputVoltBuffer)
        {
            var bodyComponent = CustomPhysicsSpace.Singleton.GetBody(body.EntityId);
            customResult.Bodies.Add(bodyComponent);
        }

        return customResult;
    }

    /// <summary>
    /// Shoots a ray in the simulation space
    /// </summary>
    public static CustomPhysicsRayResult Raycast(VoltVector2 origin, VoltVector2 direction, Fix64 distance)
    {
        VoltRayCast ray = new VoltRayCast(origin, direction, distance);
        VoltRayResult result = new();

        CustomPhysicsRayResult customResult = new();
        customResult.Hit = CustomPhysicsSpace.Singleton.SimulationSpace.RayCast(ref ray, ref result, (body) => true);
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
