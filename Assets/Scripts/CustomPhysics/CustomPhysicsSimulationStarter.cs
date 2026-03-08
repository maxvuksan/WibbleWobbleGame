using UnityEngine;

public class CustomPhysicsSimulationStarter : MonoBehaviour
{
    void Start()
    {
        CustomPhysics.ScheduleStart(Time.realtimeSinceStartupAsDouble + 2.0d);
    }
}
