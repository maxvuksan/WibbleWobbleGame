using FixMath.NET;
using UnityEngine;

public class AlwaysMaintainRotation : MonoBehaviour
{

    [SerializeField] private float fixedRotation = 0;
    [SerializeField] private CustomPhysicsBody _bodyToApplyTo; // if this is left blank, we set the transform rotation directly.


    void Awake()
    {
        CustomPhysics.OnPostPhysicsTick += OnPostPhysicsTick;    
    }

    void OnDestroy()
    {
        CustomPhysics.OnPostPhysicsTick -= OnPostPhysicsTick;    
    }

    void OnPostPhysicsTick()
    {
        if(_bodyToApplyTo != null)
        {
            _bodyToApplyTo.Angle = (Fix64)fixedRotation;
        }
    }

    void LateUpdate()
    {
        if(_bodyToApplyTo == null || CustomPhysics.Tick == 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, fixedRotation);
        }
    }

}
