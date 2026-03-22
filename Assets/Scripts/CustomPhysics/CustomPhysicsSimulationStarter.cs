using System.Linq;
using UnityEngine;

public class CustomPhysicsSimulationStarter : MonoBehaviour
{

    public bool SimulateRollbacks = true;

    void Start()
    {
        CustomPhysicsBody[] bodies = FindObjectsByType<CustomPhysicsBody>(FindObjectsSortMode.None);
        var listBodies = bodies.ToList<CustomPhysicsBody>();

        // sort by position
        listBodies.Sort((a, b) => {

            int cmp = a.GetComponent<CustomTransform>().PositionXHundredth.AsFix64().CompareTo(b.GetComponent<CustomTransform>().PositionXHundredth);
            if (cmp != 0){ 
                return cmp;
            }
            return a.GetComponent<CustomTransform>().PositionYHundredth.AsFix64().CompareTo(b.GetComponent<CustomTransform>().PositionYHundredth);
        });

        for(int i = 0; i < listBodies.Count; i++)
        {
            ulong entityId = (ulong)i + 100ul;
            
            // add +100 offset to ensure players ids are before traps
            listBodies[i].SetDesiredEntityId(entityId);

            if (Configuration.Singleton.DebugMode)
            {
                DeterminismLogger.LogExtraInfo("RecomputeEntityIds for trap: " + listBodies[i].gameObject.name + ", new EntityId " + entityId);
            }
        }


        CustomPhysics.OnPhysicsTick += OnPhysicsTick;

        CustomPhysics.RecomputeEntityIds();
 
        CustomPhysics.ScheduleStart(Time.realtimeSinceStartupAsDouble + 2.0d);
    }

    public void OnPhysicsTick()
    {

        if (!SimulateRollbacks)
        {
            return;
        }

        if (CustomPhysics.Resimulating)
        {
            return;
        }

        // simulate rollbacks...

        if(CustomPhysics.Tick == 300)
        {
            CustomPhysics.RequestRollbackAndResimulate(290);
        }
        if(CustomPhysics.Tick == 400)
        {
            CustomPhysics.RequestRollbackAndResimulate(390);
        }
        if(CustomPhysics.Tick == 500)
        {
            CustomPhysics.RequestRollbackAndResimulate(490);
        }
        if(CustomPhysics.Tick == 600)
        {
            CustomPhysics.RequestRollbackAndResimulate(590);
        }
    }
}
