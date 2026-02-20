using System;
using System.Collections.Generic;
using System.Linq;
using FixMath.NET;
using UnityEngine;

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
    public static bool Resimulating { get; private set; } = false;

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

    /// <summary>
    /// Determines if the simulation is allowed to proceed to the next tick
    /// </summary>
    public static bool IsAllowedToTick = true;


    
    void Awake()
    {
        _historyRingBuffer = new List<CustomSimulationSnapshot>();

        for(int i = 0; i < HistoryLength; i++)
        {
            _historyRingBuffer.Add(new());
        }
    }

    /// <summary>
    /// Is called whenever physics ticks forward
    /// </summary>
    public static void PhysicsTick()
    {
        Helpers.SafeInvoke(OnPrePhysicsTick, "OnPrePhysicsTick()");
        Helpers.SafeInvoke(OnPhysicsTick, "OnPhysicsTick()");

        CustomPhysicsSpace.Singleton.UpdateSimulation(TimeBetweenTicks);

        Helpers.SafeInvoke(OnPostPhysicsTick, "OnPostPhysicsTick()");
        
        RecordSnapshotToHistory();

        Tick++;
    }
    
    void Update()
    {
        _timeAccumulator += Time.unscaledDeltaTime;

        if(_timeAccumulator >= (double)TimeBetweenTicks && IsAllowedToTick)
        {
            PhysicsTick();
            _timeAccumulator -= (double)TimeBetweenTicks;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Rollback(Tick - HistoryLength + 1);
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
    public static void Rollback(long previousTick)
    {
        long tickDifference = Tick - previousTick;

        if(tickDifference > HistoryLength)
        {
            Debug.LogError("Cannot rollback to the provided tick because the history only stores recent ticks, this tick is too far in the future");
            return;
        }
        
        int shiftedIndex = (int)(tickDifference % HistoryLength); 

        if(_historyRingBuffer[shiftedIndex].Tick != previousTick)
        {
            Debug.LogError("The tick in the history buffer does not match the desired tick to rollback to");
            return;
        }

        CustomPhysicsSpace.Singleton.RestoreSimulationSnapshot(_historyRingBuffer[shiftedIndex]);

        Tick = shiftedIndex;
    }
    
    /// <summary>
    /// Simulates the 
    /// </summary>
    /// <param name="futureTick">The tick we wish to simulate up to</param>
    public static void SimulateFuture(long futureTick)
    {
        // TODO: Prevent death spiral
        
        Resimulating = true;
        
        // add i to prevent potential infinite loop
        while(Tick <= futureTick)
        {
            PhysicsTick();
        }

        Resimulating = false;

    }

}
