using UnityEngine;

public class TrapLiftTrigger : MonoBehaviour
{

    private Trap_Lift _trapLift;

    void Awake()
    {
        _trapLift = GetComponentInParent<Trap_Lift>();
    }

    public void OnTriggerStay2D(Collider2D collision)
    {
        print("TRIGGER ENTER");
        _trapLift.ReactToOnTriggerStay(collision);
    }
}
