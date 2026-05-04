using UnityEngine;
using Volatile;

public class Attacher : MonoBehaviour
{
    
    public CustomPhysicsBody BodyToAttach;
    public IntHundredthVector2 VectorOffset;
    [SerializeField] private IntHundredth _attachRadius;

    private bool _attached;        
    private TrapHeader _header;

    void Awake()
    {
        _attached = false;

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
        if(CustomPhysics.Tick == 5)
        {
            AttemptAttach();
        }
    }

    public void AttemptAttach()
    {
        if (_attached)
        {
            return;
        }
        _attached = true;

        VoltVector2 sensorWorldPos = BodyToAttach.Position + Helpers.RotatePosition(VectorOffset, BodyToAttach.Body.Angle);

        var result = CustomPhysics.OverlapPoint(sensorWorldPos);

        print("AttackAttempt: " + result.Bodies.Count + ", " + sensorWorldPos.x + ", " + sensorWorldPos.y);


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

        // Use transform rotation for visualization
        Vector2 rotatedOffset = Quaternion.Euler(0, 0, BodyToAttach.transform.eulerAngles.z) * VectorOffset.AsVector2();
        Vector3 attachPosition = (Vector2)BodyToAttach.transform.position + rotatedOffset;
        
        Gizmos.color = Color.purple;
        Gizmos.DrawWireSphere(attachPosition, _attachRadius.AsFloat());
    }
}
