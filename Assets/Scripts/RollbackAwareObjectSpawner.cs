using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides a way to spawn objects at specific ticks, ensuring when rollback occurs those objects are reverted
/// This should NOT be used for physics objects, all bodies should be configured before the simulation starts
/// </summary>
public class RollbackAwareObjectSpawner : MonoBehaviour
{
    private static Dictionary<long, List<GameObject>> _spawnedHistory = new Dictionary<long, List<GameObject>>();
    private static Dictionary<GameObject, long> _objectToTickMap = new Dictionary<GameObject, long>();


    private static void RecordObjectCreationAtCurrentTick(GameObject newObject)
    {
        // TODO: this should store the object at CustomPhysics.Tick, ensuring it gets removed if we rollback past said tick

        long currentTick = CustomPhysics.Tick;

        // if tick exists
        if (!_spawnedHistory.ContainsKey(currentTick))
        {
            _spawnedHistory[currentTick] = new List<GameObject>();
        }

        _spawnedHistory[currentTick].Add(newObject);
        _objectToTickMap[newObject] = currentTick;
    }

    /// <summary>
    /// Removes objects that exceed the provided physics tick
    /// </summary>
    /// <param name="tick">The physics tick to return to, all objects ahead of this tick will be removed</param>
    public static void Rollback(long rollbackTick)
    {
        foreach(var entry in _spawnedHistory)
        {
            // remove any future ticks
            if(entry.Key >= rollbackTick)
            {
                _spawnedHistory.Remove(entry.Key);
                
                // remove objects from object to tick map
                foreach(var objectEntry in entry.Value)
                {
                    if(objectEntry != null)
                    {
                        Destroy(objectEntry);
                    }
                    _objectToTickMap.Remove(objectEntry);
                }
            }
        }
    }

    public static void CleanupTickFarInThePast()
    {
        // TODO: This should cleanup ticks at CustomPhysics.TIck - HistoryLength?
    }

    public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion? rotation = null)
    {
        if(rotation == null)
        {
            rotation = Quaternion.identity;
        }

        GameObject newObject = MonoBehaviour.Instantiate(prefab, position, (Quaternion)rotation);
        TrapPlacementArea.Singleton.RegisterScopedObject(newObject);
        RecordObjectCreationAtCurrentTick(newObject);
        return newObject;
    }


    public static GameObject Instantiate(GameObject prefab, Transform parent)
    {
        GameObject newObject = MonoBehaviour.Instantiate(prefab, parent);
        TrapPlacementArea.Singleton.RegisterScopedObject(newObject);
        RecordObjectCreationAtCurrentTick(newObject);
        return newObject;
    }


}
