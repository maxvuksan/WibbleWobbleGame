using UnityEngine;
using Volatile;

public class Attacher : MonoBehaviour
{
    
    public CustomPhysicsBody BodyToAttach;
    public IntHundredthVector2 VectorOffset;
    [SerializeField] private IntHundredth _attachRadius;

    private TrapHeader _header;

    void Awake()
    {
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;

        _header = BodyToAttach.GetComponent<TrapHeader>();
        if(_header == null)
        {
            _header = BodyToAttach.GetComponentInParent<TrapHeader>();
        }
    }

    void OnDestroy()
    {
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
    }


    public void OnPhysicsTick()
    {
        if(CustomPhysics.Tick == 0)
        {
            AttemptAttach();
        }
    }

    public void AttemptAttach()
    {
        VoltVector2 sensorWorldPos = BodyToAttach.Position + Helpers.RotatePosition(VectorOffset, BodyToAttach.Body.Angle);

        var result = CustomPhysics.OverlapCircle(sensorWorldPos, _attachRadius);

        foreach(var otherBody in result.Bodies)
        {
            if(otherBody == BodyToAttach) continue;
            
            print("Overlap length: " + otherBody.name);

            var otherHeader = otherBody.GetComponent<TrapHeader>() ?? otherBody.GetComponentInParent<TrapHeader>();

            if(otherHeader != null)
            {
                // We found a valid parent. Pass the responsibility to the parent header.
                otherHeader.AttachChildBody(_header, BodyToAttach, otherBody);
                break; 
            }
        } 
    }


    public void OnDrawGizmos()
    {
        if(BodyToAttach == null)
        {
            return;
        }

        Vector3 attachPosition = (Vector2)BodyToAttach.transform.position + VectorOffset.AsVector2();
        Gizmos.DrawSphere(attachPosition, _attachRadius.AsFloat());
    }
}
