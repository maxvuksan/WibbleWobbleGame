using FixMath.NET;
using Unity.Netcode;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private float _speed;
    [SerializeField] private CustomPhysicsBody _bodyToApplyTo; // if this is left blank, we set the transform rotation directly.
    private Fix64 _speedFix64;

    void Awake()
    {
        _speedFix64 = (Fix64)_speed;
        if(_bodyToApplyTo != null)
        {
            _bodyToApplyTo.Body.AngularDamping = Fix64.One;
            CustomPhysics.OnPrePhysicsTick += OnPrePhysicsTick;    
        }
    }

    void OnDestroy()
    {
        if(_bodyToApplyTo != null)
        {
            CustomPhysics.OnPrePhysicsTick -= OnPrePhysicsTick;    
        }
    }

    void OnPrePhysicsTick()
    {
        // rotate through physics if the body is assigned
        if(_bodyToApplyTo != null)
        {
            _bodyToApplyTo.Body.AngularVelocity = ((Fix64)_speedFix64);
        }
    }

    public void Update()
    {
        // rotate through transform if no physics body is assigned
        if(_bodyToApplyTo == null)
        {
            transform.Rotate(new Vector3(0,0, _speed * Time.deltaTime));
        }
    }
}
