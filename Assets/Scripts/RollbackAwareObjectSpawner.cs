using System;
using UnityEngine;

/// <summary>
/// Provides a way to spawn objects at specific ticks, ensuring when rollback occurs those objects are reverted
/// </summary>
public class RollbackAwareObjectSpawner : MonoBehaviour
{
    private static void RecordObjectCreationAtCurrentTick(GameObject newObject)
    {
        // TODO: this should store the object at CustomPhysics.Tick, ensuring it gets removed if we rollback past said tick
    }

    public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion? rotation = null)
    {
        if(rotation == null)
        {
            rotation = Quaternion.identity;
        }

        GameObject newObject = MonoBehaviour.Instantiate(prefab, position, (Quaternion)rotation);
        RecordObjectCreationAtCurrentTick(newObject);
        return newObject;
    }


    public static GameObject Instantiate(GameObject prefab, Transform parent)
    {
        GameObject newObject = MonoBehaviour.Instantiate(prefab, parent);
        RecordObjectCreationAtCurrentTick(newObject);
        return newObject;
    }


}
